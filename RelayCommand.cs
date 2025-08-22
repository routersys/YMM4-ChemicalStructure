using System;
using System.Windows.Input;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        private readonly string _name;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null, string name = "")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = string.IsNullOrEmpty(name) ? "RelayCommand" : name;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return _canExecute?.Invoke(parameter) ?? true;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.CanExecute: {ex.Message}");
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (CanExecute(parameter))
                {
                    _execute(parameter);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.Execute: {ex.Message}");

                System.Windows.MessageBox.Show(
                    $"コマンド実行中にエラーが発生しました: {_name}\n詳細: {ex.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            try
            {
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error raising CanExecuteChanged for {_name}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"RelayCommand: {_name}";
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        private readonly string _name;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null, string name = "")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = string.IsNullOrEmpty(name) ? $"RelayCommand<{typeof(T).Name}>" : name;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                if (parameter is null && default(T) is not null)
                    return false;

                if (parameter is not null && parameter is not T)
                    return false;

                return _canExecute?.Invoke((T?)parameter) ?? true;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.CanExecute: {ex.Message}");
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (!CanExecute(parameter))
                    return;

                if (parameter is null && default(T) is not null)
                    return;

                if (parameter is not null && parameter is not T)
                {
                    AsyncLogger.Instance.Log(LogType.Warning, $"Invalid parameter type for {_name}: expected {typeof(T).Name}, got {parameter.GetType().Name}");
                    return;
                }

                _execute((T?)parameter);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.Execute: {ex.Message}");

                System.Windows.MessageBox.Show(
                    $"コマンド実行中にエラーが発生しました: {_name}\n詳細: {ex.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            try
            {
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error raising CanExecuteChanged for {_name}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"RelayCommand<{typeof(T).Name}>: {_name}";
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, System.Threading.Tasks.Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private readonly string _name;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, System.Threading.Tasks.Task> execute, Predicate<object?>? canExecute = null, string name = "")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = string.IsNullOrEmpty(name) ? "AsyncRelayCommand" : name;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.CanExecute: {ex.Message}");
                return false;
            }
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute(parameter);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.Execute: {ex.Message}");

                System.Windows.MessageBox.Show(
                    $"非同期コマンド実行中にエラーが発生しました: {_name}\n詳細: {ex.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            try
            {
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error raising CanExecuteChanged for {_name}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"AsyncRelayCommand: {_name} (Executing: {_isExecuting})";
        }
    }

    public class ActionCommand : ICommand
    {
        private readonly Predicate<object?>? _canExecute;
        private readonly Action<object?> _execute;
        private readonly string _name;

        public ActionCommand(Predicate<object?>? canExecute, Action<object?> execute, string name = "")
        {
            _canExecute = canExecute;
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _name = string.IsNullOrEmpty(name) ? "ActionCommand" : name;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return _canExecute?.Invoke(parameter) ?? true;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.CanExecute: {ex.Message}");
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (CanExecute(parameter))
                {
                    _execute(parameter);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.Execute: {ex.Message}");

                System.Windows.MessageBox.Show(
                    $"アクション実行中にエラーが発生しました: {_name}\n詳細: {ex.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            try
            {
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error raising CanExecuteChanged for {_name}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"ActionCommand: {_name}";
        }
    }

    public static class CommandExtensions
    {
        public static void SafeExecute(this ICommand command, object? parameter = null)
        {
            try
            {
                if (command?.CanExecute(parameter) == true)
                {
                    command.Execute(parameter);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in SafeExecute: {ex.Message}");
            }
        }

        public static bool SafeCanExecute(this ICommand command, object? parameter = null)
        {
            try
            {
                return command?.CanExecute(parameter) ?? false;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in SafeCanExecute: {ex.Message}");
                return false;
            }
        }

        public static void SafeRaiseCanExecuteChanged(this ICommand command)
        {
            try
            {
                if (command is RelayCommand relayCommand)
                {
                    relayCommand.RaiseCanExecuteChanged();
                }
                else if (command is ActionCommand actionCommand)
                {
                    actionCommand.RaiseCanExecuteChanged();
                }
                else if (command is AsyncRelayCommand asyncCommand)
                {
                    asyncCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in SafeRaiseCanExecuteChanged: {ex.Message}");
            }
        }
    }

    public class DelegateCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        private readonly string _name;

        public DelegateCommand(Action execute, Func<bool>? canExecute = null, string name = "")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = string.IsNullOrEmpty(name) ? "DelegateCommand" : name;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return _canExecute?.Invoke() ?? true;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.CanExecute: {ex.Message}");
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (CanExecute(parameter))
                {
                    _execute();
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {_name}.Execute: {ex.Message}");

                System.Windows.MessageBox.Show(
                    $"デリゲートコマンド実行中にエラーが発生しました: {_name}\n詳細: {ex.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            try
            {
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error raising CanExecuteChanged for {_name}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"DelegateCommand: {_name}";
        }
    }
}