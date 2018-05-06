using CSharpRaft;
namespace CSharpRaft.Test.Mocks
{
    class Command1 : Command
    {
        public string Val { get; set; }
        public string CommandName()
        {
            return "Command1";
        }

        public object Apply(Server server) {
            return null;
        }
    }
}
