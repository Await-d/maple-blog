namespace MapleBlog.Domain.Events;

/// <summary>
/// 领域事件接口
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// 事件名称
    /// </summary>
    string EventName { get; }

    /// <summary>
    /// 事件版本
    /// </summary>
    int Version { get; }
}

/// <summary>
/// 领域事件基类
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// 事件名称
    /// </summary>
    public abstract string EventName { get; }

    /// <summary>
    /// 事件版本
    /// </summary>
    public virtual int Version => 1;
}