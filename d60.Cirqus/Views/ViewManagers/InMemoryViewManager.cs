﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Views.ViewManagers
{
    public class InMemoryViewManager<TViewInstance> : IViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly ConcurrentDictionary<string, TViewInstance> _views = new ConcurrentDictionary<string, TViewInstance>();
        readonly ViewDispatcherHelper<TViewInstance> _dispatcher = new ViewDispatcherHelper<TViewInstance>();
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();

        long _position = -1;

        public TViewInstance Load(string viewId)
        {
            TViewInstance instance;

            return _views.TryGetValue(viewId, out instance)
                ? instance
                : null;
        }

        public long GetPosition(bool canGetFromCache = true)
        {
            return InnerGetPosition();
        }

        public void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            foreach (var e in batch)
            {
                if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                var affectedViewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                foreach (var viewId in affectedViewIds)
                {
                    var viewInstance = _views.GetOrAdd(viewId, id => _dispatcher.CreateNewInstance(id));

                    _dispatcher.DispatchToView(viewContext, e, viewInstance);
                }

                Interlocked.Exchange(ref _position, e.GetGlobalSequenceNumber());
            }
        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GetNewPosition();

            var stopwatch = Stopwatch.StartNew();

            while (InnerGetPosition() < result.GetNewPosition())
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(string.Format("View for {0} did not catch up to {1} within {2} timeout!",
                        typeof(TViewInstance), mostRecentGlobalSequenceNumber, timeout));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        long InnerGetPosition()
        {
            return Interlocked.Read(ref _position);
        }

        public void Purge()
        {
            _views.Clear();
        }
    }
}