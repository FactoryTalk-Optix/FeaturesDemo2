#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
#endregion

public class ApplicationNameLogic : BaseNetLogic
{
    public override void Start()
    {
        var label = Owner as Label;
        label.Text = Project.Current.BrowseName;
    }

    public override void Stop()
    {
        // Method intentionally left empty.
    }
}
