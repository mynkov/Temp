using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication21
{
    public class Program
    {
        public static void Main()
        {
            // На IoC
            //var eventManager = new EventManager()
            //    .Register(container.GetInstance<UserStateEventHandler>);
            //    .Register(container.GetInstance<UserStateEventHandler1>);

            var eventManager = new EventManager()
                .Register(() => new EventHandler());

            Test(eventManager);
        }

        public static void Test(IEventManager eventManager)
        {
            eventManager.RaiseEvent(new TestEvent1("Message1"));
            eventManager.RaiseEvent(new TestEvent2("Message2"));
        }
    }

    public class EventHandler :
        IEventHandler<TestEvent1>,
        IEventHandler<TestEvent2>
    {
        public void Handle(TestEvent1 @event)
        {
            Console.WriteLine("{0}: '{1}'", @event.GetType().Name, @event.Message);
        }

        public void Handle(TestEvent2 @event)
        {
            Console.WriteLine("{0}: '{1}'", @event.GetType().Name, @event.Message);
        }
    }

    /// <summary>
    /// Базовый интерфейс для обработчиков событий.
    /// </summary>
    public interface IEventHandler
    {
    }

    /// <summary>
    /// Интерфейс для обработчиков событий.
    /// </summary>
    public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : Event
    {
        void Handle(TEvent @event);
    }
     
   

    public class TestEvent2 : Event
    {
        public string Message { get; private set; }

        public TestEvent2(string message)
        {
            Message = message;
        }
    }

    public class TestEvent1 : Event
    {
        public string Message { get; private set; }

        public TestEvent1(string message)
        {
            Message = message;
        }
    }

    public abstract class Event
    {
    }

    public interface IEventManager
    {
        void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event;
    }


    /// <summary>
    /// Класс для управления событиями.
    /// </summary>
    public class EventManager : IEventManager
    {
        /// <summary>
        /// Класс предоставляющий обработчиков событий.
        /// </summary>
        private readonly HandlerProvider _handlerProvider = new HandlerProvider();

        /// <summary>
        /// Зарегистрировать обработчик события.
        /// </summary>
        public EventManager Register<THandler>(Func<THandler> factory)
            where THandler : IEventHandler
        {
            _handlerProvider.Register(factory);
            return this;
        }

        /// <summary>
        /// Сгенерировать событие.
        /// </summary>
        public void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event
        {
            foreach (dynamic handler in _handlerProvider.GetHandlers(typeof(TEvent)))
            {
                handler.Handle(@event);
            }
        }
    }

    /// <summary>
    /// Класс предоставляющий обработчиков событий.
    /// </summary>
    internal class HandlerProvider
    {
        /// <summary>
        /// Зарегистрированные фабрики для создания обработчиков.
        /// </summary>
        private readonly Dictionary<Type, Func<object>> _registeredHandlerFactories = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// Маппинг между типами событий и их типами обработчиков.
        /// </summary>
        private readonly ConcurrentDictionary<Type, List<Type>> _handlerTypes = new ConcurrentDictionary<Type, List<Type>>();      

        /// <summary>
        /// Зарегистрировать обработчик события.
        /// </summary>
        public void Register<THandler>(Func<THandler> factory)
            where THandler : IEventHandler
        {
            _registeredHandlerFactories.Add(typeof(THandler), () => factory());
        }

        /// <summary>
        /// Получить экземпляры обработчиков для типа события.
        /// </summary>
        public List<object> GetHandlers(Type eventType)
        {
            var handlerTypes = _handlerTypes.GetOrAdd(eventType, GetHandlerTypes);
            return handlerTypes.Select(handlerType => _registeredHandlerFactories[handlerType]()).ToList();
        }

        /// <summary>
        /// Получить типы обработчиков для типа события.
        /// </summary>
        private List<Type> GetHandlerTypes(Type eventType)
        {
            var handlerInterface = typeof(IEventHandler<>).MakeGenericType(eventType);
            return _registeredHandlerFactories.Keys.Where(handlerInterface.IsAssignableFrom).ToList();
        }
    }
}