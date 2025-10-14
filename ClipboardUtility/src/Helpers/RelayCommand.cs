using System.Windows.Input;

namespace ClipboardUtility.src.Helpers;

internal class RelayCommand : ICommand
{
    private readonly Action<object>? _executeWithParam;
    private readonly Action? _execute;
    private readonly Predicate<object>? _canExecute;
    private readonly Func<bool>? _canExecuteFunc;

    public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
    {
        _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteFunc = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter)
    {
        if (_canExecute != null)
        {
            return _canExecute(parameter);
        }
        if (_canExecuteFunc != null)
        {
            return _canExecuteFunc();
        }
        return true;
    }

    public void Execute(object parameter)
    {
        if (_executeWithParam != null)
        {
            _executeWithParam(parameter);
        }
        else
        {
            _execute?.Invoke();
        }
    }
}

