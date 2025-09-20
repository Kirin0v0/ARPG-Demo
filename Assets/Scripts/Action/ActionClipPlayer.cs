using System;
using System.Collections.Generic;
using Animancer;
using Character;
using Character.Ability;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Humanoid;
using Sirenix.Utilities;
using UnityEngine;

namespace Action
{
    public enum ActionStage
    {
        Idle,
        Start,
        Anticipation,
        Judgment,
        Recovery,
        End,
        Stop,
    }

    public class ActionClipPlayer : IActionClipPlay
    {
        public event System.Action OnStartStage;
        public event System.Action OnAnticipationStage;
        public event System.Action OnJudgmentStage;
        public event System.Action OnRecoveryStage;
        public event System.Action OnEndStage;
        public event System.Action OnStopStage;

        private readonly ActionClip _actionClip;
        private readonly CharacterObject _character;
        private readonly float _colliderDetectionInterval;
        private readonly int _colliderDetectionCount;
        private readonly int _colliderLayerMask;
        private readonly Action<string, Collider> _colliderDetectionDelegate;
        private readonly bool _loop;
        private readonly float _tickDeltaTime;

        private float _time;
        private int _tick;

        // 动画参数
        private ActionAnimationClipData _playingAnimation;
        private AnimancerState _playingAnimancerState;

        // 音效参数
        private readonly Dictionary<ActionAudioClipData, int> _playingAudioClips = new();

        // 特效参数
        private readonly Dictionary<ActionEffectClipData, string> _playingEffectClips = new();

        // 碰撞检测参数
        private readonly Dictionary<ActionCollideDetectionClipData, ActionCollideDetectionDelegate>
            _playingCollideDetectionClips = new();

        // 事件参数
        private readonly List<System.Action<string, ActionEventParameter, object>> _eventListeners = new();

        // 动作阶段，默认情况下只能前进不能后退，在循环时将跳过结束阶段重新开始
        private ActionStage _stage = ActionStage.Idle;

        public ActionStage Stage
        {
            private set
            {
                if (_stage >= value)
                {
                    if (!_loop || value != ActionStage.Start)
                    {
                        return;
                    }
                }

                _stage = value;
                switch (value)
                {
                    case ActionStage.Start:
                        StartInternal();
                        OnStartStage?.Invoke();
                        break;
                    case ActionStage.Anticipation:
                        OnAnticipationStage?.Invoke();
                        break;
                    case ActionStage.Judgment:
                        OnJudgmentStage?.Invoke();
                        break;
                    case ActionStage.Recovery:
                        OnRecoveryStage?.Invoke();
                        break;
                    case ActionStage.End:
                        EndAndStopInternal();
                        OnEndStage?.Invoke();
                        break;
                    case ActionStage.Stop:
                        EndAndStopInternal();
                        OnStopStage?.Invoke();
                        break;
                }
            }
            get => _stage;
        }

        public int CurrentTick => _tick;
        public float CurrentTime => _time;

        public ActionClipPlayer(
            ActionClip actionClip,
            CharacterObject character,
            int colliderLayerMask,
            Action<string, Collider> colliderDetectionDelegate,
            bool loop = false
        )
        {
            _actionClip = actionClip;
            _character = character;
            _colliderLayerMask = colliderLayerMask;
            _colliderDetectionDelegate = colliderDetectionDelegate;
            _loop = loop;
            _tickDeltaTime = 1f / _actionClip.frameRate;
        }

        public void Start()
        {
            if (!_loop && Stage >= ActionStage.Start)
            {
                return;
            }

            Stage = ActionStage.Start;

            CheckTimelineData();
        }

        /// <summary>
        /// 帧函数，由于存在动作循环（即到结束阶段时直接切换到开始阶段），所以采用递归写法实现帧追赶
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Tick(float deltaTime)
        {
            // 计算当前帧序号
            var newestTick = GetTick(_time + deltaTime);
            // 在执行帧函数前检测动作阶段，非循环动作或被外部停止则屏蔽结束阶段后续逻辑
            if (Stage >= ActionStage.End)
            {
                if (!_loop || Stage != ActionStage.End)
                {
                    return;
                }
            }

            // 判断当前帧是否与先前帧序号相同，若不同则执行新帧数据并递归执行帧函数，否则仅增加时间
            if (_tick != newestTick)
            {
                // 每次仅增加一个帧，并计算后续的间隔时间，交由帧函数递归执行
                _tick++;
                var newDeltaTime = _time + deltaTime - GetTickTime(_tick);
                // 如果当前是结束阶段，就调用开始函数重置帧，否则就执行新帧数据
                if (Stage == ActionStage.End)
                {
                    Start();
                }
                else
                {
                    _time = GetTickTime(_tick);
                    CheckTimelineData();
                }

                // 递归执行帧函数
                Tick(newDeltaTime);
            }
            else
            {
                _time += deltaTime;
            }
        }

