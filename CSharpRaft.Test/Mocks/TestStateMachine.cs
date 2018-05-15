using System;

namespace CSharpRaft.Test.Mocks
{
    class TestStateMachine : IStateMachine
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
