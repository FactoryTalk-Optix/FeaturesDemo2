#region Using directives
using FTOptix.NetLogic;
using FTOptix.System;
using UAManagedCore;
#endregion

public class IncomingConnectionApprovalLogic : BaseNetLogic
{
    private const string LOG_CATEGORY = nameof(IncomingConnectionApprovalLogic);

    public override void Start()
    {
        ftRemoteAccessWidgetDataObject = LogicObject.GetAlias("FTRemoteAccessWidgetDataObject") as FTRemoteAccessWidgetDataObject;
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
            return;
        }
    }

    public override void Stop()
    {
        // destruct class objects
        ftRemoteAccessNode = null;
        ftRemoteAccessWidgetDataObject = null;
        connectionPendingVariable = null;
    }

    [ExportMethod]
    public void AcceptIncomingConnection()
    {
        if (ftRemoteAccessWidgetDataObject == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessWidgetDataObject" + notDefinedMessage);
            return;
        }

        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode" + notDefinedMessage);
            return;
        }

        if (connectionPendingVariable == null)
        {
            Log.Error(LOG_CATEGORY, connectionPendingVariableName + notDefinedMessage);
            return;
        }

        var result = ftRemoteAccessNode.ApproveIncomingRemoteConnection(ftRemoteAccessWidgetDataObject.SupervisorId);
        if (result == IncomingRemoteConnectionApprovalResult.Success)
        {
            connectionPendingVariable.Value = false;
        }
        else if (result == IncomingRemoteConnectionApprovalResult.NoConnectionToApprove)
        {
            Log.Info(LOG_CATEGORY, "Connection request has already been handled.");
            connectionPendingVariable.Value = false;
        }
        else
        {
            Log.Error(LOG_CATEGORY, "Failed to accept connection request. FTRemoteAccess runtime is not connected.");
        }
    }

    [ExportMethod]
    public void DenyIncomingConnection()
    {
        if (ftRemoteAccessWidgetDataObject == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessWidgetDataObject" + notDefinedMessage);
            return;
        }

        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode" + notDefinedMessage);
            return;
        }

        if (connectionPendingVariable == null)
        {
            Log.Error(LOG_CATEGORY, connectionPendingVariableName + notDefinedMessage);
            return;
        }

        var result = ftRemoteAccessNode.DenyIncomingRemoteConnection(ftRemoteAccessWidgetDataObject.SupervisorId);
        if (result == IncomingRemoteConnectionApprovalResult.Success)
        {
            connectionPendingVariable.Value = false;
        }
        else if (result == IncomingRemoteConnectionApprovalResult.NoConnectionToApprove)
        {
            Log.Info(LOG_CATEGORY, "Connection request has already been handled.");
            connectionPendingVariable.Value = false;
        }
        else
        {
            Log.Error(LOG_CATEGORY, "Failed to deny connection request. FTRemoteAccess runtime is not connected.");
        }
    }

    private FTRemoteAccessWidgetDataObject ftRemoteAccessWidgetDataObject;
    private FTRemoteAccess ftRemoteAccessNode;
    private IUAVariable connectionPendingVariable;
    private const string connectionPendingVariableName = "FTRemoteAccessWidgetConnectionPending";
    private const string notDefinedMessage = " is not defined.";
}
