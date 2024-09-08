#pragma warning disable CS1591

namespace DemoFile;

public struct GameEvents
{
    public Action<CMsgTEPlayerAnimEvent>? PlayerAnimEvent;
    public Action<CMsgTERadioIcon>? RadioIcon;
    public Action<CMsgTEFireBullets>? FireBullets;

    internal bool ParseNetMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            /*
            case (int) ECsgoGameEvents.GePlayerAnimEventId:
                PlayerAnimEvent?.Invoke(CMsgTEPlayerAnimEvent.Parser.ParseFrom(buf));
                return true;
            case (int) ECsgoGameEvents.GeRadioIconEventId:
                RadioIcon?.Invoke(CMsgTERadioIcon.Parser.ParseFrom(buf));
                return true;
            */
            case (int) ECsgoGameEvents.GeFireBulletsId:
                FireBullets?.Invoke(CMsgTEFireBullets.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
