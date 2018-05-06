using NUnit.Framework;
using CSharpRaft;

namespace CSharpRaft.Test
{

    [TestFixture]
    class DebugTraceTest
    {
        [Test]
        public void WarnTest()
        {
            DebugTrace.Warn("This is a warning");

            DebugTrace.Warn("{0}","This is a warning");

            DebugTrace.Warn("{0} {1}", "This is first warning", "This is second warning");

            DebugTrace.Warn("{0} {1} {2} {3}", 1,"This is first warning", 2, "This is second warning");

            DebugTrace.WarnLine("This is a warning");
        }

        [Test]
        public void DebugTest()
        {
            DebugTrace.Debug("This is a warning");

            DebugTrace.Debug("{0}", "This is a warning");

            DebugTrace.Debug("{0} {1}", "This is first warning", "This is second warning");

            DebugTrace.Debug("{0} {1} {2} {3}", 1, "This is first warning", 2, "This is second warning");

            DebugTrace.DebugLine("This is a warning");
        }


        [Test]
        public void TraceTest()
        {
            DebugTrace.Trace("This is a warning");

            DebugTrace.Trace("{0}", "This is a warning");

            DebugTrace.Trace("{0} {1}", "This is first warning", "This is second warning");

            DebugTrace.Trace("{0} {1} {2} {3}", 1, "This is first warning", 2, "This is second warning");

            DebugTrace.TraceLine("This is a warning");
        }
    }
}
