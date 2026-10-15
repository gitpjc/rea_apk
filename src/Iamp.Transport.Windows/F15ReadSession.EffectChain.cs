using Iamp.Protocol;

namespace Iamp.Transport.Windows;

public sealed partial class F15ReadSession
{
    // Allocate separate bands from observed mobile starts 0/50/100/150. Keep these across
    // reconnects: the amplifier can suppress a token reused by a new BLE session.
    private static int addChainSequence=-1,removeChainSequence=-1,replaceChainSequence=-1,moveChainSequence=-1;
    private static byte ChainToken(ref int sequence,int start)=>checked((byte)(start+(uint)Interlocked.Increment(ref sequence)%50));

    public Task<CurrentPresetSnapshot> MoveEffectAsync(CurrentPresetSnapshot expected,byte from,byte to,CancellationToken ct)
        => ChangeChainAsync(expected,b=>EffectChainCommands.Move(b.Blocks,from,to,ChainToken(ref moveChainSequence,150)),
            (b,a)=>EffectChainCommands.IsExpectedMove(Convert.FromHexString(b.RawDetailPayloadHex),Convert.FromHexString(a.RawDetailPayloadHex),from,to),ct);
    public Task<CurrentPresetSnapshot> RemoveEffectAsync(CurrentPresetSnapshot expected,byte position,CancellationToken ct)
        => ChangeChainAsync(expected,b=>EffectChainCommands.Remove(b.Blocks,position,ChainToken(ref removeChainSequence,50)),
            (b,a)=>EffectChainCommands.IsExpectedRemoval(Convert.FromHexString(b.RawDetailPayloadHex),Convert.FromHexString(a.RawDetailPayloadHex),position),ct);
    public Task<CurrentPresetSnapshot> AddEffectAsync(CurrentPresetSnapshot expected,EffectDefinition effect,CancellationToken ct)
        => ChangeChainAsync(expected,b=>EffectChainCommands.Add(b.Blocks,effect,ChainToken(ref addChainSequence,0)),
            (b,a)=>EffectChainCommands.IsExpectedAddition(Convert.FromHexString(b.RawDetailPayloadHex),Convert.FromHexString(a.RawDetailPayloadHex),effect),ct);

    public Task<CurrentPresetSnapshot> ReplaceEffectAsync(CurrentPresetSnapshot expected,byte position,EffectDefinition effect,CancellationToken ct)
    {
        var block=EffectChainCommands.Active(expected.Blocks).Single(b=>b.Position==position);
        if(block.TypeCode==effect.TypeCode && block.Values[1]==effect.Model)
            throw new InvalidOperationException("That model is already selected; replacing it would reset its values.");
        return ChangeChainAsync(expected,b=>EffectChainCommands.Replace(b.Blocks,position,effect,ChainToken(ref replaceChainSequence,100)),
            (b,a)=>EffectChainCommands.IsExpectedReplacement(Convert.FromHexString(b.RawDetailPayloadHex),Convert.FromHexString(a.RawDetailPayloadHex),position,effect),ct);
    }

    private Task<CurrentPresetSnapshot> ChangeChainAsync(CurrentPresetSnapshot expected,
        Func<CurrentPresetSnapshot,byte[]> encode,Func<CurrentPresetSnapshot,CurrentPresetSnapshot,bool> verify,CancellationToken ct)
        =>RunWorkflowAsync(async token=> {
            var identity=await ReadIdentityCoreAsync(token);
            if(identity.Firmware!="V1.1.9")throw new NotSupportedException("Effect-chain editing is verified on F15i V1.1.9 only.");
            var before=await ReadCurrentPresetCoreAsync(token);
            if(before.WireIndex!=expected.WireIndex || before.RawStatePayloadHex!=expected.RawStatePayloadHex || before.RawDetailPayloadHex!=expected.RawDetailPayloadHex)
                throw new EffectChainReadbackException("The amplifier changed. Read it again before editing the chain; nothing was sent.",before);
            await WriteObservedUpdateAsync(encode(before),token);
            var after=await ReadCurrentPresetCoreAsync(token);
            // A restarted client can collide with the amp's previous token.
            // Only an exact, successful complete read proving no change permits
            // one fresh-token attempt. Never retry a failed/ambiguous read or a
            // partial/unexpected change, and never retry a save.
            if(after.RawStatePayloadHex==before.RawStatePayloadHex && after.RawDetailPayloadHex==before.RawDetailPayloadHex && !verify(before,after)) {
                await WriteObservedUpdateAsync(encode(before),token);
                after=await ReadCurrentPresetCoreAsync(token);
            }
            if(after.WireIndex!=before.WireIndex || after.RawStatePayloadHex!=before.RawStatePayloadHex || !verify(before,after))
                throw new EffectChainReadbackException("Effect-chain readback differs from the requested change. Refresh before another action; nothing was saved.",after);
            return after;
        },ct);
}
