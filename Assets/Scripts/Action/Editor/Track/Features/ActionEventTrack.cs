using Sirenix.OdinInspector;
using UnityEngine;

namespace Action.Editor.Track.Features
{
    public class ActionEventTrackEditorData : ActionTrackPointEditorData
    {
        public ActionEventParameter Parameter;
        public bool BoolPayload;
        public int IntPayload;
        public float FloatPayload;
        public string StringPayload;
        public Object ObjectPayload;

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionEventTrackEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                Time = targetTick * tickTime,
                Tick = targetTick,
                Parameter = Parameter,
                BoolPayload = BoolPayload,
                IntPayload = IntPayload,
                FloatPayload = FloatPayload,
                StringPayload = StringPayload,
                ObjectPayload = ObjectPayload
            };
        }
    }

    public class ActionEventTrackInspectorSO : ActionTrackPointInspectorSO
    {
        [LabelText("事件名称"), Delayed, OnValueChanged("Update")] public string @event;
        
        [LabelText("事件参数"), Delayed, OnValueChanged("Update")] public ActionEventParameter parameter;

        [LabelText("事件参数值"), Delayed, OnValueChanged("Update")] [ShowIf("parameter", ActionEventParameter.Bool)]
        public bool boolPayload;

        [LabelText("事件参数值"), Delayed, OnValueChanged("Update")] [ShowIf("parameter", ActionEventParameter.Int)]
        public int intPayload;

        [LabelText("事件参数值"), Delayed, OnValueChanged("Update")] [ShowIf("parameter", ActionEventParameter.Float)]
        public float floatPayload;

        [LabelText("事件参数值"), Delayed, OnValueChanged("Update")] [ShowIf("parameter", ActionEventParameter.String)]
        public string stringPayload;

        [LabelText("事件参数值"), Delayed, OnValueChanged("Update")] [ShowIf("parameter", ActionEventParameter.UnityObject)]
        public Object objectPayload;
    }

    public class ActionEventTrack : BaseActionTrack<ActionEventTrackInspectorSO>
    {
        protected override void SynchronizeToInspector(ActionEventTrackInspectorSO inspector)
        {
            base.SynchronizeToInspector(inspector);
            if (Data is ActionEventTrackEditorData data)
            {
                inspector.name = data.Name;
                inspector.@event = data.Name;
                inspector.parameter = data.Parameter;
                inspector.boolPayload = data.BoolPayload;
                inspector.intPayload = data.IntPayload;
                inspector.floatPayload = data.FloatPayload;
                inspector.stringPayload = data.StringPayload;
                inspector.objectPayload = data.ObjectPayload;
            }
        }

        protected override void SynchronizeToTrackData(ActionEventTrackInspectorSO inspector)
        {
            base.SynchronizeToTrackData(inspector);
            if (Data is ActionEventTrackEditorData data)
            {
                data.Name = inspector.@event;
                data.Parameter = inspector.parameter;
                data.BoolPayload = inspector.boolPayload;
                data.IntPayload = inspector.intPayload;
                data.FloatPayload = inspector.floatPayload;
                data.StringPayload = inspector.stringPayload;
                data.ObjectPayload = inspector.objectPayload;
            }
        }
    }
}