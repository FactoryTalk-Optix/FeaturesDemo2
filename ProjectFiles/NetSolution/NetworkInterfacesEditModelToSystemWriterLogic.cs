#region Using directives
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FTOptix.NetLogic;
using FTOptix.System;
using UAManagedCore;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
#endregion

public class NetworkInterfacesEditModelToSystemWriterLogic : BaseNetLogic
{
    public NetworkInterfacesEditModelToSystemWriterLogic(IUAObject editModelNetworkInterfacesObject) => editModelNetworkInterfacesReader = new NetworkInterfacesEditModelReaderLogic(editModelNetworkInterfacesObject);

    public NetworkInterfacesEditModelToSystemWriterLogic() { }

    public void WriteEditModelToSystemModel(List<NetworkInterface> networkInterfaces)
    {
        foreach (var networkInterface in networkInterfaces)
        {
            string interfaceName = networkInterface.BrowseName;
            if (string.IsNullOrEmpty(interfaceName))
                continue;

            var dhcpEditModel = editModelNetworkInterfacesReader.GetDhcpVariable(interfaceName).Value;
            if (dhcpEditModel)
            {
                networkInterface.DHCPClientEnabled = dhcpEditModel;
                Log.Info(NetworkSettingsLogic.LOGGING_CATEGORY, $"DHCP is enabled for network interface {interfaceName}. The rest of the configuration will be ignored.");
                continue;
            }

            var ipAddressVariable = editModelNetworkInterfacesReader.GetIPAddressVariable(interfaceName);
            var maskVariable = editModelNetworkInterfacesReader.GetMaskVariable(interfaceName);
            var dns1Variable = editModelNetworkInterfacesReader.GetDNS1Variable(interfaceName);
            var dns2Variable = editModelNetworkInterfacesReader.GetDNS2Variable(interfaceName);
            var defaultGatewayVariable = editModelNetworkInterfacesReader.GetDefaultGatewayVariable(interfaceName);

            EditModelNetworkInterface editModelNetworkInterface = new()
            {
                interfaceName = interfaceName,
                dhcpEnabled = false,
                ipAddress = ipAddressVariable.Value,
                mask = maskVariable.Value,
                dns1 = dns1Variable.Value,
                dns2 = dns2Variable.Value,
                defaultGateway = defaultGatewayVariable.Value
            };

            if (!WriteStandardPropertiesIntoSystemModel(editModelNetworkInterface, networkInterface))
                continue;
        }
    }

    private static bool WriteStandardPropertiesIntoSystemModel(EditModelNetworkInterface editModelNetworkInterface, NetworkInterface networkInterface)
    {
        bool isStandardConfigurationValid = IPAddressValidator.IsStandardConfigurationValid(editModelNetworkInterface);
        if (!isStandardConfigurationValid)
        {
            Log.Error(NetworkSettingsLogic.LOGGING_CATEGORY, $"Configuration for network interface {editModelNetworkInterface.interfaceName} is not valid. The last valid configuration will be used.");
            return false;
        }

        networkInterface.DHCPClientEnabled = editModelNetworkInterface.dhcpEnabled;
        networkInterface.IPAddress = editModelNetworkInterface.ipAddress;
        networkInterface.IPAddressVariable.Mask = editModelNetworkInterface.mask;
        networkInterface.DNS1 = editModelNetworkInterface.dns1;
        networkInterface.DNS2 = editModelNetworkInterface.dns2;
        networkInterface.DefaultGateway = editModelNetworkInterface.defaultGateway;

        return true;
    }

    private struct EditModelNetworkInterface
    {
        public string interfaceName;
        public bool dhcpEnabled;
        public string ipAddress;
        public string mask;
        public string dns1;
        public string dns2;
        public string defaultGateway;
    };

    private readonly NetworkInterfacesEditModelReaderLogic editModelNetworkInterfacesReader;

    private class IPAddressValidator
    {
        public static bool IsStandardConfigurationValid(EditModelNetworkInterface editModelNetworkInterface)
        {
            bool isValid = true;
            string interfaceName = editModelNetworkInterface.interfaceName;
            string ipAddressString = editModelNetworkInterface.ipAddress;
            if (!IsValidIPAddress(ipAddressString))
            {
                Log.Error(NetworkSettingsLogic.LOGGING_CATEGORY, $"IP address {ipAddressString} for network interface {interfaceName} is invalid.");
                isValid = false;
            }

            string maskString = editModelNetworkInterface.mask;
            if (!IsValidIPAddress(maskString))
            {
                Log.Error(NetworkSettingsLogic.LOGGING_CATEGORY, $"Mask {maskString} for network interface {interfaceName} is invalid.");
                isValid = false;
            }

            string dns1String = editModelNetworkInterface.dns1;
            if (!string.IsNullOrEmpty(dns1String) && !IsValidIPAddress(dns1String))
            {
                Log.Error(NetworkSettingsLogic.LOGGING_CATEGORY, $"DNS1 {dns1String} for network interface {interfaceName} is invalid.");
                isValid = false;
            }

            string dns2String = editModelNetworkInterface.dns2;
            if (!string.IsNullOrEmpty(dns2String) && !IsValidIPAddress(dns2String))
            {
                Log.Error(NetworkSettingsLogic.LOGGING_CATEGORY, $"DNS2 {dns2String} for network interface {interfaceName} is invalid.");
                isValid = false;
            }

            string defaultGatewayString = editModelNetworkInterface.defaultGateway;
            if (!string.IsNullOrEmpty(defaultGatewayString) && !IsValidIPAddress(defaultGatewayString))
            {
                Log.Error(NetworkSettingsLogic.LOGGING_CATEGORY, $"DefaultGateway {defaultGatewayString} for network interface {interfaceName} is invalid.");
                isValid = false;
            }

            return isValid;
        }

        private static bool IsValidIPAddress(string ipAddress) => Regex.IsMatch(ipAddress, "^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
    }
}
