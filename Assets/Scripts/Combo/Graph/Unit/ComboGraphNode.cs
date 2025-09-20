using System;
using Combo.Blackboard;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combo.Graph.Unit
{
    public abstract class ComboGraphNode : ScriptableObject, IComboGraphNode
    {
        [HideInInspector] public string guid;
        [FormerlySerializedAs("position")] [HideInInspector] public Rect rect;
        
        [NonSerialized] public ComboGraph Graph;
        public bool Played { protected set; get; } = false;
        public bool Playing { protected set; get; }

        protected float DeltaTime { private set; get; }

        public void Enter(IComboGraphNode from, ComboGraphTransition transition, ComboGraphParameters parameters)
        {
            Played = true;
            Playing = true;
            OnEnter(from, transition, parameters);
        }

        public void Tick(float deltaTime, ComboGraphParameters parameters)
        {
            DeltaTime = deltaTime;
            OnTick(deltaTime, parameters);
        }

        public void Exit(IComboGraphNode to, ComboGraphTransition transition, ComboGraphParameters parameters)
        {
            OnExit(to, transition, parameters);
            Playing = false;
        }

        public void Abort(ComboGraphParameters parameters)
        {
            OnAbort(parameters);
            Playing = false;
        }

        protected virtual void OnEnter([CanBeNull] IComboGraphNode from, [CanBeNull] ComboGraphTransition transition,
            ComboGraphParameters parameters)
        {
        }

        protected virtual void OnTick(float deltaTime, ComboGraphParameters parameters)
        {
        }

        protected virtual void OnExit([CanBeNull] IComboGraphNode to, [CanBeNull] ComboGraphTransition transition,
            ComboGraphParameters parameters)
        {
        }

        protected virtual  void OnAbort(ComboGraphParameters parameters)
        {
        }
    }
}