        public void Stop()
        {
            if (Stage != ActionStage.End)
            {
                Stage = ActionStage.Stop;
            }
        }

        public void RegisterEventListener(Action<string, ActionEventParameter, object> listener)
        {
            _eventListeners.Add(listener);
        }

        public void UnregisterEventListener(Action<string, ActionEventParameter, object> listener)
        {
            _eventListeners.Remove(listener);
        }

#if UNITY_EDITOR
        public void ChangeActionClip(ActionClip actionClip)
        {
            throw new Exception("Don't support this method");
        }

        public void StartAt(int tick)
        {
            throw new Exception("Don't support this method");
        }

        public void PlayAt(int tick)
        {
            throw new Exception("Don't support this method");
        }
#endif

        private void StartInternal()
        {
            ClearData();

            _time = 0f;
            _tick = 0;
            if (_actionClip)
            {
                _character?.AnimationAbility?.SwitchTransitionLibrary(_actionClip.animation.transitionLibrary);
            }
        }

        private void EndAndStopInternal()
        {
            if (_playingAnimancerState != null)
            {
                _character.AnimationAbility?.StopAction(_playingAnimancerState);
            }

            ClearData();
        }

        private void ClearData()
        {
            // 清除动画数据
            _playingAnimation = null;
            _playingAnimancerState = null;

            // 清除音频数据
            _playingAudioClips.ForEach((kv) => { _character.AudioAbility?.StopSound(kv.Value); });
            _playingAudioClips.Clear();

            // 清除特效数据
            _playingEffectClips.ForEach((kv) => { _character.EffectAbility?.RemoveEffect(kv.Value); });
            _playingEffectClips.Clear();

            // 清除碰撞检测数据
            _playingCollideDetectionClips.ForEach((kv) => kv.Value.Destroy());
            _playingCollideDetectionClips.Clear();
        }

        private void CheckTimelineData()
        {
            if (!_character || _character.Parameters.dead)
            {
                Stop();
                return;
            }

            if (!_actionClip)
            {
                return;
            }

            CheckProcess();
            CheckAnimation();
            CheckAudio();
            CheckEffect();
            CheckCollideDetection();
            CheckEvent();

            if (_tick >= _actionClip.totalTicks)
            {
                Stage = ActionStage.End;
            }

            DebugUtil.LogCyan(
                $"角色({_character.Parameters.DebugName})已播放动作({_actionClip.name}), 当前帧: {_tick}, 时间: {_time}, 阶段: {Stage}");
        }

        private void CheckProcess()
        {
            if (_tick >= _actionClip.process.anticipationTick)
            {
                Stage = ActionStage.Anticipation;
            }

            if (_tick >= _actionClip.process.judgmentTick)
            {
                Stage = ActionStage.Judgment;
            }

            if (_tick >= _actionClip.process.recoveryTick)
            {
                Stage = ActionStage.Recovery;
            }
        }

        private void CheckAnimation()
        {
            if (!_character || !_character.AnimationAbility)
            {
                return;
            }

            // 检查能否切换并播放动画
            foreach (var animationClip in _actionClip.animation.animationClips)
            {
                var startTick = animationClip.startTick;
                if (_tick >= startTick &&
                    (_playingAnimation == null || animationClip.startTick > _playingAnimation.startTick))
                {
                    _playingAnimation = animationClip;
                    _playingAnimancerState = _character.AnimationAbility?.PlayAction(animationClip.transition, true);
                    if (_playingAnimancerState != null)
                    {
                        _playingAnimancerState.Speed *= animationClip.speed;
                    }
                }
            }
        }

        private void CheckAudio()
        {
            if (!_character.AudioAbility)
            {
                return;
            }

            foreach (var audioClip in _actionClip.audio.audioClips)
            {
                var startTick = audioClip.startTick;
                var endTick = audioClip.startTick + audioClip.durationTicks;
                if (_tick >= startTick && _tick <= endTick)
                {
                    if (!_playingAudioClips.ContainsKey(audioClip))
                    {
                        var id = _character.AudioAbility.PlaySound(audioClip.AudioClip, false, audioClip.volume);
                        _playingAudioClips.Add(audioClip, id);
                    }
                }

                if (_tick >= endTick && _playingAudioClips.TryGetValue(audioClip, out var id1))
                {
                    _character.AudioAbility.StopSound(id1);
                    _playingAudioClips.Remove(audioClip);
                }
            }
        }

