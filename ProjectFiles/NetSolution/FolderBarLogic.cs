#region Using directives
using System;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.Core;
using System.IO;
using System.Collections.Generic;
using System.Linq;
#endregion

using FilesystemBrowser;

public class FolderBarLogic : BaseNetLogic {
    public override void Start() {
        isFreeNavigationSupportedForCurrentPlatform = PlatformConfigurationHelper.IsFreeNavigationSupported();

        // Path variables
        pathVariable = LogicObject.GetVariable("Path");
        if (pathVariable == null)
            throw new CoreConfigurationException("Path variable not found in FilesystemBrowserLogic");

        // FolderBar variables
        locationsComboBox = (ComboBox)Owner.GetObject("Locations");
        if (locationsComboBox == null)
            throw new CoreConfigurationException("Locations combo box not found");

        relativePathTextBox = (TextBox)Owner.GetObject("RelativePath");
        if (relativePathTextBox == null)
            throw new CoreConfigurationException("RelativePath textbox not found");

        accessFullFilesystemVariable = Owner.Owner.GetVariable("AccessFullFilesystem");
        if (accessFullFilesystemVariable == null)
            throw new CoreConfigurationException("AccessFullFilesystem variable not found");

        if (accessFullFilesystemVariable.Value && !isFreeNavigationSupportedForCurrentPlatform)
            return;

        accessNetworkDrivesVariable = Owner.Owner.GetVariable("AccessNetworkDrives");
        if (accessNetworkDrivesVariable == null)
            throw new CoreConfigurationException("AccessNetworkDrives variable not found");

        // In case of invalid path the fallback is %PROJECTDIR%
        var startFolderPathResourceUri = new ResourceUri(pathVariable.Value);

        resourceUriHelper = new ResourceUriHelper(LogicObject.NodeId.NamespaceIndex);
        if (!resourceUriHelper.IsFolderPathAllowed(startFolderPathResourceUri, accessFullFilesystemVariable.Value, accessNetworkDrivesVariable.Value)) {
            startFolderPathResourceUri = resourceUriHelper.GetDefaultResourceUri();
            pathVariable.Value = startFolderPathResourceUri;
        }

        locationsObject = Owner.Owner.GetObject("Locations");
        if (locationsObject == null)
            throw new CoreConfigurationException("Locations object not found");

        InitalizeLocationsObject();
        resourceUriHelper.LocationsObject = locationsObject;

        InitializeComboBoxAndTextBox(startFolderPathResourceUri);

        pathVariable.VariableChange += PathVariable_VariableChange;
        locationsComboBox.SelectedValueVariable.VariableChange += SelectedValueComboBox_VariableChange;
        relativePathTextBox.OnUserTextChanged += RelativePathTextBox_UserTextChanged;

        periodicTaskUsbUpdater = new PeriodicTask(UpdateUsbDevices, usbCheckMillisecondsPeriod, LogicObject);
        periodicTaskUsbUpdater.Start();

        if (Environment.OSVersion.Platform != PlatformID.Unix) {
            periodicTaskSystemDrivesUpdater = new PeriodicTask(UpdateSystemDrives, usbCheckMillisecondsPeriod, LogicObject);
            periodicTaskSystemDrivesUpdater.Start();
        }
    }

    public override void Stop() {
        pathVariable.VariableChange -= PathVariable_VariableChange;
        locationsComboBox.SelectedValueVariable.VariableChange -= SelectedValueComboBox_VariableChange;
        relativePathTextBox.OnUserTextChanged -= RelativePathTextBox_UserTextChanged;
        periodicTaskUsbUpdater.Cancel();
        if (Environment.OSVersion.Platform != PlatformID.Unix) {
            periodicTaskSystemDrivesUpdater.Cancel();
        }
    }

    private void SelectedValueComboBox_VariableChange(object sender, VariableChangeEventArgs e) {
        // Clear RelativePath Textbox and update the current path (a new browse is made)
        SetTextBoxValue(string.Empty);
        pathVariable.Value = new ResourceUri(e.NewValue);
    }

