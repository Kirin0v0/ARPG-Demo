using System;
using System.Collections.Generic;
using Character;
using Framework.Common.Timeline;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Skill.Unit
{
    public enum SkillFlowNodePortCapacity
    {
        Single,
        Multiple,
    }

    public abstract class SkillFlowNode : SerializedScriptableObject
    {
        [ReadOnly] public Dictionary<string, List<SkillFlowNode>> outputChildren = new();
        [HideInInspector] public SkillFlow skillFlow;
        [Title("节点配置")] public string id;
        public virtual string RunningId => skillFlow.GetRunningId(id);

        // 运行时数据
        [NonSerialized] public CharacterObject Caster;
        [NonSerialized] [CanBeNull] public CharacterObject Target;

        [Inject] private TimelineManager _timelineManager;

        public TimelineManager TimelineManager
        {
            get
            {
                // 如果没有注入成功，就尝试去场景查找绑定该组件的游戏物体
                if (!_timelineManager)
                {
                    _timelineManager = GameEnvironment.FindEnvironmentComponent<TimelineManager>();
                }

                return _timelineManager;
            }
        }

        public abstract string Title { get; }

#if UNITY_EDITOR
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;
        [TextArea] public string comment;

        public virtual List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>();
        }

        public virtual bool AddChildNode(string key, SkillFlowNode child)
        {
            return false;
        }

        public virtual bool RemoveChildNode(string key, SkillFlowNode child)
        {
            return false;
        }

        protected void AddChildNodeInternal(string key, SkillFlowNode child)
        {
            if (outputChildren.TryGetValue(key, out var children))
            {
                children.Add(child);
            }
            else
            {
                children = new List<SkillFlowNode> { child };
                outputChildren.Add(key, children);
            }
        }

        protected bool RemoveChildNodeInternal(string key, SkillFlowNode child)
        {
            if (outputChildren.TryGetValue(key, out var children))
            {
                return children.Remove(child);
            }

            return false;
        }
#endif

        public virtual SkillFlowNode Clone()
        {
            var node = Instantiate(this);
            var newDictionary = new Dictionary<string, List<SkillFlowNode>>();
            outputChildren.ForEach(pair =>
            {
                newDictionary.Add(pair.Key, pair.Value.ConvertAll(child => child.Clone()));
            });
            node.outputChildren = newDictionary;
            return node;
        }

        public List<SkillFlowNode> GetChildNodes(string key)
        {
            if (outputChildren.TryGetValue(key, out var children))
            {
                return children;
            }
            else
            {
                return new List<SkillFlowNode>();
            }
        }

        public void Visit(Action<SkillFlowNode> visitor)
        {
            visitor.Invoke(this);
            outputChildren.Values.ForEach(children => children.ForEach(child => child.Visit(visitor)));
        }
    }

    [Serializable]
    public class SkillFlowNodePort
    {
        public string key; // 用于将端口与子节点关联的关键字
        public string title; // 输出端口标题
        public SkillFlowNodePortCapacity capacity; // 输出端口容量
    }
}