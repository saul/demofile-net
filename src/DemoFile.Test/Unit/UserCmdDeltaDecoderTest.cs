using DemoFile.Game.Cs;
using Google.Protobuf;

namespace DemoFile.Test.Unit;

[TestFixture]
public class UserCmdDeltaDecoderTest
{
    // Real delta from forze-reload-vs-nexus-m3-dust2.dem (patch 14197), player slot 0, command 12729
    private static readonly byte[] RealDelta = Convert.FromHexString(
        "0A0C10D1960350B69CE2CE06A701122E1F020D20CF96032DF811673F30D096030A0D20D096032D168EDC3E30D09603120D20D096032DC657743F30D19603");

    [Test]
    public void Apply_RealDelta()
    {
        var cmd = new CSGOUserCmdPB();
        cmd.Base = new CBaseUserCmdPB { ConsumedServerAngleChanges = 7 };

        UserCmdDeltaDecoder.Apply(cmd, RealDelta);

        Assert.That(cmd.Base.ClientTick, Is.EqualTo(52049));
        Assert.That(cmd.Base.RandomSeed, Is.EqualTo(1775799862));
        // `A7 01` = field 20 with wire type 7: cleared
        Assert.That(cmd.Base.HasConsumedServerAngleChanges, Is.False);

        // `1F` = array resized to 3, then elements 0, 1, 2
        Assert.That(cmd.InputHistory, Has.Count.EqualTo(3));
        Assert.That(cmd.InputHistory[0].RenderTickCount, Is.EqualTo(52047));
        Assert.That(cmd.InputHistory[0].PlayerTickCount, Is.EqualTo(52048));
        Assert.That(cmd.InputHistory[2].RenderTickCount, Is.EqualTo(52048));
        Assert.That(cmd.InputHistory[2].PlayerTickCount, Is.EqualTo(52049));
    }

    [Test]
    public void Apply_OnlyChangesFieldsInDelta()
    {
        var cmd = new CSGOUserCmdPB
        {
            Base = new CBaseUserCmdPB
            {
                ClientTick = 100,
                Forwardmove = 1.0f,
                Viewangles = new CMsgQAngle { X = 10.0f, Y = 20.0f },
            },
        };

        // base { viewangles { y = 25 } }
        var delta = new MemoryStream();
        var inner = new CMsgQAngle { Y = 25.0f }.ToByteArray();
        var baseDelta = new byte[] { 0x22, (byte)inner.Length }.Concat(inner).ToArray();
        delta.Write(new byte[] { 0x0A, (byte)baseDelta.Length });
        delta.Write(baseDelta);

        UserCmdDeltaDecoder.Apply(cmd, delta.ToArray());

        Assert.That(cmd.Base.ClientTick, Is.EqualTo(100));
        Assert.That(cmd.Base.Forwardmove, Is.EqualTo(1.0f));
        Assert.That(cmd.Base.Viewangles.X, Is.EqualTo(10.0f));
        Assert.That(cmd.Base.Viewangles.Y, Is.EqualTo(25.0f));
    }

    [Test]
    public void Apply_WireType7_ClearsFields()
    {
        var cmd = new CSGOUserCmdPB
        {
            Base = new CBaseUserCmdPB
            {
                Forwardmove = 1.0f,
                ButtonsPb = new CInButtonStatePB { Buttonstate1 = 1 },
            },
            Attack1StartHistoryIndex = 2,
        };

        // base { clear forwardmove (5), clear buttons_pb (3) }, clear attack1_start_history_index (6)
        UserCmdDeltaDecoder.Apply(cmd, new byte[] { 0x0A, 0x02, (5 << 3) | 7, (3 << 3) | 7, (6 << 3) | 7 });

        Assert.That(cmd.Base.HasForwardmove, Is.False);
        Assert.That(cmd.Base.ButtonsPb, Is.Null);
        Assert.That(cmd.HasAttack1StartHistoryIndex, Is.False);
    }

