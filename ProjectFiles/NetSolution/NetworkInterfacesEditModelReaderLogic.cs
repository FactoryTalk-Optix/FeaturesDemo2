#region Using directives
using FTOptix.NetLogic;
using UAManagedCore;
#endregion

public class NetworkInterfacesEditModelReaderLogic : BaseNetLogic
{
    public NetworkInterfacesEditModelReaderLogic(IUAObject editModelNetworkInterfacesObject) => this.editModelNetworkInterfacesObject = editModelNetworkInterfacesObject;

    public NetworkInterfacesEditModelReaderLogic() { }

    public IUAVariable GetDhcpVariable(string interfaceName)
    {
        var networkInterfaceObject = editModelNetworkInterfacesObject.GetObject(interfaceName);
        var variable = networkInterfaceObject.GetVariable("DHCPClientEnabled");
        if (variable == null)
            throw new CoreConfigurationException("DHCPClientEnabled variable not found");

        return variable;
    }

    public IUAVariable GetIPAddressVariable(string interfaceName)
    {
        var networkInterfaceObject = editModelNetworkInterfacesObject.GetObject(interfaceName);
        var variable = networkInterfaceObject.GetVariable("IPAddress");
        if (variable == null)
            throw new CoreConfigurationException("IPAddress variable not found");

        return variable;
    }

    public IUAVariable GetMaskVariable(string interfaceName)
    {
        var variable = GetIPAddressVariable(interfaceName).GetVariable("Mask");
        if (variable == null)
            throw new CoreConfigurationException("Mask variable not found");

        return variable;
    }

    public IUAVariable GetDNS1Variable(string interfaceName)
    {
        var networkInterfaceObject = editModelNetworkInterfacesObject.GetObject(interfaceName);
        var variable = networkInterfaceObject.GetVariable("DNS1");
        if (variable == null)
            throw new CoreConfigurationException("DNS1 variable not found");

        return variable;
    }

    public IUAVariable GetDNS2Variable(string interfaceName)
    {
        var networkInterfaceObject = editModelNetworkInterfacesObject.GetObject(interfaceName);
        var variable = networkInterfaceObject.GetVariable("DNS2");
        if (variable == null)
            throw new CoreConfigurationException("DNS2 variable not found");

        return variable;
    }

    public IUAVariable GetDefaultGatewayVariable(string interfaceName)
    {
        var networkInterfaceObject = editModelNetworkInterfacesObject.GetObject(interfaceName);
        var variable = networkInterfaceObject.GetVariable("DefaultGateway");
        if (variable == null)
            throw new CoreConfigurationException("DefaultGateway variable not found");

        return variable;
    }

    public IUAObject GetAdditionalIPAddressesObject(string interfaceName)
    {
        var networkInterfaceObject = editModelNetworkInterfacesObject.GetObject(interfaceName);
        var additonalIPAddressesObject = networkInterfaceObject.GetObject("AdditionalIPAddresses");
        if (additonalIPAddressesObject == null)
            throw new CoreConfigurationException("AdditionalIPAddresses object not found");

        return additonalIPAddressesObject;
    }

    private readonly IUAObject editModelNetworkInterfacesObject;
}
