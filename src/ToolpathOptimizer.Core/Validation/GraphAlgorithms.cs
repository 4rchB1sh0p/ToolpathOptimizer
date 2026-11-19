namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Deterministic graph helpers: Kahn acyclicity check and iterative Tarjan strongly connected components.
/// </summary>
internal static class GraphAlgorithms
{
    /// <summary>Returns <c>true</c> when the directed graph has no cycle (Kahn's algorithm).</summary>
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
        for (int v = 0; v < nodeCount; v++)
        {
            if (indegree[v] == 0)
            {
                ready.Enqueue(v);
            }
        }

        int processed = 0;
        while (ready.Count > 0)
        {
            int v = ready.Dequeue();
            processed++;
            foreach (int w in successors[v])
            {
                if (--indegree[w] == 0)
                {
                    ready.Enqueue(w);
                }
            }
        }

        return processed == nodeCount;
    }

    /// <summary>Finds components that contain a cycle (size &gt; 1, or a self-loop). Iterative to avoid deep recursion.</summary>
    public static List<int[]> FindCyclicComponents(int nodeCount, int[][] successors)
    {
        var components = new List<int[]>();
        int[] index = new int[nodeCount];
        int[] low = new int[nodeCount];
        bool[] onStack = new bool[nodeCount];
        Array.Fill(index, -1);
        var sccStack = new Stack<int>();
        int[] callNode = new int[nodeCount];
        int[] callChild = new int[nodeCount];
        int counter = 0;

        for (int root = 0; root < nodeCount; root++)
        {
            if (index[root] != -1)
            {
                continue;
            }

            int depth = 0;
            Visit(root);
            while (depth > 0)
            {
                int u = callNode[depth - 1];
                int[] succ = successors[u];
                if (callChild[depth - 1] < succ.Length)
                {
                    int w = succ[callChild[depth - 1]++];
                    if (index[w] == -1)
                    {
                        Visit(w);
                    }
                    else if (onStack[w])
                    {
                        low[u] = Math.Min(low[u], index[w]);
                    }

                    continue;
                }

                depth--;
                if (depth > 0)
                {
                    int parent = callNode[depth - 1];
                    low[parent] = Math.Min(low[parent], low[u]);
                }

                if (low[u] == index[u])
                {
                    var members = new List<int>();
                    int w;
                    do
                    {
                        w = sccStack.Pop();
                        onStack[w] = false;
                        members.Add(w);
                    }
                    while (w != u);

                    if (members.Count > 1 || Array.IndexOf(successors[u], u) >= 0)
                    {
                        components.Add(members.ToArray());
                    }
                }
            }

            void Visit(int v)
            {
                index[v] = low[v] = counter++;
                sccStack.Push(v);
                onStack[v] = true;
                callNode[depth] = v;
                callChild[depth] = 0;
                depth++;
            }
        }

        return components;
    }
}
