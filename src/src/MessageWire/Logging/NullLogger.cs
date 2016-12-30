using System;

namespace MessageWire.Logging
{
    internal class NullLogger : ILog
    {
        public Exception LastError {
            get {
                return null;
            }
        }

        public void Debug(string formattedMessage, params object[] args)
        {
        }

        public void Info(string formattedMessage, params object[] args)
        {
        }

        public void Warn(string formattedMessage, params object[] args)
        {
        }

        public void Error(string formattedMessage, params object[] args)
        {
        }

        public void Fatal(string formattedMessage, params object[] args)
        {
        }
    }
}