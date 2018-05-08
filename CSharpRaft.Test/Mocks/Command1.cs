using CSharpRaft;

namespace CSharpRaft.Test.Mocks
{
    class Command1 : Command
    {
        public string Val { get; set; }

        public string CommandName
        {
            get {
                return "Command1";
            }
        }

        public object Apply(IContext context) {
            return null;
        }
    }
}
