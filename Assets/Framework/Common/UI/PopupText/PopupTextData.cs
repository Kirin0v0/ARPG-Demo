using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework.Common.UI.PopupText
{
    [Serializable]
    public class PopupTextProcess
    {
        public List<PopupTextCurve> curves;

        public float Duration
        {
            get
            {
                if (curves.Count == 0)
                {
                    return 0f;
                }

                return curves.Max(popupTextCurve =>
                    popupTextCurve.curve.keys[popupTextCurve.curve.length - 1].time);
            }
        }
    }

    public enum PopupTextCurveType
    {
        Scale,
        VerticalMovement,
        HorizontalMovement,
        Alpha,
    }

    [Serializable]
    public class PopupTextCurve
    {
        public AnimationCurve curve;
        public PopupTextCurveType type;
    }

    public struct PopupTextTimePoint
    {
        public PopupTextCurveType Type;
        public float Value;
    }
}