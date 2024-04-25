#region Using directives
using System;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class LoginButtonLogic : BaseNetLogic
{
    public override void Start()
    {
        var comboBox = Owner.Owner.Get<ComboBox>("Username");
        if (Project.Current.Authentication.AuthenticationMode == AuthenticationMode.ModelOnly)
        {
            comboBox.Mode = ComboBoxMode.Normal;
        }
        else
        {
            comboBox.Mode = ComboBoxMode.Editable;
        }
    }

    public override void Stop()
    {
        // Method intentionally left empty.
    }

    [ExportMethod]
    public void PerformLogin(string username, string password)
    {
        var usersAlias = LogicObject.GetAlias("Users");
        if (usersAlias == null || usersAlias.NodeId == NodeId.Empty)
        {
            Log.Error("LoginButtonLogic", "Missing Users alias");
            return;
        }

        if (LogicObject.GetAlias("PasswordExpiredDialogType") is not DialogType passwordExpiredDialogType)
        {
            Log.Error("LoginButtonLogic", "Missing PasswordExpiredDialogType alias");
            return;
        }

        var loginButton = (Button)Owner;
        loginButton.Enabled = false;

        try
        {
            var loginResult = Session.Login(username, password);
            if (loginResult.ResultCode == ChangeUserResultCode.PasswordExpired)
            {
                loginButton.Enabled = true;
                var user = usersAlias.Get<User>(username);
                var ownerButton = (Button)Owner;
                _ = ownerButton.OpenDialog(passwordExpiredDialogType, user.NodeId);
                return;
            }
            else if (loginResult.ResultCode != ChangeUserResultCode.Success)
            {
                loginButton.Enabled = true;
                Log.Error("LoginButtonLogic", "Authentication failed");
            }

            if (loginResult.ResultCode != ChangeUserResultCode.Success)
            {
                var outputMessageLabel = Owner.Owner.GetObject("LoginFormOutputMessage");
                var outputMessageLogic = outputMessageLabel.GetObject("LoginFormOutputMessageLogic");
                outputMessageLogic.ExecuteMethod("SetOutputMessage", new object[] { (int)loginResult.ResultCode });
            }
        }
        catch (Exception e)
        {
            Log.Error("LoginButtonLogic", e.Message);
        }

        loginButton.Enabled = true;
    }
}
