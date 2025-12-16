using System;
using System.Collections.Generic;

namespace WinFlow.Core.Model
{
    public class ExecutionContext
    {
        public bool DryRun { get; set; }
        public bool Verbose { get; set; }
        public string WorkingDirectory { get; set; } = System.Environment.CurrentDirectory;
        public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Action<string> Log { get; set; } = _ => { };
    }
}