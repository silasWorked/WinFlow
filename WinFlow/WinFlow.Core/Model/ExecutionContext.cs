using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WinFlow.Core.Model
{
    public class ExecutionContext
    {
        public bool DryRun { get; set; }
        public bool Verbose { get; set; }
        public string WorkingDirectory { get; set; } = System.Environment.CurrentDirectory;
        public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IDictionary<string, Model.FlowFunction> Functions { get; } = new Dictionary<string, Model.FlowFunction>(StringComparer.OrdinalIgnoreCase);
        public Action<string> Log { get; set; } = _ => { };
        public string? LogFile { get; set; }
        public Action<string>? LogError { get; set; }
        public Action<string>? LogWarning { get; set; }
        
        // Return value from function
        public string? ReturnValue { get; set; }
        
        // Current script file and line for __FILE__ and __LINE__
        public string? CurrentFile { get; set; }
        public int CurrentLine { get; set; }
        
        // CLI arguments for __ARGS__
        public string[]? CliArguments { get; set; }

        // Object store for complex module data (config/csv/xml/etc.)
        public IDictionary<string, object> ObjectStore { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Async tasks registry
        public IDictionary<string, Task> AsyncTasks { get; } = new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Initialize built-in variables (__FILE__, __OS__, __USER__, etc.)
        /// </summary>
        public void InitializeBuiltInVariables()
        {
            Environment["__OS__"] = System.Environment.OSVersion.ToString();
            Environment["__USER__"] = System.Environment.UserName;
            Environment["__TEMP__"] = System.IO.Path.GetTempPath();
            Environment["__MACHINE__"] = System.Environment.MachineName;
            Environment["__DOMAIN__"] = System.Environment.UserDomainName;
            Environment["__PROCESSOR_COUNT__"] = System.Environment.ProcessorCount.ToString();
            
            if (CurrentFile != null)
            {
                Environment["__FILE__"] = CurrentFile;
                Environment["__FILE_NAME__"] = System.IO.Path.GetFileName(CurrentFile);
                Environment["__FILE_DIR__"] = System.IO.Path.GetDirectoryName(CurrentFile) ?? "";
            }
            
            if (CliArguments != null && CliArguments.Length > 0)
            {
                Environment["__ARGS__"] = string.Join(" ", CliArguments);
                for (int i = 0; i < CliArguments.Length; i++)
                {
                    Environment[$"__ARG{i}__"] = CliArguments[i];
                }
            }
            
            Environment["__LINE__"] = CurrentLine.ToString();
        }
        
        /// <summary>
        /// Update __LINE__ variable
        /// </summary>
        public void UpdateLine(int line)
        {
            CurrentLine = line;
            Environment["__LINE__"] = line.ToString();
        }
    }
}