#region Using directives
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class AlarmGridLogic : BaseNetLogic {
    public override void Start() {
        alarmsDataGridModel = Owner.Get<DataGrid>("AlarmsDataGrid").GetVariable("Model");

        var currentSession = LogicObject.Context.Sessions.CurrentSessionInfo;
        actualLanguagesVariable = currentSession.SessionObject.Get<IUAVariable>("ActualLanguage");
        actualLanguagesVariable.VariableChange += OnSessionActualLanguagesChange;
    }

    public override void Stop() {
        actualLanguagesVariable.VariableChange -= OnSessionActualLanguagesChange;
    }

    public void OnSessionActualLanguagesChange(object sender, VariableChangeEventArgs e) {
        var dynamicLink = alarmsDataGridModel.GetVariable("DynamicLink");
        if (dynamicLink == null)
            return;

        // Restart the data bind on the data grid model variable to refresh data
        string dynamicLinkValue = dynamicLink.Value;
        dynamicLink.Value = string.Empty;
        dynamicLink.Value = dynamicLinkValue;
    }

    private IUAVariable alarmsDataGridModel;
    private IUAVariable actualLanguagesVariable;
}
