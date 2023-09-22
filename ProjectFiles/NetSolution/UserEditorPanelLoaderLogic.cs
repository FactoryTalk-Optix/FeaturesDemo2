#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.WebUI;
#endregion

public class UserEditorPanelLoaderLogic : BaseNetLogic
{
	[ExportMethod]
	public void GoToUserDetailsPanel(NodeId user)
	{
		if (user == null)
			return;

		var userCountVariable = LogicObject.GetVariable("UserCount");
		if (userCountVariable == null)
			return;

		var noUsersPanelVariable = LogicObject.GetVariable("NoUsersPanel");
		if (noUsersPanelVariable == null)
			return;

		var userDetailPanelVariable = LogicObject.GetVariable("UserDetailPanel");
		if (userDetailPanelVariable == null)
			return;

        var panelLoader = (PanelLoader)Owner;

		NodeId newPanelNode = userCountVariable.Value > 0 ? userDetailPanelVariable.Value : noUsersPanelVariable.Value;
		Owner.Owner.Get<ListBox>("UsersList").SelectedItem = user;

		panelLoader.ChangePanel(newPanelNode, user);
	}
}
