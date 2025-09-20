using System;
using System.Collections.Generic;
using System.Reflection;
using Animancer;
using Framework.Common.Debug;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;

namespace Action.Editor
{
    public class ActionClipEditorPlayer : IActionClipPlay
    {
        private readonly AnimancerComponent _animancer;
        private readonly Transform _model;
        private ActionClip _actionClip;
        private GameObject _bindObject;

        // 动作阶段
        private ActionStage _stage = ActionStage.Idle;

        private float _time;
        private int _tick;
        private float TickDeltaTime => _actionClip ? 1f / _actionClip.frameRate : 1f / 60;

        // 动画参数
        private ActionAnimationClipData _playingAnimation;

        // 音效参数
        private readonly List<ActionAudioClipData> _playingAudioClips = new();
        private readonly MethodInfo _playAudioClipMethod;
        private readonly MethodInfo _stopAllAudioClipMethod;

        // 特效参数
        private readonly Dictionary<ActionEffectClipData, GameObject> _playingEffectClips = new();

        // 碰撞检测参数
        private readonly Dictionary<ActionCollideDetectionClipData, ActionCollideDetectionDelegate>
            _playingCollideDetectionClips = new();

        // 事件参数
        private readonly List<System.Action<string, ActionEventParameter, object>> _eventListeners = new();

        public int CurrentTick => _tick;
        public float CurrentTime => _time;

        public ActionClipEditorPlayer(AnimancerComponent animancer, Transform model)
        {
            _animancer = animancer;
            _model = model;
            // 反射获取编辑状态播放AudioClip API
            var assembly = typeof(UnityEditor.AudioImporter).Assembly;
            var type = assembly.GetType("UnityEditor.AudioUtil");
            _playAudioClipMethod = type.GetMethod("PlayPreviewClip", new Type[]
            {
                typeof(AudioClip), typeof(int), typeof(bool)
            });
            _stopAllAudioClipMethod =
                type.GetMethod("StopAllPreviewClips", new Type[] { });
        }

        public void Start()
        {
            StartInternal(0);
        }

        public void Tick(float deltaTime)
        {
            // 计算当前帧序号
            var newestTick = GetTick(_time + deltaTime);
            // 在执行帧函数前检测动作阶段，屏蔽结束阶段后续逻辑
            if (_stage >= ActionStage.End)
            {
                return;
            }

            // 判断当前帧是否与先前帧序号相同，若不同则执行新帧数据并递归执行帧函数，否则仅增加时间
            if (_tick != newestTick)
            {
                // 每次仅增加一个帧，并计算后续的间隔时间，交由帧函数递归执行
                _tick++;
                var newDeltaTime = _time + deltaTime - GetTickTime(_tick);
                // 执行新帧数据
                _time = GetTickTime(_tick);
                CheckTimelineData();
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
            _stage = ActionStage.Stop;
        }

        public void RegisterEventListener(Action<string, ActionEventParameter, object> listener)
        {
            _eventListeners.Add(listener);
        }

        public void UnregisterEventListener(Action<string, ActionEventParameter, object> listener)
        {
            _eventListeners.Remove(listener);
        }

        public void ChangeActionClip(ActionClip actionClip)
        {
            ClearData();
            _actionClip = actionClip;
        }

        public void BindCollider(GameObject bindObject)
        {
            _bindObject = bindObject;
        }

        /// <summary>
        /// 从指定帧开始播放动作内容
        /// </summary>
        /// <param name="tick"></param>
        public void StartAt(int tick)
        {
            _animancer.Animator.applyRootMotion = true;
            StartInternal(Mathf.Clamp(tick, 0, _actionClip.totalTicks));
        }

        /// <summary>
        /// 播放指定帧的动作内容
        /// </summary>
        /// <param name="tick"></param>
        public void PlayAt(int tick)
        {
            if (tick < 0 || tick > _actionClip.totalTicks || !_actionClip)
            {
                return;
            }

            _time = GetTickTime(tick);
            _tick = tick;

            // 清除动画数据并播放动画
            if (_animancer)
            {
                _animancer.Transitions = _actionClip.animation.transitionLibrary;
                _animancer.transform.position = Vector3.zero;
            }

            _playingAnimation = null;
            CheckAnimation();

            // 清除特效数据并播放特效
            _playingEffectClips.ForEach((kv) => GameObject.DestroyImmediate(kv.Value));
            _playingEffectClips.Clear();
            CheckEffect();

            // 清除碰撞检测数据并播放碰撞检测
            _playingCollideDetectionClips.ForEach((kv) => { kv.Value.Destroy(); });
            _playingCollideDetectionClips.Clear();
            CheckCollideDetection();

            // 检查事件
            CheckEvent();
        }

        private void StartInternal(int startTick)
        {
            if (!_actionClip)
            {
                return;
            }

            ClearData();

            _stage = ActionStage.Start;
            _time = GetTickTime(startTick);
            _tick = startTick;
            if (_actionClip)
            {
                _animancer.Transitions = _actionClip.animation.transitionLibrary;
            }

            // 检查第一帧数据
            CheckTimelineData();
        }

        private void ClearData()
        {
            // 清除动画数据
            _playingAnimation = null;
            if (_animancer)
            {
                _animancer.transform.position = Vector3.zero;
            }

            // 清除音频数据
            _stopAllAudioClipMethod.Invoke(null, null);
            _playingAudioClips.Clear();

            // 清除特效数据
            _playingEffectClips.ForEach((kv) => { GameObject.DestroyImmediate(kv.Value); });
            _playingEffectClips.Clear();

            // 清除碰撞检测数据
            _playingCollideDetectionClips.ForEach((kv) => { kv.Value.Destroy(); });
            _playingCollideDetectionClips.Clear();
        }

        private void CheckTimelineData()
        {
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
                _stage = ActionStage.End;
            }

            DebugUtil.LogCyan($"已播放动作({_actionClip.name}), 当前帧: {_tick}, 时间: {_time}, 阶段: {_stage}");
        }

