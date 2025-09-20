using System.Collections.Generic;
using System.Linq;
using Framework.DataStructure;
using UnityEngine;

namespace Framework.Algorithm.AStar
{
    public class AStarPathFinding
    {
        private enum NodeState
        {
            Unvisited,
            Opened,
            Closed,
        }

        /// <summary>
        /// 节点数据，动态数据，每次寻路开始时重置
        /// </summary>
        private class Node
        {
            // A星公式：F(寻路消耗)=G(起点代价)+H(终点代价)
            public float F;
            public float G;
            public float H;

            // 节点对应格子
            public AStarGridInfo Grid;

            // 节点当前状态，会随寻路阶段改变
            public NodeState State;

            // 父节点，会随寻路阶段改变
            public Node Parent;

            public void Reset()
            {
                F = 0;
                G = 0;
                H = 0;
                State = NodeState.Unvisited;
                Parent = null;
            }
        }

        /// <summary>
        /// 节点F值比较器，F值小的节点优先，如果相同则比较H值，H值小的优先
        /// </summary>
        private class NodeComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                if (y == null)
                {
                    return -1;
                }

                if (x == null)
                {
                    return 1;
                }

                if (x.F < y.F)
                {
                    return -1;
                }

                if (Mathf.Approximately(x.F, y.F))
                {
                    return x.H <= y.H ? -1 : 1;
                }

                return 1;
            }
        }

#if UNITY_EDITOR
        public class Step
        {
            private readonly AStarPathFinding _pathFinding;

            private readonly bool _hasNextStep;
            private readonly bool _findPath;
            public bool FindPath => _findPath;
            public bool IsDeadPath => !_hasNextStep && !_findPath;

            public readonly AStarGridInfo CurrentGrid;
            public readonly AStarGridInfo TargetGrid;
            public readonly List<AStarGridInfo> OpenedList;
            public readonly List<AStarGridInfo> ClosedList;
            public readonly List<AStarGridInfo> Path;

            public Step(
                AStarPathFinding pathFinding,
                bool hasNextStep,
                bool findPath,
                AStarGridInfo currentGrid,
                AStarGridInfo targetGrid,
                List<AStarGridInfo> openedList,
                List<AStarGridInfo> closedList,
                List<AStarGridInfo> path
            )
            {
                _pathFinding = pathFinding;
                _hasNextStep = hasNextStep;
                _findPath = findPath;
                CurrentGrid = currentGrid;
                TargetGrid = targetGrid;
                OpenedList = openedList;
                ClosedList = closedList;
                Path = path;
            }

            public bool Next(out Step step)
            {
                if (!_hasNextStep)
                {
                    step = null;
                    return false;
                }

                step = _pathFinding.FindPathStepByStepInternal(TargetGrid.Index);
                return step._hasNextStep;
            }
        }
