using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

[Obsolete("Removed in v14090")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public partial class CPlayer_ViewModelServices : CPlayerPawnComponent
{
    internal static SendNodeDecoder<CPlayer_ViewModelServices> CreateDowncastDecoder(SerializerKey serializerKey, DecoderSet decoderSet, out Func<CPlayer_ViewModelServices> factory)
    {
        if (serializerKey.Name == "CPlayer_ViewModelServices")
        {
            factory = () => new CPlayer_ViewModelServices();
            return decoderSet.GetDecoder<CPlayer_ViewModelServices>(serializerKey);
        }
        else if (serializerKey.Name == "CCSObserver_ViewModelServices")
        {
            factory = () => new CCSObserver_ViewModelServices();
            var childClassDecoder = decoderSet.GetDecoder<CCSObserver_ViewModelServices>(serializerKey);
            return (CPlayer_ViewModelServices instance, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                Debug.Assert(instance is CCSObserver_ViewModelServices);
                var downcastInstance = Unsafe.As<CCSObserver_ViewModelServices>(instance);
                childClassDecoder(downcastInstance, path, ref buffer);
            };
        }
        else if (serializerKey.Name == "CCSPlayer_ViewModelServices")
        {
            factory = () => new CCSPlayer_ViewModelServices();
            var childClassDecoder = decoderSet.GetDecoder<CCSPlayer_ViewModelServices>(serializerKey);
            return (CPlayer_ViewModelServices instance, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                Debug.Assert(instance is CCSPlayer_ViewModelServices);
                var downcastInstance = Unsafe.As<CCSPlayer_ViewModelServices>(instance);
                childClassDecoder(downcastInstance, path, ref buffer);
            };
        }
        throw new NotImplementedException($"Unknown derived class of CPlayer_ViewModelServices: {serializerKey}");
    }

    internal new static SendNodeDecoder<CPlayer_ViewModelServices> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        return CPlayerPawnComponent.CreateFieldDecoder(field, decoderSet);
    }
}
