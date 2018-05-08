using CSharpRaft;

namespace CSharpRaft.Test.Mocks
{
    class Command2 : Command
    {
        public int Id { get; set; }

        public string CommandName
        {
            get
            {
                return "Command2";
            }
        }

        public object Apply(IContext context)
        {
            return null;
        }
    }
}
