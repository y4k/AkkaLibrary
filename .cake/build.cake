#load parameters.cake
#load clean.cake
#load test.cake
#load restore.cake

///////////////////////////////////////////////////////////////////////////////
// BUILD TASKS
///////////////////////////////////////////////////////////////////////////////

var buildAllLibraries = Task("Build-All-Libraries")
.DoesForEach(
GetFiles("**/*csproj").Where(x => !x.ToString().Contains(".Test")),
csproj =>
{
    var projectFile = csproj.ToString();
    var name = csproj.GetFilenameWithoutExtension().ToString();
    var outputDirectory = $"{BuildDirectory}/{configuration}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
    };

    Information($"Building {name}");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
});

var buildAllTests = Task("Build-All-Tests")
.DoesForEach(
GetFiles("**/*csproj").Where(x => x.ToString().Contains(".Test")),
csproj =>
{
    var projectFile = csproj.ToString();
    var name = csproj.GetFilenameWithoutExtension().ToString();
    var outputDirectory = $"{TestDirectory}/{configuration}/{runtime}/{name}/";

    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory,
        Runtime = runtime,
    };

    Information($"Building {name}");

    DotNetCoreClean(projectFile, new DotNetCoreCleanSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDirectory
    });
    CleanDirectory(outputDirectory);
    DotNetCoreBuild(projectFile, settings);
});

var buildAll = Task("Build-All")
.Description(TaskDescriptions.Build)
.IsDependentOn(buildAllLibraries)
.IsDependentOn(buildAllTests);

var rebuildAll = Task("Rebuild-All")
.Description(TaskDescriptions.Build)
.IsDependentOn(cleanAll)
.IsDependentOn(buildAll);