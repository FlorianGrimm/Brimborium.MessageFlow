namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;

public class SystemTextJsonUtilitiesTest {
    [Fact]
    public async Task SystemTextJsonUtilities001() {
        var folderPath = TestUtility.GetTestDataPath("SystemTextJsonUtilities");
        System.IO.Directory.CreateDirectory(folderPath);
        var filename = System.IO.Path.Combine(folderPath, "lines001-diff.utf8");
        var sut = new SystemTextJsonUtilities();
        List<DataItem> listDataItem = [new DataItem("1", "one"), new DataItem("2", "two")];
        using (var fileStream = System.IO.File.Create(filename, 0, FileOptions.WriteThrough)) {
            await sut.SerializeLinesAsync<DataItem>(fileStream, listDataItem, CancellationToken.None);
        }
        using (var fileStream = System.IO.File.Open(filename, FileMode.Open)) {
            var listDataItem2 = await sut.DeserializeLines<DataItem>(fileStream, CancellationToken.None);
            Assert.NotNull(listDataItem2);
            Assert.Equal(listDataItem.Count, listDataItem2.Count);
        }
    }

    public record DataItem(string Key, string Value);
}
