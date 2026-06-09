using System.Text.Json;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Cli;

/// <summary>
/// SECONDARY ADAPTER — persists the last update-check timestamp in a small JSON
/// file under the user profile (<c>~/.outlet/update-check.json</c>), independent
/// of any project's working directory.
///
/// Throttle state is disposable: reads return null on any problem and writes
/// swallow failures, so a read-only or unusual home directory never breaks a command.
/// </summary>
public sealed class JsonFileCliUpdateStateStore(string? stateDirectory = null) : ICliUpdateStateStore
{
    private readonly string _filePath = Path.Combine(
        stateDirectory
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".outlet"),
        "update-check.json");

    public DateTime? ReadLastCheckUtc()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var state = JsonSerializer.Deserialize<UpdateCheckState>(File.ReadAllText(_filePath));
            return state?.LastCheckUtc;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void SaveLastCheckUtc(DateTime timestampUtc)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(new UpdateCheckState(timestampUtc)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort: a missed write just means we check again next time.
        }
    }

    private sealed record UpdateCheckState(DateTime LastCheckUtc);
}
