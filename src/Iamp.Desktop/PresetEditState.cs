using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

/// <summary>UI edit equality, separate from the strict byte checks used for writes and saves.</summary>
internal static class PresetEditState
{
    internal static CurrentPresetSnapshot Freeze(CurrentPresetSnapshot preset)=>preset with {
        Blocks=preset.Blocks.Select(block=>block with {Values=block.Values.ToArray()}).ToArray()
    };

    internal static bool Equivalent(CurrentPresetSnapshot baseline,CurrentPresetSnapshot current)
    {
        if(baseline.WireIndex!=current.WireIndex||baseline.Name!=current.Name||baseline.Blocks.Count!=current.Blocks.Count)return false;
        for(var i=0;i<baseline.Blocks.Count;i++){
            var left=baseline.Blocks[i];var right=current.Blocks[i];
            if(left.Position!=right.Position||left.TypeCode!=right.TypeCode||left.Values.Count!=right.Values.Count)return false;
            for(var word=0;word<left.Values.Count;word++){
                // Captured chain operations recalculate this Boolean Amp companion word.
                // It is not an editable setting; on/off is independently compared at word 0.
                if(word==8&&AmpParameters.IsKnownLayout(left)&&AmpParameters.IsKnownLayout(right)&&left.Values[8]<=1&&right.Values[8]<=1)continue;
                if(left.Values[word]!=right.Values[word])return false;
            }
        }
        // Retain differences in other opaque preset data when complete records are available.
        try{
            var left=PresetDataParser.Parse(Convert.FromHexString(baseline.RawDetailPayloadHex));
            var right=PresetDataParser.Parse(Convert.FromHexString(current.RawDetailPayloadHex));
            if(!left.UnknownTrailer.AsSpan().SequenceEqual(right.UnknownTrailer))return false;
        }catch(FormatException){/* Synthetic/offline views can contain decoded blocks only. */}
        return true;
    }
}
