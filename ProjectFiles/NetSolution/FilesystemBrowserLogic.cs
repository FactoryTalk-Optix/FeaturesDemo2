#region StandardUsing
using System;
using FTOptix.HMIProject;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.Core;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
#endregion

using FilesystemBrowser;

public class FilesystemBrowserLogic : BaseNetLogic {
    public override void Start() {
        pathVariable = LogicObject.GetVariable("Path");
        if (pathVariable == null)
            throw new CoreConfigurationException("Path variable not found in FilesystemBrowserLogic");

        filterVariable = Owner.GetVariable("ExtensionFilter");
        if (filterVariable == null)
            throw new CoreConfigurationException("ExtensionFilter variable not found in FilesystemBrowserLogic");

        accessFullFilesystemVariable = Owner.GetVariable("AccessFullFilesystem");
        if (accessFullFilesystemVariable == null)
            throw new CoreConfigurationException("AccessFullFilesystem variable not found");

        if (accessFullFilesystemVariable.Value && !PlatformConfigurationHelper.IsFreeNavigationSupported())
            throw new CoreConfigurationException($"Current system does not support full filesystem access");

        showHiddenFilesVariable = Owner.GetVariable("ShowHiddenFiles");
        if (showHiddenFilesVariable == null)
            throw new CoreConfigurationException("ShowHiddenFiles variable not found");

        accessNetworkDrivesVariable = Owner.GetVariable("AccessNetworkDrives");
        if (accessNetworkDrivesVariable == null)
            throw new CoreConfigurationException("AccessNetworkDrives variable not found");

        // In case of invalid path the fallback is %PROJECTDIR%
        var startFolderPathResourceUri = new ResourceUri(pathVariable.Value);

        resourceUriHelper = new ResourceUriHelper(LogicObject.NodeId.NamespaceIndex);
        if (!resourceUriHelper.IsFolderPathAllowed(startFolderPathResourceUri, accessFullFilesystemVariable.Value, accessNetworkDrivesVariable.Value)) {
            Log.Error("FilesystemBrowserLogic", $"Path variable '{pathVariable.Value.Value}' is invalid. Falling back to '%PROJECTDIR%\\'");
            startFolderPathResourceUri = resourceUriHelper.GetDefaultResourceUri();
            pathVariable.Value = startFolderPathResourceUri;
        }

        Browse(startFolderPathResourceUri);

        pathVariable.VariableChange += PathVariable_VariableChange;
    }

    public override void Stop() {
        pathVariable.VariableChange -= PathVariable_VariableChange;
    }

    private void PathVariable_VariableChange(object sender, VariableChangeEventArgs e) {
        var updatedPathResourceUri = new ResourceUri(e.NewValue);
        if (!resourceUriHelper.IsFolderPathAllowed(updatedPathResourceUri, accessFullFilesystemVariable.Value, accessNetworkDrivesVariable.Value)) {
            Log.Error("FilesystemBrowserLogic", $"Cannot browse to {updatedPathResourceUri} since this path is not allowed in current configuration");
            return;
        }

        Browse(updatedPathResourceUri);
    }

    private void Browse(ResourceUri resourceUri) {
        string path = resourceUri.Uri;

        if (path == string.Empty) {
            Log.Warning("FilesystemBrowserLogic", "Path variable is empty");
            return;
        }

        if (!Directory.Exists(path)) {
            Log.Warning("FilesystemBrowserLogic", $"Path '{path}' does not exist");
            return;
        }

        var currentDirectory = new DirectoryInfo(@path);
        var filesList = LogicObject.GetObject("FilesList");
        if (filesList == null)
            return;

        // Clean files list
        filesList.Children.ToList().ForEach((entry) => entry.Delete());

        // Create back entry
        if (BackEntryMustBeAdded(resourceUri)) {
            var backEntry = InformationModel.MakeObject<FileEntry>("back");
            backEntry.FileName = "..";
            backEntry.IsDirectory = true;
            filesList.Add(backEntry);
        }

        string extensions = filterVariable.Value;
        var extensionsList = extensions.Split(';').ToList();

        var directories = currentDirectory.GetFileSystemInfos().Where(entry => entry is DirectoryInfo &&
                                                                                FileHasToBeListed(entry));
        foreach (var dir in directories) {
            var fileSystemEntry = CreateFilesystemEntry(dir, true);
            filesList.Add(fileSystemEntry);
        }

        var files = currentDirectory.GetFileSystemInfos().Where(entry => entry is FileInfo &&
                                                                         FileHasToBeListed(entry));
        foreach (var file in files) {
            if (!AllFilesFilterSelected(extensionsList) && FileHasToBeFiltered(extensionsList, file))
                continue;

            var fileSystemEntry = CreateFilesystemEntry(file, false);
            filesList.Add(fileSystemEntry);
        }

        return;
    }

