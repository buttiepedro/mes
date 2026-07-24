namespace Nexo.WorkModel.Domain;

/// <summary>
/// Pure graph algorithms over the precedences of a <see cref="ProcessVersion"/>: cycle detection,
/// start/terminal nodes and reachability.
/// </summary>
/// <remarks>
/// <b>Where the DAG is defended (docs/design/03-data-schema.md §2.6.3).</b> The design describes three
/// barriers: B1 the trivial edge (row CHECK), B2 a deferrable constraint trigger with a recursive CTE
/// and B3 the integral validation at publish time. This slice implements <b>B1 and B2 in the
/// domain</b> (the traversal below, run on every <see cref="ProcessVersion.SetGraph"/>) and <b>B3 in
/// <see cref="ProcessVersion.Validate"/></b>. The database trigger <c>work.assert_task_dag_acyclic</c>
/// is <b>pending</b>: it needs raw SQL in a migration and this slice generates no migrations.
/// </remarks>
public static class TaskGraph
{
    /// <summary>
    /// Depth-first cycle detection. Returns the nodes of the first cycle found, closed on itself
    /// (<c>A, B, C, A</c>), or <c>null</c> when the graph is acyclic.
    /// </summary>
    /// <remarks>
    /// Iterative (explicit stack) so a pathological graph cannot blow the CLR stack; the RNF ceiling
    /// is ~200 tasks per process, but the traversal is O(V+E) and does not care.
    /// </remarks>
    public static IReadOnlyList<Guid>? FindCycle(
        IEnumerable<Guid> nodes,
        IEnumerable<(Guid From, Guid To)> edges)
    {
        var adjacency = BuildAdjacency(nodes, edges);

        // 0 = unvisited, 1 = on the current path (grey), 2 = fully explored (black).
        var state = new Dictionary<Guid, int>();
        var parent = new Dictionary<Guid, Guid>();

        foreach (var node in adjacency.Keys)
        {
            state[node] = 0;
        }

        foreach (var root in adjacency.Keys)
        {
            if (state[root] != 0)
            {
                continue;
            }

            var stack = new Stack<(Guid Node, IEnumerator<Guid> Successors)>();
            state[root] = 1;
            stack.Push((root, adjacency[root].GetEnumerator()));

            while (stack.Count > 0)
            {
                var (node, successors) = stack.Peek();

                if (!successors.MoveNext())
                {
                    state[node] = 2;
                    stack.Pop();
                    continue;
                }

                var next = successors.Current;

                if (state[next] == 1)
                {
                    return BuildCyclePath(next, node, parent);
                }

                if (state[next] == 0)
                {
                    state[next] = 1;
                    parent[next] = node;
                    stack.Push((next, adjacency[next].GetEnumerator()));
                }
            }
        }

        return null;
    }

    /// <summary>Nodes with no predecessor: the entry points of the graph (G3).</summary>
    public static IReadOnlyList<Guid> FindStartNodes(
        IEnumerable<Guid> nodes,
        IEnumerable<(Guid From, Guid To)> edges)
    {
        var withPredecessor = edges.Select(edge => edge.To).ToHashSet();

        return nodes.Where(node => !withPredecessor.Contains(node)).ToArray();
    }

    /// <summary>Nodes with no successor: the terminal points of the graph (G3).</summary>
    public static IReadOnlyList<Guid> FindTerminalNodes(
        IEnumerable<Guid> nodes,
        IEnumerable<(Guid From, Guid To)> edges)
    {
        var withSuccessor = edges.Select(edge => edge.From).ToHashSet();

        return nodes.Where(node => !withSuccessor.Contains(node)).ToArray();
    }

    /// <summary>
    /// Nodes that cannot be reached from any start node (G2). On a cyclic graph the whole cycle is
    /// reported as unreachable, which is why G1 is evaluated first.
    /// </summary>
    public static IReadOnlyList<Guid> FindUnreachableNodes(
        IEnumerable<Guid> nodes,
        IEnumerable<(Guid From, Guid To)> edges)
    {
        var materializedNodes = nodes as IReadOnlyCollection<Guid> ?? nodes.ToArray();
        var materializedEdges = edges as IReadOnlyCollection<(Guid From, Guid To)> ?? edges.ToArray();

        var adjacency = BuildAdjacency(materializedNodes, materializedEdges);
        var visited = new HashSet<Guid>();
        var pending = new Queue<Guid>(FindStartNodes(materializedNodes, materializedEdges));

        while (pending.Count > 0)
        {
            var node = pending.Dequeue();

            if (!visited.Add(node))
            {
                continue;
            }

            foreach (var successor in adjacency[node])
            {
                if (!visited.Contains(successor))
                {
                    pending.Enqueue(successor);
                }
            }
        }

        return materializedNodes.Where(node => !visited.Contains(node)).ToArray();
    }

    private static Dictionary<Guid, List<Guid>> BuildAdjacency(
        IEnumerable<Guid> nodes,
        IEnumerable<(Guid From, Guid To)> edges)
    {
        var adjacency = new Dictionary<Guid, List<Guid>>();

        foreach (var node in nodes)
        {
            adjacency.TryAdd(node, new List<Guid>());
        }

        foreach (var edge in edges)
        {
            adjacency.TryAdd(edge.From, new List<Guid>());
            adjacency.TryAdd(edge.To, new List<Guid>());
            adjacency[edge.From].Add(edge.To);
        }

        return adjacency;
    }

    /// <summary>Walks the parent chain back from <paramref name="from"/> to <paramref name="target"/>.</summary>
    private static IReadOnlyList<Guid> BuildCyclePath(Guid target, Guid from, IReadOnlyDictionary<Guid, Guid> parent)
    {
        var path = new List<Guid> { from };
        var current = from;

        while (current != target && parent.TryGetValue(current, out var previous))
        {
            path.Add(previous);
            current = previous;
        }

        path.Reverse();
        path.Add(target);

        return path;
    }
}
