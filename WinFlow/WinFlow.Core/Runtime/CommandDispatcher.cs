using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.Win32;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Model;

namespace WinFlow.Core.Runtime
{
    public class CommandDispatcher
    {
        private readonly Dictionary<string, Action<FlowCommand, ExecutionContext>> _handlers =
            new(StringComparer.OrdinalIgnoreCase);

        public CommandDispatcher()
        {
            RegisterBuiltIns();
        }

        public void Register(string name, Action<FlowCommand, ExecutionContext> handler)
        {
            _handlers[name] = handler;
        }

        private static void RunInline(string body, ExecutionContext ctx)
        {
            var temp = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(temp, body + System.Environment.NewLine);
            WinFlow.Core.Parsing.IParser parser = new WinFlow.Core.Parsing.WinFlowParser();
            var tasks = parser.Parse(temp);
            var exec = new TaskExecutor();
            exec.Run(tasks, ctx);
            try { System.IO.File.Delete(temp); } catch { }
        }

        public void Execute(FlowCommand command, ExecutionContext context)
        {
            if (!_handlers.TryGetValue(command.Name, out var handler))
                throw new InvalidOperationException($"Unknown command: {command.Name}");

            if (context.DryRun)
            {
                context.Log($"[dry-run] {command.Name} {FormatArgs(command.Args)}");
                return;
            }

            // Expand variables in arguments before execution
            var expanded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in command.Args)
                expanded[kv.Key] = Expand(kv.Value, context);
            var cmd = new FlowCommand { Name = command.Name, Args = expanded };

            handler(cmd, context);
        }

        private static string FormatArgs(Dictionary<string, string> args)
        {
            var parts = new List<string>();
            foreach (var kv in args)
                parts.Add($"{kv.Key}='{kv.Value}'");
            return string.Join(" ", parts);
        }

