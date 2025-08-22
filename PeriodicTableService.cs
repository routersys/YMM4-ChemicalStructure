using System.Collections.Generic;
using System.Linq;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public class ElementInfo
    {
        public int AtomicNumber { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Period { get; set; }
        public int Group { get; set; }
        public int DisplayRow { get; set; }
        public int DisplayColumn { get; set; }
    }

    public static class PeriodicTableService
    {
        private static readonly List<ElementInfo> _elements;

        static PeriodicTableService()
        {
            _elements = CreatePeriodicTable();
        }

        public static List<ElementInfo> GetAllElements() => _elements;

        private static List<ElementInfo> CreatePeriodicTable()
        {
            var elements = new List<ElementInfo>
            {
                new ElementInfo { AtomicNumber = 1, Symbol = "H", Name = "水素", Category = "非金属（二原子分子）", Period = 1, Group = 1 },
                new ElementInfo { AtomicNumber = 2, Symbol = "He", Name = "ヘリウム", Category = "貴ガス", Period = 1, Group = 18 },
                new ElementInfo { AtomicNumber = 3, Symbol = "Li", Name = "リチウム", Category = "アルカリ金属", Period = 2, Group = 1 },
                new ElementInfo { AtomicNumber = 4, Symbol = "Be", Name = "ベリリウム", Category = "アルカリ土類金属", Period = 2, Group = 2 },
                new ElementInfo { AtomicNumber = 5, Symbol = "B", Name = "ホウ素", Category = "半金属", Period = 2, Group = 13 },
                new ElementInfo { AtomicNumber = 6, Symbol = "C", Name = "炭素", Category = "非金属（多原子分子）", Period = 2, Group = 14 },
                new ElementInfo { AtomicNumber = 7, Symbol = "N", Name = "窒素", Category = "非金属（二原子分子）", Period = 2, Group = 15 },
                new ElementInfo { AtomicNumber = 8, Symbol = "O", Name = "酸素", Category = "非金属（二原子分子）", Period = 2, Group = 16 },
                new ElementInfo { AtomicNumber = 9, Symbol = "F", Name = "フッ素", Category = "非金属（二原子分子）", Period = 2, Group = 17 },
                new ElementInfo { AtomicNumber = 10, Symbol = "Ne", Name = "ネオン", Category = "貴ガス", Period = 2, Group = 18 },
                new ElementInfo { AtomicNumber = 11, Symbol = "Na", Name = "ナトリウム", Category = "アルカリ金属", Period = 3, Group = 1 },
                new ElementInfo { AtomicNumber = 12, Symbol = "Mg", Name = "マグネシウム", Category = "アルカリ土類金属", Period = 3, Group = 2 },
                new ElementInfo { AtomicNumber = 13, Symbol = "Al", Name = "アルミニウム", Category = "ポスト遷移金属", Period = 3, Group = 13 },
                new ElementInfo { AtomicNumber = 14, Symbol = "Si", Name = "ケイ素", Category = "半金属", Period = 3, Group = 14 },
                new ElementInfo { AtomicNumber = 15, Symbol = "P", Name = "リン", Category = "非金属（多原子分子）", Period = 3, Group = 15 },
                new ElementInfo { AtomicNumber = 16, Symbol = "S", Name = "硫黄", Category = "非金属（多原子分子）", Period = 3, Group = 16 },
                new ElementInfo { AtomicNumber = 17, Symbol = "Cl", Name = "塩素", Category = "非金属（二原子分子）", Period = 3, Group = 17 },
                new ElementInfo { AtomicNumber = 18, Symbol = "Ar", Name = "アルゴン", Category = "貴ガス", Period = 3, Group = 18 },
                new ElementInfo { AtomicNumber = 19, Symbol = "K", Name = "カリウム", Category = "アルカリ金属", Period = 4, Group = 1 },
                new ElementInfo { AtomicNumber = 20, Symbol = "Ca", Name = "カルシウム", Category = "アルカリ土類金属", Period = 4, Group = 2 },
                new ElementInfo { AtomicNumber = 21, Symbol = "Sc", Name = "スカンジウム", Category = "遷移金属", Period = 4, Group = 3 },
                new ElementInfo { AtomicNumber = 22, Symbol = "Ti", Name = "チタン", Category = "遷移金属", Period = 4, Group = 4 },
                new ElementInfo { AtomicNumber = 23, Symbol = "V", Name = "バナジウム", Category = "遷移金属", Period = 4, Group = 5 },
                new ElementInfo { AtomicNumber = 24, Symbol = "Cr", Name = "クロム", Category = "遷移金属", Period = 4, Group = 6 },
                new ElementInfo { AtomicNumber = 25, Symbol = "Mn", Name = "マンガン", Category = "遷移金属", Period = 4, Group = 7 },
                new ElementInfo { AtomicNumber = 26, Symbol = "Fe", Name = "鉄", Category = "遷移金属", Period = 4, Group = 8 },
                new ElementInfo { AtomicNumber = 27, Symbol = "Co", Name = "コバルト", Category = "遷移金属", Period = 4, Group = 9 },
                new ElementInfo { AtomicNumber = 28, Symbol = "Ni", Name = "ニッケル", Category = "遷移金属", Period = 4, Group = 10 },
                new ElementInfo { AtomicNumber = 29, Symbol = "Cu", Name = "銅", Category = "遷移金属", Period = 4, Group = 11 },
                new ElementInfo { AtomicNumber = 30, Symbol = "Zn", Name = "亜鉛", Category = "遷移金属", Period = 4, Group = 12 },
                new ElementInfo { AtomicNumber = 31, Symbol = "Ga", Name = "ガリウム", Category = "ポスト遷移金属", Period = 4, Group = 13 },
                new ElementInfo { AtomicNumber = 32, Symbol = "Ge", Name = "ゲルマニウム", Category = "半金属", Period = 4, Group = 14 },
                new ElementInfo { AtomicNumber = 33, Symbol = "As", Name = "ヒ素", Category = "半金属", Period = 4, Group = 15 },
                new ElementInfo { AtomicNumber = 34, Symbol = "Se", Name = "セレン", Category = "非金属（多原子分子）", Period = 4, Group = 16 },
                new ElementInfo { AtomicNumber = 35, Symbol = "Br", Name = "臭素", Category = "非金属（二原子分子）", Period = 4, Group = 17 },
                new ElementInfo { AtomicNumber = 36, Symbol = "Kr", Name = "クリプトン", Category = "貴ガス", Period = 4, Group = 18 },
                new ElementInfo { AtomicNumber = 37, Symbol = "Rb", Name = "ルビジウム", Category = "アルカリ金属", Period = 5, Group = 1 },
                new ElementInfo { AtomicNumber = 38, Symbol = "Sr", Name = "ストロンチウム", Category = "アルカリ土類金属", Period = 5, Group = 2 },
                new ElementInfo { AtomicNumber = 39, Symbol = "Y", Name = "イットリウム", Category = "遷移金属", Period = 5, Group = 3 },
                new ElementInfo { AtomicNumber = 40, Symbol = "Zr", Name = "ジルコニウム", Category = "遷移金属", Period = 5, Group = 4 },
                new ElementInfo { AtomicNumber = 41, Symbol = "Nb", Name = "ニオブ", Category = "遷移金属", Period = 5, Group = 5 },
                new ElementInfo { AtomicNumber = 42, Symbol = "Mo", Name = "モリブデン", Category = "遷移金属", Period = 5, Group = 6 },
                new ElementInfo { AtomicNumber = 43, Symbol = "Tc", Name = "テクネチウム", Category = "遷移金属", Period = 5, Group = 7 },
                new ElementInfo { AtomicNumber = 44, Symbol = "Ru", Name = "ルテニウム", Category = "遷移金属", Period = 5, Group = 8 },
                new ElementInfo { AtomicNumber = 45, Symbol = "Rh", Name = "ロジウム", Category = "遷移金属", Period = 5, Group = 9 },
                new ElementInfo { AtomicNumber = 46, Symbol = "Pd", Name = "パラジウム", Category = "遷移金属", Period = 5, Group = 10 },
                new ElementInfo { AtomicNumber = 47, Symbol = "Ag", Name = "銀", Category = "遷移金属", Period = 5, Group = 11 },
                new ElementInfo { AtomicNumber = 48, Symbol = "Cd", Name = "カドミウム", Category = "遷移金属", Period = 5, Group = 12 },
                new ElementInfo { AtomicNumber = 49, Symbol = "In", Name = "インジウム", Category = "ポスト遷移金属", Period = 5, Group = 13 },
                new ElementInfo { AtomicNumber = 50, Symbol = "Sn", Name = "スズ", Category = "ポスト遷移金属", Period = 5, Group = 14 },
                new ElementInfo { AtomicNumber = 51, Symbol = "Sb", Name = "アンチモン", Category = "半金属", Period = 5, Group = 15 },
                new ElementInfo { AtomicNumber = 52, Symbol = "Te", Name = "テルル", Category = "半金属", Period = 5, Group = 16 },
                new ElementInfo { AtomicNumber = 53, Symbol = "I", Name = "ヨウ素", Category = "非金属（二原子分子）", Period = 5, Group = 17 },
                new ElementInfo { AtomicNumber = 54, Symbol = "Xe", Name = "キセノン", Category = "貴ガス", Period = 5, Group = 18 },
                new ElementInfo { AtomicNumber = 55, Symbol = "Cs", Name = "セシウム", Category = "アルカリ金属", Period = 6, Group = 1 },
                new ElementInfo { AtomicNumber = 56, Symbol = "Ba", Name = "バリウム", Category = "アルカリ土類金属", Period = 6, Group = 2 },
                new ElementInfo { AtomicNumber = 57, Symbol = "La", Name = "ランタン", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 58, Symbol = "Ce", Name = "セリウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 59, Symbol = "Pr", Name = "プラセオジム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 60, Symbol = "Nd", Name = "ネオジム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 61, Symbol = "Pm", Name = "プロメチウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 62, Symbol = "Sm", Name = "サマリウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 63, Symbol = "Eu", Name = "ユウロピウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 64, Symbol = "Gd", Name = "ガドリニウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 65, Symbol = "Tb", Name = "テルビウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 66, Symbol = "Dy", Name = "ジスプロシウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 67, Symbol = "Ho", Name = "ホルミウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 68, Symbol = "Er", Name = "エルビウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 69, Symbol = "Tm", Name = "ツリウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 70, Symbol = "Yb", Name = "イッテルビウム", Category = "ランタノイド", Period = 6, Group = 3 },
                new ElementInfo { AtomicNumber = 71, Symbol = "Lu", Name = "ルテチウム", Category = "ランタノイド", Period = 6, Group = 4 },
                new ElementInfo { AtomicNumber = 72, Symbol = "Hf", Name = "ハフニウム", Category = "遷移金属", Period = 6, Group = 4 },
                new ElementInfo { AtomicNumber = 73, Symbol = "Ta", Name = "タンタル", Category = "遷移金属", Period = 6, Group = 5 },
                new ElementInfo { AtomicNumber = 74, Symbol = "W", Name = "タングステン", Category = "遷移金属", Period = 6, Group = 6 },
                new ElementInfo { AtomicNumber = 75, Symbol = "Re", Name = "レニウム", Category = "遷移金属", Period = 6, Group = 7 },
                new ElementInfo { AtomicNumber = 76, Symbol = "Os", Name = "オスミウム", Category = "遷移金属", Period = 6, Group = 8 },
                new ElementInfo { AtomicNumber = 77, Symbol = "Ir", Name = "イリジウム", Category = "遷移金属", Period = 6, Group = 9 },
                new ElementInfo { AtomicNumber = 78, Symbol = "Pt", Name = "白金", Category = "遷移金属", Period = 6, Group = 10 },
                new ElementInfo { AtomicNumber = 79, Symbol = "Au", Name = "金", Category = "遷移金属", Period = 6, Group = 11 },
                new ElementInfo { AtomicNumber = 80, Symbol = "Hg", Name = "水銀", Category = "遷移金属", Period = 6, Group = 12 },
                new ElementInfo { AtomicNumber = 81, Symbol = "Tl", Name = "タリウム", Category = "ポスト遷移金属", Period = 6, Group = 13 },
                new ElementInfo { AtomicNumber = 82, Symbol = "Pb", Name = "鉛", Category = "ポスト遷移金属", Period = 6, Group = 14 },
                new ElementInfo { AtomicNumber = 83, Symbol = "Bi", Name = "ビスマス", Category = "ポスト遷移金属", Period = 6, Group = 15 },
                new ElementInfo { AtomicNumber = 84, Symbol = "Po", Name = "ポロニウム", Category = "半金属", Period = 6, Group = 16 },
                new ElementInfo { AtomicNumber = 85, Symbol = "At", Name = "アスタチン", Category = "半金属", Period = 6, Group = 17 },
                new ElementInfo { AtomicNumber = 86, Symbol = "Rn", Name = "ラドン", Category = "貴ガス", Period = 6, Group = 18 },
                new ElementInfo { AtomicNumber = 87, Symbol = "Fr", Name = "フランシウム", Category = "アルカリ金属", Period = 7, Group = 1 },
                new ElementInfo { AtomicNumber = 88, Symbol = "Ra", Name = "ラジウム", Category = "アルカリ土類金属", Period = 7, Group = 2 },
                new ElementInfo { AtomicNumber = 89, Symbol = "Ac", Name = "アクチニウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 90, Symbol = "Th", Name = "トリウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 91, Symbol = "Pa", Name = "プロトアクチニウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 92, Symbol = "U", Name = "ウラン", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 93, Symbol = "Np", Name = "ネプツニウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 94, Symbol = "Pu", Name = "プルトニウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 95, Symbol = "Am", Name = "アメリシウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 96, Symbol = "Cm", Name = "キュリウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 97, Symbol = "Bk", Name = "バークリウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 98, Symbol = "Cf", Name = "カリホルニウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 99, Symbol = "Es", Name = "アインスタイニウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 100, Symbol = "Fm", Name = "フェルミウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 101, Symbol = "Md", Name = "メンデレビウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 102, Symbol = "No", Name = "ノーベリウム", Category = "アクチノイド", Period = 7, Group = 3 },
                new ElementInfo { AtomicNumber = 103, Symbol = "Lr", Name = "ローレンシウム", Category = "アクチノイド", Period = 7, Group = 4 },
                new ElementInfo { AtomicNumber = 104, Symbol = "Rf", Name = "ラザホージウム", Category = "遷移金属", Period = 7, Group = 4 },
                new ElementInfo { AtomicNumber = 105, Symbol = "Db", Name = "ドブニウム", Category = "遷移金属", Period = 7, Group = 5 },
                new ElementInfo { AtomicNumber = 106, Symbol = "Sg", Name = "シーボーギウム", Category = "遷移金属", Period = 7, Group = 6 },
                new ElementInfo { AtomicNumber = 107, Symbol = "Bh", Name = "ボーリウム", Category = "遷移金属", Period = 7, Group = 7 },
                new ElementInfo { AtomicNumber = 108, Symbol = "Hs", Name = "ハッシウム", Category = "遷移金属", Period = 7, Group = 8 },
                new ElementInfo { AtomicNumber = 109, Symbol = "Mt", Name = "マイトネリウム", Category = "遷移金属", Period = 7, Group = 9 },
                new ElementInfo { AtomicNumber = 110, Symbol = "Ds", Name = "ダームスタチウム", Category = "遷移金属", Period = 7, Group = 10 },
                new ElementInfo { AtomicNumber = 111, Symbol = "Rg", Name = "レントゲニウム", Category = "遷移金属", Period = 7, Group = 11 },
                new ElementInfo { AtomicNumber = 112, Symbol = "Cn", Name = "コペルニシウム", Category = "遷移金属", Period = 7, Group = 12 },
                new ElementInfo { AtomicNumber = 113, Symbol = "Nh", Name = "ニホニウム", Category = "ポスト遷移金属", Period = 7, Group = 13 },
                new ElementInfo { AtomicNumber = 114, Symbol = "Fl", Name = "フレロビウム", Category = "ポスト遷移金属", Period = 7, Group = 14 },
                new ElementInfo { AtomicNumber = 115, Symbol = "Mc", Name = "モスコビウム", Category = "ポスト遷移金属", Period = 7, Group = 15 },
                new ElementInfo { AtomicNumber = 116, Symbol = "Lv", Name = "リバモリウム", Category = "ポスト遷移金属", Period = 7, Group = 16 },
                new ElementInfo { AtomicNumber = 117, Symbol = "Ts", Name = "テネシン", Category = "半金属", Period = 7, Group = 17 },
                new ElementInfo { AtomicNumber = 118, Symbol = "Og", Name = "オガネソン", Category = "貴ガス", Period = 7, Group = 18 },
            };

            foreach (var element in elements)
            {
                if (element.AtomicNumber >= 57 && element.AtomicNumber <= 71)
                {
                    element.DisplayRow = 8;
                    element.DisplayColumn = element.AtomicNumber - 57 + 3;
                }
                else if (element.AtomicNumber >= 89 && element.AtomicNumber <= 103)
                {
                    element.DisplayRow = 9;
                    element.DisplayColumn = element.AtomicNumber - 89 + 3;
                }
                else
                {
                    element.DisplayRow = element.Period - 1;
                    element.DisplayColumn = element.Group - 1;
                    if (element.AtomicNumber == 71) element.Group = 3;
                    if (element.AtomicNumber == 103) element.Group = 3;
                }
            }
            elements.First(e => e.AtomicNumber == 71).DisplayColumn = 3;
            elements.First(e => e.AtomicNumber == 103).DisplayColumn = 3;

            return elements;
        }
    }
}