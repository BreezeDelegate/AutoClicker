using AutoClicker.Core.Storage;
using Serilog;

namespace AutoClicker.Utils
{
    public static class JsonUtils
    {
        public static T ReadJson<T>(string filePath)
        {
            if (JsonFileStore.TryRead<T>(filePath, out T result))
            {
                Log.Debug("Read from file {FilePath} successfully", filePath);
                return result;
            }

            Log.Warning("File {FilePath} is missing, unreadable, or contains invalid JSON; defaults will be used", filePath);
            return default;
        }

        public static void WriteJson<T>(string filePath, T data)
        {
            JsonFileStore.WriteAtomic(filePath, data);
            Log.Debug("Wrote file {FilePath} successfully", filePath);
        }
    }
}