    private void RelativePathTextBox_UserTextChanged(object sender, UserTextChangedEvent e) {
        var updatedRelativePathString = e.NewText.Text;

        if (updatedRelativePathString.Contains("..")) {
            Log.Error("FolderBarLogic", $"Input is incorrect: '..' is not supported");
            SetTextBoxValue(lastTexboxValidText);
            return;
        }

        if (Path.IsPathRooted(updatedRelativePathString)) {
            Log.Error("FolderBarLogic", "Input is incorrect: cannot insert a full path");
            SetTextBoxValue(lastTexboxValidText);
            return;
        }

        // Update pathVariable value with the text inserted into the textbox
        string qstudioBasePath = (string)locationsComboBox.SelectedValue;

        bool endsWithSlash = qstudioBasePath.EndsWith("/") || qstudioBasePath.EndsWith("\\");
        if (!endsWithSlash && !string.IsNullOrEmpty(updatedRelativePathString))
            qstudioBasePath += "/";

        string updatedPathResourceUriString = qstudioBasePath + updatedRelativePathString;
        ResourceUri updatedResourceUri = new ResourceUri(updatedPathResourceUriString);
        if (!resourceUriHelper.IsResourceUriValid(updatedResourceUri)) {
            Log.Error("FolderBarLogic", $"Input is incorrect: folder path {updatedPathResourceUriString} does not exist");
            SetTextBoxValue(lastTexboxValidText);
            return;
        }

        pathVariable.Value = qstudioBasePath + updatedRelativePathString;

    }

    private void PathVariable_VariableChange(object sender, VariableChangeEventArgs e) {
        var updatedPathResourceUri = new ResourceUri(e.NewValue);

        // Update the textbox with the relative path to the current selected location
        try {
            SetTextBoxValue(resourceUriHelper.GetRelativePathToLocationFromResourceUri(updatedPathResourceUri));
        } catch (Exception exception) {
            Log.Error("FolderBarLogic", $"Unable to set value on textbox. Path '{e.NewValue.Value}' not found: {exception.Message}");
            return;
        }
    }

    private void InitalizeLocationsObject() {
        // Set the namespace prefix to %APPLICATIONDIR% and %PROJECTDIR%
        AddNamespacePrefixToStandardLocations();

        // Detect fixed drives
        if (accessFullFilesystemVariable.Value) {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                InitializeSystemDrives();
            else
                InitializeLinuxRootFolder();
        }

        // Detect connected USB devices
        InitializeUsbDevices();
    }

    private void AddNamespacePrefixToStandardLocations() {
        foreach (IUAVariable location in locationsObject.Children)
            location.Value = resourceUriHelper.AddNamespacePrefixToQRuntimeFolder((string)location.Value.Value);
    }

    private void InitializeSystemDrives() {
        if (!accessFullFilesystemVariable.Value)
            return;

        DriveInfo[] drives;
        try {
            drives = DriveInfo.GetDrives();
        } catch (Exception exception) {
            Log.Error("FolderBarLogic", $"Unable to get the list of system drives: {exception.Message}");
            return;
        }

        currentlyConnectedSystemDrives = new List<string>();
        foreach (var drive in drives) {
            string driveName = drive.RootDirectory.FullName;
            var systemDriveNode = locationsObject.GetVariable(driveName);
            bool systemdriveNodeAlreadyExists = systemDriveNode != null;

            bool isFixedDrive = drive.DriveType == DriveType.Fixed;
            bool isNetworkDrive = drive.DriveType == DriveType.Network;

            // A network drive is valid only if AccessFullFilesystem and AccessNetworkDrives are true
            if (isNetworkDrive && !accessNetworkDrivesVariable.Value)
                continue;

            // Check if network drives are reachable
            if (isNetworkDrive && !PlatformConfigurationHelper.IsWindowsNetworkDriveAccessible(driveName))
                continue;

            // Drive is reachable
            if (isFixedDrive || isNetworkDrive) {
                // Assumption: in the small periodic task update period (e.g. 5secs) there is collision of system drive names.
                // For example in that period we assume that this case is not possible:
                // D:/ is removed and then a different disk is inserted with the same drive name D:/
                currentlyConnectedSystemDrives.Add(driveName);
                if (systemdriveNodeAlreadyExists)
                    continue;

                AddLocation(driveName,
                    ResourceUri.FromAbsoluteFilePath(driveName),
                    $"WindowsDrive_{driveName}",
                    driveName);
            }
        }
    }

