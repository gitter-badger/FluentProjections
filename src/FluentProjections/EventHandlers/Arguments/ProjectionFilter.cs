﻿using System;
using System.Reflection;

namespace FluentProjections.EventHandlers.Arguments
{
    public class ProjectionFilter<TEvent>
    {
        private readonly Func<TEvent, object> _getValue;
        private readonly PropertyInfo _property;

        public ProjectionFilter(PropertyInfo property, Func<TEvent, object> getValue)
        {
            _property = property;
            _getValue = getValue;
        }

        public FluentProjectionFilterValue GetValue(TEvent @event)
        {
            return new FluentProjectionFilterValue(_property, _getValue(@event));
        }
    }
}