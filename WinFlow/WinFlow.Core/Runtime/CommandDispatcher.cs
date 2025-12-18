using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Win32;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Model;

namespace WinFlow.Core.Runtime
{
    public class CommandDispatcher
    {
        private static readonly Dictionary<string, Model.FlowFunction> GlobalFunctions = 
            new(StringComparer.OrdinalIgnoreCase);
        
        public static void RegisterFunction(Model.FlowFunction func)
        {
            GlobalFunctions[func.Name] = func;
        }
        
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

            Register("isset", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("var", out var varName))
                    throw new ArgumentException("isset requires var=<name>");
                
                var exists = ctx.Environment.ContainsKey(varName);
                var resultVar = cmd.Args.TryGetValue("result", out var rv) ? rv : "ISSET";
                ctx.Environment[resultVar] = exists.ToString().ToLowerInvariant();
                ctx.Log($"isset {varName} = {exists}");
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

            Register("file.read", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.read requires path=<file>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                if (!System.IO.File.Exists(full))
                    throw new ArgumentException($"file.read: file not found: {path}");
                var content = System.IO.File.ReadAllText(full);
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = content;
                else
                    ctx.Log(content);
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

            // String module
            Register("string.replace", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("string.replace requires text=<string>");
                if (!cmd.Args.TryGetValue("from", out var from))
                    throw new ArgumentException("string.replace requires from=<pattern>");
                if (!cmd.Args.TryGetValue("to", out var to))
                    throw new ArgumentException("string.replace requires to=<replacement>");
                var result = text.Replace(from, to);
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            Register("string.contains", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("string.contains requires text=<string>");
                if (!cmd.Args.TryGetValue("pattern", out var pattern))
                    throw new ArgumentException("string.contains requires pattern=<string>");
                var result = text.Contains(pattern) ? "true" : "false";
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            Register("string.length", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("string.length requires text=<string>");
                var result = text.Length.ToString();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            Register("string.upper", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("string.upper requires text=<string>");
                var result = text.ToUpperInvariant();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            Register("string.lower", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("string.lower requires text=<string>");
                var result = text.ToLowerInvariant();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            Register("string.trim", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("string.trim requires text=<string>");
                var result = text.Trim();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            // JSON module
            Register("json.parse", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("json.parse requires text=<json>");
                try
                {
                    var doc = JsonDocument.Parse(text);
                    var json = doc.RootElement.GetRawText();
                    if (cmd.Args.TryGetValue("var", out var varName))
                        ctx.Environment[varName] = json;
                    else
                        ctx.Log(json);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"json.parse: invalid JSON - {ex.Message}");
                }
            });

            Register("json.get", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("json.get requires text=<json>");
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("json.get requires path=<json.path>");
                try
                {
                    var doc = JsonDocument.Parse(text);
                    var paths = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    var current = doc.RootElement;
                    foreach (var p in paths)
                    {
                        if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(p, out var prop))
                            current = prop;
                        else if (current.ValueKind == JsonValueKind.Array && int.TryParse(p, out var idx))
                            current = current[idx];
                        else
                            throw new ArgumentException($"Path not found: {path}");
                    }
                    var result = current.GetRawText();
                    if (cmd.Args.TryGetValue("var", out var varName))
                        ctx.Environment[varName] = result;
                    else
                        ctx.Log(result);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"json.get failed: {ex.Message}");
                }
            });

            // HTTP module
            Register("http.get", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("url", out var url))
                    throw new ArgumentException("http.get requires url=<URL>");
                using var http = new HttpClient();
                var response = http.GetAsync(url).GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = content;
                else
                    ctx.Log(content);
            });

