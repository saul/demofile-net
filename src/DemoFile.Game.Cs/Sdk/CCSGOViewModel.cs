using System.ComponentModel;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

[Obsolete("Removed in v14090")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public partial class CCSGOViewModel : CPredictedViewModel
{
    internal CCSGOViewModel(CsDemoParser.EntityContext context, SendNodeDecoder<object> decoder) : base(context, decoder) {}

    public bool ShouldIgnoreOffsetAndAccuracy { get; private set; }

    internal new static SendNodeDecoder<CCSGOViewModel> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        if (field.VarName == "m_bShouldIgnoreOffsetAndAccuracy")
        {
            var decoder = FieldDecode.CreateDecoder_bool(field.FieldEncodingInfo);
            return (CCSGOViewModel @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.ShouldIgnoreOffsetAndAccuracy = decoder(ref buffer);
            };
        }
        return CPredictedViewModel.CreateFieldDecoder(field, decoderSet);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FireCreateEvent()
    {
        Demo.EntityEvents.CCSGOViewModel.Create?.Invoke(this);
        base.FireCreateEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FireDeleteEvent()
    {
        Demo.EntityEvents.CCSGOViewModel.Delete?.Invoke(this);
        base.FireDeleteEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FirePreUpdateEvent()
    {
        Demo.EntityEvents.CCSGOViewModel.PreUpdate?.Invoke(this);
        base.FirePreUpdateEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FirePostUpdateEvent()
    {
        Demo.EntityEvents.CCSGOViewModel.PostUpdate?.Invoke(this);
        base.FirePostUpdateEvent();
    }
}

