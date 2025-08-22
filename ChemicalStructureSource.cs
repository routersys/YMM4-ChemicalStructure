using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Media;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using VorticeColor = Vortice.Mathematics.Color4;
using WPFColor = System.Windows.Media.Color;

namespace YMM4ChemicalStructurePlugin.Shape
{
    internal class ChemicalStructureSource : IShapeSource2, IDisposable
    {
        private readonly IGraphicsDevicesAndContext devices;
        private readonly ChemicalStructureParameter parameter;
        private readonly DisposeCollector disposer = new();
        private bool isDisposed = false;

        public ID2D1Image Output => commandList ?? throw new InvalidOperationException("commandList is null");
        private ID2D1CommandList? commandList;

        public IEnumerable<VideoController> Controllers { get; private set; } = Enumerable.Empty<VideoController>();

        private readonly IDWriteFactory? dwriteFactory;
        private IDWriteTextFormat? textFormat;
        private IDWriteTextFormat? smallTextFormat;
        private readonly ID2D1StrokeStyle? dashStrokeStyle;
        private readonly ID2D1StrokeStyle? wavyStrokeStyle;
        private readonly ID2D1StrokeStyle? dottedStrokeStyle;

        private readonly Dictionary<WPFColor, ID2D1SolidColorBrush> brushCache = new();
        private readonly Dictionary<string, IDWriteTextFormat> textFormatCache = new();

        private float currentScale = 1.0f;
        private Vector2 currentCenter = Vector2.Zero;
        private float qualityMultiplier = 1.0f;

        public ChemicalStructureSource(IGraphicsDevicesAndContext devices, ChemicalStructureParameter parameter)
        {
            this.devices = devices ?? throw new ArgumentNullException(nameof(devices));
            this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));

            try
            {
                dwriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
                disposer.Collect(dwriteFactory);

                CreateTextFormats();

                dashStrokeStyle = devices.D2D.Factory.CreateStrokeStyle(
                    new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Custom },
                    new float[] { 4, 2 }
                );
                disposer.Collect(dashStrokeStyle);

