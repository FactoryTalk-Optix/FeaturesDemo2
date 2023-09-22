#region Using directives
using FTOptix.NetLogic;
using System;
using UAManagedCore;
#endregion

public class FilteredAlarmHistoryGridLogic : BaseNetLogic {
    public override void Start() {
        // After checking validity, we set a default time interval of 24 hours
        var toVariable = Owner.GetVariable("To");
        if (toVariable == null) {
            Log.Error("FilteredAlarmHistoryGridLogic", "Missing To variable");
            return;
        }

        if (toVariable.Value == null) {
            Log.Error("FilteredAlarmHistoryGridLogic", "Missing To variable value");
            return;
        }

        toVariable.Value = DateTime.Now;
        var fromVariable = Owner.GetVariable("From");
        if (fromVariable == null) {
            Log.Error("FilteredAlarmHistoryGridLogic", "Missing From variable");
            return;
        }

        if (fromVariable.Value == null) {
            Log.Error("FilteredAlarmHistoryGridLogic", "Missing From variable value");
            return;
        }

        fromVariable.Value = DateTime.Now.AddHours(-24);
    }
}
