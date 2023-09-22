#region StandardUsing
using FTOptix.Core;
using UAManagedCore;
using FTOptix.NetLogic;
using System.IO;
using System;
#endregion

using FilesystemBrowser;

public class FilesystemBrowserDatagridLogic : BaseNetLogic {
    public override void Start() {
        pathVariable = Owner.Owner.GetVariable("FolderPath");
        if (pathVariable == null)
            throw new CoreConfigurationException("FolderPath variable not found in FilesystemBrowser");

        fullPathVariable = Owner.Owner.GetVariable("FullPath");
        if (fullPathVariable == null)
            throw new CoreConfigurationException("FullPath variable not found in FilesystemBrowser");

        accessFullFilesystemVariable = Owner.Owner.GetVariable("AccessFullFilesystem");
        if (accessFullFilesystemVariable == null)
            throw new CoreConfigurationException("AccessFullFilesystem variable not found");

        if (accessFullFilesystemVariable.Value && !PlatformConfigurationHelper.IsFreeNavigationSupported())
            return;

        locationsObject = Owner.Owner.GetObject("Locations");
        if (locationsObject == null)
            throw new CoreConfigurationException("Locations object not found");

        resourceUriHelper = new ResourceUriHelper(LogicObject.NodeId.NamespaceIndex) {
            LocationsObject = locationsObject
        };

        selectedItemVariable = Owner.GetVariable("SelectedItem");
        selectedItemVariable.VariableChange += SelectedItemVariable_VariableChange;
    }

    public override void Stop() {
        selectedItemVariable.VariableChange -= SelectedItemVariable_VariableChange;
    }

    private void SelectedItemVariable_VariableChange(object sender, VariableChangeEventArgs e) {
        var nodeId = (NodeId)e.NewValue;
        if (nodeId == null || nodeId.IsEmpty)
            return;

        var entry = (FileEntry)LogicObject.Context.GetObject(nodeId);
        if (entry == null)
            return;

        UpdateCurrentPath(entry.FileName);
    }

    private void UpdateCurrentPath(string lastPathToken) {
        // Necessary when QStudio placeholder path is configured with only %APPLICATIONDIR%\, %PROJECTDIR%\
        // i.e. at the start of the project
        var currentPath = resourceUriHelper.AddNamespacePrefixToQRuntimeFolder(pathVariable.Value);
        var currentPathResourceUri = new ResourceUri(currentPath);

        if (lastPathToken == "..")
            SetPathsToParentFolder(currentPathResourceUri);
        else
            SetPathsToSelectedFile(currentPathResourceUri, lastPathToken);
    }

    private void SetPathsToParentFolder(ResourceUri startingDirectoryResourceUri) {
        DirectoryInfo parentDirectory;
        try {
            parentDirectory = Directory.GetParent(startingDirectoryResourceUri.Uri);
        } catch (Exception exception) {
            Log.Error($"Unable to get parent folder: {exception.Message}");
            return;
        }

        if (parentDirectory == null)
            return;

        var parentDirectoryPath = parentDirectory.FullName;

        // E.g. %PROJECTDIR%/PKI
        pathVariable.Value = resourceUriHelper.GetQStudioFormattedPath(startingDirectoryResourceUri,
                                                                       parentDirectoryPath);

        fullPathVariable.Value = ResourceUri.FromAbsoluteFilePath(parentDirectoryPath);
    }

    private void SetPathsToSelectedFile(ResourceUri currentDirectoryResourceUri, string targetFile) {
        string updatedPath;
        try {
            updatedPath = Path.Combine(currentDirectoryResourceUri.Uri, targetFile);
        } catch (Exception exception) {
            Log.Error("FilesystemBrowserDatagridLogic", $"Path not found {exception.Message}");
            return;
        }

        fullPathVariable.Value = ResourceUri.FromAbsoluteFilePath(updatedPath);

        if (!IsDirectory(updatedPath))
            return;

        // E.g. %PROJECTDIR%/PKI
        pathVariable.Value = resourceUriHelper.GetQStudioFormattedPath(currentDirectoryResourceUri,
                                                                       updatedPath);
    }

    private bool IsDirectory(string path) {
        return Directory.Exists(path);
    }

    private IUAVariable pathVariable;
    private IUAVariable fullPathVariable;
    private IUAVariable selectedItemVariable;
    private IUAVariable accessFullFilesystemVariable;
    private IUAObject locationsObject;

    private ResourceUriHelper resourceUriHelper;
}
