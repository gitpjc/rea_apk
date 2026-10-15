using System.Globalization;

namespace Iamp.Protocol;

public enum EqValueKind { Gain, Frequency, Q, Cut, Level }

public sealed record EqParameterSpec(ObservedParameter Parameter, byte Profile, int ValueIndex,
    string Name, string? Unit, string? Group, EqValueKind Kind, IReadOnlyList<ushort> WireValues)
{
    public decimal DisplayValue(ushort wire)=>Kind switch {EqValueKind.Gain=>(wire-32)/2m,EqValueKind.Q=>(wire+2)/10m,_=>wire};
    public bool IsOff(ushort wire)=>Kind==EqValueKind.Cut && wire==0;
    public string Format(ushort wire)=>IsOff(wire)?"Off":DisplayValue(wire).ToString(Kind is EqValueKind.Gain or EqValueKind.Q?"0.0":"0",CultureInfo.InvariantCulture);
    public bool TryParse(string text,out ushort wire)
    {
        wire=0;
        if(Kind==EqValueKind.Cut && text.Equals("Off",StringComparison.OrdinalIgnoreCase))return true;
        if(!decimal.TryParse(text,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))return false;
        var encoded=Kind switch {EqValueKind.Gain=>value*2+32,EqValueKind.Q=>value*10-2,_=>value};
        if(encoded<0 || encoded>ushort.MaxValue || encoded!=decimal.Truncate(encoded))return false;
        wire=(ushort)encoded;return WireValues.Contains(wire);
    }
}

/// <summary>Preset EQ layouts captured from iAMP; never the amplifier's global EQ.</summary>
public static class EqParameters
{
    private static ushort[] Range(int minimum,int maximum)=>Enumerable.Range(minimum,maximum-minimum+1).Select(v=>(ushort)v).ToArray();
    private static ushort[] Frequencies(int minimum,int maximum,bool cut=false)
    {
        var values=new List<ushort>();if(cut)values.Add(0);
        for(var v=minimum;v<=maximum;v+=v<100?1:v<1000?10:v<10000?100:1000)values.Add((ushort)v);
        return values.ToArray();
    }
    public static IReadOnlyList<EqParameterSpec> All {get;}=Build();
    private static EqParameterSpec[] Build()
    {
        var result=new List<EqParameterSpec>();
        var fixedNames=new[]{"100Hz","250Hz","630Hz","1.6kHz","4kHz"};
        for(var i=0;i<5;i++)result.Add(new((ObservedParameter)((int)ObservedParameter.Eq100Hz+i),12,i+2,fixedNames[i],"dB",null,EqValueKind.Gain,Range(0,64)));
        result.Add(new(ObservedParameter.EqLevel,12,7,"Level",null,null,EqValueKind.Level,Range(0,100)));
        var groups=new[]{"Low","Low Mid","High Mid","High"};
        var frequencies=new[]{(40,600),(200,2000),(600,7000),(1500,20000)};
        for(var band=0;band<4;band++) {
            var parameter=(int)ObservedParameter.CustomEqBand1Gain+band*3;var index=2+band*3;
            result.Add(new((ObservedParameter)parameter,13,index,$"Band {band+1} Gain","dB",groups[band],EqValueKind.Gain,Range(0,64)));
            result.Add(new((ObservedParameter)(parameter+1),13,index+1,$"Band {band+1} Freq","Hz",groups[band],EqValueKind.Frequency,Frequencies(frequencies[band].Item1,frequencies[band].Item2)));
            result.Add(new((ObservedParameter)(parameter+2),13,index+2,$"Band {band+1} Q",null,groups[band],EqValueKind.Q,Range(0,98)));
        }
        result.Add(new(ObservedParameter.CustomEqLowCut,13,14,"Low Cut","Hz",null,EqValueKind.Cut,Frequencies(20,600,true)));
        result.Add(new(ObservedParameter.CustomEqHighCut,13,15,"High Cut","Hz",null,EqValueKind.Cut,Frequencies(2000,20000,true)));
        result.Add(new(ObservedParameter.CustomEqLevel,13,16,"Level",null,null,EqValueKind.Level,Range(0,100)));
        return result.ToArray();
    }
    public static EqParameterSpec? Get(ObservedParameter parameter)=>All.FirstOrDefault(s=>s.Parameter==parameter);
    public static IReadOnlyList<EqParameterSpec> For(PresetEffectBlock block)
        =>block.TypeCode==9 && block.Values.Count==17 && block.Values[1]<=1?All.Where(s=>s.Profile==12+block.Values[1]).ToArray():Array.Empty<EqParameterSpec>();
    public static EqParameterSpec? Find(PresetEffectBlock block,int index)=>For(block).FirstOrDefault(s=>s.ValueIndex==index);
    public static bool IsSupported(PresetEffectBlock block)
    {
        var specs=For(block);
        return specs.Count>0 && block.Values[0]==1 && specs.All(s=>s.WireValues.Contains(block.Values[s.ValueIndex])) &&
            (block.Values[1]!=0 || block.Values.Skip(8).All(v=>v==0));
    }
}
