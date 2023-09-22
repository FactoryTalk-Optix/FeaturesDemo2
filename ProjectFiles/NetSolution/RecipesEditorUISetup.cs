#region Using directives
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Recipe;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class RecipesEditorUISetup : BaseNetLogic {
    [ExportMethod]
    public void Setup() {
        try {
            schema = GetRecipeSchema();

            var schemaEntries = GetSchemaEntries();

            var controlsContainer = GetControlsContainer();
            CleanUI(controlsContainer);

            ConfigureComboBox();

            target = GetTargetNode();

            BuildUIFromSchemaRecursive(schemaEntries, controlsContainer, new List<string>());
        } catch (Exception e) {
            Log.Error("RecipesEditor", e.Message);
        }
    }

    private RecipeSchema GetRecipeSchema() {
        var recipeSchemaPtr = Owner.GetVariable("RecipeSchema");
        if (recipeSchemaPtr == null)
            throw new Exception("RecipeSchema variable not found");

        var nodeId = (NodeId)recipeSchemaPtr.Value;
        if (nodeId == null)
            throw new Exception("RecipeSchema not set");

        var recipeSchema = InformationModel.Get(nodeId);
        if (recipeSchema == null)
            throw new Exception("Recipe not found");

        // Check if it has correct type
        var schema = recipeSchema as RecipeSchema;
        if (schema == null)
            throw new Exception(recipeSchema.BrowseName + " is not a recipe");

        return schema;
    }

    private ChildNodeCollection GetSchemaEntries() {
        var rootNode = schema.Get("Root");
        if (rootNode == null)
            throw new Exception("Root node not found in recipe schema " + schema.BrowseName);

        var schemaEntries = rootNode.Children;
        if (schemaEntries.Count == 0)
            throw new Exception("Recipe schema " + schema.BrowseName + " has no entries");

        return schemaEntries;
    }

    private ColumnLayout GetControlsContainer() {
        var scrollView = Owner.Get("ScrollView");
        if (scrollView == null)
            throw new Exception("ScrollView not found");

        var controlsContainer = scrollView.Get<ColumnLayout>("ColumnLayout");
        if (controlsContainer == null)
            throw new Exception("ColumnLayout not found");

        return controlsContainer;
    }

    private void CleanUI(ColumnLayout controlsContainer) {
        controlsContainer.Children.Clear();
        controlsContainer.Height = 0;
        controlsContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private void ConfigureComboBox() {
        // Set store as model for ComboBox
        var recipesComboBox = Owner.Get<ComboBox>("RecipesComboBox");
        if (recipesComboBox == null)
            throw new Exception("Recipes ComboBox not found");

        if (schema.Store == null)
            throw new Exception("Store of schema " + schema.BrowseName + " is not set");

        recipesComboBox.Model = schema.Store;

        // Set query of combobox with correct table name
        var tableName = !String.IsNullOrEmpty(schema.TableName) ? schema.TableName : schema.BrowseName;
        recipesComboBox.Query = "SELECT Name FROM \"" + tableName + "\"";
    }

    private IUANode GetTargetNode() {
        var targetNode = schema.GetVariable("TargetNode");
        if (targetNode == null)
            throw new Exception("Target Node variable not found in schema " + schema.BrowseName);

        if ((NodeId)targetNode.Value == NodeId.Empty)
            throw new Exception("Target Node variable not set in schema " + schema.BrowseName);

        target = InformationModel.Get(targetNode.Value);
        if (target == null)
            throw new Exception("Target " + targetNode.Value + " not found");

        return target;
    }

    private void BuildUIFromSchemaRecursive(IEnumerable<IUANode> entries, Item controlsContainer, List<string> browsePath) {
        foreach (var entry in entries) {
            List<string> currentBrowsePath = browsePath.ToList();
            currentBrowsePath.Add(entry.BrowseName);

            if (entry.NodeClass == NodeClass.Variable) {
                var variable = (IUAVariable)entry;
                var controls = BuildControl(variable, currentBrowsePath);
                foreach (var control in controls) {
                    controlsContainer.Height += control.Height;
                    controlsContainer.Add(control);
                }
            }

            if (entry.Children.Count > 0)
                BuildUIFromSchemaRecursive(entry.Children, controlsContainer, currentBrowsePath);
        }
    }

    private List<Item> BuildControl(IUAVariable variable, List<string> browsePath) {
        var result = new List<Item>();

        var dataType = variable.Context.GetDataType(variable.DataType);
        var arrayDimensions = variable.ArrayDimensions;

        if (arrayDimensions.Length == 0) {
            if (dataType.IsSubTypeOf(OpcUa.DataTypes.Integer))
                result.Add(BuildSpinbox(variable, browsePath));
            else if (dataType.IsSubTypeOf(OpcUa.DataTypes.Boolean))
                result.Add(BuildSwitch(variable, browsePath));
            else if (dataType.IsSubTypeOf(OpcUa.DataTypes.Duration))
                result.Add(BuildDurationPicker(variable, browsePath));
            else
                result.Add(BuildTextBox(variable, browsePath));
        } else if (arrayDimensions.Length == 1) {
            if (dataType.IsSubTypeOf(OpcUa.DataTypes.Integer)) {
                foreach (var item in BuildSpinBoxArray(variable, browsePath))
                    result.Add(item);
            } else if (dataType.IsSubTypeOf(OpcUa.DataTypes.Boolean)) {
                foreach (var item in BuildSwitchArray(variable, browsePath))
                    result.Add(item);
            } else if (dataType.IsSubTypeOf(OpcUa.DataTypes.Duration)) {
                foreach (var item in BuildDurationPickerArray(variable, browsePath))
                    result.Add(item);
            } else {
                foreach (var item in BuildTextBoxArray(variable, browsePath))
                    result.Add(item);
            }
        } else
            Log.Error("RecipesEditor", "Unsupported multi-dimensional array parameter " + Log.Node(variable));

        return result;
    }

    private Item BuildControlPanel(IUAVariable variable, List<string> browsePath, uint[] indexes = null) {
        var panel = InformationModel.MakeObject<Panel>(variable.BrowseName);
        panel.Height = 40;
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var label = InformationModel.MakeObject<Label>("Path");
        label.Text = BrowsePathToNodePath(browsePath);
        if (indexes != null)
            label.Text += "_" + indexes[0];

        label.LeftMargin = 20;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.Add(label);

        var node = target;
        foreach (var nodeBrowseName in browsePath) {
            if (node == null) {
                Log.Error("RecipesEditor", "Node " + BrowsePathToNodePath(browsePath) + " not found in target " + target.BrowseName);
                continue;
            }

            node = node.Get(nodeBrowseName);
        }

        var variableTarget = (IUAVariable)node;

        var label2 = InformationModel.MakeObject<Label>("CurrentValue");
        if (indexes == null)
            label2.TextVariable.SetDynamicLink(variableTarget);
        else
            label2.TextVariable.SetDynamicLink(variableTarget, indexes[0]);

        label2.VerticalAlignment = VerticalAlignment.Center;
        label2.HorizontalAlignment = HorizontalAlignment.Right;
        panel.Add(label2);

        return panel;
    }

    private Item BuildDurationPicker(IUAVariable variable, List<string> browsePath) {
        var panel = BuildControlPanel(variable, browsePath);

        var durationPicker = InformationModel.MakeObject<DurationPicker>("DurationPicker");
        durationPicker.VerticalAlignment = VerticalAlignment.Center;
        durationPicker.HorizontalAlignment = HorizontalAlignment.Right;
        durationPicker.RightMargin = 100;
        durationPicker.Width = 100;

        string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
        MakeDynamicLink(durationPicker.GetVariable("Value"), aliasRelativeNodePath);
        panel.Add(durationPicker);

        return panel;
    }

    private List<Item> BuildDurationPickerArray(IUAVariable variable, List<string> browsePath) {
        var result = new List<Item>();

        var arrayDimensions = variable.ArrayDimensions;
        for (uint index = 0; index < arrayDimensions[0]; ++index) {
            var panel = BuildControlPanel(variable, browsePath, new uint[] { index });

            var durationPicker = InformationModel.MakeObject<DurationPicker>("DurationPicker");
            durationPicker.VerticalAlignment = VerticalAlignment.Center;
            durationPicker.HorizontalAlignment = HorizontalAlignment.Right;
            durationPicker.RightMargin = 100;
            durationPicker.Width = 100;

            string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
            MakeDynamicLink(durationPicker.GetVariable("Value"), aliasRelativeNodePath, index);
            panel.Add(durationPicker);

            result.Add(panel);
        }

        return result;
    }

    private Item BuildSpinbox(IUAVariable variable, List<string> browsePath) {
        var panel = BuildControlPanel(variable, browsePath);

        var spinbox = InformationModel.MakeObject<SpinBox>("SpinBox");
        spinbox.VerticalAlignment = VerticalAlignment.Center;
        spinbox.HorizontalAlignment = HorizontalAlignment.Right;
        spinbox.RightMargin = 100;
        spinbox.Width = 100;

        string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
        MakeDynamicLink(spinbox.GetVariable("Value"), aliasRelativeNodePath);
        panel.Add(spinbox);

        return panel;
    }

    private List<Item> BuildSpinBoxArray(IUAVariable variable, List<string> browsePath) {
        var result = new List<Item>();

        var arrayDimensions = variable.ArrayDimensions;
        for (uint index = 0; index < arrayDimensions[0]; ++index) {
            var panel = BuildControlPanel(variable, browsePath, new uint[] { index });

            var spinbox = InformationModel.MakeObject<SpinBox>("SpinBox");
            spinbox.VerticalAlignment = VerticalAlignment.Center;
            spinbox.HorizontalAlignment = HorizontalAlignment.Right;
            spinbox.RightMargin = 100;
            spinbox.Width = 100;

            string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
            MakeDynamicLink(spinbox.GetVariable("Value"), aliasRelativeNodePath, index);
            panel.Add(spinbox);

            result.Add(panel);
        }

        return result;
    }

    private Item BuildTextBox(IUAVariable variable, List<string> browsePath) {
        var panel = BuildControlPanel(variable, browsePath);

        var textbox = InformationModel.MakeObject<TextBox>("Textbox");
        textbox.VerticalAlignment = VerticalAlignment.Center;
        textbox.HorizontalAlignment = HorizontalAlignment.Right;
        textbox.RightMargin = 100;
        textbox.Width = 100;

        string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
        MakeDynamicLink(textbox.GetVariable("Text"), aliasRelativeNodePath);
        panel.Add(textbox);

        return panel;
    }

    private List<Item> BuildTextBoxArray(IUAVariable variable, List<string> browsePath) {
        var result = new List<Item>();

        var arrayDimensions = variable.ArrayDimensions;
        for (uint index = 0; index < arrayDimensions[0]; ++index) {
            var panel = BuildControlPanel(variable, browsePath, new uint[] { index });

            var textbox = InformationModel.MakeObject<TextBox>("Textbox");
            textbox.VerticalAlignment = VerticalAlignment.Center;
            textbox.HorizontalAlignment = HorizontalAlignment.Right;
            textbox.RightMargin = 100;
            textbox.Width = 100;

            string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
            MakeDynamicLink(textbox.GetVariable("Text"), aliasRelativeNodePath, index);
            panel.Add(textbox);

            result.Add(panel);
        }

        return result;
    }

    private Item BuildSwitch(IUAVariable variable, List<string> browsePath) {
        var panel = BuildControlPanel(variable, browsePath);

        var switchControl = InformationModel.MakeObject<Switch>("Switch");
        switchControl.VerticalAlignment = VerticalAlignment.Center;
        switchControl.HorizontalAlignment = HorizontalAlignment.Right;
        switchControl.RightMargin = 100;
        switchControl.Width = 60;

        string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
        MakeDynamicLink(switchControl.GetVariable("Checked"), aliasRelativeNodePath);
        panel.Add(switchControl);

        return panel;
    }

    private List<Item> BuildSwitchArray(IUAVariable variable, List<string> browsePath) {
        var result = new List<Item>();

        var arrayDimensions = variable.ArrayDimensions;
        for (uint index = 0; index < arrayDimensions[0]; ++index) {
            var panel = BuildControlPanel(variable, browsePath, new uint[] { index });

            var switchControl = InformationModel.MakeObject<Switch>("Switch");
            switchControl.VerticalAlignment = VerticalAlignment.Center;
            switchControl.HorizontalAlignment = HorizontalAlignment.Right;
            switchControl.RightMargin = 100;
            switchControl.Width = 60;

            string aliasRelativeNodePath = MakeNodePathRelativeToAlias(schema.BrowseName, browsePath);
            MakeDynamicLink(switchControl.GetVariable("Checked"), aliasRelativeNodePath, index);
            panel.Add(switchControl);

            result.Add(panel);
        }

        return result;
    }

    private void MakeDynamicLink(IUAVariable parent, string nodePath) {
        var dynamicLink = InformationModel.MakeVariable<DynamicLink>("DynamicLink", FTOptix.Core.DataTypes.NodePath);
        dynamicLink.Value = nodePath;
        dynamicLink.Mode = DynamicLinkMode.ReadWrite;
        parent.Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, dynamicLink);
    }

    private void MakeDynamicLink(IUAVariable parent, string nodePath, uint index) {
        MakeDynamicLink(parent, nodePath + "[" + index.ToString() + "]");
    }

    private string MakeNodePathRelativeToAlias(string aliasName, List<string> browsePath) {
        return "{" + NodePath.EscapeNodePathBrowseName(schema.BrowseName) + "}/" + BrowsePathToNodePath(browsePath);
    }

    private string BrowsePathToNodePath(List<string> browsePath) {
        if (browsePath.Count == 1)
            return NodePath.EscapeNodePathBrowseName(browsePath[0]);

        string result = "";

        for (int i = 0; i < browsePath.Count; ++i) {
            result += NodePath.EscapeNodePathBrowseName(browsePath[i]);
            if (i != browsePath.Count - 1)
                result += "/";
        }

        return result;
    }

    private RecipeSchema schema;
    private IUANode target;
}
