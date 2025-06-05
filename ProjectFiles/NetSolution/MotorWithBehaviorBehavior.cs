#region Using directives
using FTOptix.NetLogic;
using FTOptix.MQTTClient;
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
    public void StartMotor() => Node.Running = true;

    [ExportMethod]
    public void StopMotor() => Node.Running = false;

    #region Auto-generated code, do not edit!
    protected new MotorWithBehavior Node => (MotorWithBehavior)base.Node;
    #endregion
}
