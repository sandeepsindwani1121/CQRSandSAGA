using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CQRSConsole
{
    class Program
    {
        static public IKernel kernal = new StandardKernel();
       
        static void Main(string[] args)
        {
            kernal.Load(Assembly.GetExecutingAssembly());
            //Console.WriteLine("Hello World!");
            CreateCustomer createCustomer = new CreateCustomer();
            createCustomer.Name = "Shiv";
            IDispatcher dispatcher = new CreateCustomerDispatcher();
            dispatcher.send<CreateCustomer>(createCustomer);

            createCustomer = new CreateCustomer();
            createCustomer.Name = "Shiva";
            dispatcher.send<CreateCustomer>(createCustomer);

            foreach(var item in dispatcher._eventPublisher.Getevents())
            {
                Console.WriteLine(((CustomerCreated)item).Guid);
            }

        }
     }

    public class Bindings : NinjectModule
    {
        public override void Load()
        {
            Bind(typeof(ICommandHandler<CreateCustomer>)).To(typeof(CreateCustomerHandler));
            Bind(typeof(IEventHandler<CustomerCreated>)).To(typeof(CustomerCreatedEventHandler));
        }
    }
   
    public class customer 
    {
        public int id { get; set; }
        public string Name { get; set; }
    }

    public class CreateCustomer : customer, ICommand
    {
        public DateTime createDate { get; set; }
    }

    public interface ICommand
    {

    }
    public interface IDispatcher
    {
        void send<T>(T command) where T : ICommand;
        public IEventBus _eventPublisher { get; set; }

        public Guid Guid { get; set; }

    }
    public interface ICommandHandler<T> where T : ICommand
    {
        void handle(T command);
    }

    ///////////////////////////////////////////////////////////////implementation of SAGA pattern
    public interface IEvent
    {
        public string Guid { get; set; }
    }

    public interface IEventBus
    {
        void Publish<T>(Guid guid, T @event) where T : IEvent;
        List<IEvent> Getevents(Guid aggrecatedId);
        List<IEvent> Getevents();
    }

    public interface IEventStore
    {
        void SaveEvent(Guid aggrecatedId, IEvent e);
        List<IEvent> GetEvent(Guid aggrecatedId);
        List<IEvent> GetEvent();
    }
    public interface IEventHandler
    {

    }
    public interface IEventHandler<TEvent> : IEventHandler where TEvent : IEvent
    {
        void Handle(TEvent @event);
    }
    /// <summary>
    /// //////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    
    public class CreateCustomerDispatcher : IDispatcher
    {
        public CreateCustomerDispatcher()
        {
            this.Guid = Guid.NewGuid();
           this._eventPublisher = new EventBus();
        }
        public IEventBus _eventPublisher { get; set; }
        public Guid Guid { get; set; }
        public void send<T>(T command) where T : ICommand
        {
            var handler = Program.kernal.Get<ICommandHandler<T>>();
            handler.handle(command);
            _eventPublisher.Publish(this.Guid, new CustomerCreated(this.Guid.ToString()));
            
        }
    }
    public class CreateCustomerHandler : ICommandHandler<CreateCustomer>
    {
        public void handle(CreateCustomer command)
        {
            Console.WriteLine("insert records in DB");
            //Event to rabbit Queue and from there other microservice read.
        }
    }

    public class CustomerCreated : IEvent
    {
        public string Guid { get; set; }

        public CustomerCreated(string _guid)
        {
            Guid = _guid;
        }
    }

        public class EventBus : IEventBus
        {
            IEventStore eventStore = new EventStore();
            public List<IEvent> Getevents(Guid aggrecatedId)
            {
                return eventStore.GetEvent(aggrecatedId);
            }

            public List<IEvent> Getevents()
            {
                return eventStore.GetEvent();
            }

            public void Publish<T>(Guid guid, T @event) where T : IEvent
            {
                var handler = Program.kernal.Get<IEventHandler<T>>();
                handler.Handle(@event);
                this.eventStore.SaveEvent(guid, @event);
            }
        }

        public class EventStore : IEventStore
        {
            public readonly Dictionary<Guid, List<IEvent>> _eventStore = new Dictionary<Guid, List<IEvent>>();
            public List<IEvent> GetEvent(Guid aggrecatedId)
            {
                return _eventStore[aggrecatedId];
            }

            public List<IEvent> GetEvent()
            {
                return _eventStore.SelectMany(x => x.Value).ToList();
            }

            public void SaveEvent(Guid aggrecatedId, IEvent e)
            {
                List<IEvent> events = null;
                if (!_eventStore.ContainsKey(aggrecatedId))
                {
                    events = new List<IEvent>();
                    _eventStore.Add(aggrecatedId, events);
                }
                else
                {
                    events = _eventStore[aggrecatedId];
                }
                events.Add(e);
            }
        }

        public class CustomerCreatedEventHandler : IEventHandler<CustomerCreated>
        {
            public void Handle(CustomerCreated @event)
            {
                System.Console.WriteLine($"User was created {@event.Guid} event");
            }
        }
    }

    
