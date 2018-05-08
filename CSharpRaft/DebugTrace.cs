using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public static class DebugTrace
    {
        public const int DEBUG = 1;
        public const int TRACE = 2;

        static DebugTrace()
        {
            LogLevel = 1;

            LogWriter = Console.Out;
        }

        public static TextWriter LogWriter;

        public static int LogLevel;

        //------------------------------------------------------------------------------
        //
        // Functions
        //
        //------------------------------------------------------------------------------

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
            if (LogLevel >= DEBUG)
            {
                LogWriter.WriteLine(string.Join(" ", objs));
            }
        }

        // Prints to the standard logger if debug mode is enabled. Arguments
        // are handled in the manner of fmt.Printf.
        public static void Debug(string format, params object[] objs)
        {
            if (LogLevel >= DEBUG)
            {
                LogWriter.WriteLine(format, objs);
            }
        }

        // Prints to the standard logger if debug mode is enabled. Arguments
        // are handled in the manner of fmt.Println.
        public static void DebugLine(params object[] objs)
        {
            if (LogLevel >= DEBUG)
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
            if (LogLevel >= TRACE)
            {
                LogWriter.WriteLine(string.Join(" ", objs));
            }
        }

        // Prints to the standard logger if trace debugging is enabled. Arguments
        // are handled in the manner of fmt.Printf.
        public static void Trace(string format, params object[] objs)
        {
            if (LogLevel >= TRACE)
            {
                LogWriter.WriteLine(format, objs);
            }
        }

        // Prints to the standard logger if trace debugging is enabled. Arguments
        // are handled in the manner of debugln.
        public static void TraceLine(params object[] objs)
        {
            if (LogLevel >= TRACE)
            {
                LogWriter.WriteLine(string.Join(" ", objs));
            }
        }
    }
}
