using System.Windows.Input;

namespace MAMEUtility.Commands;

/// <inheritdoc />
/// <summary>
/// A command whose sole purpose is to relay its functionality to other
/// objects by invoking delegates
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="execute">Action to execute</param>
    /// <param name="canExecute">Function to determine if command can execute</param>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    /// <summary>
    /// Constructor with parameterless execute action
    /// </summary>
    /// <param name="execute">Action to execute</param>
    /// <param name="canExecute">Function to determine if command can execute</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(
            p => execute(),
            canExecute == null ? null : p => canExecute())
    {
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _execute(parameter);
    }
}

/// <inheritdoc />
/// <summary>
/// A generic command with a typed parameter
/// </summary>
/// <typeparam name="T">Type of the command parameter</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="execute">Action to execute</param>
    /// <param name="canExecute">Function to determine if command can execute</param>
    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter is T t ? t : default);
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _execute(parameter is T t ? t : default);
    }
}