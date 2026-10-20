namespace Iamp.Protocol;

public sealed record PresetImportSnapshot(byte[] State, byte[] Detail);
public sealed record PresetImportProgress(string Message, bool Restoring = false);
public sealed record PresetImportResult(PresetImportSnapshot Snapshot, bool Cancelled, bool Saved);

/// <summary>A live change may have occurred. A started save has an unknown stored outcome on failure.</summary>
public sealed class PresetImportException(string message, bool saveStarted, Exception inner, PresetImportSnapshot? lastVerifiedSnapshot = null)
    : IOException(message, inner)
{
    public bool SaveStarted { get; } = saveStarted;
    public PresetImportSnapshot? LastVerifiedSnapshot { get; } = lastVerifiedSnapshot;
    public bool RestorationFailed { get; init; }
}

/// <summary>Composes captured mobile chain, complete effect-record, and save commands. Never retries writes.</summary>
public static class PresetImportWorkflow
{
    public static Task<PresetImportSnapshot> RunAsync(
        PresetImportSnapshot expected, byte[] importedDetail,
        Func<CancellationToken, Task<PresetImportSnapshot>> read,
        Func<byte[], CancellationToken, Task> write,
        Func<byte[], Func<byte[], bool>, CancellationToken, Task<byte[]>> query,
        Func<string, byte> nextToken, CancellationToken ct)
        => RunCoreAsync(expected, importedDetail, read, write, query, nextToken, ct, CancellationToken.None, null, false, true);

    /// <summary>User cancellation is cooperative: finish verifying the current command, then restore the previous preset.
    /// The transport/deadline token remains independent and never triggers automatic recovery.</summary>
    public static async Task<PresetImportResult> RunCancellableAsync(
        PresetImportSnapshot expected, byte[] importedDetail,
        Func<CancellationToken, Task<PresetImportSnapshot>> read,
        Func<byte[], CancellationToken, Task> write,
        Func<byte[], Func<byte[], bool>, CancellationToken, Task<byte[]>> query,
        Func<string, byte> nextToken, CancellationToken ct, CancellationToken cancelRequest,
        IProgress<PresetImportProgress>? progress = null)
    {
        var original = Copy(expected);
        try
        {
            var result = await RunCoreAsync(original, importedDetail, read, write, query, nextToken,
                ct, cancelRequest, progress, false, true);
            return new(result, false, true);
        }
        catch (UserCancellation cancellation)
        {
            if (!cancellation.Mutated)
                return new(Copy(original), true, false);
            try
            {
                progress?.Report(new("Restoring the previous preset…", true));
                // The core begins by requiring the complete last verified snapshot. A changed
                // amplifier or uncertain transport failure prevents recovery from overwriting it.
                var restored = await RunCoreAsync(cancellation.Current, original.Detail, read, write, query,
                    nextToken, ct, CancellationToken.None, progress, true, cancellation.SaveStarted);
                if (!Same(original, restored))
                    throw new IOException("The previous preset could not be restored exactly.");
                progress?.Report(new("Previous preset restored.", true));
                return new(restored, true, cancellation.SaveStarted);
            }
            catch (Exception error)
            {
                var latest = error is PresetImportException importError ? importError.LastVerifiedSnapshot : cancellation.Current;
                throw new PresetImportException("Cancellation could not restore the previous preset completely. Refresh the amplifier to inspect its actual settings; do not retry automatically.",
                    cancellation.SaveStarted || error is PresetImportException { SaveStarted: true }, error, latest) { RestorationFailed = true };
            }
        }
    }

    private sealed class UserCancellation(PresetImportSnapshot current, bool mutated, bool saveStarted) : Exception
    {
        public PresetImportSnapshot Current { get; } = current;
        public bool Mutated { get; } = mutated;
        public bool SaveStarted { get; } = saveStarted;
    }

