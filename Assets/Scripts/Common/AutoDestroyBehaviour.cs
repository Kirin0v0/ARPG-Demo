using System;
using UnityEngine;

namespace Common
{
    public class AutoDestroyBehaviour: MonoBehaviour
    {
        public float destroyTime;

        private float _time;

        private void OnEnable()
        {
            _time = 0f;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_time >= destroyTime)
            {
                GameObject.Destroy(gameObject);
            }
        }
    }
}