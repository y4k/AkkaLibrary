///////////////////////////////////////////////////////////////////////////////
// PUBLISH TASKS
///////////////////////////////////////////////////////////////////////////////

#load parameters.cake
#load build.cake
#load test.cake

#addin "Cake.FileHelpers"

using System.Xml;

var nugetUpdateAllPackages = Task("Update-All-Packages")
.DoesForEach(
GetFiles("**/*csproj"),
target =>
{
    Information($"Updating {target}");

    var reader = new XmlTextReader(target.ToString());

    var pkgVersions = new Dictionary<string, string>();
    while(reader.ReadToFollowing("PackageReference"))
    {
        pkgVersions.Add(reader.GetAttribute("Include"), reader.GetAttribute("Version"));
    }
});

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

var publish = Task("Publish-Example")
.Does(() =>
{
    Information("Publishing Example NuGet packages");

    var files = GetFiles($"{PackagesDirectory}*nupkg");

    var settings = new NuGetAddSettings
    {
        ConfigFile = "/home/alex/.nuget/NuGet/NuGet.Config",
        Source = "/home/alex/Documents/Programming/CSharp/.nuget/Packages/",
        //WorkingDirectory = PackagesDirectory,
        Verbosity = NuGetVerbosity.Detailed
    };

    foreach (var file in files.Where(x => !x.ToString().Contains("symbols")))
    {
        Information($"Publishing:{file.GetFilenameWithoutExtension()}");
        NuGetAdd(file.ToString(), settings);
    }
}).IsDependentOn(packageAkkaLibrary);