using System;
using Framework.DataStructure;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.Data
{
    /// <summary>
    /// 角色地图存档数据类
    /// </summary>
    [Serializable]
    public class CharacterMapArchiveData
    {
        public int id = -1;
        public SerializableVector3 position = new(Vector3.zero);
        public float forwardAngle = 0f;
    }
}