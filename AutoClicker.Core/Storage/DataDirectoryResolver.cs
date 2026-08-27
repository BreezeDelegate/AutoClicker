namespace AutoClicker.Core.Storage;

public static class DataDirectoryResolver
{
    public static string Resolve(string applicationDirectory, string fallbackDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackDirectory);

        var adjacent = Path.Combine(applicationDirectory, "data");
        if (CanWriteDirectory(adjacent))
            return adjacent;

        Directory.CreateDirectory(fallbackDirectory);
        return fallbackDirectory;
    }

    private static bool CanWriteDirectory(string directory)
    {
        string? probePath = null;
        try
        {
            Directory.CreateDirectory(directory);
            probePath = Path.Combine(directory, ".write-probe-" + Guid.NewGuid().ToString("N"));
            using (var stream = new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.WriteByte(0xA5);
                stream.Flush(flushToDisk: true);
            }
            File.Delete(probePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            if (probePath is not null)
            {
                try { if (File.Exists(probePath)) File.Delete(probePath); }
                catch { }
            }
        }
    }
}
