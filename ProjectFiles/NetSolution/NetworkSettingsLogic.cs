#region Using directives
using System;
using UAManagedCore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Linq;
#endregion

public class NetworkSettingsLogic : BaseNetLogic
{
    public const string LOGGING_CATEGORY = nameof(NetworkSettingsLogic);

    public override void Start()
    {
        IUAVariable systemNodePointer = Owner.GetVariable("SystemNode");
        if (systemNodePointer == null)
        {
            HandleInvalidConfiguration();
            Log.Error(LOGGING_CATEGORY, "SystemNode NodePointer not found.");
            return;
        }

        NodeId systemNodeId = (NodeId)systemNodePointer.Value;
        if (systemNodeId == null || systemNodeId == NodeId.Empty)
        {
            HandleInvalidConfiguration();
            Log.Error(LOGGING_CATEGORY, "SystemNode is not defined.");
            return;
        }

        systemNode = InformationModel.Get(systemNodeId) as FTOptix.System.System;

        if (systemNode == null || systemNode.NetworkInterfaces.Count < 1)
            HandleInvalidConfiguration();

        if (systemNode == null)
        {
            Log.Error(LOGGING_CATEGORY, "SystemNode not found.");
            return;
        }

        networkConfiguratorEditModels = Owner.GetObject("NetworkConfiguratorEditModels");
        if (networkConfiguratorEditModels == null)
            throw new CoreConfigurationException("NetworkConfiguratorEditModels object not found");

        NetworkConfiguratorEditModelsLogic.CreateEditModels(systemNode, networkConfiguratorEditModels);
        var editModelNetworkInterfaces = NetworkConfiguratorEditModelsLogic.GetNetworkInterfacesEditModel(networkConfiguratorEditModels);

        networkInterfacesEditModelToSystemWriter = new NetworkInterfacesEditModelToSystemWriterLogic(editModelNetworkInterfaces);

        navPanel = Owner.Get<NavigationPanel>("NavigationPanel1");
        if (navPanel == null)
            throw new CoreConfigurationException("NavigationPanel1 object not found");

        systemNode.RebootRequiredVariable.VariableChange += RebootRequiredVariable_VariableChange;
        EnableControlsForRebootRequired(systemNode.RebootRequired);

        navPanel.CurrentPanelVariable.VariableChange += CurrentPanelVariable_VariableChange;

        NodeId nodeId = (NodeId)navPanel.CurrentPanel;
        if (nodeId != null && nodeId != NodeId.Empty)
            HandleNetworkInterfacePanel(nodeId);
    }

    public override void Stop()
    {
        if (networkConfiguratorEditModels != null)
            NetworkConfiguratorEditModelsLogic.DeleteEditModels(networkConfiguratorEditModels);

        if (systemNode != null)
            systemNode.RebootRequiredVariable.VariableChange -= RebootRequiredVariable_VariableChange;

        if (navPanel != null)
            navPanel.CurrentPanelVariable.VariableChange -= CurrentPanelVariable_VariableChange;

        systemNode = null;
        networkConfiguratorEditModels = null;
        networkInterfacesEditModelToSystemWriter = null;
        networkInterfaceLogic = null;
        navPanel = null;
    }

    [ExportMethod]
    public void ApplyNetworkConfiguration()
    {
        try
        {
            networkInterfacesEditModelToSystemWriter.WriteEditModelToSystemModel(systemNode.NetworkInterfaces.ToList());
        }
        catch (Exception ex)
        {
            Log.Error(LOGGING_CATEGORY, $"Failed to apply network configuration: {ex.Message}");
        }
    }

    [ExportMethod]
    public void Reboot()
    {
        if (systemNode == null)
        {
            Log.Error(LOGGING_CATEGORY, "SystemNode reference not defined. Reboot failed.");
            return;
        }

        systemNode.Reboot();
    }

    private void CurrentPanelVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        HandleNetworkInterfacePanel((NodeId)e.NewValue.Value);
    }

    private void RebootRequiredVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        EnableControlsForRebootRequired((bool)e.NewValue.Value);
    }

    private void HandleNetworkInterfacePanel(NodeId nodeId)
    {
        if (nodeId == null || nodeId == NodeId.Empty)
            return;

        IUANode node = InformationModel.Get(nodeId);

        networkInterfaceLogic = new NetworkInterfaceLogic();
        networkInterfaceLogic.SetMembers(systemNode, node.BrowseName, networkConfiguratorEditModels, node);
    }

    private void EnableControlsForRebootRequired(bool rebootRequired)
    {
        Button rebootButton = Owner.Get<Button>("BottomBar/RebootButton");
        if (rebootButton == null)
            throw new CoreConfigurationException("BottomBar/RebootButton object not found");

        rebootButton.Enabled = rebootRequired;

        Image warningIcon = Owner.Get<Image>("BottomBar/WarningImage");
        if (warningIcon == null)
            throw new CoreConfigurationException("BottomBar/WarningImage object not found");

        warningIcon.Visible = rebootRequired;

        Label messageLabel = Owner.Get<Label>("BottomBar/MessageLabel");
        if (messageLabel == null)
            throw new CoreConfigurationException("BottomBar/MessageLabel object not found");

        messageLabel.Visible = rebootRequired;
    }

    private void HandleInvalidConfiguration()
    {
        Button applyButton = Owner.Get<Button>("BottomBar/ApplyButton");
        if (applyButton != null)
            applyButton.Enabled = false;

        EnableControlsForRebootRequired(false);
    }

    private FTOptix.System.System systemNode;

    private IUAObject networkConfiguratorEditModels;
    private NetworkInterfacesEditModelToSystemWriterLogic networkInterfacesEditModelToSystemWriter;

    private NetworkInterfaceLogic networkInterfaceLogic;
    private NavigationPanel navPanel;
}
