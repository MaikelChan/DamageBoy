using System;

namespace GBEmu
{
    public enum LogType
    {
        Info,
        Warning,
        Error
    }

    static class Utils
    {
        public static void Log(string message)
        {
            Log(LogType.Info, message);
        }

        public static void Log(LogType type, string message)
        {
            switch (type)
            {
                default:
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }

            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [{type}] {message}");
        }

        public static void LogEmpty()
        {
            Console.WriteLine();
        }
    }
}