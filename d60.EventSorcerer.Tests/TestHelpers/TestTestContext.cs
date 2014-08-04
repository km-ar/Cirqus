﻿using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;
using NUnit.Framework;
using TestContext = d60.EventSorcerer.TestHelpers.TestContext;

namespace d60.EventSorcerer.Tests.TestHelpers
{
    [TestFixture]
    public class TestTestContext : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = new TestContext();
        }

        [Test]
        public void CanDispatchToViews()
        {
            var viewMan = new SillyViewManager();
            _context.AddViewManager(viewMan);
            var aggregateRootId = Guid.NewGuid();

            _context.Save(aggregateRootId, new AnEvent());
            _context.Commit();

            Assert.That(viewMan.ReceivedDomainEvents.Count, Is.EqualTo(1));
        }

        class SillyViewManager : IViewManager
        {
            public SillyViewManager()
            {
                ReceivedDomainEvents = new List<DomainEvent>();
            }
            public List<DomainEvent> ReceivedDomainEvents { get; set; }
            public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
            {
                ReceivedDomainEvents.AddRange(eventStore.Stream().ToList());
            }

            public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
            {
                ReceivedDomainEvents.AddRange(events);
            }
        }


        [Test]
        public void HydratesEntitiesWithExistingEvents()
        {
            // arrange
            var rootId = Guid.NewGuid();

            _context.Save(rootId, new AnEvent());
            _context.Save(rootId, new AnEvent());
            _context.Save(rootId, new AnEvent());
            
            // act
            var firstInstance = _context.Get<AnAggregate>(rootId);

            // assert
            Assert.That(firstInstance.ProcessedEvents, Is.EqualTo(3));
        }

        [Test]
        public void EmittedEventsAreCollectedInUnitOfWork()
        {
            // arrange
            var rootId = Guid.NewGuid();
            var root = _context.Get<AnAggregate>(rootId);

            // act
            root.DoStuff();

            // assert
            Assert.That(_context.UnitOfWork.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
            Assert.That(_context.UnitOfWork.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo(rootId));
        }

        [Test]
        public void CommittedEventsBecomeTheHistory()
        {
            // arrange
            var rootId = Guid.NewGuid();
            var root = _context.Get<AnAggregate>(rootId);
            root.DoStuff();

            // act
            _context.Commit();

            // assert
            Assert.That(_context.UnitOfWork.Count(), Is.EqualTo(0));
            Assert.That(_context.History.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
            Assert.That(_context.History.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo(rootId));
        }
    }

    public class AnAggregate : AggregateRoot, IEmit<AnEvent>
    {
        public int ProcessedEvents { get; set; }
        public void Apply(AnEvent e)
        {
            ProcessedEvents++;
        }

        public void DoStuff()
        {
            Emit(new AnEvent());
        }
    }

    public class AnEvent : DomainEvent<AnAggregate>
    {
        
    }

    public class AnotherAggregate : AggregateRoot, IEmit<AnotherEvent>
    {
        public void Apply(AnotherEvent e)
        {
            
        }
    }

    public class AnotherEvent : DomainEvent<AnotherAggregate>
    {

    }
}