using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace UHigh.StandardLibrary
{
    /// <summary>
    /// Observable value that can be watched for changes
    /// </summary>
    public class Observable<T> where T : class
    {
        private T _value;
        private readonly List<Action<T, T>> _observers = new();
        private readonly Temporal<T>? _temporal;

        public Observable(T initialValue, bool trackHistory = false)
        {
            _value = initialValue;
            if (trackHistory && typeof(T).IsClass)
            {
                _temporal = new Temporal<T>(_value as dynamic);
            }
        }

        public T Value
        {
            get => _value;
            set
            {
                var oldValue = _value;
                _value = value;
                
                // Update temporal tracking
                _temporal?.Update(_value as dynamic, "value changed");
                
                // Notify observers
                foreach (var observer in _observers)
                {
                    observer(oldValue, _value);
                }
            }
        }

        public void Subscribe(Action<T, T> observer)
        {
            _observers.Add(observer);
        }

        public void Unsubscribe(Action<T, T> observer)
        {
            _observers.Remove(observer);
        }

        public T? GetSecondsAgo(double seconds) => _temporal?.GetSecondsAgo(seconds);
        public T? GetMinutesAgo(double minutes) => _temporal?.GetMinutesAgo(minutes);
    }

    /// <summary>
    /// Event system with temporal tracking
    /// </summary>
    public class EventStream<T>
    {
        private readonly ConcurrentQueue<TimestampedEvent<T>> _events = new();
        private readonly List<Action<T>> _subscribers = new();
        private readonly int _maxEvents;

        public EventStream(int maxEvents = 1000)
        {
            _maxEvents = maxEvents;
        }

        public void Emit(T eventData)
        {
            var timestampedEvent = new TimestampedEvent<T>(eventData);
            _events.Enqueue(timestampedEvent);
            
            // Maintain size limit
            while (_events.Count > _maxEvents)
            {
                _events.TryDequeue(out _);
            }
            
            // Notify subscribers
            foreach (var subscriber in _subscribers)
            {
                subscriber(eventData);
            }
        }

        public void Subscribe(Action<T> handler)
        {
            _subscribers.Add(handler);
        }

        public IEnumerable<T> GetEventsInLastSeconds(double seconds)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-seconds);
            return _events.Where(e => e.Timestamp >= cutoff).Select(e => e.Data);
        }

        public IEnumerable<T> GetEventsInLastMinutes(double minutes)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return _events.Where(e => e.Timestamp >= cutoff).Select(e => e.Data);
        }

        public int CountEventsInLastSeconds(double seconds)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-seconds);
            return _events.Count(e => e.Timestamp >= cutoff);
        }
    }

    public class TimestampedEvent<T>
    {
        public DateTime Timestamp { get; }
        public T Data { get; }

        public TimestampedEvent(T data)
        {
            Data = data;
            Timestamp = DateTime.UtcNow;
        }
    }
}
