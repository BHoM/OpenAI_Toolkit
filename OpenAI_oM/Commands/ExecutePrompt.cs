using BH.oM.Adapter;
using BH.oM.Base;

namespace BH.oM.Adapters.OpenAI.Commands
{
    public class ExecutePrompt : IExecuteCommand, IObject
    {
        public string System { get; set; }
        public string User { get; set; }
    }
}