        private void CheckProcess()
        {
            if (_tick >= _actionClip.process.anticipationTick)
            {
                _stage = ActionStage.Anticipation;
            }

            if (_tick >= _actionClip.process.judgmentTick)
            {
                _stage = ActionStage.Judgment;
            }

            if (_tick >= _actionClip.process.recoveryTick)
            {
                _stage = ActionStage.Recovery;
            }
        }

        private void CheckAnimation()
        {
            if (!_animancer)
            {
                return;
            }

            // 检查能否切换并播放动画
            foreach (var animationClip in _actionClip.animation.animationClips)
            {
                // 判断缩放切换到其他动画
                if (_tick >= animationClip.startTick &&
                    (_playingAnimation == null || animationClip.startTick > _playingAnimation.startTick)
                   )
                {
                    // 编辑器在切换时添加根位移
                    if (_playingAnimation != null)
                    {
                        var animationClips = new List<AnimationClip>();
                        _playingAnimation.transition.GetAnimationClips(animationClips);
                        var clip = animationClips[0];
                        var simulateRootMotion = clip.averageSpeed * (_time - _playingAnimation.startTime);
                        _animancer.Animator.transform.position += simulateRootMotion;
                    }

                    _playingAnimation = animationClip;
                }
            }

            if (_playingAnimation == null)
            {
                return;
            }

            // 检查正在播放的动画是否结束，未结束就定帧播放动画
            var endTick = _playingAnimation.startTick + _playingAnimation.durationTicks;
            if (_tick <= endTick)
            {
                var animationClips = new List<AnimationClip>();
                _playingAnimation.transition.GetAnimationClips(animationClips);
                var targetTime = Mathf.Clamp(_time - _playingAnimation.startTime, 0,
                    _playingAnimation.duration) * _playingAnimation.speed;
                var clip = animationClips[0];
                clip.EditModeSampleAnimation(_animancer, targetTime);
            }
        }

        private void CheckAudio()
        {
            foreach (var audioClip in _actionClip.audio.audioClips)
            {
                var startTick = audioClip.startTick;
                var endTick = audioClip.startTick + audioClip.durationTicks;
                if (_tick >= startTick && _tick <= endTick)
                {
                    if (_playingAudioClips.Contains(audioClip))
                    {
                        continue;
                    }

                    var clip = audioClip.AudioClip;
                    var startSample = (int)((_time - audioClip.startTime) / clip.length * clip.samples);
                    _playAudioClipMethod.Invoke(null, new object[] { clip, startSample, false });
                    _playingAudioClips.Add(audioClip);
                }

                if (_tick >= endTick)
                {
                    if (!_playingAudioClips.Contains(audioClip))
                    {
                        continue;
                    }

                    _playingAudioClips.Remove(audioClip);

                    if (_playingAudioClips.Count == 0)
                    {
                        _stopAllAudioClipMethod.Invoke(null, null);
                    }
                }
            }
        }

