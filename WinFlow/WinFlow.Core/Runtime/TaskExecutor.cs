using System.Collections.Generic;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Model;

namespace WinFlow.Core.Runtime
{
    public class TaskExecutor
    {
        private readonly CommandDispatcher _dispatcher = new();

        public int Run(IEnumerable<FlowTask> tasks, ExecutionContext context)
        {
            foreach (var task in tasks)
            {
                context.Log($"[task] {task.Name}");
                foreach (var step in task.Steps)
                {
                    foreach (var cmd in step.Commands)
                    {
                        _dispatcher.Execute(cmd, context);
                    }
                }
            }
            return 0;
        }
    }
}