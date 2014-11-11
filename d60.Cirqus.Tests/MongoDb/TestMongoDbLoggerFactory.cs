﻿using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Logging;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.MongoDb.Logging;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.Stubs;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MongoDb
{
    public class TestMongoDbLoggerFactory : FixtureBase
    {
        MongoDatabase _database;
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();
        readonly DefaultCommandMapper _commandMapper = new DefaultCommandMapper();

        protected override void DoSetUp()
        {
            _database = MongoHelper.InitializeTestDatabase();
        }

        [Test]
        public void DoStuff()
        {
            CirqusLoggerFactory.Current = new MongoDbLoggerFactory(_database, "logs");

            var eventStore = new MongoDbEventStore(_database, "events");
            var aggregateRootRepository = new DefaultAggregateRootRepository(eventStore, _domainEventSerializer);
            var commandProcessor = new CommandProcessor(eventStore, aggregateRootRepository, new ConsoleOutEventDispatcher(),
                _domainEventSerializer, _commandMapper);
            
            RegisterForDisposal(commandProcessor);
            
            commandProcessor.Initialize();

            var logStatements = _database.GetCollection("logs").FindAll().ToList();

            Console.WriteLine("---------------------------------------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, logStatements.Select(s => s["text"])));
            Console.WriteLine("---------------------------------------------------------------------------------------");
        }
    }
}