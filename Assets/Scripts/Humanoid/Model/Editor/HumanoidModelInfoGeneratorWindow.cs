using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanoid.Model.Data;
using Humanoid.Model.Extension;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

namespace Humanoid.Model.Editor
{
    /// <summary>
    /// 将指定物体上的模型信息导入到对应的Excel文件中
    /// </summary>
    public class HumanoidModelInfoGeneratorWindow : EditorWindow
    {
        private bool _init = false;
        private Vector2 _scrollPosition;

        // 读取相关
        private GameObject _modelPrefab;

        // 导出相关
        private string _exportGearExcelPath;
        private string _exportSheetName;
        private int _exportStartRowNumber;

        [MenuItem("Tools/Game/Model Info Generator")]
        public static void ShowModelInfoGeneratorWindow()
        {
            var window = GetWindow<HumanoidModelInfoGeneratorWindow>("Model Info Generator");
        }

        private void OnEnable()
        {
            if (_init)
            {
                return;
            }

            _init = true;
            _scrollPosition = Vector2.zero;

            _modelPrefab = null;
            _exportGearExcelPath = $"{Application.dataPath}/Configurations/";
            _exportSheetName = "HumanoidModelInfo";
            _exportStartRowNumber = 5;
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 100f;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            ShowImportArea();
            EditorGUILayout.Separator();
            ShowExportArea();

            EditorGUILayout.EndScrollView();
        }

        private void ShowImportArea()
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

            _modelPrefab =
                EditorGUILayout.ObjectField("导入模型预设体", _modelPrefab, typeof(GameObject),
                    true) as GameObject;
        }

        private void ShowExportArea()
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出至Model Excel文件");
            EditorGUILayout.TextField(_exportGearExcelPath);
            if (GUILayout.Button("选择文件"))
            {
                _exportGearExcelPath =
                    EditorUtility.OpenFilePanel("选择Excel文件", _exportGearExcelPath, "xls,xlsx");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _exportSheetName = EditorGUILayout.TextField("Excel分页名称", _exportSheetName);
            _exportStartRowNumber = EditorGUILayout.IntField("起始行号", _exportStartRowNumber);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("导出"))
            {
                ExportToExcelFile();
            }

            void ExportToExcelFile()
            {
                var modelLoader = new HumanoidModelLoader(_modelPrefab);
                var models = modelLoader.GetModels(false);
                var modelInfos = new List<HumanoidModelInfoData>();
                for (var i = 0; i < models.Count; i++)
                {
                    modelInfos.Add(models[i].modelRecord.ToModelInfoData(i));
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(_exportGearExcelPath));

                // 查找或创建sheet
                var workbookWorksheet = package.Workbook.Worksheets[_exportSheetName];
                if (workbookWorksheet == null)
                {
                    workbookWorksheet = package.Workbook.Worksheets.Add(_exportSheetName);
                }

                // 写入数据
                for (var i = 0; i < modelInfos.Count; i++)
                {
                    var row = _exportStartRowNumber + i;
                    var modelInfo = modelInfos[i];
                    workbookWorksheet.Cells[row, 1].Value = modelInfo.Id;
                    workbookWorksheet.Cells[row, 2].Value = modelInfo.Type;
                    workbookWorksheet.Cells[row, 3].Value = modelInfo.Part;
                    workbookWorksheet.Cells[row, 4].Value = modelInfo.GenderRestriction;
                    workbookWorksheet.Cells[row, 5].Value = modelInfo.Name;
                }

                // 保存数据
                package.Save();

                EditorUtility.DisplayDialog("导出结果", "成功！！！", "好的");
            }
        }
    }
}