using System.Windows.Input;

namespace MAMEUtility.Commands;

public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Predicate<object?>? _canExecute = canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(
            p => execute(),
            canExecute == null ? null : p => canExecute())
    {
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }
}

public class RelayCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Predicate<T?>? _canExecute = canExecute;

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter is T t ? t : default);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter is T t ? t : default);
    }
}