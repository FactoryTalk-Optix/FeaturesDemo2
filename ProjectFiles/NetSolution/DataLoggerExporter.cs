#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.IO;
using System.Text;
using UAManagedCore;
#endregion

public class DataLoggerExporter : BaseNetLogic {
    private DelayedTask outMessage;

    private void ResetMessage()
    {
        LogicObject.GetVariable("ExportResult").Value = false;
        outMessage.Dispose();
    }

    [ExportMethod]
    public void Export() {
        try {
            ValidateTimeSlice();

            var csvPath = GetCSVFilePath();
            if (string.IsNullOrEmpty(csvPath))
                throw new Exception("No CSV file chosen, please fill the CSVPath variable");

            char? fieldDelimiter = GetFieldDelimiter();
            bool wrapFields = GetWrapFields();
            var tableObject = GetTable();
            var storeObject = GetStoreObject(tableObject);
            var selectQuery = CreateQuery(tableObject);

            storeObject.Query(selectQuery, out string[] header, out object[,] resultSet);

            if (header == null || resultSet == null)
                throw new Exception("Unable to execute SQL query, malformed result");

            var rowCount = resultSet.GetLength(0);
            var columnCount = resultSet.GetLength(1);

            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = fieldDelimiter.Value, WrapFields = wrapFields }) {
                csvWriter.WriteLine(header);
                WriteTableContent(resultSet, rowCount, columnCount, csvWriter);
            }

            Log.Info("DataLoggerExporter", "The Data logger " + tableObject.BrowseName + " has been succesfully exported to " + csvPath);

            // Reset message
            LogicObject.GetVariable("ExportResult").Value = true;
            outMessage?.Dispose();
            outMessage = new DelayedTask(ResetMessage, 2500, LogicObject);
            outMessage.Start();

        } catch (Exception ex) {
            Log.Error("DataLoggerExporter", "Unable to export data logger: " + ex.Message);
        }

    }

    private void WriteTableContent(object[,] resultSet, int rowCount, int columnCount, CSVFileWriter csvWriter) {
        for (var r = 0; r < rowCount; ++r) {
            var currentRow = new string[columnCount];

            for (var c = 0; c < columnCount; ++c)
                currentRow[c] = resultSet[r, c]?.ToString() ?? "NULL";

            csvWriter.WriteLine(currentRow);
        }
    }

    private Table GetTable() {
        var tableVariable = LogicObject.GetVariable("Table");

        if (tableVariable == null)
            throw new Exception("Table variable not found");

        NodeId tableNodeId = tableVariable.Value;
        if (tableNodeId == null || tableNodeId == NodeId.Empty)
            throw new Exception("Table variable is empty");

        var tableNode = InformationModel.Get<Table>(tableNodeId);
        if (tableNode == null)
            throw new Exception("The specified table node is not an instance of Store Table type");

        return tableNode;
    }

    private Store GetStoreObject(Table tableNode) {
        return tableNode.Owner.Owner as Store;
    }

    private string GetCSVFilePath() {
        var csvPathVariable = LogicObject.GetVariable("CSVPath");
        if (csvPathVariable == null)
            throw new Exception("CSVPath variable not found");

        return new ResourceUri(csvPathVariable.Value).Uri;
    }

    private char GetFieldDelimiter() {
        var separatorVariable = LogicObject.GetVariable("FieldDelimiter");
        if (separatorVariable == null)
            throw new Exception("FieldDelimiter variable not found");

        string separator = separatorVariable.Value;

        if (separator == string.Empty)
            throw new Exception("FieldDelimiter variable is empty");

        if (separator.Length != 1)
            throw new Exception("Wrong FieldDelimiter configuration. Please insert a single character");

        if (!char.TryParse(separator, out char result))
            throw new Exception("Wrong FieldDelimiter configuration. Please insert a char");

        return result;
    }

    private bool GetWrapFields() {
        var wrapFieldsVariable = LogicObject.GetVariable("WrapFields");
        if (wrapFieldsVariable == null)
            throw new Exception("WrapFields variable not found");

        return wrapFieldsVariable.Value;
    }

    private string GetQueryFilter() {
        var queryVariable = LogicObject.GetVariable("Query");
        if (queryVariable == null)
            throw new Exception("Query variable not found");

        string query = queryVariable.Value;

        if (string.IsNullOrEmpty(query))
            throw new Exception("Query variable is empty or not valid");

        return query;
    }

    private string CreateQuery(Table table) {
        var queryColumns = GetQueryColumns(table);
        var queryFilter = GetQueryFilter();

        return $"SELECT {queryColumns} FROM \"{table.BrowseName}\" WHERE {queryFilter}";
    }

    private string GetQueryColumns(Table table) {
        var tableColumns = table.Columns;
        string columns = "";

        for (int i = 0; i < tableColumns.Count; i++) {
            var columnName = tableColumns[i].BrowseName;
            if (columnName == "Id")
                continue;

            columns += $"\"{columnName}\"";

            if (i != tableColumns.Count - 1 && tableColumns[i + 1].BrowseName != "Id")
                columns += ", ";
        }

        return columns;
    }

    private void ValidateTimeSlice() {
        var fromVariable = LogicObject.GetVariable("From");
        if (fromVariable == null || fromVariable.Value == null)
            throw new Exception("From variable is empty or missing");
        var toVariable = LogicObject.GetVariable("To");
        if (toVariable == null || toVariable.Value == null)
            throw new Exception("To variable is empty or missing");

        DateTime fromValue = fromVariable.Value;
        DateTime toValue = toVariable.Value;

        if (fromValue <= minumumSupportedDate)
            Log.Warning("DataLoggerExporter", "The From value is lower than the minumum supported date");

        if (toValue == minimumOpcUaDate)
            throw new Exception("The To property is not set");

        if (toValue < fromValue)
            throw new Exception("Not a valid time slice. The date entered in the From property is later than the date entered in the To");
    }

    // OPC UA DateTime minimum value is "1601-01-01T00:00:00.000Z". It indicates that the value was not set.
    private readonly DateTime minimumOpcUaDate = new DateTime(1601, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);

    // An intersection of the minumum supported Timestamps by Sqlserver (1/1/1753), MYSQL (1/1/1970) and an Embedded database
    private readonly DateTime minumumSupportedDate = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);

    #region CSVFileWriter
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
}
