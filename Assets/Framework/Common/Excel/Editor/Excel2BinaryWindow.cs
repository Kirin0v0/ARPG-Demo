using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Excel;
using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;

namespace Framework.Common.Excel.Editor
{
    /// <summary>
    /// 导入Excel表，生成对应格式的数据Container类和Data类，同时输出表内数据的指定格式的二进制文件
    /// 后续数据读取需要借助<see cref="ExcelBinaryManager"/>
    /// </summary>
    public class Excel2BinaryWindow : EditorWindow
    {
        private struct ExcelSheetData
        {
            public string Name;
            public string PrimaryKeyFieldName;
            public string PrimaryKeyDataType;
            public List<ExcelColumnData> ColumnDataList;
        }

        private struct ExcelRowData
        {
            public int RowId;
            public List<string> ValueList;
        }

        private struct ExcelColumnData
        {
            public int ColumnId;
            public string FieldName;
            public string DataType;
            public bool PrimaryKey;
            public List<string> ValueList;
        }

        private static readonly string[] AllowedDataType = new[] { "string", "int", "float", "bool", "long" };

        private bool _init = false;
        private Vector2 _scrollPosition;

        // 导入相关
        private bool _isImportFolder;
        private string _importExcelPath;
        private string _importExcelFolderPath;
        private bool _onlyImportSpecifiedSheet;
        private string _importSheetName;
        private readonly List<string> _importExcelPathList = new();

        // 设置相关
        private int _fieldNameRowNumber;
        private int FieldNameRowNumber => _fieldNameRowNumber - 1;
        private int _dataTypeRowNumber;
        private int DataTypeRowNumber => _dataTypeRowNumber - 1;
        private int _primaryKeyRowNumber;
        private int PrimaryKeyRowNumber => _primaryKeyRowNumber - 1;
        private int _inputDataStartRowNumber;
        private int InputDataStartRowNumber => _inputDataStartRowNumber - 1;

        // 导出相关
        private bool _exportData;
        private string _exportDataPath;
        private string _exportDataNamespace;
        private bool _exportBinary;
        private string _exportBinaryPath;

        // 预览相关
        private bool _preview;
        private readonly List<ExcelSheetData> _previewExcelDataList = new();

        private Vector2 _scrollRect;

        [MenuItem("Window/Excel/Excel To Binary")]
        public static void ShowExcel2BinaryWindow()
        {
            var window = EditorWindow.GetWindow<Excel2BinaryWindow>(false, "Excel to Binary");
            window.Show();
        }

        private void OnEnable()
        {
            if (_init)
            {
                return;
            }

            _init = true;
            _scrollPosition = Vector2.zero;

            _isImportFolder = false;
            _importExcelPath = Application.dataPath;
            _importExcelFolderPath = Application.dataPath;
            _onlyImportSpecifiedSheet = false;
            _importSheetName = "";
            _fieldNameRowNumber = 1;
            _dataTypeRowNumber = 2;
            _primaryKeyRowNumber = 3;
            _inputDataStartRowNumber = 5;
            _exportData = false;
            _exportDataPath = Application.dataPath;
            _exportDataNamespace = "";
            _exportBinary = false;
            _exportBinaryPath = Application.dataPath;
            _importExcelPathList.Add(_importExcelPath);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            ShowExcelImportArea();
            EditorGUILayout.Separator();
            ShowExcelSettingsArea();
            EditorGUILayout.Separator();
            ShowExcelExportArea();
            EditorGUILayout.Separator();
            ShowPreviewAndGenerateArea();

            EditorGUILayout.EndScrollView();
        }

        private void ShowExcelImportArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "导入",
                titleStyle
            );

            var previousValue = _isImportFolder;

