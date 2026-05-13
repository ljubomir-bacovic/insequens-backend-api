using System.Runtime.Serialization;

namespace Insequens.Core.Exceptions;

[Serializable]
public class ResourceForbiddenException : Exception
{
    public Guid Id { get; }

    public ResourceForbiddenException(Guid id)
        : base($"Access denied for resource {id}.")
    {
        Id = id;
    }

    [Obsolete]
    protected ResourceForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Id = (Guid)(info.GetValue(nameof(Id), typeof(Guid)) ?? Guid.Empty);
    }

    [Obsolete]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ArgumentNullException.ThrowIfNull(info);

        info.AddValue(nameof(Id), Id);
        base.GetObjectData(info, context);
    }
}
