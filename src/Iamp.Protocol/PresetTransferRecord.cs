namespace Iamp.Protocol;

/// <summary>Complete preset content, excluding amplifier-wide state. No commands are sent here.</summary>
public static class PresetTransferRecord
{
    public const int MaximumDetailBytes = 5184;

    public static DecodedPreset Validate(string firmware, ReadOnlySpan<byte> detail)
    {
        if (firmware != "V1.1.9") throw new NotSupportedException("Preset import/export supports F15i V1.1.9 only.");
        if (detail.Length > MaximumDetailBytes) throw new FormatException("Preset data exceeds the supported size.");
        var decoded = PresetDataParser.Parse(detail);
        if (!detail.Slice(2,20).SequenceEqual(PresetSaveCommand.EncodeName(decoded.Name)))
            throw new FormatException("Preset name bytes are not supported printable ASCII.");
        foreach (var block in EffectChainCommands.Active(decoded.Blocks))
        {
            var profile = ObservedParameterCommands.Profile(block);
            if (profile == 0 || !EffectBypassCommand.TryGetEnabled(block, out _))
                throw new NotSupportedException("The preset contains an unsupported effect model or layout.");
            // Reuse the mobile-verified ranges, including signed/stepped encodings.
            var values=block.Values.ToArray();values[0]=1;
            var enabled=block with {Values=values};
            foreach(var parameter in Enum.GetValues<ObservedParameter>())
            {
                var definition=ObservedParameterCommands.Definition(parameter);
                if(definition.Position==profile)
                    _=ObservedParameterCommands.Create(enabled,parameter,values[definition.ValueIndex]);
            }
        }
        return decoded;
    }

    public static byte[] Retarget(string firmware, ReadOnlySpan<byte> detail, byte destination)
    {
        var preset=Validate(firmware,detail);
        return PresetSaveCommand.ExpectedDetail(detail,destination,preset.Name);
    }
}
