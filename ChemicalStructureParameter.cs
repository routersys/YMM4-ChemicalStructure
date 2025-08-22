using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public enum ChemicalDisplayMode
    {
        [Display(Name = "構造式")]
        Structural,
        [Display(Name = "骨格式")]
        Skeletal,
        [Display(Name = "線式")]
        Linear,
        [Display(Name = "分子式")]
        Molecular,
        [Display(Name = "示性式")]
        Constitutional,
        [Display(Name = "電子式")]
        Electronic
    }

    public enum AutoLayoutType
    {
        [Display(Name = "なし")]
        None,
        [Display(Name = "直鎖")]
        Linear,
        [Display(Name = "分岐")]
        Branched,
        [Display(Name = "最適化")]
        Optimized
    }

    public enum RenderingQuality
    {
        [Display(Name = "低")]
        Low,
        [Display(Name = "標準")]
        Normal,
        [Display(Name = "高")]
        High,
        [Display(Name = "最高")]
        Ultra
    }

    public class ChemicalStructureParameter : ShapeParameterBase
    {
        [Display(Name = "", Description = "現在の設定に問題がある場合はここに表示されます。", Order = -1)]
        [ValidationPanelEditor(PropertyEditorSize = PropertyEditorSize.FullWidth)]
        public bool ValidationPlaceholder { get; set; }

        public ImmutableList<Atom> Atoms { get => atoms; set => Set(ref atoms, value ?? ImmutableList<Atom>.Empty); }
        private ImmutableList<Atom> atoms = ImmutableList<Atom>.Empty;

        public ImmutableList<Bond> Bonds { get => bonds; set => Set(ref bonds, value ?? ImmutableList<Bond>.Empty); }
        private ImmutableList<Bond> bonds = ImmutableList<Bond>.Empty;

        [Display(GroupName = "表示設定", Name = "表示モード")]
        [EnumComboBox]
        public ChemicalDisplayMode DisplayMode { get => displayMode; set => Set(ref displayMode, value); }
        private ChemicalDisplayMode displayMode = ChemicalDisplayMode.Structural;

        [Display(GroupName = "表示設定", Name = "全体スケール")]
        [AnimationSlider("F2", "", 0.1, 5.0)]
        public Animation Scale { get; } = new Animation(1.0, 0.1, 10.0);

        [Display(GroupName = "スタイル", Name = "デフォルト結合色")]
        [ColorPicker]
        public Color DefaultBondColor { get => defaultBondColor; set => Set(ref defaultBondColor, value); }
        private Color defaultBondColor = Colors.Black;

        [Display(GroupName = "スタイル", Name = "デフォルト文字色")]
        [ColorPicker]
        public Color DefaultTextColor { get => defaultTextColor; set => Set(ref defaultTextColor, value); }
        private Color defaultTextColor = Colors.Black;

        [Display(GroupName = "スタイル", Name = "背景色")]
        [ColorPicker]
        public Color BackgroundColor { get => backgroundColor; set => Set(ref backgroundColor, value); }
        private Color backgroundColor = Colors.Transparent;

        [Display(GroupName = "スタイル", Name = "線の太さ")]
        [TextBoxSlider("F1", "px", 0.5, 10)]
        [DefaultValue(2.0)]
        public double DefaultBondThickness { get => defaultBondThickness; set => Set(ref defaultBondThickness, Math.Max(0.1, value)); }
        private double defaultBondThickness = 2.0;

        [Display(GroupName = "スタイル", Name = "フォントサイズ")]
        [TextBoxSlider("F0", "px", 8, 72)]
        [DefaultValue(24)]
        public double DefaultFontSize { get => defaultFontSize; set => Set(ref defaultFontSize, Math.Max(8, value)); }
        private double defaultFontSize = 24;

        [Display(GroupName = "スタイル", Name = "原子サイズ")]
        [TextBoxSlider("F1", "px", 0, 100)]
        [DefaultValue(20)]
        public double DefaultAtomSize { get => defaultAtomSize; set => Set(ref defaultAtomSize, Math.Max(0, value)); }
        private double defaultAtomSize = 20;

        [Display(GroupName = "スタイル", Name = "結合長")]
        [TextBoxSlider("F1", "px", 20, 200)]
        [DefaultValue(80)]
        public double DefaultBondLength { get => defaultBondLength; set => Set(ref defaultBondLength, Math.Max(10, value)); }
        private double defaultBondLength = 80;

        [Display(GroupName = "スタイル", Name = "フォント")]
        [FontComboBox]
        public string FontFamily { get => fontFamily; set => Set(ref fontFamily, value ?? "メイリオ"); }
        private string fontFamily = "メイリオ";

        [Display(GroupName = "スタイル", Name = "アウトライン太さ")]
        [TextBoxSlider("F1", "px", 0, 5)]
        [DefaultValue(0)]
        public double OutlineThickness { get => outlineThickness; set => Set(ref outlineThickness, Math.Max(0, value)); }
        private double outlineThickness = 0;

        [Display(GroupName = "スタイル", Name = "アウトライン色")]
        [ColorPicker]
        public Color OutlineColor { get => outlineColor; set => Set(ref outlineColor, value); }
        private Color outlineColor = Colors.Black;

        [Display(GroupName = "オプション", Name = "水素を表示")]
        [ToggleSlider]
        public bool ShowHydrogen { get => showHydrogen; set => Set(ref showHydrogen, value); }
        private bool showHydrogen = true;

        [Display(GroupName = "オプション", Name = "電荷を表示")]
        [ToggleSlider]
        public bool ShowCharges { get => showCharges; set => Set(ref showCharges, value); }
        private bool showCharges = true;

        [Display(GroupName = "オプション", Name = "孤立電子対を表示")]
        [ToggleSlider]
        public bool ShowLonePairs { get => showLonePairs; set => Set(ref showLonePairs, value); }
        private bool showLonePairs = false;

        [Display(GroupName = "オプション", Name = "アンチエイリアス")]
        [ToggleSlider]
        public bool AntiAliasing { get => antiAliasing; set => Set(ref antiAliasing, value); }
        private bool antiAliasing = true;

        [Display(GroupName = "オプション", Name = "略記法表示")]
        [ToggleSlider]
        public bool UseSkeletalFormula { get => useSkeletalFormula; set => Set(ref useSkeletalFormula, value); }
        private bool useSkeletalFormula = false;

        [Display(GroupName = "オプション", Name = "レンダリング品質")]
        [EnumComboBox]
        public RenderingQuality RenderingQuality { get => renderingQuality; set => Set(ref renderingQuality, value); }
        private RenderingQuality renderingQuality = RenderingQuality.Normal;

        [Display(GroupName = "自動配置", Name = "配置タイプ")]
        [EnumComboBox]
        public AutoLayoutType AutoLayout { get => autoLayout; set { Set(ref autoLayout, value); ApplyAutoLayout(); } }
        private AutoLayoutType autoLayout = AutoLayoutType.None;

        [Display(GroupName = "自動配置", Name = "環のサイズ")]
        [TextBoxSlider("F1", "px", 50, 200)]
        [DefaultValue(80)]
        public double RingSize { get => ringSize; set => Set(ref ringSize, Math.Max(20, value)); }
        private double ringSize = 80;

        [Display(GroupName = "自動配置", Name = "間隔調整")]
        [TextBoxSlider("F2", "", 0.5, 2.0)]
        [DefaultValue(1.0)]
        public double SpacingMultiplier { get => spacingMultiplier; set => Set(ref spacingMultiplier, Math.Max(0.1, value)); }
        private double spacingMultiplier = 1.0;

        [Display(GroupName = "自動配置", Name = "自動最適化")]
        [ToggleSlider]
        public bool AutoOptimize { get => autoOptimize; set => Set(ref autoOptimize, value); }
        private bool autoOptimize = false;

        [Display(Name = "")]
        [ChemicalStructureEditor(PropertyEditorSize = PropertyEditorSize.FullWidth)]
        public bool Editor { get; set; }

        public ChemicalStructureParameter() : this(null) { }

        public ChemicalStructureParameter(SharedDataStore? sharedData) : base(sharedData) { }

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        {
            try
            {
                return new ChemicalStructureSource(devices, this);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Failed to create shape source: {ex.Message}");
                throw;
            }
        }

        protected override IEnumerable<IAnimatable> GetAnimatables()
        {
            var result = new List<IAnimatable>
            {
                Scale
            };

            try
            {
                if (Atoms != null)
                    result.AddRange(Atoms.Cast<IAnimatable>());
                if (Bonds != null)
                    result.AddRange(Bonds.Cast<IAnimatable>());
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error getting animatables: {ex.Message}");
            }

            return result;
        }

        public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskDesc)
        {
            return Enumerable.Empty<string>();
        }

        public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc)
        {
            return Enumerable.Empty<string>();
        }

        protected override void LoadSharedData(SharedDataStore store)
        {
            try
            {
                var data = store?.Load<SharedData>();
                if (data != null)
                {
                    data.CopyTo(this);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error loading shared data: {ex.Message}");
            }
        }

        protected override void SaveSharedData(SharedDataStore store)
        {
            try
            {
                store?.Save(new SharedData(this));
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error saving shared data: {ex.Message}");
            }
        }

        private void ApplyAutoLayout()
        {
            if (AutoLayout == AutoLayoutType.None || !Atoms.Any()) return;

            try
            {
                switch (AutoLayout)
                {
                    case AutoLayoutType.Linear:
                        ApplyLinearLayout();
                        break;
                    case AutoLayoutType.Branched:
                        ApplyBranchedLayout();
                        break;
                    case AutoLayoutType.Optimized:
                        ApplyOptimizedLayout();
                        break;
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error applying auto layout: {ex.Message}");
            }
        }

        private void ApplyLinearLayout()
        {
            if (!Atoms.Any()) return;

            var atoms = Atoms.ToList();
            var spacing = DefaultBondLength * SpacingMultiplier;
            var startX = -(atoms.Count - 1) * spacing / 2.0;

            for (int i = 0; i < atoms.Count; i++)
            {
                atoms[i].X.Values[0].Value = startX + i * spacing;
                atoms[i].Y.Values[0].Value = 0;
            }

            Atoms = atoms.ToImmutableList();
        }

        private void ApplyBranchedLayout()
        {
            if (!Atoms.Any()) return;

            var atoms = Atoms.ToList();
            var bonds = Bonds.ToList();

            if (!bonds.Any())
            {
                ApplyLinearLayout();
                return;
            }

            var atomConnections = new Dictionary<Guid, List<Guid>>();
            foreach (var bond in bonds)
            {
                if (!atomConnections.ContainsKey(bond.Atom1Id))
                    atomConnections[bond.Atom1Id] = new List<Guid>();
                if (!atomConnections.ContainsKey(bond.Atom2Id))
                    atomConnections[bond.Atom2Id] = new List<Guid>();

                atomConnections[bond.Atom1Id].Add(bond.Atom2Id);
                atomConnections[bond.Atom2Id].Add(bond.Atom1Id);
            }

            var startAtom = atoms.FirstOrDefault(a => atomConnections.GetValueOrDefault(a.Id, new List<Guid>()).Count <= 1);
            if (startAtom == null) startAtom = atoms[0];

            var visited = new HashSet<Guid>();
            var queue = new Queue<(Guid atomId, double x, double y, double angle)>();

            queue.Enqueue((startAtom.Id, 0, 0, 0));

            while (queue.Count > 0)
            {
                var (atomId, x, y, angle) = queue.Dequeue();
                if (visited.Contains(atomId)) continue;

                visited.Add(atomId);
                var atom = atoms.FirstOrDefault(a => a.Id == atomId);
                if (atom != null)
                {
                    atom.X.Values[0].Value = x;
                    atom.Y.Values[0].Value = y;
                }

                var connections = atomConnections.GetValueOrDefault(atomId, new List<Guid>());
                var unvisitedConnections = connections.Where(id => !visited.Contains(id)).ToList();

                for (int i = 0; i < unvisitedConnections.Count; i++)
                {
                    var nextAngle = angle + (i - (unvisitedConnections.Count - 1) / 2.0) * Math.PI / 3;
                    var distance = DefaultBondLength * SpacingMultiplier;
                    var nextX = x + distance * Math.Cos(nextAngle);
                    var nextY = y + distance * Math.Sin(nextAngle);

                    queue.Enqueue((unvisitedConnections[i], nextX, nextY, nextAngle));
                }
            }

            Atoms = atoms.ToImmutableList();
        }

        private void ApplyOptimizedLayout()
        {
            if (Atoms.Count < 3) return;

            var atoms = Atoms.ToList();
            var bonds = Bonds.ToList();

            for (int iteration = 0; iteration < 100; iteration++)
            {
                var forces = new Dictionary<Guid, (double fx, double fy)>();

                foreach (var atom in atoms)
                {
                    forces[atom.Id] = (0, 0);
                }

                for (int i = 0; i < atoms.Count; i++)
                {
                    for (int j = i + 1; j < atoms.Count; j++)
                    {
                        var atom1 = atoms[i];
                        var atom2 = atoms[j];
                        var x1 = atom1.X.Values.FirstOrDefault()?.Value ?? 0;
                        var y1 = atom1.Y.Values.FirstOrDefault()?.Value ?? 0;
                        var x2 = atom2.X.Values.FirstOrDefault()?.Value ?? 0;
                        var y2 = atom2.Y.Values.FirstOrDefault()?.Value ?? 0;

                        var dx = x1 - x2;
                        var dy = y1 - y2;
                        var distance = Math.Sqrt(dx * dx + dy * dy);

                        if (distance > 0.1)
                        {
                            var repulsion = 2000.0 / (distance * distance);
                            var fx = repulsion * dx / distance;
                            var fy = repulsion * dy / distance;

                            var force1 = forces[atom1.Id];
                            forces[atom1.Id] = (force1.fx + fx, force1.fy + fy);
                            var force2 = forces[atom2.Id];
                            forces[atom2.Id] = (force2.fx - fx, force2.fy - fy);
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
                    var idealLength = DefaultBondLength * SpacingMultiplier;

                    if (distance > 0.1)
                    {
                        var spring = 0.2 * (distance - idealLength);
                        var fx = spring * dx / distance;
                        var fy = spring * dy / distance;

                        var force1 = forces[atom1.Id];
                        forces[atom1.Id] = (force1.fx + fx, force1.fy + fy);
                        var force2 = forces[atom2.Id];
                        forces[atom2.Id] = (force2.fx - fx, force2.fy - fy);
                    }
                }

                var damping = 0.05;
                var maxForce = 0.0;

                foreach (var atom in atoms)
                {
                    if (forces.TryGetValue(atom.Id, out var force))
                    {
                        var forceLength = Math.Sqrt(force.fx * force.fx + force.fy * force.fy);
                        maxForce = Math.Max(maxForce, forceLength);

                        var newX = (atom.X.Values.FirstOrDefault()?.Value ?? 0) + force.fx * damping;
                        var newY = (atom.Y.Values.FirstOrDefault()?.Value ?? 0) + force.fy * damping;

                        atom.X.Values[0].Value = Math.Max(-1000, Math.Min(1000, newX));
                        atom.Y.Values[0].Value = Math.Max(-1000, Math.Min(1000, newY));
                    }
                }

                if (maxForce < 1.0) break;
            }

            Atoms = atoms.ToImmutableList();
        }

        public class SharedData
        {
            public ImmutableList<Atom> Atoms { get; }
            public ImmutableList<Bond> Bonds { get; }
            public ChemicalDisplayMode DisplayMode { get; }
            public Animation Scale { get; }
            public Color DefaultBondColor { get; }
            public Color DefaultTextColor { get; }
            public Color BackgroundColor { get; }
            public double DefaultBondThickness { get; }
            public double DefaultFontSize { get; }
            public double DefaultAtomSize { get; }
            public double DefaultBondLength { get; }
            public bool ShowHydrogen { get; }
            public bool ShowCharges { get; }
            public bool ShowLonePairs { get; }
            public bool AntiAliasing { get; }
            public bool UseSkeletalFormula { get; }
            public RenderingQuality RenderingQuality { get; }
            public AutoLayoutType AutoLayout { get; }
            public double RingSize { get; }
            public double SpacingMultiplier { get; }
            public bool AutoOptimize { get; }
            public string FontFamily { get; }
            public double OutlineThickness { get; }
            public Color OutlineColor { get; }

            public SharedData(ChemicalStructureParameter parameter)
            {
                if (parameter == null)
                {
                    Atoms = ImmutableList<Atom>.Empty;
                    Bonds = ImmutableList<Bond>.Empty;
                    DisplayMode = ChemicalDisplayMode.Structural;
                    Scale = new Animation(1.0, 0.1, 10.0);
                    DefaultBondColor = Colors.Black;
                    DefaultTextColor = Colors.Black;
                    BackgroundColor = Colors.Transparent;
                    DefaultBondThickness = 2.0;
                    DefaultFontSize = 24;
                    DefaultAtomSize = 20;
                    DefaultBondLength = 80;
                    ShowHydrogen = true;
                    ShowCharges = true;
                    ShowLonePairs = false;
                    AntiAliasing = true;
                    UseSkeletalFormula = false;
                    RenderingQuality = RenderingQuality.Normal;
                    AutoLayout = AutoLayoutType.None;
                    RingSize = 80;
                    SpacingMultiplier = 1.0;
                    AutoOptimize = false;
                    FontFamily = "メイリオ";
                    OutlineThickness = 0;
                    OutlineColor = Colors.Black;
                    return;
                }

                try
                {
                    Atoms = parameter.Atoms?.Select(a => new Atom(a)).ToImmutableList() ?? ImmutableList<Atom>.Empty;
                    Bonds = parameter.Bonds?.Select(b => new Bond(b)).ToImmutableList() ?? ImmutableList<Bond>.Empty;
                    DisplayMode = parameter.DisplayMode;
                    Scale = new Animation(1.0, 0.1, 10.0);
                    Scale.CopyFrom(parameter.Scale);
                    DefaultBondColor = parameter.DefaultBondColor;
                    DefaultTextColor = parameter.DefaultTextColor;
                    BackgroundColor = parameter.BackgroundColor;
                    DefaultBondThickness = parameter.DefaultBondThickness;
                    DefaultFontSize = parameter.DefaultFontSize;
                    DefaultAtomSize = parameter.DefaultAtomSize;
                    DefaultBondLength = parameter.DefaultBondLength;
                    ShowHydrogen = parameter.ShowHydrogen;
                    ShowCharges = parameter.ShowCharges;
                    ShowLonePairs = parameter.ShowLonePairs;
                    AntiAliasing = parameter.AntiAliasing;
                    UseSkeletalFormula = parameter.UseSkeletalFormula;
                    RenderingQuality = parameter.RenderingQuality;
                    AutoLayout = parameter.AutoLayout;
                    RingSize = parameter.RingSize;
                    SpacingMultiplier = parameter.SpacingMultiplier;
                    AutoOptimize = parameter.AutoOptimize;
                    FontFamily = parameter.FontFamily;
                    OutlineThickness = parameter.OutlineThickness;
                    OutlineColor = parameter.OutlineColor;
                }
                catch (Exception ex)
                {
                    AsyncLogger.Instance.Log(LogType.Error, $"Error creating SharedData: {ex.Message}");
                    Atoms = ImmutableList<Atom>.Empty;
                    Bonds = ImmutableList<Bond>.Empty;
                    DisplayMode = ChemicalDisplayMode.Structural;
                    Scale = new Animation(1.0, 0.1, 10.0);
                    DefaultBondColor = Colors.Black;
                    DefaultTextColor = Colors.Black;
                    BackgroundColor = Colors.Transparent;
                    DefaultBondThickness = 2.0;
                    DefaultFontSize = 24;
                    DefaultAtomSize = 20;
                    DefaultBondLength = 80;
                    ShowHydrogen = true;
                    ShowCharges = true;
                    ShowLonePairs = false;
                    AntiAliasing = true;
                    UseSkeletalFormula = false;
                    RenderingQuality = RenderingQuality.Normal;
                    AutoLayout = AutoLayoutType.None;
                    RingSize = 80;
                    SpacingMultiplier = 1.0;
                    AutoOptimize = false;
                    FontFamily = "メイリオ";
                    OutlineThickness = 0;
                    OutlineColor = Colors.Black;
                }
            }

            public void CopyTo(ChemicalStructureParameter parameter)
            {
                if (parameter == null) return;

                try
                {
                    parameter.Atoms = Atoms?.Select(a => new Atom(a)).ToImmutableList() ?? ImmutableList<Atom>.Empty;
                    parameter.Bonds = Bonds?.Select(b => new Bond(b)).ToImmutableList() ?? ImmutableList<Bond>.Empty;
                    parameter.DisplayMode = DisplayMode;
                    parameter.Scale.CopyFrom(Scale);
                    parameter.DefaultBondColor = DefaultBondColor;
                    parameter.DefaultTextColor = DefaultTextColor;
                    parameter.BackgroundColor = BackgroundColor;
                    parameter.DefaultBondThickness = DefaultBondThickness;
                    parameter.DefaultFontSize = DefaultFontSize;
                    parameter.DefaultAtomSize = DefaultAtomSize;
                    parameter.DefaultBondLength = DefaultBondLength;
                    parameter.ShowHydrogen = ShowHydrogen;
                    parameter.ShowCharges = ShowCharges;
                    parameter.ShowLonePairs = ShowLonePairs;
                    parameter.AntiAliasing = AntiAliasing;
                    parameter.UseSkeletalFormula = UseSkeletalFormula;
                    parameter.RenderingQuality = RenderingQuality;
                    parameter.AutoLayout = AutoLayout;
                    parameter.RingSize = RingSize;
                    parameter.SpacingMultiplier = SpacingMultiplier;
                    parameter.AutoOptimize = AutoOptimize;
                    parameter.FontFamily = FontFamily;
                    parameter.OutlineThickness = OutlineThickness;
                    parameter.OutlineColor = OutlineColor;
                }
                catch (Exception ex)
                {
                    AsyncLogger.Instance.Log(LogType.Error, $"Error copying SharedData: {ex.Message}");
                }
            }
        }
    }
}