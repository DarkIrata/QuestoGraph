using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace QuestoGraph.Services.Events
{
    internal class EventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<object>> events = new();

        public void Subscribe<TEvent>([DisallowNull] Action<TEvent> subscriber)
            where TEvent : class, IEvent
        {
            var type = typeof(TEvent);
            if (this.events.TryGetValue(type, out var handlers))
            {
                lock (handlers)
                {
                    handlers.Add(subscriber);
                    this.LogToDebug($"Subscriber for '{type.Name}' added");
                }
            }
            else
            {
                if (this.events.TryAdd(type, [subscriber]))
                {
                    this.LogToDebug($"No subscriber for '{type.Name}'. Added '{type.Name}'");
                }
                else
                {
                    this.LogToDebug($"No subscriber for '{type.Name}'. Failed adding '{type.Name}'");
                }
            }
        }

        public void Unsubscribe<TEvent>([DisallowNull] Action<TEvent> subscriber)
            where TEvent : class, IEvent
        {
            var type = typeof(TEvent);
            lock (this.events)
            {
                if (this.events.TryGetValue(type, out var handlers))
                {
                    lock (handlers)
                    {
                        if (handlers.Remove(subscriber))
                        {
                            this.LogToDebug($"Subscriber for '{type.Name}' removed");
                        }
                        else
                        {
                            this.LogToDebug($"Subscriber for '{type.Name}' not removed. Error or not found");
                        }

                        if (handlers.Count == 0)
                        {
                            if (this.events.TryRemove(type, out _))
                            {
                                this.LogToDebug($"No more subscriber for '{type.Name}'. Removed '{type.Name}'");
                            }
                            else
                            {
                                this.LogToDebug($"No more subscriber for '{type.Name}'. Failed removing '{type.Name}'");
                            }
                        }
                    }
                }
            }
        }

        public void Publish<TEvent>(TEvent data)
            where TEvent : class, IEvent
        {
            var type = typeof(TEvent);
            if (this.events.TryGetValue(type, out var handlers))
            {
                lock (handlers)
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            ((Action<TEvent>)handler)?.Invoke(data);
                        }
                        catch (Exception ex)
                        {
                            this.LogToDebug($"Exception while invoking '{type.Name}' subscriber");
                            this.LogToDebug($"{ex.Message}");
                        }
                    }
                }
            }
        }

        public async Task PublishAsync<TEvent>(TEvent data, CancellationToken cancellationToken = default)
            where TEvent : class, IEvent
            => await Task.Run(() => this.Publish(data), cancellationToken).ConfigureAwait(false);

        private void LogToDebug(string text)
        {
#if DEBUG
            Plugin.Log.Debug($"{nameof(QuestoGraph)}[EventAggregator]{text}");
#endif
        }
    }
}
