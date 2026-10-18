using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class TunerReadingTests
{
    [Theory]
    [InlineData("45060C", "E", 12, 11, "Low!")]
    [InlineData("45010B", "B", 11, 11, "Low!")]
    [InlineData("45020A", "C", 10, 12, "Low!")]
    [InlineData("450B08", "A", 8, 13, "Low!")]
    [InlineData("450B0F", "A", 15, 9, "Perfect!")]
    [InlineData("450B10", "A", 16, 8, "Perfect!")]
    [InlineData("450B12", "A", 18, 6, "High!")]
    [InlineData("450B1D", "A", 29, 1, "High!")]
    [InlineData("450B0E", "A", 14, 10, "Low!")]
    [InlineData("450C0B", "A#", 11, 11, "Low!")]
    public void CapturedPayloadsDecodeToObservedNotesAndVerticalPositions(string hex, string note, int position, int displayedPosition, string direction)
    {
        Assert.True(TunerReading.TryParse(Convert.FromHexString(hex), out var parsed));
        var reading = parsed!.Value;
        Assert.True(reading.HasSignal);
        Assert.Equal(note, reading.NoteName);
        Assert.Equal(position, reading.MeterPosition);
        Assert.Equal(displayedPosition, reading.DisplayPosition);
        Assert.Equal(direction, reading.Direction);
    }

    [Fact]
    public void CapturedChromaticNoteOrderMatchesMobileNames()
    {
        string[] notes = ["B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#"];
        for (byte code = 1; code <= 12; code++)
        {
            Assert.True(TunerReading.TryParse([0x45, code, 16], out var reading));
            Assert.Equal(code, reading!.Value.NoteCode);
            Assert.Equal(notes[code - 1], reading.Value.NoteName);
            Assert.Equal("Perfect!", reading.Value.Direction);
        }
    }

    [Theory]
    [InlineData(0, "Low!")]
    [InlineData(7, "Low!")]
    [InlineData(8, "Low!")]
    [InlineData(9, "Low!")]
    [InlineData(14, "Low!")]
    [InlineData(15, "Perfect!")]
    [InlineData(16, "Perfect!")]
    [InlineData(17, "Perfect!")]
    [InlineData(18, "High!")]
    [InlineData(22, "High!")]
    [InlineData(27, "High!")]
    [InlineData(32, "High!")]
    [InlineData(255, "High!")]
    public void RawMeterPositionIsPreservedAndOnlyRenderingIsClamped(byte position, string direction)
    {
        Assert.True(TunerReading.TryParse([0x45, 6, position], out var reading));
        Assert.Equal(position, reading!.Value.MeterPosition);
        Assert.InRange(reading.Value.DisplayPosition,0,16);
        Assert.Equal(direction, reading.Value.Direction);
    }

    [Fact]
    public void MobilePerfectBandAndMeterExtremesUseRawScaleInsteadOfVisibleSegmentCount()
    {
        Assert.Equal(new[]{8,9},new TunerReading(11,15).DisplayPositions);
        Assert.Equal(Enumerable.Range(0,17),new TunerReading(11,16).DisplayPositions);
        Assert.Equal(new[]{7,8},new TunerReading(11,17).DisplayPositions);
        Assert.Equal(new[]{12},new TunerReading(6,9).DisplayPositions);
        Assert.Equal(new[]{6},new TunerReading(11,18).DisplayPositions);
        Assert.Equal(new[]{10},new TunerReading(11,14).DisplayPositions);
        Assert.Equal(new[]{1},new TunerReading(11,29).DisplayPositions);
        Assert.Equal(new[]{16},new TunerReading(11,0).DisplayPositions);
        Assert.Equal(new[]{0},new TunerReading(11,32).DisplayPositions);
        Assert.Empty(new TunerReading(0,0).DisplayPositions);
        Assert.False(new TunerReading(0,0).IsInTune);
    }

    [Fact]
    public void CapturedNoSignalPayloadDoesNotBecomeAHighReading()
    {
        Assert.True(TunerReading.TryParse([0x45, 0, 0], out var reading));
        Assert.False(reading!.Value.HasSignal);
        Assert.Equal("", reading.Value.NoteName);
        Assert.Equal("No signal", reading.Value.Direction);
    }

    [Theory]
    [InlineData("")]
    [InlineData("45")]
    [InlineData("4506")]
    [InlineData("45060800")]
    [InlineData("510608")]
    [InlineData("420608")]
    [InlineData("450D08")]
    [InlineData("45FF08")]
    [InlineData("450001")]
    [InlineData("450008")]
    public void UnrelatedOrMalformedPayloadsAreNotTunerReadings(string hex)
    {
        Assert.False(TunerReading.TryParse(Convert.FromHexString(hex), out var reading));
        Assert.Null(reading);
    }

    [Fact]
    public void AllSignalMeterBytesAreAcceptedWithoutInventingADeviceRange()
    {
        for (var position = 0; position <= byte.MaxValue; position++)
        {
            Assert.True(TunerReading.TryParse([0x45, 6, (byte)position], out var reading));
            Assert.Equal(position, reading!.Value.MeterPosition);
        }
    }
}