            Register("http.post", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("url", out var url))
                    throw new ArgumentException("http.post requires url=<URL>");
                cmd.Args.TryGetValue("body", out var body);
                using var http = new HttpClient();
                var content = new StringContent(body ?? "", System.Text.Encoding.UTF8, "application/json");
                var response = http.PostAsync(url, content).GetAwaiter().GetResult();
                var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            Register("http.put", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("url", out var url))
                    throw new ArgumentException("http.put requires url=<URL>");
                cmd.Args.TryGetValue("body", out var body);
                using var http = new HttpClient();
                var content = new StringContent(body ?? "", System.Text.Encoding.UTF8, "application/json");
                var response = http.PutAsync(url, content).GetAwaiter().GetResult();
                var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = result;
                else
                    ctx.Log(result);
            });

            // Array module
            Register("array.split", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("array.split requires text=<string>");
                cmd.Args.TryGetValue("sep", out var sep);
                sep ??= ",";
                var parts = text.Split(sep);
                var arrayJson = JsonSerializer.Serialize(parts);
                if (cmd.Args.TryGetValue("var", out var varName))
                    ctx.Environment[varName] = arrayJson;
                else
                    ctx.Log(arrayJson);
            });

            Register("array.join", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("array", out var arrayJson))
                    throw new ArgumentException("array.join requires array=<json_array>");
                cmd.Args.TryGetValue("sep", out var sep);
                sep ??= ",";
                try
                {
                    var arr = JsonSerializer.Deserialize<string[]>(arrayJson);
                    var result = string.Join(sep, arr ?? Array.Empty<string>());
                    if (cmd.Args.TryGetValue("var", out var varName))
                        ctx.Environment[varName] = result;
                    else
                        ctx.Log(result);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"array.join failed: {ex.Message}");
                }
            });

            Register("array.length", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("array", out var arrayJson))
                    throw new ArgumentException("array.length requires array=<json_array>");
                try
                {
                    var doc = JsonDocument.Parse(arrayJson);
                    var length = doc.RootElement.GetArrayLength().ToString();
                    if (cmd.Args.TryGetValue("var", out var varName))
                        ctx.Environment[varName] = length;
                    else
                        ctx.Log(length);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"array.length failed: {ex.Message}");
                }
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

            // Import module (register functions without executing top-level code)
            Register("import", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("import requires path=<script.wflow>");
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                if (!System.IO.File.Exists(full))
                    throw new System.IO.FileNotFoundException($"Import file not found: {path}");

                WinFlow.Core.Parsing.IParser parser = new WinFlow.Core.Parsing.WinFlowParser();
                parser.Parse(full); // functions are registered during parse
                ctx.Log($"imported {full}");
            });

            // Config (INI) module
            Register("config.create", (cmd, ctx) =>
            {
                var cfg = new ConfigData();
                var id = StoreObject(ctx, cfg);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CONFIG";
                ctx.Environment[varName] = id;
                ctx.Log($"config.create -> {id}");
            });

            Register("config.read", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("config.read requires file=<path>");
                var full = System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(ctx.WorkingDirectory, file);
                if (!System.IO.File.Exists(full))
                    throw new System.IO.FileNotFoundException($"Config file not found: {file}");

                var cfg = ParseIni(System.IO.File.ReadAllLines(full));
                var id = StoreObject(ctx, cfg);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CONFIG";
                ctx.Environment[varName] = id;
                ctx.Log($"config.read {file} -> {id}");
            });

            Register("config.get", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("config", out var cfgId))
                    throw new ArgumentException("config.get requires config=<id>");
                if (!cmd.Args.TryGetValue("section", out var section))
                    throw new ArgumentException("config.get requires section=<name>");
                if (!cmd.Args.TryGetValue("key", out var key))
                    throw new ArgumentException("config.get requires key=<name>");
                cmd.Args.TryGetValue("default", out var defaultVal);

                if (!TryGetObject<ConfigData>(ctx, cfgId, out var cfg))
                    throw new ArgumentException($"config id not found: {cfgId}");

                var value = defaultVal ?? string.Empty;
                if (cfg.Sections.TryGetValue(section, out var sec) && sec.TryGetValue(key, out var found))
                    value = found;

                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CONFIG_VALUE";
                ctx.Environment[varName] = value;
                ctx.Log($"config.get [{section}] {key} -> {value}");
            });

            Register("config.set", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("config", out var cfgId))
                    throw new ArgumentException("config.set requires config=<id>");
                if (!cmd.Args.TryGetValue("section", out var section))
                    throw new ArgumentException("config.set requires section=<name>");
                if (!cmd.Args.TryGetValue("key", out var key))
                    throw new ArgumentException("config.set requires key=<name>");
                if (!cmd.Args.TryGetValue("value", out var value))
                    throw new ArgumentException("config.set requires value=<val>");

                if (!TryGetObject<ConfigData>(ctx, cfgId, out var cfg))
                    throw new ArgumentException($"config id not found: {cfgId}");

                if (!cfg.Sections.TryGetValue(section, out var sec))
                {
                    sec = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    cfg.Sections[section] = sec;
                }
                sec[key] = value;

                var varName = cmd.Args.TryGetValue("var", out var v) ? v : null;
                if (!string.IsNullOrWhiteSpace(varName))
                    ctx.Environment[varName!] = cfgId;
                ctx.Log($"config.set [{section}] {key} = {value}");
            });

            Register("config.write", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("config", out var cfgId))
                    throw new ArgumentException("config.write requires config=<id>");
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("config.write requires file=<path>");
                var full = System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(ctx.WorkingDirectory, file);

                if (!TryGetObject<ConfigData>(ctx, cfgId, out var cfg))
                    throw new ArgumentException($"config id not found: {cfgId}");

                var lines = new List<string>();
                foreach (var section in cfg.Sections)
                {
                    lines.Add($"[{section.Key}]");
                    foreach (var kv in section.Value)
                        lines.Add($"{kv.Key}={kv.Value}");
                    lines.Add(string.Empty);
                }
                System.IO.File.WriteAllLines(full, lines);
                ctx.Log($"config.write -> {full}");
            });

            // CSV module
            Register("csv.read", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("csv.read requires file=<path>");
                var full = System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(ctx.WorkingDirectory, file);
                if (!System.IO.File.Exists(full))
                    throw new System.IO.FileNotFoundException($"CSV file not found: {file}");
                var hasHeader = cmd.Args.TryGetValue("has_header", out var hh) && hh.Equals("true", StringComparison.OrdinalIgnoreCase);
                var lines = System.IO.File.ReadAllLines(full);
                var data = new CsvData { HasHeader = hasHeader };

                if (lines.Length > 0)
                {
                    var startIdx = 0;
                    if (hasHeader)
                    {
                        data.Headers = lines[0].Split(',', StringSplitOptions.TrimEntries).ToList();
                        startIdx = 1;
                    }
                    else
                    {
                        var firstCols = lines[0].Split(',', StringSplitOptions.TrimEntries).Length;
                        for (int i = 0; i < firstCols; i++) data.Headers.Add($"col{i+1}");
                    }

                    for (int i = startIdx; i < lines.Length; i++)
                    {
                        var cols = lines[i].Split(',', StringSplitOptions.TrimEntries);
                        var rowId = $"csvrow_{Guid.NewGuid():N}";
                        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < data.Headers.Count && c < cols.Length; c++)
                            row[data.Headers[c]] = cols[c];
                        data.Rows[rowId] = row;
                        data.RowIds.Add(rowId);
                    }
                }

                var dataId = StoreObject(ctx, data);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CSV";
                ctx.Environment[varName] = string.Join(",", data.RowIds);
                ctx.Environment[$"{varName}_id"] = dataId;
                ctx.Log($"csv.read {file} rows={data.RowIds.Count}");
            });

            Register("csv.create", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("headers", out var headers))
                    throw new ArgumentException("csv.create requires headers=<a,b,c>");
                var data = new CsvData
                {
                    Headers = headers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                    HasHeader = true
                };
                var dataId = StoreObject(ctx, data);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CSV";
                ctx.Environment[varName] = dataId;
                ctx.Environment[$"{varName}_id"] = dataId;
                ctx.Log($"csv.create {string.Join(",", data.Headers)}");
            });

            Register("csv.add_row", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("data", out var dataArg))
                    throw new ArgumentException("csv.add_row requires data=<id>");
                if (!cmd.Args.TryGetValue("values", out var valuesStr))
                    throw new ArgumentException("csv.add_row requires values=<v1,v2>");
                var data = ResolveCsvData(ctx, dataArg, out var rowIdsRef);
                var cols = valuesStr.Split(',', StringSplitOptions.TrimEntries);
                var rowId = $"csvrow_{Guid.NewGuid():N}";
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < data.Headers.Count && i < cols.Length; i++)
                    row[data.Headers[i]] = cols[i];
                data.Rows[rowId] = row;
                data.RowIds.Add(rowId);
                rowIdsRef.Add(rowId);
                if (cmd.Args.TryGetValue("var", out var v))
                    ctx.Environment[v] = string.Join(",", rowIdsRef);
                ctx.Log($"csv.add_row -> {rowId}");
            });

            Register("csv.get_field", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("row", out var rowId))
                    throw new ArgumentException("csv.get_field requires row=<rowId>");
                if (!cmd.Args.TryGetValue("field", out var field))
                    throw new ArgumentException("csv.get_field requires field=<name>");
                var row = ResolveCsvRow(ctx, rowId, out _);
                var value = row.TryGetValue(field, out var v) ? v : string.Empty;
                var varName = cmd.Args.TryGetValue("var", out var vn) ? vn : "CSV_FIELD";
                ctx.Environment[varName] = value;
                ctx.Log($"csv.get_field {field} -> {value}");
            });

            Register("csv.write", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("data", out var dataArg))
                    throw new ArgumentException("csv.write requires data=<id>");
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("csv.write requires file=<path>");
                var full = System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(ctx.WorkingDirectory, file);

                var data = ResolveCsvData(ctx, dataArg, out var rowsToWrite);
                var lines = new List<string>();
                if (data.HasHeader && data.Headers.Count > 0)
                    lines.Add(string.Join(",", data.Headers));
                foreach (var rowId in rowsToWrite)
                {
                    if (!data.Rows.TryGetValue(rowId, out var row)) continue;
                    var cols = data.Headers.Select(h => row.TryGetValue(h, out var v) ? v : string.Empty);
                    lines.Add(string.Join(",", cols));
                }
                System.IO.File.WriteAllLines(full, lines);
                ctx.Log($"csv.write -> {full}");
            });

            Register("csv.filter", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("data", out var dataArg))
                    throw new ArgumentException("csv.filter requires data=<id>");
                if (!cmd.Args.TryGetValue("column", out var column))
                    throw new ArgumentException("csv.filter requires column=<name>");
                if (!cmd.Args.TryGetValue("value", out var value))
                    throw new ArgumentException("csv.filter requires value=<val>");

                var data = ResolveCsvData(ctx, dataArg, out var rows);
                var filtered = new CsvData
                {
                    Headers = new List<string>(data.Headers),
                    HasHeader = data.HasHeader
                };
                foreach (var rowId in rows)
                {
                    if (!data.Rows.TryGetValue(rowId, out var row)) continue;
                    if (row.TryGetValue(column, out var v) && string.Equals(v, value, StringComparison.OrdinalIgnoreCase))
                    {
                        filtered.RowIds.Add(rowId);
                        filtered.Rows[rowId] = row;
                    }
                }
                var fid = StoreObject(ctx, filtered);
                var varName = cmd.Args.TryGetValue("var", out var vn) ? vn : "CSV_FILTERED";
                ctx.Environment[varName] = string.Join(",", filtered.RowIds);
                ctx.Environment[$"{varName}_id"] = fid;
                ctx.Log($"csv.filter {filtered.RowIds.Count} rows");
            });

            Register("csv.sort", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("data", out var dataArg))
                    throw new ArgumentException("csv.sort requires data=<id>");
                if (!cmd.Args.TryGetValue("column", out var column))
                    throw new ArgumentException("csv.sort requires column=<name>");
                var order = cmd.Args.TryGetValue("order", out var ord) ? ord.ToLowerInvariant() : "asc";

                var data = ResolveCsvData(ctx, dataArg, out var rows);
                var sortedRows = rows
                    .Select(id => new { id, value = data.Rows.TryGetValue(id, out var r) && r.TryGetValue(column, out var v) ? v : string.Empty })
                    .OrderBy(x => x.value, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (order == "desc") sortedRows.Reverse();

                var sorted = new CsvData
                {
                    Headers = new List<string>(data.Headers),
                    HasHeader = data.HasHeader,
                    RowIds = sortedRows.Select(x => x.id).ToList()
                };
                foreach (var id in sorted.RowIds)
                    sorted.Rows[id] = data.Rows[id];

                var sid = StoreObject(ctx, sorted);
                var varName = cmd.Args.TryGetValue("var", out var vn) ? vn : "CSV_SORTED";
                ctx.Environment[varName] = string.Join(",", sorted.RowIds);
                ctx.Environment[$"{varName}_id"] = sid;
                ctx.Log($"csv.sort {sorted.RowIds.Count} rows order={order}");
            });

            // Registry (friendly aliases with defaults)
            Register("registry.get", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("registry.get requires key=<path>");
                if (!cmd.Args.TryGetValue("value", out var valueName))
                    throw new ArgumentException("registry.get requires value=<name>");
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                cmd.Args.TryGetValue("default", out var defaultVal);

                var baseKey = ResolveHive(hive);
                using var key = baseKey.OpenSubKey(keyPath);
                var val = key?.GetValue(valueName, defaultVal ?? string.Empty)?.ToString() ?? defaultVal ?? string.Empty;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "REG_VALUE";
                ctx.Environment[varName] = val;
                ctx.Log($"registry.get [{hive}] {keyPath} {valueName} -> {val}");
            });

            Register("registry.set", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("registry.set requires key=<path>");
                if (!cmd.Args.TryGetValue("value", out var valueName))
                    throw new ArgumentException("registry.set requires value=<name>");
                if (!cmd.Args.TryGetValue("data", out var data))
                    throw new ArgumentException("registry.set requires data=<value>");
                cmd.Args.TryGetValue("type", out var type);
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                var baseKey = ResolveHive(hive);
                using var key = baseKey.CreateSubKey(keyPath);
                var kind = ResolveValueKind(type);
                key!.SetValue(valueName, data, kind);
                ctx.Log($"registry.set [{hive}] {keyPath} {valueName} = {data}");
            });

            Register("registry.exists", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("registry.exists requires key=<path>");
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                var baseKey = ResolveHive(hive);
                var exists = baseKey.OpenSubKey(keyPath) != null;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "REG_EXISTS";
                ctx.Environment[varName] = exists.ToString().ToLowerInvariant();
                ctx.Log($"registry.exists [{hive}] {keyPath} = {exists}");
            });

            Register("registry.delete", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("key", out var keyPath))
                    throw new ArgumentException("registry.delete requires key=<path>");
                var hive = cmd.Args.TryGetValue("hive", out var hv) ? hv : "HKCU";
                var baseKey = ResolveHive(hive);
                if (cmd.Args.TryGetValue("value", out var valueName))
                {
                    using var key = baseKey.OpenSubKey(keyPath, writable: true);
                    key?.DeleteValue(valueName, throwOnMissingValue: false);
                    ctx.Log($"registry.delete value [{hive}] {keyPath} {valueName}");
                }
                else
                {
                    try { baseKey.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false); } catch { }
                    ctx.Log($"registry.delete key [{hive}] {keyPath}");
                }
            });

            // XML module
            Register("xml.create", (cmd, ctx) =>
            {
                var doc = new XDocument(new XElement("root"));
                var id = StoreObject(ctx, doc);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "XML";
                ctx.Environment[varName] = id;
                ctx.Log($"xml.create -> {id}");
            });

            Register("xml.parse", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("xml.parse requires file=<path>");
                var full = System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(ctx.WorkingDirectory, file);
                if (!System.IO.File.Exists(full))
                    throw new System.IO.FileNotFoundException($"XML file not found: {file}");
                var doc = XDocument.Load(full);
                var id = StoreObject(ctx, doc);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "XML";
                ctx.Environment[varName] = id;
                ctx.Log($"xml.parse {file} -> {id}");
            });

            Register("xml.add_element", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("parent", out var parentId))
                    throw new ArgumentException("xml.add_element requires parent=<id>");
                if (!cmd.Args.TryGetValue("tag", out var tag))
                    throw new ArgumentException("xml.add_element requires tag=<name>");

                if (!TryResolveXmlNode(ctx, parentId, out var doc, out var parent))
                    throw new ArgumentException($"xml parent not found: {parentId}");

                var elem = new XElement(tag);
                if (parent != null)
                    parent.Add(elem);
                else
                    doc.Add(elem);

                var elemId = StoreObject(ctx, elem);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "XML_NODE";
                ctx.Environment[varName] = elemId;
                ctx.Log($"xml.add_element {tag} -> {elemId}");
            });

            Register("xml.set_attribute", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("element", out var elemId))
                    throw new ArgumentException("xml.set_attribute requires element=<id>");
                if (!cmd.Args.TryGetValue("name", out var name))
                    throw new ArgumentException("xml.set_attribute requires name=<attr>");
                if (!cmd.Args.TryGetValue("value", out var value))
                    throw new ArgumentException("xml.set_attribute requires value=<val>");

                if (!TryGetObject<XElement>(ctx, elemId, out var elem))
                    throw new ArgumentException($"xml element not found: {elemId}");
                elem.SetAttributeValue(name, value);
                ctx.Log($"xml.set_attribute {name}={value}");
            });

            Register("xml.get", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("data", out var dataId))
                    throw new ArgumentException("xml.get requires data=<id>");
                if (!cmd.Args.TryGetValue("xpath", out var xpath))
                    throw new ArgumentException("xml.get requires xpath=<expr>");
                if (!TryResolveXmlNode(ctx, dataId, out var doc, out var node))
                    throw new ArgumentException($"xml data not found: {dataId}");
                var target = (node ?? doc.Root)?.XPathSelectElement(xpath);
                var value = target?.Value ?? string.Empty;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "XML_VALUE";
                ctx.Environment[varName] = value;
                ctx.Log($"xml.get {xpath} -> {value}");
            });

            Register("xml.write", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("data", out var dataId))
                    throw new ArgumentException("xml.write requires data=<id>");
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("xml.write requires file=<path>");
                var full = System.IO.Path.IsPathRooted(file)
                    ? file
                    : System.IO.Path.Combine(ctx.WorkingDirectory, file);
                if (!TryResolveXmlNode(ctx, dataId, out var doc, out _))
                    throw new ArgumentException($"xml data not found: {dataId}");
                doc.Save(full);
                ctx.Log($"xml.write -> {full}");
            });

            // Async module
            Register("async.start", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("command", out var command))
                    throw new ArgumentException("async.start requires command=<...>");
                var id = cmd.Args.TryGetValue("var", out var v) ? v : $"task_{Guid.NewGuid():N}";
                var task = Task.Run(() => RunInline(command, ctx));
                ctx.AsyncTasks[id] = task;
                ctx.Environment[id] = id;
                ctx.Log($"async.start {id}");
            });

            Register("async.wait", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("task", out var taskId))
                    throw new ArgumentException("async.wait requires task=<id>");
                if (!ctx.AsyncTasks.TryGetValue(taskId, out var task))
                    throw new ArgumentException($"task not found: {taskId}");
                var timeoutMs = cmd.Args.TryGetValue("timeout", out var t) && int.TryParse(t, out var ms) ? ms * 1000 : -1;
                if (timeoutMs >= 0)
                    task.Wait(timeoutMs);
                else
                    task.Wait();
                var status = task.IsCompleted ? "completed" : task.Status.ToString().ToLowerInvariant();
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "ASYNC_STATUS";
                ctx.Environment[varName] = status;
                ctx.Log($"async.wait {taskId} -> {status}");
            });

            Register("async.wait_all", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("tasks", out var tasksStr))
                    throw new ArgumentException("async.wait_all requires tasks=<id1,id2>");
                var ids = tasksStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var tasks = ids.Select(id => ctx.AsyncTasks.TryGetValue(id, out var t) ? t : null).Where(t => t != null).ToArray();
                Task.WaitAll(tasks!);
                ctx.Log($"async.wait_all {ids.Length} tasks");
            });

            Register("async.status", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("task", out var taskId))
                    throw new ArgumentException("async.status requires task=<id>");
                if (!ctx.AsyncTasks.TryGetValue(taskId, out var task))
                    throw new ArgumentException($"task not found: {taskId}");
                var status = task.Status.ToString().ToLowerInvariant();
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "ASYNC_STATUS";
                ctx.Environment[varName] = status;
                ctx.Log($"async.status {taskId} -> {status}");
            });

            // Input module
            Register("input.text", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("prompt", out var prompt))
                    throw new ArgumentException("input.text requires prompt=<text>");
                cmd.Args.TryGetValue("default", out var def);
                Console.Write(prompt);
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) input = def ?? string.Empty;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "INPUT";
                ctx.Environment[varName] = input;
                ctx.Log($"input.text -> {input}");
            });

            Register("input.password", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("prompt", out var prompt))
                    throw new ArgumentException("input.password requires prompt=<text>");
                Console.Write(prompt);
                var sb = new System.Text.StringBuilder();
                ConsoleKeyInfo key;
                while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
                {
                    if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                    {
                        sb.Length--;
                        continue;
                    }
                    sb.Append(key.KeyChar);
                }
                Console.WriteLine();
                var input = sb.ToString();
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "INPUT";
                ctx.Environment[varName] = input;
                ctx.Log("input.password captured");
            });

            Register("input.confirm", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("prompt", out var prompt))
                    throw new ArgumentException("input.confirm requires prompt=<text>");
                var def = cmd.Args.TryGetValue("default", out var d) ? d : "no";
                Console.Write($"{prompt} (y/n) [{def}]: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) input = def;
                var result = input!.StartsWith("y", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CONFIRM";
                ctx.Environment[varName] = result;
                ctx.Log($"input.confirm -> {result}");
            });

            Register("input.choice", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("prompt", out var prompt))
                    throw new ArgumentException("input.choice requires prompt=<text>");
                if (!cmd.Args.TryGetValue("options", out var options))
                    throw new ArgumentException("input.choice requires options=<a,b,c>");
                var opts = options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                Console.WriteLine(prompt);
                for (int i = 0; i < opts.Length; i++)
                    Console.WriteLine($"[{i + 1}] {opts[i]}");
                Console.Write("Select: ");
                var input = Console.ReadLine();
                if (!int.TryParse(input, out var idx) || idx < 1 || idx > opts.Length)
                    idx = 1;
                var choice = opts[idx - 1];
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "CHOICE";
                ctx.Environment[varName] = choice;
                ctx.Log($"input.choice -> {choice}");
            });

            // Try module (error handling)
            Register("try", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("body", out var body))
                    throw new ArgumentException("try requires body=<command>");
                cmd.Args.TryGetValue("catch", out var catchBody);
                
                try
                {
                    RunInline(body!, ctx);
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrWhiteSpace(catchBody))
                    {
                        ctx.Environment["_error"] = ex.Message;
                        RunInline(catchBody!, ctx);
                    }
                    else
                    {
                        ctx.LogWarning?.Invoke($"Error caught: {ex.Message}");
                    }
                }
            });

            Register("__define_func__", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("name", out var funcName))
                    throw new ArgumentException("define requires name=<function_name>");
                
                if (!GlobalFunctions.TryGetValue(funcName, out var func))
                    throw new InvalidOperationException($"Function '{funcName}' not found in global registry");
                
                ctx.Functions[funcName] = func;
                ctx.Log($"Function '{funcName}' defined with {func.Parameters.Count} parameter(s)");
            });

            Register("call", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("name", out var funcNameArg))
                    throw new ArgumentException("call requires name=<function_name>");
                
                if (!ctx.Functions.TryGetValue(funcNameArg!, out var func))
                    throw new InvalidOperationException($"Function '{funcNameArg}' not defined");
                
                // Create a new context scope for the function
                var funcContext = new ExecutionContext
                {
                    DryRun = ctx.DryRun,
                    Verbose = ctx.Verbose,
                    WorkingDirectory = ctx.WorkingDirectory,
                    Log = ctx.Log,
                    LogError = ctx.LogError,
                    LogWarning = ctx.LogWarning,
                    LogFile = ctx.LogFile
                };
                
                // Copy parent environment
                foreach (var kvp in ctx.Environment)
                    funcContext.Environment[kvp.Key] = kvp.Value;
                
                // Bind positional arguments to parameters
                int argIdx = 0;
                foreach (var param in func.Parameters)
                {
                    var argKey = $"arg{argIdx}";
                    if (cmd.Args.TryGetValue(argKey, out var value))
                    {
                        funcContext.Environment[param] = Expand(value, ctx);
                    }
                    argIdx++;
                }
                
                // Execute function commands
                var executor = new TaskExecutor();
                var step = new FlowStep { Commands = func.Commands };
                var task = new FlowTask { Name = funcNameArg, Steps = new List<FlowStep> { step } };
                executor.Run(new List<FlowTask> { task }, funcContext);
                
                // Copy modified environment back (except parameters which are local)
                foreach (var kvp in funcContext.Environment)
                {
                    if (!func.Parameters.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        ctx.Environment[kvp.Key] = kvp.Value;
                    }
                }
                
                // Propagate return value
                if (funcContext.ReturnValue != null)
                {
                    ctx.ReturnValue = funcContext.ReturnValue;
                    ctx.Environment["__RETURN__"] = funcContext.ReturnValue;
                }
            });

            // Return statement
            Register("return", (cmd, ctx) =>
            {
                if (cmd.Args.TryGetValue("value", out var value))
                {
                    ctx.ReturnValue = Expand(value, ctx);
                }
                else if (cmd.Args.Count > 0)
                {
                    // return ${variable} or return literal
                    var firstArg = cmd.Args.First().Value;
                    ctx.ReturnValue = Expand(firstArg, ctx);
                }
                else
                {
                    ctx.ReturnValue = string.Empty;
                }
                
                ctx.Log($"return {ctx.ReturnValue}");
            });

            // Math operations
            Register("math.add", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("a", out var aStr) || !cmd.Args.TryGetValue("b", out var bStr))
                    throw new ArgumentException("math.add requires a=<num> b=<num>");
                
                if (!double.TryParse(aStr, out var a) || !double.TryParse(bStr, out var b))
                    throw new ArgumentException("math.add requires numeric arguments");
                
                var result = a + b;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.add {a} + {b} = {result}");
            });

            Register("math.subtract", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("a", out var aStr) || !cmd.Args.TryGetValue("b", out var bStr))
                    throw new ArgumentException("math.subtract requires a=<num> b=<num>");
                
                if (!double.TryParse(aStr, out var a) || !double.TryParse(bStr, out var b))
                    throw new ArgumentException("math.subtract requires numeric arguments");
                
                var result = a - b;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.subtract {a} - {b} = {result}");
            });

            Register("math.multiply", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("a", out var aStr) || !cmd.Args.TryGetValue("b", out var bStr))
                    throw new ArgumentException("math.multiply requires a=<num> b=<num>");
                
                if (!double.TryParse(aStr, out var a) || !double.TryParse(bStr, out var b))
                    throw new ArgumentException("math.multiply requires numeric arguments");
                
                var result = a * b;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.multiply {a} * {b} = {result}");
            });

            Register("math.divide", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("a", out var aStr) || !cmd.Args.TryGetValue("b", out var bStr))
                    throw new ArgumentException("math.divide requires a=<num> b=<num>");
                
                if (!double.TryParse(aStr, out var a) || !double.TryParse(bStr, out var b))
                    throw new ArgumentException("math.divide requires numeric arguments");
                
                if (Math.Abs(b) < 0.000001)
                    throw new DivideByZeroException("Cannot divide by zero");
                
                var result = a / b;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.divide {a} / {b} = {result}");
            });

            Register("math.round", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("value", out var valueStr))
                    throw new ArgumentException("math.round requires value=<num>");
                
                if (!double.TryParse(valueStr, out var value))
                    throw new ArgumentException("math.round requires numeric value");
                
                var decimals = 0;
                if (cmd.Args.TryGetValue("decimals", out var decStr))
                    int.TryParse(decStr, out decimals);
                
                var result = Math.Round(value, decimals);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.round {value} (decimals={decimals}) = {result}");
            });

            Register("math.floor", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("value", out var valueStr))
                    throw new ArgumentException("math.floor requires value=<num>");
                
                if (!double.TryParse(valueStr, out var value))
                    throw new ArgumentException("math.floor requires numeric value");
                
                var result = Math.Floor(value);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.floor {value} = {result}");
            });

            Register("math.ceil", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("value", out var valueStr))
                    throw new ArgumentException("math.ceil requires value=<num>");
                
                if (!double.TryParse(valueStr, out var value))
                    throw new ArgumentException("math.ceil requires numeric value");
                
                var result = Math.Ceiling(value);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "MATH_RESULT";
                ctx.Environment[varName] = result.ToString();
                ctx.Log($"math.ceil {value} = {result}");
            });

            // String operations
            Register("string.concat", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("left", out var left))
                    left = string.Empty;
                if (!cmd.Args.TryGetValue("right", out var right))
                    right = string.Empty;
                
                var separator = cmd.Args.TryGetValue("sep", out var sep) ? sep : "";
                var result = left + separator + right;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "STRING_RESULT";
                ctx.Environment[varName] = result;
                ctx.Log($"string.concat '{left}' + '{right}' = '{result}'");
            });

            Register("string.format", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("template", out var template))
                    throw new ArgumentException("string.format requires template=<string>");
                
                // Simple placeholder replacement: {0}, {1}, {2}, etc.
                var result = template;
                for (int i = 0; i < 10; i++)
                {
                    var placeholder = "{" + i + "}";
                    if (cmd.Args.TryGetValue(i.ToString(), out var value))
                    {
                        result = result.Replace(placeholder, value);
                    }
                }
                
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "STRING_RESULT";
                ctx.Environment[varName] = result;
                ctx.Log($"string.format result = '{result}'");
            });

            // DateTime module
            Register("datetime.now", (cmd, ctx) =>
            {
                var now = DateTime.Now;
                var format = cmd.Args.TryGetValue("format", out var fmt) ? fmt : "o"; // ISO 8601
                var result = now.ToString(format);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "DATETIME";
                ctx.Environment[varName] = result;
                ctx.Log($"datetime.now = {result}");
            });

            Register("datetime.format", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("date", out var dateStr))
                    throw new ArgumentException("datetime.format requires date=<value>");
                
                var format = cmd.Args.TryGetValue("format", out var fmt) ? fmt : "o";
                
                if (DateTime.TryParse(dateStr, out var date))
                {
                    var result = date.ToString(format);
                    var varName = cmd.Args.TryGetValue("var", out var v) ? v : "DATETIME";
                    ctx.Environment[varName] = result;
                    ctx.Log($"datetime.format = {result}");
                }
                else
                {
                    throw new ArgumentException($"Invalid date format: {dateStr}");
                }
            });

            Register("datetime.parse", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("datetime.parse requires text=<value>");
                
                if (DateTime.TryParse(text, out var date))
                {
                    var result = date.ToString("o");
                    var varName = cmd.Args.TryGetValue("var", out var v) ? v : "DATETIME";
                    ctx.Environment[varName] = result;
                    ctx.Log($"datetime.parse = {result}");
                }
                else
                {
                    throw new ArgumentException($"Cannot parse date: {text}");
                }
            });

            Register("datetime.add", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("date", out var dateStr))
                    throw new ArgumentException("datetime.add requires date=<value>");
                
                if (!DateTime.TryParse(dateStr, out var date))
                    throw new ArgumentException($"Invalid date: {dateStr}");
                
                if (cmd.Args.TryGetValue("days", out var daysStr) && int.TryParse(daysStr, out var days))
                    date = date.AddDays(days);
                if (cmd.Args.TryGetValue("hours", out var hoursStr) && int.TryParse(hoursStr, out var hours))
                    date = date.AddHours(hours);
                if (cmd.Args.TryGetValue("minutes", out var minsStr) && int.TryParse(minsStr, out var mins))
                    date = date.AddMinutes(mins);
                if (cmd.Args.TryGetValue("seconds", out var secsStr) && int.TryParse(secsStr, out var secs))
                    date = date.AddSeconds(secs);
                
                var result = date.ToString("o");
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "DATETIME";
                ctx.Environment[varName] = result;
                ctx.Log($"datetime.add = {result}");
            });

            Register("datetime.diff", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("start", out var startStr) || !cmd.Args.TryGetValue("end", out var endStr))
                    throw new ArgumentException("datetime.diff requires start=<date> end=<date>");
                
                if (!DateTime.TryParse(startStr, out var start) || !DateTime.TryParse(endStr, out var end))
                    throw new ArgumentException("Invalid date format in datetime.diff");
                
                var span = end - start;
                var unit = cmd.Args.TryGetValue("unit", out var u) ? u : "seconds";
                
                double result = unit.ToLowerInvariant() switch
                {
                    "days" => span.TotalDays,
                    "hours" => span.TotalHours,
                    "minutes" => span.TotalMinutes,
                    "seconds" => span.TotalSeconds,
                    "milliseconds" => span.TotalMilliseconds,
                    _ => span.TotalSeconds
                };
                
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "DATETIME_DIFF";
                ctx.Environment[varName] = result.ToString("F2");
                ctx.Log($"datetime.diff = {result:F2} {unit}");
            });

            // Path module
            Register("path.join", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("parts", out var partsStr))
                    throw new ArgumentException("path.join requires parts=<comma-separated>");
                
                var parts = partsStr.Split(',').Select(p => p.Trim()).ToArray();
                var result = System.IO.Path.Combine(parts);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "PATH";
                ctx.Environment[varName] = result;
                ctx.Log($"path.join = {result}");
            });

            Register("path.dirname", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("path.dirname requires path=<value>");
                
                var result = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "PATH";
                ctx.Environment[varName] = result;
                ctx.Log($"path.dirname = {result}");
            });

            Register("path.basename", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("path.basename requires path=<value>");
                
                var result = System.IO.Path.GetFileName(path);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "PATH";
                ctx.Environment[varName] = result;
                ctx.Log($"path.basename = {result}");
            });

            Register("path.extension", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("path.extension requires path=<value>");
                
                var result = System.IO.Path.GetExtension(path);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "PATH";
                ctx.Environment[varName] = result;
                ctx.Log($"path.extension = {result}");
            });

            Register("path.exists", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("path.exists requires path=<value>");
                
                var exists = System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "PATH_EXISTS";
                ctx.Environment[varName] = exists.ToString().ToLowerInvariant();
                ctx.Log($"path.exists({path}) = {exists}");
            });

            Register("path.is_directory", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("path.is_directory requires path=<value>");
                
                var isDir = System.IO.Directory.Exists(path);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "IS_DIR";
                ctx.Environment[varName] = isDir.ToString().ToLowerInvariant();
                ctx.Log($"path.is_directory({path}) = {isDir}");
            });

            Register("path.normalize", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("path.normalize requires path=<value>");
                
                var result = System.IO.Path.GetFullPath(path);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "PATH";
                ctx.Environment[varName] = result;
                ctx.Log($"path.normalize = {result}");
            });

            // Log module
            Register("log.config", (cmd, ctx) =>
            {
                // Configure logging settings
                var level = cmd.Args.TryGetValue("level", out var l) ? l : "INFO";
                var file = cmd.Args.TryGetValue("file", out var f) ? f : null;
                var format = cmd.Args.TryGetValue("format", out var fmt) ? fmt : "[%TIME%] %LEVEL% - %MESSAGE%";
                
                ctx.Environment["__LOG_LEVEL__"] = level;
                if (file != null) ctx.Environment["__LOG_FILE__"] = file;
                ctx.Environment["__LOG_FORMAT__"] = format;
                ctx.Log($"log.config: level={level}, file={file}, format={format}");
            });

            Register("log.info", (cmd, ctx) =>
            {
                var message = cmd.Args.TryGetValue("message", out var m) ? m : cmd.Args.FirstOrDefault().Value ?? "";
                LogMessage(ctx, "INFO", message);
            });

            Register("log.debug", (cmd, ctx) =>
            {
                var message = cmd.Args.TryGetValue("message", out var m) ? m : cmd.Args.FirstOrDefault().Value ?? "";
                var level = ctx.Environment.TryGetValue("__LOG_LEVEL__", out var l) ? l : "INFO";
                if (level == "DEBUG" || ctx.Verbose)
                    LogMessage(ctx, "DEBUG", message);
            });

            Register("log.warning", (cmd, ctx) =>
            {
                var message = cmd.Args.TryGetValue("message", out var m) ? m : cmd.Args.FirstOrDefault().Value ?? "";
                LogMessage(ctx, "WARNING", message);
            });

            Register("log.error", (cmd, ctx) =>
            {
                var message = cmd.Args.TryGetValue("message", out var m) ? m : cmd.Args.FirstOrDefault().Value ?? "";
                LogMessage(ctx, "ERROR", message);
            });

            // Regex module
            Register("regex.match", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("pattern", out var pattern) || !cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("regex.match requires pattern=<regex> text=<value>");
                
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                var isMatch = regex.IsMatch(text);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "REGEX_MATCH";
                ctx.Environment[varName] = isMatch.ToString().ToLowerInvariant();
                ctx.Log($"regex.match = {isMatch}");
            });

            Register("regex.find", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("pattern", out var pattern) || !cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("regex.find requires pattern=<regex> text=<value>");
                
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                var matches = regex.Matches(text);
                var results = string.Join(",", matches.Select(m => m.Value));
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "REGEX_RESULTS";
                ctx.Environment[varName] = results;
                ctx.Log($"regex.find found {matches.Count} match(es)");
            });

            Register("regex.replace", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("pattern", out var pattern) || 
                    !cmd.Args.TryGetValue("replacement", out var replacement) ||
                    !cmd.Args.TryGetValue("text", out var text))
                    throw new ArgumentException("regex.replace requires pattern=<regex> replacement=<value> text=<value>");
                
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                var result = regex.Replace(text, replacement);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "REGEX_RESULT";
                ctx.Environment[varName] = result;
                ctx.Log($"regex.replace = {result}");
            });

            // Archive module (requires System.IO.Compression)
            Register("archive.create", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("source", out var source) || !cmd.Args.TryGetValue("destination", out var dest))
                    throw new ArgumentException("archive.create requires source=<dir> destination=<zip>");
                
                if (System.IO.File.Exists(dest))
                    System.IO.File.Delete(dest);
                
                System.IO.Compression.ZipFile.CreateFromDirectory(source, dest);
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "ARCHIVE_PATH";
                ctx.Environment[varName] = dest;
                ctx.Log($"archive.create: {dest}");
            });

            Register("archive.extract", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("source", out var source) || !cmd.Args.TryGetValue("destination", out var dest))
                    throw new ArgumentException("archive.extract requires source=<zip> destination=<dir>");
                
                System.IO.Compression.ZipFile.ExtractToDirectory(source, dest, overwriteFiles: true);
                ctx.Log($"archive.extract: {source} -> {dest}");
            });

            Register("archive.list", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("archive.list requires file=<zip>");
                
                using var archive = System.IO.Compression.ZipFile.OpenRead(file);
                var entries = string.Join(",", archive.Entries.Select(e => e.FullName));
                var varName = cmd.Args.TryGetValue("var", out var v) ? v : "ARCHIVE_CONTENTS";
                ctx.Environment[varName] = entries;
                ctx.Log($"archive.list: {archive.Entries.Count} entries");
            });

            Register("archive.add", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("archive", out var archivePath) || !cmd.Args.TryGetValue("files", out var filesStr))
                    throw new ArgumentException("archive.add requires archive=<zip> files=<comma-separated>");
                
                var files = filesStr.Split(',').Select(f => f.Trim()).ToArray();
                using var archive = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Update);
                foreach (var file in files)
                {
                    if (System.IO.File.Exists(file))
                    {
                        var entryName = System.IO.Path.GetFileName(file);
                        archive.CreateEntryFromFile(file, entryName);
                        ctx.Log($"archive.add: {entryName}");
                    }
                }
            });
        }

        private static void LogMessage(ExecutionContext ctx, string level, string message)
        {
            var format = ctx.Environment.TryGetValue("__LOG_FORMAT__", out var fmt) ? fmt : "[%TIME%] %LEVEL% - %MESSAGE%";
            var logFile = ctx.Environment.TryGetValue("__LOG_FILE__", out var file) ? file : null;
            
            var log = format
                .Replace("%TIME%", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("%LEVEL%", level.PadRight(7))
                .Replace("%MESSAGE%", message);
            
            // Console output with color
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                "ERROR" => ConsoleColor.Red,
                "WARNING" => ConsoleColor.Yellow,
                "DEBUG" => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
            Console.WriteLine(log);
            Console.ForegroundColor = originalColor;
            
            // File output
            if (logFile != null)
            {
                try
                {
                    System.IO.File.AppendAllText(logFile, log + Environment.NewLine);
                }
                catch { }
            }
        }

        private static (string, string?) SplitFirstToken(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i]))
                {
                    return (line.Substring(0, i), line.Substring(i + 1));
                }
            }
            return (line, null);
        }

        private static Dictionary<string, string> ParseKeyValueArgs(string? rest)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rest)) return result;
            
            var tokens = Tokenize(rest);
            foreach (var tok in tokens)
            {
                var idx = tok.IndexOf('=');
                if (idx <= 0) continue;
                var key = tok.Substring(0, idx).Trim();
                var val = tok.Substring(idx + 1).Trim();
                if (val.Length >= 2 && ((val[0] == '"' && val[^1] == '"') || (val[0] == '\'' && val[^1] == '\'')))
                    val = val.Substring(1, val.Length - 2);
                result[key] = val;
            }
            return result;
        }

        private static List<string> Tokenize(string s)
        {
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (inQuotes)
                {
                    if (c == quoteChar)
                        inQuotes = false;
                    else
                        sb.Append(c);
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

        private static bool IsTrue(string value)
        {
            return value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EvaluateCondition(string condition, ExecutionContext ctx)
        {
            // ...existing code...
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
            string left, right;
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

        private static string StoreObject(ExecutionContext ctx, object obj, string? preferredId = null)
        {
            var id = string.IsNullOrWhiteSpace(preferredId) ? $"obj_{Guid.NewGuid():N}" : preferredId;
            ctx.ObjectStore[id] = obj;
            return id;
        }

        private static bool TryGetObject<T>(ExecutionContext ctx, string id, out T obj)
        {
            if (ctx.ObjectStore.TryGetValue(id, out var value) && value is T typed)
            {
                obj = typed;
                return true;
            }
            obj = default!;
            return false;
        }

        private class ConfigData
        {
            public Dictionary<string, Dictionary<string, string>> Sections { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private class CsvData
        {
            public List<string> Headers { get; set; } = new();
            public List<string> RowIds { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> Rows { get; } = new(StringComparer.OrdinalIgnoreCase);
            public bool HasHeader { get; set; } = true;
        }

        private class XmlHolder
        {
            public XDocument Document { get; set; } = new();
        }

        private static ConfigData ParseIni(string[] lines)
        {
            var cfg = new ConfigData();
            string current = "";
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("//"))
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    current = line.Substring(1, line.Length - 2);
                    if (!cfg.Sections.ContainsKey(current))
                        cfg.Sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }
                var idx = line.IndexOf('=');
                if (idx > 0)
                {
                    var key = line.Substring(0, idx).Trim();
                    var val = line.Substring(idx + 1).Trim();
                    if (!cfg.Sections.ContainsKey(current))
                        cfg.Sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    cfg.Sections[current][key] = val;
                }
            }
            return cfg;
        }

        private static CsvData ResolveCsvData(ExecutionContext ctx, string dataArg, out List<string> rowIds)
        {
            if (TryGetObject<CsvData>(ctx, dataArg, out var data))
            {
                rowIds = data.RowIds;
                return data;
            }

            var ids = dataArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            foreach (var entry in ctx.ObjectStore.Values)
            {
                if (entry is CsvData cd && ids.Any(id => cd.Rows.ContainsKey(id)))
                {
                    rowIds = ids;
                    return cd;
                }
            }
            throw new ArgumentException($"csv data not found: {dataArg}");
        }

        private static Dictionary<string, string> ResolveCsvRow(ExecutionContext ctx, string rowId, out CsvData data)
        {
            foreach (var entry in ctx.ObjectStore.Values)
            {
                if (entry is CsvData cd && cd.Rows.TryGetValue(rowId, out var row))
                {
                    data = cd;
                    return row;
                }
            }
            throw new ArgumentException($"csv row not found: {rowId}");
        }

        private static bool TryResolveXmlNode(ExecutionContext ctx, string id, out XDocument doc, out XElement? node)
        {
            node = null;
            if (TryGetObject<XDocument>(ctx, id, out var d))
            {
                doc = d;
                return true;
            }
            if (TryGetObject<XElement>(ctx, id, out var el))
            {
                node = el;
                doc = el.Document ?? new XDocument(el);
                return true;
            }
            doc = new XDocument();
            return false;
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