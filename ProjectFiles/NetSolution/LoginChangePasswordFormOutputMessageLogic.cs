#region Using directives
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class LoginChangePasswordFormOutputMessageLogic : BaseNetLogic
{
    public override void Start()
    {
        HideMessageLabel();
        changePasswordResultCodeVariable = Owner.GetVariable("ChangePasswordResultCode");

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
        if (changePasswordResultCodeVariable == null)
        {
            Log.Error("ChangePasswordFormOutputMessageLogic", "Unable to find ChangePasswordResultCode variable in ChangePasswordFormOutputMessage label");
            return;
        }

        changePasswordResultCodeVariable.Value = resultCode;
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
    private IUAVariable changePasswordResultCodeVariable;
}
