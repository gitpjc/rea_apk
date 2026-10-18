using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class TunerDiagnosticsTests
{
    [Theory]
    [InlineData("4101",DiagnosticOperation.ToolsEntry)]
    [InlineData("4102",DiagnosticOperation.TunerEntry)]
    [InlineData("4100",DiagnosticOperation.TunerExit)]
    [InlineData("4A01",DiagnosticOperation.ToolsReply)]
    [InlineData("91",DiagnosticOperation.BatteryRead)]
    [InlineData("03",DiagnosticOperation.ProtocolSetupRead)]
    [InlineData("4103",DiagnosticOperation.OtherCommand)]
    [InlineData("410201",DiagnosticOperation.OtherCommand)]
    public void CommandsExposeOnlyAllowlistedOperationNames(string hex,DiagnosticOperation expected)
        =>Assert.Equal(expected,DiagnosticJournal.CommandKind(Convert.FromHexString(hex)));

    [Theory]
    [InlineData("4900",DiagnosticOperation.ToolsRequest)]
    [InlineData("4300",DiagnosticOperation.TunerReportedInactive)]
    [InlineData("4302",DiagnosticOperation.TunerReportedActive)]
    [InlineData("450B0C",DiagnosticOperation.TunerSignal)]
    [InlineData("450000",DiagnosticOperation.TunerNoSignal)]
    [InlineData("4301",null)]
    [InlineData("4901",null)]
    [InlineData("450001",null)]
    [InlineData("45FF00",null)]
    [InlineData("",null)]
    public void NotificationsClassifyCapturedSignalAndModeStatesOnly(string hex,DiagnosticOperation? expected)
        =>Assert.Equal(expected,DiagnosticJournal.TunerNotificationKind(Convert.FromHexString(hex)));
}