    // See https://docs.microsoft.com/it-it/dotnet/api/system.io.fileattributes?view=netcore-2.1 for additional information on FileAttributes values
    // Junction points must be filtered out on Windows and system files must be filtered out
    private bool FileHasToBeListed(FileSystemInfo fileSystemInfo) {
        bool showHiddenFiles = showHiddenFilesVariable.Value;
        return !fileSystemInfo.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
               !fileSystemInfo.Attributes.HasFlag(FileAttributes.System) &&
               (showHiddenFiles || !fileSystemInfo.Attributes.HasFlag(FileAttributes.Hidden));
    }

    private bool BackEntryMustBeAdded(ResourceUri resourceUri) {
        switch (resourceUri.UriType) {
            case UriType.ApplicationRelative:
                return !string.IsNullOrEmpty(resourceUri.ApplicationRelativePath);
            case UriType.ProjectRelative:
                return !string.IsNullOrEmpty(resourceUri.ProjectRelativePath);
            case UriType.USBRelative:
                return !string.IsNullOrEmpty(resourceUri.USBRelativePath);
            case UriType.AbsoluteFilePath:
                string path = resourceUri.Uri;
                return !PlatformConfigurationHelper.IsWindowsDriveBaseFolder(path) &&
                    !PlatformConfigurationHelper.IsGenericLinuxRoot(path);
        }

        return false;
    }

    private FileEntry CreateFilesystemEntry(FileSystemInfo entry, bool isDirectory) {
        var fileSystemEntry = InformationModel.MakeObject<FileEntry>(entry.Name);
        fileSystemEntry.FileName = entry.Name;
        fileSystemEntry.IsDirectory = isDirectory;
        if (!isDirectory) {
            var file = entry as FileInfo;
            fileSystemEntry.Size = (ulong)Math.Round(file.Length / 1000.0);
        }

        return fileSystemEntry;
    }

    private bool FileHasToBeFiltered(List<string> extensionsList, FileSystemInfo file) {
        return !extensionsList.Contains($"*{file.Extension}");
    }

    // All files are shown if the filter is empty or "*.*" is present in the filter list
    private bool AllFilesFilterSelected(List<string> extensionsList) {
        return extensionsList.Contains("*.*") || (extensionsList.Count == 1 && extensionsList.Contains(string.Empty));
    }

    private IUAVariable pathVariable;
    private IUAVariable filterVariable;
    private IUAVariable accessFullFilesystemVariable;
    private IUAVariable accessNetworkDrivesVariable;
    private IUAVariable showHiddenFilesVariable;
    private ResourceUriHelper resourceUriHelper;
}

namespace FilesystemBrowser {
    public class ResourceUriHelper {
        public ResourceUriHelper(int namespaceIndex) {
            this.namespaceIndex = namespaceIndex;
        }
        private IUAObject locationsObject;

        public IUAObject LocationsObject {
            set {
                if (locationsObject != value)
                    locationsObject = value;
            }
        }

        public string AddNamespacePrefixToQRuntimeFolder(string resourceUriString) {
            if (resourceUriString.StartsWith("%APPLICATIONDIR") || resourceUriString.StartsWith("%PROJECTDIR"))
                resourceUriString = $"ns={namespaceIndex};{resourceUriString}";

            return resourceUriString;
        }

        public bool IsResourceUriValid(ResourceUri resourceUri) {
            // Check that the start folder resource uri can be resolved (i.e. non existing USB device or mispelled path)
            string resolvedPath;
            try {
                resolvedPath = resourceUri.Uri;
            } catch (Exception) {
                return false;
            }

            if (!Directory.Exists(resolvedPath))
                return false;

            return true;
        }

        public bool IsResourceUriRelative(ResourceUri resourceUri) {
            return resourceUri.UriType == UriType.ApplicationRelative ||
                resourceUri.UriType == UriType.ProjectRelative ||
                resourceUri.UriType == UriType.USBRelative;
        }

        public bool IsFolderPathAllowed(ResourceUri startFolderPathResourceUri,
                                        bool accessFullFilesystem,
                                        bool accessNetworkDrives) {
            if (IsResourceUriRelative(startFolderPathResourceUri))
                return IsResourceUriValid(startFolderPathResourceUri);

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                return PlatformConfigurationHelper.IsFreeNavigationSupported() &&
                    accessFullFilesystem &&
                    IsResourceUriValid(startFolderPathResourceUri);
            }

