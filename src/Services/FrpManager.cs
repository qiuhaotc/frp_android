using System.Text.RegularExpressions;
using Android.Content.PM;
using AApp = Android.App.Application;

namespace FrpAndroid.Services;

public class FrpManager
{
    // 单独放置正则常量 (使用 verbatim + 双反斜杠 正确转义)
    static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);
    readonly string _appDataDir;
    System.Diagnostics.Process? _frpcProc;
    System.Diagnostics.Process? _frpsProc;

    readonly object _logLock = new();
    readonly List<string> _logBuffer = new(1024);
    const int MaxLogs = 1000;

    public FrpManager()
    {
        _appDataDir = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(_appDataDir);
    }

    string GetNativeLibDir()
    {
        try
        {
            var ctx = AApp.Context;
            ApplicationInfo ai = ctx.PackageManager!.GetApplicationInfo(ctx.PackageName!, PackageInfoFlags.SharedLibraryFiles);
#pragma warning disable CA1422
            return ai.NativeLibraryDir!; // /data/app/.../lib/<abi>
#pragma warning restore CA1422
        }
        catch (Exception ex)
        {
            AddLog(FrpType.Frpc, "获取 nativeLibraryDir 失败: " + ex.Message);
            return _appDataDir;
        }
    }

    string GetBinaryPath(FrpType type) => Path.Combine(GetNativeLibDir(), type == FrpType.Frpc ? "libfrpc.so" : "libfrps.so");

    string EnsureConfig(string filename, string template)
    {
        var full = Path.Combine(_appDataDir, filename);
        if (!File.Exists(full)) File.WriteAllText(full, template);
        return full;
    }

    public string GetConfigPath(FrpType type) => Path.Combine(_appDataDir, type == FrpType.Frpc ? "frpc.ini" : "frps.ini");

    void EnsureDefaults(FrpType type)
    {
        if (type == FrpType.Frpc) EnsureConfig("frpc.ini", "[common]\nserver_addr=0.0.0.0\nserver_port=7000\n");
        else EnsureConfig("frps.ini", "[common]\nbind_port=7000\n");
    }

    public Task<string> ReadConfigAsync(FrpType type)
    {
        var path = GetConfigPath(type);
        if (!File.Exists(path)) EnsureDefaults(type);
        return Task.FromResult(File.ReadAllText(path));
    }

    public Task SaveConfigAsync(FrpType type, string content)
    { File.WriteAllText(GetConfigPath(type), content ?? string.Empty); return Task.CompletedTask; }

    public bool IsRunning(FrpType t) => t == FrpType.Frpc ? _frpcProc is { HasExited: false } : _frpsProc is { HasExited: false };

    public async Task<(bool ok,string message)> StartFrpcAsync()
    {
        if (IsRunning(FrpType.Frpc)) return (true, "frpc 已在运行");
        EnsureDefaults(FrpType.Frpc);
        var cfg = GetConfigPath(FrpType.Frpc);
        var bin = GetBinaryPath(FrpType.Frpc);
        _frpcProc = StartProcess(bin, cfg, FrpType.Frpc, out var msg);
        var ok = _frpcProc != null; OnStateChanged?.Invoke(this, new FrpStateEvent(FrpType.Frpc, ok, msg));
        return (ok, msg);
    }

    public async Task<(bool ok,string message)> StartFrpsAsync()
    {
        if (IsRunning(FrpType.Frps)) return (true, "frps 已在运行");
        EnsureDefaults(FrpType.Frps);
        var cfg = GetConfigPath(FrpType.Frps);
        var bin = GetBinaryPath(FrpType.Frps);
        _frpsProc = StartProcess(bin, cfg, FrpType.Frps, out var msg);
        var ok = _frpsProc != null; OnStateChanged?.Invoke(this, new FrpStateEvent(FrpType.Frps, ok, msg));
        return (ok, msg);
    }

    System.Diagnostics.Process? StartProcess(string binaryPath, string configPath, FrpType type, out string message)
    {
        try
        {
            if (!File.Exists(binaryPath)) { message = "二进制不存在: " + binaryPath; return null; }
            if (!File.Exists(configPath)) { message = "配置文件不存在: " + configPath; return null; }
            try { Java.Lang.Runtime.GetRuntime().Exec(new[]{"/system/bin/chmod","755", binaryPath}); } catch { }
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = "-c \"" + configPath + "\"",
                WorkingDirectory = _appDataDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) { message = "启动失败"; return null; }
            _ = Task.Run(()=>PumpLogsAsync(proc, type));
            message = "启动成功"; return proc;
        }
        catch (Exception ex)
        { message = ex.Message.Contains("ermission")? ("执行权限错误: "+ex.Message): ("启动异常: "+ex.Message); return null; }
    }

    async Task PumpLogsAsync(System.Diagnostics.Process proc, FrpType type)
    {
        try
        {
            while(!proc.HasExited)
            {
                var line = await proc.StandardOutput.ReadLineAsync();
                if (line == null) break; AddLog(type, line);
            }
            string? err; while((err = await proc.StandardError.ReadLineAsync())!=null) AddLog(type, err);
        }
        catch(Exception ex){ AddLog(type, "日志捕获异常: "+ex.Message); }
        finally{ OnStateChanged?.Invoke(this, new FrpStateEvent(type,false,"已退出")); }
    }

    public bool StopFrpc() => Stop(ref _frpcProc, FrpType.Frpc);
    public bool StopFrps() => Stop(ref _frpsProc, FrpType.Frps);

    bool Stop(ref System.Diagnostics.Process? p, FrpType type)
    {
        try
        {
            if (p is null) return false;
            if (!p.HasExited){ p.Kill(); p.WaitForExit(2000);} p.Dispose(); p=null; AddLog(type,"已停止"); OnStateChanged?.Invoke(this,new FrpStateEvent(type,false,"已停止")); return true;
        }
        catch(Exception ex){ AddLog(type,"停止异常: "+ex.Message); OnStateChanged?.Invoke(this,new FrpStateEvent(type,false,"停止异常")); return false; }
    }

    void AddLog(FrpType type, string raw)
    {
        var cleaned = Sanitize(raw);
        lock(_logLock)
        {
            _logBuffer.Add($"[{DateTime.Now:HH:mm:ss}] [{type}] {cleaned}");
            if (_logBuffer.Count>MaxLogs) _logBuffer.RemoveRange(0,_logBuffer.Count-MaxLogs);
        }
        OnLog?.Invoke(this,new FrpLogEvent(type, cleaned));
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var noAnsi = AnsiRegex.Replace(s, string.Empty);
        var filtered = new string(noAnsi.Where(c => !char.IsControl(c) || c=='\n' || c=='\r' || c=='\t').ToArray());
        return filtered.TrimEnd('\r');
    }

    public IReadOnlyList<string> GetLogs(){ lock(_logLock) return _logBuffer.ToList(); }

    public event EventHandler<FrpLogEvent>? OnLog;
    public event EventHandler<FrpStateEvent>? OnStateChanged;
}

public enum FrpType { Frpc, Frps }
public class FrpLogEvent{ public FrpType Type {get;} public string Message {get;} public FrpLogEvent(FrpType t,string m){Type=t;Message=m;} }
public class FrpStateEvent{ public FrpType Type{get;} public bool Running{get;} public string? Info{get;} public FrpStateEvent(FrpType type,bool running,string? info){Type=type;Running=running;Info=info;} }
