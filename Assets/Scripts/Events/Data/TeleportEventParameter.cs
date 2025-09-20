using UnityEngine;

namespace Events.Data
{
    public struct TeleportEventParameter
    {
        public int MapId;
        public Vector3 Position;
        public float ForwardAngle;
    }
}