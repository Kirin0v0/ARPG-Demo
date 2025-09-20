using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using Framework.Common.Debug;
using Framework.Core.Attribute;
using UnityEditor;
using UnityEngine;

namespace Framework.Common.BehaviourTree
{
    public enum TreeState
    {
        Initialized,
        Running,
        Success,
        Failure,
    }

    [CreateAssetMenu(menuName = "Behaviour Tree")]
    public class BehaviourTree : ScriptableObject
    {
        [DisplayOnly] public Node.Node rootNode;
        [DisplayOnly] public TreeState treeState = TreeState.Initialized;
        [DisplayOnly] public List<Node.Node> nodes = new();
        [DisplayOnly] public Blackboard.Blackboard blackboard; // 这里允许在调试时动态更改内部数据

        [NonSerialized] public BehaviourTree Parent; // 运行时关联的父树，仅在子树节点中设置
        [NonSerialized] public float Time; // 运行时时间

#if UNITY_EDITOR
        public virtual void CreateBlackboard()
        {
            var blackboard = ScriptableObject.CreateInstance<Blackboard.Blackboard>();
            blackboard.name = "Blackboard";
            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(blackboard, this);
            }

            this.blackboard = blackboard;
            Dfs(rootNode, node => node.blackboard = blackboard);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public virtual Node.Node CreateRootNode()
        {
            return CreateNode(typeof(RootNode));
        }

        public Node.Node CreateNode(Type type)
        {
            Undo.RecordObject(this, "Behaviour Tree(Create Node)");
            var node = ScriptableObject.CreateInstance(type) as Node.Node;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            node.blackboard = blackboard;
            nodes.Add(node);

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            Undo.RegisterCreatedObjectUndo(node, "Behaviour Tree(Create Node)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        public void DeleteNode(Node.Node node)
        {
            Undo.RecordObject(this, "Behaviour Tree(Delete Node)");
            nodes.Remove(node);
            if (rootNode == node)
            {
                rootNode = null;
            }

            // AssetDatabase.RemoveObjectFromAsset(node);
            Undo.DestroyObjectImmediate(node);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public bool AddChildNode(Node.Node parent, Node.Node child)
        {
            Undo.RecordObject(parent, "Behaviour Tree(Add Child Node)");
            var result = parent.AddChildNode(child);
            EditorUtility.SetDirty(parent);
            return result;
        }

        public bool RemoveChildNode(Node.Node parent, Node.Node child)
        {
            Undo.RecordObject(parent, "Behaviour Tree(Remove Child Node)");
            var result = parent.RemoveChildNode(child);
            EditorUtility.SetDirty(parent);
            return result;
        }
#endif

        public List<Node.Node> GetChildNodes(Node.Node parent)
        {
            var childNodes = new List<Node.Node>();
            switch (parent)
            {
                case RootNode rootNode:
                    if (rootNode.child)
                    {
                        childNodes.Add(rootNode.child);
                    }

                    return childNodes;
                case DecoratorNode decoratorNode:
                    if (decoratorNode.child)
                    {
                        childNodes.Add(decoratorNode.child);
                    }

                    return childNodes;
                case CompositeNode compositeNode:
                    return compositeNode.children.Where(child => child != null).ToList();

                default:
                    return childNodes;
            }
        }

        public virtual TreeState Tick(float deltaTime, object payload)
        {
            Time += deltaTime;
            treeState = rootNode.Tick(deltaTime, payload) switch
            {
                NodeState.Running => TreeState.Running,
                NodeState.Failure => TreeState.Failure,
                NodeState.Success => TreeState.Success,
                _ => TreeState.Running
            };
            return treeState;
        }

        /// <summary>
        /// 克隆函数，用于多个组件同时执行同一文件的行为树时克隆数据
        /// 注意，这里仅克隆行为树流程的节点，那些不处于Root树中的节点存在不被克隆的情况
        /// </summary>
        /// <returns></returns>
        public BehaviourTree Clone()
        {
            var behaviourTree = Instantiate(this);
            behaviourTree.blackboard = Instantiate(blackboard);
            // 树节点克隆在这里从根节点递归克隆节点
            behaviourTree.rootNode = rootNode.Clone() as RootNode;
            // 这里节点列表采用DFS遍历获取流程的克隆节点
            var cloneNodes = new List<Node.Node>();
            Dfs(behaviourTree.rootNode, node =>
            {
                node.Tree = behaviourTree;
                node.blackboard = behaviourTree.blackboard;
                cloneNodes.Add(node);
            });
            behaviourTree.nodes = cloneNodes;
            return behaviourTree;
        }

        /// <summary>
        /// 销毁函数，用于彻底销毁自身及自身关联的节点资源
        /// </summary>
        public void Destroy()
        {
            // 销毁自身
            GameObject.Destroy(this);
            // 销毁黑板
            GameObject.Destroy(blackboard);
            // 销毁内部节点
            nodes.ForEach(GameObject.Destroy);
            nodes.Clear();
        }

        public void Dfs(Node.Node node, Action<Node.Node> visitor)
        {
            visitor.Invoke(node);
            GetChildNodes(node).ForEach(childNode => Dfs(childNode, visitor));
        }
    }
}