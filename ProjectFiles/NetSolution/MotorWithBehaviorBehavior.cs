#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.UI;
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

[CustomBehavior]
public class MotorWithBehaviorBehavior : BaseNetBehavior
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
    }
    [ExportMethod]
    public void StartMotor() {
        Node.Running = true;
    }

    [ExportMethod]
    public void StopMotor() {
        Node.Running = false;
    }

    #region Auto-generated code, do not edit!
    protected new MotorWithBehavior Node => (MotorWithBehavior)base.Node;
#endregion
}
