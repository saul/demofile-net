using System.ComponentModel;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

[Obsolete("Removed in v14090")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public partial class CCSObserver_ViewModelServices : CPlayer_ViewModelServices
{
    internal new static SendNodeDecoder<CCSObserver_ViewModelServices> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        return CPlayer_ViewModelServices.CreateFieldDecoder(field, decoderSet);
    }
}

