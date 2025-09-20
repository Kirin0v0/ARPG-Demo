using System;
using Action.Editor.Channel;
using Action.Editor.Channel.Extension;
using Action.Editor.Track.Extension;
using Action.Editor.Track.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Action.Editor.Track
{
    public abstract class BaseActionTrack<Inspector> : IActionTrack
        where Inspector : ActionTrackInspectorSO
    {
        private readonly ActionTrackTemplateUI _comboTrackTemplateUI = new();

        public string Name { get; private set; }
        public IActionChannel Channel { get; private set; }

        public ActionTrackEditorData Data { get; private set; }

        protected ActionChannelTrackSupportAbility SupportAbility;

        public virtual void Init(VisualElement parent, IActionChannel channel,
            ActionChannelTrackSupportAbility supportAbility)
        {
            Channel = channel;
            SupportAbility = supportAbility;
            _comboTrackTemplateUI.CreateView(parent);
            _comboTrackTemplateUI.ToShowDropdownMenu = ToShowDropdownMenu;
            _comboTrackTemplateUI.OnTrackSelected = selected =>
            {
                if (!selected) return;
                ToSelectTrack();
            };
            _comboTrackTemplateUI.MoveToTargetFrame = TryMoveToTargetFrame;
            ActionEditorWindow.Instance.OnTracksRefreshed += Refresh;
        }

        public virtual void Bind(ActionTrackEditorData data)
        {
            Data = data;
            var usedUITickRange = data.UsedUITickRange;
            _comboTrackTemplateUI.BindView(
                new ActionTrackEditorUIData
                {
                    Name = data.Name,
                    StartFrame = usedUITickRange.start,
                    DurationFrames = usedUITickRange.end - usedUITickRange.start,
                    NormalColor = Channel.Data.TrackNormalColor,
                    SelectedColor = Channel.Data.TrackSelectedColor,
                }
            );
            Name = data.Name;

            // 如果当前激活是当前轨道窗口，就更新数据
            if (Selection.activeObject is ActionTrackInspectorSO inspector && inspector.IsBindToTrack(this))
            {
                Selection.activeObject = GetTrackInspector();
            }

            // 更新轨道预览
            ActionEditorWindow.Instance.UpdateTrackPreview(Data);
        }

        public virtual void Destroy()
        {
            Channel = null;
            _comboTrackTemplateUI.DestroyView();
            _comboTrackTemplateUI.ToShowDropdownMenu = null;
            _comboTrackTemplateUI.OnTrackSelected = null;
            _comboTrackTemplateUI.MoveToTargetFrame = null;
            ActionEditorWindow.Instance.OnTracksRefreshed -= Refresh;
        }

        public virtual void Refresh()
        {
            if (Data.CheckWhetherDirty(ActionEditorWindow.Instance.TotalFrames))
            {
                Bind(Data);
            }
            else
            {
                _comboTrackTemplateUI.RefreshView();
            }
        }

        public bool Active(float currentTime)
        {
            var tickRange = Data.UsedTickRange;
            return currentTime >= tickRange.start * ActionEditorWindow.Instance.FrameTimeUnit &&
                   currentTime < tickRange.end * ActionEditorWindow.Instance.FrameTimeUnit;
        }

        protected virtual GenericDropdownMenu ToShowDropdownMenu()
        {
            if (SupportAbility == ActionChannelTrackSupportAbility.None)
            {
                return null;
            }

            var dropdownMenu = new GenericDropdownMenu();

            if ((SupportAbility & ActionChannelTrackSupportAbility.MoveToSelectedFrame) != 0)
            {
                dropdownMenu.AddItem("移动轨道至选中帧", false, () =>
                {
                    if (!Channel.AllowMoveTrackToPosition(this, ActionEditorWindow.Instance.SelectedFrame))
                    {
                        return;
                    }

                    // 重新绑定数据
                    Data.MoveTo(ActionEditorWindow.Instance.SelectedFrame, ActionEditorWindow.Instance.FrameTimeUnit);
                    Bind(Data);
                });
            }

            if ((SupportAbility & ActionChannelTrackSupportAbility.DeleteTrack) != 0)
            {
                dropdownMenu.AddItem("删除轨道", false, () => { Channel.UnbindTrack(this); });
            }

            if ((SupportAbility & ActionChannelTrackSupportAbility.CopyToSelectedFrame) != 0)
            {
                dropdownMenu.AddItem("复制轨道到选中帧", false, () =>
                {
                    var newTrackData = Data.CopyTo(ActionEditorWindow.Instance.SelectedFrame,
                        ActionEditorWindow.Instance.FrameTimeUnit);
                    if (Channel.HasTimeCollisionWithTarget(newTrackData))
                    {
                        return;
                    }

                    // 绑定数据到新的轨道
                    var track = (BaseActionTrack<Inspector>)Activator.CreateInstance(this.GetType());
                    Channel.BindTrack(track, newTrackData);
                    // 选中新轨道
                    track.ToSelectTrack();
                });
            }

            return dropdownMenu;
        }

        protected virtual void ToSelectTrack()
        {
            Selection.activeObject = GetTrackInspector();
        }

        protected virtual bool TryMoveToTargetFrame(int targetFrame)
        {
            if (!Channel.TryMoveTrackToPosition(this, targetFrame, out var moveToFrame))
            {
                return false;
            }

            // 重新绑定数据
            Data.MoveTo(moveToFrame, ActionEditorWindow.Instance.FrameTimeUnit);
            Bind(Data);
            return true;
        }

        private ActionTrackInspectorSO GetTrackInspector()
        {
            var inspector = ScriptableObject.CreateInstance<Inspector>();
            inspector.Init(this, UpdateTrackData);
            SynchronizeToInspector(inspector);
            return inspector;
        }

        private void UpdateTrackData(ActionTrackInspectorSO inspector)
        {
            SynchronizeToTrackData(inspector as Inspector);
            Bind(Data);
        }

        protected virtual void SynchronizeToInspector(Inspector inspector)
        {
            inspector.name = Data.Name;
            switch (inspector)
            {
                case ActionTrackPointInspectorSO pointInspector when Data is ActionTrackPointEditorData pointData:
                {
                    pointInspector.time = pointData.Time;
                    pointInspector.tick = pointData.Tick;
                }
                    break;
                case ActionTrackFragmentInspectorSO fragmentInspector
                    when Data is ActionTrackFragmentEditorData fragmentData:
                {
                    fragmentInspector.startTime = fragmentData.StartTime;
                    fragmentInspector.startTick = fragmentData.StartTick;
                    fragmentInspector.duration = fragmentData.Duration;
                    fragmentInspector.durationTicks = fragmentData.DurationTicks;
                }
                    break;
            }
        }

        protected virtual void SynchronizeToTrackData(Inspector inspector)
        {
            Data.Name = inspector.name;
            switch (Data)
            {
                case ActionTrackPointEditorData pointData when inspector is ActionTrackPointInspectorSO pointInspector:
                {
                    pointData.Time = pointInspector.time;
                    pointData.Tick = pointInspector.tick;
                }
                    break;
                case ActionTrackFragmentEditorData fragmentData
                    when inspector is ActionTrackFragmentInspectorSO fragmentInspector:
                {
                    fragmentData.StartTime = fragmentInspector.startTime;
                    fragmentData.Duration = fragmentInspector.duration;
                    fragmentData.StartTick = fragmentInspector.startTick;
                    fragmentData.DurationTicks = fragmentInspector.durationTicks;
                }
                    break;
            }
        }
    }
}