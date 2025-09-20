using System.Linq;
using Combo.Blackboard;
using Combo.Graph.Unit;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Combo.Graph
{
    public class ComboGraphExecutor : MonoBehaviour
    {
        [Inject] private IObjectResolver _objectResolver;

        [FormerlySerializedAs("graph")] [SerializeField]
        public ComboGraph graphTemplate;

        private ComboGraph _runningGraph;
        [ShowInInspector] public ComboGraph Graph => _runningGraph;

        public void Init()
        {
            _runningGraph = graphTemplate?.Clone(_objectResolver) ?? null;
        }

        public void Destroy()
        {
            if (_runningGraph)
            {
                _runningGraph.Destroy();
                _runningGraph = null;
            }
        }

        public void UseBlackboard(System.Action<ComboBlackboard> callback)
        {
            if (_runningGraph == null)
            {
                return;
            }

            callback.Invoke(_runningGraph.blackboard);
        }

        public ComboGraphState Tick(float deltaTime, ComboGraphParameters parameters)
        {
            return _runningGraph?.Tick(deltaTime, parameters) ?? ComboGraphState.Finish;
        }

        public void Abort(ComboGraphParameters parameters)
        {
            _runningGraph?.Abort(parameters);
        }
    }
}