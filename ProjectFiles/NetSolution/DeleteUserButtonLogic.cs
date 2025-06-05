#region Using directives
using System.Linq;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using FTOptix.MQTTClient;
#endregion

public class DeleteUserButtonLogic : BaseNetLogic
{
    [ExportMethod]
    public void DeleteUser(NodeId userToDelete)
    {
        var userObjectToRemove = InformationModel.Get(userToDelete);
        if (userObjectToRemove == null)
        {
            Log.Error("UserEditor", "Cannot obtain the selected user.");
            return;
        }

        var userVariable = Owner.Owner.Owner.Owner.GetVariable("Users");
        if (userVariable == null)
        {
            Log.Error("UserEditor", "Missing user variable in UserEditor Panel.");
            return;
        }

        if (userVariable.Value == null || (NodeId)userVariable.Value == NodeId.Empty)
        {
            Log.Error("UserEditor", "Fill User variable in UserEditor.");
            return;
        }
        var usersFolder = InformationModel.Get(userVariable.Value);
        if (usersFolder == null)
        {
            Log.Error("UserEditor", "Cannot obtain Users folder.");
            return;
        }

        usersFolder.Remove(userObjectToRemove);

        if (usersFolder.Children.Count > 0)
        {
            var usersList = Owner.Owner.Owner.Get<ListBox>("HorizontalLayout1/UsersList");
            usersList.SelectedItem = usersFolder.Children.First().NodeId;
        }
    }
}
