using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using YukkuriMovieMaker.Commons;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public partial class ChemicalStructureEditor : UserControl, IPropertyEditorControl
    {
        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        private bool _isLoaded = false;
        private bool _isDisposed = false;

        public ChemicalStructureEditor()
        {
            try
            {
                InitializeComponent();
                DataContextChanged += OnDataContextChanged;
                Loaded += UserControl_Loaded;
                Unloaded += UserControl_Unloaded;
                SetupKeyboardShortcuts();
                SetupAccessibility();
            }
            catch (Exception ex)
            {
                HandleError(ex, "コントロール初期化");
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_isDisposed) return;

            try
            {
                if (e.OldValue is ChemicalStructureEditorViewModel oldVm)
                {
                    oldVm.BeginEdit -= OnViewModelBeginEdit;
                    oldVm.EndEdit -= OnViewModelEndEdit;
                    oldVm.ErrorOccurred -= OnViewModelErrorOccurred;
                }

                if (e.NewValue is ChemicalStructureEditorViewModel newVm)
                {
                    newVm.BeginEdit += OnViewModelBeginEdit;
                    newVm.EndEdit += OnViewModelEndEdit;
                    newVm.ErrorOccurred += OnViewModelErrorOccurred;
                    SetupContextMenus(newVm);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "データコンテキスト変更");
            }
        }

        private void PeriodicTable_ElementSelected(object sender, ElementSelectedEventArgs e)
        {
            try
            {
                if (DataContext is ChemicalStructureEditorViewModel vm && e.SelectedElement != null)
                {
                    vm.SelectedElement = e.SelectedElement.Symbol;
                }
                if (FindName("ElementSelectorToggle") is ToggleButton toggleButton)
                {
                    toggleButton.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "元素選択");
            }
        }

        private void OnViewModelBeginEdit(object? sender, EventArgs e)
        {
            try
            {
                BeginEdit?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                HandleError(ex, "編集開始");
            }
        }

        private void OnViewModelEndEdit(object? sender, EventArgs e)
        {
            try
            {
                EndEdit?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                HandleError(ex, "編集終了");
            }
        }

        private void OnViewModelErrorOccurred(object? sender, ErrorEventArgs e)
        {
            try
            {
                if (e.IsCritical)
                {
                    MessageBox.Show(
                        $"重大なエラーが発生しました:\n{e.Message}",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else
                {
                    AsyncLogger.Instance.Log(LogType.Warning, $"Warning: {e.Message}");
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error handling ViewModel error: {ex.Message}");
            }
        }

        private void PropertiesEditor_BeginEdit(object sender, EventArgs e)
        {
            try
            {
                BeginEdit?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                HandleError(ex, "プロパティエディタ編集開始");
            }
        }

        private void PropertiesEditor_EndEdit(object sender, EventArgs e)
        {
            try
            {
                EndEdit?.Invoke(this, e);
                if (DataContext is ChemicalStructureEditorViewModel vm && vm.SelectedAtom1 != null)
                {
                    vm.SelectedElement = vm.SelectedAtom1.Element;
                    vm.SelectedCharge = vm.SelectedAtom1.Charge;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "プロパティエディタ編集終了");
            }
        }

        private void SetupKeyboardShortcuts()
        {
            if (_isDisposed) return;

            try
            {
                var keyBindings = new[]
                {
                    new { Key = Key.A, Modifiers = ModifierKeys.Control, Action = "AddAtom" },
                    new { Key = Key.B, Modifiers = ModifierKeys.Control, Action = "AddBond" },
                    new { Key = Key.Delete, Modifiers = ModifierKeys.None, Action = "Delete" },
                    new { Key = Key.R, Modifiers = ModifierKeys.Control, Action = "CreateRing" },
                    new { Key = Key.L, Modifiers = ModifierKeys.Control, Action = "Linearize" },
                    new { Key = Key.O, Modifiers = ModifierKeys.Control, Action = "Optimize" },
                    new { Key = Key.H, Modifiers = ModifierKeys.Control, Action = "ToggleHydrogen" },
                    new { Key = Key.G, Modifiers = ModifierKeys.Control, Action = "HideCarbons" },
                    new { Key = Key.N, Modifiers = ModifierKeys.Control, Action = "Clear" },
                    new { Key = Key.S, Modifiers = ModifierKeys.Control, Action = "SavePreset" },
                    new { Key = Key.F, Modifiers = ModifierKeys.Control, Action = "GenerateFromFormula" },
                    new { Key = Key.D1, Modifiers = ModifierKeys.None, Action = "SetElement_H" },
                    new { Key = Key.D2, Modifiers = ModifierKeys.None, Action = "SetElement_C" },
                    new { Key = Key.D3, Modifiers = ModifierKeys.None, Action = "SetElement_N" },
                    new { Key = Key.D4, Modifiers = ModifierKeys.None, Action = "SetElement_O" },
                    new { Key = Key.D5, Modifiers = ModifierKeys.None, Action = "SetElement_F" },
                    new { Key = Key.D6, Modifiers = ModifierKeys.None, Action = "SetElement_P" },
                    new { Key = Key.D7, Modifiers = ModifierKeys.None, Action = "SetElement_S" },
                    new { Key = Key.D8, Modifiers = ModifierKeys.None, Action = "SetElement_Cl" }
                };

                foreach (var binding in keyBindings)
                {
                    try
                    {
                        var inputBinding = new KeyBinding
                        {
                            Key = binding.Key,
                            Modifiers = binding.Modifiers,
                            Command = new RelayCommand(_ => ExecuteKeyboardAction(binding.Action))
                        };
                        InputBindings.Add(inputBinding);
                    }
                    catch (Exception ex)
                    {
                        AsyncLogger.Instance.Log(LogType.Warning, $"Failed to add key binding {binding.Key}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "キーボードショートカット設定");
            }
        }

        private void ExecuteKeyboardAction(string action)
        {
            if (_isDisposed || DataContext is not ChemicalStructureEditorViewModel vm) return;

            try
            {
                switch (action)
                {
                    case "AddAtom":
                        if (vm.AddAtomCommand?.CanExecute(null) == true)
                            vm.AddAtomCommand.Execute(null);
                        break;
                    case "AddBond":
                        if (vm.AddBondCommand?.CanExecute(null) == true)
                            vm.AddBondCommand.Execute(null);
                        break;
                    case "Delete":
                        if (vm.SelectedAtom1 != null && vm.RemoveAtomCommand?.CanExecute(null) == true)
                            vm.RemoveAtomCommand.Execute(null);
                        else if (vm.SelectedBond != null && vm.RemoveBondCommand?.CanExecute(null) == true)
                            vm.RemoveBondCommand.Execute(null);
                        break;
                    case "CreateRing":
                        if (vm.CreateRingCommand?.CanExecute(null) == true)
                            vm.CreateRingCommand.Execute(null);
                        break;
                    case "Linearize":
                        if (vm.LinearizeCommand?.CanExecute(null) == true)
                            vm.LinearizeCommand.Execute(null);
                        break;
                    case "Optimize":
                        if (vm.OptimizeLayoutCommand?.CanExecute(null) == true)
                            vm.OptimizeLayoutCommand.Execute(null);
                        break;
                    case "ToggleHydrogen":
                        vm.ShowHydrogen = !vm.ShowHydrogen;
                        break;
                    case "HideCarbons":
                        if (vm.HideCarbonsCommand?.CanExecute(null) == true)
                            vm.HideCarbonsCommand.Execute(null);
                        break;
                    case "Clear":
                        if (vm.ClearAllCommand?.CanExecute(null) == true)
                        {
                            vm.ClearAllCommand.Execute(null);
                        }
                        break;
                    case "SavePreset":
                        if (vm.SavePresetCommand?.CanExecute(null) == true)
                            vm.SavePresetCommand.Execute(null);
                        break;
                    case "GenerateFromFormula":
                        if (vm.GenerateFromFormulaCommand?.CanExecute(null) == true)
                            vm.GenerateFromFormulaCommand.Execute(null);
                        break;
                    case "SetElement_H":
                        vm.SelectedElement = "H";
                        break;
                    case "SetElement_C":
                        vm.SelectedElement = "C";
                        break;
                    case "SetElement_N":
                        vm.SelectedElement = "N";
                        break;
                    case "SetElement_O":
                        vm.SelectedElement = "O";
                        break;
                    case "SetElement_F":
                        vm.SelectedElement = "F";
                        break;
                    case "SetElement_P":
                        vm.SelectedElement = "P";
                        break;
                    case "SetElement_S":
                        vm.SelectedElement = "S";
                        break;
                    case "SetElement_Cl":
                        vm.SelectedElement = "Cl";
                        break;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, $"キーボードアクション実行: {action}");
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (_isDisposed)
            {
                e.Handled = true;
                return;
            }

            try
            {
                if (e.Key == Key.Tab)
                {
                    e.Handled = HandleTabNavigation(e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift));
                }
                else if (e.Key == Key.Escape)
                {
                    ClearSelection();
                    e.Handled = true;
                }

                base.OnPreviewKeyDown(e);
            }
            catch (Exception ex)
            {
                HandleError(ex, "キー入力処理");
                e.Handled = true;
            }
        }

        private bool HandleTabNavigation(bool reverse)
        {
            if (_isDisposed || DataContext is not ChemicalStructureEditorViewModel vm) return false;

            try
            {
                if (reverse)
                {
                    if (vm.SelectedAtom1 != null && vm.Atoms?.Count > 0)
                    {
                        var currentIndex = vm.Atoms.IndexOf(vm.SelectedAtom1);
                        if (currentIndex > 0)
                        {
                            vm.SelectedAtom1 = vm.Atoms[currentIndex - 1];
                            return true;
                        }
                    }
                }
                else
                {
                    if (vm.SelectedAtom1 != null && vm.Atoms?.Count > 0)
                    {
                        var currentIndex = vm.Atoms.IndexOf(vm.SelectedAtom1);
                        if (currentIndex < vm.Atoms.Count - 1)
                        {
                            vm.SelectedAtom1 = vm.Atoms[currentIndex + 1];
                            return true;
                        }
                    }
                    else if (vm.Atoms?.Any() == true)
                    {
                        vm.SelectedAtom1 = vm.Atoms[0];
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Tab ナビゲーション");
            }

            return false;
        }

        private void ClearSelection()
        {
            if (_isDisposed || DataContext is not ChemicalStructureEditorViewModel vm) return;

            try
            {
                vm.SelectedAtom1 = null;
                vm.SelectedAtom2 = null;
                vm.SelectedBond = null;
            }
            catch (Exception ex)
            {
                HandleError(ex, "選択クリア");
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            try
            {
                base.OnGotFocus(e);
            }
            catch (Exception ex)
            {
                HandleError(ex, "フォーカス取得");
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            try
            {
                base.OnLostFocus(e);
            }
            catch (Exception ex)
            {
                HandleError(ex, "フォーカス失去");
            }
        }

        private void OnAtomSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDisposed || DataContext is not ChemicalStructureEditorViewModel vm) return;

            try
            {
                if (vm.SelectedAtom1 != null && !string.IsNullOrEmpty(vm.SelectedAtom1.Element))
                {
                    vm.SelectedElement = vm.SelectedAtom1.Element;
                    vm.SelectedCharge = vm.SelectedAtom1.Charge;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "原子選択変更");
            }
        }

        private void OnBondSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDisposed || DataContext is not ChemicalStructureEditorViewModel vm) return;

            try
            {
                UpdateBondPropertiesDisplay(vm);
            }
            catch (Exception ex)
            {
                HandleError(ex, "結合選択変更");
            }
        }

        private void UpdateBondPropertiesDisplay(ChemicalStructureEditorViewModel vm)
        {
            if (vm.SelectedBond != null)
            {
                var atom1 = vm.Atoms.FirstOrDefault(a => a.Id == vm.SelectedBond.Atom1Id);
                var atom2 = vm.Atoms.FirstOrDefault(a => a.Id == vm.SelectedBond.Atom2Id);
                if (atom1 != null && atom2 != null)
                {
                    AsyncLogger.Instance.Log(LogType.Info, $"Selected bond: {vm.SelectedBond.GetDisplayName(atom1, atom2)}");
                }
            }
        }


        private void SetupContextMenus(ChemicalStructureEditorViewModel vm)
        {
            if (_isDisposed || vm == null) return;

            try
            {
                var atomContextMenu = new ContextMenu();
                var bondContextMenu = new ContextMenu();

                var atomMenuItems = new FrameworkElement[]
                {
                    new MenuItem { Header = "元素色を適用", Command = vm.ApplyElementColorCommand },
                    new MenuItem { Header = "プロパティをリセット", Command = vm.ResetAtomPropertiesCommand },
                    new MenuItem { Header = "削除", Command = vm.RemoveAtomCommand },
                    new Separator()
                };

                foreach (var item in atomMenuItems)
                {
                    atomContextMenu.Items.Add(item);
                }

                var elementSubmenu = new MenuItem { Header = "元素を変更" };
                var commonElements = new[] { "H", "C", "N", "O", "F", "P", "S", "Cl", "Br", "I" };

                foreach (var element in commonElements)
                {
                    elementSubmenu.Items.Add(new MenuItem
                    {
                        Header = $"{element} ({Atom.GetElementName(element)})",
                        Command = vm.SetElementCommand,
                        CommandParameter = element
                    });
                }
                atomContextMenu.Items.Add(elementSubmenu);

                var bondMenuItems = new FrameworkElement[]
                {
                    new MenuItem { Header = "削除", Command = vm.RemoveBondCommand },
                    new Separator()
                };

                foreach (var item in bondMenuItems)
                {
                    bondContextMenu.Items.Add(item);
                }

                var bondTypeSubmenu = new MenuItem { Header = "結合タイプを変更" };
                var bondTypes = Enum.GetValues<BondType>();

                foreach (var bondType in bondTypes)
                {
                    var displayName = bondType switch
                    {
                        BondType.Single => "単結合",
                        BondType.Double => "二重結合",
                        BondType.Triple => "三重結合",
                        BondType.Wedge => "くさび形",
                        BondType.Dash => "破線",
                        BondType.Wavy => "波線",
                        BondType.Partial => "部分結合",
                        BondType.Coordinate => "配位結合",
                        BondType.Aromatic => "芳香族",
                        BondType.Hydrogen => "水素結合",
                        BondType.Ionic => "イオン結合",
                        BondType.Hidden => "非表示",
                        _ => bondType.ToString()
                    };

                    bondTypeSubmenu.Items.Add(new MenuItem
                    {
                        Header = displayName,
                        Command = vm.SetBondTypeCommand,
                        CommandParameter = bondType
                    });
                }
                bondContextMenu.Items.Add(bondTypeSubmenu);

                var chargeSubmenu = new MenuItem { Header = "電荷を変更" };
                foreach (var charge in vm.Charges)
                {
                    var chargeText = charge == 0 ? "中性" : charge > 0 ? $"+{charge}" : charge.ToString();
                    chargeSubmenu.Items.Add(new MenuItem
                    {
                        Header = chargeText,
                        Command = new RelayCommand<int>(c => { if (vm.SelectedAtom1 != null) { vm.SelectedAtom1.Charge = c; } }),
                        CommandParameter = charge
                    });
                }
                atomContextMenu.Items.Add(chargeSubmenu);

                Tag = new { AtomContextMenu = atomContextMenu, BondContextMenu = bondContextMenu };
            }
            catch (Exception ex)
            {
                HandleError(ex, "コンテキストメニュー設定");
            }
        }

        private void OptimizePerformance()
        {
            if (_isDisposed || DataContext is not ChemicalStructureEditorViewModel vm) return;

            try
            {
                var atomCount = vm.AtomCount;
                var bondCount = vm.BondCount;

                if (atomCount > 100 || bondCount > 200)
                {
                    AsyncLogger.Instance.Log(LogType.Warning, $"Large molecule detected: {atomCount} atoms, {bondCount} bonds");

                    if (atomCount > 500 || bondCount > 1000)
                    {
                        MessageBox.Show(
                            "大きな分子が検出されました。パフォーマンスが低下する可能性があります。",
                            "パフォーマンス警告",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "パフォーマンス最適化");
            }
        }

        private void HandleError(Exception ex, string operation)
        {
            try
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in {operation}: {ex.Message}");

                if (!_isDisposed)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            MessageBox.Show(
                                $"操作中にエラーが発生しました: {operation}\n詳細: {ex.Message}",
                                "エラー",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                        }
                        catch { }
                    }));
                }
            }
            catch { }
        }

        private void SetupAccessibility()
        {
            try
            {
                AutomationProperties.SetName(this, "化学構造式エディタ");
                AutomationProperties.SetHelpText(this, "化学構造式を編集するためのコントロールです。ショートカットキー: Ctrl+A(原子追加), Ctrl+B(結合追加), Delete(削除), Ctrl+S(プリセット保存)");

                this.IsTabStop = true;
                this.Focusable = true;
            }
            catch (Exception ex)
            {
                HandleError(ex, "アクセシビリティ設定");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded || _isDisposed) return;

            try
            {
                _isLoaded = true;
                OptimizePerformance();
            }
            catch (Exception ex)
            {
                HandleError(ex, "コントロール読み込み");
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isDisposed = true;
                _isLoaded = false;

                if (DataContext is ChemicalStructureEditorViewModel vm)
                {
                    vm.BeginEdit -= OnViewModelBeginEdit;
                    vm.EndEdit -= OnViewModelEndEdit;
                    vm.ErrorOccurred -= OnViewModelErrorOccurred;
                }

                InputBindings.Clear();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error during unload: {ex.Message}");
            }
        }
    }
}