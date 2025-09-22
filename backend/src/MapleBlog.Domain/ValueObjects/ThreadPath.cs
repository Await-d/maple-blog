using System.Text.Json;

namespace MapleBlog.Domain.ValueObjects;

/// <summary>
/// 评论线程路径值对象
/// 用于高效地管理和查询嵌套评论结构
/// </summary>
public record ThreadPath
{
    private const int MaxDepth = 10;
    private const string PathSeparator = "/";

    /// <summary>
    /// 路径字符串（如 "root/child1/child2"）
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// 层级深度
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// 路径中的所有节点ID
    /// </summary>
    public IReadOnlyList<Guid> NodeIds { get; init; }

    /// <summary>
    /// 根节点ID
    /// </summary>
    public Guid RootId { get; init; }

    /// <summary>
    /// 父节点ID（如果是根节点则为null）
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 当前节点ID
    /// </summary>
    public Guid CurrentId { get; init; }

    /// <summary>
    /// 私有构造函数，强制使用工厂方法创建
    /// </summary>
    private ThreadPath(string path, int depth, IReadOnlyList<Guid> nodeIds,
        Guid rootId, Guid? parentId, Guid currentId)
    {
        Path = path;
        Depth = depth;
        NodeIds = nodeIds;
        RootId = rootId;
        ParentId = parentId;
        CurrentId = currentId;
    }

    /// <summary>
    /// 创建根线程路径
    /// </summary>
    /// <param name="rootId">根节点ID</param>
    /// <returns>根线程路径</returns>
    public static ThreadPath CreateRoot(Guid rootId)
    {
        return new ThreadPath(
            path: rootId.ToString(),
            depth: 0,
            nodeIds: new[] { rootId },
            rootId: rootId,
            parentId: null,
            currentId: rootId
        );
    }

    /// <summary>
    /// 从现有路径创建子路径
    /// </summary>
    /// <param name="parentPath">父路径</param>
    /// <param name="childId">子节点ID</param>
    /// <returns>子线程路径</returns>
    /// <exception cref="InvalidOperationException">当层级超过最大深度时抛出</exception>
    public static ThreadPath CreateChild(ThreadPath parentPath, Guid childId)
    {
        if (parentPath.Depth >= MaxDepth)
            throw new InvalidOperationException($"评论嵌套层级不能超过{MaxDepth}层");

        var newNodeIds = parentPath.NodeIds.Concat(new[] { childId }).ToList();
        var newPath = $"{parentPath.Path}{PathSeparator}{childId}";

        return new ThreadPath(
            path: newPath,
            depth: parentPath.Depth + 1,
            nodeIds: newNodeIds,
            rootId: parentPath.RootId,
            parentId: parentPath.CurrentId,
            currentId: childId
        );
    }

    /// <summary>
    /// 从当前路径创建子路径（便于使用的实例方法）
    /// </summary>
    /// <param name="childId">子节点ID</param>
    /// <returns>子线程路径</returns>
    public ThreadPath CreateChildPath(Guid childId)
    {
        return CreateChild(this, childId);
    }

    /// <summary>
    /// 从路径字符串创建线程路径
    /// </summary>
    /// <param name="pathString">路径字符串</param>
    /// <returns>线程路径</returns>
    /// <exception cref="ArgumentException">当路径字符串无效时抛出</exception>
    public static ThreadPath FromString(string pathString)
    {
        if (string.IsNullOrWhiteSpace(pathString))
            throw new ArgumentException("路径字符串不能为空", nameof(pathString));

        var parts = pathString.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            throw new ArgumentException("路径字符串格式无效", nameof(pathString));

        if (parts.Length > MaxDepth + 1)
            throw new ArgumentException($"路径层级不能超过{MaxDepth + 1}层", nameof(pathString));

        var nodeIds = new List<Guid>();
        foreach (var part in parts)
        {
            if (!Guid.TryParse(part, out var nodeId))
                throw new ArgumentException($"路径中包含无效的GUID: {part}", nameof(pathString));

            nodeIds.Add(nodeId);
        }

        var rootId = nodeIds[0];
        var currentId = nodeIds[^1];
        var parentId = nodeIds.Count > 1 ? nodeIds[^2] : (Guid?)null;
        var depth = nodeIds.Count - 1;

        return new ThreadPath(
            path: pathString,
            depth: depth,
            nodeIds: nodeIds,
            rootId: rootId,
            parentId: parentId,
            currentId: currentId
        );
    }

    /// <summary>
    /// 获取父路径
    /// </summary>
    /// <returns>父路径，如果是根路径则返回null</returns>
    public ThreadPath? GetParent()
    {
        if (IsRoot())
            return null;

        var parentNodeIds = NodeIds.Take(NodeIds.Count - 1).ToList();
        var parentPath = string.Join(PathSeparator, parentNodeIds);

        return new ThreadPath(
            path: parentPath,
            depth: Depth - 1,
            nodeIds: parentNodeIds,
            rootId: RootId,
            parentId: parentNodeIds.Count > 1 ? parentNodeIds[^2] : (Guid?)null,
            currentId: parentNodeIds[^1]
        );
    }

    /// <summary>
    /// 获取根路径
    /// </summary>
    /// <returns>根路径</returns>
    public ThreadPath GetRoot()
    {
        if (IsRoot())
            return this;

        return CreateRoot(RootId);
    }

    /// <summary>
    /// 检查是否为根路径
    /// </summary>
    /// <returns>是否为根路径</returns>
    public bool IsRoot()
    {
        return Depth == 0;
    }

