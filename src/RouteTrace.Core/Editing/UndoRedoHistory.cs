namespace RouteTrace.Core.Editing;

public sealed class UndoRedoHistory<T>(T initialState)
{
    private readonly Stack<T> undo = [];
    private readonly Stack<T> redo = [];

    public T Current { get; private set; } = initialState;

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public void Apply(T state)
    {
        undo.Push(Current);
        Current = state;
        redo.Clear();
    }

    public bool TryUndo()
    {
        if (!undo.TryPop(out T? state)) return false;

        redo.Push(Current);
        Current = state;
        return true;
    }

    public bool TryRedo()
    {
        if (!redo.TryPop(out T? state)) return false;

        undo.Push(Current);
        Current = state;
        return true;
    }
}
