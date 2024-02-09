using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DemoFile.Extensions;
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

        public EntityEventRegistration<T> AddChangeCallback<TState>(
            Func<T, TState> read,
            Action<T, TState, TState> onChange,
            IEqualityComparer<TState>? equalityComparer = null)
        {
            equalityComparer ??= EqualityComparer<TState>.Default;

            var oldValues = new Queue<(T Entity, TState State)>();

            Action<T> preFunc = entity => oldValues.Enqueue((entity, read(entity)));
            Action<T> postFunc = entity =>
            {
                // Invariant: postFunc will be called in the same order that preFunc was
                var (queuedEnt, oldValue) = oldValues.Dequeue();
                Debug.Assert(queuedEnt.EntityIndex == entity.EntityIndex && queuedEnt.SerialNumber == entity.SerialNumber);

                var newValue = read(entity);
                if (!equalityComparer.Equals(oldValue, newValue))
                {
                    onChange(entity, oldValue, newValue);
                }
            };

            PreUpdate += preFunc;
            PostUpdate += postFunc;

            return new EntityEventRegistration<T>(preFunc, postFunc);
        }

        public EntityEventRegistration<T> AddCollectionChangeCallback<TState>(
            Func<T, IEnumerable<TState>> read,
            Action<T, IReadOnlyList<TState>, IReadOnlyList<TState>> onChange)
        {
            var oldValues = new Queue<(T Entity, ImmutableList<TState> State)>();

            Action<T> preFunc = entity => oldValues.Enqueue((entity, read(entity).ToImmutableList()));
            Action<T> postFunc = entity =>
            {
                // Invariant: postFunc will be called in the same order that preFunc was
                var (queuedEnt, oldValue) = oldValues.Dequeue();
                Debug.Assert(queuedEnt.EntityIndex == entity.EntityIndex && queuedEnt.SerialNumber == entity.SerialNumber);

                var newValue = read(entity).ToImmutableList();
                if (!StructuralEqualityComparer<TState>.Instance.Equals(oldValue, newValue))
                {
                    onChange(entity, oldValue, newValue);
                }
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
}
