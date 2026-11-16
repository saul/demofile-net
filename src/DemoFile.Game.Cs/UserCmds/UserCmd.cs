namespace DemoFile.Game.Cs;

/// <summary>
/// A player's input for one command, decoded from <c>svc_UserCmds</c>.
/// </summary>
/// <param name="PlayerSlot">Player slot of the player who sent the command (controller entity index - 1).</param>
/// <param name="CommandNumber">Client command number. Increases by one per command.</param>
/// <param name="ServerTickExecuted">Server tick on which the command was executed.</param>
/// <param name="ClientTick">Client tick the command was created on.</param>
/// <param name="Command">
/// The decoded command.
/// <para>
/// IMPORTANT: since CS2 patch 14197 commands are delta-encoded against the player's previous command, so this
/// instance is updated in place and reused for the player's next command. Use <c>Command.Clone()</c> if you
/// need to keep it beyond the callback.
/// </para>
/// </param>
/// <param name="IsComplete">
/// <c>true</c> if every field of <paramref name="Command"/> is known. Delta-encoded commands only carry the
/// fields that changed, so after the start of a recording or a seek, fields that haven't changed since
/// (and so haven't been sent yet) are missing until the server sends the player a full command.
/// Fields that change often (view angles, buttons, input history) fill in almost immediately.
/// </param>
/// <param name="IsDelta"><c>true</c> if the command was delta-encoded, <c>false</c> if it was sent in full.</param>
public readonly record struct UserCmd(
    CPlayerSlot PlayerSlot,
    int CommandNumber,
    GameTick ServerTickExecuted,
    GameTick ClientTick,
    CSGOUserCmdPB Command,
    bool IsComplete,
    bool IsDelta)
{
    /// <summary>
    /// Buttons held down for this command (<c>base.buttons_pb.buttonstate1</c>).
    /// </summary>
    /// <remarks>
    /// This is the same mask as the pawn's networked <c>m_nButtonDownMaskPrev</c> (<see cref="CCSPlayerPawn.InputButtons"/>),
    /// which is no longer networked from CS2 patch 14197.
    /// </remarks>
    public InputButtons Buttons => (InputButtons)(Command.Base?.ButtonsPb?.Buttonstate1 ?? 0);

    /// <summary>
    /// The controller of the player who sent the command, if it exists.
    /// </summary>
    public CCSPlayerController? GetController(CsDemoParser demo) =>
        demo.GetEntityByIndex<CCSPlayerController>(new CEntityIndex(PlayerSlot.Value + 1));
}
