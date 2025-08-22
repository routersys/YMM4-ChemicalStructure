using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using System.Text.Json.Serialization;


namespace YMM4ChemicalStructurePlugin.Shape
{
    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public bool IsCritical { get; }

        public ErrorEventArgs(string message, bool isCritical = false)
        {
            Message = message;
            IsCritical = isCritical;
        }
    }

    public class BondDisplayInfo : INotifyPropertyChanged
    {
        private Bond _bond;
        private string _displayText;
        private Color _color;

        public Bond Bond
        {
            get => _bond;
            set
            {
                _bond = value;
                OnPropertyChanged();
            }
        }

        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }

        public BondDisplayInfo(Bond bond, string displayText, Color color)
        {
            _bond = bond;
            _displayText = displayText;
            _color = color;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PresetData
    {
        public string Name { get; set; } = "";
        public string Formula { get; set; } = "";
        public string Description { get; set; } = "";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MoleculeCategory Category { get; set; } = MoleculeCategory.Simple;
        public List<SerializableAtom> Atoms { get; set; } = new();
        public List<SerializableBond> Bonds { get; set; } = new();
        public DateTime Created { get; set; } = DateTime.Now;
    }

    public class SerializableAtom
    {
        public string Element { get; set; } = "C";
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int Charge { get; set; }
        public int HydrogenCount { get; set; }
        public string FillColor { get; set; } = "#FFFFFF";
        public string TextColor { get; set; } = "#000000";
        public double Radius { get; set; } = 20;
        public double FontSize { get; set; } = 24;
        public int DisplayMode { get; set; } = 0;
    }

    public class SerializableBond
    {
        public int Atom1Index { get; set; }
        public int Atom2Index { get; set; }
        public int BondType { get; set; } = 0;
        public string Color { get; set; } = "#000000";
        public double Thickness { get; set; } = 2.0;
        public double Opacity { get; set; } = 1.0;
        public int EndStyle { get; set; } = 0;
        public double Order { get; set; } = 1.0;
    }

    public class PresetNameDialog : Window
    {
        private string _presetName = "";
        public string PresetName
        {
            get => _presetName;
            set => _presetName = value ?? "";
        }

        public PresetNameDialog()
        {
            Title = "プリセット名入力";
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "プリセット名を入力してください:",
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new TextBox
            {
                Text = PresetName,
                Margin = new Thickness(0, 0, 0, 10)
            };
            textBox.TextChanged += (s, e) => PresetName = textBox.Text;
            panel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 60,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "キャンセル",
                Width = 60,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            Content = panel;

            textBox.Focus();
            textBox.SelectAll();
        }
    }

    internal class ChemicalStructureEditorViewModel : Bindable, IDisposable
    {
        private readonly ChemicalStructureParameter _parameter;
        private bool _isDisposed = false;
        private string _presetsFilePath;
        private bool _isUpdatingBondDisplayInfos = false;
        private bool _isUpdatingSelection = false;
        private bool _suppressParameterUpdate = false;

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;

        public ImmutableList<Atom> Atoms { get => atoms; set => Set(ref atoms, value ?? ImmutableList<Atom>.Empty); }
        private ImmutableList<Atom> atoms = ImmutableList<Atom>.Empty;

        public ImmutableList<Bond> Bonds { get => bonds; set => Set(ref bonds, value ?? ImmutableList<Bond>.Empty); }
        private ImmutableList<Bond> bonds = ImmutableList<Bond>.Empty;

        public ObservableCollection<BondDisplayInfo> BondDisplayInfos { get; } = new();

        public Atom? SelectedAtom1
        {
            get => selectedAtom1;
            set
            {
                if (Set(ref selectedAtom1, value))
                {
                    if (value is not null)
                    {
                        SelectedElement = value.Element ?? "C";
                        SelectedCharge = value.Charge;
                    }
                    UpdateCommands();
                }
            }
        }
        private Atom? selectedAtom1;

        public Atom? SelectedAtom2 { get => selectedAtom2; set { Set(ref selectedAtom2, value); UpdateCommands(); } }
        private Atom? selectedAtom2;

        public Bond? SelectedBond
        {
            get => selectedBond;
            set
            {
                if (Set(ref selectedBond, value) && !_isUpdatingSelection)
                {
                    _isUpdatingSelection = true;
                    try
                    {
                        var displayInfo = BondDisplayInfos.FirstOrDefault(info => info.Bond?.Id == value?.Id);
                        if (selectedBondDisplayInfo != displayInfo)
                        {
                            selectedBondDisplayInfo = displayInfo;
                            OnPropertyChanged(nameof(SelectedBondDisplayInfo));
                        }
                    }
                    finally
                    {
                        _isUpdatingSelection = false;
                    }
                    UpdateCommands();
                }
            }
        }
        private Bond? selectedBond;

        public BondDisplayInfo? SelectedBondDisplayInfo
        {
            get => selectedBondDisplayInfo;
            set
            {
                if (Set(ref selectedBondDisplayInfo, value) && !_isUpdatingSelection)
                {
                    _isUpdatingSelection = true;
                    try
                    {
                        var newSelectedBond = value?.Bond;
                        if (selectedBond != newSelectedBond)
                        {
                            selectedBond = newSelectedBond;
                            OnPropertyChanged(nameof(SelectedBond));
                        }
                    }
                    finally
                    {
                        _isUpdatingSelection = false;
                    }
                    UpdateCommands();
                }
            }
        }
        private BondDisplayInfo? selectedBondDisplayInfo;

        public string MolecularFormula { get => molecularFormula; set => Set(ref molecularFormula, value ?? ""); }
        private string molecularFormula = "";

        public string SmilesNotation { get => smilesNotation; set => Set(ref smilesNotation, value ?? ""); }
        private string smilesNotation = "";

        public string InputFormula { get => inputFormula; set => Set(ref inputFormula, value ?? ""); }
        private string inputFormula = "";

        public int RingCount { get => ringCount; set => Set(ref ringCount, value); }
        private int ringCount = 0;

        public ObservableCollection<string> PresetNames { get; } = new();
        public string? SelectedPreset { get => selectedPreset; set { Set(ref selectedPreset, value); UpdateCommands(); } }
        private string? selectedPreset;

        public bool UseParameterDefaults { get => useParameterDefaults; set => Set(ref useParameterDefaults, value); }
        private bool useParameterDefaults = true;

        public bool UseSkeletalFormula
        {
            get => _parameter?.UseSkeletalFormula ?? false;
            set
            {
                if (_parameter != null && _parameter.UseSkeletalFormula != value)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    _parameter.UseSkeletalFormula = value;
                    EndEdit?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        public List<ElementInfo> AllElements { get; }

        public string SelectedElement
        {
            get => selectedElement;
            set
            {
                if (Set(ref selectedElement, value ?? "C"))
                {
                    if (SelectedAtom1 != null && SelectedAtom1.Element != value)
                    {
                        BeginEdit?.Invoke(this, EventArgs.Empty);
                        SelectedAtom1.Element = value;
                        EndEdit?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
        private string selectedElement = "C";

        public List<int> Charges { get; } = new List<int> { -3, -2, -1, 0, 1, 2, 3 };
        public int SelectedCharge { get => selectedCharge; set => Set(ref selectedCharge, value); }
        private int selectedCharge = 0;

        public IEnumerable<int> GroupNumbers { get; } = Enumerable.Range(1, 18);
        public IEnumerable<int> PeriodNumbers { get; } = Enumerable.Range(1, 7);

        public bool ShowHydrogen
        {
            get => _parameter?.ShowHydrogen ?? true;
            set
            {
                if (_parameter != null && _parameter.ShowHydrogen != value)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    _parameter.ShowHydrogen = value;
                    EndEdit?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowCharges
        {
            get => _parameter?.ShowCharges ?? true;
            set
            {
                if (_parameter != null && _parameter.ShowCharges != value)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    _parameter.ShowCharges = value;
                    EndEdit?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowLonePairs
        {
            get => _parameter?.ShowLonePairs ?? false;
            set
            {
                if (_parameter != null && _parameter.ShowLonePairs != value)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    _parameter.ShowLonePairs = value;
                    EndEdit?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        public bool AntiAliasing
        {
            get => _parameter?.AntiAliasing ?? true;
            set
            {
                if (_parameter != null && _parameter.AntiAliasing != value)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    _parameter.AntiAliasing = value;
                    EndEdit?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        public int AtomCount => Atoms?.Count ?? 0;
        public int BondCount => Bonds?.Count ?? 0;
        public double MolecularWeight => CalculateMolecularWeight();

        public ActionCommand AddAtomCommand { get; }
        public ActionCommand RemoveAtomCommand { get; }
        public ActionCommand AddBondCommand { get; }
        public ActionCommand RemoveBondCommand { get; }
        public ActionCommand ApplyElementColorCommand { get; }
        public ActionCommand ResetAtomPropertiesCommand { get; }
        public RelayCommand<string> SetElementCommand { get; }
        public RelayCommand<BondType> SetBondTypeCommand { get; }
        public ActionCommand ApplyPresetCommand { get; }
        public ActionCommand SavePresetCommand { get; }
        public ActionCommand DeletePresetCommand { get; }
        public ActionCommand ClearAllCommand { get; }
        public ActionCommand AutoBondCommand { get; }
        public ActionCommand OptimizeLayoutCommand { get; }
        public ActionCommand AlignAtomsCommand { get; }
        public ActionCommand CreateRingCommand { get; }
        public ActionCommand LinearizeCommand { get; }
        public ActionCommand ShowAllAtomsCommand { get; }
        public ActionCommand HideCarbonsCommand { get; }
        public ActionCommand GenerateFromFormulaCommand { get; }

        public ChemicalStructureEditorViewModel(ItemProperty[] itemProperties)
        {
            if (itemProperties == null || itemProperties.Length == 0)
                throw new ArgumentException("Item properties cannot be null or empty");

            _parameter = itemProperties[0].PropertyOwner as ChemicalStructureParameter ??
                throw new ArgumentException("Property owner must be ChemicalStructureParameter");

            _presetsFilePath = Path.Combine(ChemicalStructurePlugin.PluginDataPath, "user_presets.json");

            AllElements = PeriodicTableService.GetAllElements();

            AddAtomCommand = new ActionCommand(_ => !_isDisposed, _ => SafeExecute(AddAtom));
            RemoveAtomCommand = new ActionCommand(_ => !_isDisposed && SelectedAtom1 is not null, _ => SafeExecute(RemoveAtom));
            AddBondCommand = new ActionCommand(_ => !_isDisposed && CanAddBond(), _ => SafeExecute(AddBond));
            RemoveBondCommand = new ActionCommand(_ => !_isDisposed && SelectedBond is not null, _ => SafeExecute(RemoveBond));
            ApplyElementColorCommand = new ActionCommand(_ => !_isDisposed, _ => SafeExecute(ApplyElementColor));
            ResetAtomPropertiesCommand = new ActionCommand(_ => !_isDisposed && SelectedAtom1 is not null, _ => SafeExecute(ResetAtomProperties));
            SetElementCommand = new RelayCommand<string>(element => SafeExecute(() => SetSelectedElement(element)));
            SetBondTypeCommand = new RelayCommand<BondType>(bondType => SafeExecute(() => SetSelectedBondType(bondType)));

            ApplyPresetCommand = new ActionCommand(_ => !_isDisposed && !string.IsNullOrEmpty(SelectedPreset), _ => SafeExecute(ApplyPreset));
            SavePresetCommand = new ActionCommand(_ => !_isDisposed, _ => SafeExecute(SavePreset));
            DeletePresetCommand = new ActionCommand(_ => !_isDisposed && !string.IsNullOrEmpty(SelectedPreset), _ => SafeExecute(DeletePreset));
            ClearAllCommand = new ActionCommand(_ => !_isDisposed && Atoms.Any(), _ => SafeExecute(ClearAll));
            AutoBondCommand = new ActionCommand(_ => !_isDisposed && Atoms.Count > 1, _ => SafeExecute(AutoBond));

            OptimizeLayoutCommand = new ActionCommand(_ => !_isDisposed && Atoms.Count > 2, _ => SafeExecute(OptimizeLayout));
            AlignAtomsCommand = new ActionCommand(_ => !_isDisposed && Atoms.Count > 1, _ => SafeExecute(AlignAtoms));
            CreateRingCommand = new ActionCommand(_ => !_isDisposed && Atoms.Count >= 3, _ => SafeExecute(CreateRing));
            LinearizeCommand = new ActionCommand(_ => !_isDisposed && Atoms.Count > 1, _ => SafeExecute(LinearizeAtoms));
            ShowAllAtomsCommand = new ActionCommand(_ => !_isDisposed, _ => SafeExecute(ShowAllAtoms));
            HideCarbonsCommand = new ActionCommand(_ => !_isDisposed, _ => SafeExecute(HideCarbons));

            GenerateFromFormulaCommand = new ActionCommand(_ => !_isDisposed && !string.IsNullOrEmpty(InputFormula), _ => SafeExecute(GenerateFromFormula));

            LoadPresets();
            UpdateProperties();
            SubscribeToBondChanges();

            _parameter.PropertyChanged += OnItemPropertyChanged;
        }

        private void SafeExecute(Action action)
        {
            if (_isDisposed || _parameter == null) return;

            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                HandleError(ex, "操作実行中");
            }
        }

        private void AddAtom()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                double x = 0;
                double y = 0;

                if (_parameter.Atoms.Any())
                {
                    var lastAtom = _parameter.Atoms.Last();
                    x = lastAtom.X.Values.LastOrDefault()?.Value ?? 0 + 30;
                    y = lastAtom.Y.Values.LastOrDefault()?.Value ?? 0 + 30;
                }

                var newAtom = new Atom(SelectedElement, x, y)
                {
                    Charge = SelectedCharge
                };

                if (UseParameterDefaults)
                {
                    newAtom.FillColor = newAtom.GetDefaultElementColor();
                    newAtom.TextColor = _parameter.DefaultTextColor;
                    newAtom.Radius = _parameter.DefaultAtomSize;
                    newAtom.FontSize = _parameter.DefaultFontSize;
                }

                _parameter.Atoms = _parameter.Atoms.Add(newAtom);
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RemoveAtom()
        {
            if (SelectedAtom1 is null) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                var atomToRemove = SelectedAtom1;
                _parameter.Atoms = _parameter.Atoms.Remove(atomToRemove);
                _parameter.Bonds = _parameter.Bonds.Where(b => b.Atom1Id != atomToRemove.Id && b.Atom2Id != atomToRemove.Id).ToImmutableList();
                SelectedAtom1 = null;
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanAddBond()
        {
            if (SelectedAtom1 is null || SelectedAtom2 is null || SelectedAtom1 == SelectedAtom2) return false;
            return !_parameter.Bonds.Any(b =>
                (b.Atom1Id == SelectedAtom1.Id && b.Atom2Id == SelectedAtom2.Id) ||
                (b.Atom1Id == SelectedAtom2.Id && b.Atom2Id == SelectedAtom1.Id));
        }

        private void AddBond()
        {
            if (!CanAddBond() || SelectedAtom1 is null || SelectedAtom2 is null) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                var newBond = new Bond(SelectedAtom1.Id, SelectedAtom2.Id);

                if (UseParameterDefaults)
                {
                    newBond.Color = _parameter.DefaultBondColor;
                    newBond.Thickness = _parameter.DefaultBondThickness;
                }

                newBond.BondChanged += OnBondChanged;
                _parameter.Bonds = _parameter.Bonds.Add(newBond);
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RemoveBond()
        {
            if (SelectedBond is null) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                _parameter.Bonds = _parameter.Bonds.Remove(SelectedBond);
                SelectedBond = null;
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyElementColor()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                if (SelectedAtom1 != null)
                {
                    SelectedAtom1.FillColor = SelectedAtom1.GetDefaultElementColor();
                }
                else
                {
                    foreach (var atom in _parameter.Atoms)
                    {
                        atom.FillColor = atom.GetDefaultElementColor();
                    }
                }
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ResetAtomProperties()
        {
            if (SelectedAtom1 is null) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                SelectedAtom1.ResetToElementDefaults();
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyPreset()
        {
            if (string.IsNullOrEmpty(SelectedPreset)) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                Atom.ResetAtomCounter();
                var allPresets = LoadDefaultPresetsFromFile().Concat(LoadPresetsFromFile()).ToList();
                var preset = allPresets.FirstOrDefault(p => p.Name == SelectedPreset);
                if (preset != null)
                {
                    ApplyPresetData(preset);
                    UpdateParameterData();
                }
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SavePreset()
        {
            try
            {
                var dialog = new PresetNameDialog
                {
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.PresetName))
                {
                    var presets = LoadPresetsFromFile();

                    var existingPreset = presets.FirstOrDefault(p => p.Name == dialog.PresetName);
                    if (existingPreset != null)
                    {
                        presets.Remove(existingPreset);
                    }

                    var newPreset = new PresetData
                    {
                        Name = dialog.PresetName,
                        Atoms = _parameter.Atoms.Select(ConvertToSerializableAtom).ToList(),
                        Bonds = _parameter.Bonds.Select((b, i) => ConvertToSerializableBond(b, i)).ToList(),
                        Created = DateTime.Now
                    };

                    presets.Add(newPreset);
                    SavePresetsToFile(presets);
                    LoadPresets();
                    SelectedPreset = dialog.PresetName;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "プリセット保存中");
            }
        }

        private void DeletePreset()
        {
            if (string.IsNullOrEmpty(SelectedPreset)) return;

            try
            {
                var result = MessageBox.Show(
                    $"プリセット '{SelectedPreset}' を削除しますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var presets = LoadPresetsFromFile();
                    var presetToRemove = presets.FirstOrDefault(p => p.Name == SelectedPreset);
                    if (presetToRemove != null)
                    {
                        presets.Remove(presetToRemove);
                        SavePresetsToFile(presets);
                        LoadPresets();
                        SelectedPreset = null;
                    }
                    else
                    {
                        MessageBox.Show("デフォルトプリセットは削除できません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "プリセット削除中");
            }
        }

        private void GenerateFromFormula()
        {
            if (string.IsNullOrEmpty(InputFormula)) return;

            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                var atoms = ParseMolecularFormula(InputFormula);
                if (atoms.Any())
                {
                    _parameter.Atoms = atoms.ToImmutableList();
                    _parameter.Bonds = ImmutableList<Bond>.Empty;

                    if (atoms.Count > 1)
                    {
                        AutoLayoutMolecule();
                    }
                    UpdateParameterData();
                }
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private List<Atom> ParseMolecularFormula(string formula)
        {
            var atoms = new List<Atom>();
            var pattern = @"([A-Z][a-z]?)(\d*)";
            var matches = Regex.Matches(formula, pattern);

            double x = 0;
            double y = 0;
            const double spacing = 80;

            foreach (Match match in matches)
            {
                var element = match.Groups[1].Value;
                var countStr = match.Groups[2].Value;
                var count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);

                for (int i = 0; i < count; i++)
                {
                    var atom = new Atom(element, x, y);
                    if (UseParameterDefaults)
                    {
                        atom.FillColor = atom.GetDefaultElementColor();
                        atom.TextColor = _parameter.DefaultTextColor;
                        atom.Radius = _parameter.DefaultAtomSize;
                        atom.FontSize = _parameter.DefaultFontSize;
                    }
                    atoms.Add(atom);
                    x += spacing;
                }
            }

            return atoms;
        }

        private void AutoLayoutMolecule()
        {
            if (_parameter.Atoms.Count <= 1) return;

            var atoms = _parameter.Atoms.ToList();

            if (atoms.Count <= 6)
            {
                ArrangeInLine(atoms);
            }
            else
            {
                ArrangeInGrid(atoms);
            }

            _parameter.Atoms = atoms.ToImmutableList();
        }

        private void ArrangeInLine(List<Atom> atoms)
        {
            const double spacing = 80;
            double startX = -(atoms.Count - 1) * spacing / 2.0;

            for (int i = 0; i < atoms.Count; i++)
            {
                atoms[i].X.Values[0].Value = startX + i * spacing;
                atoms[i].Y.Values[0].Value = 0;
            }
        }

        private void ArrangeInGrid(List<Atom> atoms)
        {
            const double spacing = 80;
            int cols = (int)Math.Ceiling(Math.Sqrt(atoms.Count));
            int rows = (int)Math.Ceiling((double)atoms.Count / cols);

            for (int i = 0; i < atoms.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                double x = (col - cols / 2.0) * spacing;
                double y = (row - rows / 2.0) * spacing;

                atoms[i].X.Values[0].Value = x;
                atoms[i].Y.Values[0].Value = y;
            }
        }

        private void ClearAll()
        {
            var result = MessageBox.Show(
                "すべての原子と結合を削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BeginEdit?.Invoke(this, EventArgs.Empty);
                try
                {
                    _parameter.Atoms = ImmutableList<Atom>.Empty;
                    _parameter.Bonds = ImmutableList<Bond>.Empty;
                    Atom.ResetAtomCounter();
                    UpdateParameterData();
                }
                finally
                {
                    EndEdit?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void AutoBond()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                var bondLengthSquared = _parameter.DefaultBondLength * _parameter.DefaultBondLength * 1.5;
                var newBonds = _parameter.Bonds.ToList();

                for (int i = 0; i < _parameter.Atoms.Count; i++)
                {
                    for (int j = i + 1; j < _parameter.Atoms.Count; j++)
                    {
                        var atom1 = _parameter.Atoms[i];
                        var atom2 = _parameter.Atoms[j];
                        var pos1 = atom1.X.Values.FirstOrDefault()?.Value ?? 0;
                        var pos2 = atom1.Y.Values.FirstOrDefault()?.Value ?? 0;
                        var pos3 = atom2.X.Values.FirstOrDefault()?.Value ?? 0;
                        var pos4 = atom2.Y.Values.FirstOrDefault()?.Value ?? 0;

                        var dx = pos1 - pos3;
                        var dy = pos2 - pos4;

                        if ((dx * dx + dy * dy) < bondLengthSquared)
                        {
                            var exists = newBonds.Any(b => (b.Atom1Id == atom1.Id && b.Atom2Id == atom2.Id) || (b.Atom1Id == atom2.Id && b.Atom2Id == atom1.Id));
                            if (!exists)
                            {
                                var newBond = new Bond(atom1.Id, atom2.Id);
                                if (UseParameterDefaults)
                                {
                                    newBond.Color = _parameter.DefaultBondColor;
                                    newBond.Thickness = _parameter.DefaultBondThickness;
                                }
                                newBonds.Add(newBond);
                            }
                        }
                    }
                }
                _parameter.Bonds = newBonds.ToImmutableList();
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ShowAllAtoms()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                foreach (var atom in _parameter.Atoms)
                {
                    atom.DisplayMode = AtomDisplayMode.Normal;
                }
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void HideCarbons()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                foreach (var atom in _parameter.Atoms)
                {
                    if (atom.Element == "C")
                    {
                        atom.DisplayMode = _parameter.UseSkeletalFormula ? AtomDisplayMode.Hidden : AtomDisplayMode.Normal;
                    }
                }
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetSelectedElement(string? element)
        {
            if (SelectedAtom1 != null && !string.IsNullOrEmpty(element))
            {
                BeginEdit?.Invoke(this, EventArgs.Empty);
                try
                {
                    SelectedAtom1.Element = element;
                    UpdateParameterData();
                }
                finally
                {
                    EndEdit?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void OptimizeLayout()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                if (Atoms.Count < 3) return;

                var atoms = _parameter.Atoms.ToList();
                var bonds = _parameter.Bonds.ToList();

                for (int iteration = 0; iteration < 50; iteration++)
                {
                    var forces = new Dictionary<Guid, (double fx, double fy)>();

                    for (int i = 0; i < atoms.Count; i++)
                    {
                        forces[atoms[i].Id] = (0, 0);

                        for (int j = 0; j < atoms.Count; j++)
                        {
                            if (i == j) continue;

                            var pos1 = atoms[i];
                            var pos2 = atoms[j];
                            var x1 = pos1.X.Values.FirstOrDefault()?.Value ?? 0;
                            var y1 = pos1.Y.Values.FirstOrDefault()?.Value ?? 0;
                            var x2 = pos2.X.Values.FirstOrDefault()?.Value ?? 0;
                            var y2 = pos2.Y.Values.FirstOrDefault()?.Value ?? 0;

                            var dx = x1 - x2;
                            var dy = y1 - y2;
                            var distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance > 0.1)
                            {
                                var repulsion = 1000.0 / (distance * distance);
                                var fx = repulsion * dx / distance;
                                var fy = repulsion * dy / distance;

                                var current = forces[atoms[i].Id];
                                forces[atoms[i].Id] = (current.fx + fx, current.fy + fy);
                            }
                        }
                    }

                    foreach (var bond in bonds)
                    {
                        var atom1 = atoms.FirstOrDefault(a => a.Id == bond.Atom1Id);
                        var atom2 = atoms.FirstOrDefault(a => a.Id == bond.Atom2Id);

                        if (atom1 == null || atom2 == null) continue;

                        var x1 = atom1.X.Values.FirstOrDefault()?.Value ?? 0;
                        var y1 = atom1.Y.Values.FirstOrDefault()?.Value ?? 0;
                        var x2 = atom2.X.Values.FirstOrDefault()?.Value ?? 0;
                        var y2 = atom2.Y.Values.FirstOrDefault()?.Value ?? 0;

                        var dx = x2 - x1;
                        var dy = y2 - y1;
                        var distance = Math.Sqrt(dx * dx + dy * dy);
                        var idealLength = _parameter.DefaultBondLength;

                        if (distance > 0.1)
                        {
                            var spring = 0.1 * (distance - idealLength);
                            var fx = spring * dx / distance;
                            var fy = spring * dy / distance;

                            var force1 = forces[atom1.Id];
                            forces[atom1.Id] = (force1.fx + fx, force1.fy + fy);

                            var force2 = forces[atom2.Id];
                            forces[atom2.Id] = (force2.fx - fx, force2.fy - fy);
                        }
                    }

                    var damping = 0.1;
                    foreach (var atom in atoms)
                    {
                        if (forces.TryGetValue(atom.Id, out var force))
                        {
                            var newX = (atom.X.Values.FirstOrDefault()?.Value ?? 0) + force.fx * damping;
                            var newY = (atom.Y.Values.FirstOrDefault()?.Value ?? 0) + force.fy * damping;

                            atom.X.Values[0].Value = Math.Max(-500, Math.Min(500, newX));
                            atom.Y.Values[0].Value = Math.Max(-500, Math.Min(500, newY));
                        }
                    }
                }

                _parameter.Atoms = atoms.ToImmutableList();
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AlignAtoms()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                if (Atoms.Count < 2) return;

                var atoms = _parameter.Atoms.ToList();
                var minX = atoms.Min(a => a.X.Values.FirstOrDefault()?.Value ?? 0);
                var maxX = atoms.Max(a => a.X.Values.FirstOrDefault()?.Value ?? 0);
                var avgY = atoms.Average(a => a.Y.Values.FirstOrDefault()?.Value ?? 0);

                for (int i = 0; i < atoms.Count; i++)
                {
                    var x = minX + (maxX - minX) * i / (atoms.Count - 1);
                    atoms[i].X.Values[0].Value = x;
                    atoms[i].Y.Values[0].Value = avgY;
                }

                _parameter.Atoms = atoms.ToImmutableList();
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CreateRing()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                if (Atoms.Count < 3) return;

                var atoms = _parameter.Atoms.ToList();
                var centerX = atoms.Average(a => a.X.Values.FirstOrDefault()?.Value ?? 0);
                var centerY = atoms.Average(a => a.Y.Values.FirstOrDefault()?.Value ?? 0);
                var radius = _parameter.RingSize;

                for (int i = 0; i < atoms.Count; i++)
                {
                    var angle = 2 * Math.PI * i / atoms.Count;
                    var x = centerX + radius * Math.Cos(angle);
                    var y = centerY + radius * Math.Sin(angle);

                    atoms[i].X.Values[0].Value = x;
                    atoms[i].Y.Values[0].Value = y;
                }

                var bonds = _parameter.Bonds.ToList();
                var atomIds = atoms.Select(a => a.Id).ToHashSet();
                bonds.RemoveAll(b => atomIds.Contains(b.Atom1Id) && atomIds.Contains(b.Atom2Id));

                for (int i = 0; i < atoms.Count; i++)
                {
                    var nextIndex = (i + 1) % atoms.Count;
                    var newBond = new Bond(atoms[i].Id, atoms[nextIndex].Id);
                    if (UseParameterDefaults)
                    {
                        newBond.Color = _parameter.DefaultBondColor;
                        newBond.Thickness = _parameter.DefaultBondThickness;
                    }
                    bonds.Add(newBond);
                }

                _parameter.Atoms = atoms.ToImmutableList();
                _parameter.Bonds = bonds.ToImmutableList();
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetSelectedBondType(BondType bondType)
        {
            if (SelectedBond != null)
            {
                BeginEdit?.Invoke(this, EventArgs.Empty);
                try
                {
                    SelectedBond.Type = bondType;
                    UpdateBondDisplayInfosInternal();
                    UpdateParameterData();
                }
                finally
                {
                    EndEdit?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void LinearizeAtoms()
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            try
            {
                if (Atoms.Count < 2) return;

                var atoms = _parameter.Atoms.ToList();
                var spacing = _parameter.DefaultBondLength;
                var startX = -(atoms.Count - 1) * spacing / 2.0;

                for (int i = 0; i < atoms.Count; i++)
                {
                    atoms[i].X.Values[0].Value = startX + i * spacing;
                    atoms[i].Y.Values[0].Value = 0;
                }

                var bonds = new List<Bond>();
                for (int i = 0; i < atoms.Count - 1; i++)
                {
                    var newBond = new Bond(atoms[i].Id, atoms[i + 1].Id);
                    if (UseParameterDefaults)
                    {
                        newBond.Color = _parameter.DefaultBondColor;
                        newBond.Thickness = _parameter.DefaultBondThickness;
                    }
                    bonds.Add(newBond);
                }

                _parameter.Atoms = atoms.ToImmutableList();
                _parameter.Bonds = bonds.ToImmutableList();
                UpdateParameterData();
            }
            finally
            {
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateParameterData()
        {
            if (_suppressParameterUpdate) return;

            try
            {
                _suppressParameterUpdate = true;
                UpdateProperties();
                SubscribeToBondChanges();
            }
            finally
            {
                _suppressParameterUpdate = false;
            }
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isDisposed || _suppressParameterUpdate) return;

            try
            {
                if (e.PropertyName is nameof(ChemicalStructureParameter.Atoms) or nameof(ChemicalStructureParameter.Bonds))
                {
                    UpdateProperties();
                    SubscribeToBondChanges();
                }

                if (e.PropertyName is nameof(ChemicalStructureParameter.ShowHydrogen) or
                                     nameof(ChemicalStructureParameter.ShowCharges) or
                                     nameof(ChemicalStructureParameter.ShowLonePairs) or
                                     nameof(ChemicalStructureParameter.AntiAliasing) or
                                     nameof(ChemicalStructureParameter.UseSkeletalFormula))
                {
                    OnPropertyChanged(e.PropertyName);
                }

                if (e.PropertyName is nameof(ChemicalStructureParameter.DefaultBondColor) or
                                     nameof(ChemicalStructureParameter.DefaultBondThickness) or
                                     nameof(ChemicalStructureParameter.DefaultTextColor) or
                                     nameof(ChemicalStructureParameter.DefaultAtomSize) or
                                     nameof(ChemicalStructureParameter.DefaultFontSize))
                {
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateBondDisplayInfosInternal();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "プロパティ変更処理");
            }
        }

        private void OnBondChanged(object? sender, EventArgs e)
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateBondDisplayInfosInternal();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                HandleError(ex, "結合変更処理");
            }
        }

        private void SubscribeToBondChanges()
        {
            try
            {
                foreach (var bond in _parameter.Bonds)
                {
                    bond.BondChanged -= OnBondChanged;
                    bond.BondChanged += OnBondChanged;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "結合変更イベント登録");
            }
        }

        private void ApplyAutoLayoutChange()
        {
            try
            {
                var autoLayout = _parameter?.AutoLayout ?? AutoLayoutType.None;
                if (autoLayout != AutoLayoutType.None && _parameter?.Atoms?.Any() == true)
                {
                    BeginEdit?.Invoke(this, EventArgs.Empty);
                    try
                    {
                        switch (autoLayout)
                        {
                            case AutoLayoutType.Linear:
                                LinearizeAtoms();
                                break;
                            case AutoLayoutType.Optimized:
                                OptimizeLayout();
                                break;
                            default:
                                break;
                        }
                    }
                    finally
                    {
                        EndEdit?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "自動配置変更処理");
            }
        }

        private void UpdateProperties()
        {
            if (_isDisposed || _parameter == null) return;

            try
            {
                Atoms = _parameter.Atoms ?? ImmutableList<Atom>.Empty;
                Bonds = _parameter.Bonds ?? ImmutableList<Bond>.Empty;

                UpdateBondDisplayInfosInternal();

                MolecularFormula = CalculateMolecularFormula();

                OnPropertyChanged(nameof(AtomCount));
                OnPropertyChanged(nameof(BondCount));
                OnPropertyChanged(nameof(MolecularWeight));
                OnPropertyChanged(nameof(RingCount));
                OnPropertyChanged(nameof(SmilesNotation));
            }
            catch (Exception ex)
            {
                HandleError(ex, "プロパティ更新処理");
            }
        }

        public void UpdateBondDisplayInfos()
        {
            UpdateBondDisplayInfosInternal();
        }

        private void UpdateBondDisplayInfosInternal()
        {
            if (_isUpdatingBondDisplayInfos || _isDisposed) return;

            try
            {
                _isUpdatingBondDisplayInfos = true;

                var existingInfos = BondDisplayInfos.ToDictionary(info => info.Bond?.Id ?? Guid.Empty, info => info);
                var currentBondIds = new HashSet<Guid>();

                foreach (var bond in Bonds)
                {
                    currentBondIds.Add(bond.Id);
                    var atom1 = Atoms.FirstOrDefault(a => a.Id == bond.Atom1Id);
                    var atom2 = Atoms.FirstOrDefault(a => a.Id == bond.Atom2Id);

                    if (atom1 != null && atom2 != null)
                    {
                        var displayText = bond.GetDisplayName(atom1, atom2);
                        var color = bond.GetEffectiveColor();

                        if (existingInfos.TryGetValue(bond.Id, out var existingInfo))
                        {
                            existingInfo.Bond = bond;
                            existingInfo.DisplayText = displayText;
                            existingInfo.Color = color;
                        }
                        else
                        {
                            var newInfo = new BondDisplayInfo(bond, displayText, color);
                            BondDisplayInfos.Add(newInfo);
                        }
                    }
                }

                for (int i = BondDisplayInfos.Count - 1; i >= 0; i--)
                {
                    var info = BondDisplayInfos[i];
                    if (info.Bond?.Id == null || !currentBondIds.Contains(info.Bond.Id))
                    {
                        BondDisplayInfos.RemoveAt(i);
                    }
                }

                if (SelectedBond != null && !currentBondIds.Contains(SelectedBond.Id))
                {
                    SelectedBond = null;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "結合表示情報更新処理");
            }
            finally
            {
                _isUpdatingBondDisplayInfos = false;
            }
        }

        private void UpdateCommands()
        {
            if (_isDisposed) return;

            try
            {
                RemoveAtomCommand.RaiseCanExecuteChanged();
                AddBondCommand.RaiseCanExecuteChanged();
                RemoveBondCommand.RaiseCanExecuteChanged();
                ApplyElementColorCommand.RaiseCanExecuteChanged();
                ResetAtomPropertiesCommand.RaiseCanExecuteChanged();
                ApplyPresetCommand.RaiseCanExecuteChanged();
                SavePresetCommand.RaiseCanExecuteChanged();
                DeletePresetCommand.RaiseCanExecuteChanged();
                ClearAllCommand.RaiseCanExecuteChanged();
                AutoBondCommand.RaiseCanExecuteChanged();
                OptimizeLayoutCommand.RaiseCanExecuteChanged();
                AlignAtomsCommand.RaiseCanExecuteChanged();
                CreateRingCommand.RaiseCanExecuteChanged();
                LinearizeCommand.RaiseCanExecuteChanged();
                ShowAllAtomsCommand.RaiseCanExecuteChanged();
                HideCarbonsCommand.RaiseCanExecuteChanged();
                GenerateFromFormulaCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                HandleError(ex, "コマンド更新処理");
            }
        }

        private double CalculateMolecularWeight()
        {
            try
            {
                var atomicWeights = Atom.GetAtomicWeights();
                double totalWeight = 0;

                foreach (var atom in Atoms)
                {
                    if (atomicWeights.TryGetValue(atom.Element ?? "C", out var weight))
                    {
                        totalWeight += weight;
                    }
                    if (atomicWeights.TryGetValue("H", out var hWeight))
                    {
                        totalWeight += atom.HydrogenCount * hWeight;
                    }
                }
                return totalWeight;
            }
            catch
            {
                return 0.0;
            }
        }

        private string CalculateMolecularFormula()
        {
            try
            {
                if (!Atoms.Any()) return string.Empty;

                var elementCounts = new Dictionary<string, int>();
                foreach (var atom in Atoms)
                {
                    var element = atom.Element ?? "C";
                    elementCounts[element] = elementCounts.GetValueOrDefault(element, 0) + 1;
                    if (atom.HydrogenCount > 0)
                    {
                        elementCounts["H"] = elementCounts.GetValueOrDefault("H", 0) + atom.HydrogenCount;
                    }
                }

                var formula = new StringBuilder();
                var orderedElements = new[] { "C", "H", "N", "O", "F", "P", "S", "Cl", "Br", "I" };

                foreach (var element in orderedElements)
                {
                    if (elementCounts.TryGetValue(element, out var count))
                    {
                        formula.Append(element);
                        if (count > 1) formula.Append(count);
                        elementCounts.Remove(element);
                    }
                }

                foreach (var kvp in elementCounts.OrderBy(x => x.Key))
                {
                    formula.Append(kvp.Key);
                    if (kvp.Value > 1) formula.Append(kvp.Value);
                }

                return formula.ToString();
            }
            catch
            {
                return "";
            }
        }

        private void LoadPresets()
        {
            try
            {
                PresetNames.Clear();

                var defaultPresets = LoadDefaultPresetsFromFile();
                foreach (var preset in defaultPresets.OrderBy(p => p.Name))
                {
                    PresetNames.Add(preset.Name);
                }

                var userPresets = LoadPresetsFromFile();
                foreach (var preset in userPresets.OrderBy(p => p.Name))
                {
                    if (!PresetNames.Contains(preset.Name))
                    {
                        PresetNames.Add(preset.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "プリセット読み込み中");
            }
        }

        private List<PresetData> LoadPresetsFromFile()
        {
            try
            {
                if (!File.Exists(_presetsFilePath))
                    return new List<PresetData>();

                var json = File.ReadAllText(_presetsFilePath);
                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
                return JsonSerializer.Deserialize<List<PresetData>>(json, options) ?? new List<PresetData>();
            }
            catch
            {
                return new List<PresetData>();
            }
        }

        private List<PresetData> LoadDefaultPresetsFromFile()
        {
            try
            {
                var defaultPresetPath = Path.Combine(ChemicalStructurePlugin.PluginDataPath, "default_presets.json");
                if (!File.Exists(defaultPresetPath))
                    return new List<PresetData>();

                var json = File.ReadAllText(defaultPresetPath);
                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
                return JsonSerializer.Deserialize<List<PresetData>>(json, options) ?? new List<PresetData>();
            }
            catch
            {
                return new List<PresetData>();
            }
        }


        private void SavePresetsToFile(List<PresetData> presets)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var json = JsonSerializer.Serialize(presets, options);
                File.WriteAllText(_presetsFilePath, json);
            }
            catch (Exception ex)
            {
                HandleError(ex, "プリセットファイル保存中");
            }
        }

        private void ApplyPresetData(PresetData preset)
        {
            var atoms = new List<Atom>();
            var atomIdMap = new Dictionary<int, Guid>();

            for (int i = 0; i < preset.Atoms.Count; i++)
            {
                var serAtom = preset.Atoms[i];
                var atom = new Atom(serAtom.Element, serAtom.X, serAtom.Y);
                atom.Z.Values[0].Value = serAtom.Z;
                atom.Charge = serAtom.Charge;
                atom.HydrogenCount = serAtom.HydrogenCount;
                atom.FillColor = (Color)ColorConverter.ConvertFromString(serAtom.FillColor);
                atom.TextColor = (Color)ColorConverter.ConvertFromString(serAtom.TextColor);
                atom.Radius = serAtom.Radius;
                atom.FontSize = serAtom.FontSize;
                atom.DisplayMode = (AtomDisplayMode)serAtom.DisplayMode;

                atoms.Add(atom);
                atomIdMap[i] = atom.Id;
            }

            var bonds = new List<Bond>();
            foreach (var serBond in preset.Bonds)
            {
                if (atomIdMap.TryGetValue(serBond.Atom1Index, out var atom1Id) &&
                    atomIdMap.TryGetValue(serBond.Atom2Index, out var atom2Id))
                {
                    var bond = new Bond(atom1Id, atom2Id);
                    bond.Type = (BondType)serBond.BondType;
                    bond.Color = (Color)ColorConverter.ConvertFromString(serBond.Color);
                    bond.Thickness = serBond.Thickness;
                    bond.Opacity = serBond.Opacity;
                    bond.EndStyle = (BondEndStyle)serBond.EndStyle;
                    bond.Order = serBond.Order;
                    bonds.Add(bond);
                }
            }

            _parameter.Atoms = atoms.ToImmutableList();
            _parameter.Bonds = bonds.ToImmutableList();
        }

        private SerializableAtom ConvertToSerializableAtom(Atom atom)
        {
            return new SerializableAtom
            {
                Element = atom.Element,
                X = atom.X.Values[0].Value,
                Y = atom.Y.Values[0].Value,
                Z = atom.Z.Values[0].Value,
                Charge = atom.Charge,
                HydrogenCount = atom.HydrogenCount,
                FillColor = atom.FillColor.ToString(),
                TextColor = atom.TextColor.ToString(),
                Radius = atom.Radius,
                FontSize = atom.FontSize,
                DisplayMode = (int)atom.DisplayMode
            };
        }

        private SerializableBond ConvertToSerializableBond(Bond bond, int bondIndex)
        {
            var atom1Index = _parameter.Atoms.ToList().FindIndex(a => a.Id == bond.Atom1Id);
            var atom2Index = _parameter.Atoms.ToList().FindIndex(a => a.Id == bond.Atom2Id);

            return new SerializableBond
            {
                Atom1Index = atom1Index,
                Atom2Index = atom2Index,
                BondType = (int)bond.Type,
                Color = bond.Color.ToString(),
                Thickness = bond.Thickness,
                Opacity = bond.Opacity,
                EndStyle = (int)bond.EndStyle,
                Order = bond.Order
            };
        }

        private void HandleError(Exception ex, string operation)
        {
            System.Diagnostics.Debug.WriteLine($"Error in {operation}: {ex.Message}");
            ErrorOccurred?.Invoke(this, new ErrorEventArgs($"{operation}でエラーが発生しました: {ex.Message}", false));
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            try
            {
                if (_parameter != null)
                {
                    _parameter.PropertyChanged -= OnItemPropertyChanged;

                    foreach (var bond in _parameter.Bonds)
                    {
                        bond.BondChanged -= OnBondChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during dispose: {ex.Message}");
            }
        }
    }
}