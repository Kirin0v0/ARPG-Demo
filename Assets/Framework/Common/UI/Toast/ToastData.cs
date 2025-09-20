using UnityEngine;

namespace Framework.Common.UI.Toast
{
    public enum ToastLocation
    {
        Top,
        Center,
        Bottom,
        Custom
    }
    
    public class ToastData
    {
        public string Text;
        public Color TextColor;
        public int TextSize;
        public float Duration;
        public bool RealTime;
        public ToastLocation Location;
        public Vector2 CustomCenter;
    }
}