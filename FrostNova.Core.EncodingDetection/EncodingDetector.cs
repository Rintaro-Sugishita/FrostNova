using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.EncodingDetection
{
    public class EncodingDetector
    {
        private List<Encoding> _encodingList;

        private string _fallbackString = "�[Fallback]";

        private class CHARSET_NAME
        {
            public const string SJIS_NAME = "shift-jis";
            public const string JIS_NAME = "iso-2022-jp";

            public const string EUC_NAME = "EUC-JP";
            public const string UTF8 = "UTF8";
        }

        private enum CodePage
        {
            SJIS = 932,
            UTF16 = 1200,
            UTF16BE = 1201,
            UTF32 = 12000,
            UTF32BE = 12001,
            UTF8 = 65001,
            UTF7 = 65000,
            ASCII = 20127,

            JIS = 50220,
            EUC = 20932,
        }

        private class ENCODING_NAME
        {
            public const string SJIS = "[SJIS]";
            public const string JIS = "[JIS]";
            public const string UTF8 = "[UTF8]";
            public const string UTF8N = "[UTF8N]";
            public const string UTF16 = "[Unicode]";
            public const string UTF16BE = "[UnicodeBE]";
            public const string UTF32 = "[UTF32]";
            public const string UTF32BE = "[UTF32BE]";
            public const string ASCII = "[ASCII]";
            public const string EUC = "[EUC-JP]";
            public const string UNKOWN = "[UNKOWN]";
        }

        /// <summary>
        /// コンストラクタ 新しい EncodingDetector クラスを生成します。
        /// </summary>
        public EncodingDetector()
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //BOM付きファイルは扱わない
            _encodingList = new List<Encoding> {
                (Encoding)Encoding.ASCII.Clone(),
                (Encoding)new UTF8Encoding(false).Clone(),
                (Encoding)Encoding.GetEncoding(932).Clone(),
                (Encoding)Encoding.GetEncoding(CHARSET_NAME.EUC_NAME).Clone(),
                (Encoding)Encoding.GetEncoding(CHARSET_NAME.JIS_NAME).Clone(),
            };

            //全てにFallbackを設定する
            foreach (Encoding ee in _encodingList)
            {
                ee.DecoderFallback = new DecoderReplacementFallback(_fallbackString);
            }
        }

        public static List<EncodingData> GetEncoding()
        {
            List<EncodingData> dict = new List<EncodingData>();
            dict.Add(new EncodingData((int)CodePage.SJIS, CHARSET_NAME.SJIS_NAME));
            dict.Add(new EncodingData((int)CodePage.UTF8, CHARSET_NAME.UTF8));

            return dict;
        }

        /// <summary>
        /// 文字コードクラスから略称を返します。
        /// </summary>
        public string EncodingToString(Encoding encoding, bool withBOM)
        {
            if (encoding == null) { return ""; }
            return CodePageToName(encoding.CodePage, withBOM);
        }

        /// <summary>
        /// コードから、文字コード略称を返します。
        /// </summary>
        public string CodePageToName(int code, bool withBOM)
        {
            if (code == Encoding.UTF8.CodePage)
            {
                if (withBOM)
                {
                    return ENCODING_NAME.UTF8;
                }
                else { return ENCODING_NAME.UTF8N; }
            }
            else if (code == Encoding.Unicode.CodePage)
            {
                return ENCODING_NAME.UTF16;
            }
            else if (code == Encoding.GetEncoding((int)CodePage.UTF16BE).CodePage)
            {
                return ENCODING_NAME.UTF16BE;
            }
            else if (code == Encoding.GetEncoding(CHARSET_NAME.SJIS_NAME).CodePage)
            {
                return ENCODING_NAME.SJIS;
            }
            else if (code == Encoding.UTF32.CodePage)
            {
                return ENCODING_NAME.UTF32;
            }
            else if (code == Encoding.GetEncoding((int)CodePage.UTF32BE).CodePage)
            {
                return ENCODING_NAME.UTF32BE;
            }
            else if (code == Encoding.GetEncoding(CHARSET_NAME.EUC_NAME).CodePage)
            {
                return ENCODING_NAME.EUC;
            }
            else if (code == Encoding.ASCII.CodePage)
            {
                return ENCODING_NAME.ASCII;
            }
            else if (code == Encoding.GetEncoding(CHARSET_NAME.JIS_NAME).CodePage)
            {
                return ENCODING_NAME.JIS;
            }

            return ENCODING_NAME.UNKOWN;
        }

        /// <summary>
        /// 略称から Encoding を取得します（判定外は null）。
        /// </summary>
        public Encoding NameToEncoding(string encodingName)
        {
            switch (encodingName)
            {
                case ENCODING_NAME.UTF8:
                    return Encoding.UTF8;

                case ENCODING_NAME.UTF8N:
                    return new UTF8Encoding(false);

                case ENCODING_NAME.UTF16:
                    return Encoding.Unicode;

                case ENCODING_NAME.UTF16BE:
                    return Encoding.GetEncoding((int)CodePage.UTF16BE);

                case ENCODING_NAME.SJIS:
                    return Encoding.GetEncoding(CHARSET_NAME.SJIS_NAME);

                case ENCODING_NAME.UTF32:
                    return Encoding.UTF32;

                case ENCODING_NAME.UTF32BE:
                    return Encoding.GetEncoding((int)CodePage.UTF16BE);

                case ENCODING_NAME.EUC:
                    return Encoding.GetEncoding(CHARSET_NAME.EUC_NAME);

                case ENCODING_NAME.ASCII:
                    return Encoding.ASCII;

                case ENCODING_NAME.JIS:
                    return Encoding.GetEncoding(CHARSET_NAME.JIS_NAME);

                default:
                    return null;
            }
        }

        /// <summary>
        /// ファイルの文字コード略称を取得します（半角英数のみの場合 UTF8N と認識）。
        /// </summary>
        public string GetFileEncodingText(string fileName)
        {
            return EncodingToString(GetFileEncoding(fileName), false);
        }

        public Encoding GetFileEncoding(string fileName)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(fileName);

            //BOM判別
            Encoding encoding = GetFileEncodingFromBOM(bytes);
            if (encoding == null)
            {
                //BOM無しの場合
                encoding = GetEncodingFromLength(bytes);
            }

            return encoding;
        }

        /// <summary>
        /// BOM付きファイルであれば、文字コードを判定します。
        /// </summary>
        private System.Text.Encoding GetFileEncodingFromBOM(byte[] bytes)
        {
            if (bytes.Length < 2)
            {
                return null;
            }
            if ((bytes[0] == 0xfe) && (bytes[1] == 0xff))
            {
                //UTF-16 BE
                return new UnicodeEncoding(true, true);
            }
            if ((bytes[0] == 0xff) && (bytes[1] == 0xfe))
            {
                if ((4 <= bytes.Length) &&
                    (bytes[2] == 0x00) && (bytes[3] == 0x00))
                {
                    //UTF-32 LE
                    return new UTF32Encoding(false, true);
                }
                //UTF-16 LE
                return new UnicodeEncoding(false, true);
            }
            if (bytes.Length < 3)
            {
                return null;
            }
            if ((bytes[0] == 0xef) && (bytes[1] == 0xbb) && (bytes[2] == 0xbf))
            {
                //UTF-8
                return new UTF8Encoding(true, true);
            }
            if (bytes.Length < 4)
            {
                return null;
            }
            if ((bytes[0] == 0x00) && (bytes[1] == 0x00) &&
                (bytes[2] == 0xfe) && (bytes[3] == 0xff))
            {
                //UTF-32 BE
                return new UTF32Encoding(true, true);
            }

            return null;
        }

        /// <summary>
        /// 各文字コードで読んだ文字数が最小のものをファイルの文字コードとします。
        /// </summary>
        private System.Text.Encoding GetEncodingFromLength(byte[] bytes)
        {
            int minLength = int.MaxValue;
            Encoding e = null;
            foreach (Encoding item in _encodingList)
            {
                string s = item.GetString(bytes);

                //エラーとなる文字列を含んでいたらContinueする
                if (s.Contains(_fallbackString)) { continue; }

                int length = s.Length;
                if (length < minLength)
                {
                    minLength = length;
                    e = item;
                }
            }

            //該当ありなら文字コードを返す ないなら null
            return e;
        }
    }
}
