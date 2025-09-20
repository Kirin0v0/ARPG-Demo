using System;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Action.Editor.Track
{
    public enum ActionTrackType
    {
        Point,
        Fragment,
    }

    public enum ActionTrackRestrictionStrategy
    {
        RestrictInTotalFrames, // 限制轨道在总帧数内
        None,
    }

    public abstract class ActionTrackEditorData
    {
        public string Name;

        public ActionTrackRestrictionStrategy
            RestrictionStrategy = ActionTrackRestrictionStrategy.RestrictInTotalFrames;

        public abstract int PivotTick { get; }
        public abstract (int start, int end) UsedTickRange { get; }
        public abstract (float start, float end) UsedUITickRange { get; }

        public abstract void MoveTo(int targetTick, float tickTime);
        public abstract ActionTrackEditorData CopyTo(int targetTick, float tickTime);

        public abstract void UpdateFrameRate(float frameRate);
        public abstract bool IsActive(int tick);
    }

    public class ActionTrackPointEditorData : ActionTrackEditorData
    {
        public float Time;
        public int Tick;

        public override int PivotTick => Tick;
        public override (int start, int end) UsedTickRange => (Tick, Tick);
        public override (float start, float end) UsedUITickRange => (Tick - 0.5f, Tick + 0.5f);

        public override void MoveTo(int targetTick, float tickTime)
        {
            Time = targetTick * tickTime;
            Tick = targetTick;
        }

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionTrackPointEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                Time = targetTick * tickTime,
                Tick = targetTick
            };
        }

        public override void UpdateFrameRate(float frameRate)
        {
            Tick = Mathf.RoundToInt(Time * frameRate);
        }

        public override bool IsActive(int tick)
        {
            return tick == Tick;
        }
    }

    public class ActionTrackFragmentEditorData : ActionTrackEditorData
    {
        public float StartTime;
        public float Duration;
        public int StartTick;
        public int DurationTicks;

        public override int PivotTick => StartTick;
        public override (int start, int end) UsedTickRange => (StartTick, StartTick + DurationTicks);
        public override (float start, float end) UsedUITickRange => (StartTick, StartTick + DurationTicks);

        public override void MoveTo(int targetTick, float tickTime)
        {
            StartTime = targetTick * tickTime;
            StartTick = targetTick;
        }

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionTrackFragmentEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                StartTime = targetTick * tickTime,
                StartTick = targetTick,
                Duration = 1 * tickTime,
                DurationTicks = 1
            };
        }

        public override void UpdateFrameRate(float frameRate)
        {
            StartTick = Mathf.RoundToInt(StartTime * frameRate);
            DurationTicks = Mathf.CeilToInt(Duration * frameRate);
        }

        public override bool IsActive(int tick)
        {
            return tick >= StartTick && tick <= StartTick + DurationTicks;
        }
    }
}