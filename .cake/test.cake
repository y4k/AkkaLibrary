//////////////////////////////////////////////////////////////////////
/// TEST TASKS
//////////////////////////////////////////////////////////////////////

#load build.cake

var testLibrary = Task("Test-Library")
.Description(TaskDescriptions.RunTest)
.Does(() =>
{
    var name = "AkkaLibrary.Test";
    var framework = "netcoreapp2.0";
    
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
.IsDependentOn(buildAllTests);

var testCluster = Task("Test-Cluster")
.Description(TaskDescriptions.RunTest)
.Does(() =>
{
    var name = "AkkaLibrary.Cluster.Test";
    var framework = "netcoreapp2.0";
    
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
.IsDependentOn(buildAllTests);

var testStreams = Task("Test-Streams")
.Description(TaskDescriptions.RunTest)
.Does(() =>
{
    var name = "AkkaLibrary.Streams.Test";
    var framework = "netcoreapp2.0";
    
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
.IsDependentOn(buildAllTests);

var runAllTests = Task("Test-All")
.IsDependentOn(testLibrary)
.IsDependentOn(testCluster)
.IsDependentOn(testStreams)
;