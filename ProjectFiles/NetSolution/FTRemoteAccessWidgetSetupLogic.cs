#region Using directives
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.System;
using UAManagedCore;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class FTRemoteAccessWidgetSetupLogic : BaseNetLogic
{
    private const string LOG_CATEGORY = nameof(FTRemoteAccessWidgetSetupLogic);

    [ExportMethod]
    public void SetupFTRemoteAccessNode()
    {
        var ftRemoteAccessNodePointer = Owner.Get<NodePointer>("FTRemoteAccessNode");
        if (ftRemoteAccessNodePointer == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode property not defined.");
            return;
        }

        if (Owner.Context.GetNode(ftRemoteAccessNodePointer.Value) is not FTRemoteAccess ftRemoteAccessNode)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode is not defined.");
            return;
        }

        var eventArguments = InformationModel.MakeObject("EventArguments", FTOptix.System.ObjectTypes.RemoteConnectionRequestEvent);

        if (ftRemoteAccessNode.Get("RemoteConnectionRequestEventHandler") is not EventHandler eventHandlerNode)
        {
            eventHandlerNode = InformationModel.MakeObject<EventHandler>("RemoteConnectionRequestEventHandler");
            ftRemoteAccessNode.Add(eventHandlerNode);
            eventHandlerNode.ListenEventType = FTOptix.System.ObjectTypes.RemoteConnectionRequestEvent;
            eventHandlerNode.Add(eventArguments);
        }

        CreateVariables(ftRemoteAccessNode, eventHandlerNode, eventArguments);
    }

    private static void CreateVariables(FTRemoteAccess ftRemoteAccessNode,
                                        EventHandler eventHandlerNode,
                                        IUAObject eventArguments)
    {
        const string usernameVariableName = "FTRemoteAccessWidgetUsername";
        const string ipAddressVariableName = "FTRemoteAccessWidgetIPAddress";
        const string supervisorIdVariableName = "FTRemoteAccessWidgetSupervisorId";
        const string connectionPendingVariableName = "FTRemoteAccessWidgetConnectionPending";
        const string getErrorMessage = "Unable to get event argument: ";

        Log.Info(LOG_CATEGORY, "Creating FTRemoteAccess widget variables and Remote Connection Request event commands.");

        var usernameVariable = ftRemoteAccessNode.GetVariable(usernameVariableName);
        if (usernameVariable == null)
        {
            usernameVariable = InformationModel.MakeVariable(usernameVariableName, OpcUa.DataTypes.String);
            ftRemoteAccessNode.Add(usernameVariable);
        }

        var eventArgumentUsernameVariable = eventArguments.GetVariable("Username");
        if (eventArgumentUsernameVariable == null)
        {
            Log.Error(LOG_CATEGORY, getErrorMessage + "Username");
            return;
        }

        SetupEventHandler(eventHandlerNode, usernameVariable, OpcUa.DataTypes.String, eventArgumentUsernameVariable);

        var ipAddressVariable = ftRemoteAccessNode.GetVariable(ipAddressVariableName);
        if (ipAddressVariable == null)
        {
            ipAddressVariable = InformationModel.MakeVariable(ipAddressVariableName, OpcUa.DataTypes.String);
            ftRemoteAccessNode.Add(ipAddressVariable);
        }

        var eventArgumentIpAddressVariable = eventArguments.GetVariable("IpAddress");
        if (eventArgumentIpAddressVariable == null)
        {
            Log.Error(LOG_CATEGORY, getErrorMessage + "IpAddress");
            return;
        }

        SetupEventHandler(eventHandlerNode, ipAddressVariable, OpcUa.DataTypes.String, eventArgumentIpAddressVariable);

        var supervisorIdVariable = ftRemoteAccessNode.GetVariable(supervisorIdVariableName);
        if (supervisorIdVariable == null)
        {
            supervisorIdVariable = InformationModel.MakeVariable(supervisorIdVariableName, OpcUa.DataTypes.ByteString);
            ftRemoteAccessNode.Add(supervisorIdVariable);
        }

        var eventArgumentSupervisorIdVariable = eventArguments.GetVariable("SupervisorId");
        if (eventArgumentSupervisorIdVariable == null)
        {
            Log.Error(LOG_CATEGORY, getErrorMessage + "SupervisorId");
            return;
        }

        SetupEventHandler(eventHandlerNode, supervisorIdVariable, OpcUa.DataTypes.ByteString, eventArgumentSupervisorIdVariable);

        var connectionPendingVariable = ftRemoteAccessNode.GetVariable(connectionPendingVariableName);
        if (connectionPendingVariable == null)
        {
            connectionPendingVariable = InformationModel.MakeVariable(connectionPendingVariableName, OpcUa.DataTypes.Boolean);
            ftRemoteAccessNode.Add(connectionPendingVariable);
        }

        SetupEventHandler(eventHandlerNode, connectionPendingVariable, OpcUa.DataTypes.Boolean, null);
    }

    private static void SetupEventHandler(EventHandler eventHandlerNode,
                                          IUAVariable variableNode,
                                          NodeId valueType,
                                          IUAVariable eventArgumentVariable)
    {
        string methodContainerName = "MethodContainer" + variableNode.BrowseName;
        var methodContainerNode = eventHandlerNode.Find(methodContainerName);
        if (methodContainerNode != null)
            return;

        var methodContainer = InformationModel.MakeObject(methodContainerName);
        eventHandlerNode.MethodsToCall.Add(methodContainer);

        var objectPointerVariable = InformationModel.MakeVariable<NodePointer>("ObjectPointer", OpcUa.DataTypes.NodeId);
        objectPointerVariable.Value = InformationModel.GetObject(FTOptix.CoreBase.Objects.VariableCommands).NodeId;
        methodContainer.Add(objectPointerVariable);

        var methodNameVariable = InformationModel.MakeVariable("Method", OpcUa.DataTypes.String);
        methodNameVariable.Value = "Set";
        methodContainer.Add(methodNameVariable);

        var inputArguments = InformationModel.MakeObject("InputArguments");
        methodContainer.Add(inputArguments);

        var variableToModify = InformationModel.MakeVariable("VariableToModify", FTOptix.Core.DataTypes.VariablePointer);
        variableToModify.Value = variableNode.NodeId;
        inputArguments.Add(variableToModify);

        var valueVariable = InformationModel.MakeVariable("Value", valueType);
        if (eventArgumentVariable != null)
            valueVariable.SetDynamicLink(eventArgumentVariable, DynamicLinkMode.ReadWrite);
        if (valueType == OpcUa.DataTypes.Boolean)
            valueVariable.Value = true;
        inputArguments.Add(valueVariable);

        var arrayIndexVariable = InformationModel.MakeVariable("ArrayIndex", OpcUa.DataTypes.UInt32);
        arrayIndexVariable.ValueRank = ValueRank.ScalarOrOneDimension;
        inputArguments.Add(arrayIndexVariable);
    }
}
