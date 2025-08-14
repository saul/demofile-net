using System.Text;

namespace DemoFile.Game.Cs;

public partial class CBaseEntity
{
    public override string ToString() => $"{ServerClass.Name} ({EntityHandle})";

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

    /// <summary>
    /// Checks whether this entity has a model matching <paramref name="modelPath" />.
    /// </summary>
    /// <param name="modelPath">Path to the model, e.g. <c>weapons/models/defuser/defuser.vmdl</c></param>
    /// <returns><c>true</c> if this entity has the given model.</returns>
    public bool HasModel(string modelPath)
    {
        if (CBodyComponent is not CBodyComponentSkeletonInstance body)
            return false;

        Span<byte> bytes = stackalloc byte[Encoding.ASCII.GetByteCount(modelPath)];
        var len = Encoding.ASCII.GetBytes(modelPath, bytes);
        var expectedHash = MurmurHash.MurmurHash64(bytes[..len], MurmurHash.ResourceIdSeed);
        return body.SkeletonInstance.ModelState.Model.Value == expectedHash;
    }
}
