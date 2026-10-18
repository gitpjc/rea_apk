using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class TunerCapturedFrameTests
{
    [Fact]
    public void IndependentlyObservedMobileDisplaysMatchDecodedMeterClassification()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-tuner-observations.json")));
        foreach(var row in json.RootElement.GetProperty("displayObservations").EnumerateArray())
        {
            Assert.True(TunerReading.TryParse(Convert.FromHexString(row.GetProperty("payloadHex").GetString()!),out var parsed));
            Assert.Equal(row.GetProperty("label").GetString(),parsed!.Value.Direction);
            Assert.Equal(row.GetProperty("litSegments").EnumerateArray().Select(x=>x.GetInt32()),parsed.Value.DisplayPositions);
        }
    }

    [Fact]
    public void IndependentCapturedFramesDecodeAndCoverAllNotesAndReferences()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-tuner-observations.json")));
        var root=json.RootElement;var notes=new HashSet<byte>();var references=new HashSet<int>();var payloads=new HashSet<string>();
        var baseline=Convert.FromHexString(root.GetProperty("baselineStatePayloadHex").GetString()!);
        foreach(var row in root.GetProperty("frames").EnumerateArray()) {
            var bytes=Convert.FromHexString(row.GetProperty("frameHex").GetString()!);
            var payload=Convert.FromHexString(row.GetProperty("payloadHex").GetString()!);
            payloads.Add(Convert.ToHexString(payload));
            var decoder=new FrameStreamDecoder();var frames=decoder.Push(bytes);
            Assert.Equal(payload,Assert.Single(frames).Payload);
            if(payload[0]==0x45) {
                Assert.True(TunerReading.TryParse(payload,out var parsed));
                if(parsed!.Value.HasSignal)notes.Add(parsed.Value.NoteCode);
            }
            if(payload[0]==0x13) {
                var state=payload.ToArray();state[0]=0x12;var settings=TunerSettings.Parse(state);
                references.Add(settings.ReferenceHertz);
                var expected=TunerCommands.CreateReference(baseline,settings.ReferenceHertz);
                expected[0]=0x12;expected=TunerCommands.CreateOutputMode(expected,settings.OutputMode);
                Assert.Equal(payload,expected);
            }
        }
        Assert.Equal(Enumerable.Range(1,12).Select(n=>(byte)n),notes.Order());
        Assert.Equal(Enumerable.Range(430,16),references.Order());
        foreach(var payload in new[]{"4101","4900","4A01","4102","4201","4100"})Assert.Contains(payload,payloads);
    }
}
