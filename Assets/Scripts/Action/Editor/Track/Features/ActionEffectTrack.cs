using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Action.Editor.Track.Features
{
    public class ActionEffectTrackEditorData : ActionTrackFragmentEditorData
    {
        public GameObject Prefab;
        public ActionEffectType Type;
        public float StartLifetime;
        public float SimulationSpeed;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionEffectTrackEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                StartTime = targetTick * tickTime,
                StartTick = targetTick,
                Duration = 1 * tickTime,
                DurationTicks = 1,
                Prefab = Prefab,
                Type = Type,
                StartLifetime = StartLifetime,
                SimulationSpeed = SimulationSpeed,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation,
                LocalScale = LocalScale,
            };
        }
    }

    public class ActionEffectTrackInspectorSO : ActionTrackFragmentInspectorSO
    {
        [LabelText("特效预设体"), Delayed, OnValueChanged("Update")] public GameObject prefab;
        [LabelText("特效类型"), Delayed, OnValueChanged("Update")] public ActionEffectType type;
        [LabelText("特效初始时间"), Delayed, OnValueChanged("Update")] public float startLifetime;
        [LabelText("特效模拟速度"), Delayed, OnValueChanged("Update")] public float simulationSpeed = 1f;
        [LabelText("特效本地位置"), DelayedProperty, OnValueChanged("Update")] public Vector3 localPosition = Vector3.zero;
        [LabelText("特效本地旋转量"), DelayedProperty, OnValueChanged("Update")] public Vector3 localRotation = Vector3.zero;
        [LabelText("特效本地缩放"), DelayedProperty, OnValueChanged("Update")] public Vector3 localScale;
    }

    public class ActionEffectTrack : BaseActionTrack<ActionEffectTrackInspectorSO>
    {
        public override void Bind(ActionTrackEditorData data)
        {
            base.Bind(data);

            // 检查资源是否包含预设体对象，不是则弹出错误提示
            if (data is ActionEffectTrackEditorData actionEffectTrackEditorData &&
                (!actionEffectTrackEditorData.Prefab ||
                 actionEffectTrackEditorData.Prefab.GetComponentsInChildren<ParticleSystem>().Length == 0))
            {
                DebugUtil.LogWarning($"The track({this}) has no effect prefab or particle system");
            }
        }

        protected override void SynchronizeToInspector(ActionEffectTrackInspectorSO inspector)
        {
            base.SynchronizeToInspector(inspector);
            if (Data is ActionEffectTrackEditorData data)
            {
                inspector.prefab = data.Prefab;
                inspector.type = data.Type;
                inspector.startLifetime = data.StartLifetime;
                inspector.simulationSpeed = data.SimulationSpeed;
                inspector.localPosition = data.LocalPosition;
                inspector.localRotation = data.LocalRotation.eulerAngles;
                inspector.localScale = data.LocalScale;
            }
        }

        protected override void SynchronizeToTrackData(ActionEffectTrackInspectorSO inspector)
        {
            base.SynchronizeToTrackData(inspector);
            if (Data is ActionEffectTrackEditorData data)
            {
                data.Prefab = inspector.prefab;
                data.Type = inspector.type;
                data.StartLifetime = inspector.startLifetime;
                data.SimulationSpeed = inspector.simulationSpeed;
                data.LocalPosition = inspector.localPosition;
                data.LocalRotation = Quaternion.Euler(inspector.localRotation);
                data.LocalScale = inspector.localScale;
            }
        }
    }
}