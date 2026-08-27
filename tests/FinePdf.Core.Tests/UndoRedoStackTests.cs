using FinePdf.Core.Operations;

namespace FinePdf.Core.Tests;

public class UndoRedoStackTests
{
    private sealed class CounterAction(Action onDo, Action onUndo, string description = "test") : IUndoableAction
    {
        public string Description { get; } = description;

        public void Do() => onDo();

        public void Undo() => onUndo();
    }

    [Fact]
    public void Execute_RunsAction_AndEnablesUndo()
    {
        var stack = new UndoRedoStack();
        int value = 0;

        stack.Execute(new CounterAction(() => value++, () => value--));

        Assert.Equal(1, value);
        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Undo_Then_Redo_RoundTrips()
    {
        var stack = new UndoRedoStack();
        int value = 0;
        var action = new CounterAction(() => value += 10, () => value -= 10, "add ten");

        stack.Execute(action);
        stack.Undo();

        Assert.Equal(0, value);
        Assert.True(stack.CanRedo);
        Assert.Equal("add ten", stack.NextRedoDescription);

        stack.Redo();

        Assert.Equal(10, value);
        Assert.True(stack.CanUndo);
    }

    [Fact]
    public void Execute_ClearsRedoHistory()
    {
        var stack = new UndoRedoStack();
        int value = 0;

        stack.Execute(new CounterAction(() => value++, () => value--));
        stack.Undo();
        Assert.True(stack.CanRedo);

        stack.Execute(new CounterAction(() => value += 5, () => value -= 5));

        Assert.False(stack.CanRedo);
        Assert.Equal(5, value);
    }

    [Fact]
    public void Changed_FiresOnEveryMutation()
    {
        var stack = new UndoRedoStack();
        int changed = 0;
        stack.Changed += (_, _) => changed++;

        stack.Execute(new CounterAction(() => { }, () => { }));
        stack.Undo();
        stack.Redo();
        stack.Clear();

        Assert.Equal(4, changed);
    }

    [Fact]
    public void Undo_WhenEmpty_IsNoOp()
    {
        var stack = new UndoRedoStack();
        int changed = 0;
        stack.Changed += (_, _) => changed++;

        stack.Undo();

        Assert.Equal(0, changed);
    }
}
