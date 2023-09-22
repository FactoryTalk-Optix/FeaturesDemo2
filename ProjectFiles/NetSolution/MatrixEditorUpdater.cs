#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.Reflection;
using UAManagedCore;
using FTOptix.UI;
#endregion

public class MatrixEditorUpdater : BaseNetLogic {
    public override void Start() {
        var context = LogicObject.Context;
        logicObjectAffinityId = context.AssignAffinityId();
        logicObjectSenderId = context.AssignSenderId();

        // Check if the given array is valid and convert it to a C# Array
        matrixValueVariable = Owner.GetVariable("MatrixValue");
        if (matrixValueVariable == null)
            throw new CoreConfigurationException("Unable to find MatrixValue variable");
        var matrixValueVariableValue = matrixValueVariable.Value.Value;
        if (!matrixValueVariableValue.GetType().IsArray)
            throw new CoreConfigurationException("MatrixValue is not an array");
        var matrixArray = (Array)matrixValueVariableValue;
        if (matrixArray.Rank != 2)
            throw new CoreConfigurationException("Only two-dimensional arrays are supported");

        // GridModel represents a support variable that acts as a link between the VectorValue model variable and the widget data grid.
        gridModelVariable = LogicObject.GetVariable("GridModel");

        using (var resumeDispatchOnExit = context.SuspendDispatch(logicObjectAffinityId)) {
            // Register the observer on MatrixValue
            matrixValueVariableChangeObserver = new CallbackVariableChangeObserver(MatrixValueVariableValueChanged);
            matrixValueVariableRegistration = matrixValueVariable.RegisterEventObserver(
                matrixValueVariableChangeObserver, EventType.VariableValueChanged, logicObjectAffinityId);

            cellVariableChangeObserver = new CallbackVariableChangeObserver(CellVariableValueChanged);
            CreateGrid(matrixArray);
        }
    }

    public override void Stop() {
        using (var destroyDispatchOnExit = LogicObject.Context.TerminateDispatchOnStop(logicObjectAffinityId)) {
            if (cellVariableRegistrations != null) {
                cellVariableRegistrations.ForEach(registration => registration.Dispose());
                cellVariableRegistrations = null;
            }

            if (matrixValueVariableRegistration != null) {
                matrixValueVariableRegistration.Dispose();
                matrixValueVariableRegistration = null;
            }

            if (gridModelVariable != null)
                gridModelVariable.Value = NodeId.Empty;

            if (gridObject != null) {
                gridObject.Delete();
                gridObject = null;
            }

            currentRowCount = 0;
            currentCellCount = 0;

            gridModelVariable = null;
            matrixValueVariable = null;
            logicObjectSenderId = 0;
            logicObjectAffinityId = 0;
        }
    }

    #region Initialize GridModel from MatrixValue
    private void CreateGrid(Array matrixArray) {
        if (cellVariableRegistrations != null) {
            cellVariableRegistrations.ForEach(registration => registration.Dispose());
            cellVariableRegistrations.Clear();
        } else
            cellVariableRegistrations = new List<IEventRegistration>();

        currentRowCount = (uint)matrixArray.GetLength(0);
        currentCellCount = (uint)matrixArray.GetLength(1);

        // Create and initialize the Grid-supporting object
        gridObject = InformationModel.MakeObject("Grid");
        for (uint rowIndex = 0; rowIndex < currentRowCount; ++rowIndex)
            gridObject.Add(CreateRow(matrixArray, rowIndex));

        LogicObject.Add(gridObject);
        gridModelVariable.Value = gridObject.NodeId;
    }

