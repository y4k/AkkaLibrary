///////////////////////////////////////////////////////////////////////////////
// This is the main cake file and should only pull together all of the other
// files and define top level tasks such as the default and those that depend
// on tasks present in multiple files.

// It is also responsible for pulling in and loading any additional tools
///////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
// Additional Tools
///////////////////////////////////////////////////////////////////////////////
#tool "docfx.console"
///////////////////////////////////////////////////////////////////////////////
// Load other *.cake files
///////////////////////////////////////////////////////////////////////////////
#load .cake/parameters.cake
#load .cake/clean.cake
#load .cake/test.cake
#load .cake/restore.cake
#load .cake/run.cake
#load .cake/nuget.cake

// Default
Task("Default")
.Description(TaskDescriptions.Build)
.Does(() =>
{
    Information("Default Task. Clean All, then Build All, then Test All.");
})
.IsDependentOn(rebuildAll)
.IsDependentOn(testLibrary)
.IsDependentOn(testCluster);

RunTarget(target);