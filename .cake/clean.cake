///////////////////////////////////////////////////////////////////////////////
// Standard directory cleaning tasks
///////////////////////////////////////////////////////////////////////////////
#load parameters.cake

var cleanProject = Task("Clean-Project-Bin")
.Description(TaskDescriptions.Clean)
.DoesForEach(
    () => GetFiles("./*sln"),
    project => DotNetCoreClean(project.FullPath)
);

var cleanBuild = Task("Clean-Build")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.Does(() => CleanDirectory(BuildDirectory))
.IsDependentOn(cleanProject);

var cleanTests = Task("Clean-Tests")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.Does(() => CleanDirectory(TestDirectory))
.IsDependentOn(cleanProject);

var cleanTestResults = Task("Clean-Test-Results")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.Does(() => CleanDirectory(TestResultsDirectory))
.IsDependentOn(cleanProject);

var cleanPackages = Task("Clean-Packages")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.Does(() => CleanDirectory(PackagesDirectory));

var cleanOutput = Task("Clean-Output")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.Does(() => CleanDirectory(OutputDirectoryRoot))
.IsDependentOn(cleanProject);

var cleanBinObj = Task("Clean-Bin-Obj")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.DoesForEach(
    () => GetDirectories("./*/bin").Concat(GetDirectories("./*/obj")),
    dir => 
    {
        Information($"Cleaning {dir}");
        DeleteDirectory(dir, new DeleteDirectorySettings
        {
            Recursive = true
        });
    }
);

var cleanCakeToolsAll = Task("Clean-Cake-Tools")
.ContinueOnError()
.Description(TaskDescriptions.Clean)
.DoesForEach(
    () => GetDirectories("./tools/*", d => !d.Path.Segments.Last().Contains("Cake")),
    dir => 
    {
        Information($"Cleaning {dir}");
        DeleteDirectory(dir, new DeleteDirectorySettings
        {
            Recursive = true
        });
    }
);

var cleanAll = Task("Clean-All")
.Description(TaskDescriptions.Clean)
.IsDependentOn(cleanBinObj)
.IsDependentOn(cleanOutput);