//////////////////////////////////////////////////////////////////////
/// RUN TASKS
//////////////////////////////////////////////////////////////////////

#load build.cake

var runBootstrapper = Task("Run-TestHarness")
.Description(TaskDescriptions.Run)
.Does(() =>
{
    var name = "TestHarness";

    var project = $"{name}/{name}.csproj";

    var settings = new DotNetCoreRunSettings
    {
    };

    DotNetCoreRun(project, new ProcessArgumentBuilder(), settings);
});

var runClusterTestHarness = Task("Run-ClusterTestHarness")
.Description(TaskDescriptions.Run)
.Does(() =>
{
    var project = "ClusterTestHarness/ClusterTestHarness.csproj";

    var settings = new DotNetCoreRunSettings
    {
    };

    DotNetCoreRun(project, new ProcessArgumentBuilder(), settings);
});