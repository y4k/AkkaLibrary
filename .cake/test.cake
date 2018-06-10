//////////////////////////////////////////////////////////////////////
/// TEST TASKS
//////////////////////////////////////////////////////////////////////

#load build.cake

var testLibrary = Task("Test-Library")
.Description(TaskDescriptions.RunTest)
.Does(() =>
{
    var name = "AkkaLibrary.Test";

    Information($"Testing {name}");

    var outputDirectory = $"{TestDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, buildSettings);

    var testSettings = new DotNetCoreTestSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        ResultsDirectory = $"{TestResultsDirectory}/{configuration}/{framework}/{runtime}/{name}/",
        Verbosity = DotNetCoreVerbosity.Minimal
    };

    DotNetCoreTest(project,testSettings);
})
.IsDependentOn(buildLibraryTests);

var testCluster = Task("Test-Cluster")
.Description(TaskDescriptions.RunTest)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster.Test";

    Information($"Testing {name}");

    var outputDirectory = $"{TestDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, buildSettings);

    var testSettings = new DotNetCoreTestSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        ResultsDirectory = $"{TestResultsDirectory}/{configuration}/{framework}/{runtime}/{name}/",
        Verbosity = DotNetCoreVerbosity.Minimal
    };

    DotNetCoreTest(project,testSettings);
})
.IsDependentOn(buildClusterTests);

var testStreams = Task("Test-Streams")
.Description(TaskDescriptions.RunTest)
.Does(() =>
{
    var name = "AkkaLibrary.Streams.Test";

    Information($"Testing {name}");

    var outputDirectory = $"{TestDirectory}/{configuration}/{framework}/{runtime}/{name}/";

    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
        Framework = framework
    };

    var project = $"{name}/{name}.csproj";

    CleanDirectory(outputDirectory);
    DotNetCoreBuild(project, buildSettings);

    var testSettings = new DotNetCoreTestSettings
    {
        Framework = framework,
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        ResultsDirectory = $"{TestResultsDirectory}/{configuration}/{framework}/{runtime}/{name}/",
        Verbosity = DotNetCoreVerbosity.Minimal
    };

    DotNetCoreTest(project,testSettings);
})
.IsDependentOn(buildAkkaLibraryStreamsTest);