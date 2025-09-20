using Framework.Common.Debug;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Framework.Common.Editor.UI
{
    public class TwoPanelSpiltView : TwoPaneSplitView
    {
        public new class UXmlFactory : UxmlFactory<TwoPanelSpiltView, TwoPaneSplitView.UxmlTraits>
        {
        }
    }
}