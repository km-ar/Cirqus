﻿using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Tests.Stubs;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Aggregates
{
    [TestFixture]
    public class TestLoading : FixtureBase
    {
        [Test]
        public void DefaultsToThrowingIfLoadedAggregateRootCannotBeFound()
        {
            var someRoot = new BeetRoot {AggregateRootRepository = new InMemoryAggregateRootRepository()};

            Assert.Throws<ArgumentException>(someRoot.LoadOtherBeetRootWithDefaultBehavior);
        }

        [Test]
        public void CanBeToldToIgnoreNonExistenceOfOtherAggregateRoot()
        {
            var someRoot = new BeetRoot {AggregateRootRepository = new InMemoryAggregateRootRepository()};

            Assert.DoesNotThrow(someRoot.LoadOtherBeetRootButOverrideBehavior);
        }


        class BeetRoot : AggregateRoot
        {
            public void LoadOtherBeetRootWithDefaultBehavior()
            {
                Load<BeetRoot>(Guid.NewGuid());
            }
            public void LoadOtherBeetRootButOverrideBehavior()
            {
                Load<BeetRoot>(Guid.NewGuid(), createIfNotExists: true);
            }
        }
    }
}