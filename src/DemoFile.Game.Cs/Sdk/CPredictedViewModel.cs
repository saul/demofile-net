using System.ComponentModel;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

[Obsolete("Removed in v14090")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public partial class CPredictedViewModel : CBaseViewModel
{
    internal CPredictedViewModel(CsDemoParser.EntityContext context, SendNodeDecoder<object> decoder) : base(context, decoder) {}

    internal new static SendNodeDecoder<CPredictedViewModel> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        return CBaseViewModel.CreateFieldDecoder(field, decoderSet);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FireCreateEvent()
    {
        Demo.EntityEvents.CPredictedViewModel.Create?.Invoke(this);
        base.FireCreateEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FireDeleteEvent()
    {
        Demo.EntityEvents.CPredictedViewModel.Delete?.Invoke(this);
        base.FireDeleteEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FirePreUpdateEvent()
    {
        Demo.EntityEvents.CPredictedViewModel.PreUpdate?.Invoke(this);
        base.FirePreUpdateEvent();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override void FirePostUpdateEvent()
    {
        Demo.EntityEvents.CPredictedViewModel.PostUpdate?.Invoke(this);
        base.FirePostUpdateEvent();
    }
}
