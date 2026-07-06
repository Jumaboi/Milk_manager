using System.Text.Json;

namespace Milk_manager.Services;

public static class DeveloperActionLogService
{
    private static readonly object SyncRoot = new();

    public static string LogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "developer-actions.jsonl");

    public static void Log(string action, string description, object? before = null, object? after = null, string? rollbackHint = null)
    {
        var entry = new DeveloperActionLogEntry(
            DateTimeOffset.Now,
            action,
            description,
            before,
            after,
            rollbackHint ?? "Проверьте before/after и выполните обратное действие вручную в LiteDB или через экран приложения.");

        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = false });
        lock (SyncRoot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            File.AppendAllText(LogFilePath, json + Environment.NewLine);
        }
    }
}

public record DeveloperActionLogEntry(
    DateTimeOffset Timestamp,
    string Action,
    string Description,
    object? Before,
    object? After,
    string RollbackHint);
