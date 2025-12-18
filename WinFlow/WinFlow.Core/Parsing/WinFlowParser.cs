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
                var lname = name.ToLowerInvariant();

                // handle two-part commands like "env set" => command name becomes "env.set"
                if (lname is "env" or "file" or "process" or "reg" or "net" or "sleep" or "loop")
                {
                    var (sub, rest2) = SplitFirstToken(rest ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(sub))
                    {
                        lname = lname + "." + sub.ToLowerInvariant();
                        rest = rest2;
                    }
                }

                switch (lname)
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
                    case "env.set":
                    case "env.unset":
                    case "env.print":
                    case "file.write":
                    case "file.append":
                    case "file.mkdir":
                    case "file.delete":
                    case "file.copy":
                    case "process.run":
                    case "process.exec":
                    case "reg.set":
                    case "reg.get":
                    case "reg.delete":
                    case "sleep.ms":
                    case "sleep.sec":
                    case "net.download":
                    case "loop.repeat":
                    case "loop.foreach":
                        step.Commands.Add(new FlowCommand
                        {
                            Name = lname,
                            Args = ParseKeyValueArgs(rest)
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

        private static Dictionary<string, string> ParseKeyValueArgs(string? rest)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rest)) return result;
            foreach (var tok in Tokenize(rest))
            {
                var idx = tok.IndexOf('=');
                if (idx <= 0) continue;
                var key = tok.Substring(0, idx).Trim();
                var val = tok.Substring(idx + 1).Trim();
                result[key] = Unquote(val);
            }
            return result;
        }

        private static List<string> Tokenize(string s)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (inQuotes)
                {
                    if (c == quoteChar)
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"' || c == '\'')
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        if (sb.Length > 0)
                        {
                            tokens.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }
    }
}