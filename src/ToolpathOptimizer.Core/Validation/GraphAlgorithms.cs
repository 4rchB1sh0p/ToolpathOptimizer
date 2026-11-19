namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Deterministic graph helpers over adjacency lists: Kahn acyclicity check and Tarjan strongly connected components.
/// </summary>
internal static class GraphAlgorithms
{
    /// <summary>Determines whether a directed graph has no cycle (Kahn's algorithm).</summary>
    /// <param name="nodeCount">Number of nodes.</param>
    /// <param name="successors">Successor indices per node.</param>
    /// <returns><c>true</c> when every node can be ordered topologically.</returns>
    public static bool IsAcyclic(int nodeCount, int[][] successors)
    {
        int[] indegree = new int[nodeCount];
        foreach (int[] targets in successors)
        {
            foreach (int target in targets)
            {
                indegree[target]++;
            }
        }

        var ready = new Queue<int>();
        for (int i = 0; i < nodeCount; i++)
        {
            if (indegree[i] == 0)
            {
                ready.Enqueue(i);
            }
        }

        int processedCount = 0;
        while (ready.Count > 0)
        {
            int node = ready.Dequeue();
            processedCount++;
            foreach (int successor in successors[node])
            {
                if (--indegree[successor] == 0)
                {
                    ready.Enqueue(successor);
                }
            }
        }

        return processedCount == nodeCount;
    }

    /// <summary>Finds the strongly connected components that contain a cycle (more than one node, or a self-loop).</summary>
    /// <param name="nodeCount">Number of nodes.</param>
    /// <param name="successors">Successor indices per node.</param>
    /// <returns>The cyclic components, each as a list of node indices.</returns>
    public static List<int[]> FindCyclicComponents(int nodeCount, int[][] successors) =>
        new TarjanSearch(nodeCount, successors).FindCyclicComponents();

    /// <summary>
    /// Iterative Tarjan search (no recursion, so large graphs cannot overflow the stack). One instance per search.
    /// </summary>
    private sealed class TarjanSearch
    {
        private const int Unvisited = -1;

        private readonly int[][] _successors;
        private readonly int[] _visitIndex;
        private readonly int[] _lowLink;
        private readonly bool[] _isOnComponentStack;
        private readonly Stack<int> _componentStack = new();
        private readonly int[] _callStackNodes;
        private readonly int[] _callStackChildCursor;
        private readonly List<int[]> _cyclicComponents = [];
        private int _callStackDepth;
        private int _nextVisitIndex;

        /// <summary>Creates a search over the given graph.</summary>
        /// <param name="nodeCount">Number of nodes.</param>
        /// <param name="successors">Successor indices per node.</param>
        public TarjanSearch(int nodeCount, int[][] successors)
        {
            _successors = successors;
            _visitIndex = new int[nodeCount];
            _lowLink = new int[nodeCount];
            _isOnComponentStack = new bool[nodeCount];
            _callStackNodes = new int[nodeCount];
            _callStackChildCursor = new int[nodeCount];
            Array.Fill(_visitIndex, Unvisited);
        }

        /// <summary>Runs the search from every unvisited node.</summary>
        /// <returns>The cyclic components.</returns>
        public List<int[]> FindCyclicComponents()
        {
            for (int root = 0; root < _visitIndex.Length; root++)
            {
                if (_visitIndex[root] != Unvisited)
                {
                    continue;
                }

                EnterNode(root);
                while (_callStackDepth > 0)
                {
                    AdvanceSearch();
                }
            }

            return _cyclicComponents;
        }

        private void EnterNode(int node)
        {
            _visitIndex[node] = _lowLink[node] = _nextVisitIndex++;
            _componentStack.Push(node);
            _isOnComponentStack[node] = true;
            _callStackNodes[_callStackDepth] = node;
            _callStackChildCursor[_callStackDepth] = 0;
            _callStackDepth++;
        }

        private void AdvanceSearch()
        {
            int top = _callStackDepth - 1;
            int node = _callStackNodes[top];
            int[] children = _successors[node];
            if (_callStackChildCursor[top] >= children.Length)
            {
                LeaveNode(node);
                return;
            }

            int child = children[_callStackChildCursor[top]++];
            if (_visitIndex[child] == Unvisited)
            {
                EnterNode(child);
            }
            else if (_isOnComponentStack[child])
            {
                _lowLink[node] = Math.Min(_lowLink[node], _visitIndex[child]);
            }
        }

        private void LeaveNode(int node)
        {
            _callStackDepth--;
            if (_callStackDepth > 0)
            {
                int parent = _callStackNodes[_callStackDepth - 1];
                _lowLink[parent] = Math.Min(_lowLink[parent], _lowLink[node]);
            }

            if (_lowLink[node] == _visitIndex[node])
            {
                CollectComponent(node);
            }
        }

        private void CollectComponent(int rootNode)
        {
            var members = new List<int>();
            int member;
            do
            {
                member = _componentStack.Pop();
                _isOnComponentStack[member] = false;
                members.Add(member);
            }
            while (member != rootNode);

            bool hasSelfLoop = Array.IndexOf(_successors[rootNode], rootNode) >= 0;
            if (members.Count > 1 || hasSelfLoop)
            {
                _cyclicComponents.Add(members.ToArray());
            }
        }
    }
}
