using System.Drawing;
using Xunit;
using Tunkoz.Core.Tests.Helpers;

namespace Tunkoz.Core.Tests.Fixtures;

public class SampleQrFixture : IAsyncLifetime
{
    public string SampleDirectory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        SampleDirectory = Path.Combine(Path.GetTempPath(), "tunkoz-tests", Guid.NewGuid().ToString());
        await Task.Run(() => QrImageGenerator.SaveSampleQrCodes(SampleDirectory));
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(SampleDirectory))
        {
            await Task.Run(() => Directory.Delete(SampleDirectory, recursive: true));
        }
    }

    public Bitmap GetSampleQr(string filename)
    {
        var path = Path.Combine(SampleDirectory, filename);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Sample QR not found: {path}");
        return new Bitmap(path);
    }
}

[CollectionDefinition("Sample QR Collection")]
public class SampleQrCollection : ICollectionFixture<SampleQrFixture>
{
}
