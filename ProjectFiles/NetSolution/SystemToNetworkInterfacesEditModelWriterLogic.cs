#region Using directives
using UAManagedCore;
using FTOptix.HMIProject;
using FTOptix.System;
using FTOptix.NetLogic;
using System.Collections.Generic;
#endregion

public class SystemToNetworkInterfacesEditModelWriterLogic : BaseNetLogic
{
    public SystemToNetworkInterfacesEditModelWriterLogic(IUAObject editModelNetworkInterfacesObject)
    {
        this.editModelNetworkInterfacesObject = editModelNetworkInterfacesObject;
        editModelNetworkInterfacesElementsReader = new NetworkInterfacesEditModelReaderLogic(editModelNetworkInterfacesObject);
    }

    public SystemToNetworkInterfacesEditModelWriterLogic() {}

    public void InitializeEditModel(List<NetworkInterface> networkInterfaces)
    {
        foreach (var networkInterface in networkInterfaces)
        {
            if (string.IsNullOrEmpty(networkInterface.BrowseName))
                continue;

            editModelNetworkInterfacesObject.Add(CreateNetworkInterfaceObject(networkInterface.BrowseName));
            WriteMandatoryPropertiesIntoEditModel(networkInterface);
        }
    }

    private static IUAObject CreateNetworkInterfaceObject(string interfaceName)
    {
        var interfaceObject = InformationModel.MakeObject(interfaceName);
        var dhcpVariable = InformationModel.MakeVariable("DHCPClientEnabled", UAManagedCore.OpcUa.DataTypes.Boolean);
        var ipVariable = InformationModel.MakeVariable("IPAddress", UAManagedCore.OpcUa.DataTypes.String);
        var maskVariable = InformationModel.MakeVariable("Mask", UAManagedCore.OpcUa.DataTypes.String);
        ipVariable.Add(maskVariable);
        var dns1Variable = InformationModel.MakeVariable("DNS1", UAManagedCore.OpcUa.DataTypes.String);
        var dns2Variable = InformationModel.MakeVariable("DNS2", UAManagedCore.OpcUa.DataTypes.String);
        var defaultGatewayVariable = InformationModel.MakeVariable("DefaultGateway", UAManagedCore.OpcUa.DataTypes.String);

        interfaceObject.Add(dhcpVariable);
        interfaceObject.Add(ipVariable);
        interfaceObject.Add(dns1Variable);
        interfaceObject.Add(dns2Variable);
        interfaceObject.Add(defaultGatewayVariable);
        return interfaceObject;
    }

    private void WriteMandatoryPropertiesIntoEditModel(NetworkInterface networkInterface)
    {
        string interfaceName = networkInterface.BrowseName;

        var dhcpVariable = editModelNetworkInterfacesElementsReader.GetDhcpVariable(interfaceName);
        dhcpVariable.Value = networkInterface.DHCPClientEnabled;

        var ipAddressVariable = editModelNetworkInterfacesElementsReader.GetIPAddressVariable(interfaceName);
        ipAddressVariable.Value = networkInterface.IPAddress;

        var maskVariable = editModelNetworkInterfacesElementsReader.GetMaskVariable(interfaceName);
        maskVariable.Value = networkInterface.IPAddressVariable.Mask;

        var dns1Variable = editModelNetworkInterfacesElementsReader.GetDNS1Variable(interfaceName);
        dns1Variable.Value = networkInterface.DNS1;

        var dns2Variable = editModelNetworkInterfacesElementsReader.GetDNS2Variable(interfaceName);
        dns2Variable.Value = networkInterface.DNS2;

        var defaultGatewayVariable = editModelNetworkInterfacesElementsReader.GetDefaultGatewayVariable(interfaceName);
        defaultGatewayVariable.Value = networkInterface.DefaultGateway;
    }

    readonly private IUAObject editModelNetworkInterfacesObject;
    readonly private NetworkInterfacesEditModelReaderLogic editModelNetworkInterfacesElementsReader;
}
