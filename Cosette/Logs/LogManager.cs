using System;
using System.IO;

namespace Cosette.Logs
{
    public static class LogManager
    {
        private static readonly string _logFile;
        private static readonly string _basePath;

#if LOGGER
        private static StreamWriter _infoLogStreamWriter;
#endif

        static LogManager()
        {
            _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _logFile = Path.Combine(_basePath, $"info-{DateTime.Now.Ticks}.log");

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }

#if LOGGER
            _infoLogStreamWriter = new StreamWriter(_logFile);
#endif
        }

        public static void LogInfo(string message)
        {
#if LOGGER
            _infoLogStreamWriter.WriteLine(message);
#endif
        }

        public static void LogError(string message)
        {
            using (var streamWriter = new StreamWriter(Path.Combine(_basePath, $"error-{DateTime.Now.Ticks}.log"), true))
            {
                streamWriter.WriteLine(message);
            }
        }
    }
}
