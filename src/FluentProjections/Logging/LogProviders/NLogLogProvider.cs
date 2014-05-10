﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentProjections.Logging.LogProviders
{
    public class NLogLogProvider : ILogProvider
    {
        private static bool _providerIsAvailableOverride = true;
        private readonly Func<string, object> _getLoggerByNameDelegate;

        public NLogLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("NLog.LogManager not found");
            }
            _getLoggerByNameDelegate = GetGetLoggerMethodCall();
        }

        public static bool ProviderIsAvailableOverride
        {
            get { return _providerIsAvailableOverride; }
            set { _providerIsAvailableOverride = value; }
        }

        public ILog GetLogger(string name)
        {
            return new NLogLogger(_getLoggerByNameDelegate(name));
        }

        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        private static Type GetLogManagerType()
        {
            return Type.GetType("NLog.LogManager, nlog");
        }

        private static Func<string, object> GetGetLoggerMethodCall()
        {
            Type logManagerType = GetLogManagerType();
            MethodInfo method = logManagerType.GetMethod("GetLogger", new[] {typeof (string)});
            ParameterExpression resultValue;
            ParameterExpression keyParam = Expression.Parameter(typeof (string), "key");
            MethodCallExpression methodCall = Expression.Call(null, method, new Expression[] {resultValue = keyParam});
            return Expression.Lambda<Func<string, object>>(methodCall, new[] {resultValue}).Compile();
        }

        public class NLogLogger : ILog
        {
            private static readonly Type LoggerType = Type.GetType("NLog.Logger, NLog");
            private static readonly Func<object, bool> IsTraceEnabledDelegate;
            private static readonly Action<object, string> TraceDelegate;
            private static readonly Action<object, string, Exception> TraceExceptionDelegate;

            private static readonly Func<object, bool> IsDebugEnabledDelegate;
            private static readonly Action<object, string> DebugDelegate;
            private static readonly Action<object, string, Exception> DebugExceptionDelegate;

            private static readonly Func<object, bool> IsInfoEnabledDelegate;
            private static readonly Action<object, string> InfoDelegate;
            private static readonly Action<object, string, Exception> InfoExceptionDelegate;

            private static readonly Func<object, bool> IsWarnEnabledDelegate;
            private static readonly Action<object, string> WarnDelegate;
            private static readonly Action<object, string, Exception> WarnExceptionDelegate;

            private static readonly Func<object, bool> IsErrorEnabledDelegate;
            private static readonly Action<object, string> ErrorDelegate;
            private static readonly Action<object, string, Exception> ErrorExceptionDelegate;

            private static readonly Func<object, bool> IsFatalEnabledDelegate;
            private static readonly Action<object, string> FatalDelegate;
            private static readonly Action<object, string, Exception> FatalExceptionDelegate;
            private readonly object _logger;

            static NLogLogger()
            {
                IsTraceEnabledDelegate = GetPropertyGetter("IsTraceEnabled");
                TraceDelegate = GetMethodCallForMessage("Trace");
                TraceExceptionDelegate = GetMethodCallForMessageException("TraceException");

                IsDebugEnabledDelegate = GetPropertyGetter("IsDebugEnabled");
                DebugDelegate = GetMethodCallForMessage("Debug");
                DebugExceptionDelegate = GetMethodCallForMessageException("DebugException");

                IsInfoEnabledDelegate = GetPropertyGetter("IsInfoEnabled");
                InfoDelegate = GetMethodCallForMessage("Info");
                InfoExceptionDelegate = GetMethodCallForMessageException("InfoException");

                IsErrorEnabledDelegate = GetPropertyGetter("IsErrorEnabled");
                ErrorDelegate = GetMethodCallForMessage("Error");
                ErrorExceptionDelegate = GetMethodCallForMessageException("ErrorException");

                IsWarnEnabledDelegate = GetPropertyGetter("IsWarnEnabled");
                WarnDelegate = GetMethodCallForMessage("Warn");
                WarnExceptionDelegate = GetMethodCallForMessageException("WarnException");

                IsFatalEnabledDelegate = GetPropertyGetter("IsFatalEnabled");
                FatalDelegate = GetMethodCallForMessage("Fatal");
                FatalExceptionDelegate = GetMethodCallForMessageException("FatalException");
            }

            public NLogLogger(object logger)
            {
                _logger = logger;
            }

            public void Log(LogLevel logLevel, Func<string> messageFunc)
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        if (IsDebugEnabledDelegate(_logger))
                        {
                            DebugDelegate(_logger, messageFunc());
                        }
                        break;
                    case LogLevel.Info:
                        if (IsInfoEnabledDelegate(_logger))
                        {
                            InfoDelegate(_logger, messageFunc());
                        }
                        break;
                    case LogLevel.Warn:
                        if (IsWarnEnabledDelegate(_logger))
                        {
                            WarnDelegate(_logger, messageFunc());
                        }
                        break;
                    case LogLevel.Error:
                        if (IsErrorEnabledDelegate(_logger))
                        {
                            ErrorDelegate(_logger, messageFunc());
                        }
                        break;
                    case LogLevel.Fatal:
                        if (IsFatalEnabledDelegate(_logger))
                        {
                            FatalDelegate(_logger, messageFunc());
                        }
                        break;
                    default:
                        if (IsTraceEnabledDelegate(_logger))
                        {
                            TraceDelegate(_logger, messageFunc());
                        }
                        break;
                }
            }

            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
                where TException : Exception
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        if (IsDebugEnabledDelegate(_logger))
                        {
                            DebugExceptionDelegate(_logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Info:
                        if (IsInfoEnabledDelegate(_logger))
                        {
                            InfoExceptionDelegate(_logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Warn:
                        if (IsWarnEnabledDelegate(_logger))
                        {
                            WarnExceptionDelegate(_logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Error:
                        if (IsErrorEnabledDelegate(_logger))
                        {
                            ErrorExceptionDelegate(_logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Fatal:
                        if (IsFatalEnabledDelegate(_logger))
                        {
                            FatalExceptionDelegate(_logger, messageFunc(), exception);
                        }
                        break;
                    default:
                        if (IsTraceEnabledDelegate(_logger))
                        {
                            TraceExceptionDelegate(_logger, messageFunc(), exception);
                        }
                        break;
                }
            }

            private static Func<object, bool> GetPropertyGetter(string propertyName)
            {
                ParameterExpression funcParam = Expression.Parameter(typeof (object), "l");
                Expression convertedParam = Expression.Convert(funcParam, LoggerType);
                Expression property = Expression.Property(convertedParam, propertyName);
                return (Func<object, bool>) Expression.Lambda(property, funcParam).Compile();
            }

            private static Action<object, string> GetMethodCallForMessage(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof (object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof (string), "o");
                Expression convertedParam = Expression.Convert(loggerParam, LoggerType);
                MethodCallExpression methodCall = Expression.Call(convertedParam,
                    LoggerType.GetMethod(methodName, new[] {typeof (object)}),
                    messageParam);
                return (Action<object, string>) Expression.Lambda(methodCall, new[] {loggerParam, messageParam}).Compile();
            }

            private static Action<object, string, Exception> GetMethodCallForMessageException(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof (object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof (string), "o");
                ParameterExpression exceptionParam = Expression.Parameter(typeof (Exception), "e");
                Expression convertedParam = Expression.Convert(loggerParam, LoggerType);
                MethodInfo method = LoggerType.GetMethod(methodName, new[] {typeof (string), typeof (Exception)});
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, messageParam, exceptionParam);
                return (Action<object, string, Exception>) Expression.Lambda(methodCall,
                    new[] {loggerParam, messageParam, exceptionParam}).Compile();
            }
        }
    }
}