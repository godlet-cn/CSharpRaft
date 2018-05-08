using CSharpRaft.Test.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft.Test
{
    [TestFixture]
    class ServerTest
    {
        [Test]
        public void TestServerRequestVote()
        {
            Server server = TestServer.NewTestServer("1", new TestTransporter { });

            server.Start();
            server.Do(new DefaultJoinCommand { Name = server.Name });
            
            var resp = server.RequestVote(new RequestVoteRequest(1, "foo", 1, 0));
            if (resp.Term != 1||!resp.VoteGranted)
            {
                throw new Exception(string.Format("Invalid request vote response:{0}/{1}", resp.Term, resp.VoteGranted));
            }

            server.Stop();
        }
    }
}
