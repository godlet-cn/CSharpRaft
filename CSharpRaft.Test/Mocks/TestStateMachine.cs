using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpRaft;
namespace CSharpRaft.Test.Mocks
{
    class TestStateMachine : StateMachine
    {
        public Func<byte[]> SaveFunc;
        public Action<byte[]> RecoveryFunc;

        public void Recovery(byte[] state)
        {
             this.RecoveryFunc(state);
        }

        public byte[] Save()
        {
            return this.SaveFunc();
        }
    }
}
