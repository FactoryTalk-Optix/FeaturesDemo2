#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.System;
#endregion

public class RequestAssistanceSwitchLogic : BaseNetLogic
{
    private const string LOG_CATEGORY = nameof(RequestAssistanceSwitchLogic);

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

        assistanceSwitch = Owner as Switch;
        assistanceSwitch.Checked = ftRemoteAccessNode.AssistanceRequested;

        // value update handler needed to cover external changes (ex: request closed via FTRA API)
        ftRemoteAccessNode.AssistanceRequestedVariable.VariableChange += UpdateSwitchOnAssistanceChange;
    }

    public override void Stop()
    {
        if (ftRemoteAccessNode != null)
            ftRemoteAccessNode.AssistanceRequestedVariable.VariableChange -= UpdateSwitchOnAssistanceChange;

        // destruct class objects
        ftRemoteAccessNode = null;
        assistanceSwitch = null;
    }

    [ExportMethod]
    public void HandleRequestAssistanceSwitch(string name, string contactInfo, string description)
    {
        if (ftRemoteAccessNode == null)
        {
            Log.Error(LOG_CATEGORY, "FTRemoteAccessNode is not defined.");
            return;
        }

        if (assistanceSwitch.Checked)
        {
            if (ftRemoteAccessNode.AssistanceRequestMode == AssistanceRequestMode.Enabled)
                ftRemoteAccessNode.OpenAssistanceRequest(name, contactInfo, description);
            else if (ftRemoteAccessNode.AssistanceRequestMode == AssistanceRequestMode.ContactDetailsRequired)
            {
                if (!string.IsNullOrEmpty(name) &&
                    !string.IsNullOrEmpty(contactInfo) &&
                    !string.IsNullOrEmpty(description))
                {
                    var result = ftRemoteAccessNode.OpenAssistanceRequest(name, contactInfo, description);
                    if (result == OpenAssistanceRequestResult.CannotBeOpened)
                        Log.Warning(LOG_CATEGORY, "Assistance request could not be opened.");
                    else if (result == OpenAssistanceRequestResult.RuntimeNotConnected)
                        Log.Error(LOG_CATEGORY, "Failed to open assistance request. FTRemoteAccess runtime is not connected.");
                }
                else
                    Log.Warning(LOG_CATEGORY, "Assistance requests cannot be submitted with empty information.");
            }
            else
                Log.Warning(LOG_CATEGORY, "Assistance requests are disabled.");
        }
        else
        {
            // switch has been unchecked, request has been cancelled
            ftRemoteAccessNode.CloseAssistanceRequest();
        }
    }

    private void UpdateSwitchOnAssistanceChange(object sender, VariableChangeEventArgs args)
    {
        bool assistanceRequested = (bool)args.NewValue.Value;

        // only update the switch value if it is different, otherwise the switch handler would get triggered
        if (assistanceSwitch.Checked != assistanceRequested)
            assistanceSwitch.Checked = assistanceRequested;
    }

    private FTRemoteAccess ftRemoteAccessNode;
    private Switch assistanceSwitch;
}
