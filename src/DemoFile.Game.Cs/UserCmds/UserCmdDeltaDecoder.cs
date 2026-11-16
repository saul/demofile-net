using System.Collections;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using static DemoFile.Game.Cs.ProtoDeltaReader;
using ProtoFieldType = Google.Protobuf.Reflection.FieldType;

namespace DemoFile.Game.Cs;

/// <summary>
/// Applies Valve <c>codegen_delta_encoder</c> deltas (<c>CMsgServerUserCmd.delta_data</c>, CS2 patch 14197+)
/// onto the previous <see cref="CSGOUserCmdPB"/> for the same player, in place.
/// </summary>
/// <remarks>
/// <para>Format (protobuf wire format plus wire type 7):</para>
/// <list type="bullet">
/// <item>scalar field present → set to the new value</item>
/// <item>wire type 7 on a field → clear the field (no payload)</item>
/// <item>message field → nested delta applied onto the existing sub-message (created if absent)</item>
/// <item>repeated field → array delta, a nested buffer where tag <c>(n &lt;&lt; 3) | 7</c> resizes the array
/// to <c>n</c> and tag <c>(i &lt;&lt; 3) | 2</c> carries the delta for element <c>i</c></item>
/// </list>
/// <para>
/// The hot paths are hand-written per message type. Any field number not handled here (e.g. added by a
/// future proto update) falls back to a reflection-based applier, so it's still decoded correctly.
/// </para>
/// </remarks>
internal static class UserCmdDeltaDecoder
{
    public static void Apply(CSGOUserCmdPB m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CSGOUserCmdPB.BaseFieldNumber when wt == WireClear:
                    m.Base = null;
                    break;
                case CSGOUserCmdPB.BaseFieldNumber when wt == WireLengthDelimited:
                    Apply(m.Base ??= new CBaseUserCmdPB(), r.ReadLengthDelimited());
                    break;
                case CSGOUserCmdPB.InputHistoryFieldNumber when wt == WireClear:
                    m.InputHistory.Clear();
                    break;
                case CSGOUserCmdPB.InputHistoryFieldNumber when wt == WireLengthDelimited:
                    ApplyInputHistory(m.InputHistory, r.ReadLengthDelimited());
                    break;
                case CSGOUserCmdPB.Attack1StartHistoryIndexFieldNumber when wt == WireClear:
                    m.ClearAttack1StartHistoryIndex();
                    break;
                case CSGOUserCmdPB.Attack1StartHistoryIndexFieldNumber when wt == WireVarint:
                    m.Attack1StartHistoryIndex = r.ReadInt32();
                    break;
                case CSGOUserCmdPB.Attack2StartHistoryIndexFieldNumber when wt == WireClear:
                    m.ClearAttack2StartHistoryIndex();
                    break;
                case CSGOUserCmdPB.Attack2StartHistoryIndexFieldNumber when wt == WireVarint:
                    m.Attack2StartHistoryIndex = r.ReadInt32();
                    break;
                case CSGOUserCmdPB.LeftHandDesiredFieldNumber when wt == WireClear:
                    m.ClearLeftHandDesired();
                    break;
                case CSGOUserCmdPB.LeftHandDesiredFieldNumber when wt == WireVarint:
                    m.LeftHandDesired = r.ReadBool();
                    break;
                case CSGOUserCmdPB.IsPredictingBodyShotFxFieldNumber when wt == WireClear:
                    m.ClearIsPredictingBodyShotFx();
                    break;
                case CSGOUserCmdPB.IsPredictingBodyShotFxFieldNumber when wt == WireVarint:
                    m.IsPredictingBodyShotFx = r.ReadBool();
                    break;
                case CSGOUserCmdPB.IsPredictingHeadShotFxFieldNumber when wt == WireClear:
                    m.ClearIsPredictingHeadShotFx();
                    break;
                case CSGOUserCmdPB.IsPredictingHeadShotFxFieldNumber when wt == WireVarint:
                    m.IsPredictingHeadShotFx = r.ReadBool();
                    break;
                case CSGOUserCmdPB.IsPredictingKillRagdollsFieldNumber when wt == WireClear:
                    m.ClearIsPredictingKillRagdolls();
                    break;
                case CSGOUserCmdPB.IsPredictingKillRagdollsFieldNumber when wt == WireVarint:
                    m.IsPredictingKillRagdolls = r.ReadBool();
                    break;
                default:
                    ApplyFieldReflection(m, field, wt, ref r);
                    break;
            }
        }
    }

    public static void Apply(CBaseUserCmdPB m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            if (wt == WireClear)
            {
                switch (field)
                {
                    case CBaseUserCmdPB.LegacyCommandNumberFieldNumber: m.ClearLegacyCommandNumber(); break;
                    case CBaseUserCmdPB.ClientTickFieldNumber: m.ClearClientTick(); break;
                    case CBaseUserCmdPB.PredictionOffsetTicksX256FieldNumber: m.ClearPredictionOffsetTicksX256(); break;
                    case CBaseUserCmdPB.ButtonsPbFieldNumber: m.ButtonsPb = null; break;
                    case CBaseUserCmdPB.ViewanglesFieldNumber: m.Viewangles = null; break;
                    case CBaseUserCmdPB.ForwardmoveFieldNumber: m.ClearForwardmove(); break;
                    case CBaseUserCmdPB.LeftmoveFieldNumber: m.ClearLeftmove(); break;
                    case CBaseUserCmdPB.UpmoveFieldNumber: m.ClearUpmove(); break;
                    case CBaseUserCmdPB.ImpulseFieldNumber: m.ClearImpulse(); break;
                    case CBaseUserCmdPB.WeaponselectFieldNumber: m.ClearWeaponselect(); break;
                    case CBaseUserCmdPB.RandomSeedFieldNumber: m.ClearRandomSeed(); break;
                    case CBaseUserCmdPB.MousedxFieldNumber: m.ClearMousedx(); break;
                    case CBaseUserCmdPB.MousedyFieldNumber: m.ClearMousedy(); break;
                    case CBaseUserCmdPB.PawnEntityHandleFieldNumber: m.ClearPawnEntityHandle(); break;
                    case CBaseUserCmdPB.SubtickMovesFieldNumber: m.SubtickMoves.Clear(); break;
                    case CBaseUserCmdPB.MoveCrcFieldNumber: m.ClearMoveCrc(); break;
                    case CBaseUserCmdPB.ConsumedServerAngleChangesFieldNumber: m.ClearConsumedServerAngleChanges(); break;
                    case CBaseUserCmdPB.CmdFlagsFieldNumber: m.ClearCmdFlags(); break;
                    case CBaseUserCmdPB.ExecutionNotesFieldNumber: m.ExecutionNotes = null; break;
                    default: ApplyFieldReflection(m, field, wt, ref r); break;
                }

                continue;
            }

            switch (field)
            {
                case CBaseUserCmdPB.LegacyCommandNumberFieldNumber when wt == WireVarint: m.LegacyCommandNumber = r.ReadInt32(); break;
                case CBaseUserCmdPB.ClientTickFieldNumber when wt == WireVarint: m.ClientTick = r.ReadInt32(); break;
                case CBaseUserCmdPB.PredictionOffsetTicksX256FieldNumber when wt == WireVarint: m.PredictionOffsetTicksX256 = r.ReadUInt32(); break;
                case CBaseUserCmdPB.ButtonsPbFieldNumber when wt == WireLengthDelimited: Apply(m.ButtonsPb ??= new CInButtonStatePB(), r.ReadLengthDelimited()); break;
                case CBaseUserCmdPB.ViewanglesFieldNumber when wt == WireLengthDelimited: Apply(m.Viewangles ??= new CMsgQAngle(), r.ReadLengthDelimited()); break;
                case CBaseUserCmdPB.ForwardmoveFieldNumber when wt == WireFixed32: m.Forwardmove = r.ReadFloat(); break;
                case CBaseUserCmdPB.LeftmoveFieldNumber when wt == WireFixed32: m.Leftmove = r.ReadFloat(); break;
                case CBaseUserCmdPB.UpmoveFieldNumber when wt == WireFixed32: m.Upmove = r.ReadFloat(); break;
                case CBaseUserCmdPB.ImpulseFieldNumber when wt == WireVarint: m.Impulse = r.ReadInt32(); break;
                case CBaseUserCmdPB.WeaponselectFieldNumber when wt == WireVarint: m.Weaponselect = r.ReadInt32(); break;
                case CBaseUserCmdPB.RandomSeedFieldNumber when wt == WireVarint: m.RandomSeed = r.ReadInt32(); break;
                case CBaseUserCmdPB.MousedxFieldNumber when wt == WireVarint: m.Mousedx = r.ReadInt32(); break;
                case CBaseUserCmdPB.MousedyFieldNumber when wt == WireVarint: m.Mousedy = r.ReadInt32(); break;
                case CBaseUserCmdPB.PawnEntityHandleFieldNumber when wt == WireVarint: m.PawnEntityHandle = r.ReadUInt32(); break;
                case CBaseUserCmdPB.SubtickMovesFieldNumber when wt == WireLengthDelimited:
                    ApplySubtickMoves(m.SubtickMoves, r.ReadLengthDelimited());
                    break;
                case CBaseUserCmdPB.MoveCrcFieldNumber when wt == WireLengthDelimited: m.MoveCrc = ByteString.CopyFrom(r.ReadLengthDelimited()); break;
                case CBaseUserCmdPB.ConsumedServerAngleChangesFieldNumber when wt == WireVarint: m.ConsumedServerAngleChanges = r.ReadUInt32(); break;
                case CBaseUserCmdPB.CmdFlagsFieldNumber when wt == WireVarint: m.CmdFlags = r.ReadInt32(); break;
                case CBaseUserCmdPB.ExecutionNotesFieldNumber when wt == WireLengthDelimited: Apply(m.ExecutionNotes ??= new CBaseUserCmdExecutionNotes(), r.ReadLengthDelimited()); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CInButtonStatePB m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CInButtonStatePB.Buttonstate1FieldNumber when wt == WireVarint: m.Buttonstate1 = r.ReadVarint64(); break;
                case CInButtonStatePB.Buttonstate1FieldNumber when wt == WireClear: m.ClearButtonstate1(); break;
                case CInButtonStatePB.Buttonstate2FieldNumber when wt == WireVarint: m.Buttonstate2 = r.ReadVarint64(); break;
                case CInButtonStatePB.Buttonstate2FieldNumber when wt == WireClear: m.ClearButtonstate2(); break;
                case CInButtonStatePB.Buttonstate3FieldNumber when wt == WireVarint: m.Buttonstate3 = r.ReadVarint64(); break;
                case CInButtonStatePB.Buttonstate3FieldNumber when wt == WireClear: m.ClearButtonstate3(); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CSubtickMoveStep m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CSubtickMoveStep.ButtonFieldNumber when wt == WireVarint: m.Button = r.ReadVarint64(); break;
                case CSubtickMoveStep.ButtonFieldNumber when wt == WireClear: m.ClearButton(); break;
                case CSubtickMoveStep.PressedFieldNumber when wt == WireVarint: m.Pressed = r.ReadBool(); break;
                case CSubtickMoveStep.PressedFieldNumber when wt == WireClear: m.ClearPressed(); break;
                case CSubtickMoveStep.WhenFieldNumber when wt == WireFixed32: m.When = r.ReadFloat(); break;
                case CSubtickMoveStep.WhenFieldNumber when wt == WireClear: m.ClearWhen(); break;
                case CSubtickMoveStep.AnalogForwardDeltaFieldNumber when wt == WireFixed32: m.AnalogForwardDelta = r.ReadFloat(); break;
                case CSubtickMoveStep.AnalogForwardDeltaFieldNumber when wt == WireClear: m.ClearAnalogForwardDelta(); break;
                case CSubtickMoveStep.AnalogLeftDeltaFieldNumber when wt == WireFixed32: m.AnalogLeftDelta = r.ReadFloat(); break;
                case CSubtickMoveStep.AnalogLeftDeltaFieldNumber when wt == WireClear: m.ClearAnalogLeftDelta(); break;
                case CSubtickMoveStep.PitchDeltaFieldNumber when wt == WireFixed32: m.PitchDelta = r.ReadFloat(); break;
                case CSubtickMoveStep.PitchDeltaFieldNumber when wt == WireClear: m.ClearPitchDelta(); break;
                case CSubtickMoveStep.YawDeltaFieldNumber when wt == WireFixed32: m.YawDelta = r.ReadFloat(); break;
                case CSubtickMoveStep.YawDeltaFieldNumber when wt == WireClear: m.ClearYawDelta(); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CBaseUserCmdExecutionNotes m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CBaseUserCmdExecutionNotes.IgnoredReasonFieldNumber when wt == WireLengthDelimited:
                    m.IgnoredReason = System.Text.Encoding.UTF8.GetString(r.ReadLengthDelimited());
                    break;
                case CBaseUserCmdExecutionNotes.IgnoredReasonFieldNumber when wt == WireClear:
                    m.ClearIgnoredReason();
                    break;
                default:
                    ApplyFieldReflection(m, field, wt, ref r);
                    break;
            }
        }
    }

    public static void Apply(CSGOInputHistoryEntryPB m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            if (wt == WireClear)
            {
                switch (field)
                {
                    case CSGOInputHistoryEntryPB.ViewAnglesFieldNumber: m.ViewAngles = null; break;
                    case CSGOInputHistoryEntryPB.RenderTickCountFieldNumber: m.ClearRenderTickCount(); break;
                    case CSGOInputHistoryEntryPB.RenderTickFractionFieldNumber: m.ClearRenderTickFraction(); break;
                    case CSGOInputHistoryEntryPB.PlayerTickCountFieldNumber: m.ClearPlayerTickCount(); break;
                    case CSGOInputHistoryEntryPB.PlayerTickFractionFieldNumber: m.ClearPlayerTickFraction(); break;
                    case CSGOInputHistoryEntryPB.ClInterpFieldNumber: m.ClInterp = null; break;
                    case CSGOInputHistoryEntryPB.SvInterp0FieldNumber: m.SvInterp0 = null; break;
                    case CSGOInputHistoryEntryPB.SvInterp1FieldNumber: m.SvInterp1 = null; break;
                    case CSGOInputHistoryEntryPB.PlayerInterpFieldNumber: m.PlayerInterp = null; break;
                    case CSGOInputHistoryEntryPB.FrameNumberFieldNumber: m.ClearFrameNumber(); break;
                    case CSGOInputHistoryEntryPB.TargetEntIndexFieldNumber: m.ClearTargetEntIndex(); break;
                    case CSGOInputHistoryEntryPB.ShootPositionFieldNumber: m.ShootPosition = null; break;
                    case CSGOInputHistoryEntryPB.TargetHeadPosCheckFieldNumber: m.TargetHeadPosCheck = null; break;
                    case CSGOInputHistoryEntryPB.TargetAbsPosCheckFieldNumber: m.TargetAbsPosCheck = null; break;
                    case CSGOInputHistoryEntryPB.TargetAbsAngCheckFieldNumber: m.TargetAbsAngCheck = null; break;
                    default: ApplyFieldReflection(m, field, wt, ref r); break;
                }

                continue;
            }

            switch (field)
            {
                case CSGOInputHistoryEntryPB.ViewAnglesFieldNumber when wt == WireLengthDelimited: Apply(m.ViewAngles ??= new CMsgQAngle(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.RenderTickCountFieldNumber when wt == WireVarint: m.RenderTickCount = r.ReadInt32(); break;
                case CSGOInputHistoryEntryPB.RenderTickFractionFieldNumber when wt == WireFixed32: m.RenderTickFraction = r.ReadFloat(); break;
                case CSGOInputHistoryEntryPB.PlayerTickCountFieldNumber when wt == WireVarint: m.PlayerTickCount = r.ReadInt32(); break;
                case CSGOInputHistoryEntryPB.PlayerTickFractionFieldNumber when wt == WireFixed32: m.PlayerTickFraction = r.ReadFloat(); break;
                case CSGOInputHistoryEntryPB.ClInterpFieldNumber when wt == WireLengthDelimited: Apply(m.ClInterp ??= new CSGOInterpolationInfoPB_CL(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.SvInterp0FieldNumber when wt == WireLengthDelimited: Apply(m.SvInterp0 ??= new CSGOInterpolationInfoPB(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.SvInterp1FieldNumber when wt == WireLengthDelimited: Apply(m.SvInterp1 ??= new CSGOInterpolationInfoPB(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.PlayerInterpFieldNumber when wt == WireLengthDelimited: Apply(m.PlayerInterp ??= new CSGOInterpolationInfoPB(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.FrameNumberFieldNumber when wt == WireVarint: m.FrameNumber = r.ReadInt32(); break;
                case CSGOInputHistoryEntryPB.TargetEntIndexFieldNumber when wt == WireVarint: m.TargetEntIndex = r.ReadInt32(); break;
                case CSGOInputHistoryEntryPB.ShootPositionFieldNumber when wt == WireLengthDelimited: Apply(m.ShootPosition ??= new CMsgVector(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.TargetHeadPosCheckFieldNumber when wt == WireLengthDelimited: Apply(m.TargetHeadPosCheck ??= new CMsgVector(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.TargetAbsPosCheckFieldNumber when wt == WireLengthDelimited: Apply(m.TargetAbsPosCheck ??= new CMsgVector(), r.ReadLengthDelimited()); break;
                case CSGOInputHistoryEntryPB.TargetAbsAngCheckFieldNumber when wt == WireLengthDelimited: Apply(m.TargetAbsAngCheck ??= new CMsgQAngle(), r.ReadLengthDelimited()); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CSGOInterpolationInfoPB m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CSGOInterpolationInfoPB.SrcTickFieldNumber when wt == WireVarint: m.SrcTick = r.ReadInt32(); break;
                case CSGOInterpolationInfoPB.SrcTickFieldNumber when wt == WireClear: m.ClearSrcTick(); break;
                case CSGOInterpolationInfoPB.DstTickFieldNumber when wt == WireVarint: m.DstTick = r.ReadInt32(); break;
                case CSGOInterpolationInfoPB.DstTickFieldNumber when wt == WireClear: m.ClearDstTick(); break;
                case CSGOInterpolationInfoPB.FracFieldNumber when wt == WireFixed32: m.Frac = r.ReadFloat(); break;
                case CSGOInterpolationInfoPB.FracFieldNumber when wt == WireClear: m.ClearFrac(); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CSGOInterpolationInfoPB_CL m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CSGOInterpolationInfoPB_CL.FracFieldNumber when wt == WireFixed32: m.Frac = r.ReadFloat(); break;
                case CSGOInterpolationInfoPB_CL.FracFieldNumber when wt == WireClear: m.ClearFrac(); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CMsgQAngle m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CMsgQAngle.XFieldNumber when wt == WireFixed32: m.X = r.ReadFloat(); break;
                case CMsgQAngle.XFieldNumber when wt == WireClear: m.ClearX(); break;
                case CMsgQAngle.YFieldNumber when wt == WireFixed32: m.Y = r.ReadFloat(); break;
                case CMsgQAngle.YFieldNumber when wt == WireClear: m.ClearY(); break;
                case CMsgQAngle.ZFieldNumber when wt == WireFixed32: m.Z = r.ReadFloat(); break;
                case CMsgQAngle.ZFieldNumber when wt == WireClear: m.ClearZ(); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    public static void Apply(CMsgVector m, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
        {
            switch (field)
            {
                case CMsgVector.XFieldNumber when wt == WireFixed32: m.X = r.ReadFloat(); break;
                case CMsgVector.XFieldNumber when wt == WireClear: m.ClearX(); break;
                case CMsgVector.YFieldNumber when wt == WireFixed32: m.Y = r.ReadFloat(); break;
                case CMsgVector.YFieldNumber when wt == WireClear: m.ClearY(); break;
                case CMsgVector.ZFieldNumber when wt == WireFixed32: m.Z = r.ReadFloat(); break;
                case CMsgVector.ZFieldNumber when wt == WireClear: m.ClearZ(); break;
                case CMsgVector.WFieldNumber when wt == WireFixed32: m.W = r.ReadFloat(); break;
                case CMsgVector.WFieldNumber when wt == WireClear: m.ClearW(); break;
                default: ApplyFieldReflection(m, field, wt, ref r); break;
            }
        }
    }

    // Array deltas: tag `(n << 3) | 7` resizes the array to n elements; tag `(i << 3) | 2` applies a delta
    // to element i (growing the array if needed). New elements start empty: verified on 14197 demos, where
    // every element that (re)appears carries all of its fields.
    // These are written out per element type (rather than generically with delegates) because they're hot.

    private static void ApplyInputHistory(RepeatedField<CSGOInputHistoryEntryPB> list, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var index, out var wt))
        {
            if (wt == WireClear)
            {
                TrimTo(list, index);
                while (list.Count < index)
                    list.Add(new CSGOInputHistoryEntryPB());
            }
            else if (wt == WireLengthDelimited)
            {
                while (list.Count <= index)
                    list.Add(new CSGOInputHistoryEntryPB());
                Apply(list[index], r.ReadLengthDelimited());
            }
            else
            {
                r.SkipField(wt);
            }
        }
    }

    private static void ApplySubtickMoves(RepeatedField<CSubtickMoveStep> list, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var index, out var wt))
        {
            if (wt == WireClear)
            {
                TrimTo(list, index);
                while (list.Count < index)
                    list.Add(new CSubtickMoveStep());
            }
            else if (wt == WireLengthDelimited)
            {
                while (list.Count <= index)
                    list.Add(new CSubtickMoveStep());
                Apply(list[index], r.ReadLengthDelimited());
            }
            else
            {
                r.SkipField(wt);
            }
        }
    }

    private static void TrimTo<T>(RepeatedField<T> list, int count)
    {
        while (list.Count > count)
            list.RemoveAt(list.Count - 1);
    }

    #region Reflection fallback (fields not handled above)

    /// <summary>
    /// Apply a single field delta using protobuf reflection. Only used for fields that aren't handled by the
    /// hand-written appliers, e.g. fields added in a newer proto. Unknown field numbers are skipped.
    /// </summary>
    private static void ApplyFieldReflection(IMessage message, int fieldNumber, int wireType, ref ProtoDeltaReader r)
    {
        var field = message.Descriptor.FindFieldByNumber(fieldNumber);
        if (field == null)
        {
            r.SkipField(wireType);
            return;
        }

        if (wireType == WireClear)
        {
            field.Accessor.Clear(message);
            return;
        }

        if (field.IsRepeated)
        {
            if (wireType != WireLengthDelimited || field.FieldType != ProtoFieldType.Message)
            {
                // Repeated scalars aren't used by any delta-encoded message yet
                r.SkipField(wireType);
                return;
            }

            ApplyArrayReflection((IList)field.Accessor.GetValue(message), field.MessageType, r.ReadLengthDelimited());
            return;
        }

        if (field.FieldType == ProtoFieldType.Message)
        {
            if (wireType != WireLengthDelimited)
            {
                r.SkipField(wireType);
                return;
            }

            var sub = field.Accessor.GetValue(message) as IMessage ?? CreateMessage(field.MessageType);
            ApplyReflection(sub, r.ReadLengthDelimited());
            field.Accessor.SetValue(message, sub);
            return;
        }

        if (!TryReadScalar(field, wireType, ref r, out var value))
        {
            r.SkipField(wireType);
            return;
        }

        field.Accessor.SetValue(message, value);
    }

    private static void ApplyReflection(IMessage message, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var field, out var wt))
            ApplyFieldReflection(message, field, wt, ref r);
    }

    private static void ApplyArrayReflection(IList list, MessageDescriptor elementType, ReadOnlySpan<byte> delta)
    {
        var r = new ProtoDeltaReader(delta);
        while (r.TryReadTag(out var index, out var wt))
        {
            if (wt == WireClear)
            {
                while (list.Count > index)
                    list.RemoveAt(list.Count - 1);
                while (list.Count < index)
                    list.Add(CreateMessage(elementType));
                continue;
            }

            if (wt != WireLengthDelimited)
            {
                r.SkipField(wt);
                continue;
            }

            while (list.Count <= index)
                list.Add(CreateMessage(elementType));

            ApplyReflection((IMessage)list[index]!, r.ReadLengthDelimited());
        }
    }

    private static IMessage CreateMessage(MessageDescriptor descriptor) =>
        descriptor.Parser.ParseFrom(ByteString.Empty);

    private static bool TryReadScalar(FieldDescriptor field, int wireType, ref ProtoDeltaReader r, out object value)
    {
        value = null!;
        switch (field.FieldType)
        {
            case ProtoFieldType.Float when wireType == WireFixed32:
                value = r.ReadFloat();
                return true;
            case ProtoFieldType.Int32 when wireType == WireVarint:
                value = r.ReadInt32();
                return true;
            case ProtoFieldType.UInt32 when wireType == WireVarint:
                value = r.ReadUInt32();
                return true;
            case ProtoFieldType.Int64 when wireType == WireVarint:
                value = (long)r.ReadVarint64();
                return true;
            case ProtoFieldType.UInt64 when wireType == WireVarint:
                value = r.ReadVarint64();
                return true;
            case ProtoFieldType.Bool when wireType == WireVarint:
                value = r.ReadBool();
                return true;
            case ProtoFieldType.Enum when wireType == WireVarint:
                value = r.ReadInt32();
                return true;
            case ProtoFieldType.SInt32 when wireType == WireVarint:
            {
                var raw = r.ReadUInt32();
                value = (int)(raw >> 1) ^ -(int)(raw & 1);
                return true;
            }
            case ProtoFieldType.String when wireType == WireLengthDelimited:
                value = System.Text.Encoding.UTF8.GetString(r.ReadLengthDelimited());
                return true;
            case ProtoFieldType.Bytes when wireType == WireLengthDelimited:
                value = ByteString.CopyFrom(r.ReadLengthDelimited());
                return true;
            default:
                return false;
        }
    }

    #endregion
}
