///////////////////////////////////////////////////////////////////////////////
// PACKAGE TASKS
///////////////////////////////////////////////////////////////////////////////

#load parameters.cake
#load build.cake
#load test.cake

var packageAkkaLibrary = Task("Package-AkkaLibrary")
.Description(TaskDescriptions.Package)
.Does(() =>
{
    var name = "AkkaLibrary";

    var projectFile = $"{name}/{name}.csproj";

    var settings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        IncludeSymbols = true,
        OutputDirectory = ArtifactsDirectory,

    };

    DotNetCorePack(projectFile, settings);
})
.IsDependentOn(testLibrary);