        private static string Expand(string input, ExecutionContext ctx)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var s = input;
            int start = 0;
            while (true)
            {
                var i = s.IndexOf("${", start, StringComparison.Ordinal);
                if (i < 0) break;
                var j = s.IndexOf('}', i + 2);
                if (j < 0) break;
                var name = s.Substring(i + 2, j - (i + 2));
                if (!ctx.Environment.TryGetValue(name, out var val)) val = string.Empty;
                s = s.Substring(0, i) + val + s.Substring(j + 1);
                start = i + val.Length;
            }
            return s;
        }

        private void RegisterBuiltIns()
        {
            Register("noop", (cmd, ctx) =>
            {
                ctx.Log("noop");
            });

            Register("echo", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("message", out var message))
                    message = string.Empty;
                ctx.Log(message);
            });

            // Env module
            Register("env.set", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("name", out var name) && !cmd.Args.TryGetValue("key", out name))
                    throw new ArgumentException("env.set requires name=<VAR> or key=<VAR>");
                if (!cmd.Args.TryGetValue("value", out var value))
                    value = string.Empty;
                ctx.Environment[name] = value;
                ctx.Log($"env set {name}='{value}'");
            });

            Register("env.unset", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("name", out var name) && !cmd.Args.TryGetValue("key", out name))
                    throw new ArgumentException("env.unset requires name=<VAR> or key=<VAR>");
                ctx.Environment.Remove(name);
                ctx.Log($"env unset {name}");
            });

            Register("env.print", (cmd, ctx) =>
            {
                foreach (var kv in ctx.Environment)
                    ctx.Log($"{kv.Key}={kv.Value}");
            });

            // File module
            Register("file.write", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.write requires path=<file>");
                cmd.Args.TryGetValue("content", out var content);
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                System.IO.File.WriteAllText(full, content ?? string.Empty);
                ctx.Log($"wrote {path}");
            });

            Register("file.append", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.append requires path=<file>");
                cmd.Args.TryGetValue("content", out var content);
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                System.IO.File.AppendAllText(full, content ?? string.Empty);
                ctx.Log($"appended {path}");
            });

            Register("file.mkdir", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.mkdir requires path=<dir>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                System.IO.Directory.CreateDirectory(full);
                ctx.Log($"mkdir {path}");
            });

            Register("file.delete", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.delete requires path=<file|dir>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                var recursive = cmd.Args.TryGetValue("recursive", out var r) && IsTrue(r);
                if (System.IO.File.Exists(full))
                {
                    System.IO.File.Delete(full);
                    ctx.Log($"deleted file {path}");
                }
                else if (System.IO.Directory.Exists(full))
                {
                    System.IO.Directory.Delete(full, recursive);
                    ctx.Log($"deleted dir {path}");
                }
                else
                {
                    ctx.Log($"not found {path}");
                }
            });

            Register("file.copy", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("src", out var src))
                    throw new ArgumentException("file.copy requires src=<file>");
                if (!cmd.Args.TryGetValue("dst", out var dst) && !cmd.Args.TryGetValue("dest", out dst))
                    throw new ArgumentException("file.copy requires dst=<file>");
                var sfull = System.IO.Path.IsPathRooted(src)
                    ? src
                    : System.IO.Path.Combine(ctx.WorkingDirectory, src);
                var dfull = System.IO.Path.IsPathRooted(dst)
                    ? dst
                    : System.IO.Path.Combine(ctx.WorkingDirectory, dst);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dfull)!);
                var overwrite = !cmd.Args.TryGetValue("overwrite", out var o) || IsTrue(o);
                System.IO.File.Copy(sfull, dfull, overwrite);
                ctx.Log($"copied {src} -> {dst}");
            });

            Register("file.move", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("src", out var src))
                    throw new ArgumentException("file.move requires src=<file>");
                if (!cmd.Args.TryGetValue("dst", out var dst) && !cmd.Args.TryGetValue("dest", out dst))
                    throw new ArgumentException("file.move requires dst=<file>");
                var sfull = System.IO.Path.IsPathRooted(src)
                    ? src
                    : System.IO.Path.Combine(ctx.WorkingDirectory, src);
                var dfull = System.IO.Path.IsPathRooted(dst)
                    ? dst
                    : System.IO.Path.Combine(ctx.WorkingDirectory, dst);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dfull)!);
                var overwrite = cmd.Args.TryGetValue("overwrite", out var o) && IsTrue(o);
                if (overwrite && System.IO.File.Exists(dfull))
                    System.IO.File.Delete(dfull);
                System.IO.File.Move(sfull, dfull);
                ctx.Log($"moved {src} -> {dst}");
            });

            Register("file.exists", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.exists requires path=<file|dir>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                var exists = System.IO.File.Exists(full) || System.IO.Directory.Exists(full);
                var result = exists ? "true" : "false";
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            // Process module
            Register("process.run", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("process.run requires file=<exe>");
                cmd.Args.TryGetValue("args", out var args);
                var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args ?? string.Empty,
                    WorkingDirectory = ctx.WorkingDirectory,
                    UseShellExecute = true
                });
                ctx.Log($"process run {file} started (PID: {proc?.Id})");
            });

            Register("process.exec", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("process.exec requires file=<exe>");
                cmd.Args.TryGetValue("args", out var args);
                var proc = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = file,
                        Arguments = args ?? string.Empty,
                        WorkingDirectory = ctx.WorkingDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
                var output = proc.StandardOutput.ReadToEnd();
                var error = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(output)) ctx.Log(output);
                if (!string.IsNullOrEmpty(error)) ctx.Log("[error] " + error);
                ctx.Log($"process exec {file} exited with code {proc.ExitCode}");
            });

            // Registry module
            Register("reg.set", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("reg.set requires key=<path>");
                if (!cmd.Args.TryGetValue("name", out var name))
                    throw new ArgumentException("reg.set requires name=<valueName>");
                cmd.Args.TryGetValue("value", out var value);
                cmd.Args.TryGetValue("type", out var type);
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                var baseKey = ResolveHive(hive);
                using var key = baseKey.CreateSubKey(keyPath);
                var kind = ResolveValueKind(type);
                key!.SetValue(name, value ?? string.Empty, kind);
                ctx.Log($"reg set [{hive}] {keyPath} {name}");
            });

            Register("reg.get", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("reg.get requires key=<path>");
                if (!cmd.Args.TryGetValue("name", out var name))
                    throw new ArgumentException("reg.get requires name=<valueName>");
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                var baseKey = ResolveHive(hive);
                using var key = baseKey.OpenSubKey(keyPath);
                var val = key?.GetValue(name, null);
                ctx.Log(val?.ToString() ?? string.Empty);
            });

            Register("reg.delete", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("reg.delete requires key=<path>");
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                var baseKey = ResolveHive(hive);
                if (cmd.Args.TryGetValue("name", out var name))
                {
                    using var key = baseKey.OpenSubKey(keyPath, writable: true);
                    key?.DeleteValue(name, throwOnMissingValue: false);
                    ctx.Log($"reg delete value [{hive}] {keyPath} {name}");
                }
                else
                {
                    try { baseKey.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false); } catch { }
                    ctx.Log($"reg delete key [{hive}] {keyPath}");
                }
            });

            // Sleep module
            Register("sleep.ms", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("ms", out var msStr))
                    throw new ArgumentException("sleep.ms requires ms=<millis>");
                if (!int.TryParse(msStr, out var ms)) ms = 0;
                Thread.Sleep(ms);
                ctx.Log($"slept {ms} ms");
            });

            Register("sleep.sec", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("sec", out var secStr))
                    throw new ArgumentException("sleep.sec requires sec=<seconds>");
                if (!int.TryParse(secStr, out var sec)) sec = 0;
                Thread.Sleep(sec * 1000);
                ctx.Log($"slept {sec} sec");
            });

            // Net module
            Register("net.download", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("url", out var url))
                    throw new ArgumentException("net.download requires url=<http(s)>");
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("net.download requires path=<file>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                using var http = new HttpClient();
                var data = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                System.IO.File.WriteAllBytes(full, data);
                ctx.Log($"downloaded {url} -> {path}");
            });

            // Loop module
            Register("loop.repeat", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("count", out var countStr))
                    throw new ArgumentException("loop.repeat requires count=<n>");
                if (!int.TryParse(countStr, out var count) || count < 0)
                    count = 0;
                cmd.Args.TryGetValue("body", out var body);
                if (string.IsNullOrWhiteSpace(body)) return;
                ctx.Environment["index"] = "0";
                for (int i = 0; i < count; i++)
                {
                    ctx.Environment["index"] = i.ToString();
                    RunInline(body!, ctx);
                }
            });

            Register("loop.foreach", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("items", out var items))
                    throw new ArgumentException("loop.foreach requires items=<a,b,c>");
                cmd.Args.TryGetValue("var", out var varName);
                if (string.IsNullOrWhiteSpace(varName)) varName = "item";
                cmd.Args.TryGetValue("sep", out var sep);
                if (string.IsNullOrEmpty(sep)) sep = ",";
                cmd.Args.TryGetValue("body", out var body);
                if (string.IsNullOrWhiteSpace(body)) return;
                var parts = items.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                int idx = 0;
                foreach (var p in parts)
                {
                    ctx.Environment["index"] = idx.ToString();
                    ctx.Environment[varName!] = p;
                    RunInline(body!, ctx);
                    idx++;
                }
            });

            // Conditional module
            Register("if", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("condition", out var condition))
                    throw new ArgumentException("if requires condition=<expr>");
                cmd.Args.TryGetValue("body", out var body);
                cmd.Args.TryGetValue("else", out var elseBody);
                
                var result = EvaluateCondition(condition, ctx);
                if (result && !string.IsNullOrWhiteSpace(body))
                    RunInline(body!, ctx);
                else if (!result && !string.IsNullOrWhiteSpace(elseBody))
                    RunInline(elseBody!, ctx);
            });

            // Include module
            Register("include", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("include requires path=<script.wflow>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                if (!System.IO.File.Exists(full))
                    throw new System.IO.FileNotFoundException($"Include file not found: {path}");
                
                WinFlow.Core.Parsing.IParser parser = new WinFlow.Core.Parsing.WinFlowParser();
                var tasks = parser.Parse(full);
                var exec = new TaskExecutor();
                exec.Run(tasks, ctx);
            });
        }

        private static bool IsTrue(string value)
        {
            return value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EvaluateCondition(string condition, ExecutionContext ctx)
        {
            // Simple condition evaluator for: ==, !=, >, <, exists
            condition = condition.Trim();
            
            // exists operator
            if (condition.StartsWith("exists ", StringComparison.OrdinalIgnoreCase))
            {
                var pathExpr = condition.Substring(7).Trim();
                pathExpr = Expand(pathExpr, ctx);
                var full = System.IO.Path.IsPathRooted(pathExpr)
                    ? pathExpr
                    : System.IO.Path.Combine(ctx.WorkingDirectory, pathExpr);
                return System.IO.File.Exists(full) || System.IO.Directory.Exists(full);
            }

            // binary operators
            string left, right, op;
            if (condition.Contains(" == "))
            {
                var parts = condition.Split(new[] { " == " }, 2, StringSplitOptions.None);
                left = Expand(parts[0].Trim(), ctx);
                right = Expand(parts[1].Trim(), ctx);
                return left == right;
            }
            else if (condition.Contains(" != "))
            {
                var parts = condition.Split(new[] { " != " }, 2, StringSplitOptions.None);
                left = Expand(parts[0].Trim(), ctx);
                right = Expand(parts[1].Trim(), ctx);
                return left != right;
            }
            else if (condition.Contains(" > "))
            {
                var parts = condition.Split(new[] { " > " }, 2, StringSplitOptions.None);
                left = Expand(parts[0].Trim(), ctx);
                right = Expand(parts[1].Trim(), ctx);
                if (double.TryParse(left, out var l) && double.TryParse(right, out var r))
                    return l > r;
                return string.Compare(left, right, StringComparison.Ordinal) > 0;
            }
            else if (condition.Contains(" < "))
            {
                var parts = condition.Split(new[] { " < " }, 2, StringSplitOptions.None);
                left = Expand(parts[0].Trim(), ctx);
                right = Expand(parts[1].Trim(), ctx);
                if (double.TryParse(left, out var l) && double.TryParse(right, out var r))
                    return l < r;
                return string.Compare(left, right, StringComparison.Ordinal) < 0;
            }

            // fallback: treat as boolean string
            var val = Expand(condition, ctx);
            return IsTrue(val);
        }

        private static RegistryKey ResolveHive(string hive)
        {
            return hive.ToUpperInvariant() switch
            {
                "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                _ => Registry.CurrentUser
            };
        }

        private static RegistryValueKind ResolveValueKind(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return RegistryValueKind.String;
            return type.ToUpperInvariant() switch
            {
                "DWORD" => RegistryValueKind.DWord,
                "QWORD" => RegistryValueKind.QWord,
                "MULTI" or "MULTI_SZ" => RegistryValueKind.MultiString,
                "EXPAND" or "EXPAND_SZ" => RegistryValueKind.ExpandString,
                _ => RegistryValueKind.String
            };
        }
    }
}