#region Using directives
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
#endregion

public class ResetDefaultStyle : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        styleSheetVariable = InformationModel.GetVariable(LogicObject.GetVariable("CurrentStylesheet").Value);
        startingStylesheet = styleSheetVariable.Value;
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        styleSheetVariable.Value = startingStylesheet;
    }

    NodeId startingStylesheet;
    IUAVariable styleSheetVariable;
}
