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

    public static List<string> GetLatestStateFileName(FileInfo[] listFileInfo) {
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

    public static List<string> GetLatestStateFileName(List<string> listFileName) {
        List<string> result = new();
        listFileName.Sort(StringComparer.OrdinalIgnoreCase);
        for (int idx = listFileName.Count - 1; 0 <= idx; idx--) {
            var fileName = listFileName[idx];
            var suffix = fileName.AsSpan()[27..^5];
            if ((suffix.Length == 5)
                && suffix.StartsWith("state".AsSpan())) {
                result.Add(fileName);
                break;
            }
            if ((suffix.Length == 4)
                && suffix.StartsWith("diff".AsSpan())) {
                result.Add(fileName);
                continue;
            }
            //if (StringComparer.OrdinalIgnoreCase.Equals("state", suffix)) { break;}
        }
        return result;
    }
}
