using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
namespace StdLib
{
    /// <summary>
    /// Observable value that can be watched for changes
    /// </summary>
    public class Observable<T> where T : class
    {
        /// <summary>
        /// The value
        /// </summary>
        private T _value;
        /// <summary>
        /// The observers
        /// </summary>
        private readonly List<Action<T, T>> _observers = new();
        /// <summary>
        /// The temporal
        /// </summary>
        private readonly Temporal<T>? _temporal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observable{T}"/> class
        /// </summary>
        /// <param name="initialValue">The initial value</param>
        /// <param name="trackHistory">The track history</param>
        public Observable(T initialValue, bool trackHistory = false)
        {
            _value = initialValue;
            if (trackHistory && typeof(T).IsClass)
            {
                _temporal = new Temporal<T>(_value as dynamic);
            }
        }

        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
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

        /// <summary>
        /// Subscribes the observer
        /// </summary>
        /// <param name="observer">The observer</param>
        public void Subscribe(Action<T, T> observer)
        {
            _observers.Add(observer);
        }

        /// <summary>
        /// Subscribes with single parameter (new value only)
        /// </summary>
        /// <param name="observer">The observer</param>
        public void Subscribe(Action<T> observer)
        {
            _observers.Add((oldVal, newVal) => observer(newVal));
        }

        /// <summary>
        /// Adds a new value (alias for Value setter for method chaining)
        /// </summary>
        /// <param name="value">The value to add</param>
        public void Add(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Emits a value (alias for Value setter)
        /// </summary>
        /// <param name="value">The value to emit</param>
        public void Emit(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the seconds ago using the specified seconds
        /// </summary>
        /// <param name="seconds">The seconds</param>
        /// <returns>The</returns>
        public T? GetSecondsAgo(double seconds) => _temporal?.GetSecondsAgo(seconds);
        /// <summary>
        /// Gets the minutes ago using the specified minutes
        /// </summary>
        /// <param name="minutes">The minutes</param>
        /// <returns>The</returns>
        public T? GetMinutesAgo(double minutes) => _temporal?.GetMinutesAgo(minutes);
    }

    /// <summary>
    /// Event system with temporal tracking
    /// </summary>
    public class EventStream<T>
    {
        /// <summary>
        /// The events
        /// </summary>
        private readonly ConcurrentQueue<TimestampedEvent<T>> _events = new();
        /// <summary>
        /// The subscribers
        /// </summary>
        private readonly List<Action<T>> _subscribers = new();
        /// <summary>
        /// The max events
        /// </summary>
        private readonly int _maxEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStream{T}"/> class
        /// </summary>
        /// <param name="maxEvents">The max events</param>
        public EventStream(int maxEvents = 1000)
        {
            _maxEvents = maxEvents;
        }

        /// <summary>
        /// Emits the event data
        /// </summary>
        /// <param name="eventData">The event data</param>
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

        /// <summary>
        /// Subscribes the handler
        /// </summary>
        /// <param name="handler">The handler</param>
        public void Subscribe(Action<T> handler)
        {
            _subscribers.Add(handler);
        }

        /// <summary>
        /// Gets the events in last seconds using the specified seconds
        /// </summary>
        /// <param name="seconds">The seconds</param>
        /// <returns>An enumerable of t</returns>
        public IEnumerable<T> GetEventsInLastSeconds(double seconds)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-seconds);
            return _events.Where(e => e.Timestamp >= cutoff).Select(e => e.Data);
        }

        /// <summary>
        /// Gets the events in last minutes using the specified minutes
        /// </summary>
        /// <param name="minutes">The minutes</param>
        /// <returns>An enumerable of t</returns>
        public IEnumerable<T> GetEventsInLastMinutes(double minutes)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return _events.Where(e => e.Timestamp >= cutoff).Select(e => e.Data);
        }

        /// <summary>
        /// Counts the events in last seconds using the specified seconds
        /// </summary>
        /// <param name="seconds">The seconds</param>
        /// <returns>The int</returns>
        public int CountEventsInLastSeconds(double seconds)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-seconds);
            return _events.Count(e => e.Timestamp >= cutoff);
        }
    }

    /// <summary>
    /// The timestamped event class
    /// </summary>
    public class TimestampedEvent<T>
    {
        /// <summary>
        /// Gets the value of the timestamp
        /// </summary>
        public DateTime Timestamp { get; }
        /// <summary>
        /// Gets the value of the data
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampedEvent{T}"/> class
        /// </summary>
        /// <param name="data">The data</param>
        public TimestampedEvent(T data)
        {
            Data = data;
            Timestamp = DateTime.UtcNow;
        }
    }
}
