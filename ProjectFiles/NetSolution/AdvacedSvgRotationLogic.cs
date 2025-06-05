#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Core;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using FTOptix.MQTTClient;
#endregion

public class AdvacedSvgRotationLogic : BaseNetLogic
{
    public override void Start()
    {
        // LogicObject.Owner is the button, so LogicObject.Owner is the MainWindow
        svgImage = Owner.Get<AdvancedSVGImage>("Content/AdvancedSVGImage");

        // Retrieve the Path to the SVG
        var imageAbsolutePath = svgImage.Path.Uri;

        // Load the SVG into an XDocument
        xDocument = XDocument.Load(imageAbsolutePath);

        // Find the first path element
        var pathNode = xDocument.Descendants().FirstOrDefault(x => x.Name.LocalName == "path");

        // Find the first polygon element
        var polyNode = xDocument.Descendants().FirstOrDefault(x => x.Name.LocalName == "polygon");

        if (polyNode == null || pathNode == null)
            throw new ArgumentException("SVG does not contain a polygon or path element.");

        // Get the polygon transform attribute
        polyTransformAttribute = polyNode.Attribute("transform");

        // Get the path transform attribute
        pathTransformAttribute = pathNode.Attribute("transform");

        // Get the polygon color attribute
        polyColorAttribute = polyNode.Attribute("style");

        // Get the path color attribute
        pathColorAttribute = pathNode.Attribute("style");
    }

    public override void Stop()
    {
        keepGoing = false;
        myLRT?.Dispose();
    }

    private void DoRotate()
    {
        int degrees = 3;

        while (keepGoing)
        {
            if (myLRT.IsCancellationRequested)
                return;

            Thread.Sleep(50);

            degrees += 3;
            if (degrees > 360)
                degrees = 0;
            string newTransformPoly = new string("rotate(" + degrees + ", 50, 250)");
            polyTransformAttribute.SetValue(newTransformPoly);

            string newTransformPath = new string("rotate(" + degrees + ", 96, 97)");
            pathTransformAttribute.SetValue(newTransformPath);

            //Update the SVG
            svgImage.SetImageContent(xDocument.ToString());
        }
    }

    [ExportMethod]
    public void RotateSvg()
    {
        if (myLRT == null)
        {
            keepGoing = true;
            myLRT = new LongRunningTask(DoRotate, LogicObject);
            myLRT.Start();
        }
        else
        {
            keepGoing = false;
            myLRT.Dispose();
            myLRT = null;
        }
    }

    [ExportMethod]
    public void ChangeColor()
    {
        var random = new Random();
        string newColor = $"fill:rgb({random.Next(0, 255)},{random.Next(0, 255)},{random.Next(0, 255)});";
        polyColorAttribute.SetValue(newColor);
        Thread.Sleep(1);
        newColor = $"fill:rgb({random.Next(0, 255)},{random.Next(0, 255)},{random.Next(0, 255)});";
        pathColorAttribute.SetValue(newColor);

        //Update the SVG
        svgImage.SetImageContent(xDocument.ToString());
    }

    private XDocument xDocument;
    private AdvancedSVGImage svgImage;
    private XAttribute polyTransformAttribute;
    private XAttribute pathTransformAttribute;
    private XAttribute polyColorAttribute;
    private XAttribute pathColorAttribute;
    private LongRunningTask myLRT;
    private bool keepGoing = true;
}
