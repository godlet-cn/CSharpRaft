namespace CSharpRaft
{
    public interface StateMachine
    {
        byte[] Save();

        void Recovery(byte[] bytes);
    }
}