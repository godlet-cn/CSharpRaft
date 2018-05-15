using System;

namespace CSharpRaft
{
    public interface IStateMachine
    {
        byte[] Save();

        void Recovery(byte[] bytes);
    }


    public class DefaultStateMachine : IStateMachine
    {
        public void Recovery(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public byte[] Save()
        {
            throw new NotImplementedException();
        }
    }
}