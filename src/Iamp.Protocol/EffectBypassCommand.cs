using System.Buffers.Binary;

namespace Iamp.Protocol;

/// <summary>F15i effect on/off state for verified model layouts only.</summary>
public static class EffectBypassCommand
{
    public static bool TryGetEnabled(PresetEffectBlock block, out bool enabled)
    {
        enabled=false;
        if(block.Position is <1 or >20 || block.Values.Count<2 || block.Values[0]>1)return false;
        var known=AmpParameters.IsKnownLayout(block) || OdParameters.IsKnownLayout(block) || DelayParameters.IsKnownLayout(block) || ReverbParameters.IsKnownLayout(block) || ModParameters.IsKnownLayout(block) || (block.TypeCode,block.Values.Count,block.Values[1]) is
            (4,6,0) or (4,6,1) or (4,6,2) or (4,6,3) or (5,6,0) or (5,6,1) or (5,6,2) or (9,17,0) or (9,17,1);
        if(!known || (block.TypeCode==2 && block.Values[8]>1))return false;
        enabled=block.Values[0]==1;return true;
    }

    public static byte[] Create(PresetEffectBlock block, bool enabled)
    {
        if(!TryGetEnabled(block,out _))throw new NotSupportedException("Effect on/off is not verified for this model or state.");
        var payload=new byte[4+block.Values.Count*2];
        payload[0]=0x25;payload[1]=block.Position;payload[2]=block.TypeCode;payload[3]=checked((byte)(block.Values.Count*2));
        for(var i=0;i<block.Values.Count;i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4+i*2,2),i==0?(ushort)(enabled?1:0):block.Values[i]);
        return payload;
    }

    /// <summary>PLX 100 readback mirrors on/off in its ninth word; other words remain unchanged.</summary>
    public static ushort[] ExpectedValues(PresetEffectBlock block,bool enabled)
    {
        if(!TryGetEnabled(block,out _))throw new NotSupportedException("Unverified effect layout.");
        var values=block.Values.ToArray();values[0]=(ushort)(enabled?1:0);
        if(block.TypeCode==2)values[8]=values[0];
        return values;
    }
}
