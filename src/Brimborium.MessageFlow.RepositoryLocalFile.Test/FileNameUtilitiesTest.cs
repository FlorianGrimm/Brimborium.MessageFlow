#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;

public class FileNameUtilitiesTest {
    List<DateTime> _ListDT;
    private readonly ITestOutputHelper _TestOutputHelper;

    public FileNameUtilitiesTest(ITestOutputHelper testOutputHelper) {
        this._TestOutputHelper = testOutputHelper;
        var dt = DateTime.UtcNow;
        List<DateTime> listDT = new();
        for (int i = 0; i < 60; i++) {
            var dt2 = dt.Subtract(TimeSpan.FromSeconds(Random.Shared.Next(31 * 24 * 60 * 60)));
            listDT.Add(dt2);
        }
        this._ListDT = listDT;
    }

    [Fact]
    public void FileNameUtilities_GetFullPath() {
        var act = FileNameUtilities.GetFullPath(@"c:\temp\", DateTime.UnixEpoch, "abcd", ".temp");
        Assert.Equal(@"c:\temp\1970-01-01-00-00-00-000000-abcd.temp", act);
    }

    /*
    [Fact]
    public void FileNameUtilities_1() {
        Assert.Equal(
            @"C:\github.com\FlorianGrimm\Brimborium.MessageFlow\src\Brimborium.MessageFlow.RepositoryLocalFile.Test\TestData\",
            TestUtility.GetTestDataPath(""));
    }
    */

    [Fact]
    public void FileNameUtilities_GetLatestStateFileName_002() {
        List<string> listFileName = new() {
            "2023-12-24-17-59-21-222222-diff.json",
            "2023-01-01-18-00-00-000000-state.json",
            "2022-07-02-19-42-42-111111-state.json",
            "2022-06-02-19-42-42-111111-state.json"
        };
        var act = FileNameUtilities.GetLatestStateFileName(listFileName);
        Assert.True(act.TryGetValue(out var stateDiffFileNames));
        Assert.True(!string.IsNullOrEmpty(stateDiffFileNames.StateFileName));
        Assert.Equal(1, stateDiffFileNames.ListDiffFileName.Count);
    }

    [Fact]
    public void FileNameUtilities_003() {
        var folderPath = TestUtility.GetTestDataPath(@"TestLoad\Hack");
        System.IO.DirectoryInfo directoryInfo = new(folderPath);
        var listFileInfo = directoryInfo.GetFiles("*.json", new EnumerationOptions() {
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false
        });
        // FileNameUtilities.GetLatestStateFileName(listFileInfo);
        var act = FileNameUtilities.GetLatestStateFileName(listFileInfo);
        Assert.True(act.TryGetValue(out var stateDiffFileNames));
        Assert.True(!string.IsNullOrEmpty(stateDiffFileNames.StateFileName));
        Assert.Equal(0, stateDiffFileNames.ListDiffFileName.Count);
    }
}
