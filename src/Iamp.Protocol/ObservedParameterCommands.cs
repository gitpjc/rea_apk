using System.Buffers.Binary;

namespace Iamp.Protocol;

public enum ObservedParameter { NoiseThreshold, NoiseLevel, OdGain, OdTone, OdVolume, AmpBass, AmpMid, AmpTreble, AmpPresence, AmpGain, AmpMaster, DelayLevel, DelayFeedback, DelayTime, ReverbPreDelay, ReverbLevel, ReverbDecay, ReverbTone,
    RedCompSensitivity, RedCompLevel, YellowCompAttack, YellowCompRatio, YellowCompThreshold, YellowCompLevel, IntelReducerThreshold, IntelReducerDepth, IntelReducerLevel,
    TouchWahAttack, TouchWahPeak, TouchWahSensitivity, TouchWahLevel, AutoWahRate, AutoWahPeak, AutoWahRange, AutoWahLevel, QFilterRate, QFilterQ, QFilterMix,
    Eq100Hz, Eq250Hz, Eq630Hz, Eq1600Hz, Eq4000Hz, EqLevel,
    CustomEqBand1Gain, CustomEqBand1Freq, CustomEqBand1Q, CustomEqBand2Gain, CustomEqBand2Freq, CustomEqBand2Q,
    CustomEqBand3Gain, CustomEqBand3Freq, CustomEqBand3Q, CustomEqBand4Gain, CustomEqBand4Freq, CustomEqBand4Q,
    CustomEqLowCut, CustomEqHighCut, CustomEqLevel,
    DelayCrystalLevel, DelayCrystalFeedback, DelayCrystalTime, DelayCrystalDepth, DelayCrystalRate, DelayVintageDelayLevel, DelayVintageDelayFeedback, DelayVintageDelayTime, DelayVintageDelayBit, DelayVintageDelaySRate, DelayGalaxyDelayLevel, DelayGalaxyDelayFeedback, DelayGalaxyDelayTime, DelayGalaxyDelayModRate, DelayGalaxyDelayModDepth, DelaySweepLevel, DelaySweepFeedback, DelaySweepTime, DelaySweepRate, DelaySweepRange,
    ReverbFlReverbPreDelay, ReverbFlReverbLevel, ReverbFlReverbDecay, ReverbFlReverbTone, ReverbFlReverbRate, ReverbFlReverbFeedback, ReverbFlReverbModDelay, ReverbFlReverbModLevel, ReverbDistReverbPreDelay, ReverbDistReverbLevel, ReverbDistReverbDecay, ReverbDistReverbTone, ReverbDistReverbGain, ReverbDistReverbDistLevel,
    ModPhaserRate, ModPhaserLevel, ModPhaserDepth, ModFlangerRate, ModFlangerMix, ModFlangerFeedback, ModTremoloRate, ModTremoloMix, ModTremoloTone, ModPolyShiftMix, ModPolyShiftTone, ModPolyShiftPitch, ModAnaChorusRate, ModAnaChorusMix, ModAnaChorusTone, ModAnaChorusDepth, ModLofiSample, ModLofiMix, ModLofiBit, ModSlowGearRise, ModSlowGearLevel, ModPannerRate, ModPannerTone, ModPannerDepth, ModPannerDuty, ModDetunePitch, ModDetuneTone, ModDetuneMix, ModOctaveSub, ModOctaveSubTone, ModOctaveUpper, ModOctaveUpperTone, ModOctaveDry }
public sealed record ObservedParameterDefinition(byte Position, int ValueIndex, ushort Minimum, ushort Maximum);

/// <summary>Captured F15i V1.1.9 model profiles and verified ranges only. Unknown fields are preserved.</summary>
public static class ObservedParameterCommands
{
    public static ObservedParameterDefinition Definition(ObservedParameter parameter) => parameter switch
    {
        ObservedParameter.NoiseThreshold => new(1, 2, 0, 100),
        ObservedParameter.NoiseLevel => new(1, 3, 0, 100),
        ObservedParameter.OdGain => new(2, 2, 0, 100),
        ObservedParameter.OdTone => new(2, 3, 0, 100),
        ObservedParameter.OdVolume => new(2, 4, 0, 100),
        ObservedParameter.AmpBass => new(3, 3, 0, 100),
        ObservedParameter.AmpMid => new(3, 4, 0, 100),
        ObservedParameter.AmpTreble => new(3, 5, 0, 100),
        ObservedParameter.AmpPresence => new(3, 6, 0, 100),
        ObservedParameter.AmpGain => new(3, 2, 0, 100),
        ObservedParameter.AmpMaster => new(3, 7, 0, 100),
        ObservedParameter.DelayLevel => new(4, 2, 0, 100),
        ObservedParameter.DelayFeedback => new(4, 3, 0, 100),
        ObservedParameter.DelayTime => new(4, 4, 40, 2500),
        ObservedParameter.ReverbPreDelay => new(5, 2, 0, 200),
        ObservedParameter.ReverbLevel => new(5, 3, 0, 100),
        ObservedParameter.ReverbDecay => new(5, 4, 0, 100),
        ObservedParameter.ReverbTone => new(5, 5, 0, 100),
        ObservedParameter.RedCompSensitivity => new(6, 2, 0, 100),
        ObservedParameter.RedCompLevel => new(6, 3, 0, 100),
        ObservedParameter.YellowCompAttack => new(7, 2, 0, 100),
        ObservedParameter.YellowCompRatio => new(7, 3, 0, 100),
        ObservedParameter.YellowCompThreshold => new(7, 4, 0, 100),
        ObservedParameter.YellowCompLevel => new(7, 5, 0, 100),
        ObservedParameter.IntelReducerThreshold => new(8, 2, 0, 100),
        ObservedParameter.IntelReducerDepth => new(8, 3, 0, 100),
        ObservedParameter.IntelReducerLevel => new(8, 4, 0, 100),
        ObservedParameter.TouchWahAttack => new(9, 2, 0, 100),
        ObservedParameter.TouchWahPeak => new(9, 3, 0, 100),
        ObservedParameter.TouchWahSensitivity => new(9, 4, 0, 100),
        ObservedParameter.TouchWahLevel => new(9, 5, 0, 100),
        ObservedParameter.AutoWahRate => new(10, 2, 0, 100),
        ObservedParameter.AutoWahPeak => new(10, 3, 0, 100),
        ObservedParameter.AutoWahRange => new(10, 4, 0, 100),
        ObservedParameter.AutoWahLevel => new(10, 5, 0, 100),
        ObservedParameter.QFilterRate => new(11, 2, 0, 100),
        ObservedParameter.QFilterQ => new(11, 3, 0, 100),
        ObservedParameter.QFilterMix => new(11, 4, 0, 100),
        _ => DelayParameters.Get(parameter) is {} delay?new(delay.Profile,delay.ValueIndex,delay.Minimum,delay.Maximum):ReverbParameters.Get(parameter) is {} reverb?new(reverb.Profile,reverb.ValueIndex,reverb.Minimum,reverb.Maximum):ModParameters.Get(parameter) is {} mod?new(mod.Profile,mod.ValueIndex,mod.Minimum,mod.Maximum):EqParameters.Get(parameter) is {} eq?new(eq.Profile,eq.ValueIndex,eq.WireValues[0],eq.WireValues[^1]):throw new ArgumentOutOfRangeException(nameof(parameter))
    };

