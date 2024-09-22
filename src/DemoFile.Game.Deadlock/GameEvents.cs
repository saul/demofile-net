#pragma warning disable CS1591

namespace DemoFile.Game.Deadlock;

public struct GameEvents
{
    public Action<CMsgFireBullets>? FireBullets;
    public Action<CMsgPlayerAnimEvent>? PlayerAnimEvent;
    public Action<CMsgParticleSystemManager>? ParticleSystemManager;
    public Action<CMsgScreenTextPretty>? ScreenTextPretty;
    public Action<CMsgServerRequestedTracer>? ServerRequestedTracer;
    public Action<CMsgBulletImpact>? BulletImpact;
    public Action<CMsgEnableSatVolumesEvent>? EnableSatVolumesEvent;
    public Action<CMsgPlaceSatVolumeEvent>? PlaceSatVolumeEvent;
    public Action<CMsgDisableSatVolumesEvent>? DisableSatVolumesEvent;
    public Action<CMsgRemoveSatVolumeEvent>? RemoveSatVolumeEvent;

    internal bool ParseNetMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int) ECitadelGameEvents.GeFireBullets:
                FireBullets?.Invoke(CMsgFireBullets.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GePlayerAnimEvent:
                PlayerAnimEvent?.Invoke(CMsgPlayerAnimEvent.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeParticleSystemManager:
                ParticleSystemManager?.Invoke(CMsgParticleSystemManager.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeScreenTextPretty:
                ScreenTextPretty?.Invoke(CMsgScreenTextPretty.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeServerRequestedTracer:
                ServerRequestedTracer?.Invoke(CMsgServerRequestedTracer.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeBulletImpact:
                BulletImpact?.Invoke(CMsgBulletImpact.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeEnableSatVolumesEvent:
                EnableSatVolumesEvent?.Invoke(CMsgEnableSatVolumesEvent.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GePlaceSatVolumeEvent:
                PlaceSatVolumeEvent?.Invoke(CMsgPlaceSatVolumeEvent.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeDisableSatVolumesEvent:
                DisableSatVolumesEvent?.Invoke(CMsgDisableSatVolumesEvent.Parser.ParseFrom(buf));
                return true;
            case (int) ECitadelGameEvents.GeRemoveSatVolumeEvent:
                RemoveSatVolumeEvent?.Invoke(CMsgRemoveSatVolumeEvent.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
