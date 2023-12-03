namespace Brimborium.MessageFlow.RepositoryLocalFile.Test;

internal static class TestUtility {
    public static string GetTestDataPath(string subFolderName) {
        return System.IO.Path.GetFullPath(
                System.IO.Path.Combine(
                    GetCurrentFolder(),
                    $"TestData/{subFolderName}") ?? "");
    }

    private static string GetCurrentFolder([CallerFilePath] string? callerFilePath = default) {
        return System.IO.Path.GetDirectoryName(callerFilePath) ?? "";
    }
}
