using System;
using System.IO;
using MonoGameLibrary.Core.Diagnostics;

namespace DungeonSlime.Diagnostics;

public class FileLogger : ILogger {
    private readonly string _pathFile;
    private readonly object _lock = new object();
    
    public FileLogger(string pathFile = "log.txt") {
        _pathFile = pathFile;
        File.AppendAllText(_pathFile, $"Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} \n");
    }
    
    public void Log(LogLevel level, string message) {
        Log(level, message, null);
    }
    
    public void Log(LogLevel level, string message, Exception exception) {
        lock (_lock) {
            string entry = $"[{level}] {DateTime.Now:HH:mm:ss.fff} {message}";
            if (exception != null) {
                entry += $"\n{exception}";
            }
            File.AppendAllText(_pathFile, entry + "\n");
        }
    }
    
    public bool IsEnabled(LogLevel level) {
        return true;
    }
    
    public void Info(string message) { Log(LogLevel.Info, message); }
    public void Warning(string message) { Log(LogLevel.Warning, message); }
    public void Error(string message, Exception exception = null) { Log(LogLevel.Error, message, exception); }
    public void Debug(string message) { Log(LogLevel.Debug, message); }
}