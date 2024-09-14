namespace DemoFile.Sdk;

public partial class CBaseEntity
{
    public override string ToString() => $"{ServerClass.Name} ({EntityHandle})";

    public bool IsAlive => LifeState == 0;

    public TeamNumber CitadelTeamNum => (TeamNumber) TeamNum;

    public CCitadelTeam Team => Demo.GetTeam(CitadelTeamNum);

    /// Position of this entity in the game world.
    public Vector Origin => CBodyComponent is CBodyComponentSkeletonInstance body
        ? body.SkeletonInstance.Origin.Vector
        : default;

    /// The rotation of this entity.
    /// Note for players, use <see cref="CCitadelPlayerPawn.EyeAngles"/> for exact viewing angle.
    public QAngle Rotation => CBodyComponent is CBodyComponentSkeletonInstance body
        ? body.SkeletonInstance.Rotation
        : default;
}