    [Test]
    public void Apply_ArrayDelta_ResizesAndPatchesElements()
    {
        var cmd = new CSGOUserCmdPB();
        cmd.InputHistory.Add(new CSGOInputHistoryEntryPB { RenderTickCount = 1, PlayerTickCount = 10 });
        cmd.InputHistory.Add(new CSGOInputHistoryEntryPB { RenderTickCount = 2 });
        cmd.InputHistory.Add(new CSGOInputHistoryEntryPB { RenderTickCount = 3 });

        // input_history { resize to 1 (0x0F), element 0 { render_tick_count = 5 } }
        UserCmdDeltaDecoder.Apply(cmd, new byte[] { 0x12, 0x05, 0x0F, 0x02, 0x02, 0x20, 0x05 });

        Assert.That(cmd.InputHistory, Has.Count.EqualTo(1));
        Assert.That(cmd.InputHistory[0].RenderTickCount, Is.EqualTo(5));
        Assert.That(cmd.InputHistory[0].PlayerTickCount, Is.EqualTo(10));

        // input_history { element 2 { render_tick_count = 7 } } grows the array
        UserCmdDeltaDecoder.Apply(cmd, new byte[] { 0x12, 0x04, 0x12, 0x02, 0x20, 0x07 });

        Assert.That(cmd.InputHistory, Has.Count.EqualTo(3));
        Assert.That(cmd.InputHistory[2].RenderTickCount, Is.EqualTo(7));
    }

    [Test]
    public void Apply_UnknownFields_AreSkipped()
    {
        var cmd = new CSGOUserCmdPB();

        // field 99 (varint 5), field 98 (fixed32), field 97 (length-delimited), field 96 (clear), then field 6 = 3
        var delta = new byte[]
        {
            0x98, 0x06, 0x05,
            0x95, 0x06, 0x00, 0x00, 0x80, 0x3F,
            0x8A, 0x06, 0x02, 0xAA, 0xBB,
            0x87, 0x06,
            0x30, 0x03,
        };
        UserCmdDeltaDecoder.Apply(cmd, delta);

        Assert.That(cmd.Attack1StartHistoryIndex, Is.EqualTo(3));
    }

