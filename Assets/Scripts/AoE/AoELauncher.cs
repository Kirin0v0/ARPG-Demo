using System.Collections.Generic;
using AoE.Data;
using Character;
using UnityEngine;
using VContainer;

namespace AoE
{
    public class AoELauncher
    {
        public const string Debug = "debug";

        private readonly GameObject _prefab; // AoE外观预设体
        private readonly float _prefabSimulationSpeed; // AoE外观模拟速度
        private readonly Vector3 _prefabLocalPosition; // AoE外观本地位移值
        private readonly Quaternion _prefabLocalRotation; // AoE外观本地旋转值
        private readonly CharacterObject _caster; // AoE施放者
        private readonly Vector3 _castPosition; // AoE施放位置
        private readonly bool _fixedPositionAndRotation; // AoE固定位置和旋转，如果是则位置和旋转始终保持创建位置和旋转
        private readonly float _duration; // AoE时长
        private readonly float _destroyDelay; // AoE在逻辑销毁后的实际游戏对象销毁延迟时间，单位是秒
        private readonly Dictionary<string, object> _runtimeParams; // AoE运行时参数

        public AoELauncher(
            GameObject prefab,
            float prefabSimulationSpeed,
            Vector3 prefabLocalPosition,
            Quaternion prefabLocalRotation,
            CharacterObject caster,
            Vector3 castPosition,
            bool fixedPositionAndRotation,
            float duration,
            float destroyDelay,
            Dictionary<string, object> runtimeParams = null
        )
        {
            _prefab = prefab;
            _prefabSimulationSpeed = prefabSimulationSpeed;
            _prefabLocalPosition = prefabLocalPosition;
            _prefabLocalRotation = prefabLocalRotation;
            _caster = caster;
            _castPosition = castPosition;
            _fixedPositionAndRotation = fixedPositionAndRotation;
            _duration = duration;
            _destroyDelay = destroyDelay;
            _runtimeParams = runtimeParams ?? new Dictionary<string, object>();
        }

        public AoEObject Launch(IObjectResolver objectResolver, AoEInfo info, Transform parent)
        {
            var gameObject = new GameObject($"AoE(id={info.id})")
            {
                transform =
                {
                    parent = parent,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one,
                    position = _castPosition
                },
                tag = "AoE",
            };
            var aoe = gameObject.AddComponent<AoEObject>();
            objectResolver.Inject(aoe);
            aoe.info = info;
            aoe.prefab = _prefab;
            aoe.prefabSimulationSpeed = _prefabSimulationSpeed;
            aoe.prefabLocalPosition = _prefabLocalPosition;
            aoe.prefabLocalRotation = _prefabLocalRotation;
            aoe.caster = _caster;
            aoe.fixedPositionAndRotation = _fixedPositionAndRotation;
            aoe.duration = _duration;
            aoe.destroyDelay = _destroyDelay;
            aoe.RuntimeParams = _runtimeParams;
            return aoe;
        }
    }
}