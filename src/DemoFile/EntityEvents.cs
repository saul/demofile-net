using System.Runtime.InteropServices;
using DemoFile.Sdk;

namespace DemoFile;

public struct EntityEventRegistration<T>
{
    internal Action<T> PreFunc { get; }
    internal Action<T> PostFunc { get; }

    internal EntityEventRegistration(Action<T> preFunc, Action<T> postFunc)
    {
        PreFunc = preFunc;
        PostFunc = postFunc;
    }
}

[StructLayout(LayoutKind.Auto)]
public partial struct EntityEvents
{
    public struct Events<T> where T : CEntityInstance
    {
        public Action<T>? Create;
        public Action<T>? Delete;
        public Action<T>? PreUpdate;
        public Action<T>? PostUpdate;

        public EntityEventRegistration<T> AddChangeCallback<TState>(Func<T, TState> read, Action<T, TState, TState> onChange)
            where TState : struct, IEquatable<TState>
        {
            TState oldValue = default!;

            Action<T> preFunc = entity => oldValue = read(entity);
            Action<T> postFunc = entity =>
            {
                var newValue = read(entity);
                if (!oldValue.Equals(newValue))
                {
                    onChange(entity, oldValue, newValue);
                }

                oldValue = default!;
            };

            PreUpdate += preFunc;
            PostUpdate += postFunc;

            return new EntityEventRegistration<T>(preFunc, postFunc);
        }

        public void RemoveChangeCallback(EntityEventRegistration<T> registration)
        {
            PreUpdate -= registration.PreFunc;
            PostUpdate -= registration.PostFunc;
        }
    }

    public Events<CEntityInstance> CEntityInstance;
}