            // Here we have a Windows full path
            FileInfo drivePathRoot = new FileInfo(Path.GetPathRoot(startFolderPathResourceUri.Uri));
            DriveInfo drive = new DriveInfo(drivePathRoot.FullName);

            bool isNetworkDrive = drive.DriveType == DriveType.Network;
            if (accessFullFilesystem && !isNetworkDrive)
                return IsResourceUriValid(startFolderPathResourceUri);

            bool isAccessibleNetworkDrive = isNetworkDrive && PlatformConfigurationHelper.IsWindowsNetworkDriveAccessible(startFolderPathResourceUri.Uri);
            if (accessFullFilesystem && isAccessibleNetworkDrive)
                return accessNetworkDrives && IsResourceUriValid(startFolderPathResourceUri);

            return false;
        }

        public ResourceUri GetDefaultResourceUri() {
            return new ResourceUri($"ns={namespaceIndex};%PROJECTDIR%\\");
        }

        public string GetDefaultResourceUriAsString() {
            return $"ns={namespaceIndex};%PROJECTDIR%\\";
        }

        public string GetRelativePathToLocationFromResourceUri(ResourceUri resourceUri) {
            string relativeFolderPath;
            switch (resourceUri.UriType) {
                case UriType.ApplicationRelative:
                    relativeFolderPath = resourceUri.ApplicationRelativePath;
                    break;
                case UriType.ProjectRelative:
                    relativeFolderPath = resourceUri.ProjectRelativePath;
                    break;
                case UriType.USBRelative:
                    relativeFolderPath = resourceUri.USBRelativePath;
                    break;
                case UriType.AbsoluteFilePath:
                    string baseLocation = GetBaseLocationPathFromLocationsObject(resourceUri);

                    // If a location exists, its value must be correct because it was created by this widget at startup
                    var resolvedBaseLocationPath = new ResourceUri(baseLocation).Uri;
                    relativeFolderPath = GetRelativePathToLocationFromAbsoluteSystemPath(resolvedBaseLocationPath, resourceUri.Uri);
                    break;
                default:
                    throw new CoreConfigurationException($"UriType '{resourceUri.UriType}' not expected");
            }

            return relativeFolderPath;
        }

        // Convert the updated full path (e.g. D:\\MyFolder\\SubFolder) to a string following QStudio conventions:
        // location base path + relative path that location (e.g. %USB1%/MyFolder\\SubFolder)
        // A string with this format can then be parsed back into a ResourceUri with the corresponding UriType
        public string GetQStudioFormattedPath(ResourceUri oldResourceUri,
            string updatedPath) {
            var baseLocationPath = GetBaseLocationPathFromLocationsObject(oldResourceUri);
            string relativePathToLocation;

            if (oldResourceUri.UriType == UriType.AbsoluteFilePath)
                relativePathToLocation = GetRelativePathToLocationFromAbsoluteSystemPath(baseLocationPath, updatedPath);
            else
                relativePathToLocation = GetRelativePathToLocationFromRelativeSystemPath(baseLocationPath, oldResourceUri.UriType, updatedPath);

            bool endsWithSlash = baseLocationPath.EndsWith("/") || baseLocationPath.EndsWith("\\");
            if (!endsWithSlash && !string.IsNullOrEmpty(relativePathToLocation))
                baseLocationPath += "/";

            return baseLocationPath + relativePathToLocation;
        }

        public string GetBaseLocationPathFromLocationsObject(ResourceUri resourceUri) {
            if (locationsObject == null)
                throw new CoreConfigurationException("Object Locations is not initialized");

            var baseLocation = locationsObject.GetVariable(GetBaseLocationBrowseName(resourceUri));
            if (baseLocation == null)
                throw new CoreConfigurationException($"Locations object is malformed");

            return baseLocation.Value;
        }

        private string GetRelativePathToLocationFromAbsoluteSystemPath(string baseLocationPath, string systemPath) {
            // Get the base location from the current resource uri value
            var resolvedBaseLocationPath = new ResourceUri(baseLocationPath).Uri;
            if (systemPath == resolvedBaseLocationPath)
                return string.Empty;

            return systemPath.Substring(resolvedBaseLocationPath.Length);
        }

