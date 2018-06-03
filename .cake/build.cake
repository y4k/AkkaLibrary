#load parameters.cake
#load clean.cake
#load test.cake
#load restore.cake

///////////////////////////////////////////////////////////////////////////////
// BUILD TASKS
///////////////////////////////////////////////////////////////////////////////

var buildCommon = Task("Build-Common")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "AkkaLibrary.Common";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library Common");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
});


var buildLibrary = Task("Build-AkkaLibrary")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "AkkaLibrary";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
})
.IsDependentOn(buildCommon);

var buildLibraryTests = Task("Build-AkkaLibrary.Test")
.Description(TaskDescriptions.BuildTest)
.Does(() =>
{
    var name = "AkkaLibrary.Test";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{TestDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library Tests");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
})
.IsDependentOn(buildLibrary);

var buildCluster = Task("Build-AkkaLibrary.Cluster")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library - Cluster");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
})
.IsDependentOn(buildLibrary);

var buildClusterTests = Task("Build-AkkaLibrary.Cluster.Test")
.Description(TaskDescriptions.BuildTest)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster.Test";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{TestDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library Cluster Tests");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
})
.IsDependentOn(buildCluster);

var buildClusterHub = Task("Build-AkkaLibrary.Cluster.Hub")
.Description(TaskDescriptions.BuildTest)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster.Hub";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library Cluster Hub");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
});

var buildTestHarness = Task("Build-TestHarness")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "TestHarness";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{TestHarnessDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building TestHarness");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
});

var buildClusterTestHarness = Task("Build-ClusterTestHarness")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "ClusterTestHarness";

    Information("Building Cluster Test Harness");

    var outputDirectory = $"{TestHarnessDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    DotNetCoreClean(project, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, settings);
})
.IsDependentOn(buildCluster);

var buildClusterWorker = Task("Build-ClusterWorker")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster.Worker";

    Information("Building Cluster Worker");

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    DotNetCoreClean(project, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, settings);
})
.IsDependentOn(buildCluster);

var buildClusterManager = Task("Build-ClusterManager")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster.Manager";

    Information("Building Cluster Manager");

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    DotNetCoreClean(project, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, settings);
})
.IsDependentOn(buildCluster);

var buildAkkaLibraryStreams = Task("Build-AkkaLibrary.Streams")
.Description("")
.Does(() =>
{
    var name = "AkkaLibrary.Streams";

    Information("Building AkkaLibrary Streams");

    var outputDirectory = $"{BuildDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    DotNetCoreClean(project, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, settings);
})
.IsDependentOn(buildCommon);

var buildAkkaLibraryStreamsTest = Task("Build-AkkaLibrary.Streams.Test")
.Description(TaskDescriptions.BuildTest)
.Does(() =>
{
    var name = "AkkaLibrary.Streams.Test";

    var projectFile = $"{name}/{name}.csproj";

    var outputDirectory = $"{TestDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    Information("Building Akka Library Streams Tests");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
})
.IsDependentOn(buildAkkaLibraryStreams);

// Build and rebuild all aggregated tasks
var buildAllList = new []
{
    buildCommon,
    buildLibrary,
    buildLibraryTests,
    buildCluster,
    buildClusterTests,
    buildClusterHub,
    buildTestHarness,
    buildClusterTestHarness,
    buildClusterWorker,
    buildClusterManager,
    buildAkkaLibraryStreams,
    buildAkkaLibraryStreamsTest
};

var buildAll = Task("Build-All")
.Description(TaskDescriptions.Build);

foreach (var task in buildAllList)
{
    buildAll.IsDependentOn(task);
}

var rebuildAll = Task("Rebuild-All")
.Description(TaskDescriptions.Build)
.IsDependentOn(cleanAll)
.IsDependentOn(buildAll);