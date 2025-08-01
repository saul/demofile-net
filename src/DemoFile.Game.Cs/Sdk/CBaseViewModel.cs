using System.ComponentModel;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

[Obsolete("Removed in v14090")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
// MNetworkIncludeByName "m_nModelIndex"
// MNetworkIncludeByName "m_hModel"
// MNetworkIncludeByName "m_hOwnerEntity"
// MNetworkIncludeByName "m_MeshGroupMask"
// MNetworkIncludeByName "m_fEffects"
// MNetworkIncludeByName "m_baseLayer.m_hSequence"
// MNetworkIncludeByName "m_animationController.m_flPlaybackRate"
// MNetworkIncludeByName "m_flAnimTime"
// MNetworkIncludeByName "m_flSimulationTime"
// MNetworkIncludeByName "m_animationController.m_animGraphNetworkedVars"
// MNetworkIncludeByName "m_nResetEventsParity"
// MNetworkExcludeByUserGroup "m_flPoseParameter"
// MNetworkOverride "m_fEffects "
// MNetworkIncludeByName "m_clrRender"
public partial class CBaseViewModel : CBaseAnimGraph
{
    internal CBaseViewModel(CsDemoParser.EntityContext context, SendNodeDecoder<object> decoder) : base(context, decoder) {}

    public UInt32 ViewModelIndex { get; private set; }

    public UInt32 AnimationParity { get; private set; }

    public float AnimationStartTime { get; private set; }

    public CHandle<CBasePlayerWeapon, CsDemoParser> WeaponHandle { get; private set; }
    public CBasePlayerWeapon? Weapon => WeaponHandle.Get(Demo);

    public CHandle<CBaseEntity, CsDemoParser> ControlPanelHandle { get; private set; }
    public CBaseEntity? ControlPanel => ControlPanelHandle.Get(Demo);

    internal new static SendNodeDecoder<CBaseViewModel> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        if (field.VarName == "m_nViewModelIndex")
        {
            var decoder = FieldDecode.CreateDecoder_UInt32(field.FieldEncodingInfo);
            return (CBaseViewModel @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.ViewModelIndex = decoder(ref buffer);
            };
        }
        if (field.VarName == "m_nAnimationParity")
        {
            var decoder = FieldDecode.CreateDecoder_UInt32(field.FieldEncodingInfo);
            return (CBaseViewModel @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.AnimationParity = decoder(ref buffer);
            };
        }
        if (field.VarName == "m_flAnimationStartTime")
        {
            var decoder = FieldDecode.CreateDecoder_float(field.FieldEncodingInfo);
            return (CBaseViewModel @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.AnimationStartTime = decoder(ref buffer);
            };
        }
        if (field.VarName == "m_hWeapon")
        {
            var decoder = FieldDecode.CreateDecoder_CHandle<CBasePlayerWeapon, CsDemoParser>(field.FieldEncodingInfo);
            return (CBaseViewModel @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.WeaponHandle = decoder(ref buffer);
            };
        }
        if (field.VarName == "m_hControlPanel")
        {
            var decoder = FieldDecode.CreateDecoder_CHandle<CBaseEntity, CsDemoParser>(field.FieldEncodingInfo);
            return (CBaseViewModel @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.ControlPanelHandle = decoder(ref buffer);
            };
        }
        return CBaseAnimGraph.CreateFieldDecoder(field, decoderSet);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FireCreateEvent()
    {
        Demo.EntityEvents.CBaseViewModel.Create?.Invoke(this);
        base.FireCreateEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FireDeleteEvent()
    {
        Demo.EntityEvents.CBaseViewModel.Delete?.Invoke(this);
        base.FireDeleteEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FirePreUpdateEvent()
    {
        Demo.EntityEvents.CBaseViewModel.PreUpdate?.Invoke(this);
        base.FirePreUpdateEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FirePostUpdateEvent()
    {
        Demo.EntityEvents.CBaseViewModel.PostUpdate?.Invoke(this);
        base.FirePostUpdateEvent();
    }
}

