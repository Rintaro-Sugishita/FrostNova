using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    [Flags]
    public enum AllowedCharTypes
    {
        None = 0,


        Digit = 1 << 0, // 0-9

        LowerAlpha = 1 << 1, // a-z

        UpperAlpha = 1 << 2, // A-Z

        HalfWidthSymbol = 1 << 3, // ASCII 記号

        FullWidth = 1 << 4, // 全角（広義）

        HalfKana = 1 << 5, // 半角カタカナ
        Hiragana = 1 << 6, // ひらがな
        Katakana = 1 << 7, // 全角カタカナ

        Alpha = LowerAlpha | UpperAlpha,
        AlphaNumeric = Digit | LowerAlpha | UpperAlpha,
        AllHalfWidth = Digit | LowerAlpha | UpperAlpha | HalfWidthSymbol | HalfKana,
        Japanese = Hiragana | Katakana,
        All = ~None
    }
}
