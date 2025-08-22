using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public partial class ParameterValidationPanel : UserControl, INotifyPropertyChanged, IPropertyEditorControl
    {
        private ChemicalStructureParameter? _parameter;
        private readonly DispatcherTimer _scrollDelayTimer;
        private Storyboard? _scrollStoryboard;

        private static string? _updateMessage;
        private static bool _updateCheckCompleted = false;
        private static readonly HttpClient _httpClient = new();

        public static readonly DependencyProperty ParameterProperty =
            DependencyProperty.Register(nameof(Parameter), typeof(ChemicalStructureParameter), typeof(ParameterValidationPanel),
                new PropertyMetadata(null, OnParameterChanged));

        public ChemicalStructureParameter? Parameter
        {
            get => (ChemicalStructureParameter?)GetValue(ParameterProperty);
            set => SetValue(ParameterProperty, value);
        }

        private bool _isPanelVisible;
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set => Set(ref _isPanelVisible, value);
        }

#pragma warning disable 0067
        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;
#pragma warning restore 0067

        public ParameterValidationPanel()
        {
            InitializeComponent();
            _scrollDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1), IsEnabled = false };
            _scrollDelayTimer.Tick += ScrollDelayTimer_Tick;
            Loaded += ParameterValidationPanel_Loaded;
            Unloaded += ParameterValidationPanel_Unloaded;

            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("YMM4-ChemicalStructure", GetCurrentVersion()));
            }
        }

        private async void ParameterValidationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync();
            ValidateParameters();
        }

        private void ParameterValidationPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAllTimersAndAnimations();
        }

        private void StopAllTimersAndAnimations()
        {
            _scrollDelayTimer.Stop();
            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
        }

        private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ParameterValidationPanel panel) return;

            panel._parameter = e.NewValue as ChemicalStructureParameter;

            if (panel._parameter != null)
            {
                panel.ValidateParameters();
            }
            else
            {
                panel.HideValidationPanel();
            }
        }

        private static string GetCurrentVersion()
        {
            return "0.0.1";
        }

        private async Task CheckForUpdatesAsync()
        {
            if (_updateCheckCompleted) return;

            try
            {
                var response = await _httpClient.GetAsync("https://api.github.com/repos/routersys/YMM4-ChemicalStructure/releases/latest");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("tag_name", out var tagNameElement))
                {
                    string latestVersionTag = tagNameElement.GetString() ?? "";
                    string latestVersionStr = latestVersionTag.StartsWith("v") ? latestVersionTag.Substring(1) : latestVersionTag;

                    if (Version.TryParse(latestVersionStr, out var latestVersion) &&
                        Version.TryParse(GetCurrentVersion(), out var currentVersion) &&
                        latestVersion > currentVersion)
                    {
                        _updateMessage = $"新しいバージョン v{latestVersionStr} が利用可能です。（現在: v{currentVersion}）";
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _updateCheckCompleted = true;
            }
        }

        private void ScrollDelayTimer_Tick(object? sender, EventArgs e)
        {
            _scrollDelayTimer.Stop();
            StartTextScrolling();
        }

        private void ValidateParameters()
        {
            if (!string.IsNullOrEmpty(_updateMessage))
            {
                ShowValidationMessage("Update", new List<string> { _updateMessage });
                return;
            }

            if (_parameter == null || !IsLoaded)
            {
                HideValidationPanel();
                return;
            }

            HideValidationPanel();
        }

        private void ShowValidationMessage(string level, List<string> messages)
        {
            IsPanelVisible = true;
            MainValidationPanel.Tag = level;
            MessageText.Text = messages.First();
            if (messages.Count > 1)
            {
                CountText.Text = $"+{messages.Count - 1}";
                CountBadge.Visibility = Visibility.Visible;
                MainValidationPanel.ToolTip = new ToolTip { Content = string.Join("\n", messages.Select(m => $"• {m}")) };
            }
            else
            {
                CountBadge.Visibility = Visibility.Collapsed;
                MainValidationPanel.ToolTip = new ToolTip { Content = messages.First() };
            }
            _scrollDelayTimer.Stop();
            _scrollDelayTimer.Start();
        }

        private void HideValidationPanel()
        {
            IsPanelVisible = false;
            StopAllTimersAndAnimations();
        }

        private void StartTextScrolling()
        {
            if (!IsPanelVisible) return;

            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
            Canvas.SetLeft(MessageText, 0);

            MessageText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (MessageCanvas.ActualWidth > 0 && MessageText.DesiredSize.Width > MessageCanvas.ActualWidth)
            {
                var scrollDistance = MessageText.DesiredSize.Width - MessageCanvas.ActualWidth + 20;
                var animation = new DoubleAnimation(0, -scrollDistance, TimeSpan.FromSeconds(Math.Max(3.0, scrollDistance / 40.0)))
                {
                    BeginTime = TimeSpan.FromSeconds(1),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                _scrollStoryboard = new Storyboard();
                _scrollStoryboard.Children.Add(animation);
                Storyboard.SetTarget(animation, MessageText);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
                _scrollStoryboard.Begin();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}