    private void UpdateSystemDrives() {
        if (!accessFullFilesystemVariable.Value)
            return;

        // If a disk (external hard drive or network drive) is removed, it is not retrieved by DriveInfo.GetDrives()
        var initialConnectedSystemDrives = currentlyConnectedSystemDrives;
        InitializeSystemDrives();

        if (initialConnectedSystemDrives.Count == currentlyConnectedSystemDrives.Count)
            return;

        if (initialConnectedSystemDrives.Count < currentlyConnectedSystemDrives.Count) {
            var connectedDrives = currentlyConnectedSystemDrives.Except(initialConnectedSystemDrives).ToList();
            foreach (var drive in connectedDrives)
                Log.Info("FolderBarLogic", $"Drive {drive} has been connected");
        }

        // Check if a system drive in locations is no longer attached
        if (initialConnectedSystemDrives.Count > currentlyConnectedSystemDrives.Count) {
            var disconnectedDrives = initialConnectedSystemDrives.Except(currentlyConnectedSystemDrives).ToList();
            foreach (var drive in disconnectedDrives) {
                var driveNode = locationsObject.Find(drive);
                if (driveNode != null) {
                    Log.Info("FolderBarLogic", $"Drive {drive} has been disconnected");
                    locationsObject.Remove(driveNode);

                    // Fallback to %PROJECTDIR% is currently selected drive is disconnected
                    ResourceUri selectedComboBoxValue = new ResourceUri((string)locationsComboBox.SelectedValue);
                    var currentlySelectedDriveRoot = PlatformConfigurationHelper.GetWindowsDrivePathRoot(selectedComboBoxValue);
                    if (currentlySelectedDriveRoot == drive)
                        locationsComboBox.SelectedValue = resourceUriHelper.GetDefaultResourceUriAsString();
                }
            }
        }
    }

    private void InitializeLinuxRootFolder() {
        // Non supported platforms was already filtered out
        // Only Debian is supported
        string linuxRootBrowseName = PlatformConfigurationHelper.GetGenericLinuxBaseBrowseNameFolder();
        string linuxRootResourceUri = ResourceUri.FromAbsoluteFilePath(PlatformConfigurationHelper.GetGenericLinuxBaseFolderPath());
        AddLocation(linuxRootBrowseName,
            linuxRootResourceUri,
            "LinuxRoot",
            linuxRootBrowseName);
    }

    private void InitializeUsbDevices() {
        uint connectedUsbDevices = 0;
        for (uint i = 1; i <= maxConnectedUsbDevices; ++i) {
            var usbLocationBrowseName = $"USB{i}";
            var usbResourceUri = new ResourceUri($"%{usbLocationBrowseName}%");
            var usbDeviceNode = locationsObject.GetVariable(usbLocationBrowseName);
            if (!resourceUriHelper.IsResourceUriValid(usbResourceUri)) {
                // USB<i> location is invalid but it exists
                if (usbDeviceNode != null)
                    locationsObject.Remove(usbDeviceNode);

                break;
            }

            connectedUsbDevices++;

            // USB<i> location is valid and it already exists
            if (usbDeviceNode != null)
                continue;

            string usbName;
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                usbName = $"USB {i} ({Path.GetPathRoot(usbResourceUri.Uri)})";
            else
                usbName = $"USB {i}";

            AddLocation($"USB{i}",
                usbResourceUri,
                "ComboBoxFileSelectorUSBDisplayName",
                usbName);
        }

        currentlyConnectedUsbDevices = connectedUsbDevices;
    }

