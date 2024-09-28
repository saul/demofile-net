namespace DemoFile.Game.Dota;

public partial class CBaseEntity
{
    public override string ToString() => $"{ServerClass.Name} ({EntityHandle})";

    public bool IsAlive => LifeState == 0;

    public TeamNumber DotaTeamNum => (TeamNumber) TeamNum;

    public CDOTATeam Team => Demo.GetTeam(DotaTeamNum);

    /// Position of this entity in the game world.
    public Vector Origin => CBodyComponent is CBodyComponentSkeletonInstance body
        ? body.SkeletonInstance.Origin.Vector
        : default;

    /// The rotation of this entity.
    public QAngle Rotation => CBodyComponent is CBodyComponentSkeletonInstance body
        ? body.SkeletonInstance.Rotation
        : default;
}
