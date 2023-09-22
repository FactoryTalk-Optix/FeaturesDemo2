#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Recipe;
using FTOptix.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;

#endregion

public class RecipeController : BaseNetLogic {

    public override void Start() {
        variableSynchronizer = new RemoteVariableSynchronizer();

        ApplyFromDBTrigger = LogicObject.GetVariable("ApplyFromDBTrigger");
        if (ApplyFromDBTrigger != null)
            variableSynchronizer.Add(ApplyFromDBTrigger);

        ApplyFromEditModelTrigger = LogicObject.GetVariable("ApplyFromEditModelTrigger");
        if (ApplyFromEditModelTrigger != null)
            variableSynchronizer.Add(ApplyFromEditModelTrigger);

        DeleteTrigger = LogicObject.GetVariable("DeleteTrigger");
        if (DeleteTrigger != null)
            variableSynchronizer.Add(DeleteTrigger);

        ExportTrigger = LogicObject.GetVariable("ExportTrigger");
        if (ExportTrigger != null)
            variableSynchronizer.Add(ExportTrigger);

        ImportTrigger = LogicObject.GetVariable("ImportTrigger");
        if (ImportTrigger != null)
            variableSynchronizer.Add(ImportTrigger);

        LoadFromPLCTrigger = LogicObject.GetVariable("LoadFromPLCTrigger");
        if (LoadFromPLCTrigger != null)
            variableSynchronizer.Add(LoadFromPLCTrigger);

        SaveToDBTrigger = LogicObject.GetVariable("SaveToDBTrigger");
        if (SaveToDBTrigger != null)
            variableSynchronizer.Add(SaveToDBTrigger);

        SelectFromDBTrigger = LogicObject.GetVariable("SelectFromDBTrigger");
        if (SelectFromDBTrigger != null)
            variableSynchronizer.Add(SelectFromDBTrigger);
    }

    public override void Stop() {
        lock (lockObject) {
            task?.Dispose();
        }
    }

