using Newtonsoft.Json;
using NLog;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HistoricalAdapter
{
    /// <summary>
    /// Below are guidelines to use to figure out what level messages/exceptions should be logged.
    /// 
    /// FATAL: The app (or at the very least a thread) is about to die horribly. This is where the
    /// info explaining why that's happening goes.
    /// 
    /// ERROR: Something that the app's doing that it shouldn't. This isn't a user error ('invalid search query');
    /// it's an assertion failure, network problem, etc etc., probably one that is going to abort the current operation
    /// 
    /// WARN: Something that's concerning but not causing the operation to abort; # of connections in the DB pool getting low, 
    /// an unusual-but-expected timeout in an operation, etc. I often think of 'WARN' as something that's useful in aggregate; 
    /// e.g. grep, group, and count them to get a picture of what's affecting the system health
    /// 
    /// INFO: Normal logging that's part of the normal operation of the app; diagnostic stuff so you can go back and say 'how 
    /// often did this broad-level operation happen?', or 'how did the user's data get into this state?'
    /// 
    /// DEBUG: This is where you might log 
    /// detailed information about key method parameters or other information that is useful for finding likely problems in
    /// specific 'problematic' areas of the code. 
    /// </summary>
    public sealed class AppLogger
    {
        /// <summary>
        /// String formatter for function entry
        /// </summary>
        private const string FunctionEntryString = "Entry";

        /// <summary>
        /// String formatter for function exit
        /// </summary>
        private const string FunctionExitString = "Exit";

        private Logger AppLog = null;

        private AppLogger(String type)
        {
            AppLog = LogManager.GetLogger(type);
        }

        private static volatile AppLogger LoggerInstance;

        private static object syncRoot = new Object();

        public static AppLogger Instance
        {
            get
            {
                if (LoggerInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (LoggerInstance == null)
                        {
                            LoggerInstance = new AppLogger("appLogger");
                        }
                    }
                }
                return LoggerInstance;
            }
        }

        private string LogMessageFormat
        {
            get { return "|{0}|{1}|{2}|{3}"; }
        }

        /// <summary>
        /// Logs an exception at the Debug log level.
        /// DEBUG: Off by default, able to be turned on for debugging specific unexpected problems. This is where you might log 
        /// detailed information about key method parameters or other information that is useful for finding likely problems in
        /// specific 'problematic' areas of the code. 
        /// Message is assumed to be type of DTO in this case, and it will be converted using JSOn utils.
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="message">The message to be logged</param>
        public void Debug(string className, string methodName, object message)
        {
            var logMessage = LogMessageParser(className, methodName, message);
            AppLog.Debug(logMessage);
        }
        public void Debug(Type className, Exception exception, string message = "", [CallerMemberName]string methodName = null)
        {
            Debug(className.FullName, methodName, message);
        }

        public void Debug(Type className, string message = "", [CallerMemberName]string methodName = null)
        {
            Debug(className.FullName, methodName, message);
        }

        /// <summary>
        /// just to format the message so that dtos passed in show up properly without duplicate Data elements
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private string LogMessageParser(string className, string methodName, object message)
        {
            if (message == null)
            {
                message = "message passed to the log message was null";
            }

            if (message.GetType().FullName.Contains(message.ToString()))
            {
                message = " Data: " + JsonConvert.SerializeObject(message, Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            }

            string logMessage = string.Format(LogMessageFormat, className, methodName,
                message.ToString(), string.Empty); // + " Data: " + JsonConvert.SerializeObject(message));

            return logMessage;
        }

        /// <summary>
        /// Logs an exception at the Error log level.
        /// ERROR: Something that the app's doing that it shouldn't. This isn't a user error ('invalid search query');
        /// it's an assertion failure, network problem, etc etc., probably one that is going to abort the current operation
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="exception">The exception to be logged</param>
        /// <param name="message">The message to be logged</param>
        public void Error(string className, string methodName, Exception exception, string message = "")
        {
            if (exception == null)
            {
                Error(className, methodName, message);
                return;
            }
            if (message == string.Empty)
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, exception.Message, exception.StackTrace);
                AppLog.Error($"{logMessage} - {exception}");
            }
            else
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, message + " " + exception.Message, exception.StackTrace);
                AppLog.Error($"{logMessage} - {exception}");
            }
        }

        /// <summary>
        /// Logs an exception at the Error log level. 
        /// ERROR: Something that the app's doing that it shouldn't. This isn't a user error ('invalid search query');
        /// it's an assertion failure, network problem, etc etc., probably one that is going to abort the current operation
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="exception">The message to be logged</param>
        public void Error(string className, string methodName, object message)
        {
            var logMessage = LogMessageParser(className, methodName, message);
            AppLog.Error(logMessage);
        }
        public void Error(Type className, Exception exception, string message = "", [CallerMemberName]string methodName = null)
        {
            Error(className.FullName, methodName, exception, message);
        }

        public void Error(Type className, string message = "", [CallerMemberName]string methodName = null)
        {
            Error(className.FullName, methodName, message);
        }

        /// <summary>
        /// Logs an exception at the Fatal log level. 
        /// FATAL: The app (or at the very least a thread) is about to die horribly. This is where the
        /// info explaining why that's happening goes.
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="exception">The exception to be logged</param>
        /// <param name="message">The message to be logged</param>
        public void Fatal(string className, string methodName, Exception exception, string message = "")
        {
            if (exception == null)
            {
                Fatal(className, methodName, message);
                return;
            }
            if (message == string.Empty)
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, string.Empty, exception);
                AppLog.Fatal($"{logMessage} - {exception}");
            }
            else
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, message, exception);
                AppLog.Fatal($"{logMessage} - {exception}");
            }
        }

        /// <summary>
        /// Logs an exception at the Fatal log level. 
        /// FATAL: The app (or at the very least a thread) is about to die horribly. This is where the
        /// info explaining why that's happening goes.
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="message">The message to be logged</param>
        public void Fatal(string className, string methodName, object message)
        {
            var logMessage = LogMessageParser(className, methodName, message);
            AppLog.Fatal(logMessage);
        }

        public void Fatal(Type className, Exception exception, string message = "", [CallerMemberName]string methodName = null)
        {
            Fatal(className.FullName, methodName, exception, message);
        }

        public void Fatal(Type className, string message = "", [CallerMemberName]string methodName = null)
        {
            Fatal(className.FullName, methodName, message);
        }

        /// <summary>
        /// Logs an exception at the Info log level. 
        /// INFO: Normal logging that's part of the normal operation of the app; diagnostic stuff so you can go back and say 'how 
        /// often did this broad-level operation happen?', or 'how did the user's data get into this state?'
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="exception">The exception to be logged</param>
        /// <param name="message">The message to be logged</param>
        public void Info(string className, string methodName, Exception exception, string message = "")
        {
            if (exception == null)
            {
                Info(className, methodName, message);
                return;
            }
            if (message == string.Empty)
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, string.Empty, exception);
                AppLog.Info($"{logMessage} - {exception}");
            }
            else
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, message, exception);
                AppLog.Info($"{logMessage} - {exception}");
            }
        }

        /// <summary>
        /// Logs an exception at the Info log level. 
        /// INFO: Normal logging that's part of the normal operation of the app; diagnostic stuff so you can go back and say 'how 
        /// often did this broad-level operation happen?', or 'how did the user's data get into this state?'
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="message">The message to be logged</param>
        public void Info(string className, string methodName, object message)
        {
            var logMessage = LogMessageParser(className, methodName, message);
            AppLog.Info(logMessage);
        }

        public void Info(Type className, Exception exception, string message = "", [CallerMemberName]string methodName = null)
        {
            Info(className.FullName, methodName, exception, message);
        }

        public void Info(Type className, string message = "", [CallerMemberName]string methodName = null)
        {
            Info(className.FullName, methodName, message);
        }


        /// <summary>
        /// Function for standizing method entry log messages.  Call this for function entry.
        /// Message are logged to the info log level.
        /// </summary>
        public void LogFunctionEntry(string className, string functionName, params object[] args)
        {
            string argString = "";
            if (args.Any())
                argString = "(" + String.Join(",", args) + ")";

            Debug(className, functionName, FunctionEntryString + argString);
        }
        public void LogFunctionEntry(Type className, [CallerMemberName]string methodName = null)
        {
            LogFunctionEntry(className.FullName, methodName);
        }

        /// <summary>
        /// Function for standizing function exit log messages.  Call this for function entry.
        /// Message are logged to the info log level.
        /// </summary>
        public void LogFunctionExit(string className, string functionName)
        {
            Debug(className, functionName, FunctionExitString);
        }

        public void LogFunctionExit(Type className, [CallerMemberName]string methodName = null)
        {
            LogFunctionExit(className.FullName, methodName);
        }

        /// <summary>
        /// Function for standizing method entry log messages.  Call this for function entry.
        /// Message are logged to the info log level.
        /// </summary>
        public void LogLogicalUnitEntry(string className, string functionName, params object[] args)
        {
            string argString = "";
            if (args.Any())
                argString = "(" + String.Join(",", args) + ")";

            Warn(className, functionName, FunctionEntryString + argString);
        }

        /// <summary>
        /// Function for standizing function exit log messages.  Call this for function entry.
        /// Message are logged to the info log level.
        /// </summary>
        public void LogLogicalUnitExit(string className, string functionName)
        {
            Warn(className, functionName, FunctionExitString);
        }

        /// <summary>
        /// Logs an exception at the Warn log level. 
        /// WARN: Something that's concerning but not causing the operation to abort; # of connections in the DB pool getting low, 
        /// an unusual-but-expected timeout in an operation, etc. I often think of 'WARN' as something that's useful in aggregate; 
        /// e.g. grep, group, and count them to get a picture of what's affecting the system health
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="exception">The exception to be logged</param>
        /// <param name="message">The message to be logged</param>
        public void Warn(string className, string methodName, Exception exception, string message = "")
        {
            if (exception == null)
            {
                Warn(className, methodName, message);
                return;
            }
            if (message == string.Empty)
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, string.Empty, exception.Message + " " + exception.StackTrace);
                AppLog.Warn($"{logMessage} - {exception}");
            }
            else
            {
                string logMessage = string.Format(LogMessageFormat, className, methodName, message, exception.Message + " " + exception.StackTrace);
                AppLog.Warn($"{logMessage} - {exception}");
            }
        }

        public void Warn(Type className, Exception exception, string message = "", [CallerMemberName]string methodName = null)
        {
            Warn(className.FullName, methodName, exception, message);
        }

        public void Warn(Type className, string message = "", [CallerMemberName]string methodName = null)
        {
            Warn(className.FullName, methodName, message);
        }

        /// <summary>
        /// Logs an exception at the Warn log level. 
        /// WARN: Something that's concerning but not causing the operation to abort; # of connections in the DB pool getting low, 
        /// an unusual-but-expected timeout in an operation, etc. I often think of 'WARN' as something that's useful in aggregate; 
        /// e.g. grep, group, and count them to get a picture of what's affecting the system health
        /// </summary>
        /// <param name="className">Name of the class from which the log message is being initiated.</param>
        /// <param name="methodName">The name of the method from which the log message is being initited.</param>
        /// <param name="message">The message to be logged</param>
        public void Warn(string className, string methodName, object message)
        {
            var logMessage = LogMessageParser(className, methodName, message);
            AppLog.Warn(logMessage);
        }

    }
}
