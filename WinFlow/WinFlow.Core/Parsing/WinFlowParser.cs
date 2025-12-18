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
            
            // First pass: collect function definitions
            var functions = ParseFunctions(lines, out var nonFunctionLines);
            
            // Register functions globally
            foreach (var func in functions)
            {
                WinFlow.Core.Runtime.CommandDispatcher.RegisterFunction(func);
            }
            
            // Add parsed functions to step as __define_func__ commands
            foreach (var func in functions)
            {
                step.Commands.Add(CreateDefineCommand(func));
            }
            
            // Second pass: parse regular commands
            for (int i = 0; i < nonFunctionLines.Count; i++)
            {
                var raw = nonFunctionLines[i];
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#") || line.StartsWith("//")) continue;

                // Check for special syntax: env set var=funcname(args)
                if (line.StartsWith("env set ", StringComparison.OrdinalIgnoreCase))
                {
                    var afterEnvSet = line.Substring(8).Trim();
                    if (afterEnvSet.Contains("=") && afterEnvSet.Contains("(") && afterEnvSet.EndsWith(")"))
                    {
                        var eqIdx = afterEnvSet.IndexOf('=');
                        var varName = afterEnvSet.Substring(0, eqIdx).Trim();
                        var funcCallStr = afterEnvSet.Substring(eqIdx + 1).Trim();
                        
                        if (funcCallStr.Contains("(") && funcCallStr.EndsWith(")"))
                        {
                            var parenIdx = funcCallStr.IndexOf('(');
                            var funcName = funcCallStr.Substring(0, parenIdx).Trim();
                            var argStr = funcCallStr.Substring(parenIdx + 1, funcCallStr.Length - parenIdx - 2).Trim();
                            
                            // Create function call command
                            var callCmd = new FlowCommand { Name = "call" };
                            callCmd.Args["name"] = funcName;
                            
                            if (!string.IsNullOrWhiteSpace(argStr))
                            {
                                var args = ParseFunctionCallArgs(argStr);
                                for (int j = 0; j < args.Count; j++)
                                {
                                    callCmd.Args[$"arg{j}"] = args[j];
                                }
                            }
                            step.Commands.Add(callCmd);
                            
                            // Create env.set command to capture return value
                            var envSetCmd = new FlowCommand { Name = "env.set" };
                            envSetCmd.Args["name"] = varName;
                            envSetCmd.Args["value"] = "${__RETURN__}";
                            step.Commands.Add(envSetCmd);
                            
                            continue;
                        }
                    }
                }
                
                // Check if this is a function call: funcname(args...)
                if (line.Contains("(") && line.EndsWith(")") && !line.Contains("="))
                {
                    var parenIdx = line.IndexOf('(');
                    var funcName = line.Substring(0, parenIdx).Trim();
                    var argStr = line.Substring(parenIdx + 1, line.Length - parenIdx - 2).Trim();
                    
                    // Parse function arguments: name(arg1, arg2, ...) or name("val1", "val2", ...)
                    var callCmd = new FlowCommand { Name = "call" };
                    callCmd.Args["name"] = funcName;
                    
                    if (!string.IsNullOrWhiteSpace(argStr))
                    {
                        var args = ParseFunctionCallArgs(argStr);
                        for (int j = 0; j < args.Count; j++)
                        {
                            callCmd.Args[$"arg{j}"] = args[j];
                        }
                    }
                    
                    step.Commands.Add(callCmd);
                    continue;
                }

                var (name, rest) = SplitFirstToken(line);
                var lname = name.ToLowerInvariant();

                // Handle two-part commands
                if (lname is "env" or "file" or "process" or "reg" or "net" or "sleep" or "loop" or "if" or "include" or "try" or "string" or "json" or "http" or "array" or "math" or "datetime" or "path" or "log" or "regex" or "archive" or "config" or "csv" or "registry" or "xml" or "async" or "input")
                {
                    var (sub, rest2) = SplitFirstToken(rest ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(sub) && lname != "if" && lname != "include" && lname != "try")
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
                    case "file.move":
                    case "file.exists":
                    case "file.read":
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
                    case "string.replace":
                    case "string.contains":
                    case "string.length":
                    case "string.upper":
                    case "string.lower":
                    case "string.trim":
                    case "string.concat":
                    case "string.format":
                    case "json.parse":
                    case "json.get":
                    case "http.get":
                    case "http.post":
                    case "http.put":
                    case "array.split":
                    case "array.join":
                    case "array.length":
                    case "math.add":
                    case "math.subtract":
                    case "math.multiply":
                    case "math.divide":
                    case "math.round":
                    case "math.floor":
                    case "math.ceil":
                    case "datetime.now":
                    case "datetime.format":
                    case "datetime.parse":
                    case "datetime.add":
                    case "datetime.diff":
                    case "path.join":
                    case "path.dirname":
                    case "path.basename":
                    case "path.extension":
                    case "path.exists":
                    case "path.is_directory":
                    case "path.normalize":
                    case "log.config":
                    case "log.info":
                    case "log.debug":
                    case "log.warning":
                    case "log.error":
                    case "regex.match":
                    case "regex.find":
                    case "regex.replace":
                    case "archive.create":
                    case "archive.extract":
                    case "archive.list":
                    case "archive.add":
                    case "config.read":
                    case "config.create":
                    case "config.get":
                    case "config.set":
                    case "config.write":
                    case "csv.read":
                    case "csv.create":
                    case "csv.add_row":
                    case "csv.get_field":
                    case "csv.write":
                    case "csv.filter":
                    case "csv.sort":
                    case "registry.get":
                    case "registry.set":
                    case "registry.exists":
                    case "registry.delete":
                    case "xml.parse":
                    case "xml.create":
                    case "xml.add_element":
                    case "xml.set_attribute":
                    case "xml.get":
                    case "xml.write":
                    case "async.start":
                    case "async.wait":
                    case "async.wait_all":
                    case "async.status":
                    case "input.text":
                    case "input.password":
                    case "input.confirm":
                    case "input.choice":
                    case "return":
                    case "isset":
                    case "if":
                    case "try":
                    case "include":
                    case "call":
                        step.Commands.Add(new FlowCommand
                        {
                            Name = lname,
                            Args = ParseKeyValueArgs(rest)
                        });
                        break;
                    case "import":
                        step.Commands.Add(new FlowCommand
                        {
                            Name = lname,
                            Args = new Dictionary<string, string>
                            {
                                ["path"] = Unquote(rest?.Trim() ?? string.Empty)
                            }
                        });
                        break;
                    default:
                        throw new InvalidDataException($"Unknown command '{name}' at line {i + 1}: {line}");
                }
            }

            task.Steps.Add(step);
            return new List<FlowTask> { task };
        }
        
        private List<FlowFunction> ParseFunctions(string[] allLines, out List<string> nonFunctionLines)
        {
            var functions = new List<FlowFunction>();
            nonFunctionLines = new List<string>();
            
            int i = 0;
            while (i < allLines.Length)
            {
                var line = allLines[i].Trim();
                
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
                {
                    i++;
                    continue;
                }

                if (line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                {
                    var func = ParseFunctionDef(allLines, i, out var nextLine);
                    functions.Add(func);
                    i = nextLine;
                }
                else
                {
                    nonFunctionLines.Add(allLines[i]);
                    i++;
                }
            }

            return functions;
        }

        private FlowFunction ParseFunctionDef(string[] allLines, int startLine, out int nextLineIndex)
        {
            var line = allLines[startLine].Trim();
            
            if (!line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Expected 'define' at line {startLine + 1}");

            var afterDefine = line.Substring(7).Trim();
            var colonIdx = afterDefine.LastIndexOf(':');
            if (colonIdx < 0)
                throw new InvalidDataException($"Function definition must end with ':' at line {startLine + 1}");

            var header = afterDefine.Substring(0, colonIdx).Trim();
            var parenIdx = header.IndexOf('(');
            
            if (parenIdx < 0)
                throw new InvalidDataException($"Function definition must have parameters at line {startLine + 1}");

            var funcName = header.Substring(0, parenIdx).Trim();
            var closeParenIdx = header.LastIndexOf(')');
            
            if (closeParenIdx < 0 || closeParenIdx <= parenIdx)
                throw new InvalidDataException($"Invalid function parameters at line {startLine + 1}");

            var paramStr = header.Substring(parenIdx + 1, closeParenIdx - parenIdx - 1).Trim();
            var parameters = new List<string>();
            if (!string.IsNullOrWhiteSpace(paramStr))
            {
                foreach (var param in paramStr.Split(','))
                {
                    parameters.Add(param.Trim());
                }
            }

            var commands = new List<FlowCommand>();
            int i = startLine + 1;
            
            while (i < allLines.Length)
            {
                var bodyLine = allLines[i];
                
                if (bodyLine.Length > 0 && char.IsWhiteSpace(bodyLine[0]))
                {
                    var trimmed = bodyLine.Trim();
                    
                    if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                    {
                        i++;
                        continue;
                    }

                    var cmd = ParseCommandLine(trimmed);
                    commands.Add(cmd);
                    i++;
                }
                else if (bodyLine.Trim().Length == 0)
                {
                    i++;
                }
                else
                {
                    break;
                }
            }

            nextLineIndex = i;
            return new FlowFunction
            {
                Name = funcName,
                Parameters = parameters,
                Commands = commands
            };
        }

        private FlowCommand ParseCommandLine(string line)
        {
            var (name, rest) = SplitFirstToken(line);
            var lname = name.ToLowerInvariant();

            if (lname is "env" or "file" or "process" or "reg" or "net" or "sleep" or "loop" or "if" or "include" or "try" or "string" or "json" or "http" or "array")
            {
                var (sub, rest2) = SplitFirstToken(rest ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(sub) && lname != "if" && lname != "include" && lname != "try")
                {
                    lname = lname + "." + sub.ToLowerInvariant();
                    rest = rest2;
                }
            }

            if (lname == "echo")
            {
                return new FlowCommand
                {
                    Name = "echo",
                    Args = new Dictionary<string, string>
                    {
                        ["message"] = Unquote(rest?.Trim() ?? string.Empty)
                    }
                };
            }

            return new FlowCommand
            {
                Name = lname,
                Args = ParseKeyValueArgs(rest)
            };
        }

        private FlowCommand CreateDefineCommand(FlowFunction func)
        {
            var cmd = new FlowCommand 
            { 
                Name = "__define_func__",
                Metadata = func
            };
            cmd.Args["name"] = func.Name;
            return cmd;
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

        private static List<string> ParseFunctionCallArgs(string argStr)
        {
            var args = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';
            
            for (int i = 0; i < argStr.Length; i++)
            {
                var c = argStr[i];
                
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
                    else if (c == ',')
                    {
                        if (sb.Length > 0)
                        {
                            args.Add(sb.ToString().Trim());
                            sb.Clear();
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            
            if (sb.Length > 0)
            {
                args.Add(sb.ToString().Trim());
            }
            
            return args;
        }
    }
}
