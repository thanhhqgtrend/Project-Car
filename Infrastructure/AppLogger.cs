using System;
using System.Diagnostics;

namespace LuxuryCar.Infrastructure
{
    public interface IAppLogger<T>
    {
        void LogWarning(Exception exception, string message);
        void LogError(Exception exception, string message);
        void LogInformation(string message);
    }

    public class TraceLogger<T> : IAppLogger<T>
    {
        public void LogWarning(Exception exception, string message)
        {
            Trace.TraceWarning("{0}: {1}", message, exception);
        }

        public void LogError(Exception exception, string message)
        {
            Trace.TraceError("{0}: {1}", message, exception);
        }

        public void LogInformation(string message)
        {
            Trace.TraceInformation(message);
        }
    }
}
