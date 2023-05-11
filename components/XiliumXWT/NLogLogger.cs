using System;
using System.Diagnostics;
using NLog;

namespace XiliumXWT
{
    public interface ILogInitializer
    {
        Logger CreateLogger();
    }

    public class NLogLogger : ILogger
    {
        private readonly ILogInitializer _logInitializer;
        private readonly object _syncObject = new object();
        private volatile Logger _log;

        public NLogLogger(ILogInitializer logInitializer)
        {
            _logInitializer = logInitializer;
        }

        public NLogLogger()
            : this(new CurrentLogInitilizer())
        {
        }

        public NLogLogger(Logger log)
        {
            _log = log;
            _logInitializer = null;
        }

        public NLogLogger(string loggerName)
            : this(new NamedLogInitilizer(loggerName))
        {
        }

        public NLogLogger(Type type)
            : this(new TypeLogInitilizer(type))
        {
        }

        private Logger Log
        {
            get
            {
                lock (_syncObject)
                {
                    if (_log == null)
                        _log = _logInitializer.CreateLogger();

                    return _log;
                }
            }
        }

        public void TraceException(string message, Exception exception)
        {
            Log.Trace(exception,message);
        }

        public void Trace(string message, params object[] args)
        {
            Log.Trace(message, args);
        }

        public void DebugException(string message, Exception exception)
        {
            Log.Debug(exception,message);
        }

        public void Debug(string message, params object[] args)
        {
            Log.Debug(message, args);
        }

        public void ErrorException(string message, Exception exception)
        {
            Log.Error(exception,message);
        }

        public void Error(string message, params object[] args)
        {
            Log.Error(message, args);
        }

        public void FatalException(string message, Exception exception)
        {
            Log.Fatal(exception, message);
        }

        public void Fatal(string message, params object[] args)
        {
            Log.Fatal(message, args);
        }

        public void InfoException(string message, Exception exception)
        {
            Log.Info(exception,message);
        }

        public void Info(string message, params object[] args)
        {
            Log.Info(message, args);
        }

        public void WarnException(string message, Exception exception)
        {
            Log.Warn(exception,message);
        }

        public void Warn(string message, params object[] args)
        {
            Log.Warn(message, args);
        }

        public bool IsTraceEnabled => Log.IsTraceEnabled;

        public bool IsDebugEnabled => Log.IsDebugEnabled;

        public bool IsErrorEnabled => Log.IsErrorEnabled;

        public bool IsFatalEnabled => Log.IsFatalEnabled;

        public bool IsInfoEnabled => Log.IsInfoEnabled;

        public bool IsWarnEnabled => Log.IsWarnEnabled;

        private static Logger GetCurrentLogger()
        {
            string loggerName;
            Type declaringType;
            var framesToSkip = 1;
            do
            {
                var frame = new StackFrame(framesToSkip, false);
                var method = frame.GetMethod();
                declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    loggerName = method.Name;
                    break;
                }

                framesToSkip++;
                loggerName = declaringType.FullName;
            } while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

            return LogManager.GetLogger(loggerName);
        }

        public static ILogger GetLogger()
        {
            return new NLogLogger(GetCurrentLogger());
        }

        private class CurrentLogInitilizer : ILogInitializer
        {
            public Logger CreateLogger()
            {
                return GetCurrentLogger();
            }
        }

        private class NamedLogInitilizer : ILogInitializer
        {
            private readonly string _loggerName;

            internal NamedLogInitilizer(string loggerName)
            {
                _loggerName = loggerName;
            }

            public Logger CreateLogger()
            {
                return LogManager.GetLogger(_loggerName);
            }
        }

        private class TypeLogInitilizer : ILogInitializer
        {
            private readonly Type _type;

            public TypeLogInitilizer(Type type)
            {
                _type = type;
            }

            public Logger CreateLogger()
            {
                return LogManager.GetCurrentClassLogger(_type);
            }
        }
    }
}