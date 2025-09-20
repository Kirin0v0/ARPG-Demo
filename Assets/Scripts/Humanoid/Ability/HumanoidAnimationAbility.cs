using System.Collections.Generic;
using Animancer;
using Character.Ability;
using Character.Ability.Animation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Humanoid.Ability
{
    public class HumanoidAnimationAbility : CharacterAnimationAbility
    {
        private new HumanoidCharacterObject Owner => base.Owner as HumanoidCharacterObject;

        [Title("动画上半身层")] [SerializeField] private float armFadeDuration = AnimancerGraph.DefaultFadeDuration;
        [SerializeField] private AvatarMask armMask;

        [Title("动画下半身层")] [SerializeField] private float legFadeDuration = AnimancerGraph.DefaultFadeDuration;
        [SerializeField] private AvatarMask legMask;

        #region 手部层

        private AnimancerLayer _armLayer;
        private int _armLayerFlag = 0;
        private readonly HashSet<AnimancerState> _armLayerStates = new();
        private readonly List<AnimancerState> _armLayerStopStates = new();

        public AnimancerLayer ArmLayer
        {
            get
            {
                if (_armLayer == null)
                {
                    _armLayer = Animancer.Layers[1];
                    _armLayer.Mask = armMask;
                    _armLayer.SetDebugName("Arm Layer");
                }

                return _armLayer;
            }
        }

        #endregion

        #region 腿部层

        private AnimancerLayer _legLayer;
        private int _legLayerFlag = 0;
        private readonly HashSet<AnimancerState> _legLayerStates = new();
        private readonly List<AnimancerState> _legLayerStopStates = new();

        public AnimancerLayer LegLayer
        {
            get
            {
                if (_legLayer == null)
                {
                    _legLayer = Animancer.Layers[2];
                    _legLayer.Mask = legMask;
                    _legLayer.SetDebugName("Leg Layer");
                }

                return _legLayer;
            }
        }

        #endregion

        public override void UpdateLayers()
        {
            base.UpdateLayers();

            if ((_armLayerFlag & UpdateFlag) != 0)
            {
                if ((_armLayerFlag & FadeFlag) != 0)
                {
                    ArmLayer.StartFade(GetWeight(_armLayerStates.Count), armFadeDuration * TimeScaleFactor);
                }
                else
                {
                    ArmLayer.Weight = GetWeight(_armLayerStates.Count);
                }
            }

            if ((_legLayerFlag & UpdateFlag) != 0)
            {
                if ((_legLayerFlag & FadeFlag) != 0)
                {
                    LegLayer.StartFade(GetWeight(_legLayerStates.Count), legFadeDuration * TimeScaleFactor);
                }
                else
                {
                    LegLayer.Weight = GetWeight(_legLayerStates.Count);
                }
            }

            _armLayerFlag = 0;
            _legLayerFlag = 0;
            _armLayerStopStates.Clear();
            _legLayerStopStates.Clear();

            return;

            static int GetWeight(int animationCount)
            {
                return animationCount != 0 ? 1 : 0;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 强制重置并销毁动画层所有状态
            ForceResetArm();
            ForceResetLeg();
            ArmLayer.DestroyStates();
            LegLayer.DestroyStates();
        }

        public AnimancerState PlayArm(ITransition transition, bool withFade = true, bool resetTime = true)
        {
            if (transition == null)
            {
                return null;
            }

            var animancerState = Play(ArmLayer, transition);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            _armLayerFlag |= UpdateFlag;
            _armLayerFlag |= withFade ? FadeFlag : 0;
            _armLayerStates.Add(animancerState);
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, ArmLayer, animancerState));

            return animancerState;
        }

        public AnimancerState PlayArm(StringAsset name, bool withFade = true, bool resetTime = true)
        {
            if (name == null)
            {
                return null;
            }

            var animancerState = TryPlay(ArmLayer, name);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            _armLayerFlag |= UpdateFlag;
            _armLayerFlag |= withFade ? FadeFlag : 0;
            _armLayerStates.Add(animancerState);
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, ArmLayer, animancerState));

            return animancerState;
        }

        public void StopArm(AnimancerState animancerState, bool withFade = true)
        {
            _armLayerStates.Remove(animancerState);
            _armLayerStopStates.Add(animancerState);
            _armLayerFlag |= UpdateFlag;
            _armLayerFlag |= withFade ? FadeFlag : 0;
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationStopped(Owner, ArmLayer, animancerState));
        }

        public void StopArm(ITransition transition, bool withFade = true)
        {
            var key = transition.Key;
            var animancerState = ArmLayer.GetState(ref key);
            StopArm(animancerState, withFade);
        }

        public void StopArm(StringAsset name, bool withFade = true)
        {
            var key = name.Key;
            var animancerState = ArmLayer.GetState(ref key);
            StopArm(animancerState, withFade);
        }

        public void ClearArm(bool withFade = true)
        {
            _armLayerStopStates.AddRange(_armLayerStates);
            _armLayerStates.Clear();
            _armLayerFlag |= UpdateFlag;
            _armLayerFlag |= withFade ? FadeFlag : 0;
            _armLayerStopStates.ForEach(animancerState =>
                animationListeners.ForEach(listener =>
                    listener?.HandleAnimationStopped(Owner, ArmLayer, animancerState)));
        }

        private void ForceResetArm()
        {
            ClearArm(false);
            ArmLayer.Stop();
        }

        public AnimancerState PlayLeg(ITransition transition, bool withFade = true, bool resetTime = true)
        {
            if (transition == null)
            {
                return null;
            }

            var animancerState = Play(LegLayer, transition);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            _legLayerFlag |= UpdateFlag;
            _legLayerFlag |= withFade ? FadeFlag : 0;
            _legLayerStates.Add(animancerState);
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, LegLayer, animancerState));

            return animancerState;
        }

        public AnimancerState PlayLeg(StringAsset name, bool withFade = true, bool resetTime = true)
        {
            if (name == null)
            {
                return null;
            }

            var animancerState = TryPlay(LegLayer, name);
            animancerState.Speed *= SpeedFactor;
            if (resetTime)
            {
                animancerState.Time = 0f;
            }

            _legLayerFlag |= UpdateFlag;
            _legLayerFlag |= withFade ? FadeFlag : 0;
            _legLayerStates.Add(animancerState);
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationPlayed(Owner, LegLayer, animancerState));

            return animancerState;
        }

        public void StopLeg(AnimancerState animancerState, bool withFade = true)
        {
            _legLayerStates.Remove(animancerState);
            _legLayerStopStates.Add(animancerState);
            _legLayerFlag |= UpdateFlag;
            _legLayerFlag |= withFade ? FadeFlag : 0;
            animationListeners.ForEach(listener =>
                listener?.HandleAnimationStopped(Owner, LegLayer, animancerState));
        }

        public void StopLeg(ITransition transition, bool withFade = true)
        {
            var key = transition.Key;
            var animancerState = LegLayer.GetState(ref key);
            StopLeg(animancerState, withFade);
        }

        public void StopLeg(StringAsset name, bool withFade = true)
        {
            var key = name.Key;
            var animancerState = LegLayer.GetState(ref key);
            StopLeg(animancerState, withFade);
        }

        public void ClearLeg(bool withFade = true)
        {
            _legLayerStopStates.AddRange(_legLayerStates);
            _legLayerStates.Clear();
            _legLayerFlag |= UpdateFlag;
            _legLayerFlag |= withFade ? FadeFlag : 0;
            _legLayerStopStates.ForEach(animancerState =>
                animationListeners.ForEach(listener =>
                    listener?.HandleAnimationPlayed(Owner, LegLayer, animancerState)));
        }

        private void ForceResetLeg()
        {
            ClearLeg(false);
            LegLayer.Stop();
        }

        public override void ClearAllLayers(bool withFade = true)
        {
            base.ClearAllLayers(withFade);
            ClearArm(withFade);
            ClearLeg(withFade);
        }

        public void ApplyArmIK(bool apply)
        {
            ArmLayer.ApplyAnimatorIK = apply;
        }

        public void ApplyLegIK(bool apply)
        {
            LegLayer.ApplyAnimatorIK = apply;
        }

        protected override void SetLayerSpeed(float factor)
        {
            base.SetLayerSpeed(factor);

            if (ArmLayer.CurrentState != null)
            {
                ArmLayer.CurrentState.Speed *= factor;
            }

            if (LegLayer.CurrentState != null)
            {
                LegLayer.CurrentState.Speed *= factor;
            }
        }
    }
}