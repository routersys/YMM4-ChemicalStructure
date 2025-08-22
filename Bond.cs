using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public enum BondType
    {
        [Display(Name = "単結合")]
        Single,
        [Display(Name = "二重結合")]
        Double,
        [Display(Name = "三重結合")]
        Triple,
        [Display(Name = "くさび形 (手前)")]
        Wedge,
        [Display(Name = "破線 (奥)")]
        Dash,
        [Display(Name = "波線 (立体不明)")]
        Wavy,
        [Display(Name = "部分結合")]
        Partial,
        [Display(Name = "配位結合")]
        Coordinate,
        [Display(Name = "芳香族")]
        Aromatic,
        [Display(Name = "水素結合")]
        Hydrogen,
        [Display(Name = "イオン結合")]
        Ionic,
        [Display(Name = "非表示")]
        Hidden
    }

    public enum BondEndStyle
    {
        [Display(Name = "通常")]
        Normal,
        [Display(Name = "矢印")]
        Arrow,
        [Display(Name = "円形")]
        Circle,
        [Display(Name = "正方形")]
        Square
    }

    public class Bond : Animatable
    {
        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public Guid Atom1Id { get; private set; }

        [JsonProperty]
        public Guid Atom2Id { get; private set; }

        [Display(GroupName = "結合", Name = "結合の種類")]
        [EnumComboBox]
        [JsonProperty]
        public BondType Type
        {
            get => type;
            set
            {
                if (Set(ref type, value))
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private BondType type = BondType.Single;

        [Display(GroupName = "結合", Name = "結合の色")]
        [ColorPicker]
        [JsonProperty]
        public Color Color
        {
            get => color;
            set
            {
                if (Set(ref color, value))
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private Color color = Colors.Black;

        [Display(GroupName = "結合", Name = "線の太さ")]
        [TextBoxSlider("F1", "px", 0.5, 10)]
        [DefaultValue(2.0)]
        [JsonProperty]
        public double Thickness
        {
            get => thickness;
            set
            {
                if (Set(ref thickness, Math.Max(0.1, value)))
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private double thickness = 2.0;

        [Display(GroupName = "結合", Name = "透明度")]
        [TextBoxSlider("F2", "", 0.0, 1.0)]
        [DefaultValue(1.0)]
        [JsonProperty]
        public double Opacity
        {
            get => opacity;
            set
            {
                if (Set(ref opacity, Math.Max(0.0, Math.Min(1.0, value))))
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private double opacity = 1.0;

        [Display(GroupName = "結合", Name = "端のスタイル")]
        [EnumComboBox]
        [JsonProperty]
        public BondEndStyle EndStyle
        {
            get => endStyle;
            set
            {
                if (Set(ref endStyle, value))
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private BondEndStyle endStyle = BondEndStyle.Normal;

        [Display(GroupName = "結合", Name = "結合次数")]
        [TextBoxSlider("F1", "", 0.5, 3.0)]
        [DefaultValue(1.0)]
        [JsonProperty]
        public double Order
        {
            get => order;
            set
            {
                if (Set(ref order, Math.Max(0.1, Math.Min(3.0, value))))
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private double order = 1.0;

        [Display(GroupName = "結合", Name = "長さ調整")]
        [AnimationSlider("F2", "", 0.1, 2.0)]
        [JsonProperty]
        public Animation LengthMultiplier { get; private set; } = new Animation(1.0, 0.1, 5.0);

        [Display(GroupName = "結合", Name = "オフセット")]
        [AnimationSlider("F1", "px", -50, 50)]
        [JsonProperty]
        public Animation Offset { get; private set; } = new Animation(0, -200, 200);

        public event EventHandler? BondChanged;

        [JsonConstructor]
        public Bond(Guid id, Guid atom1Id, Guid atom2Id, BondType type, Color color, double thickness,
                   double opacity, BondEndStyle endStyle, double order, Animation lengthMultiplier, Animation offset)
        {
            if (atom1Id == Guid.Empty || atom2Id == Guid.Empty)
                throw new ArgumentException("Atom IDs cannot be empty");

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Atom1Id = atom1Id;
            Atom2Id = atom2Id;
            this.type = type;
            this.color = color;
            this.thickness = thickness;
            this.opacity = opacity;
            this.endStyle = endStyle;
            this.order = order;
            LengthMultiplier = lengthMultiplier ?? new Animation(1.0, 0.1, 5.0);
            Offset = offset ?? new Animation(0, -200, 200);
        }

        public Bond(Guid atom1Id, Guid atom2Id) : this(Guid.NewGuid(), atom1Id, atom2Id, BondType.Single, Colors.Black, 2.0, 1.0, BondEndStyle.Normal, 1.0, new Animation(1.0, 0.1, 5.0), new Animation(0, -200, 200))
        {
        }

        public Bond(Bond other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            Id = Guid.NewGuid();
            Atom1Id = other.Atom1Id;
            Atom2Id = other.Atom2Id;
            Type = other.Type;
            Color = other.Color;
            Thickness = other.Thickness;
            Opacity = other.Opacity;
            EndStyle = other.EndStyle;
            Order = other.Order;

            LengthMultiplier = new Animation(1.0, 0.1, 5.0);
            Offset = new Animation(0, -200, 200);

            if (other.LengthMultiplier != null) LengthMultiplier.CopyFrom(other.LengthMultiplier);
            if (other.Offset != null) Offset.CopyFrom(other.Offset);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => new[] { LengthMultiplier, Offset };

        private void NotifyPropertyChanged()
        {
            try
            {
                BondChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error notifying bond change: {ex.Message}");
            }
        }

        public override string ToString()
        {
            var orderStr = Order != 1.0 ? $" ({Order:F1})" : "";
            return $"{GetJapaneseBondType()}{orderStr}: {Atom1Id.ToString("N")[..4]} - {Atom2Id.ToString("N")[..4]}";
        }

        public string GetDisplayName(Atom atom1, Atom atom2)
        {
            var atom1Name = atom1?.GetDisplayName() ?? "不明";
            var atom2Name = atom2?.GetDisplayName() ?? "不明";
            var bondTypeName = GetJapaneseBondType();
            return $"{atom1Name} - {atom2Name} [{bondTypeName}]";
        }

        private string GetJapaneseBondType()
        {
            return Type switch
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
                _ => "不明"
            };
        }

        public static Dictionary<BondType, double> GetDefaultThickness()
        {
            return new Dictionary<BondType, double>
            {
                [BondType.Single] = 2.0,
                [BondType.Double] = 1.8,
                [BondType.Triple] = 1.6,
                [BondType.Wedge] = 3.0,
                [BondType.Dash] = 2.0,
                [BondType.Wavy] = 2.0,
                [BondType.Partial] = 1.5,
                [BondType.Coordinate] = 2.5,
                [BondType.Aromatic] = 1.5,
                [BondType.Hydrogen] = 1.0,
                [BondType.Ionic] = 3.0,
                [BondType.Hidden] = 0.0
            };
        }

        public double GetInstanceDefaultThickness()
        {
            var thicknesses = GetDefaultThickness();
            return thicknesses.TryGetValue(Type, out var t) ? t : 2.0;
        }

        public Color GetEffectiveColor()
        {
            if (Opacity < 1.0)
            {
                var c = Color;
                return Color.FromArgb((byte)(c.A * Opacity), c.R, c.G, c.B);
            }
            return Color;
        }
    }
}