#endif

        private readonly Node[,] _nodes; // 全部节点信息，寻路开始前重置
        private readonly PriorityQueue<Node> _openedList; // 开放格子列表，寻路开始前清空
        private readonly List<Node> _closedList; // 关闭格子列表，寻路开始前清空

        private readonly bool _allowDiagonalVisited; // 是否允许斜向访问
        private readonly IAStarDistanceEvaluate _distanceEvaluate; // 启发式函数评估接口

        public AStarPathFinding(AStarGridInfo[,] gridInfos, bool allowDiagonalVisited) : this(
            gridInfos,
            allowDiagonalVisited,
            allowDiagonalVisited ? new AStarEuclideanDistanceEvaluate() : new AStarManhattanDistanceEvaluate()
        )
        {
        }

        public AStarPathFinding(
            AStarGridInfo[,] gridInfos,
            bool allowDiagonalVisited,
            IAStarDistanceEvaluate distanceEvaluate
        )
        {
            _nodes = new Node[gridInfos.GetLength(0), gridInfos.GetLength(1)];
            for (var i = 0; i < gridInfos.GetLength(0); i++)
            {
                for (var j = 0; j < gridInfos.GetLength(1); j++)
                {
                    _nodes[i, j] = new Node
                    {
                        Grid = gridInfos[i, j]
                    };
                }
            }

            _openedList = new PriorityQueue<Node>(new NodeComparer(), 20);
            _closedList = new List<Node>();

            _allowDiagonalVisited = allowDiagonalVisited;
            _distanceEvaluate = distanceEvaluate;
        }

        public bool TryFindPath(Vector2Int start, Vector2Int target, out List<AStarGridInfo> path)
        {
            foreach (var node in _nodes)
            {
                node.Reset();
            }

            _openedList.Clear();
            _closedList.Clear();

            // 先判断起点和目标是否有效，存在无效则证明是死路，直接返回
            if (!IsValidGrid(start) || !IsValidGrid(target))
            {
                path = new List<AStarGridInfo>();
                return false;
            }

            // 将起点纳入开放列表
            var startNode = _nodes[start.x, start.y];
            _openedList.Enqueue(startNode);

            // 循环直到开放列表为空
            while (!_openedList.IsEmpty)
            {
                // 取出开放列表中F值最小的节点，并放入关闭列表
                var node = _openedList.Dequeue();
                node.State = NodeState.Closed;
                _closedList.Add(node);

                // 如果当前节点就是目标节点，则直接构建路径并返回
                if (node.Grid.Index == target)
                {
                    path = BuildPath(node);
                    return true;
                }

                // 获取当前节点的邻居节点
                GetNeighborNodes(node).ForEach(neighborNode =>
                {
                    // 如果邻居节点已是关闭状态，就不用处理
                    if (neighborNode.State == NodeState.Closed)
                    {
                        return;
                    }

                    // 计算G值
                    var newNeighborNodeG = node.G + Vector2Int.Distance(neighborNode.Grid.Index, node.Grid.Index);

                    // 如果邻居节点是开放状态，则比较当前计算的G值和先前的G值（起点代价），因为H值同一个节点是固定的，G值小的路径F值小
                    if (neighborNode.State == NodeState.Opened)
                    {
                        if (newNeighborNodeG >= neighborNode.G)
                        {
                            return;
                        }

                        // 更新G、F和父节点
                        neighborNode.G = newNeighborNodeG;
                        neighborNode.F = neighborNode.G + neighborNode.H;
                        neighborNode.Parent = node;
                        // 注意，这里需要重排序优先队列
                        _openedList.Sort();
                        return;
                    }

                    // 到这里的话说明是未访问过的节点，直接添加到开放列表中
                    neighborNode.G = newNeighborNodeG;
                    neighborNode.H = _distanceEvaluate.Evaluate(neighborNode.Grid.Index, target);
                    neighborNode.F = neighborNode.G + neighborNode.H;
                    neighborNode.Parent = node;
                    neighborNode.State = NodeState.Opened;
                    _openedList.Enqueue(neighborNode);
                });
            }

            // 存在死路，寻路失败
            path = new List<AStarGridInfo>();
            return false;
        }

#if UNITY_EDITOR
        public Step FindPathStepByStep(Vector2Int start, Vector2Int target)
        {
            foreach (var node in _nodes)
            {
                node.Reset();
            }

            _openedList.Clear();
            _closedList.Clear();

            // 先判断起点和目标是否有效，存在无效则证明是死路，直接返回
            if (!IsValidGrid(start) || !IsValidGrid(target))
            {
                return new Step(
                    this,
                    false,
                    false,
                    _nodes[start.x, start.y].Grid,
                    _nodes[target.x, target.y].Grid,
                    new List<AStarGridInfo>(),
                    new List<AStarGridInfo>(),
                    new List<AStarGridInfo>()
                );
            }

            // 将起点纳入开放列表，并返回第一步
            var startNode = _nodes[start.x, start.y];
            _openedList.Enqueue(startNode);
            return new Step(
                this,
                true,
                false,
                _nodes[start.x, start.y].Grid,
                _nodes[target.x, target.y].Grid,
                _openedList.Select(node => node.Grid).ToList(),
                _closedList.Select(node => node.Grid).ToList(),
                new List<AStarGridInfo> { _nodes[start.x, start.y].Grid }
            );
        }

        private Step FindPathStepByStepInternal(Vector2Int target)
        {
            // 如果开放列表为空，就是死亡，直接返回失败步骤
            if (_openedList.IsEmpty)
            {
                return new Step(
                    this,
                    false,
                    false,
                    default,
                    _nodes[target.x, target.y].Grid,
                    _openedList.Select(node => node.Grid).ToList(),
                    _closedList.Select(node => node.Grid).ToList(),
                    BuildPath(_closedList[^1])
                );
            }
            
            // 取出开放列表中F值最小的节点，并放入关闭列表
            var node = _openedList.Dequeue();
            node.State = NodeState.Closed;
            _closedList.Add(node);
            
            // 如果当前节点就是目标节点，则直接构建路径并返回
            if (node.Grid.Index == target)
            {
                return new Step(
                    this,
                    false,
                    true,
                    node.Grid,
                    _nodes[target.x, target.y].Grid,
                    _openedList.Select(node => node.Grid).ToList(),
                    _closedList.Select(node => node.Grid).ToList(),
                    BuildPath(_closedList[^1])
                );
            }
            
            // 获取当前节点的邻居节点
            GetNeighborNodes(node).ForEach(neighborNode =>
            {
                // 如果邻居节点已是关闭状态，就不用处理
                if (neighborNode.State == NodeState.Closed)
                {
                    return;
                }

                // 计算G值
                var newNeighborNodeG = node.G + Vector2Int.Distance(neighborNode.Grid.Index, node.Grid.Index);

                // 如果邻居节点是开放状态，则比较当前计算的G值和先前的G值（起点代价），因为H值同一个节点是固定的，G值小的路径F值小
                if (neighborNode.State == NodeState.Opened)
                {
                    if (newNeighborNodeG >= neighborNode.G)
                    {
                        return;
                    }

                    // 更新G、F和父节点
                    neighborNode.G = newNeighborNodeG;
                    neighborNode.F = neighborNode.G + neighborNode.H;
                    neighborNode.Parent = node;
                    // 注意，这里需要重排序优先队列
                    _openedList.Sort();
                    return;
                }

                // 到这里的话说明是未访问过的节点，直接添加到开放列表中
                neighborNode.G = newNeighborNodeG;
                neighborNode.H = _distanceEvaluate.Evaluate(neighborNode.Grid.Index, target);
                neighborNode.F = neighborNode.G + neighborNode.H;
                neighborNode.Parent = node;
                neighborNode.State = NodeState.Opened;
                _openedList.Enqueue(neighborNode);
            });

            // 默认认为是有下一步的
            return new Step(
                this,
                true,
                false,
                node.Grid,
                _nodes[target.x, target.y].Grid,
                _openedList.Select(node => node.Grid).ToList(),
                _closedList.Select(node => node.Grid).ToList(),
                BuildPath(_closedList[^1])
            );
        }
