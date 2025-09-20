using System;
using UnityEditor;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NodeMenuItem : Attribute
    {
        public string ItemName;

        public NodeMenuItem(string itemName)
        {
            ItemName = itemName;
        }
    }
}