using System;
using UnityEngine;

namespace Common
{
    public enum FrameRateLimitType
    {
        NoLimit = -1,
        Limit30 = 30,
        Limit60 = 60,
    }

    public class LimitFrameRateBehaviour : MonoBehaviour
    {
        public FrameRateLimitType limitType = FrameRateLimitType.NoLimit;

        private void Awake()
        {
            Application.targetFrameRate = (int)limitType;
        }
    }
}