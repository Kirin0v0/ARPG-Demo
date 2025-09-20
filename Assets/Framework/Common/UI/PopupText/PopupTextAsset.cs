using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Framework.Common.UI.PopupText
{
    [CreateAssetMenu(menuName = "Popup Text/Asset")]
    public class PopupTextAsset : ScriptableObject
    {
        public TMP_FontAsset font;
        public Color fontColor;
        public float fontSize;

        public List<PopupTextProcess> processes; // 按顺序执行不同流程的动画曲线，同一流程内的动画曲线同时执行

        public float Duration
        {
            get
            {
                if (processes.Count == 0)
                {
                    return 0f;
                }

                return processes.Sum(process => process.Duration);
            }
        }

        public List<PopupTextTimePoint> Evaluate(float time)
        {
            time = Mathf.Clamp(time, 0f, Duration);
            var sum = 0f;
            foreach (var process in processes)
            {
                if (time >= sum && time < sum + process.Duration)
                {
                    return EvaluateProcess(process, time - sum);
                }

                sum += process.Duration;
            }

            return new List<PopupTextTimePoint>();
        }

        private List<PopupTextTimePoint> EvaluateProcess(PopupTextProcess process, float time)
        {
            var timePoints = new List<PopupTextTimePoint>();
            process.curves.ForEach(popupTextCurve =>
            {
                timePoints.Add(new PopupTextTimePoint
                {
                    Type = popupTextCurve.type,
                    Value = popupTextCurve.curve.Evaluate(time)
                });
            });
            return timePoints;
        }
    }
}