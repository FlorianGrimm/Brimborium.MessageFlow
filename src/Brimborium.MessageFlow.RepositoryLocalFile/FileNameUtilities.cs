namespace Brimborium.MessageFlow.RepositoryLocalFile;

public class FileNameUtilities {

    /*
       string dataFileName = System.IO.Path.Combine(folderPath, $"{utcNow}-state.json");

*/
    public static string GetFullPath(string folderPath, System.DateTime dt, string state, string extension) {
        var utcNow = dt.ToString("yyyy-MM-dd-HH-mm-ss-ffffff");
        string result = System.IO.Path.Combine(folderPath, $"{utcNow}-{state}{extension}");
        return result;
    }

    public static Optional<StateDiffFileNames> GetLatestStateFileNameFromFolder(string folderPath) {
        System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);
        var listFileInfo = di.GetFiles("*.json");
        var optStateDiffFileNames = FileNameUtilities.GetLatestStateFileName(listFileInfo);
        return optStateDiffFileNames;
    }

    public static Optional<StateDiffFileNames> GetLatestStateFileName(FileInfo[] listFileInfo) {
        var listFileName = ConvertToListFileName(listFileInfo);
        return GetLatestStateFileName(listFileName);
    }

    private static List<string> ConvertToListFileName(FileInfo[] listFileInfo) {
        List<string> listFileName = new();
        foreach (var fileInfo in listFileInfo) {
            listFileName.Add(fileInfo.Name);
        }
        return listFileName;
    }

    public static Optional<StateDiffFileNames> GetLatestStateFileName(List<string> listFileName) {
        List<string> listDiffFileName = new();
        listFileName.Sort(StringComparer.OrdinalIgnoreCase);
        for (int idx = listFileName.Count - 1; 0 <= idx; idx--) {
            var fileName = listFileName[idx];
            var suffix = fileName.AsSpan()[27..^5];
            if ((suffix.Length == 5)
                && suffix.StartsWith("state".AsSpan())) {
                return new (new(listDiffFileName, fileName));
            }
            if ((suffix.Length == 4)
                && suffix.StartsWith("diff".AsSpan())) {
                listDiffFileName.Add(fileName);
                continue;
            }
        }
        if (listDiffFileName.Count == 0) {
            return new();
        } else {
            return new(new(listDiffFileName, string.Empty));
        }
    }
}

public sealed record StateDiffFileNames(List<string> ListDiffFileName, string StateFileName);
