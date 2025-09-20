using System.Collections.Generic;
using Combo.Graph.Unit;
using Framework.Common.Editor.Extension;
using UnityEngine;

namespace Combo.Graph.Editor.GUI
{
    public class ComboGraphLayer
    {
        private Matrix4x4 _transformMatrix = Matrix4x4.identity;
        protected Rect Position;
        public readonly ComboGraphContext Context;

        public ComboGraphLayer(ComboGraphContext context)
        {
            Context = context;
        }

        public virtual void DrawGUI(Rect rect)
        {
            Position = rect;
            UpdateTranslateMatrix();
        }

        public virtual void ConsumeEvent()
        {
        }

        public virtual void Update()
        {
        }

        protected void UpdateTranslateMatrix()
        {
            var centerMat = Matrix4x4.Translate(-Position.center);
            var translationMat =
                Matrix4x4.Translate(Context.DragOffset / Context.ZoomFactor);
            var scaleMat = Matrix4x4.Scale(Vector3.one * Context.ZoomFactor);
            _transformMatrix = centerMat.inverse * scaleMat * translationMat * centerMat;
        }

        protected Rect GetTransformRect(Rect rect)
        {
            return new Rect
            {
                position = _transformMatrix.MultiplyPoint(rect.position),
                size = _transformMatrix.MultiplyVector(rect.size)
            };
        }

        protected Vector2 GetMousePosition(Vector2 mousePosition)
        {
            Vector2 center = mousePosition + (mousePosition - this.Position.center) *
                (1 - Context.ZoomFactor) / Context.ZoomFactor;
            center -= Context.DragOffset / Context.ZoomFactor;
            return center;
        }
    }
}