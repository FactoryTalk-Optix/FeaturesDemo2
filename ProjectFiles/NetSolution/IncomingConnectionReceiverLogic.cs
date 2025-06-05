#region Using directives
using FTOptix.NetLogic;
using FTOptix.System;
using UAManagedCore;
using FTOptix.MQTTClient;
#endregion

public class IncomingConnectionReceiverLogic : BaseNetLogic
{
    private const string LOG_CATEGORY = nameof(IncomingConnectionReceiverLogic);

    public override void Start()
    {
        bool cancelStart = false;

        ftRemoteAccessWidgetDataObject = Owner.Get<FTRemoteAccessWidgetDataObject>("FTRemoteAccessWidgetDataObject");
        if (ftRemoteAccessWidgetDataObject == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessWidgetDataObject" + notDefinedMessage);
            return;
        }

        ftRemoteAccessNode = ftRemoteAccessWidgetDataObject.Context.GetNode(ftRemoteAccessWidgetDataObject.FTRemoteAccessNode) as FTRemoteAccess;
        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode" + notDefinedMessage);
            return;
        }

        connectionPendingVariable = ftRemoteAccessNode.GetVariable(connectionPendingVariableName);
        if (connectionPendingVariable == null)
        {
            Log.Error(LOG_CATEGORY, connectionPendingVariableName + notDefinedMessage);
            cancelStart = true;
        }

        usernameVariable = ftRemoteAccessNode.GetVariable(usernameVariableName);
        if (usernameVariable == null)
        {
            Log.Error(LOG_CATEGORY, usernameVariableName + notDefinedMessage);
            cancelStart = true;
        }

        ipAddressVariable = ftRemoteAccessNode.GetVariable(ipAddressVariableName);
        if (ipAddressVariable == null)
        {
            Log.Error(LOG_CATEGORY, ipAddressVariableName + notDefinedMessage);
            cancelStart = true;
        }

        supervisorIdVariable = ftRemoteAccessNode.GetVariable(supervisorIdVariableName);
        if (supervisorIdVariable == null)
        {
            Log.Error(LOG_CATEGORY, supervisorIdVariableName + notDefinedMessage);
            cancelStart = true;
        }

        if (cancelStart)
        {
            DestroyClassFields();
            return;
        }

        //handle the case where an incoming connection request may already be pending at startup of the widget
        SetFTRemoteAccessWidgetDataObjectValues(connectionPendingVariable.Value);

        connectionPendingVariable.VariableChange += HandleConnectionPendingVariableChangedEvent;
    }

    public override void Stop()
    {
        if (connectionPendingVariable != null)
            connectionPendingVariable.VariableChange -= HandleConnectionPendingVariableChangedEvent;

        DestroyClassFields();
    }

    private void DestroyClassFields()
    {
        ftRemoteAccessNode = null;
        ftRemoteAccessWidgetDataObject = null;
        connectionPendingVariable = null;
        usernameVariable = null;
        ipAddressVariable = null;
        supervisorIdVariable = null;
    }

    private void HandleConnectionPendingVariableChangedEvent(object sender, VariableChangeEventArgs args)
    {
        if (ftRemoteAccessWidgetDataObject == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessWidgetDataObject" + notDefinedMessage);
            return;
        }

        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode" + notDefinedMessage);
        }

        SetFTRemoteAccessWidgetDataObjectValues(args.NewValue);
    }

    private void SetFTRemoteAccessWidgetDataObjectValues(bool requestPending)
    {
        bool undefinedVars = false;

        if (requestPending)
        {
            string userNameValue = usernameVariable.Value;
            if (!string.IsNullOrEmpty(userNameValue))
            {
                ftRemoteAccessWidgetDataObject.Username = userNameValue;
            }
            else
            {
                Log.Error(LOG_CATEGORY, usernameVariableName + noValueMessage);
                undefinedVars = true;
            }

            string ipAddressValue = ipAddressVariable.Value;
            if (!string.IsNullOrEmpty(ipAddressValue))
            {
                ftRemoteAccessWidgetDataObject.IpAddress = ipAddressVariable.Value;
            }
            else
            {
                Log.Error(LOG_CATEGORY, ipAddressVariableName + noValueMessage);
                undefinedVars = true;
            }

            ByteString supervisorIdValue = supervisorIdVariable.Value;
            if (!supervisorIdValue.IsEmpty)
            {
                ftRemoteAccessWidgetDataObject.SupervisorId = supervisorIdValue;
            }
            else
            {
                Log.Error(LOG_CATEGORY, supervisorIdVariableName + noValueMessage);
                undefinedVars = true;
            }
        }
        else
        {
            usernameVariable.Value = ftRemoteAccessWidgetDataObject.Username = string.Empty;
            ipAddressVariable.Value = ftRemoteAccessWidgetDataObject.IpAddress = string.Empty;
            supervisorIdVariable.Value = ftRemoteAccessWidgetDataObject.SupervisorId = ByteString.Empty;
        }

        if (requestPending && !undefinedVars)
        {
            ftRemoteAccessWidgetDataObject.IncomingConnectionRequest = true;
        }
        else
        {
            connectionPendingVariable.Value = false;
            ftRemoteAccessWidgetDataObject.IncomingConnectionRequest = false;
        }
    }

    private FTRemoteAccessWidgetDataObject ftRemoteAccessWidgetDataObject;
    private FTRemoteAccess ftRemoteAccessNode;
    private IUAVariable connectionPendingVariable;
    private IUAVariable usernameVariable;
    private IUAVariable ipAddressVariable;
    private IUAVariable supervisorIdVariable;
    private const string usernameVariableName = "FTRemoteAccessWidgetUsername";
    private const string ipAddressVariableName = "FTRemoteAccessWidgetIPAddress";
    private const string supervisorIdVariableName = "FTRemoteAccessWidgetSupervisorId";
    private const string connectionPendingVariableName = "FTRemoteAccessWidgetConnectionPending";
    private const string noValueMessage = " has no value.";
    private const string notDefinedMessage = " is not defined.";
}
