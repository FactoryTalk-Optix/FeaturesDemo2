#region Using directives
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using FTOptix.MQTTClient;
#endregion

public class NetworkInterfaceLogic : BaseNetLogic
{
    public override void Stop()
    {
        if (doesThisNetworkInterfaceExist && dhcpCheckbox != null)
            dhcpCheckbox.CheckedVariable.VariableChange -= DhcpChecked_VariableChange;

        editModelNetworkInterfacesReader = null;
        dhcpCheckbox = null;
        ipAddressTextBox = null;
        maskTextBox = null;
        defaultGatewayTextBox = null;
        dns1TextBox = null;
        dns2TextBox = null;
    }

    public void SetMembers(FTOptix.System.System systemNode, string interfaceName, IUAObject networkConfiguratorEditModelsObject, IUANode panelObject)
    {
        this.interfaceName = interfaceName;

        dhcpCheckbox = panelObject.Find<CheckBox>("DHCPClientEnabledCheckbox");
        if (dhcpCheckbox == null)
            throw new CoreConfigurationException("LAN DHCPClientEnabledCheckbox not found");

        ipAddressTextBox = panelObject.Find<TextBox>("IPAddressTextBox");
        if (ipAddressTextBox == null)
            throw new CoreConfigurationException("LAN IPAddressTextBox not found");

        maskTextBox = panelObject.Find<TextBox>("MaskTextBox");
        if (maskTextBox == null)
            throw new CoreConfigurationException("LAN MaskTextBox not found");

        defaultGatewayTextBox = panelObject.Find<TextBox>("DefaultGatewayTextBox");
        if (defaultGatewayTextBox == null)
            throw new CoreConfigurationException("LAN DefaultGatewayTextBox not found");

        dns1TextBox = panelObject.Find<TextBox>("DNS1TextBox");
        if (dns1TextBox == null)
            throw new CoreConfigurationException("LAN DNS1TextBox not found");

        dns2TextBox = panelObject.Find<TextBox>("DNS2TextBox");
        if (dns2TextBox == null)
            throw new CoreConfigurationException("LAN DNS2TextBox not found");

        NetworkConfiguratorEditModelsLogic.CreateEditModels(systemNode, networkConfiguratorEditModelsObject);
        var editModelNetworkInterfaces = NetworkConfiguratorEditModelsLogic.GetNetworkInterfacesEditModel(networkConfiguratorEditModelsObject);

        editModelNetworkInterfacesReader = new NetworkInterfacesEditModelReaderLogic(editModelNetworkInterfaces);

        var networkInterfaceObject = editModelNetworkInterfaces.GetObject(interfaceName);
        if (networkInterfaceObject == null)
        {
            if (panelObject is Panel panel)
                panel.Enabled = false;

            doesThisNetworkInterfaceExist = false;
        }
        else
        {
            SetDynamicLinksToEditModel(this.interfaceName);
            SetControlsEnabled();
            dhcpCheckbox.CheckedVariable.VariableChange += DhcpChecked_VariableChange;
        }
    }

    private void DhcpChecked_VariableChange(object sender, VariableChangeEventArgs e) => SetControlsEnabled();

    private void SetDynamicLinksToEditModel(string interfaceName)
    {
        var checkedVariable = dhcpCheckbox.CheckedVariable;
        checkedVariable.ResetDynamicLink();
        checkedVariable.SetDynamicLink(editModelNetworkInterfacesReader.GetDhcpVariable(interfaceName), DynamicLinkMode.ReadWrite);

        var ipAddressTextBoxTextVariable = ipAddressTextBox.TextVariable;
        ipAddressTextBoxTextVariable.ResetDynamicLink();
        ipAddressTextBoxTextVariable.SetDynamicLink(editModelNetworkInterfacesReader.GetIPAddressVariable(interfaceName), DynamicLinkMode.ReadWrite);

        var maskTextBoxTextVariable = maskTextBox.TextVariable;
        maskTextBoxTextVariable.ResetDynamicLink();
        maskTextBoxTextVariable.SetDynamicLink(editModelNetworkInterfacesReader.GetMaskVariable(interfaceName), DynamicLinkMode.ReadWrite);

        var dns1TextBoxTextVariable = dns1TextBox.TextVariable;
        dns1TextBoxTextVariable.ResetDynamicLink();
        dns1TextBoxTextVariable.SetDynamicLink(editModelNetworkInterfacesReader.GetDNS1Variable(interfaceName), DynamicLinkMode.ReadWrite);

        var dns2TextBoxTextVariable = dns2TextBox.TextVariable;
        dns2TextBoxTextVariable.ResetDynamicLink();
        dns2TextBoxTextVariable.SetDynamicLink(editModelNetworkInterfacesReader.GetDNS2Variable(interfaceName), DynamicLinkMode.ReadWrite);

        var defaultGatewayTextBoxTextVariable = defaultGatewayTextBox.TextVariable;
        defaultGatewayTextBoxTextVariable.ResetDynamicLink();
        defaultGatewayTextBoxTextVariable.SetDynamicLink(editModelNetworkInterfacesReader.GetDefaultGatewayVariable(interfaceName), DynamicLinkMode.ReadWrite);
    }

    private void SetControlsEnabled()
    {
        if (dhcpCheckbox.Checked)
        {
            ipAddressTextBox.Enabled = false;
            maskTextBox.Enabled = false;
            defaultGatewayTextBox.Enabled = false;
            dns1TextBox.Enabled = false;
            dns2TextBox.Enabled = false;
        }
        else
        {
            ipAddressTextBox.Enabled = true;
            maskTextBox.Enabled = true;
            defaultGatewayTextBox.Enabled = true;
            dns1TextBox.Enabled = true;
            dns2TextBox.Enabled = true;
        }
    }

    private string interfaceName;

    private NetworkInterfacesEditModelReaderLogic editModelNetworkInterfacesReader;

    private CheckBox dhcpCheckbox;
    private TextBox ipAddressTextBox;
    private TextBox maskTextBox;
    private TextBox defaultGatewayTextBox;
    private TextBox dns1TextBox;
    private TextBox dns2TextBox;

    private bool doesThisNetworkInterfaceExist = true;
}
