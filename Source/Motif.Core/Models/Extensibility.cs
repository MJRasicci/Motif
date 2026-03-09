namespace Motif.Models;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interface identifies typed model extensions attached to domain nodes.")]
public interface IModelExtension
{
}

public interface IExtensibleModel
{
    bool TryGetExtension<TExtension>([NotNullWhen(true)] out TExtension? extension)
        where TExtension : class, IModelExtension;

    IReadOnlyCollection<IModelExtension> GetExtensions();

    void SetExtension<TExtension>(TExtension extension)
        where TExtension : class, IModelExtension;

    bool RemoveExtension<TExtension>()
        where TExtension : class, IModelExtension;
}

public abstract class ExtensibleModel : IExtensibleModel
{
    private static readonly IReadOnlyCollection<IModelExtension> EmptyExtensions = Array.Empty<IModelExtension>();
    private Dictionary<Type, IModelExtension>? extensions;

    public bool TryGetExtension<TExtension>([NotNullWhen(true)] out TExtension? extension)
        where TExtension : class, IModelExtension
    {
        if (extensions is not null && extensions.TryGetValue(typeof(TExtension), out var candidate))
        {
            extension = (TExtension)candidate;
            return true;
        }

        extension = default;
        return false;
    }

    public IReadOnlyCollection<IModelExtension> GetExtensions()
        => extensions is { Count: > 0 }
            ? extensions.Values.ToArray()
            : EmptyExtensions;

    public void SetExtension<TExtension>(TExtension extension)
        where TExtension : class, IModelExtension
    {
        ArgumentNullException.ThrowIfNull(extension);

        extensions ??= [];
        extensions[typeof(TExtension)] = extension;
    }

    public bool RemoveExtension<TExtension>()
        where TExtension : class, IModelExtension
        => extensions is not null && extensions.Remove(typeof(TExtension));
}

public static class ExtensibleModelExtensions
{
    public static TExtension? GetExtension<TExtension>(this IExtensibleModel model)
        where TExtension : class, IModelExtension
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.TryGetExtension<TExtension>(out var extension)
            ? extension
            : null;
    }

    public static TExtension GetRequiredExtension<TExtension>(this IExtensibleModel model)
        where TExtension : class, IModelExtension
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.TryGetExtension<TExtension>(out var extension)
            ? extension
            : throw new InvalidOperationException($"Extension '{typeof(TExtension).FullName}' is not attached to '{model.GetType().FullName}'.");
    }
}
