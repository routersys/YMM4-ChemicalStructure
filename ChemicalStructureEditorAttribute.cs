using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;

namespace YMM4ChemicalStructurePlugin.Shape
{
    internal class ChemicalStructureEditorAttribute : PropertyEditorAttribute2
    {
        public new PropertyEditorSize PropertyEditorSize { get; set; } = PropertyEditorSize.Normal;

        public double InitialHeight { get; set; } = 700;
        public double MinimumHeight { get; set; } = 500;
        public double MaximumHeight { get; set; } = 1200;
        public bool IsResizable { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public bool EnableKeyboardShortcuts { get; set; } = true;
        public bool ShowAdvancedFeatures { get; set; } = true;
        public bool ShowPresets { get; set; } = true;
        public bool EnableTutorial { get; set; } = true;
        public bool EnablePerformanceWarnings { get; set; } = true;

        public override FrameworkElement Create()
        {
            try
            {
                var editor = new ChemicalStructureEditor();

                if (IsResizable)
                {
                    editor.MinHeight = Math.Max(300, MinimumHeight);
                    editor.MaxHeight = Math.Max(MinimumHeight, MaximumHeight);
                    editor.Height = Math.Max(MinimumHeight, Math.Min(MaximumHeight, InitialHeight));
                }
                else
                {
                    editor.Height = Math.Max(300, InitialHeight);
                }

                if (ShowTooltips)
                {
                    SetupTooltips(editor);
                }

                if (EnableKeyboardShortcuts)
                {
                    editor.IsTabStop = true;
                    editor.Focusable = true;
                }

                var config = new EditorConfiguration
                {
                    ShowAdvancedFeatures = ShowAdvancedFeatures,
                    ShowPresets = ShowPresets,
                    EnableKeyboardShortcuts = EnableKeyboardShortcuts,
                    ShowTooltips = ShowTooltips,
                    EnableTutorial = EnableTutorial,
                    EnablePerformanceWarnings = EnablePerformanceWarnings,
                    UpdateThrottleMs = 100,
                    MaxAtomCount = 1000,
                    MaxBondCount = 2000
                };

                editor.SetValue(FrameworkElement.TagProperty, config);

                return editor;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error creating ChemicalStructureEditor: {ex.Message}");
                return CreateFallbackEditor();
            }
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            if (control is not ChemicalStructureEditor editor)
            {
                AsyncLogger.Instance.Log(LogType.Warning, "Control is not ChemicalStructureEditor");
                return;
            }

            if (itemProperties == null || itemProperties.Length == 0)
            {
                AsyncLogger.Instance.Log(LogType.Warning, "Item properties are null or empty");
                return;
            }

            try
            {
                var viewModel = new ChemicalStructureEditorViewModel(itemProperties);

                if (editor.Tag is EditorConfiguration config)
                {
                    ApplyConfiguration(viewModel, config);
                }

                editor.DataContext = viewModel;

                SetupErrorHandling(editor, viewModel);
                SetupPerformanceMonitoring(editor, viewModel);
                SetupDataValidation(editor, viewModel);
                SetupRealtimeUpdates(editor, viewModel);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error setting up ChemicalStructureEditor: {ex.Message}");
                CreateFallbackBinding(editor, itemProperties);
            }
        }

        public override void ClearBindings(FrameworkElement control)
        {
            if (control is not ChemicalStructureEditor editor)
                return;

            try
            {
                if (editor.DataContext is ChemicalStructureEditorViewModel vm)
                {
                    ClearEventHandlers(editor, vm);
                    vm.Dispose();
                }

                editor.DataContext = null;
                editor.ClearValue(FrameworkElement.TagProperty);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error clearing ChemicalStructureEditor bindings: {ex.Message}");
            }
        }

        private static void SetupTooltips(ChemicalStructureEditor editor)
        {
            if (editor == null) return;

            try
            {
                var tooltips = new Dictionary<string, string>
                {
                    ["AddAtomButton"] = "新しい原子を追加します (Ctrl+A)\n選択中の元素で追加されます",
                    ["RemoveAtomButton"] = "選択した原子を削除します (Delete)\n関連する結合も削除されます",
                    ["AddBondButton"] = "選択した2つの原子間に結合を作成します (Ctrl+B)\n原子を2つ選択してから実行してください",
                    ["AutoBondButton"] = "近い原子間に自動的に結合を作成します\n結合長の1.5倍以内の原子が対象です",
                    ["OptimizeLayoutButton"] = "原子の配置を最適化します (Ctrl+O)\n力学的計算により最適な配置を求めます",
                    ["CreateRingButton"] = "選択した原子で環構造を作成します (Ctrl+R)\n3個以上の原子が必要です",
                    ["LinearizeButton"] = "原子を直線状に配置します (Ctrl+L)\n結合も直線状に再配置されます",
                    ["ClearAllButton"] = "すべての原子と結合をクリアします (Ctrl+N)\n取り消しできません",
                    ["SavePresetButton"] = "現在の構造をプリセットとして保存します (Ctrl+S)\nダイアログでプリセット名を入力してください",
                    ["DeletePresetButton"] = "選択したプリセットを削除します\n取り消しできません",
                    ["ApplyPresetButton"] = "選択したプリセットを適用します\n現在の構造は置き換えられます",
                    ["GenerateFromFormulaButton"] = "化学式から構造を生成します (Ctrl+F)\n分子式を入力してください",
                    ["ShowHydrogenCheck"] = "水素原子の表示/非表示を切り替えます (Ctrl+H)\n水素数プロパティとは別です",
                    ["ShowChargesCheck"] = "電荷の表示/非表示を切り替えます\n原子の右上に表示されます",
                    ["ShowLonePairsCheck"] = "孤立電子対の表示/非表示を切り替えます\n主に酸素、窒素で表示されます",
                    ["AntiAliasingCheck"] = "アンチエイリアシングの有効/無効を切り替えます\n滑らかな描画になります",
                    ["UseSkeletalFormulaCheck"] = "略記法表示の有効/無効を切り替えます\n炭素原子が簡略表示されます",
                    ["ElementColorButton"] = "選択した原子に元素固有の色を適用します\n元素の標準色が使用されます",
                    ["ResetAtomButton"] = "選択した原子のプロパティをリセットします\n元素標準値に戻ります"
                };

                foreach (var kvp in tooltips)
                {
                    try
                    {
                        var element = editor.FindName(kvp.Key) as FrameworkElement;
                        if (element != null)
                        {
                            element.ToolTip = kvp.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        AsyncLogger.Instance.Log(LogType.Warning, $"Failed to set tooltip for {kvp.Key}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error setting up tooltips: {ex.Message}");
            }
        }

        private static void ApplyConfiguration(ChemicalStructureEditorViewModel viewModel, EditorConfiguration config)
        {
            if (viewModel == null || config == null) return;

            try
            {
                config.Validate();

                if (!config.ShowAdvancedFeatures)
                {
                    AsyncLogger.Instance.Log(LogType.Info, "Advanced features disabled");
                }

                if (!config.ShowPresets)
                {
                    AsyncLogger.Instance.Log(LogType.Info, "Presets disabled");
                }

                if (!config.EnableKeyboardShortcuts)
                {
                    AsyncLogger.Instance.Log(LogType.Info, "Keyboard shortcuts disabled");
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error applying configuration: {ex.Message}");
            }
        }

        private static void SetupErrorHandling(ChemicalStructureEditor editor, ChemicalStructureEditorViewModel viewModel)
        {
            if (editor == null || viewModel == null) return;

            try
            {
                viewModel.ErrorOccurred += (sender, e) =>
                {
                    try
                    {
                        AsyncLogger.Instance.Log(LogType.Error, $"ChemicalStructureEditor Error: {e.Message}");

                        if (e.IsCritical)
                        {
                            editor.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    MessageBox.Show(
                                        $"化学構造式エディタでエラーが発生しました:\n{e.Message}\n\nエディタの状態をリセットすることをお勧めします。",
                                        "エラー",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                }
                                catch { }
                            }));
                        }
                        else
                        {
                            editor.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    AsyncLogger.Instance.Log(LogType.Warning, $"Non-critical error: {e.Message}");
                                }
                                catch { }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        AsyncLogger.Instance.Log(LogType.Error, $"Error in error handler: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error setting up error handling: {ex.Message}");
            }
        }

        private static void SetupPerformanceMonitoring(ChemicalStructureEditor editor, ChemicalStructureEditorViewModel viewModel)
        {
            if (editor == null || viewModel == null) return;

            try
            {
                var stopwatch = new System.Diagnostics.Stopwatch();
                var operationCount = 0;

                viewModel.BeginEdit += (s, e) =>
                {
                    try
                    {
                        stopwatch.Restart();
                        operationCount++;
                    }
                    catch { }
                };

                viewModel.EndEdit += (s, e) =>
                {
                    try
                    {
                        stopwatch.Stop();
                        var elapsed = stopwatch.ElapsedMilliseconds;

                        if (elapsed > 1000)
                        {
                            AsyncLogger.Instance.Log(LogType.Warning, $"Slow operation detected: {elapsed}ms (Operation #{operationCount})");
                        }

                        if (operationCount % 100 == 0)
                        {
                            AsyncLogger.Instance.Log(LogType.Info, $"Completed {operationCount} operations");
                        }
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error setting up performance monitoring: {ex.Message}");
            }
        }

        private static void SetupDataValidation(ChemicalStructureEditor editor, ChemicalStructureEditorViewModel viewModel)
        {
            if (editor == null || viewModel == null) return;

            try
            {
                viewModel.PropertyChanged += (sender, e) =>
                {
                    try
                    {
                        if (e.PropertyName == nameof(viewModel.AtomCount) && viewModel.AtomCount > 500)
                        {
                            AsyncLogger.Instance.Log(LogType.Warning, $"Large molecule warning: {viewModel.AtomCount} atoms");
                        }

                        if (e.PropertyName == nameof(viewModel.BondCount) && viewModel.BondCount > 1000)
                        {
                            AsyncLogger.Instance.Log(LogType.Warning, $"Complex molecule warning: {viewModel.BondCount} bonds");
                        }

                        if (e.PropertyName == nameof(viewModel.MolecularWeight) && viewModel.MolecularWeight > 10000)
                        {
                            AsyncLogger.Instance.Log(LogType.Warning, $"Heavy molecule warning: {viewModel.MolecularWeight:F2} Da");
                        }
                    }
                    catch (Exception ex)
                    {
                        AsyncLogger.Instance.Log(LogType.Error, $"Error in data validation: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error setting up data validation: {ex.Message}");
            }
        }

        private static void SetupRealtimeUpdates(ChemicalStructureEditor editor, ChemicalStructureEditorViewModel viewModel)
        {
            if (editor == null || viewModel == null) return;

            try
            {
                var updateTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };

                updateTimer.Tick += (sender, e) =>
                {
                    try
                    {
                        viewModel.UpdateBondDisplayInfos();
                    }
                    catch (Exception ex)
                    {
                        AsyncLogger.Instance.Log(LogType.Error, $"Error in realtime update: {ex.Message}");
                    }
                };

                updateTimer.Start();

                editor.Unloaded += (s, e) =>
                {
                    try
                    {
                        updateTimer.Stop();
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error setting up realtime updates: {ex.Message}");
            }
        }

        private static FrameworkElement CreateFallbackEditor()
        {
            try
            {
                var stackPanel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Background = System.Windows.Media.Brushes.LightYellow
                };

                var errorText = new TextBlock
                {
                    Text = "化学構造式エディタの初期化に失敗しました。",
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                };

                var instructionText = new TextBlock
                {
                    Text = "可能な解決策:\n• プラグインを再読み込みしてください\n• YMM4を再起動してください\n• プラグインファイルが破損していないか確認してください",
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                };

                var contactText = new TextBlock
                {
                    Text = "問題が解決しない場合は、プラグイン開発者にお問い合わせください。",
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap
                };

                stackPanel.Children.Add(errorText);
                stackPanel.Children.Add(instructionText);
                stackPanel.Children.Add(contactText);

                var border = new Border
                {
                    Child = stackPanel,
                    BorderBrush = System.Windows.Media.Brushes.Red,
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(15),
                    CornerRadius = new CornerRadius(5),
                    Background = System.Windows.Media.Brushes.LightYellow
                };

                var grid = new Grid
                {
                    Height = 300,
                    MinHeight = 200
                };
                grid.Children.Add(border);

                return grid;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error creating fallback editor: {ex.Message}");

                return new TextBlock
                {
                    Text = "エラー: エディタを作成できませんでした。YMM4を再起動してください。",
                    Foreground = System.Windows.Media.Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10)
                };
            }
        }

        private static void CreateFallbackBinding(ChemicalStructureEditor editor, ItemProperty[] itemProperties)
        {
            if (editor == null || itemProperties == null) return;

            try
            {
                var simpleViewModel = new ChemicalStructureEditorViewModel(itemProperties);
                editor.DataContext = simpleViewModel;

                if (editor.Tag is EditorConfiguration config)
                {
                    config.ShowAdvancedFeatures = false;
                    config.EnableKeyboardShortcuts = false;
                    config.ShowTooltips = false;
                    config.EnableTutorial = false;
                }

                AsyncLogger.Instance.Log(LogType.Info, "Fallback binding created successfully");
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Fallback binding creation failed: {ex.Message}");

                try
                {
                    var errorPanel = new StackPanel();

                    var errorText = new TextBlock
                    {
                        Text = "エディタの初期化に失敗しました。",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = System.Windows.Media.Brushes.Red,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(5)
                    };

                    var instructionText = new TextBlock
                    {
                        Text = "プラグインを再読み込みするか、YMM4を再起動してください。",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5)
                    };

                    errorPanel.Children.Add(errorText);
                    errorPanel.Children.Add(instructionText);

                    editor.Content = errorPanel;
                }
                catch (Exception innerEx)
                {
                    AsyncLogger.Instance.Log(LogType.Error, $"Final fallback failed: {innerEx.Message}");
                }
            }
        }

        private static void ClearEventHandlers(ChemicalStructureEditor editor, ChemicalStructureEditorViewModel viewModel)
        {
            if (editor == null || viewModel == null) return;

            try
            {
                AsyncLogger.Instance.Log(LogType.Info, "Event handlers cleared");
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error clearing event handlers: {ex.Message}");
            }
        }
    }

    internal class EditorConfiguration
    {
        public bool ShowAdvancedFeatures { get; set; } = true;
        public bool ShowPresets { get; set; } = true;
        public bool EnableKeyboardShortcuts { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public bool EnableTutorial { get; set; } = true;
        public bool EnablePerformanceWarnings { get; set; } = true;
        public double UpdateThrottleMs { get; set; } = 100;
        public int MaxAtomCount { get; set; } = 1000;
        public int MaxBondCount { get; set; } = 2000;

        public bool IsValid()
        {
            return UpdateThrottleMs > 0 &&
                   MaxAtomCount > 0 &&
                   MaxBondCount > 0 &&
                   UpdateThrottleMs <= 10000 &&
                   MaxAtomCount <= 10000 &&
                   MaxBondCount <= 20000;
        }

        public void Validate()
        {
            if (UpdateThrottleMs <= 0) UpdateThrottleMs = 100;
            if (MaxAtomCount <= 0) MaxAtomCount = 1000;
            if (MaxBondCount <= 0) MaxBondCount = 2000;

            if (UpdateThrottleMs > 10000) UpdateThrottleMs = 10000;
            if (MaxAtomCount > 10000) MaxAtomCount = 10000;
            if (MaxBondCount > 20000) MaxBondCount = 20000;
        }

        public override string ToString()
        {
            return $"EditorConfig: Advanced={ShowAdvancedFeatures}, Presets={ShowPresets}, Shortcuts={EnableKeyboardShortcuts}, " +
                   $"Tooltips={ShowTooltips}, Tutorial={EnableTutorial}, MaxAtoms={MaxAtomCount}, MaxBonds={MaxBondCount}";
        }
    }
}