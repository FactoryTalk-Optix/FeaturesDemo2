#region Using directives
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class RuntimeAlarmsLogic : BaseNetLogic {
    public override void Start() {
        // Insert code to be executed when the user-defined logic is started
        DetectAlarmType();
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void DetectAlarmType() {
        // Check which alarm type we need to create and display the related info on a Label
        var targetLabel = Owner.Get<Label>("AlarmType");
        var alarmTag = InformationModel.GetVariable(Owner.Get<ComboBox>("SourceVar").SelectedItem);
        if (alarmTag.DataType == OpcUa.DataTypes.Boolean) {
            targetLabel.LocalizedText = new LocalizedText(targetLabel.NodeId.NamespaceIndex, "DigitalAlarm");
        } else {
            targetLabel.LocalizedText = new LocalizedText(targetLabel.NodeId.NamespaceIndex, "AnalogAlarm");
        }
    }
    [ExportMethod]
    public void CreateAlarm() {
        // Get the alarms container
        var alarmsFolder = Project.Current.Get<Folder>("Alarms/RuntimeAlarms");
        var alarmTag = InformationModel.GetVariable(Owner.Get<ComboBox>("SourceVar").SelectedItem);
        var existingAlarm = alarmsFolder.Get(alarmTag.BrowseName) ?? null;
        // Check if alarm already exist (skip) or create new based on DataType
        if (existingAlarm == null) {
            if (alarmTag.DataType == OpcUa.DataTypes.Boolean) {
                var newAlarm = InformationModel.MakeObject<DigitalAlarm>(alarmTag.BrowseName);
                newAlarm.InputValueVariable.SetDynamicLink(alarmTag);
                newAlarm.Message = Owner.Get<TextBox>("AlarmMessage").Text;
                newAlarm.AutoAcknowledge = true;
                newAlarm.AutoConfirm = true;
                alarmsFolder.Add(newAlarm);
            } else {
                var newAlarm = InformationModel.MakeObject<ExclusiveLevelAlarmController>(alarmTag.BrowseName);
                newAlarm.InputValueVariable.SetDynamicLink(alarmTag);
                newAlarm.Message = Owner.Get<TextBox>("AlarmMessage").Text;
                newAlarm.HighLimit = 50;
                newAlarm.AutoAcknowledge = true;
                newAlarm.AutoConfirm = true;
                alarmsFolder.Add(newAlarm);
            }
        }
    }
    [ExportMethod]
    public void DeleteAlarm() {
        // Delete alarm if exist
        var alarmsFolder = Project.Current.Get<Folder>("Alarms/RuntimeAlarms");
        var alarmTag = InformationModel.GetVariable(Owner.Get<ComboBox>("SourceVar").SelectedItem);
        var existingAlarm = alarmsFolder.Get(alarmTag.BrowseName) ?? null;
        if (existingAlarm != null) {
            existingAlarm.Delete();
        }
    }
}
