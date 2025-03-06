using System.Data.Common;
using System.Text.Json;
using NLog;

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedParameter.Local
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable UnusedMember.Local

namespace END.Database
{
    public class QueryException : Exception
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public QueryException(string message, LogLevel level = LogLevel.Warning) 
        {
            Message = message;
            LogMessage(message, level);
            
            StackTrace = ParseTrace();
            LogMessage(StackTrace, LogLevel.Error);
        }

        public QueryException(string userId, string clientId, DbException ex, DbCommand? oCmd = null)
        {
            UserId = userId;
            ClientId = clientId;

            LogMessage($"User ID: {userId}", LogLevel.Error);
            LogMessage($"Client ID: {clientId}", LogLevel.Error);

            Message = ParseDbError(ex);
            LogMessage(Message, LogLevel.Error);

            StackTrace = ParseTrace();

            LogMessage(StackTrace, LogLevel.Error);

            InnerException = ex;
        }

        #region "Helper Functions"

        private static string ParseDbError(DbException ex)
        {
            var msg = string.Empty;
            return msg;
        }

        private static string ParseQuery(string query)
        {
            List<string> ret = new();
            query = query.Replace(Environment.NewLine,"\n");
            var sql = query.Split('\n').ToList();

            foreach(var line in sql)
            {
                var tmp = line.Replace("  ", " ");
                ret.Add(tmp);
            }

            return string.Join(" ",ret.ToArray());
        }

        private static string ParseTrace()
        {
            var currentStack = new System.Diagnostics.StackTrace(true);
            var trace = currentStack.ToString();
            trace = trace.Replace(Environment.NewLine, "\n");
            var stackTrace = trace.Split('\n');

            var newTrace = new List<string>();

            foreach(var line in stackTrace)
            {
                if (line.Contains("QueryException")) continue;
                if (line.Contains("System.Web")) break;
                newTrace.Add(line);
            }

            var ret = string.Join(Environment.NewLine, newTrace.ToArray());
            return ret;
        }

        private static void LogMessage(string message, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    Logger.Trace(message);
                    break;
                case LogLevel.Debug:
                    Logger.Debug(message);
                    break;
                case LogLevel.Info:
                    Logger.Info(message);
                    break;
                case LogLevel.Warning:
                    Logger.Warn(message);
                    break;
                case LogLevel.Error:
                    Logger.Error(message);
                    break;
                default:
                    Logger.Info(message);
                    break; 
            }
        }

        #endregion
        
        public new string Message { get; }

        public string? UserId { get; }
        public string? ClientId { get; }
        public new string StackTrace { get; }
        public LogLevel LogLevel { get; }
        
        public new DbException? InnerException { get; set; }

        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(this, options);
            return jsonString;
        }
    }
}
