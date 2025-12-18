using System.Collections.Generic;

namespace WinFlow.Core.Model
{
    public class FlowTask
    {
        public string Name { get; set; } = "";
        public List<FlowStep> Steps { get; set; } = new();
    }

    public class FlowStep
    {
        public List<FlowCommand> Commands { get; set; } = new();
    }

    public class FlowCommand
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> Args { get; set; } = new();
        public object? Metadata { get; set; } // For storing FlowFunction or other metadata
    }

    public class FlowFunction
    {
        public string Name { get; set; } = "";
        public List<string> Parameters { get; set; } = new();
        public List<FlowCommand> Commands { get; set; } = new();
    }
}