    [Test]
    public void Stream_TracksCompletenessAndReusesState()
    {
        var stream = new UserCmdStream();
        var received = new List<UserCmd>();

        // 1. A delta for a player we've never seen: applied onto an empty command, incomplete
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 3, CmdNumber = 1, DeltaData = ByteString.CopyFrom(RealDelta) }), received.Add);
        Assert.That(received[^1].IsComplete, Is.False);
        Assert.That(received[^1].IsDelta, Is.True);
        Assert.That(received[^1].Command.Base.ClientTick, Is.EqualTo(52049));

        // 2. A full command: replaces the state, complete from now on
        var full = new CSGOUserCmdPB { Base = new CBaseUserCmdPB { ClientTick = 60000, PawnEntityHandle = 1234 } };
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 3, CmdNumber = 2, Data = full.ToByteString() }), received.Add);
        Assert.That(received[^1].IsComplete, Is.True);
        Assert.That(received[^1].IsDelta, Is.False);
        Assert.That(received[^1].Command.Base.PawnEntityHandle, Is.EqualTo(1234u));

        // 3. A delta on top of the full command: still complete, untouched fields kept
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 3, CmdNumber = 3, DeltaData = ByteString.CopyFrom(RealDelta) }), received.Add);
        Assert.That(received[^1].IsComplete, Is.True);
        Assert.That(received[^1].Command.Base.PawnEntityHandle, Is.EqualTo(1234u));
        Assert.That(received[^1].Command.Base.ClientTick, Is.EqualTo(52049));
        Assert.That(received[^1].CommandNumber, Is.EqualTo(3));
        Assert.That(received[^1].PlayerSlot, Is.EqualTo(new CPlayerSlot(3)));

        // 4. After a reset (e.g. seeking), state is forgotten
        stream.Reset();
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 3, CmdNumber = 4, DeltaData = ByteString.CopyFrom(RealDelta) }), received.Add);
        Assert.That(received[^1].IsComplete, Is.False);
        Assert.That(received[^1].Command.Base.HasPawnEntityHandle, Is.False);
    }

    [Test]
    public void Events_ListenOnlyWhileSubscribed()
    {
        var demo = new CsDemoParser();
        var full = new CSGOUserCmdPB { Base = new CBaseUserCmdPB { ClientTick = 60000, PawnEntityHandle = 1234 } };
        var fullMsg = Message(new CMsgServerUserCmd { PlayerSlot = 2, CmdNumber = 1, Data = full.ToByteString() });
        var deltaMsg = Message(new CMsgServerUserCmd { PlayerSlot = 2, CmdNumber = 2, DeltaData = ByteString.CopyFrom(RealDelta) });

        Assert.That(demo.PacketEvents.SvcUserCmdsRaw, Is.Null, "nothing is decoded without a subscriber");

        var first = new List<UserCmd>();
        var second = new List<UserCmd>();
        Action<UserCmd> onFirst = first.Add;
        Action<UserCmd> onSecond = second.Add;

        demo.UserCmdEvents.UserCmd += onFirst;
        demo.UserCmdEvents.UserCmd += onSecond;
        Assert.That(demo.PacketEvents.SvcUserCmdsRaw, Is.Not.Null);

        demo.PacketEvents.ParseNetMessage((int)SVC_Messages.SvcUserCmds, fullMsg);
        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(second, Has.Count.EqualTo(1));

        // Removing one subscriber keeps the other listening, and the player's state
        demo.UserCmdEvents.UserCmd -= onSecond;
        demo.PacketEvents.ParseNetMessage((int)SVC_Messages.SvcUserCmds, deltaMsg);
        Assert.That(first[^1].IsComplete, Is.True);
        Assert.That(first[^1].Command.Base.PawnEntityHandle, Is.EqualTo(1234u));

        // The last subscriber detaches from the underlying events
        demo.UserCmdEvents.UserCmd -= onFirst;
        Assert.That(demo.PacketEvents.SvcUserCmdsRaw, Is.Null);

        // Subscribing again starts from scratch: commands sent in between weren't tracked
        demo.UserCmdEvents.UserCmd += onFirst;
        demo.PacketEvents.ParseNetMessage((int)SVC_Messages.SvcUserCmds, deltaMsg);
        Assert.That(first[^1].IsComplete, Is.False);
        Assert.That(first[^1].Command.Base.HasPawnEntityHandle, Is.False);
    }

    [Test]
    public void Stream_MalformedDelta_IsDroppedAndStateReset()
    {
        var stream = new UserCmdStream();
        var received = new List<UserCmd>();

        var full = new CSGOUserCmdPB { Base = new CBaseUserCmdPB { ClientTick = 60000, PawnEntityHandle = 1234 } };
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 1, CmdNumber = 1, Data = full.ToByteString() }), received.Add);

        // Wire type 3 (start group) isn't valid in a delta; a truncated length is out of range
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 1, CmdNumber = 2, DeltaData = ByteString.CopyFrom(0x0B) }), received.Add);
        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 1, CmdNumber = 3, DeltaData = ByteString.CopyFrom(0x0A, 0x7F) }), received.Add);
        Assert.That(received, Has.Count.EqualTo(1));

        stream.Read(Message(new CMsgServerUserCmd { PlayerSlot = 1, CmdNumber = 4, DeltaData = ByteString.CopyFrom(RealDelta) }), received.Add);
        Assert.That(received[^1].IsComplete, Is.False);
        Assert.That(received[^1].Command.Base.HasPawnEntityHandle, Is.False);
    }

    [Test]
    public void Stream_DemoUserCmd_IsLengthPrefixedFullCommand()
    {
        var stream = new UserCmdStream();
        var received = new List<UserCmd>();

        var cmd = new CSGOUserCmdPB { Base = new CBaseUserCmdPB { ClientTick = 1241, ButtonsPb = new CInButtonStatePB { Buttonstate1 = 521 } } };
        var payload = cmd.ToByteArray();
        // POV demos: varint length + CSGOUserCmdPB (+ optional trailing padding)
        var data = new byte[] { (byte)payload.Length }.Concat(payload).Append((byte)0).ToArray();

        stream.ReadDemoUserCmd(new CDemoUserCmd { CmdNumber = 953, Data = ByteString.CopyFrom(data) }, playerSlot: 0, received.Add);

        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].CommandNumber, Is.EqualTo(953));
        Assert.That(received[0].ClientTick.Value, Is.EqualTo(1241u));
        Assert.That(received[0].Buttons, Is.EqualTo((InputButtons)521));
        Assert.That(received[0].IsComplete, Is.True);
        Assert.That(received[0].IsDelta, Is.False);
    }

    private static byte[] Message(params CMsgServerUserCmd[] commands)
    {
        var msg = new CSVCMsg_UserCommands();
        msg.Commands.AddRange(commands);
        return msg.ToByteArray();
    }
}
