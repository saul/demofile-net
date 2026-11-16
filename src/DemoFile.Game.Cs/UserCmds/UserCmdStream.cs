namespace DemoFile.Game.Cs;

/// <summary>
/// Decodes <c>svc_UserCmds</c> (<see cref="CSVCMsg_UserCommands"/>), tracking each player's last command so
/// delta-encoded commands can be applied.
/// </summary>
/// <remarks>
/// The outer messages are read straight from the message buffer, and deltas are applied in place onto a
/// cached <see cref="CSGOUserCmdPB"/> per player, so decoding a command doesn't allocate in the common case.
/// </remarks>
internal sealed class UserCmdStream
{
    private sealed class PlayerState
    {
        public CSGOUserCmdPB Command = new();
        public bool IsComplete;
    }

    private PlayerState?[] _players = new PlayerState?[64];

    /// <summary>
    /// Forget all players' previous commands, e.g. after seeking, or when we start listening part way through.
    /// </summary>
    public void Reset() => Array.Clear(_players);

    public void Read(ReadOnlySpan<byte> msgBuf, Action<UserCmd> callback)
    {
        // CSVCMsg_UserCommands { repeated CMsgServerUserCmd commands = 1; }
        var r = new ProtoDeltaReader(msgBuf);
        while (r.TryReadTag(out var field, out var wt))
        {
            if (field == CSVCMsg_UserCommands.CommandsFieldNumber && wt == ProtoDeltaReader.WireLengthDelimited)
                ReadCommand(r.ReadLengthDelimited(), callback);
            else
                r.SkipField(wt);
        }
    }

    private void ReadCommand(ReadOnlySpan<byte> buf, Action<UserCmd> callback)
    {
        // CMsgServerUserCmd
        ReadOnlySpan<byte> data = default;
        ReadOnlySpan<byte> deltaData = default;
        var hasData = false;
        var hasDelta = false;
        var cmdNumber = 0;
        var playerSlot = -1;
        var serverTickExecuted = 0;
        var clientTick = 0;

        var r = new ProtoDeltaReader(buf);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CMsgServerUserCmd.DataFieldNumber when wt == ProtoDeltaReader.WireLengthDelimited:
                    data = r.ReadLengthDelimited();
                    hasData = true;
                    break;
                case CMsgServerUserCmd.CmdNumberFieldNumber when wt == ProtoDeltaReader.WireVarint:
                    cmdNumber = r.ReadInt32();
                    break;
                case CMsgServerUserCmd.PlayerSlotFieldNumber when wt == ProtoDeltaReader.WireVarint:
                    playerSlot = r.ReadInt32();
                    break;
                case CMsgServerUserCmd.ServerTickExecutedFieldNumber when wt == ProtoDeltaReader.WireVarint:
                    serverTickExecuted = r.ReadInt32();
                    break;
                case CMsgServerUserCmd.ClientTickFieldNumber when wt == ProtoDeltaReader.WireVarint:
                    clientTick = r.ReadInt32();
                    break;
                case CMsgServerUserCmd.DeltaDataFieldNumber when wt == ProtoDeltaReader.WireLengthDelimited:
                    deltaData = r.ReadLengthDelimited();
                    hasDelta = true;
                    break;
                default:
                    r.SkipField(wt);
                    break;
            }
        }

        if (playerSlot < 0)
            return;

        var player = GetPlayer(playerSlot);

        // Full commands (and all commands before patch 14197) replace the previous state.
        // Otherwise apply the delta; a command with neither is treated as unchanged.
        if (hasData)
        {
            player.Command = CSGOUserCmdPB.Parser.ParseFrom(data);
            player.IsComplete = true;
        }
        else if (hasDelta)
        {
            try
            {
                UserCmdDeltaDecoder.Apply(player.Command, deltaData);
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                // Malformed delta. The engine applies deltas to a copy and drops the command if it fails
                // ("delta apply failed"); we apply in place, so the player's state is no longer trustworthy.
                player.Command = new CSGOUserCmdPB();
                player.IsComplete = false;
                return;
            }
        }

        callback(new UserCmd(
            new CPlayerSlot((uint)playerSlot),
            cmdNumber,
            new GameTick((uint)serverTickExecuted),
            new GameTick((uint)clientTick),
            player.Command,
            player.IsComplete,
            IsDelta: !hasData));
    }

    /// <summary>
    /// Read a <c>DEM_UserCmd</c>: the recording player's own command in a POV demo.
    /// </summary>
    /// <remarks>
    /// <c>CDemoUserCmd.data</c> is a varint length followed by a full <see cref="CSGOUserCmdPB"/>
    /// (sometimes with trailing padding). Verified on POV demos from patches 14000 and 14030.
    /// The server tick the command was executed on isn't recorded.
    /// </remarks>
    public void ReadDemoUserCmd(CDemoUserCmd msg, int playerSlot, Action<UserCmd> callback)
    {
        if (playerSlot < 0)
            return;

        var data = msg.Data.Span;
        var r = new ProtoDeltaReader(data);
        var length = data.IsEmpty ? 0 : (int)r.ReadVarint32();
        var headerLength = data.Length - r.Remaining;
        var payload = headerLength + length <= data.Length
            ? data.Slice(headerLength, length)
            : data;

        var player = GetPlayer(playerSlot);
        player.Command = CSGOUserCmdPB.Parser.ParseFrom(payload);
        player.IsComplete = true;

        callback(new UserCmd(
            new CPlayerSlot((uint)playerSlot),
            msg.CmdNumber,
            default,
            new GameTick((uint)(player.Command.Base?.ClientTick ?? 0)),
            player.Command,
            IsComplete: true,
            IsDelta: false));
    }

    private PlayerState GetPlayer(int playerSlot)
    {
        if (playerSlot >= _players.Length)
            Array.Resize(ref _players, Math.Max(playerSlot + 1, _players.Length * 2));

        return _players[playerSlot] ??= new PlayerState();
    }
}
