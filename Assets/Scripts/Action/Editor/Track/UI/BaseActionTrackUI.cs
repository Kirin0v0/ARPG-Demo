using Action.Editor.Channel.UI;
using Action.Editor.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Track.UI
{
    public abstract class BaseActionTrackUI : IActionTrackUI
    {
        public VisualElement Root { get; private set; }
        public VisualElement Parent { get; private set;}
        public ActionTrackEditorUIData Data { get;private set; }
        
        protected virtual string TrackTemplateAssetPath => "";

        public virtual void CreateView(VisualElement parent)
        {
            Parent = parent;
            Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackTemplateAssetPath).CloneTree().Query().ToList()[1];
            parent.Add(Root);
        }

        public virtual void BindView(ActionTrackEditorUIData data)
        {
            Data = data;
        }

        public virtual void DestroyView()
        {
            Parent?.Remove(Root);
            Parent = null;
        }

        public abstract void RefreshView();
    }
}