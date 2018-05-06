using CSharpRaft;
namespace CSharpRaft.Test.Mocks
{
    class Command2 : Command
    {
        public int Id { get; set; }

        public string CommandName()
        {
            return "Command2";
        }

        public object Apply(Server server)
        {
            return null;
        }
    }
}