        private void CheckEffect()
        {
            if (!_character.EffectAbility)
            {
                return;
            }

            foreach (var effectClip in _actionClip.effect.effectClips)
            {
                var startTick = effectClip.startTick;
                var endTick = effectClip.startTick + effectClip.durationTicks;
                if (_tick >= startTick && _tick <= endTick)
                {
                    if (!_playingEffectClips.ContainsKey(effectClip))
                    {
                        var id = _character.EffectAbility.AddEffect(
                            prefab: effectClip.prefab,
                            localPosition: effectClip.localPosition,
                            localRotation: effectClip.localRotation,
                            localScale: effectClip.localScale,
                            position: CharacterEffectPosition.Bottom,
                            startTime: effectClip.startLifetime,
                            timeFixed: effectClip.type == ActionEffectType.Fixed,
                            duration: float.MaxValue
                        );
                        _playingEffectClips.Add(effectClip, id);
                    }
                }

                if (_tick >= endTick && _playingEffectClips.TryGetValue(effectClip, out var id1))
                {
                    _character.EffectAbility.RemoveEffect(id1);
                    _playingEffectClips.Remove(effectClip);
                }
            }
        }

        private void CheckCollideDetection()
        {
            foreach (var collideDetectionClip in _actionClip.collideDetection.collideDetectionClips)
            {
                var startTick = collideDetectionClip.startTick;
                var endTick = collideDetectionClip.startTick + collideDetectionClip.durationTicks;
                if (_tick >= startTick && _tick <= endTick)
                {
                    if (!_playingCollideDetectionClips.TryGetValue(collideDetectionClip,
                            out var collideDetectionDelegate))
                    {
                        GameObject owner;
                        if (collideDetectionClip.type == ActionCollideDetectionType.Bind)
                        {
                            if (_character is HumanoidCharacterObject humanoidCharacterObject)
                            {
                                owner = humanoidCharacterObject.WeaponAbility
                                            ?.AggressiveWeaponSlot?.Object?.WeaponCollider.gameObject ??
                                        _character.AttackBoxCollider?.gameObject;
                            }
                            else
                            {
                                owner = _character.AttackBoxCollider?.gameObject;
                            }

                            owner ??= _character.gameObject;
                        }
                        else
                        {
                            owner = _character.gameObject;
                        }

                        var @delegate = new ActionCollideDetectionDelegate(
                            _character.transform,
                            owner,
                            collideDetectionClip.data,
                            _colliderLayerMask
                        );
                        @delegate.Init(
                            collider => { _colliderDetectionDelegate?.Invoke(collideDetectionClip.groupId, collider); },
                            false
                        );
                        _playingCollideDetectionClips.Add(collideDetectionClip, @delegate);
                        collideDetectionDelegate = @delegate;
                    }

                    collideDetectionDelegate.Tick(collider =>
                    {
                        _colliderDetectionDelegate?.Invoke(collideDetectionClip.groupId, collider);
                    });
                }

                if (_tick >= endTick && _playingCollideDetectionClips.TryGetValue(collideDetectionClip,
                        out var collideDetection))
                {
                    collideDetection.Destroy();
                    _playingCollideDetectionClips.Remove(collideDetectionClip);
                }
            }
        }

        private void CheckEvent()
        {
            foreach (var eventClip in _actionClip.events.eventClips)
            {
                if (_tick == eventClip.tick)
                {
                    _eventListeners.ForEach(listener =>
                    {
                        switch (eventClip.parameter)
                        {
                            case ActionEventParameter.None:
                            {
                                listener?.Invoke(eventClip.name, eventClip.parameter, null);
                            }
                                break;
                            case ActionEventParameter.Bool:
                            {
                                listener?.Invoke(eventClip.name, eventClip.parameter, eventClip.boolPayload);
                            }
                                break;
                            case ActionEventParameter.Int:
                            {
                                listener?.Invoke(eventClip.name, eventClip.parameter, eventClip.intPayload);
                            }
                                break;
                            case ActionEventParameter.Float:
                            {
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.floatPayload);
                            }
                                break;
                            case ActionEventParameter.String:
                            {
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.stringPayload);
                            }
                                break;
                            case ActionEventParameter.UnityObject:
                            {
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.objectPayload);
                            }
                                break;
                        }
                    });
                }
            }
        }

        private int GetTick(float time)
        {
            return (int)(time / _tickDeltaTime);
        }

        private float GetTickTime(int tick)
        {
            return tick * _tickDeltaTime;
        }
    }
}