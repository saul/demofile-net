namespace DemoFile;

internal readonly record struct QueuedPacket(int MsgType, byte[] RentedBuf, int Size)
{
    public ReadOnlySpan<byte> MsgBuffer => RentedBuf.AsSpan(0, Size);

    public static int GetPriority(int msgType) => msgType switch
    {
        // These messages provide context needed for the rest of the tick
        // and should have the highest priority.
        (int) NET_Messages.NetTick
            | (int) SVC_Messages.SvcCreateStringTable
            | (int) SVC_Messages.SvcUpdateStringTable => -10,

        // These messages benefit from having context but may also need to
        // provide context in terms of delta updates.
        (int) SVC_Messages.SvcPacketEntities => 5,

        // These messages benefit from having as much context as possible and
        // should have the lowest priority.
        (int) EBaseGameEvents.GeSource1LegacyGameEvent => 10,

        _ => 0
    };
}