using System;
using Framework.Common.Debug;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Map.Editor
{
    public class MapSnapshotEditorWindow : EditorWindow
    {
        // 编辑器场景
        private const string EditorScenePath = "Assets/Scripts/Map/Editor/MapSnapshotEditorScene.unity";

        private MapObject _mapPrefab;
        private string _folderPath;
        private MapObject _mapInstance;

        [MenuItem("Tools/Game/Map Snapshot Editor")]
        public static void ShowMapSnapshotEditorWindow()
        {
            var window = GetWindow<MapSnapshotEditorWindow>();
            window.titleContent = new GUIContent("Map Snapshot Editor");
        }

        private void CreateGUI()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            _mapPrefab = null;
            _folderPath = Application.dataPath;
            _mapInstance = null;
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        private void OnGUI()
        {
            // 获取地图预设体
            var target = (GameObject)EditorGUILayout.ObjectField("地图预设体", _mapPrefab?.gameObject ?? null,
                typeof(GameObject), false);
            if (target?.TryGetComponent(out MapObject mapObject) == true)
            {
                _mapPrefab = mapObject;
            }

            // 读取地图信息
            if (_mapPrefab)
            {
                var mapSerializedObject = new SerializedObject(_mapPrefab);
                var snapshotSerializedProperty = mapSerializedObject.FindProperty("snapshot");
                EditorGUILayout.PropertyField(snapshotSerializedProperty, new GUIContent("地图预设体快照信息"));
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("前往预设场景"))
            {
                HandleGotoScene();
            }

            if (_mapPrefab && GUILayout.Button("读取地图快照"))
            {
                HandleGotoSceneAndPreviewSnapshot();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("快照保存路径");
            EditorGUILayout.TextField(_folderPath);
            if (GUILayout.Button("选择路径"))
            {
                _folderPath = EditorUtility.OpenFolderPanel("选择保存路径", _folderPath, "");
            }

            EditorGUILayout.EndHorizontal();

            if (_mapInstance && GUILayout.Button("保存地图快照"))
            {
                HandleTakeSnapshot();
            }
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    var activeScene = SceneManager.GetActiveScene();
                    if (activeScene.path != EditorScenePath)
                    {
                        return;
                    }
                    
                    // 将主要灯光改为90度直射光，避免出现阴影
                    _mapInstance = GameObject.Instantiate(_mapPrefab.gameObject).GetComponent<MapObject>();
                    var mainLights = GameObject.FindGameObjectsWithTag("MainLight");
                    mainLights.ForEach(light =>
                    {
                        light.gameObject.transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
                    });
                    var camera = UnityEngine.Camera.main;
                    if (!camera)
                    {
                        return;
                    }

                    // 设置相机位置和正交尺寸
                    camera.transform.position = new Vector3(_mapInstance.Snapshot.Center3D.x,
                        _mapInstance.Snapshot.Center3D.y + 100f,
                        _mapInstance.Snapshot.Center3D.z);
                    camera.orthographicSize =
                        Mathf.Max(_mapInstance.Snapshot.Size.x / 2f, _mapInstance.Snapshot.Size.y / 2f);
                }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                {
                    _mapInstance = null;
                }
                    break;
            }
        }

        private void HandleGotoScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != EditorScenePath)
            {
                EditorSceneManager.OpenScene(EditorScenePath);
            }
        }

        private void HandleGotoSceneAndPreviewSnapshot()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != EditorScenePath)
            {
                EditorSceneManager.OpenScene(EditorScenePath);
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            EditorApplication.EnterPlaymode();
        }

        private void HandleTakeSnapshot()
        {
            var camera = UnityEngine.Camera.main;
            if (!camera || !_mapInstance)
            {
                return;
            }
            
            // 1.定义目标区域的世界坐标范围
            var worldMin = _mapInstance.Snapshot.LeftBottom3D;
            var worldMax = _mapInstance.Snapshot.RightTop3D;

            // 2.将世界坐标转换为视口坐标（归一化）
            var viewportMin = camera.WorldToViewportPoint(worldMin);
            var viewportMax = camera.WorldToViewportPoint(worldMax);

            // 3.计算相机输出像素范围（这里设定分辨率是16:9）
            var fullWidth = 3840;
            var fullHeight = 2160;
    
            var xMin = (int)(viewportMin.x * fullWidth);
            var yMin = (int)(viewportMin.y * fullHeight);
            var xMax = (int)(viewportMax.x * fullWidth);
            var yMax = (int)(viewportMax.y * fullHeight);

            // 4.确保截取区域在合法范围内
            xMin = Mathf.Clamp(xMin, 0, fullWidth);
            yMin = Mathf.Clamp(yMin, 0, fullHeight);
            xMax = Mathf.Clamp(xMax, 0, fullWidth);
            yMax = Mathf.Clamp(yMax, 0, fullHeight);
            var width = xMax - xMin;
            var height = yMax - yMin;

            // 5.创建RenderTexture并截取区域
            var renderTexture = new RenderTexture(fullWidth, fullHeight, 32);
            camera.targetTexture = renderTexture;
            camera.Render();

            // 指定读取的区域（从左上角开始）
            var screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(new Rect(xMin, fullHeight - yMax, width, height), 0, 0);
            screenShot.Apply();

            // 6.清理资源
            camera.targetTexture = null;
            RenderTexture.active = null;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);

            // 7.保存截图
            var bytes = screenShot.EncodeToPNG();
            System.IO.File.WriteAllBytes($"{_folderPath}/{_mapPrefab.name}.png", bytes);
            EditorUtility.DisplayDialog("保存结果", "成功截取指定区域！", "好的");
        }
    }
}