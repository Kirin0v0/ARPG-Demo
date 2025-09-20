using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Action.Editor.Track.Features
{
    public class ActionCollideDetectionTrackEditorData : ActionTrackFragmentEditorData
    {
        public string GroupId;
        public ActionCollideDetectionType Type;
        public ActionCollideDetectionBaseData Data;

        public static string GetName(ActionCollideDetectionType type)
        {
            return type switch
            {
                ActionCollideDetectionType.None => "无碰撞检测",
                ActionCollideDetectionType.Bind => "绑定碰撞检测",
                ActionCollideDetectionType.Box => "盒状碰撞检测",
                ActionCollideDetectionType.Sphere => "球形碰撞检测",
                ActionCollideDetectionType.Sector => "扇形碰撞检测",
                _ => "未知",
            };
        }

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionCollideDetectionTrackEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                StartTime = targetTick * tickTime,
                StartTick = targetTick,
                Duration = 1 * tickTime,
                DurationTicks = 1,
                GroupId = GroupId,
                Type = Type,
                Data = Data,
            };
        }
    }

    public class ActionCollideDetectionTrackInspectorSO : ActionTrackFragmentInspectorSO
    {
        [LabelText("碰撞检测组"), Delayed, OnValueChanged("Update")]
        public string groupId;

        [LabelText("碰撞检测类型"), Delayed, OnValueChanged("UpdateType")]
        public ActionCollideDetectionType type;

        [LabelText("是否在编辑器中展示"), Delayed, OnValueChanged("Update")]
        [ShowIf(
            "@type == ActionCollideDetectionType.Box || type == ActionCollideDetectionType.Sphere || type == ActionCollideDetectionType.Sector")]
        public bool showInEditor = false;

        [LabelText("本地位置"), DelayedProperty, OnValueChanged("Update")]
        [ShowIf(
            "@type == ActionCollideDetectionType.Box || type == ActionCollideDetectionType.Sphere || type == ActionCollideDetectionType.Sector")]
        public Vector3 localPosition = Vector3.zero;

        [LabelText("本地旋转量"), DelayedProperty, OnValueChanged("Update")]
        [ShowIf("@type == ActionCollideDetectionType.Box || type == ActionCollideDetectionType.Sector")]
        public Vector3 localRotation = Vector3.zero;

        [LabelText("尺寸"), DelayedProperty, OnValueChanged("Update")] [ShowIf("@type == ActionCollideDetectionType.Box")]
        public Vector3 size = Vector3.one;

        [LabelText("半径"), MinValue(0f), Delayed, OnValueChanged("Update")]
        [ShowIf("@type == ActionCollideDetectionType.Sphere")]
        public float radius = 1f;

        [LabelText("内环半径"), MinValue(0f), Delayed, OnValueChanged("Update")]
        [ShowIf("@type == ActionCollideDetectionType.Sector")]
        public float insideRadius = 0f;

        [LabelText("外圈半径"), MinValue(0f), Delayed, OnValueChanged("Update")]
        [ShowIf("@type == ActionCollideDetectionType.Sector")]
        public float outsideRadius = 1f;

        [LabelText("高度"), Delayed, OnValueChanged("Update")] [ShowIf("@type == ActionCollideDetectionType.Sector")]
        public float height = 1f;

        [LabelText("扇形中心角度"), Delayed, OnValueChanged("Update")] [ShowIf("@type == ActionCollideDetectionType.Sector")]
        public float anglePivot = 0f;

        [LabelText("扇形角度"), Delayed, OnValueChanged("Update")] [ShowIf("@type == ActionCollideDetectionType.Sector")]
        public float angle = 60f;

        private void UpdateType()
        {
            name = ActionCollideDetectionTrackEditorData.GetName(type);
            Update();
        }

        private void OnValidate()
        {
            Update();
        }
    }

    public class ActionCollideDetectionTrack : BaseActionTrack<ActionCollideDetectionTrackInspectorSO>
    {
        public void UpdateSceneView()
        {
            // var previewInstance = ActionEditorWindow.Instance.PreviewInstance;
            // if (!previewInstance || Data is not ActionCollideDetectionTrackEditorData data)
            // {
            //     return;
            // }
            //
            // switch (data.Type)
            // {
            //     case ActionCollideDetectionType.Box:
            //     {
            //         var boxData = data.Data as ActionCollideDetectionBoxData;
            //         var position = previewInstance.transform.TransformPoint(boxData.localPosition);
            //         var rotation = previewInstance.transform.rotation * boxData.localRotation;
            //         EditorGUI.BeginChangeCheck();
            //         Handles.TransformHandle(ref position, ref rotation, ref boxData.size);
            //         if (EditorGUI.EndChangeCheck())
            //         {
            //             boxData.localPosition = previewInstance.transform.InverseTransformPoint(position);
            //             boxData.localRotation = Quaternion.Inverse(previewInstance.transform.rotation) * rotation;
            //             // 更新数据
            //             Bind(Data);
            //         }
            //     }
            //         break;
            //     case ActionCollideDetectionType.Sphere:
            //     {
            //         var sphereData = data.Data as ActionCollideDetectionSphereData;
            //         var position = previewInstance.transform.TransformPoint(sphereData.localPosition);
            //         EditorGUI.BeginChangeCheck();
            //         var newPosition = Handles.PositionHandle(position, Quaternion.identity);
            //         var newRadius = Handles.ScaleSlider(sphereData.radius, newPosition, Vector3.up,
            //             Quaternion.identity, sphereData.radius + 0.3f, 0.1f);
            //         if (EditorGUI.EndChangeCheck())
            //         {
            //             sphereData.localPosition = previewInstance.transform.InverseTransformPoint(newPosition);
            //             sphereData.radius = Mathf.Max(0f, newRadius);
            //             // 更新数据
            //             Bind(Data);
            //         }
            //     }
            //         break;
            //     case ActionCollideDetectionType.Sector:
            //     {
            //         var sectorData = data.Data as ActionCollideDetectionSectorData;
            //         var position = previewInstance.transform.TransformPoint(sectorData.localPosition);
            //         var rotation = previewInstance.transform.rotation * sectorData.localRotation;
            //         var scale = new Vector3(sectorData.angle, sectorData.height, sectorData.radius);
            //         EditorGUI.BeginChangeCheck();
            //         Handles.TransformHandle(ref position, ref rotation, ref scale);
            //         var insideRadiusHandle = Handles.ScaleSlider(
            //             sectorData.insideRadius == 0f ? 0.01f : sectorData.insideRadius, position,
            //             -previewInstance.transform.forward,
            //             Quaternion.identity, sectorData.insideRadius + 0.3f, 0.1f);
            //         if (EditorGUI.EndChangeCheck())
            //         {
            //             sectorData.localPosition = previewInstance.transform.InverseTransformPoint(position);
            //             sectorData.localRotation = Quaternion.Inverse(previewInstance.transform.rotation) * rotation;
            //             sectorData.angle = scale.x;
            //             sectorData.height = scale.y;
            //             sectorData.radius = Mathf.Max(0f, scale.z);
            //             sectorData.insideRadius = Mathf.Clamp(insideRadiusHandle, 0f, sectorData.radius);
            //             // 更新数据
            //             Bind(Data);
            //         }
            //     }
            //         break;
            // }
        }

        protected override void SynchronizeToInspector(ActionCollideDetectionTrackInspectorSO inspector)
        {
            base.SynchronizeToInspector(inspector);
            if (Data is ActionCollideDetectionTrackEditorData data)
            {
                inspector.groupId = data.GroupId;
                inspector.type = data.Type;
                switch (data.Data)
                {
                    case ActionCollideDetectionEmptyData emptyData:
                    {
                    }
                        break;
                    case ActionCollideDetectionBindData bindData:
                    {
                    }
                        break;
                    case ActionCollideDetectionBoxData boxData:
                    {
                        inspector.showInEditor = boxData.showInEditor;
                        inspector.localPosition = boxData.localPosition;
                        inspector.localRotation = boxData.localRotation.eulerAngles;
                        inspector.size = boxData.size;
                    }
                        break;
                    case ActionCollideDetectionSphereData sphereData:
                    {
                        inspector.showInEditor = sphereData.showInEditor;
                        inspector.localPosition = sphereData.localPosition;
                        inspector.radius = sphereData.radius;
                    }
                        break;
                    case ActionCollideDetectionSectorData sectorData:
                    {
                        inspector.showInEditor = sectorData.showInEditor;
                        inspector.localPosition = sectorData.localPosition;
                        inspector.localRotation = sectorData.localRotation.eulerAngles;
                        inspector.insideRadius = sectorData.insideRadius;
                        inspector.outsideRadius = sectorData.outsideRadius;
                        inspector.height = sectorData.height;
                        inspector.anglePivot = sectorData.anglePivot;
                        inspector.angle = sectorData.angle;
                    }
                        break;
                }
            }
        }

        protected override void SynchronizeToTrackData(ActionCollideDetectionTrackInspectorSO inspector)
        {
            base.SynchronizeToTrackData(inspector);
            if (Data is ActionCollideDetectionTrackEditorData data)
            {
                data.GroupId = inspector.groupId;
                data.Type = inspector.type;
                data.Data = inspector.type switch
                {
                    ActionCollideDetectionType.None => new ActionCollideDetectionEmptyData(),
                    ActionCollideDetectionType.Bind => new ActionCollideDetectionBindData(),
                    ActionCollideDetectionType.Box => new ActionCollideDetectionBoxData
                    {
                        showInEditor = inspector.showInEditor,
                        localPosition = inspector.localPosition,
                        localRotation = Quaternion.Euler(inspector.localRotation),
                        size = inspector.size,
                    },
                    ActionCollideDetectionType.Sphere => new ActionCollideDetectionSphereData
                    {
                        showInEditor = inspector.showInEditor,
                        localPosition = inspector.localPosition,
                        radius = inspector.radius,
                    },
                    ActionCollideDetectionType.Sector => new ActionCollideDetectionSectorData
                    {
                        showInEditor = inspector.showInEditor,
                        localPosition = inspector.localPosition,
                        localRotation = Quaternion.Euler(inspector.localRotation),
                        insideRadius = inspector.insideRadius,
                        outsideRadius = inspector.outsideRadius,
                        height = inspector.height,
                        anglePivot = inspector.anglePivot,
                        angle = inspector.angle,
                    },
                    _ => new ActionCollideDetectionEmptyData()
                };
            }
        }
    }
}