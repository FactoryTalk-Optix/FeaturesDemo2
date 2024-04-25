#region Using directives
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class LoginChangePasswordButtonLogic : BaseNetLogic
{
    [ExportMethod]
    public void PerformChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        var outputMessageLabel = Owner.Owner.GetObject("ChangePasswordFormOutputMessage");
        var outputMessageLogic = outputMessageLabel.GetObject("LoginChangePasswordFormOutputMessageLogic");

        if (newPassword != confirmPassword)
        {
            outputMessageLogic.ExecuteMethod("SetOutputMessage", new object[] { 7 });
        }
        else
        {
            string username = Session.User.BrowseName;
            try
            {
                var userWithExpiredPassword = Owner.GetAlias("UserWithExpiredPassword");
                if (userWithExpiredPassword != null)
                    username = userWithExpiredPassword.BrowseName;
            }
            catch
            {
            }

            var result = Session.ChangePassword(username, newPassword, oldPassword);
            if (result.ResultCode == ChangePasswordResultCode.Success)
            {
                if (Owner.Owner?.Owner?.Owner is Dialog parentDialog && result.Success)
                    parentDialog.Close();
            }
            else
            {
                outputMessageLogic.ExecuteMethod("SetOutputMessage", new object[] { (int)result.ResultCode });
            }
        }
    }
}
