using AutoClicker.Core.Storage;
using Xunit;

namespace AutoClicker.Tests.Storage;

public sealed class StorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "autoclicker-tests-" + Guid.NewGuid().ToString("N"));

    public StorageTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void CorruptJsonReturnsFalseWithoutThrowing()
    {
        var path = Path.Combine(_root, "settings.json");
        File.WriteAllText(path, "{not-json");

        var success = JsonFileStore.TryRead<TestSettings>(path, out var value);

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void MissingJsonReturnsFalse()
    {
        var success = JsonFileStore.TryRead<TestSettings>(Path.Combine(_root, "missing.json"), out var value);

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void AtomicWriteCreatesMissingParentAndRoundTrips()
    {
        var path = Path.Combine(_root, "nested", "settings.json");
        var expected = new TestSettings { Interval = 125, Name = "portable" };

        JsonFileStore.WriteAtomic(path, expected);
        var success = JsonFileStore.TryRead<TestSettings>(path, out var actual);

        Assert.True(success);
        Assert.NotNull(actual);
        Assert.Equal(expected.Interval, actual.Interval);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp-*"));
    }

    [Fact]
    public void SecondAtomicWriteReplacesExistingDocument()
    {
        var path = Path.Combine(_root, "settings.json");
        JsonFileStore.WriteAtomic(path, new TestSettings { Interval = 100, Name = "old" });

        JsonFileStore.WriteAtomic(path, new TestSettings { Interval = 200, Name = "new" });
        Assert.True(JsonFileStore.TryRead<TestSettings>(path, out var actual));

        Assert.Equal(200, actual!.Interval);
        Assert.Equal("new", actual.Name);
    }

    [Fact]
    public void DataDirectoryPrefersWritableAdjacentDataFolder()
    {
        var appDirectory = Path.Combine(_root, "app");
        var fallback = Path.Combine(_root, "fallback");
        Directory.CreateDirectory(appDirectory);

        var resolved = DataDirectoryResolver.Resolve(appDirectory, fallback);

        Assert.Equal(Path.Combine(appDirectory, "data"), resolved);
        Assert.True(Directory.Exists(resolved));
    }

    [Fact]
    public void DataDirectoryFallsBackWhenAdjacentLocationIsNotDirectoryWritable()
    {
        var blocker = Path.Combine(_root, "blocked");
        File.WriteAllText(blocker, "not a directory");
        var fallback = Path.Combine(_root, "fallback");

        var resolved = DataDirectoryResolver.Resolve(blocker, fallback);

        Assert.Equal(fallback, resolved);
        Assert.True(Directory.Exists(fallback));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private sealed class TestSettings
    {
        public int Interval { get; set; }
        public string? Name { get; set; }
    }
}
