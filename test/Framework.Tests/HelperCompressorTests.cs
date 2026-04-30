using System.IO;
using TUnit.Assertions;
using TUnit.Core;

namespace Framework.Tests;

public partial class HelperCompressorTests
{
    [Test]
    public async Task ZipStream_WithFiles_ReturnsStream()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(testFile, "test");
        
        var files = new List<string> { testFile };
        var stream = Helper.ZipStream(files);
        
        await Assert.That(stream).IsNotNull();
        await Assert.That(stream.Length).IsGreaterThan(0);
        
        Directory.Delete(tempDir, true);
    }

    [Test]
    public async Task Decompress_ExtractsFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        var testContent = "test content";
        var testFile = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(testFile, testContent);
        
        var zipPath = Path.Combine(tempDir, "test.zip");
        var files = new List<string> { testFile };
        Helper.Zip(files, zipPath);
        
        var extractDir = Path.Combine(tempDir, "extracted");
        Helper.Decompress(zipPath, extractDir);
        
        var extractedFiles = Directory.GetFiles(extractDir);
        await Assert.That(extractedFiles.Length).IsGreaterThan(0);
        
        Directory.Delete(tempDir, true);
    }
}