    private static async Task<PresetImportSnapshot> RunCoreAsync(
        PresetImportSnapshot expected, byte[] importedDetail,
        Func<CancellationToken, Task<PresetImportSnapshot>> read,
        Func<byte[], CancellationToken, Task> write,
        Func<byte[], Func<byte[], bool>, CancellationToken, Task<byte[]>> query,
        Func<string, byte> nextToken, CancellationToken ct, CancellationToken cancelRequest,
        IProgress<PresetImportProgress>? progress, bool restoring, bool savePreset)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(importedDetail);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(nextToken);
        ct.ThrowIfCancellationRequested();
        var current = Copy(expected);
        var imported = importedDetail.ToArray();
        var source = PresetTransferRecord.Validate("V1.1.9", imported);
        var destination = PresetTransferRecord.Validate("V1.1.9", current.Detail);
        _ = CurrentPresetReadGuard.Validate(current.State, current.Detail, current.State);
        if (!source.UnknownTrailer.AsSpan().SequenceEqual(destination.UnknownTrailer))
            throw new NotSupportedException("The file contains different unsupported preset fields. Nothing was changed.");
        var desired = EffectChainCommands.Active(source.Blocks);
        var targetLive = PresetSaveCommand.ExpectedDetail(imported, destination.WireIndex, destination.Name);
        var targetSaved = PresetSaveCommand.ExpectedDetail(imported, destination.WireIndex, source.Name);
        var mutated = false;
        var saveStarted = false;

        void Checkpoint()
        {
            ct.ThrowIfCancellationRequested();
            if (cancelRequest.IsCancellationRequested)
                throw new UserCancellation(Copy(current), mutated, saveStarted);
        }
        void Report(string message) => progress?.Report(new(message, restoring));

        async Task RequireFresh()
        {
            Checkpoint();
            var fresh = await read(ct);
            _ = CurrentPresetReadGuard.Validate(fresh.State, fresh.Detail, fresh.State);
            if (!Same(current, fresh))
                throw new IOException("The amplifier changed after confirmation. Refresh before importing again.");
            Checkpoint();
        }

        async Task ChangeChain(byte[] request, Func<byte[], byte[], bool> matches)
        {
            await RequireFresh();
            var before = current;
            mutated = true;
            await write(request, ct);
            var after = await read(ct);
            _ = CurrentPresetReadGuard.Validate(after.State, after.Detail, after.State);
            if (!before.State.AsSpan().SequenceEqual(after.State) || !matches(before.Detail, after.Detail))
                throw new IOException("Imported effect-chain readback differs. Nothing was saved.");
            current = Copy(after);
            Checkpoint();
        }

