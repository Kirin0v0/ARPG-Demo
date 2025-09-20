using Character;
using Framework.Common.Timeline;
using Framework.Common.Timeline.Data;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skill.Runtime
{
    public class SkillReleaseInfo
    {
        private readonly SkillFlow _skillFlow;
        public string Id => _skillFlow.Id;
        public string Name => _skillFlow.Name;

        private readonly TimelineInfo _timelineInfo; // 注意，这个时间轴只是总时间轴，本质上技能是可以存在多个子时间轴组装而成的
        public float Duration => _timelineInfo.Timeline.Duration;
        public float Time => _timelineInfo.TimeElapsed;

        public CharacterObject Caster { get; }

        public CharacterObject Target { get; }

        public bool Finished => _timelineInfo.Finished;

        public event System.Action<SkillReleaseInfo> OnStop;
        public event System.Action<SkillReleaseInfo> OnComplete;

        public SkillReleaseInfo(
            SkillFlow skillFlow,
            TimelineInfo timelineInfo,
            CharacterObject caster,
            CharacterObject target
        )
        {
            _skillFlow = skillFlow;
            _timelineInfo = timelineInfo;
            Caster = caster;
            Target = target;
            _timelineInfo.OnStopped += () =>
            {
                OnStop?.Invoke(this); 
            };
            _timelineInfo.OnCompleted += () =>
            {
                OnComplete?.Invoke(this);
            };
        }

        /// <summary>
        /// 设置技能时间轴的时间缩放
        /// </summary>
        /// <param name="timeScale"></param>
        public void SetTimeScale(float timeScale)
        {
            _skillFlow.SetSkillSpeed(timeScale);
        }

        /// <summary>
        /// 强制中断技能释放过程，由外部手动调用
        /// </summary>
        public void Stop()
        {
            // 强制中断全部时间轴
            _skillFlow.StopSkill();
        }

        public void Destroy()
        {
            GameObject.Destroy(_skillFlow);
        }
    }
}