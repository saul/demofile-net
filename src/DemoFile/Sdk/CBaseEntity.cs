namespace DemoFile.Sdk;

public partial class CBaseEntity
{
    public bool IsAlive => LifeState == 0;

    public CSTeamNumber CSTeamNum => (CSTeamNumber) TeamNum;

    public CCSTeam Team => Demo.GetTeam(CSTeamNum);

    /// Position of this entity in the game world.
    public Vector Origin => CBodyComponent is CBodyComponentSkeletonInstance body
        ? body.SkeletonInstance.Origin.Vector
        : default;

    /// The rotation of this entity.
    /// Note for players, use <see cref="CCSPlayerPawnBase.EyeAngles"/> for exact viewing angle.
    public QAngle Rotation => CBodyComponent is CBodyComponentSkeletonInstance body
        ? body.SkeletonInstance.Rotation
        : default;
}
