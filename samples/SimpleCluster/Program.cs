using SimpleCluster.Hosting;

var configuration = new DemoConfigurationFactory().CreateThreeNodeCluster();
var application = new ClusterDemoApplication(configuration);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cts.Cancel();
};

await application.RunAsync(cts.Token);
