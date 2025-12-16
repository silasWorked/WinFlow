using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WinFlow.Core.Model;

namespace WinFlow.Core.Parsing
{
    public class WinFlowParser : IParser
    {
        public List<FlowTask> Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Script not found: {filePath}");

            var task = new FlowTask { Name = Path.GetFileNameWithoutExtension(filePath) };
            var step = new FlowStep();

            var lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();
                if (line.Length == 0) continue; // skip blanks
                if (line.StartsWith("#") || line.StartsWith("//")) continue; // comments

                // command name is first token; remainder is arguments
                var (name, rest) = SplitFirstToken(line);
                switch (name.ToLowerInvariant())
                {
                    case "noop":
                        step.Commands.Add(new FlowCommand { Name = "noop" });
                        break;
                    case "echo":
                        step.Commands.Add(new FlowCommand
                        {
                            Name = "echo",
                            Args = new Dictionary<string, string>
                            {
                                ["message"] = Unquote(rest?.Trim() ?? string.Empty)
                            }
                        });
                        break;
                    default:
                        throw new InvalidDataException($"Unknown command '{name}' at line {i + 1}.");
                }
            }

            task.Steps.Add(step);
            return new List<FlowTask> { task };
        }

        private static (string name, string? rest) SplitFirstToken(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i]))
                {
                    var name = line.Substring(0, i);
                    var rest = line.Substring(i + 1);
                    return (name, rest);
                }
            }
            return (line, null);
        }

        private static string Unquote(string s)
        {
            if (s.Length >= 2)
            {
                if ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\''))
                {
                    return s.Substring(1, s.Length - 2);
                }
            }
            return s;
        }
    }
}