        private void CheckEffect()
        {
            foreach (var effectClip in _actionClip.effect.effectClips)
            {
                var startTick = effectClip.startTick;
                var endTick = effectClip.startTick + effectClip.durationTicks;
                if (_tick >= startTick && _tick <= endTick)
                {
                    if (!_playingEffectClips.TryGetValue(effectClip, out var instance))
                    {
                        instance = GameObject.Instantiate(effectClip.prefab, _model);
                        instance.transform.localPosition = effectClip.localPosition;
                        instance.transform.localRotation = effectClip.localRotation;
                        instance.transform.localScale = effectClip.localScale;
                        _playingEffectClips.Add(effectClip, instance);
                    }

                    var particleSystem = instance.GetComponentInChildren<ParticleSystem>();
                    switch (effectClip.type)
                    {
                        case ActionEffectType.Dynamic:
                            particleSystem.Simulate(
                                (_time - effectClip.startTime) * effectClip.simulationSpeed + effectClip.startLifetime,
                                true,
                                true);
                            break;
                        case ActionEffectType.Fixed:
                            particleSystem.Simulate(effectClip.startLifetime, true, true);
                            break;
                    }
                }

                if (_tick >= endTick)
                {
                    if (!_playingEffectClips.TryGetValue(effectClip, out var instance))
                    {
                        continue;
                    }

                    GameObject.DestroyImmediate(instance);
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
                        var @delegate = new ActionCollideDetectionDelegate(
                            _model,
                            _model.gameObject,
                            collideDetectionClip.data,
                            -1
                        );
                        @delegate.Init(collider =>
                            {
                                if (collider.gameObject == _model.gameObject)
                                {
                                    return;
                                }

                                DebugUtil.LogGreen($"发生碰撞: {collider.name}");
                            },
                            collideDetectionClip.data is ActionCollideDetectionShapeData shapeData &&
                            shapeData.showInEditor
                        );
                        _playingCollideDetectionClips.Add(collideDetectionClip, @delegate);
                        collideDetectionDelegate = @delegate;
                    }

                    collideDetectionDelegate.Tick(collider =>
                    {
                        if (collider.gameObject == _model.gameObject)
                        {
                            return;
                        }

                        DebugUtil.LogGreen($"发生碰撞: {collider.name}");
                    });
                }

                if (_tick >= endTick)
                {
                    if (!_playingCollideDetectionClips.TryGetValue(collideDetectionClip,
                            out var collideDetectionDelegate))
                    {
                        continue;
                    }

                    collideDetectionDelegate.Destroy();
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
                    DebugUtil.LogGreen($"事件: {eventClip.name}");
                    DebugUtil.LogGreen($"事件类型: {eventClip.parameter}");
                    switch (eventClip.parameter)
                    {
                        case ActionEventParameter.Bool:
                            DebugUtil.LogGreen($"事件值: {eventClip.boolPayload}");
                            break;
                        case ActionEventParameter.Int:
                            DebugUtil.LogGreen($"事件值: {eventClip.intPayload}");
                            break;
                        case ActionEventParameter.Float:
                            DebugUtil.LogGreen($"事件值: {eventClip.floatPayload}");
                            break;
                        case ActionEventParameter.String:
                            DebugUtil.LogGreen($"事件值: {eventClip.stringPayload}");
                            break;
                        case ActionEventParameter.UnityObject:
                            DebugUtil.LogGreen($"事件值: {eventClip.objectPayload}");
                            break;
                    }

                    _eventListeners.ForEach(listener =>
                    {
                        switch (eventClip.parameter)
                        {
                            case ActionEventParameter.None:
                                listener?.Invoke(eventClip.name, eventClip.parameter, null);
                                break;
                            case ActionEventParameter.Bool:
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.boolPayload);
                                break;
                            case ActionEventParameter.Int:
                                listener?.Invoke(eventClip.name, eventClip.parameter, eventClip.intPayload);
                                break;
                            case ActionEventParameter.Float:
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.floatPayload);
                                break;
                            case ActionEventParameter.String:
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.stringPayload);
                                break;
                            case ActionEventParameter.UnityObject:
                                listener?.Invoke(eventClip.name, eventClip.parameter,
                                    eventClip.objectPayload);
                                break;
                        }
                    });
                }
            }
        }

        private int GetTick(float time)
        {
            return (int)(time / TickDeltaTime);
        }

        private float GetTickTime(int tick)
        {
            return tick * TickDeltaTime;
        }
    }
}