    private IUAObject CreateRow(Array matrixArray, uint rowIndex) {
        var rowObject = InformationModel.MakeObject($"Row{rowIndex}");

        // Determine the OPC UA type from the given C# Array
        var netType = matrixArray.GetType().GetElementType().GetTypeInfo();
        var opcuaTypeNodeId = DataTypesHelper.GetDataTypeIdByNetType(netType);
        if (opcuaTypeNodeId == null)
            throw new CoreConfigurationException($"Unable to find an OPC UA data type corresponding to the {netType} .NET type");

        var cellCount = (uint)matrixArray.GetLength(1);
        for (uint cellIndex = 0; cellIndex < cellCount; ++cellIndex) {
            // Create the cell variable and register for changes
            var cellVariable = InformationModel.MakeVariable($"Cell{cellIndex}", opcuaTypeNodeId);
            cellVariable.Value = new UAValue(matrixArray.GetValue(rowIndex, cellIndex));
            cellVariableRegistrations.Add(cellVariable.RegisterEventObserver(cellVariableChangeObserver,
                EventType.VariableValueChanged, logicObjectAffinityId));

            // Add the cell variable to the grid
            rowObject.Add(cellVariable);
        }

        return rowObject;
    }

    #endregion

    #region Monitor each element inside MatrixValue
    private void CellVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) {
        if (senderId == logicObjectSenderId)
            return;

        var cellBrowseName = variable.BrowseName;
        var cellIndex = uint.Parse(cellBrowseName.Remove(0, "Cell".Length));

        var rowBrowseName = variable.Owner.BrowseName;
        var rowIndex = uint.Parse(rowBrowseName.Remove(0, "Row".Length));

        using (var restorePreviousSenderIdOnExit = LogicObject.Context.SetCurrentThreadSenderId(logicObjectSenderId)) {
            matrixValueVariable.SetValue(newValue.Value, new uint[] { rowIndex, cellIndex });
        }
    }

    #endregion

    #region Monitor MatrixValue variable

    private void MatrixValueVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) {
        if (senderId == logicObjectSenderId)
            return;

        if (indexes.Length > 0)
            UpdateCellValue(newValue, indexes);
        else
            UpdateAllCellValues((Array)newValue.Value);
    }

    private void UpdateAllCellValues(Array matrixArray) {
        var rowCount = (uint)matrixArray.GetLength(0);
        var cellCount = (uint)matrixArray.GetLength(1);

        // Rebuild the entire grid model if the number of cells changes
        if (cellCount != currentCellCount) {
            CreateGrid(matrixArray);
            return;
        }

        // Add or remove rows if the number of rows changes
        if (rowCount > currentRowCount)
            AddRows(currentRowCount, rowCount - 1, matrixArray);
        else if (rowCount < currentRowCount)
            RemoveLastRows(rowCount, currentRowCount - 1);

        currentRowCount = rowCount;
        currentCellCount = cellCount;

        for (uint rowIndex = 0; rowIndex < rowCount; ++rowIndex)
            for (uint cellIndex = 0; cellIndex < cellCount; ++cellIndex)
                UpdateCellValue(new UAValue(matrixArray.GetValue(rowIndex, cellIndex)), new uint[] { rowIndex, cellIndex });
    }

    private void AddRows(uint fromRow, uint toRow, Array values) {
        for (uint rowIndex = fromRow; rowIndex <= toRow; ++rowIndex)
            gridObject.Add(CreateRow(values, rowIndex));
    }

    private void RemoveLastRows(uint fromRow, uint toRow) {
        for (uint rowIndex = fromRow; rowIndex <= toRow; ++rowIndex) {
            var rowObject = gridObject.Children[$"Row{rowIndex}"];
            rowObject.Delete();
        }
    }

    private void UpdateCellValue(UAValue newValue, uint[] indexes) {
        var cellObject = gridObject.Children[$"Row{indexes[0]}"].GetVariable($"Cell{indexes[1]}");

        using (var restorePreviousSenderIdOnExit = LogicObject.Context.SetCurrentThreadSenderId(logicObjectSenderId)) {
            cellObject.Value = newValue;
        }
    }

    #endregion

    private uint logicObjectAffinityId;
    private ulong logicObjectSenderId;

    private IUAVariable matrixValueVariable;
    private IUAVariable gridModelVariable;
    private IUAObject gridObject;
    private uint currentRowCount = 0;
    private uint currentCellCount = 0;

    private IEventObserver matrixValueVariableChangeObserver;
    private IEventObserver cellVariableChangeObserver;
    private IEventRegistration matrixValueVariableRegistration;
    private List<IEventRegistration> cellVariableRegistrations;
}
