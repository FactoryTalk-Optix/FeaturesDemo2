#region Using directives
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class ResetDefaultStyle : BaseNetLogic
{
    public override void Start()
    {
        // Method intentionally left empty.
    }

    public override void Stop()
    {
        // Set Style to "Default"

        // Get Native Presentation Engine object to set StyleSheet property
        var nodeToUI = Project.Current.Get<NativeUIPresentationEngine>("UI/NativePresentationEngine");

        // Get "Default" StyleSheet object to get its NodeID
        var nodeToDefaultStyleSheet = Project.Current.Get<StyleSheet>("UI/StyleSheets/FeatureDemo2");
        try
        {
            // Set StyleSheet property with "Default"
            nodeToUI.StyleSheet = nodeToDefaultStyleSheet.NodeId;
        }
        catch
        {
            return;
        }
    }
}
