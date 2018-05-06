using System;
namespace CSharpRaft
{
    public interface StateMachine
    {
        byte[] Save();

        void Recovery(byte[] bytes);
    }


    public class DefaultStateMachine : StateMachine
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