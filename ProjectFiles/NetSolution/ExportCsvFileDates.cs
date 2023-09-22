#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
#endregion

public class ExportCsvFileDates : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        Project.Current.GetVariable("NetLogic/DataLoggerExporter/From").Value = DateTime.Now.AddDays(-1);
        Project.Current.GetVariable("NetLogic/DataLoggerExporter/To").Value = DateTime.Now;
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
