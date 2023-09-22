#region Using directives
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class ResetDefaultStyle : BaseNetLogic {
    public override void Start()
    {
    }

    public override void Stop() {
        // Set Style to "Default"

        // Get Native Presentation Engine object to set StyleSheet property
        NativeUIPresentationEngine nodeToUI = Project.Current.Get<NativeUIPresentationEngine>("UI/NativePresentationEngine");

        // Get "Default" StyleSheet object to get its NodeID
        StyleSheet nodeToDefaultStyleSheet = Project.Current.Get<StyleSheet>("UI/StyleSheets/FeatureDemo2");
        try {
            // Set StyleSheet property with "Default"
            nodeToUI.StyleSheet = nodeToDefaultStyleSheet.NodeId;
        } catch {
            return;
        }
    }
}
