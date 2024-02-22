#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.System;
#endregion

public class ConnectionSwitchLogic : BaseNetLogic
{
    private const string LOG_CATEGORY = nameof(ConnectionSwitchLogic);

    public override void Start()
    {
        if (LogicObject.GetAlias("FTRemoteAccessWidgetDataObject") is not FTRemoteAccessWidgetDataObject ftRemoteAccessWidgetDataObject)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessWidgetDataObject is not defined.");
            return;
        }

        ftRemoteAccessNode = ftRemoteAccessWidgetDataObject.Context.GetNode(ftRemoteAccessWidgetDataObject.FTRemoteAccessNode) as FTRemoteAccess;
        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode is not defined.");
            return;
        }

        connectionSwitch = Owner as Switch;

        connectionSwitch.Checked =
            ftRemoteAccessNode.ServerConnection == ServerConnection.Connecting ||
            ftRemoteAccessNode.ServerConnection == ServerConnection.Connected;

        // variable-change handler needed so switch state updates if connection is changed elsewhere (ex: System Manager API)
        ftRemoteAccessNode.ServerConnectionVariable.VariableChange += OnServerConnectionChanged;
    }

    public override void Stop()
    {
        if (ftRemoteAccessNode != null)
            ftRemoteAccessNode.ServerConnectionVariable.VariableChange -= OnServerConnectionChanged;

        // destruct class objects
        ftRemoteAccessNode = null;
        connectionSwitch = null;
    }

    [ExportMethod]
    public void ConnectionSwitchChanged()
    {
        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode is not defined.");
            return;
        }

        if (connectionSwitch.Checked)
            ftRemoteAccessNode.ConnectToServer();
        else
            ftRemoteAccessNode.DisconnectFromServer();
    }

    private void OnServerConnectionChanged(object sender, VariableChangeEventArgs args)
    {
        var serverConnectionValue = (ServerConnection)args.NewValue.Value;
        bool switchShouldBeOn = (serverConnectionValue == ServerConnection.Connecting || serverConnectionValue == ServerConnection.Connected);

        // only update the switch value if it's in the wrong state, otherwise the switch handler would get triggered
        if (connectionSwitch.Checked != switchShouldBeOn)
            connectionSwitch.Checked = switchShouldBeOn;
    }

    private Switch connectionSwitch;
    private FTRemoteAccess ftRemoteAccessNode;
}
