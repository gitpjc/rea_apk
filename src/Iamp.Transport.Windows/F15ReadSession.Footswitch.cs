using Iamp.Protocol;

namespace Iamp.Transport.Windows;

public sealed partial class F15ReadSession
{
    private readonly FootswitchStatusMonitor footswitchStatus=new();

    /// <summary>Current-session observation only; Unknown is not evidence of disconnection or missing pairing.</summary>
    public FootswitchConnectionStatus FootswitchStatus=>footswitchStatus.Status;

    public event Action<FootswitchConnectionStatus>? FootswitchStatusChanged
    {
        add=>footswitchStatus.StatusChanged+=value;
        remove=>footswitchStatus.StatusChanged-=value;
    }

    private void ObserveFootswitchStatus(ReadOnlySpan<byte> payload)=>footswitchStatus.Observe(payload);

    /// <summary>
    /// Perform the mobile-observed startup read once. Its separate 04 reply does not encode F4 status.
    /// Return the observed session status; 97 notifications received later still update the event/snapshot.
    /// </summary>
    public Task<FootswitchConnectionStatus> ReadFootswitchStatusAsync(CancellationToken ct)
        =>RunWorkflowAsync(async token=>{
            if(confirmedIdentity?.Firmware!="V1.1.9"||confirmedIdentity.PresetSlots!=80)
                throw new NotSupportedException("Footswitch status reading is verified only on F15i V1.1.9 with 80 preset slots.");
            var response=await QueryAsync(FootswitchCommands.CreateStatusReadCommand(),
                payload=>payload.Length>0&&payload[0]==0x04,token);
            FootswitchCommands.RequireStatusReadReply(response);
            return FootswitchStatus;
        },ct);

    /// <summary>
    /// Begin pairing or explicitly unpair, once, as observed in mobile iAMP.
    /// Returns after acknowledgement; connection notifications can arrive before or after it.
    /// Never infers a connection from acknowledgement or sends cleanup/retry commands.
    /// </summary>
    public async Task SetFootswitchPairingAsync(bool pairing,CancellationToken ct)
    {
        await RunWorkflowAsync(async token=>{
            if(confirmedIdentity?.Firmware!="V1.1.9"||confirmedIdentity.PresetSlots!=80)
                throw new NotSupportedException("Footswitch pairing is verified only on F15i V1.1.9 with 80 preset slots.");
            var response=await QueryAsync(FootswitchCommands.CreatePairingCommand(pairing),
                payload=>payload.Length>0&&payload[0]==0x96,token);
            FootswitchCommands.RequirePairingAcknowledgement(response);
            return true;
        },ct);
    }
}
