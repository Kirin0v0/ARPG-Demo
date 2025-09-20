using System;
using UnityEngine;

namespace Framework.DataStructure
{
    [Serializable]
    public class SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3()
        {
        }
        
        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SerializableVector3(Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}