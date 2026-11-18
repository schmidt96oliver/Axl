using System.Runtime.InteropServices;

namespace AxlCompiler.Mir.Interpreter;

public sealed class Frame : IDisposable
{
    private readonly Dictionary<LocalId, object?> _locals = [];
    private bool _alive = true;

    /// <summary>
    /// Gets the ref to a local place. Returns ref to <c>null</c> object,
    /// if it has not yet been initialized.
    /// </summary>
    public ref object? GetRef(Place.Local local)
    {
        if (!_alive)
            throw new InvalidOperationException("Frame is not alive anymore.");

        return ref CollectionsMarshal.GetValueRefOrAddDefault(_locals, local.Id, out _);
    }

    public bool IsDefined(Place.Local local)
        => _locals.TryGetValue(local.Id, out var value) && value is not null;
    
    /// <inheritdoc />
    public void Dispose()
    {
        _alive = false;
    }
}