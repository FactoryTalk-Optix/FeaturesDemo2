#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using UAManagedCore;
#endregion

public class InsertValues : BaseNetLogic {
    public override void Start() {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void InsertRandomValues() {
        // Get the current project folder.
        var project = Project.Current;

        // Save the names of the columns of the table to an array
        string[] columns = { "LocalTimestamp", "Timestamp", "dataloggerVariable1", "dataloggerVariable2", "dataloggerVariable3" };

        // Create and populate a matrix with values to insert into the odbc table
        var rawValues = new object[1, 5];

        // Column TimeStamp
        rawValues[0, 0] = DateTime.Now;

        // Column TimeStamp
        rawValues[0, 1] = DateTime.UtcNow;

        // Column VariableToLog1
        rawValues[0, 2] = (int)Project.Current.GetVariable("Model/Data/Query/InsertVariable1").Value;

        // Column VariableToLog2
        rawValues[0, 3] = (int)Project.Current.GetVariable("Model/Data/Query/InsertVariable2").Value;

        // Column VariableToLog3
        rawValues[0, 4] = (int)Project.Current.GetVariable("Model/Data/Query/InsertVariable3").Value;

        var myStore = LogicObject.Owner as Store;

        // Get Table1 from myStore
        var table1 = myStore.Tables.Get<Table>("DataLogger");

        // Insert values into table1
        table1.Insert(columns, rawValues);
    }
}