                wavyStrokeStyle = devices.D2D.Factory.CreateStrokeStyle(
                    new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Custom },
                    new float[] { 2, 2 }
                );
                disposer.Collect(wavyStrokeStyle);

                dottedStrokeStyle = devices.D2D.Factory.CreateStrokeStyle(
                    new StrokeStyleProperties { DashStyle = Vortice.Direct2D1.DashStyle.Custom },
                    new float[] { 1, 3 }
                );
                disposer.Collect(dottedStrokeStyle);

                UpdateQualityMultiplier();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing ChemicalStructureSource: {ex.Message}");
                Dispose();
                throw;
            }
        }

        private void UpdateQualityMultiplier()
        {
            qualityMultiplier = parameter.RenderingQuality switch
            {
                RenderingQuality.Low => 0.5f,
                RenderingQuality.Normal => 1.0f,
                RenderingQuality.High => 1.5f,
                RenderingQuality.Ultra => 2.0f,
                _ => 1.0f
            };
        }

        private void CreateTextFormats()
        {
            if (dwriteFactory == null || isDisposed) return;

            try
            {
                disposer.RemoveAndDispose(ref textFormat);
                disposer.RemoveAndDispose(ref smallTextFormat);

                textFormat = dwriteFactory.CreateTextFormat(
                    parameter.FontFamily,
                    null,
                    FontWeight.Bold,
                    FontStyle.Normal,
                    FontStretch.Normal,
                    (float)(parameter.DefaultFontSize * qualityMultiplier),
                    "ja-jp"
                );
                if (textFormat != null)
                {
                    textFormat.TextAlignment = TextAlignment.Center;
                    textFormat.ParagraphAlignment = ParagraphAlignment.Center;
                    disposer.Collect(textFormat);
                }

                smallTextFormat = dwriteFactory.CreateTextFormat(
                    parameter.FontFamily,
                    null,
                    FontWeight.Normal,
                    FontStyle.Normal,
                    FontStretch.Normal,
                    (float)(Math.Round(parameter.DefaultFontSize * 0.7) * qualityMultiplier),
                    "ja-jp"
                );
                if (smallTextFormat != null)
                {
                    smallTextFormat.TextAlignment = TextAlignment.Center;
                    smallTextFormat.ParagraphAlignment = ParagraphAlignment.Center;
                    disposer.Collect(smallTextFormat);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error creating text formats: {ex.Message}");
            }
        }

        private IDWriteTextFormat? GetTextFormatForSize(float fontSize)
        {
            if (dwriteFactory == null || isDisposed) return null;

            var adjustedSize = fontSize * qualityMultiplier;
            var key = $"{parameter.FontFamily}_{adjustedSize:F1}";
            if (textFormatCache.TryGetValue(key, out var cachedFormat))
            {
                return cachedFormat;
            }

            try
            {
                var format = dwriteFactory.CreateTextFormat(
                    parameter.FontFamily,
                    null,
                    FontWeight.Bold,
                    FontStyle.Normal,
                    FontStretch.Normal,
                    adjustedSize,
                    "ja-jp"
                );
                if (format != null)
                {
                    format.TextAlignment = TextAlignment.Center;
                    format.ParagraphAlignment = ParagraphAlignment.Center;
                    textFormatCache[key] = format;
                    disposer.Collect(format);
                    return format;
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error creating text format for size {adjustedSize}: {ex.Message}");
            }

            return textFormat;
        }

        public void Update(TimelineItemSourceDescription desc)
        {
            if (isDisposed || parameter == null) return;

            try
            {
                var frame = desc.ItemPosition.Frame;
                var itemLength = desc.ItemDuration.Frame;
                var fps = desc.FPS;

                UpdateQualityMultiplier();
                CreateTextFormats();

                var dc = devices.DeviceContext;
                if (dc == null) return;

                disposer.RemoveAndDispose(ref commandList);
                commandList = dc.CreateCommandList();
                if (commandList == null) return;

                disposer.Collect(commandList);

                dc.Target = commandList;
                dc.BeginDraw();

                dc.AntialiasMode = parameter.AntiAliasing ? AntialiasMode.PerPrimitive : AntialiasMode.Aliased;
                dc.TextAntialiasMode = parameter.AntiAliasing ? Vortice.Direct2D1.TextAntialiasMode.Cleartype : Vortice.Direct2D1.TextAntialiasMode.Aliased;

                if (parameter.BackgroundColor.A > 0)
                {
                    dc.Clear(ToVorticeColor(parameter.BackgroundColor));
                }
                else
                {
                    dc.Clear(null);
                }

                currentScale = (float)(parameter.Scale.GetValue(frame, itemLength, (int)fps) * qualityMultiplier);
                currentCenter = Vector2.Zero;

                var transform = Matrix3x2.CreateScale(currentScale);
                dc.Transform = transform;

                if (parameter.Atoms?.Any() == true)
                {
                    var atomPositions = CalculateAtomPositions(frame, itemLength, fps);

                    switch (parameter.DisplayMode)
                    {
                        case ChemicalDisplayMode.Structural:
                            DrawStructuralFormula(dc, atomPositions, frame, itemLength, fps, 1.0f);
                            break;
                        case ChemicalDisplayMode.Skeletal:
                            DrawSkeletalFormula(dc, atomPositions, frame, itemLength, fps, 1.0f);
                            break;
                        case ChemicalDisplayMode.Linear:
                            DrawLinearFormula(dc, 1.0f);
                            break;
                        case ChemicalDisplayMode.Molecular:
                            DrawMolecularFormula(dc, 1.0f);
                            break;
                        case ChemicalDisplayMode.Constitutional:
                            DrawConstitutionalFormula(dc, atomPositions, frame, itemLength, fps, 1.0f);
                            break;
                        case ChemicalDisplayMode.Electronic:
                            DrawElectronicFormula(dc, atomPositions, frame, itemLength, fps, 1.0f);
                            break;
                    }

                    CreateControllers(frame, itemLength, fps);
                }
                else
                {
                    Controllers = Enumerable.Empty<VideoController>();
                }

                dc.EndDraw();
                dc.Target = null;
                commandList.Close();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error in Update: {ex.Message}");

                try
                {
                    if (devices.DeviceContext?.Target != null)
                        devices.DeviceContext.Target = null;
                }
                catch { }
            }
        }

        private Dictionary<Guid, Vector3> CalculateAtomPositions(int frame, int itemLength, double fps)
        {
            var result = new Dictionary<Guid, Vector3>();

            try
            {
                if (parameter.Atoms == null) return result;

                foreach (var atom in parameter.Atoms)
                {
                    var x = (float)(atom.X?.GetValue(frame, itemLength, (int)fps) ?? 0);
                    var y = (float)(atom.Y?.GetValue(frame, itemLength, (int)fps) ?? 0);
                    var z = (float)(atom.Z?.GetValue(frame, itemLength, (int)fps) ?? 0);
                    result[atom.Id] = new Vector3(x, y, z);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error calculating atom positions: {ex.Message}");
            }

            return result;
        }

        private void DrawStructuralFormula(ID2D1DeviceContext dc, Dictionary<Guid, Vector3> atomPositions, int frame, int itemLength, double fps, float opacity)
        {
            if (dc == null || parameter.Bonds == null || parameter.Atoms == null) return;

            try
            {
                foreach (var bond in parameter.Bonds)
                {
                    if (atomPositions.TryGetValue(bond.Atom1Id, out var pos1) &&
                        atomPositions.TryGetValue(bond.Atom2Id, out var pos2))
                    {
                        DrawBond(dc, new Vector2(pos1.X, pos1.Y), new Vector2(pos2.X, pos2.Y), bond, frame, itemLength, fps, opacity);
                    }
                }

                foreach (var atom in parameter.Atoms)
                {
                    if (atomPositions.TryGetValue(atom.Id, out var position))
                    {
                        DrawAtom(dc, new Vector2(position.X, position.Y), atom, opacity);
                    }
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing structural formula: {ex.Message}");
            }
        }

        private void DrawSkeletalFormula(ID2D1DeviceContext dc, Dictionary<Guid, Vector3> atomPositions, int frame, int itemLength, double fps, float opacity)
        {
            if (dc == null || parameter.Bonds == null || parameter.Atoms == null) return;

            try
            {
                foreach (var bond in parameter.Bonds)
                {
                    if (atomPositions.TryGetValue(bond.Atom1Id, out var pos1) &&
                        atomPositions.TryGetValue(bond.Atom2Id, out var pos2))
                    {
                        DrawBond(dc, new Vector2(pos1.X, pos1.Y), new Vector2(pos2.X, pos2.Y), bond, frame, itemLength, fps, opacity);
                    }
                }

                foreach (var atom in parameter.Atoms)
                {
                    if (atomPositions.TryGetValue(atom.Id, out var position))
                    {
                        if (ShouldShowAtomInSkeleton(atom))
                        {
                            DrawAtom(dc, new Vector2(position.X, position.Y), atom, opacity);
                        }
                        else
                        {
                            DrawSkeletalVertex(dc, new Vector2(position.X, position.Y), atom, opacity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing skeletal formula: {ex.Message}");
            }
        }

        private bool ShouldShowAtomInSkeleton(Atom atom)
        {
            if (!parameter.UseSkeletalFormula) return true;

            if (atom.Element != "C") return true;

            if (atom.Charge != 0) return true;

            if (atom.HydrogenCount > 0) return true;

            if (atom.DisplayMode == AtomDisplayMode.Normal || atom.DisplayMode == AtomDisplayMode.SymbolOnly) return true;

            var connections = GetAtomConnections(atom.Id);
            if (connections.Count <= 1 || connections.Count > 4) return true;

            return false;
        }

        private void DrawSkeletalVertex(ID2D1DeviceContext dc, Vector2 position, Atom atom, float opacity)
        {
            if (dc == null || atom == null) return;

            try
            {
                var connections = GetAtomConnections(atom.Id);

                if (connections.Count == 2)
                {
                    return;
                }

                if (connections.Count != 2)
                {
                    var dotRadius = 1.5f * qualityMultiplier;
                    var ellipse = new Ellipse(position, dotRadius, dotRadius);
                    using var brush = GetBrush(parameter.DefaultTextColor, opacity * 0.6f);
                    if (brush != null)
                    {
                        dc.FillEllipse(ellipse, brush);
                    }
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing skeletal vertex: {ex.Message}");
            }
        }

        private List<Guid> GetAtomConnections(Guid atomId)
        {
            var connections = new List<Guid>();
            if (parameter.Bonds == null) return connections;

            foreach (var bond in parameter.Bonds)
            {
                if (bond.Atom1Id == atomId)
                    connections.Add(bond.Atom2Id);
                else if (bond.Atom2Id == atomId)
                    connections.Add(bond.Atom1Id);
            }

            return connections;
        }

        private void DrawLinearFormula(ID2D1DeviceContext dc, float opacity)
        {
            if (dc == null || textFormat == null) return;

            try
            {
                var formula = GenerateLinearFormula();
                using var brush = GetBrush(parameter.DefaultTextColor, opacity);
                if (brush != null)
                {
                    var rect = new Vortice.RawRectF(-200 * qualityMultiplier, -50 * qualityMultiplier,
                                                   200 * qualityMultiplier, 50 * qualityMultiplier);
                    dc.DrawText(formula, textFormat, rect, brush);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing linear formula: {ex.Message}");
            }
        }

        private void DrawMolecularFormula(ID2D1DeviceContext dc, float opacity)
        {
            if (dc == null || textFormat == null) return;

            try
            {
                var formula = GenerateMolecularFormula();
                using var brush = GetBrush(parameter.DefaultTextColor, opacity);
                if (brush != null)
                {
                    var rect = new Vortice.RawRectF(-200 * qualityMultiplier, -50 * qualityMultiplier,
                                                   200 * qualityMultiplier, 50 * qualityMultiplier);
                    dc.DrawText(formula, textFormat, rect, brush);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing molecular formula: {ex.Message}");
            }
        }

        private void DrawConstitutionalFormula(ID2D1DeviceContext dc, Dictionary<Guid, Vector3> atomPositions, int frame, int itemLength, double fps, float opacity)
        {
            DrawStructuralFormula(dc, atomPositions, frame, itemLength, fps, opacity);
        }

        private void DrawElectronicFormula(ID2D1DeviceContext dc, Dictionary<Guid, Vector3> atomPositions, int frame, int itemLength, double fps, float opacity)
        {
            DrawStructuralFormula(dc, atomPositions, frame, itemLength, fps, opacity);

            if (parameter.ShowLonePairs && parameter.Atoms != null)
            {
                try
                {
                    foreach (var atom in parameter.Atoms)
                    {
                        if (atomPositions.TryGetValue(atom.Id, out var position))
                        {
                            DrawLonePairs(dc, new Vector2(position.X, position.Y), atom, opacity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AsyncLogger.Instance.Log(LogType.Error, $"Error drawing lone pairs: {ex.Message}");
                }
            }
        }

        private void DrawBond(ID2D1DeviceContext dc, Vector2 p1, Vector2 p2, Bond bond, int frame, int itemLength, double fps, float globalOpacity)
        {
            if (dc == null || bond == null || p1 == p2 || bond.Type == BondType.Hidden) return;

            try
            {
                var effectiveOpacity = globalOpacity * (float)bond.Opacity;
                using var bondBrush = GetBrush(bond.Color, effectiveOpacity);
                if (bondBrush == null) return;

                var thickness = (float)(Math.Max(0.1, bond.Thickness) * qualityMultiplier);
                var lengthMultiplier = (float)Math.Max(0.1, bond.LengthMultiplier?.GetValue(frame, itemLength, (int)fps) ?? 1.0);
                var offset = (float)(bond.Offset?.GetValue(frame, itemLength, (int)fps) ?? 0);

                var direction = Vector2.Normalize(p2 - p1);
                var center = (p1 + p2) / 2f;
                var bondLength = Vector2.Distance(p1, p2) * lengthMultiplier;
                var perpendicular = new Vector2(-direction.Y, direction.X);

                p1 = center - direction * bondLength / 2f + perpendicular * offset;
                p2 = center + direction * bondLength / 2f + perpendicular * offset;

                switch (bond.Type)
                {
                    case BondType.Single:
                        dc.DrawLine(p1, p2, bondBrush, thickness);
                        break;
                    case BondType.Double:
                        var spacing = thickness + 2f * qualityMultiplier;
                        dc.DrawLine(p1 + perpendicular * spacing / 2f, p2 + perpendicular * spacing / 2f, bondBrush, thickness);
                        dc.DrawLine(p1 - perpendicular * spacing / 2f, p2 - perpendicular * spacing / 2f, bondBrush, thickness);
                        break;
                    case BondType.Triple:
                        var tripleSpacing = thickness + 1f * qualityMultiplier;
                        dc.DrawLine(p1, p2, bondBrush, thickness);
                        dc.DrawLine(p1 + perpendicular * tripleSpacing, p2 + perpendicular * tripleSpacing, bondBrush, thickness);
                        dc.DrawLine(p1 - perpendicular * tripleSpacing, p2 - perpendicular * tripleSpacing, bondBrush, thickness);
                        break;
                    case BondType.Wedge:
                        DrawWedgeBond(dc, p1, p2, bondBrush, thickness);
                        break;
                    case BondType.Dash:
                        if (dashStrokeStyle != null)
                            dc.DrawLine(p1, p2, bondBrush, thickness, dashStrokeStyle);
                        break;
                    case BondType.Wavy:
                        DrawWavyBond(dc, p1, p2, bondBrush, thickness);
                        break;
                    case BondType.Partial:
                        if (dottedStrokeStyle != null)
                            dc.DrawLine(p1, p2, bondBrush, thickness, dottedStrokeStyle);
                        break;
                    case BondType.Coordinate:
                        DrawCoordinateBond(dc, p1, p2, bondBrush, thickness);
                        break;
                    case BondType.Aromatic:
                        dc.DrawLine(p1, p2, bondBrush, thickness);
                        var aromaticOffset = perpendicular * (thickness + 3f * qualityMultiplier);
                        if (dashStrokeStyle != null)
                            dc.DrawLine(p1 + aromaticOffset, p2 + aromaticOffset, bondBrush, thickness * 0.7f, dashStrokeStyle);
                        break;
                    case BondType.Hydrogen:
                        if (dottedStrokeStyle != null)
                            dc.DrawLine(p1, p2, bondBrush, thickness * 0.5f, dottedStrokeStyle);
                        break;
                    case BondType.Ionic:
                        DrawIonicBond(dc, p1, p2, bondBrush, thickness);
                        break;
                }

                if (bond.EndStyle != BondEndStyle.Normal)
                {
                    DrawBondEndStyle(dc, p1, p2, bond.EndStyle, bondBrush, thickness);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing bond: {ex.Message}");
            }
        }

        private void DrawAtom(ID2D1DeviceContext dc, Vector2 position, Atom atom, float globalOpacity)
        {
            if (dc == null || atom == null || atom.DisplayMode == AtomDisplayMode.Hidden) return;

            try
            {
                var radius = (float)(Math.Max(0.0, atom.Radius) * qualityMultiplier);
                var fontSize = (float)(Math.Max(8.0, atom.FontSize) * qualityMultiplier);

                var atomFormat = GetTextFormatForSize((float)atom.FontSize);
                if (atomFormat == null) atomFormat = textFormat;

                if (radius > 0 && (atom.DisplayMode == AtomDisplayMode.Normal || atom.DisplayMode == AtomDisplayMode.Electron))
                {
                    var ellipse = new Ellipse(position, radius, radius);

                    using var fillBrush = GetBrush(atom.FillColor, globalOpacity);
                    using var strokeBrush = GetBrush(parameter.DefaultBondColor, globalOpacity);

                    if (fillBrush != null)
                        dc.FillEllipse(ellipse, fillBrush);
                    if (strokeBrush != null && parameter.OutlineThickness > 0)
                        dc.DrawEllipse(ellipse, strokeBrush, (float)(parameter.OutlineThickness * qualityMultiplier));
                }

                if (atom.DisplayMode != AtomDisplayMode.Hidden && atomFormat != null)
                {
                    using var textBrush = GetBrush(atom.TextColor, globalOpacity);
                    if (textBrush != null)
                    {
                        var textRadius = Math.Max(radius, fontSize);
                        var rect = new Vortice.RawRectF(position.X - textRadius, position.Y - textRadius, position.X + textRadius, position.Y + textRadius);
                        string displayText = atom.Element ?? "C";

                        if (parameter.ShowHydrogen && atom.HydrogenCount > 0)
                        {
                            displayText += "H";
                            if (atom.HydrogenCount > 1)
                                displayText += atom.HydrogenCount.ToString();
                        }

                        dc.DrawText(displayText, atomFormat, rect, textBrush);

                        if (parameter.ShowCharges && atom.Charge != 0 && smallTextFormat != null)
                        {
                            var chargeText = atom.Charge > 0 ? $"+{atom.Charge}" : atom.Charge.ToString();
                            var chargeRect = new Vortice.RawRectF(position.X + textRadius, position.Y - textRadius,
                                                                position.X + textRadius + 30f * qualityMultiplier, position.Y);
                            dc.DrawText(chargeText, smallTextFormat, chargeRect, textBrush);
                        }
                    }
                }

                if (atom.DisplayMode == AtomDisplayMode.Electron)
                {
                    DrawElectrons(dc, position, atom, globalOpacity);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing atom: {ex.Message}");
            }
        }

        private void DrawWedgeBond(ID2D1DeviceContext dc, Vector2 p1, Vector2 p2, ID2D1SolidColorBrush brush, float thickness)
        {
            if (dc == null || brush == null) return;

            try
            {
                var perpendicular = Vector2.Normalize(new Vector2(-(p2.Y - p1.Y), p2.X - p1.X));
                var wedgeWidth = thickness * 3f;

                var path = devices.D2D.Factory.CreatePathGeometry();
                using (var sink = path.Open())
                {
                    sink.BeginFigure(p1, FigureBegin.Filled);
                    sink.AddLine(p2 + perpendicular * wedgeWidth / 2f);
                    sink.AddLine(p2 - perpendicular * wedgeWidth / 2f);
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }
                dc.FillGeometry(path, brush);
                path.Dispose();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing wedge bond: {ex.Message}");
            }
        }

        private void DrawWavyBond(ID2D1DeviceContext dc, Vector2 p1, Vector2 p2, ID2D1SolidColorBrush brush, float thickness)
        {
            if (dc == null || brush == null) return;

            try
            {
                var bondLength = Vector2.Distance(p1, p2);
                var segments = Math.Max(1, (int)Math.Round(bondLength / (10f * qualityMultiplier)));
                var perpendicular = Vector2.Normalize(new Vector2(-(p2.Y - p1.Y), p2.X - p1.X));

                var path = devices.D2D.Factory.CreatePathGeometry();
                using (var sink = path.Open())
                {
                    sink.BeginFigure(p1, FigureBegin.Hollow);

                    for (int i = 1; i <= segments; i++)
                    {
                        var t = (float)i / segments;
                        var point = Vector2.Lerp(p1, p2, t);
                        var amplitude = (float)Math.Sin(t * Math.PI * 4) * thickness;
                        point += perpendicular * amplitude;
                        sink.AddLine(point);
                    }

                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();
                }
                dc.DrawGeometry(path, brush, thickness);
                path.Dispose();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing wavy bond: {ex.Message}");
            }
        }

        private void DrawCoordinateBond(ID2D1DeviceContext dc, Vector2 p1, Vector2 p2, ID2D1SolidColorBrush brush, float thickness)
        {
            if (dc == null || brush == null) return;

            try
            {
                dc.DrawLine(p1, p2, brush, thickness);

                var direction = Vector2.Normalize(p2 - p1);
                var arrowLength = thickness * 3f;
                var arrowAngle = (float)(Math.PI / 6);

                var rotated1 = RotateVector(direction * arrowLength, arrowAngle);
                var rotated2 = RotateVector(direction * arrowLength, -arrowAngle);

                dc.DrawLine(p2, p2 - rotated1, brush, thickness);
                dc.DrawLine(p2, p2 - rotated2, brush, thickness);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing coordinate bond: {ex.Message}");
            }
        }

        private void DrawIonicBond(ID2D1DeviceContext dc, Vector2 p1, Vector2 p2, ID2D1SolidColorBrush brush, float thickness)
        {
            if (dc == null || brush == null) return;

            try
            {
                var symbolSize = thickness * 2f;
                dc.DrawLine(p1 - Vector2.UnitX * symbolSize, p1 + Vector2.UnitX * symbolSize, brush, thickness);
                dc.DrawLine(p1 - Vector2.UnitY * symbolSize, p1 + Vector2.UnitY * symbolSize, brush, thickness);
                dc.DrawLine(p2 - Vector2.UnitX * symbolSize, p2 + Vector2.UnitX * symbolSize, brush, thickness);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing ionic bond: {ex.Message}");
            }
        }

        private void DrawBondEndStyle(ID2D1DeviceContext dc, Vector2 p1, Vector2 p2, BondEndStyle endStyle, ID2D1SolidColorBrush brush, float thickness)
        {
            if (dc == null || brush == null) return;

            try
            {
                var direction = Vector2.Normalize(p2 - p1);
                var endSize = thickness * 2f;

                switch (endStyle)
                {
                    case BondEndStyle.Arrow:
                        var arrowAngle = (float)(Math.PI / 6);
                        var arrow1 = p2 - RotateVector(direction * endSize, arrowAngle);
                        var arrow2 = p2 - RotateVector(direction * endSize, -arrowAngle);
                        dc.DrawLine(p2, arrow1, brush, thickness);
                        dc.DrawLine(p2, arrow2, brush, thickness);
                        break;

                    case BondEndStyle.Circle:
                        var circleEllipse = new Ellipse(p2, endSize, endSize);
                        dc.DrawEllipse(circleEllipse, brush, thickness);
                        break;

                    case BondEndStyle.Square:
                        var squareRect = new Vortice.RawRectF(p2.X - endSize, p2.Y - endSize, p2.X + endSize, p2.Y + endSize);
                        dc.DrawRectangle(squareRect, brush, thickness);
                        break;
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing bond end style: {ex.Message}");
            }
        }

        private void DrawLonePairs(ID2D1DeviceContext dc, Vector2 position, Atom atom, float globalOpacity)
        {
            if (dc == null || atom == null || !parameter.ShowLonePairs) return;

            try
            {
                var lonePairCount = GetLonePairCount(atom.Element);
                if (lonePairCount == 0) return;

                using var brush = GetBrush(parameter.DefaultTextColor, globalOpacity * 0.8f);
                if (brush == null) return;

                var dotRadius = 2f * qualityMultiplier;
                var distance = (float)(atom.Radius * qualityMultiplier) + 15f * qualityMultiplier;
                var angleOffset = GetBondAngleOffset(atom.Id);

                for (int i = 0; i < lonePairCount; i++)
                {
                    var angle = (float)(angleOffset + i * 2 * Math.PI / Math.Max(lonePairCount, 4));
                    var pairPosition = position + new Vector2(
                        (float)Math.Cos(angle) * distance,
                        (float)Math.Sin(angle) * distance
                    );

                    var pairSpacing = 4f * qualityMultiplier;
                    var offset = new Vector2((float)Math.Cos(angle + Math.PI / 2), (float)Math.Sin(angle + Math.PI / 2)) * pairSpacing / 2f;

                    var ellipse1 = new Ellipse(pairPosition - offset, dotRadius, dotRadius);
                    var ellipse2 = new Ellipse(pairPosition + offset, dotRadius, dotRadius);

                    dc.FillEllipse(ellipse1, brush);
                    dc.FillEllipse(ellipse2, brush);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing lone pairs: {ex.Message}");
            }
        }

        private float GetBondAngleOffset(Guid atomId)
        {
            if (parameter.Bonds == null) return 0;

            var bonds = parameter.Bonds.Where(b => b.Atom1Id == atomId || b.Atom2Id == atomId).ToList();
            if (bonds.Count == 0) return 0;

            return (float)(Math.PI / (2 + bonds.Count));
        }

        private void DrawElectrons(ID2D1DeviceContext dc, Vector2 position, Atom atom, float globalOpacity)
        {
            if (dc == null || atom == null) return;

            try
            {
                var electronCount = GetElectronCount(atom.Element, atom.Charge);
                using var brush = GetBrush(WPFColor.FromRgb(255, 0, 0), globalOpacity * 0.7f);
                if (brush == null) return;

                var dotRadius = 1.5f * qualityMultiplier;
                var orbitRadius = (float)(atom.Radius * qualityMultiplier) + 20f * qualityMultiplier;

                for (int i = 0; i < electronCount; i++)
                {
                    var angle = (float)(i * 2 * Math.PI / electronCount);
                    var electronPosition = position + new Vector2(
                        (float)Math.Cos(angle) * orbitRadius,
                        (float)Math.Sin(angle) * orbitRadius
                    );

                    var ellipse = new Ellipse(electronPosition, dotRadius, dotRadius);
                    dc.FillEllipse(ellipse, brush);
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error drawing electrons: {ex.Message}");
            }
        }

        private void CreateControllers(int frame, int itemLength, double fps)
        {
            if (parameter.Atoms == null) return;

            try
            {
                var controllers = new List<VideoController>();

                foreach (var atom in parameter.Atoms)
                {
                    var x = atom.X?.GetValue(frame, itemLength, (int)fps) ?? 0;
                    var y = atom.Y?.GetValue(frame, itemLength, (int)fps) ?? 0;

                    var worldX = (float)(currentCenter.X + x * currentScale);
                    var worldY = (float)(currentCenter.Y + y * currentScale);

                    var controllerPoints = new[]
                    {
                        new ControllerPoint(
                            new Vector3(worldX, worldY, 0),
                            (args) =>
                            {
                                var deltaX = args.Delta.X / currentScale;
                                var deltaY = args.Delta.Y / currentScale;
                                atom.X?.AddToEachValues(deltaX);
                                atom.Y?.AddToEachValues(deltaY);
                            }
                        )
                    };
                    controllers.Add(new VideoController(controllerPoints));
                }

                Controllers = controllers;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error creating controllers: {ex.Message}");
                Controllers = Enumerable.Empty<VideoController>();
            }
        }

        private ID2D1SolidColorBrush? GetBrush(WPFColor color, float opacity = 1.0f)
        {
            if (isDisposed || devices.DeviceContext == null) return null;

            try
            {
                var effectiveColor = WPFColor.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);

                if (!brushCache.TryGetValue(effectiveColor, out var brush))
                {
                    brush = devices.DeviceContext.CreateSolidColorBrush(ToVorticeColor(effectiveColor));
                    brushCache[effectiveColor] = brush;
                    disposer.Collect(brush);
                }
                else
                {
                    try
                    {
                        var testColor = brush.Color;
                    }
                    catch
                    {
                        brush = devices.DeviceContext.CreateSolidColorBrush(ToVorticeColor(effectiveColor));
                        brushCache[effectiveColor] = brush;
                        disposer.Collect(brush);
                    }
                }
                return brush;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error getting brush: {ex.Message}");
                return null;
            }
        }

        private static VorticeColor ToVorticeColor(WPFColor color)
        {
            return new VorticeColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        private static Vector2 RotateVector(Vector2 vector, float angle)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            return new Vector2(
                vector.X * cos - vector.Y * sin,
                vector.X * sin + vector.Y * cos
            );
        }

        private string GenerateLinearFormula()
        {
            try
            {
                if (parameter.Atoms == null || !parameter.Atoms.Any()) return "";

                var atoms = parameter.Atoms.OrderBy(a => a.X?.Values?.FirstOrDefault()?.Value ?? 0).ToList();
                var result = new StringBuilder();

                foreach (var atom in atoms)
                {
                    result.Append(atom.Element ?? "C");
                    if (atom.HydrogenCount > 0)
                    {
                        result.Append("H");
                        if (atom.HydrogenCount > 1)
                            result.Append(atom.HydrogenCount);
                    }
                    if (atom != atoms.Last())
                        result.Append("-");
                }

                return result.ToString();
            }
            catch
            {
                return "";
            }
        }

        private string GenerateMolecularFormula()
        {
            try
            {
                if (parameter.Atoms == null || !parameter.Atoms.Any()) return "";

                var elementCounts = new Dictionary<string, int>();

                foreach (var atom in parameter.Atoms)
                {
                    var element = atom.Element ?? "C";
                    elementCounts[element] = elementCounts.GetValueOrDefault(element, 0) + 1;
                    if (atom.HydrogenCount > 0)
                    {
                        elementCounts["H"] = elementCounts.GetValueOrDefault("H", 0) + atom.HydrogenCount;
                    }
                }

                var result = new StringBuilder();
                var orderedElements = new[] { "C", "H", "N", "O", "F", "P", "S", "Cl", "Br", "I" };

                foreach (var element in orderedElements)
                {
                    if (elementCounts.TryGetValue(element, out var count))
                    {
                        result.Append(element);
                        if (count > 1) result.Append(count);
                        elementCounts.Remove(element);
                    }
                }

                foreach (var kvp in elementCounts.OrderBy(x => x.Key))
                {
                    result.Append(kvp.Key);
                    if (kvp.Value > 1) result.Append(kvp.Value);
                }

                return result.ToString();
            }
            catch
            {
                return "";
            }
        }

        private static int GetLonePairCount(string? element)
        {
            return (element ?? "") switch
            {
                "O" => 2,
                "N" => 1,
                "F" or "Cl" or "Br" or "I" => 3,
                "S" => 2,
                "P" => 1,
                _ => 0
            };
        }

        private static int GetElectronCount(string? element, int charge)
        {
            var baseElectrons = (element ?? "") switch
            {
                "H" => 1,
                "He" => 2,
                "Li" => 3,
                "Be" => 4,
                "B" => 5,
                "C" => 6,
                "N" => 7,
                "O" => 8,
                "F" => 9,
                "Ne" => 10,
                "Na" => 11,
                "Mg" => 12,
                "Al" => 13,
                "Si" => 14,
                "P" => 15,
                "S" => 16,
                "Cl" => 17,
                "Ar" => 18,
                "K" => 19,
                "Ca" => 20,
                "Fe" => 26,
                "Cu" => 29,
                "Zn" => 30,
                "Br" => 35,
                "I" => 53,
                _ => 6
            };
            return Math.Max(0, baseElectrons - charge);
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            try
            {
                disposer.DisposeAndClear();

                foreach (var brush in brushCache.Values)
                {
                    try { brush?.Dispose(); } catch { }
                }
                brushCache.Clear();

                foreach (var format in textFormatCache.Values)
                {
                    try { format?.Dispose(); } catch { }
                }
                textFormatCache.Clear();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error disposing ChemicalStructureSource: {ex.Message}");
            }
        }
    }
}