        try
        {
            Report("Checking the active preset…");
            await RequireFresh();
            while (EffectChainCommands.Active(PresetDataParser.Parse(current.Detail).Blocks).Count > desired.Count)
            {
                var blocks = PresetDataParser.Parse(current.Detail).Blocks;
                var last = checked((byte)EffectChainCommands.Active(blocks).Count);
                Report($"Removing effect {last}…");
                await ChangeChain(EffectChainCommands.Remove(blocks, last, nextToken("remove")),
                    (before, after) => EffectChainCommands.IsExpectedRemoval(before, after, last));
            }
            for (var i = 0; i < desired.Count; i++)
            {
                var blocks = PresetDataParser.Parse(current.Detail).Blocks;
                var active = EffectChainCommands.Active(blocks);
                var wanted = desired[i];
                var model = PresetEffectSearch.Catalog.Single(e => e.TypeCode == wanted.TypeCode && e.Model == wanted.Values[1]);
                if (i >= active.Count)
                {
                    Report($"Adding effect {i + 1} of {desired.Count}…");
                    await ChangeChain(EffectChainCommands.Add(blocks, model, nextToken("add")),
                        (before, after) => EffectChainCommands.IsExpectedAddition(before, after, model));
                }
                else if (active[i].TypeCode != wanted.TypeCode || active[i].Values[1] != wanted.Values[1])
                {
                    Report($"Changing effect {i + 1} of {desired.Count}…");
                    await ChangeChain(EffectChainCommands.Replace(blocks, wanted.Position, model, nextToken("replace")),
                        (before, after) => EffectChainCommands.IsExpectedReplacement(before, after, wanted.Position, model));
                }
            }
            foreach (var wanted in desired)
            {
                var block = PresetDataParser.Parse(current.Detail).Blocks[wanted.Position - 1];
                if (block.Values.SequenceEqual(wanted.Values)) continue;
                Report($"Restoring settings for effect {wanted.Position} of {desired.Count}…");
                await RequireFresh();
                var expectedDetail = current.Detail.ToArray();
                for (var index = 0; index < wanted.Values.Count; index++)
                    expectedDetail = PresetDataParser.WithParameterValue(expectedDetail, wanted.Position, index, wanted.Values[index]);
                var request = EffectBypassCommand.Create(wanted, wanted.Values[0] == 1);
                mutated = true;
                await write(request, ct);
                var after = await read(ct);
                _ = CurrentPresetReadGuard.Validate(after.State, after.Detail, after.State);
                if (!current.State.AsSpan().SequenceEqual(after.State) || !expectedDetail.AsSpan().SequenceEqual(after.Detail))
                    throw new IOException($"Verification failed while applying effect {wanted.Position}. {DescribeDifference(current.State,expectedDetail,after)} Nothing was saved.");
                current = Copy(after);
                Checkpoint();
            }
            if (!current.Detail.AsSpan().SequenceEqual(targetLive))
                throw new IOException($"The complete live preset differs from the file. {DescribeDifference(current.State,targetLive,current)} Nothing was saved.");
            await RequireFresh();
            if (!savePreset)
            {
                Report("Previous live preset verified.");
                return Copy(current);
            }
            var save = PresetSaveCommand.Create("V1.1.9", current.State, current.Detail, destination.WireIndex, source.Name);
            Report(restoring ? "Saving the previous preset…" : "Saving the imported preset…");
            Checkpoint();
            // Set before sending: a timeout can mean that the device committed the save.
            mutated = true;
            saveStarted = true;
            var response = await query(save, p => p.Length > 0 && p[0] == 0x30, ct);
            if (!response.AsSpan().SequenceEqual(new byte[] { 0x30, 1 }))
                throw new IOException("Import save acknowledgement differs from the verified response.");
            Report("Recalling the saved preset…");
            var recall = current.State.ToArray(); recall[0] = 0x13;
            var recalled = await query(recall, p => p.Length > 0 && p[0] == 0x14, ct);
            if (!recalled.AsSpan().SequenceEqual(new byte[] { 0x14, 1 }))
                throw new IOException("Import recall acknowledgement differs from the verified response.");
            Report("Verifying every saved setting…");
            var final = await read(ct);
            _ = CurrentPresetReadGuard.Validate(final.State, final.Detail, final.State);
            if (!current.State.AsSpan().SequenceEqual(final.State) || !targetSaved.AsSpan().SequenceEqual(final.Detail))
                throw new IOException($"The saved preset does not exactly match the imported file. {DescribeDifference(current.State,targetSaved,final)}");
            current = Copy(final);
            Checkpoint();
            Report(restoring ? "Previous preset restored and verified." : "Import complete; every saved setting verified.");
            // A close/cancel arriving from the completion progress callback is still
            // a cancellation of this operation, so handle it before returning success.
            Checkpoint();
            return Copy(current);
        }
        catch (Exception error) when (mutated && error is not PresetImportException && error is not UserCancellation)
        {
            throw new PresetImportException(saveStarted
                ? "Import was not verified. The save may have completed; refresh the amplifier before any further action. Do not retry automatically."
                : "Import stopped after changing the live preset. Nothing was saved; refresh to inspect the current settings.", saveStarted, error, Copy(current));
        }
    }

    private static PresetImportSnapshot Copy(PresetImportSnapshot snapshot)
        => new(snapshot.State.ToArray(), snapshot.Detail.ToArray());
    private static string DescribeDifference(byte[] state,byte[] detail,PresetImportSnapshot actual)
    {
        var changes=new List<string>();
        if(!state.AsSpan().SequenceEqual(actual.State))changes.Add("Amplifier-wide state changed");
        var expected=PresetDataParser.Parse(detail);var returned=PresetDataParser.Parse(actual.Detail);
        if(expected.WireIndex!=returned.WireIndex)changes.Add("The active preset slot changed");
        if(expected.Name!=returned.Name)changes.Add("The preset name changed");
        for(var i=0;i<20&&changes.Count<4;i++){
            var left=expected.Blocks[i];var right=returned.Blocks[i];
            if(left.TypeCode!=right.TypeCode||left.Values.Count!=right.Values.Count){changes.Add($"Effect {i+1} has a different model or layout");continue;}
            for(var j=0;j<left.Values.Count&&changes.Count<4;j++)if(left.Values[j]!=right.Values[j]){
                var field=j==0?"enabled state":j==1?"model":"stored value "+(j+1);
                changes.Add($"Effect {i+1} {field}: expected {left.Values[j]}, received {right.Values[j]}");
            }
        }
        if(!expected.UnknownTrailer.AsSpan().SequenceEqual(returned.UnknownTrailer))changes.Add("Preset companion data changed");
        return string.Join("; ",changes)+".";
    }
    private static bool Same(PresetImportSnapshot left, PresetImportSnapshot right)
        => left.State.AsSpan().SequenceEqual(right.State) && left.Detail.AsSpan().SequenceEqual(right.Detail);
}
