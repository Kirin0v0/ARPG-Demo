using System;
using UnityEngine;

namespace Framework.Common.Function
{
    public class IdGenerator : MonoBehaviour
    {
        // 为了保持id值在编辑和运行模式下不变，必须为公有变量，私有变量在运行时会为null没有写入编辑值
        public string id;

        private void Awake()
        {
            id = GenerateId();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = GenerateId();
            }
        }

        public string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}