using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Ability.Animation
{
    public class CharacterAnimationAbility : BaseCharacterOptionalAbility
    {
        protected const int UpdateFlag = 1 << 0;
        protected const int FadeFlag = 1 << 1;

        [Title("动画组件")] [SerializeField] private AnimancerComponent animancer;
        public AnimancerComponent Animancer => animancer;

        [Title("动画基础层")] [SerializeField] private float baseFadeDuration = AnimancerGraph.DefaultFadeDuration;

        [Title("动画动作层")] [SerializeField] private float actionFadeDuration = AnimancerGraph.DefaultFadeDuration;

        #region 基础层

        private AnimancerLayer _baseLayer;

        public AnimancerLayer BaseLayer
        {
            get
            {
                if (_baseLayer == null)
                {
                    _baseLayer = Animancer.Layers[0];
                    _baseLayer.SetDebugName("Base Layer");
                }

                return _baseLayer;
            }
        }

        #endregion

        #region 动作层

        private AnimancerLayer _actionLayer;
        private int _actionLayerFlag = 0;
        private readonly HashSet<AnimancerState> _actionLayerStates = new();
        private readonly List<AnimancerState> _actionLayerStopStates = new();

        public AnimancerLayer ActionLayer
        {
            get
            {
                if (_actionLayer == null)
                {
                    _actionLayer = Animancer.Layers[3];
                    _actionLayer.SetDebugName("Action Layer");
                }

                return _actionLayer;
            }
        }

        #endregion

        private float _lookAtWeight = 0f;
        private Vector3 _lookAtPosition = Vector3.zero;

        private float _speed = 1f;
        protected float SpeedFactor => _speed;
        protected float TimeScaleFactor => _speed <= 0f ? float.MaxValue : 1f / _speed;

        [Title("动画监听器集合")] public List<ICharacterAnimationListen> animationListeners = new();

        protected override void OnInit()
        {
            base.OnInit();
            if (Owner.Brain)
            {
                Owner.Brain.AnimatorIKDelegate += HandleAnimatorIK;
            }
        }

        /// <summary>
        /// 更新动画层，统一管理动画层的显隐，防止同帧下同一层存在多个动画的播放和停止导致的weight总和不为1，最终影响动画效果
        /// </summary>
        public virtual void UpdateLayers()
        {
            if ((_actionLayerFlag & UpdateFlag) != 0)
            {
                if ((_actionLayerFlag & FadeFlag) != 0)
                {
                    ActionLayer.StartFade(GetWeight(_actionLayerStates.Count), actionFadeDuration * TimeScaleFactor);
                }
                else
                {
                    ActionLayer.Weight = GetWeight(_actionLayerStates.Count);
                }
            }

            _actionLayerFlag = 0;
            _actionLayerStopStates.Clear();

            return;

            static int GetWeight(int animationCount)
            {
                return animationCount != 0 ? 1 : 0;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            if (Owner.Brain)
            {
                Owner.Brain.AnimatorIKDelegate -= HandleAnimatorIK;
            }
            
            // 强制重置并销毁动画层所有状态
            ForceResetBase();
            ForceResetAction();
            BaseLayer.DestroyStates();
            ActionLayer.DestroyStates();
        }

        public void SwitchTransitionLibrary(TransitionLibraryAsset transitionLibrary)
        {
            if (Animancer.Transitions == transitionLibrary)
            {
                return;
            }

            Animancer.Transitions = transitionLibrary;
        }

        public AnimancerState SwitchBase(ITransition transition, bool resetTime = false)
        {
            if (transition == null)
            {
                return null;
            }

            if (BaseLayer.CurrentState != null)
            {
                animationListeners.ForEach(listener =>
                    listener?.HandleAnimationStopped(Owner, BaseLayer, BaseLayer.CurrentState));
            }

            var animancerState = Play(BaseLayer, transition);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, BaseLayer, animancerState));

            return animancerState;
        }

        public AnimancerState SwitchBase(StringAsset name, bool resetTime = false)
        {
            if (name == null)
            {
                return null;
            }

            if (BaseLayer.CurrentState != null)
            {
                animationListeners.ForEach(listener =>
                    listener?.HandleAnimationStopped(Owner, BaseLayer, BaseLayer.CurrentState));
            }

            var animancerState = TryPlay(BaseLayer, name);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, BaseLayer, animancerState));

            return animancerState;
        }

        private void ForceResetBase()
        {
            if (BaseLayer.CurrentState != null)
            {
                animationListeners.ForEach(listener =>
                    listener?.HandleAnimationStopped(Owner, BaseLayer, BaseLayer.CurrentState));
            }
            BaseLayer.Stop();
        }

        public AnimancerState PlayAction(ITransition transition, bool withFade = true, bool resetTime = true)
        {
            if (transition == null)
            {
                return null;
            }

            var animancerState = Play(ActionLayer, transition);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            _actionLayerFlag |= UpdateFlag;
            _actionLayerFlag |= withFade ? FadeFlag : 0;
            _actionLayerStates.Add(animancerState);
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, ActionLayer, animancerState));

            return animancerState;
        }

        public AnimancerState PlayAction(StringAsset name, bool withFade = true, bool resetTime = true)
        {
            if (name == null)
            {
                return null;
            }

            var animancerState = TryPlay(ActionLayer, name);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            _actionLayerFlag |= UpdateFlag;
            _actionLayerFlag |= withFade ? FadeFlag : 0;
            _actionLayerStates.Add(animancerState);
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, ActionLayer, animancerState));

            return animancerState;
        }

        public void StopAction(AnimancerState animancerState, bool withFade = true)
        {
            _actionLayerStates.Remove(animancerState);
            _actionLayerStopStates.Add(animancerState);
            _actionLayerFlag |= UpdateFlag;
            _actionLayerFlag |= withFade ? FadeFlag : 0;
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationStopped(Owner, ActionLayer, animancerState));
        }

        public void StopAction(ITransition transition, bool withFade = true)
        {
            var key = transition.Key;
            var animancerState = ActionLayer.GetState(ref key);
            StopAction(animancerState, withFade);
        }

        public void StopAction(StringAsset name, bool withFade = true)
        {
            var key = name.Key;
            var animancerState = ActionLayer.GetState(ref key);
            StopAction(animancerState, withFade);
        }

        public void ClearAction(bool withFade = true)
        {
            _actionLayerStopStates.AddRange(_actionLayerStates);
            _actionLayerStates.Clear();
            _actionLayerFlag |= UpdateFlag;
            _actionLayerFlag |= withFade ? FadeFlag : 0;
            _actionLayerStopStates.ForEach(animancerState =>
                animationListeners.ForEach(listener =>
                    listener?.HandleAnimationStopped(Owner, ActionLayer, animancerState))
            );
        }

        private void ForceResetAction()
        {
            ClearAction(false);
            ActionLayer.Stop();
        }

        public virtual void ClearAllLayers(bool withFade = true)
        {
            ClearAction(withFade);
        }

        public void ApplyBaseIK(bool apply)
        {
            BaseLayer.ApplyAnimatorIK = apply;
        }

        public void ApplyActionIK(bool apply)
        {
            ActionLayer.ApplyAnimatorIK = apply;
        }

        public void SetLookAtWeight(float weight)
        {
            _lookAtWeight = weight;
        }

        public void SetLookAtPosition(Vector3 position)
        {
            _lookAtPosition = position;
        }

        public void SetAnimancerSpeed(float speed)
        {
            var originalSpeed = _speed;
            _speed = Mathf.Max(speed, 0f);
            var relativeFactor = _speed / originalSpeed;
            SetLayerSpeed(relativeFactor);
        }

        protected virtual void SetLayerSpeed(float factor)
        {
            if (BaseLayer.CurrentState != null)
            {
                BaseLayer.CurrentState.Speed *= factor;
            }

            if (ActionLayer.CurrentState != null)
            {
                ActionLayer.CurrentState.Speed *= factor;
            }
        }

        protected AnimancerState Play(AnimancerLayer layer, ITransition transition)
        {
            var transitionLibrary = layer.Graph.Transitions;
            if (transitionLibrary != null)
            {
                var fadeDuration1 = transitionLibrary.GetFadeDuration(layer, transition) * TimeScaleFactor;
                return layer.Play(
                    transition,
                    fadeDuration1,
                    transition.FadeMode);
            }

            var fadeDuration2 = transition.FadeDuration * TimeScaleFactor;
            return layer.Play(transition, fadeDuration2, transition.FadeMode);
        }

        protected AnimancerState TryPlay(AnimancerLayer layer, IHasKey hasKey)
        {
            var transitionLibrary = layer.Graph.Transitions;
            if (transitionLibrary != null && transitionLibrary.TryGetTransition(hasKey.Key, out var transition))
            {
                var from = layer.CurrentState?.Key;
                var to = transition.Transition;
                var fadeDuration = from != null
                    ? transition.GetFadeDuration(from) * TimeScaleFactor
                    : to.FadeDuration * TimeScaleFactor;
                var transitionState = layer.Play(
                    to,
                    fadeDuration,
                    to.FadeMode);
                if (transitionState != null)
                    return transitionState;
            }

            return layer.Graph.States.TryGet(hasKey.Key, out var state)
                ? layer.Play(state)
                : null;
        }

        private void HandleAnimatorIK(Animator animator)
        {
            animator.SetLookAtWeight(_lookAtWeight);
            animator.SetLookAtPosition(_lookAtPosition);
        }
    }
}