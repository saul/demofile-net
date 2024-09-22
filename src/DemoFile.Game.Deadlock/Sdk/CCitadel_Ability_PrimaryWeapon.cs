namespace DemoFile.Game.Deadlock;

public partial class CCitadel_Ability_PrimaryWeapon
{
    private static FieldDecode.CustomDeserializer<CCitadel_Ability_PrimaryWeapon, int> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CCitadel_Ability_PrimaryWeapon _, ref BitBuffer buffer) => (int)buffer.ReadUVarInt32() - 1;
    }
}
