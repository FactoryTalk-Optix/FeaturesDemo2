#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using UAManagedCore;
#endregion

public class MultiStateSelectorStatusUpdater : BaseNetLogic {
    public override void Start() {
        styleSheet = GetStyleSheet();

        multiStateSelector = Owner as Rectangle;
        if (multiStateSelector == null)
            throw new CoreConfigurationException("MultiStateSelector cannot be null");

        circularHandle = Owner.Children.Get("CircularHandle") as Image;
        if (circularHandle == null)
            throw new CoreConfigurationException("CircularHandle cannot be null");

        // The width of the circular handle is set to Stretch to update its dimensions in QStudio editor.
        // In UpdateMultiStateSelectorView it is necessary to know the exact width that must be equal to
        // to the Height of the MultiStateSelector object
        circularHandle.Width = multiStateSelector.Height;

        numberOfStatesVariable = Owner.GetVariable("NumberOfStates");
        if (numberOfStatesVariable == null)
            throw new CoreConfigurationException("NumberOfStates cannot be null");

        valueVariable = Owner.GetVariable("Value");
        if (valueVariable == null)
            throw new CoreConfigurationException("Value variable cannot be null");

        affinityId = Project.Current.Context.AssignAffinityId();
        RegisterValueVariableChangedObserver();
        RegisterNumberOfStatesChangedObserver();

        // It is necessary to force a variableChanged to initialize the widget and
        // to fix possible illegal initial values
        OnValueVariableChanged(valueVariable.Value);
    }

    public override void Stop() {
        valueVariableChangeRegistration?.Dispose();
        valueVariableChangeRegistration = null;

        numberOfStatesVariableChangeRegistration?.Dispose();
        numberOfStatesVariableChangeRegistration = null;
    }

    [ExportMethod]
    public void UpdateStatus() {
        try {
            lock (isWaitingLock) {
                valueVariable.Value = (uint)((valueVariable.Value + 1) % GetNumberOfStates());
            }
        } catch (Exception ex) {
            Log.Error("MultiStateSelector", $"Unable to change value for multi-state selector {Log.Node(multiStateSelector)}: {ex}");
        }
    }

    private void RegisterValueVariableChangedObserver() {
        var valueVariableObserver = new CallbackVariableChangeObserver((_, incomingValue, __, ___, _____) => {
            OnValueVariableChanged(incomingValue);
        });
        valueVariableChangeRegistration = valueVariable.RegisterEventObserver(valueVariableObserver, EventType.VariableValueChanged, affinityId);
    }

    private void RegisterNumberOfStatesChangedObserver() {
        var numberOfStatesVariableObserver = new CallbackVariableChangeObserver((_, incomingValue, __, ___, ____) => {
            valueVariable.Value = 0;
        });
        numberOfStatesVariableChangeRegistration = numberOfStatesVariable.RegisterEventObserver(numberOfStatesVariableObserver, EventType.VariableValueChanged, affinityId);
    }

    private void OnValueVariableChanged(int incomingValue) {
        try {
            lock (isWaitingLock) {
                int updatedValue = incomingValue;

                // Normalize incoming value inside the [0, ..., numberOfStates) range
                // Outside these limits, the closest allowed value is taken.
                uint lastStatusAllowed = GetNumberOfStates() - 1;
                if (updatedValue < 0)
                    valueVariable.Value = 0;
                else if (updatedValue > lastStatusAllowed)
                    valueVariable.Value = lastStatusAllowed;
                else
                    UpdateMultiStateSelectorView((uint)incomingValue);
            }
        } catch (Exception ex) {
            Log.Error("MultiStateSelector", $"Unable to change value for multi-state selector {Log.Node(multiStateSelector)}: {ex}");
        }
    }

    private void UpdateMultiStateSelectorView(uint newStatus) {
        SetMultiStateSelectorBackgroundColor(newStatus);

        // Compute the position of newStatus inside MultiStateSelector.
        // e.g. newStatus==3 it means that before there are 3 states, with possible intra-handle spaces (states are 0-indexed).
        // If the space between two handles is negative it means these handles must ideally overlap to fit into external rectangle width.
        // Thus it is necessary to subtract some space from the circular handle LeftMargin.
        var circularHandleWithTrailingSpaceWidth = circularHandle.Width + GetSpaceBetweenHandles();
        circularHandle.LeftMargin = circularHandleWithTrailingSpaceWidth * newStatus;
    }

    private void SetMultiStateSelectorBackgroundColor(uint newStatus) {
        Color color;
        if (newStatus == 0)
            color = styleSheet.InteractiveColor;
        else
            color = styleSheet.AccentColor;

        multiStateSelector.FillColor = color;
    }

    private float GetSpaceBetweenHandles() {
        // Calculate the remaining space that is not occupied by circular handles. This space must then be uniformly distributed among all states.
        var numberOfStates = GetNumberOfStates();
        var spaceOccupiedByCircularHandles = circularHandle.Width * numberOfStates;
        var spaceToDistributeBetweenCircularHandles = multiStateSelector.Width - spaceOccupiedByCircularHandles;
        return spaceToDistributeBetweenCircularHandles / (numberOfStates - 1);
    }

    private uint GetNumberOfStates() {
        uint numberOfStates = numberOfStatesVariable.Value;
        if (numberOfStates <= 1)
            throw new CoreException($"Illegal NumberOfStates value for multi-state selector {Log.Node(multiStateSelector)}, must be greater than 1");

        return numberOfStates;
    }

    private StyleSheet GetStyleSheet() {
        IUANode node = Owner;
        while (!(node is PresentationEngine))
            node = node.Owner;

        var stylesheetVariable = node.GetVariable("StyleSheet");
        if (stylesheetVariable == null)
            throw new CoreConfigurationException("StyleSheet variable cannot be null");

        NodeId stylesheetNodeId = stylesheetVariable.Value;
        if (stylesheetNodeId == null)
            throw new CoreConfigurationException($"The StyleSheet variable in presentation engine {Log.Node(node)} must be non-empty");

        return LogicObject.Context.GetNode(stylesheetNodeId) as StyleSheet;
    }

    private StyleSheet styleSheet;

    private Rectangle multiStateSelector;
    private Image circularHandle;

    private IUAVariable numberOfStatesVariable;
    private IUAVariable valueVariable;

    // Event handling for ValueVariable and NumberOfStates changes
    private uint affinityId;
    private IEventRegistration valueVariableChangeRegistration;
    private IEventRegistration numberOfStatesVariableChangeRegistration;

    private readonly object isWaitingLock = new object();
}
