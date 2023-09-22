#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class ImportAndExportTranslations : BaseNetLogic {
    [ExportMethod]
    public void ExportTranslations() {
        Log.Info("ImportAndExportTranslations", $"--- Begin Export ---");
        var csvPath = GetCSVFilePath();
        if (string.IsNullOrEmpty(csvPath)) {
            Log.Error("ImportAndExportTranslations", "No CSV file, please fill the CSVPath variable");
            return;
        }

        char? characterSeparator = GetCharacterSeparator();
        if (characterSeparator == null || characterSeparator == '\0')
            return;

        bool wrapFields = GetWrapFields();

        var localizationDictionary = GetDictionary();
        if (localizationDictionary == null) {
            Log.Error("ImportAndExportTranslations", "No translation table found");
            return;
        }

        var dictionary = (string[,])localizationDictionary.Value.Value;
        var rowCount = dictionary.GetLength(0);
        var columnCount = dictionary.GetLength(1);

        try {
            using (var csvWriter = new CSVFileWriter(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields }) {
                for (var currentRow = 0; currentRow < rowCount; ++currentRow) {
                    var row = new string[columnCount];

                    for (var currentColumn = 0; currentColumn < columnCount; ++currentColumn) {
                        if (currentRow == 0 && currentColumn == 0)
                            row[currentColumn] = "Key";
                        else
                            row[currentColumn] = ReplaceNewLineWithSymbol(dictionary[currentRow, currentColumn]);
                    }

                    csvWriter.WriteLine(row);
                }
            }

            Log.Info("ImportAndExportTranslations", $"Translations successfully exported to {csvPath}");
        } catch (Exception ex) {
            Log.Error("ImportAndExportTranslations", $"Unable to export the translations: {ex}");
        }
    }

    [ExportMethod]
    public void ImportTranslations() {
        Log.Info("ImportAndExportTranslations", $"--- Begin Import ---");
        var csvPath = GetCSVFilePath();

        if (string.IsNullOrEmpty(csvPath)) {
            Log.Error("ImportAndExportTranslations", "No CSV file chosen, please fill the CSVPath variable");
            return;
        }

        char? characterSeparator = GetCharacterSeparator();
        if (characterSeparator == null || characterSeparator == '\0')
            return;

        bool wrapFields = GetWrapFields();

        var localizationDictionary = GetDictionary();
        if (localizationDictionary == null) {
            Log.Error("ImportAndExportTranslations", "No translation table found");
            return;
        }

        if (!File.Exists(csvPath)) {
            Log.Error("ImportAndExportTranslations", $"The file {csvPath} does not exist");
            return;
        }

        try {
            using (var csvReader = new CSVFileReader(csvPath) { FieldDelimiter = characterSeparator.Value, WrapFields = wrapFields }) {

                if (csvReader.EndOfFile()) {
                    Log.Error("ImportAndExportTranslations", $"The file {csvPath} is empty");
                    return;
                }

                var fileTranslations = csvReader.ReadAll();
                if (fileTranslations.Count == 0 || fileTranslations[0].Count == 0)
                    return;

                var fileTranslationsRows = fileTranslations.Count;
                var fileTranslationsColumns = fileTranslations[0].Count;

                var actualTranslations = (string[,])localizationDictionary.Value.Value;
                var actualTranslationsRows = actualTranslations.GetLength(0);
                var actualTranslationsColumns = actualTranslations.GetLength(1);

                string[,] newTranslations;

                if (actualTranslationsColumns > fileTranslationsColumns) {
                    Log.Error("ImportAndExportTranslations", $"One or more columns are missing into CSV file");
                    return;
                }

                if (actualTranslationsColumns < fileTranslationsColumns) {
                    newTranslations = new string[actualTranslationsRows, fileTranslationsColumns];
                } else {
                    newTranslations = new string[actualTranslationsRows, actualTranslationsColumns];
                }

                var actualTranslationList = actualTranslations.Cast<string>()
                    .Select((x, i) => new { x, index = i / actualTranslations.GetLength(1) })
                    .GroupBy(x => x.index)
                    .Select(x => x.Select(s => s.x).ToList())
                    .Skip(1)
                    .ToList();
                var fileTranslationKeys = fileTranslations.Select(fileTranslation => fileTranslation.FirstOrDefault()).Skip(1).ToList();
                var removedTranslations = fileTranslationKeys.Except(actualTranslationList.Select(actualTranslation => actualTranslation.FirstOrDefault())).ToList();
                if (removedTranslations.Count > 0) {
                    string[,] newTranslationsResized = new string[actualTranslationsRows + removedTranslations.Count, Math.Max(actualTranslationsColumns, fileTranslationsColumns)];
                    Array.Copy(newTranslations, newTranslationsResized, newTranslations.Length);
                    newTranslations = newTranslationsResized;

                    var newKeysIndex = 0;
                    foreach (var removedTranslation in removedTranslations) {
                        for (var keyFoundColumn = 0; keyFoundColumn < Math.Max(actualTranslationsColumns, fileTranslationsColumns); ++keyFoundColumn) {
                            newTranslations[actualTranslationsRows + newKeysIndex, keyFoundColumn] = ReplaceSymbolWithNewLine(fileTranslations[fileTranslationKeys.IndexOf(removedTranslation) + 1][keyFoundColumn]);
                        }
                        newKeysIndex++;
                    }
                }

                fileTranslations[0][0] = "";

                int keyFoundRow;
                var keyUpdated = 0;
                for (var currentActualTranslationRow = 0; currentActualTranslationRow < actualTranslationsRows; ++currentActualTranslationRow) {
                    keyFoundRow = -1;
                    for (var currentFileTranslationRow = 0; currentFileTranslationRow < fileTranslationsRows; ++currentFileTranslationRow) {
                        if (actualTranslations[currentActualTranslationRow, 0] == fileTranslations[currentFileTranslationRow][0]) {
                            keyFoundRow = currentFileTranslationRow;
                            break;
                        }
                    }

                    if (keyFoundRow >= 0) {

                        for (var keyFoundColumn = 0; keyFoundColumn < Math.Max(actualTranslationsColumns, fileTranslationsColumns); ++keyFoundColumn) {
                            try {
                                newTranslations[currentActualTranslationRow, keyFoundColumn] = ReplaceSymbolWithNewLine(fileTranslations[keyFoundRow][keyFoundColumn]);
                            } catch (Exception ex) {
                                Log.Error("ImportAndExportTranslations", $"Key '{fileTranslations[keyFoundRow][0]}' > exception at column {keyFoundColumn}. {ex}");
                                return;
                            }
                        }

                        keyUpdated++;
                        if (keyFoundRow != 0) {
                            Log.Info("ImportAndExportTranslations", $"Key '{fileTranslations[keyFoundRow][0]}' > successfully updated");
                        }

                    } else {
                        Log.Info("ImportAndExportTranslations", $"Key '{actualTranslations[currentActualTranslationRow, 0]}' > not found, skipped");
                        for (var column = 0; column < actualTranslationsColumns; ++column)
                            newTranslations[currentActualTranslationRow, column] = actualTranslations[currentActualTranslationRow, column];

                        for (var column = actualTranslationsColumns; column < fileTranslationsColumns; ++column) // in case files has more columns than actual dictionary
                            newTranslations[currentActualTranslationRow, column] = "";
                    }
                }

                localizationDictionary.Value = new UAValue(newTranslations);
                Log.Info("ImportAndExportTranslations", $"Successfully updated {keyUpdated - 1} of {actualTranslationsRows - 1} keys into {localizationDictionary.BrowseName} dictionary");
            }

        } catch (Exception ex) {
            Log.Error("ImportAndExportTranslations", $"Unable to import the translations: {ex}");
        }
    }

    private string GetCSVFilePath() {
        var csvPathVariable = LogicObject.GetVariable("CSVPath");
        if (csvPathVariable == null) {
            Log.Error("ImportAndExportTranslations", "CSVPath variable not found");
            return "";
        }

        string csvPath = LogicObject.GetVariable("CSVPath").Value;
        string[] csvSplittedPath = csvPath.Split('/');
        if (csvSplittedPath.Length <= 1) {
            return ResourceUri.FromProjectRelativePath(LogicObject.GetVariable("CSVPath").Value).Uri;
        } else {

            return new ResourceUri(csvPathVariable.Value).Uri;
        }
    }

    private char? GetCharacterSeparator() {
        var separatorVariable = LogicObject.GetVariable("CharacterSeparator");
        if (separatorVariable == null) {
            Log.Error("ImportAndExportTranslations", "CharacterSeparator variable not found");
            return null;
        }

        string separator = separatorVariable.Value;

        if (separator.Length != 1 || separator == string.Empty) {
            Log.Error("ImportAndExportTranslations", "Wrong CharacterSeparator configuration. Please insert a char");
            return null;
        }

        if (char.TryParse(separator, out char result))
            return result;

        return null;
    }

    private bool GetWrapFields() {
        var wrapFieldsVariable = LogicObject.GetVariable("WrapFields");
        if (wrapFieldsVariable == null) {
            Log.Error("ImportAndExportTranslations", "WrapFields variable not found");
            return false;
        }

        return wrapFieldsVariable.Value;
    }

    private IUAVariable GetDictionary() {
        var dictionaryVariable = LogicObject.GetVariable("LocalizationDictionary");
        if (dictionaryVariable == null) {
            Log.Info("ImportAndExportTranslations", "The first localization dictionary found will be used since the LocalizationDictionary variable cannot be not found");
            return GetDefaultDictionary();
        }

        NodeId nodeIdDictionaryValue = dictionaryVariable.Value;
        if (nodeIdDictionaryValue == null) {
            Log.Info("ImportAndExportTranslations", "The first localization dictionary found will be used since the LocalizationDictionary variable is not set");
            return GetDefaultDictionary();
        }

        var dictionaryNode = InformationModel.Get(nodeIdDictionaryValue);
        if (dictionaryNode == null) {
            Log.Error("ImportAndExportTranslations", "The node pointed by the LocalizationDictionary variable cannot be found");
            return null;
        }

        var resultDictionaryVariable = dictionaryNode as IUAVariable;
        if (resultDictionaryVariable == null || !resultDictionaryVariable.IsInstanceOf(FTOptix.Core.VariableTypes.LocalizationDictionary))
            Log.Error("ImportAndExportTranslations", "The node pointed by the LocalizationDictionary variable is not a localization dictionary");

        return resultDictionaryVariable;
    }

    private IUAVariable GetDefaultDictionary() {
        var localizationDictionaryType = Project.Current.Context.GetNode(FTOptix.Core.VariableTypes.LocalizationDictionary);
        var localizationDictionaries = localizationDictionaryType.InverseRefs.GetNodes(OpcUa.ReferenceTypes.HasTypeDefinition);

        foreach (var dictionaryNode in localizationDictionaries) {
            if (dictionaryNode.NodeId.NamespaceIndex == Project.Current.NodeId.NamespaceIndex)
                return (IUAVariable)dictionaryNode;
        }

        return null;
    }

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

            if (line.Length > 1 && IsQuoteChar(line, 0) && IsQuoteChar(line, line.Length - 1))
                line = line.Substring(1, line.Length - 2);

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

                    var lineErrorMessage = $"Error processing line {currentLineNumber}";
                    if (i == 0) {
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
    #endregion

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

    private string ReplaceNewLineWithSymbol(string i) => i.Replace("\n", newLinePlaceHolder);
    private string ReplaceSymbolWithNewLine(string i) => i.Replace(newLinePlaceHolder, "\n");

    private const string newLinePlaceHolder = "\\n";
}