        private string GetRelativePathToLocationFromRelativeSystemPath(string baseLocationPath, UriType uriType, string newFullPath) {
            // Extract the relative path from %APPLICATIONDIR%, %PROJECTDIR%, %USB<n>% by removing the computed baseLocationPath.
            // E.g. On Windows with 'D:\\MyFolder\\SubFolder' removing '%USB1%/'=='D:\\' results in 'MyFolder\\SubFolder'.
            // E.g. On Unix with '/storage/usb1/MyFolder/SubFolder' removing '%USB1%/'=='/storage/usb1' results in '/MyFolder/SubFolder'.
            var resolvedBaseLocationPathUri = new ResourceUri(baseLocationPath);
            var resultRelativePath = newFullPath.Substring(resolvedBaseLocationPathUri.Uri.Length);

            if (string.IsNullOrEmpty(resultRelativePath))
                return string.Empty;

            // On Unix the initial "/" must be removed always.
            // A Unix USB path has to be managed as a normal filesyestem path:
            // i.e. /storage/usb1/MyFolder has "/myFolder" as resultedRelativePath, so "/" has to be removed
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return resultRelativePath.Substring(1);

            // On Windows in case of %APPLICATIONDIR%, %PROJECTDIR% the initial "/" of resultRelativePath must be removed.
            // Windows USB path starts with <Drive>:/ so in that case the initial character has not to be removed:
            // i.e. D:/MyFolder has "MyFolder" has resultedRelativePath, so nothing has to be removed
            if (uriType != UriType.USBRelative)
                return resultRelativePath.Substring(1);

            return resultRelativePath;
        }

        private string GetBaseLocationBrowseName(ResourceUri resourceUri) {
            switch (resourceUri.UriType) {
                case UriType.ApplicationRelative:
                    return "APPLICATION_DIR";
                case UriType.ProjectRelative:
                    return "PROJECT_DIR";
                case UriType.USBRelative:
                    return $"USB{resourceUri.USBNumber}";
                case UriType.AbsoluteFilePath:
                    if (Environment.OSVersion.Platform == PlatformID.Unix) // Only Debian is supported
                        return PlatformConfigurationHelper.GetGenericLinuxBaseBrowseNameFolder();
                    else // Windows
                        return PlatformConfigurationHelper.GetWindowsDrivePathRoot(resourceUri);
                default:
                    return null;
            }
        }

        private readonly int namespaceIndex;
    }

    static class PlatformConfigurationHelper {
        public static string GetGenericLinuxBaseBrowseNameFolder() {
            return "root";
        }

        public static string GetGenericLinuxBaseFolderPath() {
            return "/";
        }

        public static bool IsWindowsDriveBaseFolder(string path) {
            return Path.GetPathRoot(path) == path;
        }

        public static bool IsGenericLinuxRoot(string path) {
            return path == "/";
        }

        public static string GetWindowsDrivePathRoot(ResourceUri resourceUri) {
            string rootPath;
            try {
                FileInfo pathInfo = new FileInfo(resourceUri.Uri);
                rootPath = Path.GetPathRoot(pathInfo.FullName);
            } catch (Exception exception) {
                throw new Exception($"Unable to get root path of '{resourceUri.Uri}': {exception.Message}");
            }

            return rootPath;
        }

        public static bool IsFreeNavigationSupported() {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                return true;

            string architecture;
            try {
                architecture = LaunchProcess("uname", "-m");
            } catch (Exception exception) {
                Log.Error("PlatformConfigurationHelper", $"Unable to determine architecture: {exception.Message}");
                return false;
            }

            // On Yocto ARM ASEM devices it is not possible to browse the filesystem
            if (architecture.StartsWith("arm", StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }

        public static bool IsWindowsNetworkDriveAccessible(string filePath) {
            var pathRoot = Path.GetPathRoot(filePath);
            var pathRootLetter = pathRoot.TrimEnd(new char[] { '\\' });

            string output;
            try {
                output = LaunchProcess("net", "use");
            } catch (Exception exception) {
                Log.Error("PlatformConfigurationHelper", $"Unable to determine connected network drives: {exception.Message}");
                return false;
            }

            foreach (string line in output.Split('\n')) {
                if (line.Contains(pathRootLetter) && line.Contains("OK")) {
                    return true;
                }
            }

            return false;
        }

        private static string LaunchProcess(string processName, string parameter) {
            string output;
            ProcessStartInfo processStartInfo = new ProcessStartInfo {
                FileName = processName,
                UseShellExecute = false,
                Arguments = parameter,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            Process process = Process.Start(processStartInfo);
            output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            process.Close();

            return output;
        }
    }
}
