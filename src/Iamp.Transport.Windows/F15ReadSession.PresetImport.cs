using Iamp.Protocol;

namespace Iamp.Transport.Windows;

public sealed record PresetImportOutcome(CurrentPresetSnapshot Preset,bool Cancelled,bool Saved);

public sealed partial class F15ReadSession
{
    public Task<PresetImportOutcome> ImportPresetAsync(CurrentPresetSnapshot expected,PresetSaveDestination destination,byte[] importedDetail,CancellationToken ct,CancellationToken cancelRequest=default,IProgress<PresetImportProgress>? progress=null)
    {
        var retained=importedDetail.ToArray();
        return RunWorkflowAsync(async token=>{
            var identity=await ReadIdentityCoreAsync(token);RequireSaveIdentity(identity);
            _=PresetTransferRecord.Validate(identity.Firmware,retained);
            if(destination.WireIndex!=expected.WireIndex)throw new PresetSavePreconditionException("Import must target the active preset slot.");
            var before=await ReadCurrentPresetCoreAsync(token);RequireSaveSnapshot(before,expected);
            var fresh=await ReadSaveDestinationCoreAsync(destination.WireIndex,token);
            if(fresh!=destination)throw new PresetSavePreconditionException("The destination changed after import confirmation. Nothing was written.");
            async Task<PresetImportSnapshot> Read(CancellationToken cancellation){
                var current=await ReadCurrentPresetCoreAsync(cancellation);
                return new(Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex));
            }
            byte NextToken(string action)=>action switch{
                "add"=>ChainToken(ref addChainSequence,0),"remove"=>ChainToken(ref removeChainSequence,50),
                "replace"=>ChainToken(ref replaceChainSequence,100),_=>throw new ArgumentOutOfRangeException(nameof(action))};
            progress?.Report(new("Checking the active preset…"));
            var result=await PresetImportWorkflow.RunCancellableAsync(new(Convert.FromHexString(before.RawStatePayloadHex),Convert.FromHexString(before.RawDetailPayloadHex)),retained,
                Read,WriteObservedUpdateAsync,QueryAsync,NextToken,token,cancelRequest,progress);
            var decoded=PresetDataParser.Parse(result.Snapshot.Detail);
            var preset=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,Convert.ToHexString(result.Snapshot.State),Convert.ToHexString(result.Snapshot.Detail),decoded.Blocks);
            return new PresetImportOutcome(preset,result.Cancelled,result.Saved);
        },ct);
    }
}
