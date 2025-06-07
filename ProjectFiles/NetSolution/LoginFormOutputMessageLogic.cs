#region Using directives
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
#endregion

public class LoginFormOutputMessageLogic : BaseNetLogic
{
    public override void Start()
    {
        HideMessageLabel();
        loginResultCodeVariable = Owner.GetVariable("LoginResultCode");

        task = new DelayedTask(() =>
        {
            HideMessageLabel();
            taskStarted = false;
        }, 10000, LogicObject);
    }

    public override void Stop() => task?.Dispose();

    [ExportMethod]
    public void SetOutputMessage(int resultCode)
    {
        if (loginResultCodeVariable == null)
        {
            Log.Error("LoginFormOutputMessageLogic", "Unable to find LoginResultCode variable in LoginFormOutputMessage label");
            return;
        }

        loginResultCodeVariable.Value = resultCode;
        ShowMessageLabel();

        if (taskStarted)
        {
            task?.Cancel();
            taskStarted = false;
        }

        task.Start();
        taskStarted = true;
    }

    private void ShowMessageLabel()
    {
        var messageLabel = (Label)Owner;
        messageLabel.Visible = true;
    }

    private void HideMessageLabel()
    {
        var messageLabel = (Label)Owner;
        messageLabel.Visible = false;
    }

    private DelayedTask task;
    private bool taskStarted = false;
    private IUAVariable loginResultCodeVariable;
}
