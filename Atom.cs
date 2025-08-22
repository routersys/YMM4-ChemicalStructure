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
    public enum AtomDisplayMode
    {
        [Display(Name = "通常")]
        Normal,
        [Display(Name = "記号のみ")]
        SymbolOnly,
        [Display(Name = "非表示")]
        Hidden,
        [Display(Name = "電子式")]
        Electron
    }

    public class Atom : Animatable
    {
        private static int _atomCounter = 0;

        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public int UserFriendlyId { get; private set; }

        [Display(GroupName = "原子", Name = "元素記号")]
        [TextEditor]
        [JsonProperty]
        public string Element { get => element; set => Set(ref element, value ?? "C"); }
        private string element = "C";

        [Display(GroupName = "原子", Name = "塗りつぶしの色")]
        [ColorPicker]
        [JsonProperty]
        public Color FillColor { get => fillColor; set => Set(ref fillColor, value); }
        private Color fillColor = Colors.White;

        [Display(GroupName = "原子", Name = "文字の色")]
        [ColorPicker]
        [JsonProperty]
        public Color TextColor { get => textColor; set => Set(ref textColor, value); }
        private Color textColor = Colors.Black;

        [Display(GroupName = "原子", Name = "半径")]
        [TextBoxSlider("F1", "px", 0, 200)]
        [DefaultValue(20d)]
        [JsonProperty]
        public double Radius
        {
            get => radius;
            set
            {
                var newValue = Math.Max(0.0, value);
                if (Set(ref radius, newValue))
                {
                    FontSize = Math.Max(8.0, newValue * 1.2);
                }
            }
        }
        private double radius = 20d;

        [Display(GroupName = "原子", Name = "表示モード")]
        [EnumComboBox]
        [JsonProperty]
        public AtomDisplayMode DisplayMode { get => displayMode; set => Set(ref displayMode, value); }
        private AtomDisplayMode displayMode = AtomDisplayMode.Normal;

        [Display(GroupName = "原子", Name = "フォントサイズ")]
        [TextBoxSlider("F0", "px", 8, 72)]
        [DefaultValue(24d)]
        [JsonProperty]
        public double FontSize { get => fontSize; set => Set(ref fontSize, Math.Max(8.0, value)); }
        private double fontSize = 24d;

        [Display(GroupName = "原子", Name = "電荷")]
        [TextBoxSlider("F0", "", -3, 3)]
        [DefaultValue(0)]
        [JsonProperty]
        public int Charge { get => charge; set => Set(ref charge, Math.Max(-3, Math.Min(3, value))); }
        private int charge = 0;

        [Display(GroupName = "原子", Name = "水素数")]
        [TextBoxSlider("F0", "", 0, 4)]
        [DefaultValue(0)]
        [JsonProperty]
        public int HydrogenCount { get => hydrogenCount; set => Set(ref hydrogenCount, Math.Max(0, Math.Min(4, value))); }
        private int hydrogenCount = 0;

        [Display(GroupName = "原子", Name = "X")]
        [AnimationSlider("F1", "px", -1000, 1000)]
        [JsonProperty]
        public Animation X { get; private set; } = new Animation(0, -10000, 10000);

        [Display(GroupName = "原子", Name = "Y")]
        [AnimationSlider("F1", "px", -1000, 1000)]
        [JsonProperty]
        public Animation Y { get; private set; } = new Animation(0, -10000, 10000);

        [Display(GroupName = "原子", Name = "Z")]
        [AnimationSlider("F1", "px", -1000, 1000)]
        [JsonProperty]
        public Animation Z { get; private set; } = new Animation(0, -1000, 1000);

        [JsonConstructor]
        public Atom(Guid id, int userFriendlyId, string element, Color fillColor, Color textColor,
                   double radius, AtomDisplayMode displayMode, double fontSize, int charge, int hydrogenCount,
                   Animation x, Animation y, Animation z)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            UserFriendlyId = userFriendlyId;
            this.element = element ?? "C";
            this.fillColor = fillColor;
            this.textColor = textColor;
            this.radius = radius;
            this.displayMode = displayMode;
            this.fontSize = fontSize;
            this.charge = charge;
            this.hydrogenCount = hydrogenCount;
            X = x ?? new Animation(0, -10000, 10000);
            Y = y ?? new Animation(0, -10000, 10000);
            Z = z ?? new Animation(0, -1000, 1000);

            if (userFriendlyId > _atomCounter)
            {
                _atomCounter = userFriendlyId;
            }
        }

        public Atom() : this("C", 0, 0) { }

        public Atom(string element, double x, double y)
        {
            Id = Guid.NewGuid();
            UserFriendlyId = ++_atomCounter;
            this.element = element ?? "C";
            X = new Animation(x, -10000, 10000);
            Y = new Animation(y, -10000, 10000);
            Z = new Animation(0, -1000, 1000);
        }

        public Atom(Atom other) : this(other?.Element ?? "C", other?.X.Values[0].Value ?? 0, other?.Y.Values[0].Value ?? 0)
        {
            if (other == null) return;

            FillColor = other.FillColor;
            TextColor = other.TextColor;
            Radius = other.Radius;
            DisplayMode = other.DisplayMode;
            FontSize = other.FontSize;
            Charge = other.Charge;
            HydrogenCount = other.HydrogenCount;

            if (other.X != null) X.CopyFrom(other.X);
            if (other.Y != null) Y.CopyFrom(other.Y);
            if (other.Z != null) Z.CopyFrom(other.Z);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => new[] { X, Y, Z };

        public override string ToString()
        {
            var chargeStr = charge != 0 ? $"{(charge > 0 ? "+" : "")}{charge}" : "";
            var hydrogenStr = hydrogenCount > 0 ? $"H{(hydrogenCount > 1 ? hydrogenCount.ToString() : "")}" : "";
            var idStr = _atomCounter > 1 ? $" ({UserFriendlyId:D3})" : "";
            return $"{Element}{hydrogenStr}{chargeStr}{idStr}";
        }

        public string GetDisplayName()
        {
            var elementColors = GetElementColors();
            var elementName = GetElementName(Element);
            var chargeStr = charge != 0 ? $"{(charge > 0 ? "+" : "")}{charge}" : "";
            var hydrogenStr = hydrogenCount > 0 ? $"H{(hydrogenCount > 1 ? hydrogenCount.ToString() : "")}" : "";
            var idStr = _atomCounter > 1 ? $"({UserFriendlyId:D3})" : "";
            return $"{elementName}({Element}){hydrogenStr}{chargeStr} {idStr}";
        }

        public static Dictionary<string, Color> GetElementColors()
        {
            return new Dictionary<string, Color>
            {
                ["H"] = Color.FromRgb(255, 255, 255),
                ["He"] = Color.FromRgb(217, 255, 255),
                ["Li"] = Color.FromRgb(204, 128, 255),
                ["Be"] = Color.FromRgb(194, 255, 0),
                ["B"] = Color.FromRgb(255, 181, 181),
                ["C"] = Color.FromRgb(144, 144, 144),
                ["N"] = Color.FromRgb(48, 80, 248),
                ["O"] = Color.FromRgb(255, 13, 13),
                ["F"] = Color.FromRgb(144, 224, 80),
                ["Ne"] = Color.FromRgb(179, 227, 245),
                ["Na"] = Color.FromRgb(171, 92, 242),
                ["Mg"] = Color.FromRgb(138, 255, 0),
                ["Al"] = Color.FromRgb(191, 166, 166),
                ["Si"] = Color.FromRgb(240, 200, 160),
                ["P"] = Color.FromRgb(255, 128, 0),
                ["S"] = Color.FromRgb(255, 255, 48),
                ["Cl"] = Color.FromRgb(31, 240, 31),
                ["Ar"] = Color.FromRgb(128, 209, 227),
                ["K"] = Color.FromRgb(143, 64, 212),
                ["Ca"] = Color.FromRgb(61, 255, 0),
                ["Sc"] = Color.FromRgb(230, 230, 230),
                ["Ti"] = Color.FromRgb(191, 194, 199),
                ["V"] = Color.FromRgb(166, 166, 171),
                ["Cr"] = Color.FromRgb(138, 153, 199),
                ["Mn"] = Color.FromRgb(156, 122, 199),
                ["Fe"] = Color.FromRgb(224, 102, 51),
                ["Co"] = Color.FromRgb(240, 144, 160),
                ["Ni"] = Color.FromRgb(80, 208, 80),
                ["Cu"] = Color.FromRgb(200, 128, 51),
                ["Zn"] = Color.FromRgb(125, 128, 176),
                ["Ga"] = Color.FromRgb(194, 143, 143),
                ["Ge"] = Color.FromRgb(102, 143, 143),
                ["As"] = Color.FromRgb(189, 128, 227),
                ["Se"] = Color.FromRgb(255, 161, 0),
                ["Br"] = Color.FromRgb(166, 41, 41),
                ["Kr"] = Color.FromRgb(92, 184, 209),
                ["Rb"] = Color.FromRgb(112, 46, 176),
                ["Sr"] = Color.FromRgb(0, 255, 0),
                ["Y"] = Color.FromRgb(148, 255, 255),
                ["Zr"] = Color.FromRgb(148, 224, 224),
                ["Nb"] = Color.FromRgb(115, 194, 201),
                ["Mo"] = Color.FromRgb(84, 181, 181),
                ["Tc"] = Color.FromRgb(59, 158, 158),
                ["Ru"] = Color.FromRgb(36, 143, 143),
                ["Rh"] = Color.FromRgb(10, 125, 140),
                ["Pd"] = Color.FromRgb(0, 105, 133),
                ["Ag"] = Color.FromRgb(192, 192, 192),
                ["Cd"] = Color.FromRgb(255, 217, 143),
                ["In"] = Color.FromRgb(166, 117, 115),
                ["Sn"] = Color.FromRgb(102, 128, 128),
                ["Sb"] = Color.FromRgb(158, 99, 181),
                ["Te"] = Color.FromRgb(212, 122, 0),
                ["I"] = Color.FromRgb(148, 0, 148),
                ["Xe"] = Color.FromRgb(66, 158, 176),
                ["Cs"] = Color.FromRgb(87, 23, 143),
                ["Ba"] = Color.FromRgb(0, 201, 0),
                ["La"] = Color.FromRgb(112, 212, 255),
                ["Ce"] = Color.FromRgb(255, 255, 199),
                ["Pr"] = Color.FromRgb(217, 255, 199),
                ["Nd"] = Color.FromRgb(199, 255, 199),
                ["Pm"] = Color.FromRgb(163, 255, 199),
                ["Sm"] = Color.FromRgb(143, 255, 199),
                ["Eu"] = Color.FromRgb(97, 255, 199),
                ["Gd"] = Color.FromRgb(69, 255, 199),
                ["Tb"] = Color.FromRgb(48, 255, 199),
                ["Dy"] = Color.FromRgb(31, 255, 199),
                ["Ho"] = Color.FromRgb(0, 255, 156),
                ["Er"] = Color.FromRgb(0, 230, 117),
                ["Tm"] = Color.FromRgb(0, 212, 82),
                ["Yb"] = Color.FromRgb(0, 191, 56),
                ["Lu"] = Color.FromRgb(0, 171, 36),
                ["Hf"] = Color.FromRgb(77, 194, 255),
                ["Ta"] = Color.FromRgb(77, 166, 255),
                ["W"] = Color.FromRgb(33, 148, 214),
                ["Re"] = Color.FromRgb(38, 125, 171),
                ["Os"] = Color.FromRgb(38, 102, 150),
                ["Ir"] = Color.FromRgb(23, 84, 135),
                ["Pt"] = Color.FromRgb(208, 208, 224),
                ["Au"] = Color.FromRgb(255, 209, 35),
                ["Hg"] = Color.FromRgb(184, 184, 208),
                ["Tl"] = Color.FromRgb(166, 84, 77),
                ["Pb"] = Color.FromRgb(87, 89, 97),
                ["Bi"] = Color.FromRgb(158, 79, 181),
                ["Po"] = Color.FromRgb(171, 92, 0),
                ["At"] = Color.FromRgb(117, 79, 69),
                ["Rn"] = Color.FromRgb(66, 130, 150),
                ["Fr"] = Color.FromRgb(66, 0, 102),
                ["Ra"] = Color.FromRgb(0, 125, 0),
                ["Ac"] = Color.FromRgb(112, 171, 250),
                ["Th"] = Color.FromRgb(0, 186, 255),
                ["Pa"] = Color.FromRgb(0, 161, 255),
                ["U"] = Color.FromRgb(0, 143, 255),
                ["Np"] = Color.FromRgb(0, 128, 255),
                ["Pu"] = Color.FromRgb(0, 107, 255),
                ["Am"] = Color.FromRgb(84, 92, 242),
                ["Cm"] = Color.FromRgb(120, 92, 227),
                ["Bk"] = Color.FromRgb(138, 79, 227),
                ["Cf"] = Color.FromRgb(161, 54, 212),
                ["Es"] = Color.FromRgb(179, 31, 212),
                ["Fm"] = Color.FromRgb(179, 31, 186),
                ["Md"] = Color.FromRgb(179, 13, 166),
                ["No"] = Color.FromRgb(189, 13, 135),
                ["Lr"] = Color.FromRgb(199, 0, 102),
                ["Rf"] = Color.FromRgb(204, 0, 89),
                ["Db"] = Color.FromRgb(209, 0, 79),
                ["Sg"] = Color.FromRgb(217, 0, 69),
                ["Bh"] = Color.FromRgb(224, 0, 56),
                ["Hs"] = Color.FromRgb(230, 0, 46),
                ["Mt"] = Color.FromRgb(235, 0, 38),
                ["Ds"] = Color.FromRgb(235, 0, 38),
                ["Rg"] = Color.FromRgb(235, 0, 38),
                ["Cn"] = Color.FromRgb(235, 0, 38),
                ["Nh"] = Color.FromRgb(235, 0, 38),
                ["Fl"] = Color.FromRgb(235, 0, 38),
                ["Mc"] = Color.FromRgb(235, 0, 38),
                ["Lv"] = Color.FromRgb(235, 0, 38),
                ["Ts"] = Color.FromRgb(235, 0, 38),
                ["Og"] = Color.FromRgb(235, 0, 38),
            };
        }

        public static Dictionary<string, double> GetAtomicWeights()
        {
            return new Dictionary<string, double>
            {
                ["H"] = 1.008,
                ["He"] = 4.003,
                ["Li"] = 6.941,
                ["Be"] = 9.012,
                ["B"] = 10.81,
                ["C"] = 12.011,
                ["N"] = 14.007,
                ["O"] = 15.999,
                ["F"] = 18.998,
                ["Ne"] = 20.180,
                ["Na"] = 22.990,
                ["Mg"] = 24.305,
                ["Al"] = 26.982,
                ["Si"] = 28.085,
                ["P"] = 30.974,
                ["S"] = 32.06,
                ["Cl"] = 35.45,
                ["Ar"] = 39.948,
                ["K"] = 39.098,
                ["Ca"] = 40.078,
                ["Sc"] = 44.956,
                ["Ti"] = 47.867,
                ["V"] = 50.942,
                ["Cr"] = 51.996,
                ["Mn"] = 54.938,
                ["Fe"] = 55.845,
                ["Co"] = 58.933,
                ["Ni"] = 58.693,
                ["Cu"] = 63.546,
                ["Zn"] = 65.38,
                ["Ga"] = 69.723,
                ["Ge"] = 72.64,
                ["As"] = 74.922,
                ["Se"] = 78.96,
                ["Br"] = 79.904,
                ["Kr"] = 83.798,
                ["Rb"] = 85.468,
                ["Sr"] = 87.62,
                ["Y"] = 88.906,
                ["Zr"] = 91.224,
                ["Nb"] = 92.906,
                ["Mo"] = 95.96,
                ["Tc"] = 98.0,
                ["Ru"] = 101.07,
                ["Rh"] = 102.906,
                ["Pd"] = 106.42,
                ["Ag"] = 107.868,
                ["Cd"] = 112.411,
                ["In"] = 114.818,
                ["Sn"] = 118.71,
                ["Sb"] = 121.76,
                ["Te"] = 127.6,
                ["I"] = 126.90,
                ["Xe"] = 131.293,
                ["Cs"] = 132.905,
                ["Ba"] = 137.327,
                ["La"] = 138.905,
                ["Ce"] = 140.116,
                ["Pr"] = 140.908,
                ["Nd"] = 144.242,
                ["Pm"] = 145.0,
                ["Sm"] = 150.36,
                ["Eu"] = 151.964,
                ["Gd"] = 157.25,
                ["Tb"] = 158.925,
                ["Dy"] = 162.5,
                ["Ho"] = 164.930,
                ["Er"] = 167.259,
                ["Tm"] = 168.934,
                ["Yb"] = 173.054,
                ["Lu"] = 174.967,
                ["Hf"] = 178.49,
                ["Ta"] = 180.948,
                ["W"] = 183.84,
                ["Re"] = 186.207,
                ["Os"] = 190.23,
                ["Ir"] = 192.217,
                ["Pt"] = 195.084,
                ["Au"] = 196.967,
                ["Hg"] = 200.592,
                ["Tl"] = 204.383,
                ["Pb"] = 207.2,
                ["Bi"] = 208.980,
                ["Po"] = 209.0,
                ["At"] = 210.0,
                ["Rn"] = 222.0,
                ["Fr"] = 223.0,
                ["Ra"] = 226.0,
                ["Ac"] = 227.0,
                ["Th"] = 232.038,
                ["Pa"] = 231.036,
                ["U"] = 238.029,
                ["Np"] = 237.0,
                ["Pu"] = 244.0,
                ["Am"] = 243.0,
                ["Cm"] = 247.0,
                ["Bk"] = 247.0,
                ["Cf"] = 251.0,
                ["Es"] = 252.0,
                ["Fm"] = 257.0,
                ["Md"] = 258.0,
                ["No"] = 259.0,
                ["Lr"] = 262.0,
                ["Rf"] = 267.0,
                ["Db"] = 268.0,
                ["Sg"] = 271.0,
                ["Bh"] = 272.0,
                ["Hs"] = 270.0,
                ["Mt"] = 276.0,
                ["Ds"] = 281.0,
                ["Rg"] = 280.0,
                ["Cn"] = 285.0,
                ["Nh"] = 284.0,
                ["Fl"] = 289.0,
                ["Mc"] = 288.0,
                ["Lv"] = 293.0,
                ["Ts"] = 294.0,
                ["Og"] = 294.0,
            };
        }

        public static string GetElementName(string symbol)
        {
            var elementNames = new Dictionary<string, string>
            {
                ["H"] = "水素",
                ["He"] = "ヘリウム",
                ["Li"] = "リチウム",
                ["Be"] = "ベリリウム",
                ["B"] = "ホウ素",
                ["C"] = "炭素",
                ["N"] = "窒素",
                ["O"] = "酸素",
                ["F"] = "フッ素",
                ["Ne"] = "ネオン",
                ["Na"] = "ナトリウム",
                ["Mg"] = "マグネシウム",
                ["Al"] = "アルミニウム",
                ["Si"] = "ケイ素",
                ["P"] = "リン",
                ["S"] = "硫黄",
                ["Cl"] = "塩素",
                ["Ar"] = "アルゴン",
                ["K"] = "カリウム",
                ["Ca"] = "カルシウム",
                ["Sc"] = "スカンジウム",
                ["Ti"] = "チタン",
                ["V"] = "バナジウム",
                ["Cr"] = "クロム",
                ["Mn"] = "マンガン",
                ["Fe"] = "鉄",
                ["Co"] = "コバルト",
                ["Ni"] = "ニッケル",
                ["Cu"] = "銅",
                ["Zn"] = "亜鉛",
                ["Ga"] = "ガリウム",
                ["Ge"] = "ゲルマニウム",
                ["As"] = "ヒ素",
                ["Se"] = "セレン",
                ["Br"] = "臭素",
                ["Kr"] = "クリプトン",
                ["Rb"] = "ルビジウム",
                ["Sr"] = "ストロンチウム",
                ["Y"] = "イットリウム",
                ["Zr"] = "ジルコニウム",
                ["Nb"] = "ニオブ",
                ["Mo"] = "モリブデン",
                ["Tc"] = "テクネチウム",
                ["Ru"] = "ルテニウム",
                ["Rh"] = "ロジウム",
                ["Pd"] = "パラジウム",
                ["Ag"] = "銀",
                ["Cd"] = "カドミウム",
                ["In"] = "インジウム",
                ["Sn"] = "スズ",
                ["Sb"] = "アンチモン",
                ["Te"] = "テルル",
                ["I"] = "ヨウ素",
                ["Xe"] = "キセノン",
                ["Cs"] = "セシウム",
                ["Ba"] = "バリウム",
                ["La"] = "ランタン",
                ["Ce"] = "セリウム",
                ["Pr"] = "プラセオジム",
                ["Nd"] = "ネオジム",
                ["Pm"] = "プロメチウム",
                ["Sm"] = "サマリウム",
                ["Eu"] = "ユウロピウム",
                ["Gd"] = "ガドリニウム",
                ["Tb"] = "テルビウム",
                ["Dy"] = "ジスプロシウム",
                ["Ho"] = "ホルミウム",
                ["Er"] = "エルビウム",
                ["Tm"] = "ツリウム",
                ["Yb"] = "イッテルビウム",
                ["Lu"] = "ルテチウム",
                ["Hf"] = "ハフニウム",
                ["Ta"] = "タンタル",
                ["W"] = "タングステン",
                ["Re"] = "レニウム",
                ["Os"] = "オスミウム",
                ["Ir"] = "イリジウム",
                ["Pt"] = "白金",
                ["Au"] = "金",
                ["Hg"] = "水銀",
                ["Tl"] = "タリウム",
                ["Pb"] = "鉛",
                ["Bi"] = "ビスマス",
                ["Po"] = "ポロニウム",
                ["At"] = "アスタチン",
                ["Rn"] = "ラドン",
                ["Fr"] = "フランシウム",
                ["Ra"] = "ラジウム",
                ["Ac"] = "アクチニウム",
                ["Th"] = "トリウム",
                ["Pa"] = "プロトアクチニウム",
                ["U"] = "ウラン",
                ["Np"] = "ネプツニウム",
                ["Pu"] = "プルトニウム",
                ["Am"] = "アメリシウム",
                ["Cm"] = "キュリウム",
                ["Bk"] = "バークリウム",
                ["Cf"] = "カリホルニウム",
                ["Es"] = "アインスタイニウム",
                ["Fm"] = "フェルミウム",
                ["Md"] = "メンデレビウム",
                ["No"] = "ノーベリウム",
                ["Lr"] = "ローレンシウム",
                ["Rf"] = "ラザホージウム",
                ["Db"] = "ドブニウム",
                ["Sg"] = "シーボーギウム",
                ["Bh"] = "ボーリウム",
                ["Hs"] = "ハッシウム",
                ["Mt"] = "マイトネリウム",
                ["Ds"] = "ダームスタチウム",
                ["Rg"] = "レントゲニウム",
                ["Cn"] = "コペルニシウム",
                ["Nh"] = "ニホニウム",
                ["Fl"] = "フレロビウム",
                ["Mc"] = "モスコビウム",
                ["Lv"] = "リバモリウム",
                ["Ts"] = "テネシン",
                ["Og"] = "オガネソン",
            };
            return elementNames.GetValueOrDefault(symbol, symbol);
        }

        public Color GetDefaultElementColor()
        {
            var colors = GetElementColors();
            return colors.TryGetValue(Element ?? "C", out var color) ? color : Colors.Gray;
        }

        public void ResetToElementDefaults()
        {
            FillColor = GetDefaultElementColor();
            TextColor = Colors.Black;
            var element = Element ?? "C";
            if (element == "H")
            {
                Radius = 15;
            }
            else if (element == "C" || element == "N" || element == "O")
            {
                Radius = 20;
            }
            else
            {
                Radius = 25;
            }
        }

        public static void ResetAtomCounter()
        {
            _atomCounter = 0;
        }

        public static void SetAtomCounter(int value)
        {
            _atomCounter = Math.Max(0, value);
        }

        public static int GetAtomCounter()
        {
            return _atomCounter;
        }
    }
}