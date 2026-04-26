namespace DemoFile.Game.Cs;

public partial class CCSObserverPawn
{
    public new CCSObserver_ObserverServices? ObserverServices => (CCSObserver_ObserverServices?) base.ObserverServices;

    public new CCSObserver_MovementServices? MovementServices => (CCSObserver_MovementServices?) base.MovementServices;

    public new CCSObserver_CameraServices? CameraServices => (CCSObserver_CameraServices?) base.CameraServices;

    public new CCSObserver_UseServices? UseServices => (CCSObserver_UseServices?) base.UseServices;
}