    [ExportMethod]
    public void SelectFromDB(string RecipeName, CopyErrorPolicy ErrorPolicy) {
        if (SelectFromDBTrigger != null)
            SelectFromDBTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName)) {
            name = GetRecipeName();
            if (String.IsNullOrEmpty(name)) {
                SetFeedback(2, GetLocalizedTextString("RecipeControllerEmptyRecipeName"));
                return;
            }
        }

        var editModelNode = schema.GetObject("EditModel");
        if (editModelNode == null)
            return;

        schema.CopyFromStoreRecipe(name, editModelNode.NodeId, ErrorPolicy);
    }

    [ExportMethod]
    public void SaveToDB(string RecipeName, CopyErrorPolicy ErrorPolicy) {
        if (SaveToDBTrigger != null)
            SaveToDBTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName)) {
            name = GetRecipeName();
            if (String.IsNullOrEmpty(name)) {
                SetFeedback(2, GetLocalizedTextString("RecipeControllerEmptyRecipeName"));
                return;
            }
        }

        try {
            var store = GetRecipeStore(schema);
            var editModel = GetEditModel(schema);

            if (store != null && editModel != null) {
                if (RecipeExistsInStore(store, schema, name)) {
                    // Save Recipe
                    schema.CopyToStoreRecipe(editModel.NodeId, name, ErrorPolicy);
                    SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerSaved")}");
                } else {
                    // Create recipe
                    schema.CreateStoreRecipe(name);
                    // Save Recipe
                    schema.CopyToStoreRecipe(editModel.NodeId, name, ErrorPolicy);
                    SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerCreatedAndSaved")}");
                }
            }
        } catch (Exception e) {
            SetFeedback(2, e.Message);
        }
    }

    [ExportMethod]
    public void Delete(string RecipeName) {
        if (DeleteTrigger != null)
            DeleteTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName)) {
            name = GetRecipeName();
            if (String.IsNullOrEmpty(name)) {
                SetFeedback(2, GetLocalizedTextString("RecipeControllerEmptyRecipeName"));
                return;
            }
        }

        try {
            schema.DeleteStoreRecipe(name);
            SetFeedback(1, $"{GetLocalizedTextString("RecipeControllerRecipe")} {name} {GetLocalizedTextString("RecipeControllerDeleted")}");
        } catch (Exception e) {
            SetFeedback(2, e.Message);
        }
    }

    [ExportMethod]
    public void LoadFromPLC(CopyErrorPolicy ErrorPolicy) {
        if (LoadFromPLCTrigger != null)
            LoadFromPLCTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        try {
            var editModel = GetEditModel(schema);
            if (editModel != null) {
                schema.Copy(schema.TargetNode, editModel.NodeId, ErrorPolicy);
                SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeLoaded"));
            }
        } catch (Exception e) {
            SetFeedback(2, e.Message);
        }
    }

    [ExportMethod]
    public void ApplyFromEditModel(CopyErrorPolicy ErrorPolicy) {
        if (ApplyFromEditModelTrigger != null)
            ApplyFromEditModelTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        try {
            var editModel = GetEditModel(schema);
            if (editModel != null) {
                schema.CopyFromEditModel(editModel.NodeId, schema.TargetNode, ErrorPolicy);
                SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeApplied"));
            }
        } catch (Exception e) {
            SetFeedback(2, e.Message);
        }
    }

    [ExportMethod]
    public void ApplyFromDB(string RecipeName, CopyErrorPolicy ErrorPolicy) {
        if (ApplyFromDBTrigger != null)
            ApplyFromDBTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        string name = RecipeName;
        if (String.IsNullOrEmpty(RecipeName)) {
            name = GetRecipeName();
            if (String.IsNullOrEmpty(name)) {
                SetFeedback(2, GetLocalizedTextString("RecipeControllerEmptyRecipeName"));
                return;
            }
        }

        try {
            schema.CopyFromStoreRecipe(name, schema.TargetNode, ErrorPolicy);
            SetFeedback(1, GetLocalizedTextString("RecipeControllerRecipeApplied"));
            LogicObject.GetVariable("LastAppliedRecipe").SetValueNoPermissions(name);
        } catch (Exception e) {
            SetFeedback(2, e.Message);
        }
    }

    [ExportMethod]
    public void Export() {
        if (ExportTrigger != null)
            ExportTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        var csvPath = GetCSVFilePath();

        if (string.IsNullOrEmpty(csvPath)) {
            SetFeedback(2, "Unable to export recipes: please specify the output CSV file");
            return;
        }

        var separator = GetSeparator();
        if (separator == '.') {
            SetFeedback(2, "Unable to import recipes: CSV separator " + separator + " is not supported");
            return;
        }

        bool wrapFields = GetWrapFields();

        var storeObject = GetStoreObject(schema);
        if (storeObject == null) {
            SetFeedback(2, "Unable to export recipes to CSV file: Store object not found");
            return;
        }

        var tableName = GetTableName(schema);

        // Retrieve all recipes from table
        object[,] resultSet;
        string[] header;
        string selectQuery = "SELECT * FROM \"" + tableName + "\"";
        storeObject.Query(selectQuery, out header, out resultSet);

        if (header == null || resultSet == null || resultSet.Length == 0) {
            // No recipes or wrong result
            SetFeedback(2, $"No recipes found to export. Store {storeObject.BrowseName} has no recipes or an error occurred");
            return;
        }

        // Check column names
        foreach (var columnName in header) {
            if (columnName.Contains(separator.ToString())) {
                SetFeedback(2, "Unable to export recipes to CSV file: the name of parameter " +
                    columnName + " contains the CSV separator " + separator + ". Please specify a different CSV separator");
                return;
            }
        }

        var rowCount = resultSet.GetLength(0);
        var columnCount = resultSet.GetLength(1);

        try {
            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = separator, WrapFields = wrapFields }) {
                // Write header
                csvWriter.WriteLine(header);

                // For each recipe write a line to the CSV file
                for (var r = 0; r < rowCount; ++r) {
                    var currentRow = new string[columnCount];

                    for (var c = 0; c < columnCount; ++c) {
                        var recipeParameter = Convert.ToString(resultSet[r, c], CultureInfo.InvariantCulture);
                        currentRow[c] = string.IsNullOrEmpty(recipeParameter) ? "NULL" : recipeParameter;
                    }

                    csvWriter.WriteLine(currentRow);
                }
            }
            SetFeedback(1, "Recipes successfully exported to " + csvPath);
        } catch (Exception e) {
            SetFeedback(2, "Unable to write CSV file: " + e.Message);
        }
    }

    [ExportMethod]
    public void Import() {
        if (ImportTrigger != null)
            ImportTrigger.Value = 0;

        RecipeSchema schema = GetRecipeSchema();
        if (schema == null) return;

        var csvPath = GetCSVFilePath();

        if (string.IsNullOrEmpty(csvPath)) {
            SetFeedback(2, "Unable to import recipes: please specify the input CSV file");
            return;
        }

        var separator = GetSeparator();
        if (separator == '.') {
            SetFeedback(2, "Unable to import recipes: CSV separator . is not supported");
            return;
        }

        bool wrapFields = GetWrapFields();

        if (!File.Exists(csvPath)) {
            SetFeedback(2, "Unable to import recipes: CSV file " + csvPath + " not found");
            return;
        }

        var storeObject = GetStoreObject(schema);
        if (storeObject == null) {
            SetFeedback(2, "Unable to import recipes from CSV file: Store object not found");
            return;
        }

        var tableName = GetTableName(schema);
        var tableNode = storeObject.Tables.Get<Table>(tableName);
        if (tableNode == null) {
            SetFeedback(2, "Unable to import recipes from CSV file: table '" + tableName + "' not found in Store " + storeObject.BrowseName);
            return;
        }

        DeleteAlreadyExistingRecipes(storeObject, tableName, csvPath, separator, wrapFields);

        try {
            using (var csvReader = new CSVFileReader(csvPath) { FieldDelimiter = separator, IgnoreMalformedLines = true, WrapFields = wrapFields }) {
                if (csvReader.EndOfFile()) {
                    SetFeedback(2, "The file " + csvPath + " is empty");
                    return;
                }

                var header = csvReader.ReadLine();
                if (header == null || header.Count == 0) {
                    SetFeedback(2, "Error importing recipes. Recipe header does not contain any value or CSV file has an incorrect format");
                    return;
                }

                while (!csvReader.EndOfFile()) {
                    var parameters = csvReader.ReadLine();

                    if (parameters.Count != header.Count) {
                        // invalid line
                        continue;
                    }

                    var recipeName = parameters[0];

                    var values = new object[1, header.Count];
                    values[0, 0] = recipeName;

                    for (var p = 1; p < header.Count; ++p) {
                        // Remove "/" from the beginning of column name
                        var parameterBrowsePath = header[p].Substring(1);

                        if (parameters[p] == "NULL") {
                            // NULL field
                            continue;
                        }

                        var recipeParameterVariable = GetVariableFromRecipeSchema(schema.Root, parameterBrowsePath);
                        if (recipeParameterVariable == null) {
                            Log.Warning("RecipeManager", $"Could not find {parameterBrowsePath} in recipe {recipeName}");
                            continue;
                        }

                        if (IsVariableLinearArray(recipeParameterVariable)) {
                            var arraySize = recipeParameterVariable.ArrayDimensions[0];
                            ImportArrayVariable(recipeParameterVariable, p, values, parameters);
                            p += (int)arraySize - 1;
                        } else {
                            try {
                                values[0, p] = ConvertVariableValueToObject(recipeParameterVariable, parameters[p]);
                            } catch (Exception e) {
                                Log.Warning("RecipeManager", "Unable to import parameter '" + parameterBrowsePath +
                                                "' of recipe '" + recipeName +
                                                "': unsupported data type " + e.Message);
                            }
                        }
                    }

                    tableNode.Insert(header.ToArray(), values);
                }

                SetFeedback(1, "Recipes successfully imported to " + tableNode.BrowseName);
            }
        } catch (Exception e) {
            SetFeedback(2, "Unable to read CSV file " + csvPath + ": " + e.Message);
        }
    }

    private void SetFeedback(byte result, string message) {
        // result = 0 --> Idle
        // result = 1 --> Ok
        // result = 2 --> Error
        if (result == 2)
            Log.Error("RecipeController", message);
        LogicObject.GetVariable("Result").SetValueNoPermissions(result);
        LogicObject.GetVariable("Message").SetValueNoPermissions(message);

        lock (lockObject) {
            task?.Dispose();
            task = new DelayedTask(() => { ResetFeedback(); }, 5000, LogicObject);
            task.Start();
        }
    }

    private void ResetFeedback() {
        LogicObject.GetVariable("Result").SetValueNoPermissions(0);
        LogicObject.GetVariable("Message").SetValueNoPermissions(string.Empty);
    }

    private RecipeSchema GetRecipeSchema() {
        var schema = Owner as RecipeSchema;

        if (schema == null) {
            var recipeSchemaPtr = LogicObject.GetVariable("RecipeSchema");
            if (recipeSchemaPtr == null)
                SetFeedback(2, GetLocalizedTextString("RecipesEditorRecipeSchemaNotFound"));

            var nodeId = (NodeId)recipeSchemaPtr.Value;
            if (nodeId == null)
                SetFeedback(2, GetLocalizedTextString("RecipesEditorRecipeSchemaNotSet"));

            var recipeSchema = InformationModel.Get(nodeId);
            if (recipeSchema == null)
                SetFeedback(2, GetLocalizedTextString("RecipesEditorRecipeNotFound"));

            // Check if it has correct type
            schema = recipeSchema as RecipeSchema;
            if (schema == null)
                SetFeedback(2, $"{recipeSchema.BrowseName} {GetLocalizedTextString("RecipesEditorNotARecipe")}");
        }

        return schema;
    }

    private string GetRecipeName() {
        var logicObjectVariable = LogicObject.GetVariable("RecipeName");
        return logicObjectVariable.Value;
    }

    private FTOptix.Store.Store GetRecipeStore(RecipeSchema schema) {
        // Check if the store is set
        if (schema.Store == NodeId.Empty)
            SetFeedback(2, $"{GetLocalizedTextString("RecipesEditorStoreOfSchema")} {schema.BrowseName} {GetLocalizedTextString("RecipesEditorNotSet")}");

        // Get store node
        var storeNode = InformationModel.GetObject(schema.Store);
        if (storeNode == null)
            SetFeedback(2, $"{GetLocalizedTextString("RecipesEditorStore")} {schema.Store} {GetLocalizedTextString("RecipesEditorNotFound")}");

        // Check that it is actually a store
        var store = storeNode as FTOptix.Store.Store;
        if (store == null)
            SetFeedback(2, $"{GetLocalizedTextString("RecipesEditorStore")} {schema.Store} {GetLocalizedTextString("RecipesEditorNotAStore")}");

        return store;
    }

    private IUANode GetEditModel(RecipeSchema schema) {
        var editModel = schema.Get("EditModel");
        if (editModel == null)
            SetFeedback(2, $"{GetLocalizedTextString("RecipesEditorEditModelOfSchema")} {schema.BrowseName} {GetLocalizedTextString("RecipesEditorNotFound")}");

        return editModel;
    }

    private bool RecipeExistsInStore(FTOptix.Store.Store store, RecipeSchema schema, string recipeName) {
        // Perform query on the store in order to check if the recipe already exists
        object[,] resultSet;
        string[] header;
        var tableName = !String.IsNullOrEmpty(schema.TableName) ? schema.TableName : schema.BrowseName;
        store.Query("SELECT * FROM \"" + tableName + "\" WHERE Name LIKE \'" + recipeName + "\'", out header, out resultSet);
        var rowCount = resultSet != null ? resultSet.GetLength(0) : 0;
        return rowCount > 0;
    }

    private string GetLocalizedTextString(string textId) {
        var myLocalizedText = new LocalizedText(textId);
        return InformationModel.LookupTranslation(myLocalizedText).Text;
    }


    #region ImportExport

    private string GetCSVFilePath() {
        var csvPathVariable = LogicObject.GetVariable("CSVFile");
        if (csvPathVariable == null) {
            SetFeedback(2, "CSVFile variable not found");
            return "";
        }

        return new ResourceUri(csvPathVariable.Value).Uri;
    }

    private char GetSeparator() {
        var separatorVariable = LogicObject.GetVariable("CSVSeparator");
        string separatorString = separatorVariable.Value;

        return (separatorString.Length != 1) ? ',' : separatorString[0];
    }

    private bool GetWrapFields() {
        var wrapFieldsVariable = LogicObject.GetVariable("WrapFields");
        if (wrapFieldsVariable == null) {
            Log.Error("RecipeManager", "WrapFields variable not found");
            return false;
        }

        return wrapFieldsVariable.Value;
    }

    private Store GetStoreObject(RecipeSchema recipeSchema) {
        NodeId storeNodeId = recipeSchema.Store;
        var storeObject = InformationModel.Get(storeNodeId);
        if (storeObject == null)
            return null;

        return storeObject as Store;
    }

    private string GetTableName(RecipeSchema recipeSchema) {
        var tableName = recipeSchema.TableName;

        if (string.IsNullOrEmpty(tableName))
            tableName = recipeSchema.BrowseName;

        return tableName;
    }

    private void DeleteAlreadyExistingRecipes(Store storeObject, string tableName, string inputFile, char separator, bool wrapFields) {
        var recipes = GetRecipes(storeObject, tableName);

        try {
            using (var csvReader = new CSVFileReader(inputFile) { FieldDelimiter = separator, IgnoreMalformedLines = true, WrapFields = wrapFields }) {
                if (csvReader.EndOfFile())
                    return;

                var header = csvReader.ReadLine();
                if (header == null || header.Count == 0) {
                    SetFeedback(2, "Error deleting existing recipes. Recipe header does not contain any value or CSV file has an incorrect format");
                    return;
                }

                while (!csvReader.EndOfFile()) {
                    var parameters = csvReader.ReadLine();

                    if (parameters.Count != header.Count) {
                        // invalid line
                        continue;
                    }

                    // Delete recipe if it already exists
                    var recipeName = parameters[0];
                    if (recipes.Contains(recipeName))
                        DeleteRecipeForImport(storeObject, tableName, recipeName);
                }
            }
        } catch (Exception e) {
            SetFeedback(2, "Unable to read CSV file " + inputFile + ": " + e.Message);
        }
    }

    private void DeleteRecipeForImport(Store storeObject, string tableName, string recipeName) {
        object[,] resultSet;
        string[] header;

        string deleteQuery = "DELETE FROM \"" + tableName + "\" WHERE Name LIKE '" + recipeName + "'";
        storeObject.Query(deleteQuery, out header, out resultSet);
    }

    private HashSet<string> GetRecipes(Store storeObject, string tableName) {
        HashSet<string> result = new HashSet<string>();

        // Retrieve all recipes from table
        object[,] resultSet;
        string[] header;
        storeObject.Query("SELECT Name FROM \"" + tableName + "\"", out header, out resultSet);

        if (resultSet == null || resultSet.Length == 0) {
            // No recipes
            return result;
        }

        var rowCount = resultSet.GetLength(0);
        var columnCount = resultSet.GetLength(1);

        if (columnCount == 0)
            return result;

        for (var r = 0; r < rowCount; ++r)
            result.Add(resultSet[r, 0] as String);

        return result;
    }

    private IUAVariable GetVariableFromRecipeSchema(IUAObject recipeShemaRoot, string variableName) {
        var recipeVariable = recipeShemaRoot.GetVariable(variableName);
        if (recipeVariable == null) {
            var underscoreIndex = variableName.LastIndexOf("_");
            if (underscoreIndex > -1) {
                var arrayVariableName = variableName.Substring(0, underscoreIndex);
                recipeVariable = recipeShemaRoot.GetVariable(arrayVariableName);
            }
        }

        return recipeVariable;
    }

    private bool IsVariableLinearArray(IUAVariable variable) {
        return variable.ActualArrayDimensions.Length == 1;
    }

    private void ImportArrayVariable(IUAVariable arrayVariable, int startIndex, object[,] values, List<string> parameterList) {
        var arraySize = arrayVariable.ActualArrayDimensions[0];
        for (int k = 0; k < arraySize; ++k) {
            var currentArrayIndex = startIndex + k;
            try {
                values[0, currentArrayIndex] = ConvertVariableValueToObject(arrayVariable, parameterList[currentArrayIndex]);
            } catch (Exception e) {
                Log.Warning("RecipeImportExport", "Unable to import parameter '" + arrayVariable.BrowseName + e.Message);
            }
        }
    }

    private object ConvertVariableValueToObject(IUAVariable recipeParameterVariable, string parameterValue) {
        if (IsInteger(recipeParameterVariable))
            return Convert.ToInt64(parameterValue);
        else if (IsBool(recipeParameterVariable))
            return Convert.ToBoolean(Convert.ToInt32(parameterValue));
        else if (IsString(recipeParameterVariable))
            return parameterValue;
        else if (IsDuration(recipeParameterVariable))
            return Convert.ToDouble(parameterValue);
        else if (IsReal(recipeParameterVariable))
            return Convert.ToDouble(parameterValue, CultureInfo.InvariantCulture);
        else
            throw new Exception("Unable to convert recipe variable value, unsupported datatype");
    }

    private bool IsInteger(IUAVariable variable) {
        var dataTypeNode = variable.Context.GetDataType(variable.DataType);
        if (dataTypeNode == null)
            return false;

        return dataTypeNode.IsSubTypeOf(OpcUa.DataTypes.Integer) ||
            dataTypeNode.IsSubTypeOf(OpcUa.DataTypes.UInteger);
    }

    private bool IsReal(IUAVariable variable) {
        var dataTypeNode = variable.Context.GetDataType(variable.DataType);
        if (dataTypeNode == null)
            return false;

        return dataTypeNode.IsSubTypeOf(OpcUa.DataTypes.Float) ||
            dataTypeNode.IsSubTypeOf(OpcUa.DataTypes.Double);
    }

    private bool IsBool(IUAVariable variable) {
        return variable.DataType == OpcUa.DataTypes.Boolean;
    }

    private bool IsString(IUAVariable variable) {
        var dataTypeNode = variable.Context.GetDataType(variable.DataType);
        if (dataTypeNode == null)
            return false;

        return dataTypeNode.IsSubTypeOf(OpcUa.DataTypes.String);
    }

    private bool IsDuration(IUAVariable variable) {
        var dataTypeNode = variable.Context.GetDataType(variable.DataType);
        if (dataTypeNode == null)
            return false;

        return dataTypeNode.IsSubTypeOf(OpcUa.DataTypes.Duration);
    }
    #endregion


    #region CSVFileReader
    private class CSVFileReader : IDisposable {
        public char FieldDelimiter { get; set; } = ',';

        public char QuoteChar { get; set; } = '"';

        public bool WrapFields { get; set; } = false;

        public bool IgnoreMalformedLines { get; set; } = false;

        public CSVFileReader(string filePath, System.Text.Encoding encoding) {
            streamReader = new StreamReader(filePath, encoding);
        }

        public CSVFileReader(string filePath) {
            streamReader = new StreamReader(filePath, System.Text.Encoding.UTF8);
        }

        public CSVFileReader(StreamReader streamReader) {
            this.streamReader = streamReader;
        }

        public bool EndOfFile() {
            return streamReader.EndOfStream;
        }

        public List<string> ReadLine() {
            if (EndOfFile())
                return null;

            var line = streamReader.ReadLine();

            var result = WrapFields ? ParseLineWrappingFields(line) : ParseLineWithoutWrappingFields(line);

            currentLineNumber++;
            return result;

        }

        public List<List<string>> ReadAll() {
            var result = new List<List<string>>();
            while (!EndOfFile())
                result.Add(ReadLine());

            return result;
        }

        private List<string> ParseLineWithoutWrappingFields(string line) {
            if (string.IsNullOrEmpty(line) && !IgnoreMalformedLines)
                throw new FormatException($"Error processing line {currentLineNumber}. Line cannot be empty");

            return line.Split(FieldDelimiter).ToList();
        }

        private List<string> ParseLineWrappingFields(string line) {
            var fields = new List<string>();
            var buffer = new StringBuilder("");
            var fieldParsing = false;

            int i = 0;
            while (i < line.Length) {
                if (!fieldParsing) {
                    if (IsWhiteSpace(line, i)) {
                        ++i;
                        continue;
                    }

                    // Line and column numbers must be 1-based for messages to user
                    var lineErrorMessage = $"Error processing line {currentLineNumber}";
                    if (i == 0) {
                        // A line must begin with the quotation mark
                        if (!IsQuoteChar(line, i)) {
                            if (IgnoreMalformedLines)
                                return null;
                            else
                                throw new FormatException($"{lineErrorMessage}. Expected quotation marks at column {i + 1}");
                        }

                        fieldParsing = true;
                    } else {
                        if (IsQuoteChar(line, i))
                            fieldParsing = true;
                        else if (!IsFieldDelimiter(line, i)) {
                            if (IgnoreMalformedLines)
                                return null;
                            else
                                throw new FormatException($"{lineErrorMessage}. Wrong field delimiter at column {i + 1}");
                        }
                    }

                    ++i;
                } else {
                    if (IsEscapedQuoteChar(line, i)) {
                        i += 2;
                        buffer.Append(QuoteChar);
                    } else if (IsQuoteChar(line, i)) {
                        fields.Add(buffer.ToString());
                        buffer.Clear();
                        fieldParsing = false;
                        ++i;
                    } else {
                        buffer.Append(line[i]);
                        ++i;
                    }
                }
            }

            return fields;
        }

        private bool IsEscapedQuoteChar(string line, int i) {
            return line[i] == QuoteChar && i != line.Length - 1 && line[i + 1] == QuoteChar;
        }

        private bool IsQuoteChar(string line, int i) {
            return line[i] == QuoteChar;
        }

        private bool IsFieldDelimiter(string line, int i) {
            return line[i] == FieldDelimiter;
        }

        private bool IsWhiteSpace(string line, int i) {
            return Char.IsWhiteSpace(line[i]);
        }

        private StreamReader streamReader;
        private int currentLineNumber = 1;

        #region IDisposable support
        private bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing)
                streamReader.Dispose();

            disposed = true;
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }

    private class CSVFileWriter : IDisposable {
        public char FieldDelimiter { get; set; } = ',';

        public char QuoteChar { get; set; } = '"';

        public bool WrapFields { get; set; } = false;

        public CSVFileWriter(string filePath) {
            streamWriter = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        }

        public CSVFileWriter(string filePath, System.Text.Encoding encoding) {
            streamWriter = new StreamWriter(filePath, false, encoding);
        }

        public CSVFileWriter(StreamWriter streamWriter) {
            this.streamWriter = streamWriter;
        }

        public void WriteLine(string[] fields) {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < fields.Length; ++i) {
                if (WrapFields)
                    stringBuilder.AppendFormat("{0}{1}{0}", QuoteChar, EscapeField(fields[i]));
                else
                    stringBuilder.AppendFormat("{0}", fields[i]);

                if (i != fields.Length - 1)
                    stringBuilder.Append(FieldDelimiter);
            }

            streamWriter.WriteLine(stringBuilder.ToString());
            streamWriter.Flush();
        }

        private string EscapeField(string field) {
            var quoteCharString = QuoteChar.ToString();
            return field.Replace(quoteCharString, quoteCharString + quoteCharString);
        }

        private StreamWriter streamWriter;

        #region IDisposable Support
        private bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing)
                streamWriter.Dispose();

            disposed = true;
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion
    }
    #endregion

    private RemoteVariableSynchronizer variableSynchronizer;
    private DelayedTask task;
    private object lockObject = new object();
    private IUAVariable ApplyFromDBTrigger;
    private IUAVariable ApplyFromEditModelTrigger;
    private IUAVariable DeleteTrigger;
    private IUAVariable ExportTrigger;
    private IUAVariable ImportTrigger;
    private IUAVariable LoadFromPLCTrigger;
    private IUAVariable SaveToDBTrigger;
    private IUAVariable SelectFromDBTrigger;
}
