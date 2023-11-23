internal class Program {
    private static void Main(string[] args) {
        var builder = DistributedApplication.CreateBuilder(args);
        var project = builder.AddProject<Projects.Brimborium_MessageFlow_APIServer>("APIServer");

        builder.Build().Run();
    }
}