    public static byte[] Create(PresetEffectBlock block, ObservedParameter parameter, ushort value)
    {
        var definition = Definition(parameter);
        var v = block.Values;
        var supported = block.Position is >= 1 and <= 20 && (definition.Position switch {
            1 => block.TypeCode == 4 && v.Count == 6 && v[0] == 1 && v[1] == 2 &&
                v[2] <= 100 && v[3] <= 100 && v[4] == 0 && v[5] == 0,
            2 => OdParameters.IsSupported(block),
            3 => AmpParameters.IsSupported(block),
            4 => Profile(block)==4 && DelayParameters.IsSupported(block),
            5 => Profile(block)==5 && ReverbParameters.IsSupported(block),
            6 => Profile(block)==6 && v[0]==1 && v[2]<=100 && v[3]<=100 && v[4]==0 && v[5]==0,
            7 => Profile(block)==7 && v[0]==1 && v.Skip(2).All(value=>value<=100),
            8 => Profile(block)==8 && v[0]==1 && v[2]<=100 && v[3]<=100 && v[4]<=100 && v[5]==0,
            9 or 10 => Profile(block)==definition.Position && v[0]==1 && v.Skip(2).All(value=>value<=100),
            11 => Profile(block)==11 && v[0]==1 && v[2]<=100 && v[3]<=100 && v[4]<=100 && v[5]==0,
            12 or 13 => Profile(block)==definition.Position && EqParameters.IsSupported(block),
            _ => Profile(block)==definition.Position && ((DelayParameters.Get(parameter) is not null && DelayParameters.IsSupported(block)) || (ReverbParameters.Get(parameter) is not null && ReverbParameters.IsSupported(block)) || (ModParameters.Get(parameter) is not null && ModParameters.IsSupported(block)))
        });
        if (!supported) throw new NotSupportedException("Effect differs from the captured configuration or supported ranges.");
        if (value < definition.Minimum || value > definition.Maximum)
            throw new ArgumentOutOfRangeException(nameof(value), $"Supported range is {definition.Minimum}–{definition.Maximum}.");
        if(EqParameters.Get(parameter) is {} spec && !spec.WireValues.Contains(value))
            throw new ArgumentOutOfRangeException(nameof(value),"Choose a supported EQ step.");
        var payload = new byte[4 + v.Count * 2];
        payload[0] = 0x25; payload[1] = block.Position; payload[2] = block.TypeCode; payload[3] = checked((byte)(v.Count * 2));
        for (var i = 0; i < v.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2), i == definition.ValueIndex ? value : v[i]);
        return payload;
    }

    /// <summary>Stable editor profile identifier, independent of the physical chain position.
    /// Zero means the type/model/word-count layout has not been verified.</summary>
    public static byte Profile(PresetEffectBlock block)=>(block.TypeCode,block.Values.Count,block.Values.Count>1?block.Values[1]:-1) switch {
        (4,6,2)=>1,(1,5,_) when OdParameters.IsKnownLayout(block)=>2,(2,9,_) when AmpParameters.IsKnownLayout(block)=>3,(7,8,_)=>DelayParameters.Profile(block),(8,10,_)=>ReverbParameters.Profile(block),
        (6,_,_)=>ModParameters.Profile(block),(4,6,0)=>6,(4,6,1)=>7,(4,6,3)=>8,(5,6,0)=>9,(5,6,1)=>10,(5,6,2)=>11,(9,17,0)=>12,(9,17,1)=>13,_=>0
    };

    /// <summary>Find a unique captured model at its current chain position. Ambiguous duplicates are not editable.</summary>
    public static PresetEffectBlock? FindSupportedBlock(IEnumerable<PresetEffectBlock> blocks, ObservedParameter parameter)
    {
        var definition = Definition(parameter);
        var candidates = blocks.Where(block => Profile(block)==definition.Position).Take(2).ToArray();
        if (candidates.Length != 1) return null;
        try { Create(candidates[0], parameter, definition.Minimum); return candidates[0]; }
        catch (NotSupportedException) { return null; }
    }
}
