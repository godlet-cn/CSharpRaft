using CSharpRaft.Test.Mocks;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpRaft.Test
{
    [TestFixture]
    class SnapshotTest
    {
        public void runServerWithMockStateMachine(ServerState state, Action<Server, StateMachine> fn)
        {
            MockRepository mocks = new MockRepository();
            StateMachine m = mocks.DynamicMock<StateMachine>();
            Server s = TestServer.NewTestServer("1", new TestTransporter(), m);
            try
            {
                if (state == ServerState.Leader)
                {
                    s.Do(new DefaultJoinCommand() { Name = s.Name });
                }
                s.Start();
            }
            catch (Exception)
            {

            }
            finally
            {
                s.Stop();
            }
            fn(s, m);
        }
        // Ensure that a snapshot occurs when there are existing logs.
        [Test]
        public void TestSnapshot()
        {
            runServerWithMockStateMachine(ServerState.Leader, (s, m) =>
            {
                Assert.AreEqual(s.Name, "1");

                s.Do(new Command1() { Val = "123" });

                s.TakeSnapshot();

                Assert.AreEqual(s.GetSnapshot().LastIndex, 2);

                // Repeat to make sure new snapshot gets created.
                s.Do(new Command1() { Val = "345" });
                s.TakeSnapshot();
                Assert.AreEqual(s.GetSnapshot().LastIndex, 4);

                // Restart server.
                s.Stop();
                // Recover from snapshot.
                s.LoadSnapshot();
                s.Start();
            });
        }

        // Ensure that a new server can recover from previous snapshot with log
        [Test]
        public void TestSnapshotRecovery()
        {
            runServerWithMockStateMachine(ServerState.Leader, (s, m) =>
            {
                s.Do(new Command1() { Val = "123" });
                s.TakeSnapshot();

                Assert.AreEqual(s.GetSnapshot().LastIndex, 2);

                // Repeat to make sure new snapshot gets created.
                s.Do(new Command1() { Val = "123" });

                // Stop the old server
                s.Stop();

                // create a new server with previous log and snapshot
                Server newS = new Server("1", s.Path, new TestTransporter { }, s.StateMachine, null, "");
                // Recover from snapshot.
                newS.LoadSnapshot();

                newS.Start();

                // wait for it to become leader
                Thread.Sleep(1000);

                // ensure server load the previous log
                Assert.AreEqual(newS.LogEntries.Count, 3, "");
                newS.Stop();
            });
        }

        // Ensure that a snapshot request can be sent and received.
        [Test]
        public void TestSnapshotRequest()
        {
            runServerWithMockStateMachine(ServerState.Follower, (s, m) => {
                // Send snapshot request.
                var resp = s.RequestSnapshot(new SnapshotRequest() { LastIndex = 5, LastTerm = 1 });
                Assert.AreEqual(resp.Success, true);


                Assert.AreEqual(s.State, ServerState.Snapshotting);

                // Send recovery request.
                var resp2 = s.SnapshotRecoveryRequest(new SnapshotRecoveryRequest()
                {
                    LeaderName = "1",
                    LastIndex = 5,
                    LastTerm = 2,
                    Peers = new List<Peer>(),
                    State = Encoding.Default.GetBytes("bar")

                });

                Assert.AreEqual(resp2.Success, true);


            });
        }
    }
}