    private void UpdateUsbDevices() {
        uint initialConnectedUsbDevices = currentlyConnectedUsbDevices;
        InitializeUsbDevices();

        if (currentlyConnectedUsbDevices == initialConnectedUsbDevices)
            return;

        bool usbConnected = currentlyConnectedUsbDevices > initialConnectedUsbDevices;
        string usbStatus = (usbConnected) ? "connected" : "disconnected";
        Log.Info("FolderBarLogic", $"USB mass storage device has been {usbStatus}");

        ResourceUri selectedComboBoxValue = new ResourceUri((string)locationsComboBox.SelectedValue);
        if (selectedComboBoxValue.UriType != UriType.USBRelative)
            return;

        // If a USB stick device is currently selected and USB devices changes it is now invalid.
        // 1. Disconnection of a USB
        // E.g. USB1 and USB2 are detected at start. Then USB1 is disconnected. USB2 is promoted by QStudio as USB1:
        // if we are browsing USB1 the current view and path values are outdated (because USB1 is a different device now);
        // if we are browsing USB2 the current view and path values are outdated (because USB2 does not exist anymore)
        // 2. Connection of a USB
        // E.g. USB1 is connected at start. USB2 is connected later. It is not guaranteed that the enumeration preserves the order.
        // It depends on the physical port: if USB2 physical port is enumerated before USB1 physical port, then values are invalid.
        // We must fallback to %PROJECTDIR%
        locationsComboBox.SelectedValue = resourceUriHelper.GetDefaultResourceUriAsString();
    }

    private void AddLocation(string locationVariableBrowseName,
        string locationVariableValue,
        string locationVariableDisplayName,
        string locationVariableDisplayNameValue) {
        var locationVariable = InformationModel.MakeVariable(locationVariableBrowseName, FTOptix.Core.DataTypes.ResourceUri);
        locationVariable.Value = locationVariableValue;

        var localeIds = Session.User.LocaleId;
        if (String.IsNullOrEmpty(localeIds))
            Log.Error("FolderBarLogic", "No locales found for the current user");

        //foreach (var localeId in localeIds)
        locationVariable.DisplayName = new LocalizedText(locationVariableDisplayName, locationVariableDisplayNameValue, localeIds);

        locationsObject.Add(locationVariable);
    }

    private void InitializeComboBoxAndTextBox(ResourceUri startFolderPathResourceUri) {
        locationsComboBox.SelectedValue = resourceUriHelper.GetBaseLocationPathFromLocationsObject(startFolderPathResourceUri);
        SetTextBoxValue(resourceUriHelper.GetRelativePathToLocationFromResourceUri(startFolderPathResourceUri));
    }

    private void SetTextBoxValue(string text) {
        lastTexboxValidText = text;
        relativePathTextBox.Text = lastTexboxValidText;
    }

    private IUAVariable pathVariable;

    // FolderBar variables
    private TextBox relativePathTextBox;
    private ComboBox locationsComboBox;
    private IUAObject locationsObject;

    private IUAVariable accessFullFilesystemVariable;
    private IUAVariable accessNetworkDrivesVariable;
    private bool isFreeNavigationSupportedForCurrentPlatform;

    private PeriodicTask periodicTaskUsbUpdater;
    private PeriodicTask periodicTaskSystemDrivesUpdater;

    private readonly uint maxConnectedUsbDevices = 5;

    private readonly int usbCheckMillisecondsPeriod = 5000;
    private uint currentlyConnectedUsbDevices = 0;
    private List<string> currentlyConnectedSystemDrives;

    private string lastTexboxValidText = "";
    private ResourceUriHelper resourceUriHelper;
}
