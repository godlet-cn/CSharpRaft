using System;
using System.IO;

namespace CSharpRaft
{
    public enum LogLevel
    {
        NONE = 0,
        DEBUG = 1,
        TRACE = 2
    }

    public static class DebugTrace
    {
        public static TextWriter LogWriter;

        public static LogLevel Level;

        static DebugTrace()
        {
            Level = LogLevel.TRACE;

            LogWriter = Console.Out;
        }

        //--------------------------------------
        // Warnings
        //--------------------------------------
        
        // Prints to the standard logger. Arguments are handled in the manner of
        // fmt.Print.
        public static void Warn(params object[] objs)
        {
            LogWriter.Write(string.Join(" ", objs));
        }

        // Prints to the standard logger. Arguments are handled in the manner of
        // fmt.Printf.
        public static void Warn(string format, params object[] objs)
        {
            LogWriter.Write(format, objs);
        }

        // Prints to the standard logger. Arguments are handled in the manner of
        // fmt.Println.
        public static void WarnLine(params object[] objs)
        {
            LogWriter.WriteLine(string.Join(" ", objs));
        }


        //--------------------------------------
        // Basic debugging
        //--------------------------------------

        // Prints to the standard logger if debug mode is enabled. Arguments
        // are handled in the manner of fmt.Print.
        public static void Debug(params object[] objs)
        {
            if (Level >= LogLevel.DEBUG)
            {
                LogWriter.Write(string.Join(" ", objs));
            }
        }

        // Prints to the standard logger if debug mode is enabled. Arguments
        // are handled in the manner of fmt.Printf.
        public static void Debug(string format, params object[] objs)
        {
            if (Level >= LogLevel.DEBUG)
            {
                LogWriter.Write(format, objs);
            }
        }

        // Prints to the standard logger if debug mode is enabled. Arguments
        // are handled in the manner of fmt.Println.
        public static void DebugLine(params object[] objs)
        {
            if (Level >= LogLevel.DEBUG)
            {
                LogWriter.WriteLine(string.Join(" ", objs));
            }
        }

        //--------------------------------------
        // Trace-level debugging
        //--------------------------------------

        // Prints to the standard logger if trace debugging is enabled. Arguments
        // are handled in the manner of fmt.Print.
        public static void Trace(params object[] objs)
        {
            if (Level >= LogLevel.TRACE)
            {
                LogWriter.Write(string.Join(" ", objs));
            }
        }

        // Prints to the standard logger if trace debugging is enabled. Arguments
        // are handled in the manner of fmt.Printf.
        public static void Trace(string format, params object[] objs)
        {
            if (Level >= LogLevel.TRACE)
            {
                LogWriter.Write(format, objs);
            }
        }

        // Prints to the standard logger if trace debugging is enabled. Arguments
        // are handled in the manner of debugln.
        public static void TraceLine(params object[] objs)
        {
            if (Level >= LogLevel.TRACE)
            {
                LogWriter.WriteLine(string.Join(" ", objs));
            }
        }
    }
}
