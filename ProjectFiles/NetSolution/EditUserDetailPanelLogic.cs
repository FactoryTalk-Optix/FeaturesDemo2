#region Using directives
using System;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.WebUI;
using System.Linq;
#endregion

public class EditUserDetailPanelLogic : BaseNetLogic
{
	[ExportMethod]
	public void SaveUser(string username, string password, string locale, out NodeId result)
	{
        result = NodeId.Empty;

		if (string.IsNullOrEmpty(username))
		{
			ShowMessage(1);
			Log.Error("EditUserDetailPanelLogic", "Cannot create user with empty username");
			return;
		}

        result = ApplyUser(username, password, locale);
	}

	private NodeId ApplyUser(string username, string password, string locale)
	{
		var users = GetUsers();
		if (users == null)
		{
			ShowMessage(2);
			Log.Error("EditUserDetailPanelLogic", "Unable to get users");
			return NodeId.Empty;
		}

		var user = users.Get<FTOptix.Core.User>(username);
		if (user == null)
		{
			ShowMessage(3);
			Log.Error("EditUserDetailPanelLogic", "User not found");
			return NodeId.Empty;
		}

		//Apply LocaleId
		if(!string.IsNullOrEmpty(locale))
			user.LocaleId = locale;

		//Apply groups
		ApplyGroups(user);

		//Apply password
		if (!string.IsNullOrEmpty(password))
		{
			var result = Session.ChangePasswordInternal(username, password);

			switch (result.ResultCode)
			{
				case FTOptix.Core.ChangePasswordResultCode.Success:
					var editPasswordTextboxPtr = LogicObject.GetVariable("PasswordTextbox");
					if (editPasswordTextboxPtr == null)
						Log.Error("EditUserDetailPanelLogic", "PasswordTextbox variable not found");

					var nodeId = (NodeId)editPasswordTextboxPtr.Value;
					if (nodeId == null)
						Log.Error("EditUserDetailPanelLogic", "PasswordTextbox not set");

					var editPasswordTextbox = (TextBox)InformationModel.Get(nodeId);
					if (editPasswordTextbox == null)
						Log.Error("EditUserDetailPanelLogic", "EditPasswordTextbox not found");

					editPasswordTextbox.Text = string.Empty;
					break;
				case FTOptix.Core.ChangePasswordResultCode.WrongOldPassword:
					//Not applicable
					break;
				case FTOptix.Core.ChangePasswordResultCode.PasswordAlreadyUsed:
					ShowMessage(4);
					return NodeId.Empty;
				case FTOptix.Core.ChangePasswordResultCode.PasswordChangedTooRecently:
					ShowMessage(5);
					return NodeId.Empty;
				case FTOptix.Core.ChangePasswordResultCode.PasswordTooShort:
					ShowMessage(6);
					return NodeId.Empty;
				case FTOptix.Core.ChangePasswordResultCode.UserNotFound:
					ShowMessage(7);
					return NodeId.Empty;
				case FTOptix.Core.ChangePasswordResultCode.UnsupportedOperation:
					ShowMessage(8);
					return NodeId.Empty;

			}
		}


		ShowMessage(9);
		return user.NodeId;
	}

	private void ApplyGroups(FTOptix.Core.User user)
	{
		Panel groupsPanel = Owner.Get<Panel>("HorizontalLayout1/GroupsPanel1");
		IUAVariable editable = groupsPanel.GetVariable("Editable");
		IUANode groups = groupsPanel.GetAlias("Groups");
		var panel = groupsPanel.Children.Get("ScrollView").Get("Container");

        if (editable.Value == false)
            return;

		if (user == null || groups == null || panel == null)
			return;

		var userNode = InformationModel.Get(user.NodeId);
        if (userNode == null)
            return;

		var groupCheckBoxes = panel.Refs.GetObjects(OpcUa.ReferenceTypes.HasOrderedComponent, false);

		foreach (var groupCheckBoxNode in groupCheckBoxes)
		{
			var group = groups.Get(groupCheckBoxNode.BrowseName);
			if (group == null)
				return;

			bool userHasGroup = UserHasGroup(user, group.NodeId);

			if (groupCheckBoxNode.GetVariable("Checked").Value && !userHasGroup)
				userNode.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, group);
			else if (!groupCheckBoxNode.GetVariable("Checked").Value && userHasGroup)
				userNode.Refs.RemoveReference(FTOptix.Core.ReferenceTypes.HasGroup, group.NodeId, false);
		}
	}

	private bool UserHasGroup(IUANode user, NodeId groupNodeId)
	{
		if (user == null)
			return false;
		var userGroups = user.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasGroup, false);
		foreach (var userGroup in userGroups)
		{
			if (userGroup.NodeId == groupNodeId)
				return true;
		}
		return false;
	}

	private IUANode GetUsers()
	{
		var pathResolverResult = LogicObject.Context.ResolvePath(LogicObject, "{Users}");
		if (pathResolverResult == null)
			return null;
		if (pathResolverResult.ResolvedNode == null)
			return null;

		return pathResolverResult.ResolvedNode;
	}

	private void ShowMessage(int message)
	{
		var errorMessageVariable = LogicObject.GetVariable("ErrorMessage");
		if (errorMessageVariable != null)
			errorMessageVariable.Value = message;

		if (delayedTask != null)
			delayedTask?.Dispose();
		
		delayedTask = new DelayedTask(DelayedAction, 5000, LogicObject);
		delayedTask.Start();
	}

	private void DelayedAction(DelayedTask task)
	{
		if (task.IsCancellationRequested)
			return;

		var errorMessageVariable = LogicObject.GetVariable("ErrorMessage");
		if (errorMessageVariable != null)
        {
			errorMessageVariable.Value = 0;
		}
		delayedTask?.Dispose();
	}

	private DelayedTask delayedTask;
}
