using System.Buffers.Binary;

namespace Iamp.Protocol;

/// <summary>F15i V1.1.9 chain operations captured from iAMP. No preset save is performed.</summary>
public static class EffectChainCommands
{
    public static IReadOnlyList<PresetEffectBlock> Active(IReadOnlyList<PresetEffectBlock> blocks)
    {
        if (blocks.Count != 20 || blocks.Where((b,i)=>b.Position!=i+1).Any())
            throw new NotSupportedException("A complete 20-record preset is required.");
        var active=blocks.TakeWhile(b=>b.TypeCode!=0).ToArray();
        if (blocks.Skip(active.Length).Any(b=>b.TypeCode!=0 || b.Values.Count!=0) ||
            active.Any(b=>b.Values.Count<2 || b.Values[0]>1))
            throw new NotSupportedException("The effect chain differs from the observed contiguous layout.");
        return active;
    }

    public static byte[] Move(IReadOnlyList<PresetEffectBlock> blocks,byte from,byte to,byte token)
    {
        var active=Active(blocks);RequirePosition(active,from);RequirePosition(active,to);
        return [0x2D,token,4,from,to];
    }
    public static byte[] Remove(IReadOnlyList<PresetEffectBlock> blocks,byte position,byte token)
    {RequirePosition(Active(blocks),position);return [0x2D,token,3,position];}
    public static byte[] Add(IReadOnlyList<PresetEffectBlock> blocks,EffectDefinition effect,byte token)
    {
        var active=Active(blocks);
        if(active.Count>=20 ||
            !PresetEffectSearch.Catalog.Contains(effect) || effect.Model>byte.MaxValue)
            throw new NotSupportedException("Choose an available built-in effect category.");
        return [0x2D,token,1,checked((byte)(active.Count+1)),effect.TypeCode,checked((byte)effect.Model)];
    }
    public static byte[] Replace(IReadOnlyList<PresetEffectBlock> blocks,byte position,EffectDefinition effect,byte token)
    {
        RequirePosition(Active(blocks),position);
        if(!PresetEffectSearch.Catalog.Contains(effect) || effect.Model>byte.MaxValue)
            throw new NotSupportedException("Choose an available built-in effect model.");
        return [0x2D,token,2,position,effect.TypeCode,checked((byte)effect.Model)];
    }
    public static bool IsExpectedReplacement(ReadOnlySpan<byte> before,ReadOnlySpan<byte> after,byte position,EffectDefinition effect)
    {
        var original=Active(PresetDataParser.Parse(before).Blocks);
        RequirePosition(original,position);
        var actual=Active(PresetDataParser.Parse(after).Blocks).ToList();
        if(actual.Count!=original.Count)return false;
        var replaced=actual[position-1];
        if(replaced.TypeCode!=effect.TypeCode || replaced.Values.Count<2 || replaced.Values[0]!=1 || replaced.Values[1]!=effect.Model)return false;
        // The amp supplies defaults for the selected model. Restore the original
        // record for comparison so every unrelated record/header/trailer is checked.
        actual[position-1]=original[position-1];
        return MatchesChainDetail(before,Rebuild(after,actual),original[position-1].TypeCode==2 || effect.TypeCode==2);
    }
    public static byte[] ExpectedMove(ReadOnlySpan<byte> detail,byte from,byte to)
    {
        var decoded=PresetDataParser.Parse(detail);var active=Active(decoded.Blocks).ToList();
        RequirePosition(active,from);RequirePosition(active,to);
        var block=active[from-1];active.RemoveAt(from-1);active.Insert(to-1,block);
        return Rebuild(detail,active);
    }
    public static byte[] ExpectedRemove(ReadOnlySpan<byte> detail,byte position)
    {
        var decoded=PresetDataParser.Parse(detail);var active=Active(decoded.Blocks).ToList();
        RequirePosition(active,position);active.RemoveAt(position-1);return Rebuild(detail,active);
    }
    public static bool IsExpectedAddition(ReadOnlySpan<byte> before,ReadOnlySpan<byte> after,EffectDefinition effect)
    {
        var original=PresetDataParser.Parse(before);var actual=PresetDataParser.Parse(after);
        var active=Active(original.Blocks);var added=Active(actual.Blocks);
        if(added.Count!=active.Count+1)return false;
        var last=added[^1];
        if(last.TypeCode!=effect.TypeCode || last.Values.Count<2 || last.Values[0]!=1 || last.Values[1]!=effect.Model)return false;
        // Removing the new record must recover every original byte, including opaque fields.
        return MatchesChainDetail(before,ExpectedRemove(after,last.Position),effect.TypeCode==2);
    }
    public static bool IsExpectedRemoval(ReadOnlySpan<byte> before,ReadOnlySpan<byte> after,byte position)
        =>MatchesChainDetail(ExpectedRemove(before,position),after,PresetDataParser.Parse(before).Blocks[position-1].TypeCode==2);
    public static bool IsExpectedMove(ReadOnlySpan<byte> before,ReadOnlySpan<byte> after,byte from,byte to)
        =>MatchesChainDetail(ExpectedMove(before,from,to),after,PresetDataParser.Parse(before).Blocks[from-1].TypeCode==2);

    /// <summary>Amp chain edits can recompute the opaque ninth Amp word. Only
    /// Boolean changes in matching nine-word Amp records are accepted; every
    /// other byte, including global state (checked by the caller), is preserved.</summary>
    public static bool MatchesChainDetail(ReadOnlySpan<byte> expected,ReadOnlySpan<byte> actual,bool allowAmpFlagUpdates)
    {
        if(expected.SequenceEqual(actual))return true;
        if(!allowAmpFlagUpdates)return false;
        var a=PresetDataParser.Parse(expected);var b=PresetDataParser.Parse(actual);var normalized=expected.ToArray();
        for(var i=0;i<a.Blocks.Count;i++) {
            var left=a.Blocks[i];var right=b.Blocks[i];
            if(left.TypeCode==2 && right.TypeCode==2 && left.Values.Count==9 && right.Values.Count==9 &&
                left.Values[8]<=1 && right.Values[8]<=1)
                normalized=PresetDataParser.WithParameterValue(normalized,left.Position,8,right.Values[8]);
        }
        return normalized.AsSpan().SequenceEqual(actual);
    }
    private static void RequirePosition(IReadOnlyList<PresetEffectBlock> active,byte position)
    {if(position<1 || position>active.Count)throw new ArgumentOutOfRangeException(nameof(position));}
    private static byte[] Rebuild(ReadOnlySpan<byte> detail,IReadOnlyList<PresetEffectBlock> active)
    {
        var decoded=PresetDataParser.Parse(detail);
        var result=new byte[24+60+active.Sum(b=>b.Values.Count*2)];detail[..22].CopyTo(result);
        var offset=22;
        for(var i=0;i<20;i++) {
            result[offset++]=(byte)(i+1);
            var b=i<active.Count?active[i]:null;
            result[offset++]=b?.TypeCode??0;result[offset++]=checked((byte)((b?.Values.Count??0)*2));
            if(b is null)continue;
            foreach(var v in b.Values){BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset,2),v);offset+=2;}
        }
        decoded.UnknownTrailer.CopyTo(result,offset);return result;
    }
}
