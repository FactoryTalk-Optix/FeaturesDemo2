#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.NativeUI;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.Recipe;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.UI;
using FTOptix.Core;
#endregion

public class PrepareForWebDemo : BaseNetLogic
{
    [ExportMethod]
    public void RemoveNativePresentationEngineFeatures()
    {
        // Insert code to be executed by the method
        Project.Current.Get("FeaturesDemo2/UI/NativePresentationEngine").Delete();
        Project.Current.GetVariable("FeaturesDemo2/UI/WebPresentationEngine/MaxNumberOfConnections").Value = 100;
        Project.Current.GetVariable("FeaturesDemo2/UI/WebPresentationEngine/Port").Value = 80;
    }
}
