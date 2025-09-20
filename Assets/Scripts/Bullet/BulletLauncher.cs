using System;
using System.Collections.Generic;
using Bullet.Data;
using Character;
using UnityEngine;
using VContainer;

namespace Bullet
{
    public class BulletLauncher
    {
        public const string Debug = "debug";

        private readonly GameObject _prefab; // 子弹外观预设体
        private readonly Vector3 _prefabLocalPosition; // 子弹外观本地位移值
        private readonly Quaternion _prefabLocalRotation; // 子弹外观本地旋转值
        private readonly CharacterObject _caster; // 子弹施放者
        private readonly Vector3 _firePosition; // 子弹开火位置
        private readonly Quaternion _fireRotation; // 子弹开火旋转量
        private readonly float _fireSpeed; // 子弹开火初速度
        private readonly float _duration; // 子弹时长，到时后销毁
        private readonly BulletTargetFunction _targetFunction; // 子弹目标函数
        private readonly BulletLocomotionFunction _locomotionFunction; // 子弹移动函数，用于控制子弹移动轨迹
        private readonly float _allowHitDelay; // 子弹在创建后允许命中的延迟时间，默认为0，少数情况如子母弹等需要在一定时间后才允许命中
        private readonly float _destroyDelay; // 子弹在逻辑销毁后的实际游戏对象销毁延迟时间，默认为0
        private readonly Dictionary<string, object> _runtimeParams; // 子弹运行时参数

        public BulletLauncher(
            GameObject prefab,
            Vector3 prefabLocalPosition,
            Quaternion prefabLocalRotation,
            CharacterObject caster,
            Vector3 firePosition,
            Quaternion fireRotation,
            float fireSpeed,
            float duration,
            BulletTargetFunction targetFunction,
            BulletLocomotionFunction locomotionFunction,
            float allowHitDelay = 0f,
            float destroyDelay = 0f,
            Dictionary<string, object> runtimeParams = null
        )
        {
            _prefab = prefab;
            _prefabLocalPosition = prefabLocalPosition;
            _prefabLocalRotation = prefabLocalRotation;
            _caster = caster;
            _firePosition = firePosition;
            _fireRotation = fireRotation;
            _fireSpeed = fireSpeed;
            _duration = duration;
            _targetFunction = targetFunction;
            _locomotionFunction = locomotionFunction;
            _allowHitDelay = allowHitDelay;
            _destroyDelay = destroyDelay;
            _runtimeParams = runtimeParams ?? new Dictionary<string, object>();
        }

        public BulletObject Launch(IObjectResolver objectResolver, BulletInfo info, Transform parent)
        {
            var gameObject = new GameObject($"Bullet(id={info.id})")
            {
                transform =
                {
                    parent = parent,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one,
                },
                tag = "Bullet",
            };
            var bullet = gameObject.AddComponent<BulletObject>();
            objectResolver.Inject(bullet);
            bullet.info = info;
            bullet.prefab = _prefab;
            bullet.prefabLocalPosition = _prefabLocalPosition;
            bullet.prefabLocalRotation = _prefabLocalRotation;
            bullet.caster = _caster;
            bullet.firePosition = _firePosition;
            bullet.fireRotation = _fireRotation;
            bullet.speed = _fireSpeed;
            bullet.duration = _duration;
            bullet.TargetFunction = _targetFunction;
            bullet.LocomotionFunction = _locomotionFunction;
            bullet.RuntimeParams = _runtimeParams;
            bullet.allowHitDelay = _allowHitDelay;
            bullet.destroyDelay = _destroyDelay;
            return bullet;
        }
    }

    public delegate CharacterObject BulletTargetFunction(BulletObject bullet, CharacterObject caster);

    public delegate void BulletLocomotionFunction(
        float timeElapsed,
        BulletObject bullet,
        CharacterObject target,
        float deltaTime
    );
}