#endif

        private bool IsValidGrid(Vector2Int position)
        {
            // 排除超出范围的格子
            if (position.x < 0 || position.x >= _nodes.GetLength(0) || position.y < 0 ||
                position.y >= _nodes.GetLength(1))
            {
                return false;
            }

            // 再判断格子是否为障碍物，是则不认为是有效格子
            return !_nodes[position.x, position.y].Grid.Block;
        }

        private List<AStarGridInfo> BuildPath(Node node)
        {
            var path = new List<AStarGridInfo>();
            var currentNode = node;
            // 从子节点一直倒推到父节点
            while (currentNode != null)
            {
                path.Add(currentNode.Grid);
                currentNode = currentNode.Parent;
            }

            // 逆序
            path.Reverse();
            return path;
        }

        private List<Node> GetNeighborNodes(Node node)
        {
            var index = node.Grid.Index;
            var neighborNodes = new List<Node>();

            // 先获取正四向的邻居节点
            {
                if (IsValidGrid(new Vector2Int(index.x - 1, index.y)))
                {
                    neighborNodes.Add(_nodes[index.x - 1, index.y]);
                }

                if (IsValidGrid(new Vector2Int(index.x + 1, index.y)))
                {
                    neighborNodes.Add(_nodes[index.x + 1, index.y]);
                }

                if (IsValidGrid(new Vector2Int(index.x, index.y - 1)))
                {
                    neighborNodes.Add(_nodes[index.x, index.y - 1]);
                }

                if (IsValidGrid(new Vector2Int(index.x, index.y + 1)))
                {
                    neighborNodes.Add(_nodes[index.x, index.y + 1]);
                }
            }
            // 如果不支持斜向移动，就立即返回节点
            if (!_allowDiagonalVisited)
            {
                return neighborNodes;
            }

            // 再获取斜四向的邻居节点
            {
                if (IsValidGrid(new Vector2Int(index.x - 1, index.y - 1)))
                {
                    neighborNodes.Add(_nodes[index.x - 1, index.y - 1]);
                }

                if (IsValidGrid(new Vector2Int(index.x + 1, index.y - 1)))
                {
                    neighborNodes.Add(_nodes[index.x + 1, index.y - 1]);
                }

                if (IsValidGrid(new Vector2Int(index.x - 1, index.y + 1)))
                {
                    neighborNodes.Add(_nodes[index.x - 1, index.y + 1]);
                }

                if (IsValidGrid(new Vector2Int(index.x + 1, index.y + 1)))
                {
                    neighborNodes.Add(_nodes[index.x + 1, index.y + 1]);
                }
            }
            return neighborNodes;
        }
    }
}