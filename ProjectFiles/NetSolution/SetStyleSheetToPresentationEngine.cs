#region Using directives
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.WebUI;
using UAManagedCore;
#endregion

public class SetStyleSheetToPresentationEngine : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        var defaultStyleSheet = Project.Current.Get<StyleSheet>("UI/StyleSheets/FeatureDemo2");
        SetStyleSheet(defaultStyleSheet.NodeId);
    }

    [ExportMethod]
    public void SetStyleSheet(NodeId newStyleSheet)
    {
        // Get the new StyleSheet
        var styleSheetNode = InformationModel.Get<StyleSheet>(newStyleSheet);
        if (styleSheetNode == null)
        {
            Log.Error("SetStyleSheetLogic.SetStyleSheet", "Cannot find new StyleSheet!");
            return;
        }
        // Get the current presentation engine
        var currentPresentationEngine = GetPresentationEngine(Owner);
        if (currentPresentationEngine == null)
        {
            Log.Error("SetStyleSheetLogic.SetStyleSheet", "Cannot find any PresentationEngine!");
            return;
        }
        ((PresentationEngine)currentPresentationEngine).StyleSheet = newStyleSheet;
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
