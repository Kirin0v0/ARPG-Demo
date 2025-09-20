using System.Collections.Generic;
using Framework.Common.Function;
using Sirenix.Utilities;
using UnityEngine;

namespace Character.Ability
{
    public enum CharacterEffectPosition
    {
        Center,
        Top,
        Bottom,
    }

    public class CharacterEffectAbility : BaseCharacterOptionalAbility
    {
        private readonly Dictionary<string, EffectInfo> _playingEffects = new(); // 播放中的特效信息

        private readonly Dictionary<string, GameObject> _playedEffects = new(); // 记录特效的对象池，用于缓存已播放并结束的特效进行复用

        public void Tick(float deltaTime)
        {
            var toRemovedEffects = new List<string>();
            _playingEffects.ForEach(pair =>
            {
                pair.Value.Time += deltaTime;
                if (pair.Value.Effect.TryGetComponent<ParticleSystem>(out var particleSystem))
                {
                    particleSystem.Simulate(
                        pair.Value.TimeFixed
                            ? pair.Value.StartTime
                            : pair.Value.Time + pair.Value.StartTime,
                        true,
                        true
                    );
                    Debug.Log("特效时间: " + particleSystem.time);
                    particleSystem.Pause();
                }

                if (pair.Value.Time >= pair.Value.Duration)
                {
                    toRemovedEffects.Add(pair.Key);
                }
            });
            toRemovedEffects.ForEach(RemoveEffect);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _playingEffects.Values.ForEach(effectInfo => { GameObject.Destroy(effectInfo.Effect); });
            _playingEffects.Clear();
            _playedEffects.Values.ForEach(effect => { GameObject.Destroy(effect); });
            _playedEffects.Clear();
        }

        public string AddEffect(
            GameObject prefab,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            CharacterEffectPosition position = CharacterEffectPosition.Bottom,
            float startTime = 0f,
            bool timeFixed = false,
            float duration = float.MaxValue
        )
        {
            // 如果特效正在播放，这里就不去添加新特效
            if (_playingEffects.TryGetValue(prefab.name, out var effectInfo))
            {
                return prefab.name;
            }

            // 如果没有特效缓存，就创建新特效
            if (!_playedEffects.Remove(prefab.name, out var effect))
            {
                effect = GameObject.Instantiate(prefab, transform);
            }

            effect.gameObject.SetActive(true);
            effect.transform.position = position switch
            {
                CharacterEffectPosition.Center => Owner.Visual.TransformCenterPoint(localPosition),
                CharacterEffectPosition.Top => Owner.Visual.TransformTopPoint(localPosition),
                CharacterEffectPosition.Bottom => Owner.Visual.TransformBottomPoint(localPosition),
                _ => Owner.Visual.TransformBottomPoint(localPosition),
            };
            effect.transform.rotation = Owner.transform.rotation * localRotation;
            effect.transform.localScale = localScale;
            _playingEffects[prefab.name] = new EffectInfo
            {
                Effect = effect,
                Time = 0,
                StartTime = startTime,
                TimeFixed = timeFixed,
                Duration = duration
            };
            // 如果存在粒子系统就控制其时间
            if (effect.TryGetComponent<ParticleSystem>(out var particleSystem))
            {
                particleSystem.Simulate(startTime, true, true);
                particleSystem.Pause();
            }

            return prefab.name;
        }

        public void RemoveEffect(string id)
        {
            if (!_playingEffects.Remove(id, out var effectInfo))
            {
                return;
            }

            effectInfo.Effect.gameObject.SetActive(false);
            if (_playedEffects.TryGetValue(id, out var effect))
            {
                GameObject.Destroy(effect);
            }

            _playedEffects.Add(id, effectInfo.Effect);
        }

        private class EffectInfo
        {
            public GameObject Effect;
            public float Time;
            public float StartTime;
            public bool TimeFixed;
            public float Duration;
        }
    }
}