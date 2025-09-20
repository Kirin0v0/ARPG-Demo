using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Framework.Common.UI.RecyclerView;

namespace Framework.Common.UI.Editor
{
    public class UICreator : UnityEditor.Editor
    {
        [MenuItem("GameObject/UI/Recycler View")]
        private static void CreateRecyclerView()
        {
            var parent = FindParent();

            var recyclerViewObject = new GameObject("RecyclerView");
            recyclerViewObject.transform.SetParent(parent, false);
            recyclerViewObject.AddComponent<Image>();
            recyclerViewObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 500);
            recyclerViewObject.AddComponent<RecyclerView.RecyclerView>();

            Selection.activeGameObject = recyclerViewObject;
        }

        private static RectTransform FindParent()
        {
            var selectedObj = Selection.activeGameObject;
            if (selectedObj != null)
            {
                if (selectedObj.TryGetComponent(out RectTransform transform))
                {
                    return transform;
                }
                else
                {
                    transform = CreateCanvas().GetComponent<RectTransform>();
                    transform.SetParent(selectedObj.transform, false);
                    return transform;
                }
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            return canvas.GetComponent<RectTransform>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<EventSystem>();
                eventObj.AddComponent<StandaloneInputModule>();
            }

            return canvas;
        }
    }
}