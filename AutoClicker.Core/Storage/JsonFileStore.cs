using System.Text.Json;

namespace AutoClicker.Core.Storage;

public static class JsonFileStore
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static bool TryRead<T>(string filePath, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        value = default;

        try
        {
            if (!File.Exists(filePath))
                return false;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            value = JsonSerializer.Deserialize<T>(stream);
            return value is not null;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
        catch (IOException)
        {
            value = default;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            value = default;
            return false;
        }
    }

    public static void WriteAtomic<T>(string filePath, T data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath))
            ?? throw new ArgumentException("A destination directory is required.", nameof(filePath));
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, Path.GetFileName(filePath) + ".tmp-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, WriteOptions);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(filePath))
            {
                try
                {
                    File.Replace(tempPath, filePath, destinationBackupFileName: null);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Move(tempPath, filePath, overwrite: true);
                }
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
