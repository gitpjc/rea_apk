using System.Buffers.Binary;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class PresetImportWorkflowTests
{
    private static PresetEffectBlock Comp(byte position, bool enabled = true, ushort sensitivity = 20)
        => new(position, 4, [(ushort)(enabled ? 1 : 0), 0, sensitivity, 70, 0, 0]);
    private static PresetEffectBlock Gate(byte position)
        => new(position, 4, [1, 2, 25, 50, 0, 0]);
    private static PresetEffectBlock Amp(byte position)
        => new(position, 2, [1, 31, 0, 47, 44, 35, 51, 75, 1]);

    private static byte[] Detail(byte slot, string name, params PresetEffectBlock[] active)
    {
        var result = new List<byte> { 0x22, slot };
        result.AddRange(PresetSaveCommand.EncodeName(name));
        for (var i = 0; i < 20; i++)
        {
            var block = i < active.Length ? active[i] : null;
            result.Add((byte)(i + 1)); result.Add(block?.TypeCode ?? 0);
            result.Add(checked((byte)((block?.Values.Count ?? 0) * 2)));
            if (block is null) continue;
            foreach (var value in block.Values) { result.Add((byte)value); result.Add((byte)(value >> 8)); }
        }
        result.AddRange([0, 0]);
        return result.ToArray();
    }

    private sealed class Device
    {
        public byte[] State = new byte[92];
        public byte[] Data;
        public readonly List<byte[]> Writes = [];
        public readonly List<byte[]> Queries = [];
        public int ReadCount;
        public int CorruptAtRead;
        public int CorruptDetailAtRead;
        public int FailAtWrite;
        public CancellationTokenSource? CancelAfterWrite;
        public bool SaveFails;
        public bool RecallFails;
        public Action<byte[]>? AfterWrite;
        public Action<byte[]>? AfterQuery;
        public Action<byte[]>? QueryStarted;
        public Device(params PresetEffectBlock[] effects)
        { State[0] = 0x12; State[1] = 79; State[45] = 53; Data = Detail(79, "New Patch", effects); }
        public PresetImportSnapshot Snapshot() => new(State.ToArray(), Data.ToArray());
        public Task<PresetImportSnapshot> Read(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ReadCount++;
            var result = Snapshot();
            if (ReadCount == CorruptAtRead) result.State[45] ^= 1;
            if (ReadCount == CorruptDetailAtRead)
                result = result with { Detail = PresetDataParser.WithParameterValue(result.Detail, 1, 3, 69) };
            return Task.FromResult(result);
        }
        public Task Write(byte[] request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Writes.Add(request.ToArray());
            if (Writes.Count == FailAtWrite) throw new IOException("Disconnected");
            var preset = PresetDataParser.Parse(Data);
            var active = EffectChainCommands.Active(preset.Blocks).ToList();
            if (request[0] == 0x2D)
            {
                if (request[2] == 3) active.RemoveAt(request[3] - 1);
                else
                {
                    // Synthetic amplifier defaults; expected checks require the selected identity,
                    // then compare every unchanged byte. These are not claims about device defaults.
                    var position = request[3];
                    var block = request[4] == 2 ? Amp(position)
                        : request[5] == 2 ? Gate(position) : Comp(position);
                    if (request[2] == 1) active.Add(block);
                    else active[position - 1] = block;
                }
                Data = Detail(preset.WireIndex, preset.Name, active.ToArray());
            }
            else
            {
                Assert.Equal(0x25, request[0]);
                for (var i = 0; i < request[3] / 2; i++)
                    Data = PresetDataParser.WithParameterValue(Data, request[1], i,
                        BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(4 + i * 2, 2)));
            }
            CancelAfterWrite?.Cancel();
            AfterWrite?.Invoke(request);
            return Task.CompletedTask;
        }
        public Task<byte[]> Query(byte[] request, Func<byte[], bool> match, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Queries.Add(request.ToArray()); QueryStarted?.Invoke(request);
            byte[] result;
            if (request[0] == 0x2F)
            {
                if (SaveFails) throw new IOException("Save timed out");
                var name = System.Text.Encoding.ASCII.GetString(request, 2, 20).TrimEnd('\0');
                Data = PresetSaveCommand.ExpectedDetail(Data, request[1], name);
                result = [0x30, 1];
            }
            else
            {
                Assert.Equal(0x13, request[0]);
                Assert.Equal(State.AsSpan(1).ToArray(), request.AsSpan(1).ToArray());
                result = [0x14, (byte)(RecallFails ? 0 : 1)];
            }
            Assert.True(match(result)); AfterQuery?.Invoke(request); return Task.FromResult(result);
        }
        public Task<PresetImportSnapshot> Import(byte[] file, CancellationToken ct = default)
            => PresetImportWorkflow.RunAsync(Snapshot(), file, Read, Write, Query,
                operation => operation == "add" ? (byte)0 : operation == "remove" ? (byte)50 : (byte)100, ct);
        public Task<PresetImportResult> ImportCancellable(byte[] file, CancellationToken userCancel,
            IProgress<PresetImportProgress>? progress = null, CancellationToken ct = default)
            => PresetImportWorkflow.RunCancellableAsync(Snapshot(), file, Read, Write, Query,
                operation => operation == "add" ? (byte)0 : operation == "remove" ? (byte)50 : (byte)100,
                ct, userCancel, progress);
    }

    [Fact]
    public async Task ImportsIntoCurrentSlotWithDuplicatesDisabledEffectsAndAllWords()
    {
        var device = new Device(Gate(1));
        var state = device.State.ToArray();
        var file = Detail(1, "British J800 Clean", Comp(1, false, 19), Comp(2, true, 71), Amp(3));
        var untouchedFile = file.ToArray();
        var final = await device.Import(file);
        Assert.Equal(state, final.State);
        Assert.Equal(PresetSaveCommand.ExpectedDetail(file, 79, "British J800 Clean"), final.Detail);
        Assert.Equal(untouchedFile, file);
        Assert.Equal(new byte[] { 0x2F, 0x13 }, device.Queries.Select(q => q[0]).ToArray());
        Assert.All(device.Queries, q => Assert.Equal(79, q[1]));
        Assert.Contains(device.Writes, p => p[0] == 0x25 && p[1] == 1 && p[4] == 0);
    }

    [Fact]
    public async Task RemovesExcessEffectsAndSavesEmptyChain()
    {
        var device = new Device(Comp(1), Gate(2));
        var result = await device.Import(Detail(1, "Empty"));
        Assert.Empty(EffectChainCommands.Active(PresetDataParser.Parse(result.Detail).Blocks));
        Assert.Equal(new byte[] { 2, 1 }, device.Writes.Select(w => w[3]).ToArray());
    }

    [Fact]
    public async Task RejectsUnsupportedTrailerWithoutWriting()
    {
        var device = new Device(Comp(1)); var file = Detail(1, "Imported", Comp(1)); file[^1] = 1;
        await Assert.ThrowsAsync<NotSupportedException>(() => device.Import(file));
        Assert.Empty(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task RejectsInvalidModelWithoutWriting()
    {
        var device = new Device(Comp(1));
        var file = Detail(1, "Imported", new PresetEffectBlock(1, 250, [1, 0]));
        await Assert.ThrowsAsync<NotSupportedException>(() => device.Import(file));
        Assert.Empty(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task StaleBeforeFirstWriteDoesNotChangeAnything()
    {
        var device = new Device(Comp(1)) { CorruptAtRead = 1 };
        await Assert.ThrowsAsync<IOException>(() => device.Import(Detail(1, "Imported", Comp(1, false))));
        Assert.Empty(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task RechecksImmediatelyBeforeEveryMutation()
    {
        var device = new Device(Comp(1)) { CorruptAtRead = 2 };
        await Assert.ThrowsAsync<IOException>(() => device.Import(Detail(1, "Imported", Comp(1, false))));
        Assert.Empty(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task CancellationBeforeImportDoesNotWrite()
    {
        var device = new Device(Comp(1)); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.Import(Detail(1, "Imported", Gate(1)), cts.Token));
        Assert.Empty(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task FailureAfterFirstLiveMutationDoesNotSaveOrRetry()
    {
        var device = new Device(Comp(1)) { FailAtWrite = 2 };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Gate(1), Comp(2))));
        Assert.False(error.SaveStarted); Assert.Equal(2, device.Writes.Count); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task ChangedGlobalStateReadbackStopsWithoutSave()
    {
        var device = new Device(Comp(1)) { CorruptAtRead = 3 };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Gate(1))));
        Assert.False(error.SaveStarted); Assert.Single(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task SaveTimeoutIsReportedAsUnknownAndNeverRetried()
    {
        var device = new Device(Comp(1)) { SaveFails = true };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Comp(1))));
        Assert.True(error.SaveStarted); Assert.Single(device.Queries); Assert.Empty(device.Writes);
    }

    [Fact]
    public async Task RecallFailureIsNotReportedAsSuccessfulImport()
    {
        var device = new Device(Comp(1)) { RecallFails = true };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Comp(1))));
        Assert.True(error.SaveStarted); Assert.Equal(2, device.Queries.Count);
    }

    [Fact]
    public async Task PreservesMatchingOpaqueTrailerAndCompanionWords()
    {
        var device = new Device(Comp(1)); device.Data[^2] = 17;
        var file = Detail(1, "Imported", Comp(1, false, 19)); file[^2] = 17;
        var final = await device.Import(file);
        Assert.Equal(PresetSaveCommand.ExpectedDetail(file, 79, "Imported"), final.Detail);
        Assert.Equal(17, final.Detail[^2]);
    }

    [Fact]
    public async Task CancellationAfterLiveWriteReportsPartialWithoutSaving()
    {
        using var cts = new CancellationTokenSource();
        var device = new Device(Comp(1)) { CancelAfterWrite = cts };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Gate(1)), cts.Token));
        Assert.False(error.SaveStarted); Assert.IsAssignableFrom<OperationCanceledException>(error.InnerException);
        Assert.Single(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task UnexpectedCompleteRecordReadbackNeverSaves()
    {
        var device = new Device(Comp(1)) { CorruptDetailAtRead = 3 };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Comp(1, false, 19))));
        Assert.False(error.SaveStarted); Assert.Single(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task FinalReadbackMismatchNeverReportsSuccessOrReplaysSave()
    {
        var device = new Device(Comp(1)) { CorruptAtRead = 3 };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.Import(Detail(1, "Imported", Comp(1))));
        Assert.True(error.SaveStarted); Assert.Equal(2, device.Queries.Count);
        Assert.Single(device.Queries, q => q[0] == 0x2F);
    }

    private sealed class ImmediateProgress(Action<PresetImportProgress> report) : IProgress<PresetImportProgress>
    {
        public void Report(PresetImportProgress value) => report(value);
    }

    [Fact]
    public async Task UserCancellationBeforeFirstMutationReturnsOriginalWithoutCommands()
    {
        var device = new Device(Comp(1)); var original = device.Snapshot();
        using var userCancel = new CancellationTokenSource(); userCancel.Cancel();
        var result = await device.ImportCancellable(Detail(1, "Imported", Gate(1)), userCancel.Token);
        Assert.True(result.Cancelled); Assert.False(result.Saved);
        Assert.Equal(original.State, result.Snapshot.State); Assert.Equal(original.Detail, result.Snapshot.Detail);
        Assert.Empty(device.Writes); Assert.Empty(device.Queries);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("replace")]
    [InlineData("settings")]
    public async Task UserCancellationAfterEachLiveCommandRestoresWithoutSaving(string operation)
    {
        using var userCancel = new CancellationTokenSource();
        var device = operation == "remove" ? new Device(Comp(1), Gate(2)) : new Device(Comp(1, true, 19));
        var original = device.Snapshot();
        var file = operation switch
        {
            "add" => Detail(1, "Imported", Comp(1), Gate(2)),
            "remove" => Detail(1, "Imported", Comp(1)),
            "replace" => Detail(1, "Imported", Gate(1)),
            _ => Detail(1, "Imported", Comp(1, false, 71))
        };
        device.AfterWrite = _ => userCancel.Cancel();
        var stages = new List<PresetImportProgress>();
        var result = await device.ImportCancellable(file, userCancel.Token, new ImmediateProgress(stages.Add));
        Assert.True(result.Cancelled); Assert.False(result.Saved);
        Assert.Equal(original.State, result.Snapshot.State); Assert.Equal(original.Detail, result.Snapshot.Detail);
        Assert.Equal(original.Detail, device.Data); Assert.Empty(device.Queries);
        Assert.Contains(stages, p => p.Restoring);
    }

    [Theory]
    [InlineData(0x2F)]
    [InlineData(0x13)]
    public async Task UserCancellationDuringSaveOrRecallCompletesVerificationThenRestoresSavedOriginal(int opcode)
    {
        using var userCancel = new CancellationTokenSource();
        var device = new Device(Comp(1, true, 19)); var original = device.Snapshot();
        device.AfterQuery = request => { if (request[0] == opcode) userCancel.Cancel(); };
        var result = await device.ImportCancellable(Detail(1, "Imported", Comp(1, false, 71)), userCancel.Token);
        Assert.True(result.Cancelled); Assert.True(result.Saved);
        Assert.Equal(original.State, result.Snapshot.State); Assert.Equal(original.Detail, result.Snapshot.Detail);
        Assert.Equal(original.Detail, device.Data);
        Assert.Equal(new byte[] { 0x2F, 0x13, 0x2F, 0x13 }, device.Queries.Select(q => q[0]).ToArray());
        Assert.Equal("New Patch", PresetDataParser.Parse(device.Data).Name);
    }

    [Fact]
    public async Task CloseRequestAtCompletionUsesSameVerifiedRestorationPath()
    {
        using var closeRequest = new CancellationTokenSource();
        var device = new Device(Comp(1)); var original = device.Snapshot();
        var progress = new ImmediateProgress(p => { if (!p.Restoring && p.Message.StartsWith("Import complete")) closeRequest.Cancel(); });
        var result = await device.ImportCancellable(Detail(1, "Imported", Gate(1)), closeRequest.Token, progress);
        Assert.True(result.Cancelled); Assert.True(result.Saved);
        Assert.Equal(original.Detail, result.Snapshot.Detail);
    }

    [Fact]
    public async Task CancellationRestorationFailureIsExplicitAndNotReportedRestored()
    {
        using var userCancel = new CancellationTokenSource();
        var device = new Device(Comp(1)) { FailAtWrite = 2 };
        device.AfterWrite = _ => userCancel.Cancel();
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.ImportCancellable(Detail(1, "Imported", Comp(1, false, 71)), userCancel.Token));
        Assert.False(error.SaveStarted); Assert.Contains("could not restore", error.Message);
        Assert.True(error.RestorationFailed);
        Assert.NotNull(error.LastVerifiedSnapshot); Assert.Empty(device.Queries); Assert.Equal(2, device.Writes.Count);
    }

    [Fact]
    public async Task ChangedAmplifierAtRestorationBoundaryIsNeverOverwritten()
    {
        using var userCancel = new CancellationTokenSource();
        var device = new Device(Comp(1)); device.AfterWrite = _ => userCancel.Cancel();
        var progress = new ImmediateProgress(p => { if (p.Restoring) device.State[45] = 99; });
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.ImportCancellable(Detail(1, "Imported", Comp(1, false, 71)), userCancel.Token, progress));
        Assert.False(error.SaveStarted); Assert.Single(device.Writes); Assert.Empty(device.Queries);
        Assert.Equal(99, device.State[45]);
        Assert.True(error.RestorationFailed);
    }

    [Fact]
    public async Task UnexpectedReadbackWithUserCancelRequestedNeverTriggersRestoration()
    {
        using var userCancel = new CancellationTokenSource();
        var device = new Device(Comp(1)) { CorruptAtRead = 3 };
        device.AfterWrite = _ => userCancel.Cancel();
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.ImportCancellable(Detail(1, "Imported", Gate(1)), userCancel.Token));
        Assert.False(error.SaveStarted); Assert.Single(device.Writes); Assert.Empty(device.Queries);
    }

    [Fact]
    public async Task SaveFailureWithPendingUserCancelNeverReplaysOrRestoresUncertainStorage()
    {
        using var userCancel = new CancellationTokenSource();
        var device = new Device(Comp(1)) { SaveFails = true };
        device.QueryStarted = request => { if (request[0] == 0x2F) userCancel.Cancel(); };
        var error = await Assert.ThrowsAsync<PresetImportException>(() => device.ImportCancellable(Detail(1, "Imported", Comp(1)), userCancel.Token));
        Assert.True(error.SaveStarted); Assert.Single(device.Queries); Assert.Empty(device.Writes);
        Assert.False(error.RestorationFailed);
    }

    [Fact]
    public async Task CancelImmediatelyBeforeSaveRestoresAppliedLiveChangesWithoutSaving()
    {
        using var userCancel = new CancellationTokenSource();
        var device = new Device(Comp(1)); var original = device.Snapshot();
        var progress = new ImmediateProgress(p => { if (!p.Restoring && p.Message.StartsWith("Saving the imported")) userCancel.Cancel(); });
        var result = await device.ImportCancellable(Detail(1, "Imported", Comp(1, false, 71)), userCancel.Token, progress);
        Assert.True(result.Cancelled); Assert.False(result.Saved);
        Assert.Equal(original.Detail, device.Data); Assert.Empty(device.Queries);
    }
}
