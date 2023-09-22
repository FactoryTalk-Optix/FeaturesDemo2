#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using System.Linq;
using UAManagedCore;
#endregion

public class RuntimeGenerationLogic : BaseNetLogic {
    public override void Start() {
        // Insert code to be executed when the user-defined logic is started
        Panel motorsContainer = InformationModel.Make<Panel>("MotorsContainer");
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Widgets").Add(motorsContainer);
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void GenerateCoords(int topMargin, int leftMargin, out int newTop, out int newLeft) {
        int maxTop = Convert.ToInt16(Owner.Get<Rectangle>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Height);
        int maxLeft = Convert.ToInt16(Owner.Get<Rectangle>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Width);
        Random rnd = new Random();
        if (topMargin == 0) {
            newTop = rnd.Next(10, (maxTop - 40));
        } else {
            newTop = topMargin;
        }
        if (leftMargin == 0) {
            newLeft = rnd.Next(10, (maxLeft - 100));
        } else {
            newLeft = leftMargin;
        }
    }
    [ExportMethod]
    public void GenerateButton(int topMargin, int leftMargin, string textToDisplay) {
        var myControl = InformationModel.Make<Button>(NodeId.Random(1).ToString().Replace("1/", ""));
        GenerateCoords(topMargin, leftMargin, out topMargin, out leftMargin);
        myControl.TopMargin = topMargin;
        myControl.LeftMargin = leftMargin;
        myControl.Text = textToDisplay;
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Add(myControl);
    }
    [ExportMethod]
    public void GenerateLabel(int topMargin, int leftMargin, string textToDisplay) {
        var myControl = InformationModel.Make<Label>(NodeId.Random(1).ToString().Replace("1/", ""));
        GenerateCoords(topMargin, leftMargin, out topMargin, out leftMargin);
        myControl.TopMargin = topMargin;
        myControl.LeftMargin = leftMargin;
        myControl.Text = textToDisplay;
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Add(myControl);
    }
    [ExportMethod]
    public void GenerateImage(int topMargin, int leftMargin) {
        var myControl = InformationModel.Make<Image>(NodeId.Random(1).ToString().Replace("1/", ""));
        GenerateCoords(topMargin, leftMargin, out topMargin, out leftMargin);
        myControl.TopMargin = topMargin;
        myControl.LeftMargin = leftMargin;
        myControl.Path = new ResourceUri("%PROJECTDIR%\\imgs\\Logos\\LogoFTOptixDarkGrey.svg").Uri;
        myControl.Width = 75;
        myControl.Height = 40;
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Add(myControl);
    }
    [ExportMethod]
    public void GenerateInstances(int instCount) {
        RowLayout targetContainer = Owner.Get<RowLayout>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Widgets/UiWidgetsArea/ScrollView/HorizontalLayout");
        Panel motorsContainer = Owner.Get<Panel>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Widgets/MotorsContainer");
        int childNodes = targetContainer.Children.OfType<MyMotorWidget>().Count();
        Log.Debug("childNodes: " + childNodes.ToString() + " - instCount: " + instCount.ToString());
        if (instCount > childNodes) {
            for (int i = childNodes; i < instCount; i++) {
                // Create new motor to link the widget
                var newMotor = InformationModel.Make<CustomMotor>("RuntimeMotor" + (i + 1).ToString());
                motorsContainer.Add(newMotor);
                // Create widget instance
                var newWidget = InformationModel.Make<MyMotorWidget>("MyMotorWidget" + i.ToString());
                newWidget.VerticalAlignment = VerticalAlignment.Stretch;
                newWidget.HorizontalAlignment = HorizontalAlignment.Left;
                newWidget.TopMargin = 8;
                newWidget.GetVariable("MotorWidgetAlias").Value = motorsContainer.Get("RuntimeMotor" + (i + 1).ToString()).NodeId;
                targetContainer.Add(newWidget);
            }
        } else if (instCount < childNodes) {
            for (int i = instCount; i < childNodes; i++) {
                targetContainer.Get("MyMotorWidget" + i.ToString()).Delete();
                motorsContainer.Get("RuntimeMotor" + (i + 1).ToString()).Delete();
            }
        }
    }
}
