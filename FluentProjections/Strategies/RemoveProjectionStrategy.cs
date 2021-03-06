﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentProjections.Logging;
using FluentProjections.Logging.Generic;
using FluentProjections.Persistence;
using FluentProjections.Strategies.Arguments;

namespace FluentProjections.Strategies
{
    public class RemoveProjectionStrategy<TMessage, TProjection> : IMessageHandlingStrategy<TMessage>
        where TProjection : class
    {
        private static readonly ILog<TMessage, TProjection> Logger =
            LogProvider<TMessage, TProjection>.GetLogger(typeof (RemoveProjectionStrategy<TMessage, TProjection>));

        private readonly Filters<TMessage> _filters;

        public RemoveProjectionStrategy(Filters<TMessage> filters)
        {
            _filters = filters;
        }

        public virtual void Handle(TMessage message, IProvideProjections store)
        {
            Logger.DebugFormat("Remove projections because of a message: {0}", message);
            var filterValues = GetFilterValues(message);
            Remove(store, filterValues);
        }

        private IEnumerable<FilterValue> GetFilterValues(TMessage message)
        {
            Logger.Debug("Get filter values from a message.");
            try
            {
                return _filters.GetValues(message).ToList();
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to get filter values.", e);
                throw;
            }
        }

        private static void Remove(IProvideProjections store, IEnumerable<FilterValue> filterValues)
        {
            Logger.Debug("Remove projections.");
            try
            {
                store.Remove<TProjection>(filterValues);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to remove projections.", e);
                throw;
            }
        }
    }
}