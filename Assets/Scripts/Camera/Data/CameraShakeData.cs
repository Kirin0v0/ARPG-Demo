using System;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Camera.Data
{
    [Serializable]
    public abstract class BaseCameraShakeData
    {
        /// <summary>
        /// 产生相机震动的函数
        /// </summary>
        /// <param name="shakePosition">震动发生的位置</param>
        public abstract void GenerateShake(Vector3 shakePosition);
    }

    public enum CameraShakeShape
    {
        Custom,
        Recoil,
        Bump,
        Explosion,
        Rumble,
    }

    [Serializable]
    public class CameraShakeUniformData : BaseCameraShakeData
    {
        [LabelText("震动形状")] public CameraShakeShape shape = CameraShakeShape.Recoil;
        [LabelText("震动时间"), MinValue(0f)] public float duration = 0.2f;

        [LabelText("是否采用伤害方向作为震动速度向量，是则需要调用设置速度向量函数")]
        public bool useDamageDirectionAsVelocity;

        [LabelText("震动默认速度向量")] [HideIf("useDamageDirectionAsVelocity")]
        public Vector3 defaultVelocity = Vector3.zero;

        [LabelText("震动力度")] public float force = 1f;

        [NonSerialized] private Vector3 _runtimeVelocity = Vector3.zero;

        public void SetVelocity(Vector3 velocity)
        {
            if (useDamageDirectionAsVelocity)
            {
                _runtimeVelocity = velocity;
            }
            else
            {
                defaultVelocity = velocity;
            }
        }

        public override void GenerateShake(Vector3 shakePosition)
        {
            var impulseDefinition = new CinemachineImpulseDefinition()
            {
                m_ImpulseChannel = 1,
                m_ImpulseShape = shape switch
                {
                    CameraShakeShape.Custom => CinemachineImpulseDefinition.ImpulseShapes.Custom,
                    CameraShakeShape.Recoil => CinemachineImpulseDefinition.ImpulseShapes.Recoil,
                    CameraShakeShape.Bump => CinemachineImpulseDefinition.ImpulseShapes.Bump,
                    CameraShakeShape.Explosion => CinemachineImpulseDefinition.ImpulseShapes.Explosion,
                    CameraShakeShape.Rumble => CinemachineImpulseDefinition.ImpulseShapes.Rumble,
                    _ => CinemachineImpulseDefinition.ImpulseShapes.Custom,
                },
                m_CustomImpulseShape = new AnimationCurve(),
                m_ImpulseDuration = duration,
                m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                m_DissipationDistance = 100,
                m_DissipationRate = 0.25f,
                m_PropagationSpeed = 343
            };
            impulseDefinition.CreateEvent(shakePosition,
                (useDamageDirectionAsVelocity ? _runtimeVelocity : defaultVelocity) * force);
        }
    }
}