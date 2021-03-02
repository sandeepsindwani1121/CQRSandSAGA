using Microsoft.EntityFrameworkCore;
using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using EasyNetQ;
namespace CQRSConsole
{
    class Program
    {
        static public IKernel kernal = new StandardKernel();
        static public MapperConfiguration MapperConfiguration = null;
        static public MapperConfiguration MapperConfiguration1 = null;
        static void Main(string[] args)
        {
            MapperConfiguration = new MapperConfiguration(x =>
              {
                  x.CreateMap<customer, CreateCustomerCommand>();
              });
            MapperConfiguration1 = new MapperConfiguration(x =>
            {
                x.CreateMap<CreateCustomerCommand, CustomerCreatedEvent>();
            });

            kernal.Load(Assembly.GetExecutingAssembly());
            //Console.WriteLine("Hello World!");
            CreateCustomerCommand createCustomer = new CreateCustomerCommand();
            createCustomer.Name = "Shiv";
            IDispatcher dispatcher = new CreateCustomerDispatcher();
            dispatcher.send<CreateCustomerCommand>(createCustomer);

            createCustomer = new CreateCustomerCommand();
            createCustomer.Name = "Shiva";
            dispatcher.send<CreateCustomerCommand>(createCustomer);

            foreach (var item in dispatcher._eventPublisher.Getevents())
            {
                Console.WriteLine(item.Guid);
            }

        }
    }

    public class Bindings : NinjectModule
    {
        public override void Load()
        {
            Bind(typeof(ICommandHandler<CreateCustomerCommand>)).To(typeof(CreateCustomerHandler));
            Bind(typeof(IEventHandler<CustomerCreatedEvent>)).To(typeof(CustomerCreatedEventHandler));
        }
    }

    public class customer
    {
        public int id { get; set; }
        public string Name { get; set; }
    }

    public interface IRepositery<T>
    {
        void Add(T obj);
        void update(T obj);
        List<T> Query();
    }

    public abstract class EfCommon<T> : DbContext, IRepositery<T> where T : class
    {
        public void Add(T obj)
        {
            Set<T>().Add(obj);
        }

        public List<T> Query()
        {
            return Set<T>().ToList();
        }

        public void update(T obj)
        {
            Set<T>().Update(obj);
        }
    }

    public class EfCustomerContext : EfCommon<customer>
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<customer>().ToTable("tblCustomer");
        }
    }



    public class CreateCustomerCommand : customer, ICommand
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
        public IEventBus _eventPublisher { get; set; }
        public Guid Guid { get; set; }
        public CreateCustomerDispatcher()
        {
            this.Guid = Guid.NewGuid();
            this._eventPublisher = new EventBus();
        }

        public void send<T>(T command) where T : ICommand
        {
            var handler = Program.kernal.Get<ICommandHandler<T>>();
            handler.handle(command);

            ////Repostery pattern implement here
            //IRepositery<customer> repositery = new EfCustomerContext();
            //var mapper = new Mapper(Program.MapperConfiguration);
            //customer x = mapper.Map<customer>(command);
            //repositery.Add(x);

            _eventPublisher.Publish(this.Guid, new CustomerCreatedEvent(this.Guid.ToString()));





        }
    }
    public class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand>
    {
        public void handle(CreateCustomerCommand command)
        {
            Console.WriteLine("insert records in DB");
            //Event to rabbit Queue and from there other microservice read.
        }
    }

    public class CustomerCreatedEvent : IEvent
    {
        public string Guid { get; set; }

        public CustomerCreatedEvent(string _guid)
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

    public class CustomerCreatedEventHandler : IEventHandler<CustomerCreatedEvent>
    {
        public void Handle(CustomerCreatedEvent @event)
        {
            var bus = EasyNetQ.RabbitHutch.CreateBus("host=localhost");
            bus.PubSub.Publish<CustomerCreatedEvent>(@event);
            System.Console.WriteLine($"User was created {@event.Guid} event");
        }
    }
}

    