    /// <summary>
    /// 检查是否为叶子路径
    /// </summary>
    /// <returns>是否为叶子路径</returns>
    public bool IsLeaf()
    {
        return Depth == MaxDepth;
    }

    /// <summary>
    /// 检查是否为指定路径的祖先
    /// </summary>
    /// <param name="other">要检查的路径</param>
    /// <returns>是否为祖先</returns>
    public bool IsAncestorOf(ThreadPath other)
    {
        if (other.Depth <= Depth)
            return false;

        return other.Path.StartsWith($"{Path}{PathSeparator}", StringComparison.Ordinal);
    }

    /// <summary>
    /// 检查是否为指定路径的后代
    /// </summary>
    /// <param name="other">要检查的路径</param>
    /// <returns>是否为后代</returns>
    public bool IsDescendantOf(ThreadPath other)
    {
        return other.IsAncestorOf(this);
    }

    /// <summary>
    /// 检查是否为指定路径的直接子路径
    /// </summary>
    /// <param name="other">要检查的路径</param>
    /// <returns>是否为直接子路径</returns>
    public bool IsDirectChildOf(ThreadPath other)
    {
        return ParentId == other.CurrentId && Depth == other.Depth + 1;
    }

    /// <summary>
    /// 检查是否为指定路径的直接父路径
    /// </summary>
    /// <param name="other">要检查的路径</param>
    /// <returns>是否为直接父路径</returns>
    public bool IsDirectParentOf(ThreadPath other)
    {
        return other.IsDirectChildOf(this);
    }

    /// <summary>
    /// 检查是否在同一线程中
    /// </summary>
    /// <param name="other">要检查的路径</param>
    /// <returns>是否在同一线程中</returns>
    public bool IsInSameThread(ThreadPath other)
    {
        return RootId == other.RootId;
    }

    /// <summary>
    /// 获取与指定路径的共同祖先路径
    /// </summary>
    /// <param name="other">要比较的路径</param>
    /// <returns>共同祖先路径，如果不在同一线程则返回null</returns>
    public ThreadPath? GetCommonAncestor(ThreadPath other)
    {
        if (!IsInSameThread(other))
            return null;

        var minDepth = Math.Min(NodeIds.Count, other.NodeIds.Count);
        var commonNodes = new List<Guid>();

        for (int i = 0; i < minDepth; i++)
        {
            if (NodeIds[i] == other.NodeIds[i])
                commonNodes.Add(NodeIds[i]);
            else
                break;
        }

        if (commonNodes.Count == 0)
            return null;

        var commonPath = string.Join(PathSeparator, commonNodes);
        return FromString(commonPath);
    }

    /// <summary>
    /// 获取所有祖先路径
    /// </summary>
    /// <returns>祖先路径列表（从根到直接父级）</returns>
    public IEnumerable<ThreadPath> GetAncestors()
    {
        var ancestors = new List<ThreadPath>();

        for (int i = 1; i <= Depth; i++)
        {
            var ancestorNodes = NodeIds.Take(i).ToList();
            var ancestorPath = string.Join(PathSeparator, ancestorNodes);
            ancestors.Add(FromString(ancestorPath));
        }

        return ancestors;
    }

    /// <summary>
    /// 获取所有祖先ID
    /// </summary>
    /// <returns>祖先ID列表（不包括当前节点）</returns>
    public IEnumerable<Guid> GetAncestorIds()
    {
        return NodeIds.Take(NodeIds.Count - 1);
    }

    /// <summary>
    /// 生成用于数据库查询的LIKE模式
    /// </summary>
    /// <returns>用于查询后代的LIKE模式</returns>
    public string GetDescendantQueryPattern()
    {
        return $"{Path}{PathSeparator}%";
    }

    /// <summary>
    /// 生成用于数据库查询祖先的模式
    /// </summary>
    /// <returns>用于查询祖先的模式列表</returns>
    public IEnumerable<string> GetAncestorQueryPatterns()
    {
        var patterns = new List<string>();

        for (int i = 1; i <= Depth; i++)
        {
            var ancestorNodes = NodeIds.Take(i);
            patterns.Add(string.Join(PathSeparator, ancestorNodes));
        }

        return patterns;
    }

    /// <summary>
    /// 计算到另一个路径的距离
    /// </summary>
    /// <param name="other">目标路径</param>
    /// <returns>距离，如果不在同一线程则返回-1</returns>
    public int DistanceTo(ThreadPath other)
    {
        if (!IsInSameThread(other))
            return -1;

        var commonAncestor = GetCommonAncestor(other);
        if (commonAncestor == null)
            return -1;

        return (Depth - commonAncestor.Depth) + (other.Depth - commonAncestor.Depth);
    }

    /// <summary>
    /// 获取用于排序的权重
    /// 根据路径结构生成排序权重，确保层次化显示
    /// </summary>
    /// <returns>排序权重</returns>
    public string GetSortWeight()
    {
        // 将每个节点ID转换为固定长度的字符串以确保正确排序
        var weightParts = NodeIds.Select(id => id.ToString("N")).ToArray();
        return string.Join("-", weightParts);
    }

    /// <summary>
    /// 转换为JSON表示
    /// </summary>
    /// <returns>JSON字符串</returns>
    public string ToJson()
    {
        var data = new
        {
            Path,
            Depth,
            NodeIds,
            RootId,
            ParentId,
            CurrentId
        };

        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// 从JSON创建线程路径
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>线程路径</returns>
    public static ThreadPath FromJson(string json)
    {
        var data = JsonSerializer.Deserialize<dynamic>(json);
        return FromString(data.GetProperty("Path").GetString()!);
    }

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    /// <returns>路径字符串</returns>
    public override string ToString()
    {
        return Path;
    }
}