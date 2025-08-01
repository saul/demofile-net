using System.ComponentModel;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

[Obsolete("Removed in v14090")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public partial class CCSPlayer_ViewModelServices : CPlayer_ViewModelServices
{
    public CHandle<CBaseViewModel, CsDemoParser>[] ViewModel { get; private set; } = Array.Empty<CHandle<CBaseViewModel, CsDemoParser>>();

    internal new static SendNodeDecoder<CCSPlayer_ViewModelServices> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        if (field.VarName == "m_hViewModel")
        {
            var fixedArraySize = field.VarType.ArrayLength;
            var decoder = FieldDecode.CreateDecoder_CHandle<CBaseViewModel, CsDemoParser>(field.FieldEncodingInfo);
            return (CCSPlayer_ViewModelServices @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                if (@this.ViewModel.Length == 0) @this.ViewModel = new CHandle<CBaseViewModel, CsDemoParser>[fixedArraySize];
                @this.ViewModel[path[1]] = decoder(ref buffer);
            };
        }
        return CPlayer_ViewModelServices.CreateFieldDecoder(field, decoderSet);
    }
}
