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

public class GetCurrentStylesheet : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        var presentationEngine = GetPresentationEngine((IUANode)Owner);
        if (presentationEngine == null)
        {
            Log.Error("GetCurrentStylesheetLogic.GetCurrentStylesheet", "Cannot find any PresentationEngine!");
            return;
        }
        var currentStyleSheet = ((PresentationEngine)presentationEngine).GetVariable("StyleSheet");
        Log.Debug("GetCurrentStylesheetLogic.GetCurrentStylesheet", $"Current StyleSheet: {currentStyleSheet}");
        Owner.GetVariable("CurrentStylesheet").SetDynamicLink(currentStyleSheet);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private IUANode GetPresentationEngine(IUANode startingPoint)
    {
        if (startingPoint.NodeId == Project.Current.NodeId)
        {
            return null;
        }
        if (startingPoint is NativeUIPresentationEngine || startingPoint is WebUIPresentationEngine)
        {
            return startingPoint;
        }
        else
        {
            return GetPresentationEngine(startingPoint.Owner);
        }
    }
}