            // 展示导入文件区域
            _isImportFolder = !EditorGUILayout.BeginToggleGroup("导入Excel文件", !_isImportFolder);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(_importExcelPath);
            if (GUILayout.Button("选择文件", GUILayout.MaxWidth(100f)))
            {
                _importExcelPath = EditorUtility.OpenFilePanel("选择Excel文件", _importExcelPath, "xls,xlsx");
                ImportExcelFile();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _onlyImportSpecifiedSheet =
                EditorGUILayout.ToggleLeft("是否仅导入特定分页", _onlyImportSpecifiedSheet);
            _importSheetName = EditorGUILayout.TextField("导入分页名称", _importSheetName);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndToggleGroup();

            // 展示导入文件夹区域
            _isImportFolder = EditorGUILayout.BeginToggleGroup("导入文件夹内的全部Excel文件", _isImportFolder);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(_importExcelFolderPath);
            if (GUILayout.Button("选择文件夹", GUILayout.MaxWidth(100f)))
            {
                _importExcelFolderPath =
                    EditorUtility.OpenFolderPanel("选择文件夹", _importExcelFolderPath, "");
                ImportExcelFolder();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();

            var nowValue = _isImportFolder;
            if (previousValue != nowValue)
            {
                if (_isImportFolder)
                {
                    ImportExcelFolder();
                }
                else
                {
                    ImportExcelFile();
                }
            }

            void ImportExcelFile()
            {
                _importExcelPathList.Clear();
                _previewExcelDataList.Clear();
                if (String.IsNullOrEmpty(_importExcelPath))
                {
                    return;
                }

                _importExcelPathList.Add(_importExcelPath);
            }

            void ImportExcelFolder()
            {
                _importExcelPathList.Clear();
                _previewExcelDataList.Clear();
                if (String.IsNullOrEmpty(_importExcelFolderPath))
                {
                    return;
                }

                // 变量文件夹内的文件（不遍历子文件夹），获取xls和xlsx文件
                var excelFilePaths = Directory.GetFiles(_importExcelFolderPath)
                    .Where(fileName => fileName.EndsWith("xls") || fileName.EndsWith("xlsx"));
                _importExcelPathList.AddRange(excelFilePaths);
            }
        }

        private void ShowExcelSettingsArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "设置",
                titleStyle
            );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("字段行号");
            _fieldNameRowNumber = EditorGUILayout.IntField(_fieldNameRowNumber);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("数据类型行号");
            _dataTypeRowNumber = EditorGUILayout.IntField(_dataTypeRowNumber);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox($"数据类型只能为以下的类型：{String.Join("，", AllowedDataType)}", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("主键行号");
            _primaryKeyRowNumber = EditorGUILayout.IntField(_primaryKeyRowNumber);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("主键唯一", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输入数据起始行号");
            _inputDataStartRowNumber = EditorGUILayout.IntField(_inputDataStartRowNumber);
            EditorGUILayout.EndHorizontal();
        }

        private void ShowExcelExportArea()
        {
            var titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            EditorGUILayout.LabelField(
                "导出",
                titleStyle
            );

            _exportData = EditorGUILayout.BeginToggleGroup("导出C#类文件", _exportData);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("C#类命名空间");
            _exportDataNamespace = EditorGUILayout.TextField(_exportDataNamespace);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出至文件夹");
            EditorGUILayout.TextField(_exportDataPath);
            if (GUILayout.Button("选择文件夹", GUILayout.MaxWidth(100f)))
            {
                _exportDataPath = EditorUtility.OpenFolderPanel("选择文件夹", _exportDataPath, "");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndToggleGroup();

            _exportBinary = EditorGUILayout.BeginToggleGroup("导出二进制文件", _exportBinary);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出至文件夹");
            EditorGUILayout.TextField(_exportBinaryPath);
            if (GUILayout.Button("选择文件夹", GUILayout.MaxWidth(100f)))
            {
                _exportBinaryPath = EditorUtility.OpenFolderPanel("选择文件夹", _exportBinaryPath, "");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndToggleGroup();
        }

        private void ShowPreviewAndGenerateArea()
        {
            _preview = EditorGUILayout.Foldout(_preview, "展示导入数据的数据结构");
            if (_preview)
            {
                if (_importExcelPathList.Count != 0)
                {
                    _scrollRect =
                        EditorGUILayout.BeginScrollView(_scrollRect, GUILayout.MaxHeight(200f));

                    // 在未指定Excel或重新指定Excel时刷新缓存数据
                    if (_previewExcelDataList.Count == 0)
                    {
                        foreach (var excelPath in _importExcelPathList)
                        {
                            _previewExcelDataList.AddRange(ParseExcelFile(excelPath));
                        }
                    }

                    // 展示表结构
                    for (var i = 0; i < _previewExcelDataList.Count; i++)
                    {
                        var excelSheetData = _previewExcelDataList[i];
                        if (!_isImportFolder && _onlyImportSpecifiedSheet)
                        {
                            if (excelSheetData.Name != _importSheetName)
                            {
                                continue;
                            }
                        }

                        if (i > 0)
                        {
                            EditorGUILayout.Separator();
                        }

                        var titleStyle = new GUIStyle
                        {
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            normal =
                            {
                                textColor = Color.white
                            }
                        };
                        EditorGUILayout.LabelField(excelSheetData.Name, titleStyle);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("字段");
                        EditorGUILayout.LabelField("数据类型");
                        EditorGUILayout.LabelField("主键");
                        EditorGUILayout.EndHorizontal();
                        foreach (var excelColumnData in excelSheetData.ColumnDataList)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(excelColumnData.FieldName);
                            EditorGUILayout.LabelField(excelColumnData.DataType);
                            EditorGUILayout.LabelField(excelColumnData.PrimaryKey.ToString());
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            if (GUILayout.Button("导出"))
            {
                foreach (var excelPath in _importExcelPathList)
                {
                    var excelSheetDataList = ParseExcelFile(excelPath);
                    foreach (var excelSheetData in excelSheetDataList)
                    {
                        if (!_isImportFolder && _onlyImportSpecifiedSheet)
                        {
                            if (excelSheetData.Name != _importSheetName)
                            {
                                continue;
                            }
                        }

                        if (_exportData)
                        {
                            GenerateDataClassFile(excelSheetData);
                            GenerateContainerClassFile(excelSheetData);
                        }

                        if (_exportBinary)
                        {
                            GenerateBinaryFile(excelSheetData);
                        }
                    }
                }

                EditorUtility.DisplayDialog("导出结果", "成功！！！", "好的");
            }

            void GenerateDataClassFile(ExcelSheetData excelSheetData)
            {
                if (String.IsNullOrEmpty(_exportDataPath))
                {
                    throw new Exception("Export data path is empty");
                }

                if (!Directory.Exists(_exportDataPath))
                {
                    Directory.CreateDirectory(_exportDataPath);
                }

                var stringBuilder = new StringBuilder();

                // 拼接命名空间
                if (!String.IsNullOrEmpty(_exportDataNamespace))
                {
                    stringBuilder.Append("namespace ")
                        .Append(_exportDataNamespace)
                        .Append(" {\n");
                }

                // 拼接数据结构类名
                stringBuilder.Append("public class ")
                    .Append(ToUpperFirstCharacter(excelSheetData.Name))
                    .Append("Data \n{\n");

                // 拼接数据结构字段
                for (int i = 0; i < excelSheetData.ColumnDataList.Count; i++)
                {
                    var excelColumnData = excelSheetData.ColumnDataList[i];
                    stringBuilder.Append("    public ")
                        .Append(excelColumnData.DataType)
                        .Append(" ")
                        .Append(ToUpperFirstCharacter(excelColumnData.FieldName))
                        .Append(";\n");
                }

                stringBuilder.Append("}");
                if (!String.IsNullOrEmpty(_exportDataNamespace))
                {
                    stringBuilder.Append("}");
                }

                // 导出到指定路径下
                File.WriteAllText($"{_exportDataPath}/{ToUpperFirstCharacter(excelSheetData.Name)}Data.cs",
                    stringBuilder.ToString());

                // 刷新Project窗口
                AssetDatabase.Refresh();
            }

            void GenerateContainerClassFile(ExcelSheetData excelSheetData)
            {
                if (String.IsNullOrEmpty(_exportDataPath))
                {
                    throw new Exception("Export data path is empty");
                }

                if (!Directory.Exists(_exportDataPath))
                {
                    Directory.CreateDirectory(_exportDataPath);
                }

                var stringBuilder = new StringBuilder();
                stringBuilder.Append("using System.Collections.Generic;\n \n");

                // 拼接命名空间
                if (!String.IsNullOrEmpty(_exportDataNamespace))
                {
                    stringBuilder.Append("namespace ")
                        .Append(_exportDataNamespace)
                        .Append(" {\n");
                }

                // 拼接数据结构类名
                stringBuilder.Append("public class ")
                    .Append(ToUpperFirstCharacter(excelSheetData.Name))
                    .Append("Container \n{\n");

                // 拼接字典结构
                stringBuilder.Append("    public readonly Dictionary<")
                    .Append(excelSheetData.PrimaryKeyDataType)
                    .Append(", ")
                    .Append(ToUpperFirstCharacter(excelSheetData.Name))
                    .Append("Data> Data = new();\n");

                stringBuilder.Append("}");
                if (!String.IsNullOrEmpty(_exportDataNamespace))
                {
                    stringBuilder.Append("}");
                }

                // 导出到指定路径下
                File.WriteAllText($"{_exportDataPath}/{ToUpperFirstCharacter(excelSheetData.Name)}Container.cs",
                    stringBuilder.ToString());

                // 刷新Project窗口
                AssetDatabase.Refresh();
            }

            void GenerateBinaryFile(ExcelSheetData excelSheetData)
            {
                if (String.IsNullOrEmpty(_exportBinaryPath))
                {
                    throw new Exception("Export binary path is empty");
                }

                if (!Directory.Exists(_exportBinaryPath))
                {
                    Directory.CreateDirectory(_exportBinaryPath);
                }

                // 这里我们规定二进制格式为（主键字段字符串+每列字段名字符串+每列字段类型字符串+每列行数+每列每行值字符串）
                using var fileStream =
                    new FileStream($"{_exportBinaryPath}/{ToUpperFirstCharacter(excelSheetData.Name)}.bin",
                        FileMode.OpenOrCreate, FileAccess.Write);
                WriteStringToFileStream(fileStream, ToUpperFirstCharacter(excelSheetData.PrimaryKeyFieldName));
                
                if (excelSheetData.ColumnDataList.Count != 0)
                {
                    foreach (var excelColumnData in excelSheetData.ColumnDataList)
                    {
                        WriteStringToFileStream(fileStream, ToUpperFirstCharacter(excelColumnData.FieldName));
                        WriteStringToFileStream(fileStream, excelColumnData.DataType);
                        fileStream.Write(BitConverter.GetBytes(excelColumnData.ValueList.Count), 0, 4);
                        for (var i = 0; i < excelColumnData.ValueList.Count; i++)
                        {
                            var value = excelColumnData.ValueList[i];
                            try
                            {
                                // 根据不同类型存储不同格式二进制数据
                                switch (excelColumnData.DataType)
                                {
                                    case "int":
                                        fileStream.Write(BitConverter.GetBytes(string.IsNullOrEmpty(value) ? 0 : int.Parse(value)), 0, 4);
                                        break;
                                    case "float":
                                        fileStream.Write(BitConverter.GetBytes(string.IsNullOrEmpty(value) ? 0F : float.Parse(value)), 0, 4);
                                        break;
                                    case "long":
                                        fileStream.Write(BitConverter.GetBytes(string.IsNullOrEmpty(value) ? 0L : long.Parse(value)), 0, 8);
                                        break;
                                    case "bool":
                                        fileStream.Write(BitConverter.GetBytes(string.IsNullOrEmpty(value) ? false : bool.Parse(value)), 0, 1);
                                        break;
                                    case "string":
                                        WriteStringToFileStream(fileStream, value);
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                throw new Exception(
                                    $"Error: line-{_inputDataStartRowNumber + i + 1}, fieldName-{excelColumnData.FieldName}, DataType-{excelColumnData.DataType}, Value-{value}");
                            }
                        }
                    }
                }

                fileStream.Flush();
                fileStream.Close();

                // 刷新Project窗口
                AssetDatabase.Refresh();

                static void WriteStringToFileStream(FileStream fileStream, string str)
                {
                    var bytes = Encoding.UTF8.GetBytes(str);
                    fileStream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private List<ExcelSheetData> ParseExcelFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The excel file(\"{filePath}\") does not exist");
            }

            if (!filePath.EndsWith(".xls") && !filePath.EndsWith(".xlsx"))
            {
                throw new FileLoadException("The file is not excel file");
            }

            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var excelDataReader = ExcelReaderFactory.CreateOpenXmlReader(fileStream);
            var excelDataSet = excelDataReader.AsDataSet();
            var excelDataList = new List<ExcelSheetData>();
            for (var i = 0; i < excelDataSet.Tables.Count; i++)
            {
                var dataTable = excelDataSet.Tables[i];
                // 读取行数据
                var rowDataList = GetExcelRowData(dataTable);
                // 将行数据转为列数据
                var columnDataList = RowDataList2ColumnDataList(dataTable.TableName, rowDataList);

                // 检查列数据
                var findPrimaryKey = false;
                var primaryKeyFieldName = "";
                var primaryKeyDataType = "";
                foreach (var excelColumnData in columnDataList)
                {
                    if (excelColumnData.PrimaryKey)
                    {
                        if (findPrimaryKey)
                        {
                            throw new Exception($"Find more than one primary key in table {dataTable.TableName}");
                        }

                        findPrimaryKey = true;
                        primaryKeyFieldName = excelColumnData.FieldName;
                        primaryKeyDataType = excelColumnData.DataType;
                    }
                }

                if (!findPrimaryKey)
                {
                    throw new Exception($"Can't find any primary key in table {dataTable.TableName}");
                }

                // 添加分表数据
                excelDataList.Add(new ExcelSheetData
                {
                    Name = ToUpperFirstCharacter(dataTable.TableName),
                    PrimaryKeyFieldName = ToUpperFirstCharacter(primaryKeyFieldName),
                    PrimaryKeyDataType = primaryKeyDataType,
                    ColumnDataList = columnDataList,
                });
            }

            // 检查分表数据
            var tableNameSet = new HashSet<string>();
            foreach (var excelSheetData in excelDataList)
            {
                if (tableNameSet.Contains(excelSheetData.Name))
                {
                    throw new Exception($"The table name ({excelSheetData.Name}) is duplicate");
                }

                tableNameSet.Add(excelSheetData.Name);
            }

            return excelDataList;

            List<ExcelRowData> GetExcelRowData(DataTable dataTable)
            {
                var rowDataList = new List<ExcelRowData>();
                for (var rowId = 0; rowId < dataTable.Rows.Count; rowId++)
                {
                    var dataTableRow = dataTable.Rows[rowId];
                    var rowValueList = new List<string>();
                    for (var columnId = 0; columnId < dataTable.Columns.Count; columnId++)
                    {
                        rowValueList.Add(dataTableRow[columnId].ToString());
                    }

                    rowDataList.Add(new ExcelRowData()
                    {
                        RowId = rowId,
                        ValueList = rowValueList,
                    });
                }

                return rowDataList;
            }

            List<ExcelColumnData> RowDataList2ColumnDataList(string tableName, List<ExcelRowData> rowDataList)
            {
                var columnDataList = new List<ExcelColumnData>();
                var fieldNameRowData = rowDataList[FieldNameRowNumber];
                var dataTypeRowData = rowDataList[DataTypeRowNumber];
                var primaryKeyRowData = rowDataList[PrimaryKeyRowNumber];
                // 以字段声明行的列数为基准遍历
                for (var columnId = 0; columnId < rowDataList[FieldNameRowNumber].ValueList.Count; columnId++)
                {
                    var columnData = new ExcelColumnData
                    {
                        ColumnId = columnId,
                        FieldName = ToUpperFirstCharacter(fieldNameRowData.ValueList[columnId]),
                        DataType = dataTypeRowData.ValueList[columnId].Trim()
                    };
                    // 检查字段类型
                    if (!AllowedDataType.Contains(columnData.DataType))
                    {
                        throw new ArgumentException(
                            $"The type ({columnData.DataType}) of the field ({columnData.FieldName}) in table {tableName} is not allowed");
                    }

                    columnData.PrimaryKey = primaryKeyRowData.ValueList[columnId].Contains("primary key");
                    var valueList = new List<string>();
                    for (var rowId = InputDataStartRowNumber; rowId < rowDataList.Count; rowId++)
                    {
                        var value = rowDataList[rowId].ValueList[columnId];
                        valueList.Add(value);
                    }

                    columnData.ValueList = valueList;
                    columnDataList.Add(columnData);
                }

                return columnDataList;
            }
        }

        private static string ToUpperFirstCharacter(string input)
        {
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }
}