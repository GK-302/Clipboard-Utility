using System;
using System.Windows.Input;

namespace ClipboardUtility.src.Common;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // WPFのコマンドマネージャーにイベントを委譲することで、
    // ViewModelのプロパティ変更時に自動的にボタンの有効/無効が切り替わります
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    // 手動で強制的に更新したい場合に使用（通常はCommandManagerが自動で行うため不要ですが、互換性のために残すなら空実装かinvalidate呼び出し）
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}