using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Controls.Converters
{
    public class ColorHslConverter
    {

        /// <summary>
        /// 指定したColorからHslColorを作成する
        /// </summary>
        /// <param name="rgb">Color</param>
        /// <returns>HslColor</returns>
        public static (float hue, float saturation, float lightness) ColorToHsl(Color rgb)
        {
            float r = (float)rgb.R / 255f;
            float g = (float)rgb.G / 255f;
            float b = (float)rgb.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            float lightness = (max + min) / 2f;

            float hue, saturation;
            if (max == min)
            {
                hue = 0f;
                saturation = 0f;
            }
            else
            {
                float c = max - min;

                if (max == r)
                {
                    hue = (g - b) / c;
                }
                else if (max == g)
                {
                    hue = (b - r) / c + 2f;
                }
                else
                {
                    hue = (r - g) / c + 4f;
                }
                hue *= 60f;
                if (hue < 0f)
                {
                    hue += 360f;
                }

                if (lightness < 0.5f)
                {
                    saturation = c / (max + min);
                }
                else
                {
                    saturation = c / (2f - max - min);
                }
            }

            return (hue, saturation, lightness);
        }

        public static Color HslToRgb(float hue, float saturation, float lightness)
        {
            float s = saturation;
            float l = lightness;

            float r1, g1, b1;
            if (s == 0)
            {
                r1 = l;
                g1 = l;
                b1 = l;
            }
            else
            {
                float h = hue / 60f;
                int i = (int)Math.Floor(h);
                float f = h - i;

                float c;
                if (l < 0.5f)
                {
                    c = 2f * s * l;
                }
                else
                {
                    c = 2f * s * (1f - l);
                }
                float m = l - c / 2f;
                float p = c + m;

                float q;
                if (i % 2 == 0)
                {
                    q = l + c * (f - 0.5f);
                }
                else
                {
                    q = l - c * (f - 0.5f);
                }

                switch (i)
                {
                    case 0:
                        r1 = p;
                        g1 = q;
                        b1 = m;
                        break;
                    case 1:
                        r1 = q;
                        g1 = p;
                        b1 = m;
                        break;
                    case 2:
                        r1 = m;
                        g1 = p;
                        b1 = q;
                        break;
                    case 3:
                        r1 = m;
                        g1 = q;
                        b1 = p;
                        break;
                    case 4:
                        r1 = q;
                        g1 = m;
                        b1 = p;
                        break;
                    case 5:
                        r1 = p;
                        g1 = m;
                        b1 = q;
                        break;
                    case 6:
                        r1 = p;
                        g1 = q;
                        b1 = m;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            return Color.FromRgb(
                (byte)Math.Round(r1 * 255f),
                (byte)Math.Round(g1 * 255f),
                (byte)Math.Round(b1 * 255f));
        }


        public static string ColorToText(Color? color)
        {
            if (color == null) return "";

            int red = color.Value.R;
            int green = color.Value.G;
            int blue = color.Value.B;

            int alpha = color.Value.A;
            if (alpha == 255)
            {
                return "#" + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
            }
            else
            {
                return "#" + alpha.ToString("X2") + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");

            }
        }

        public static Color? TextToColor(string? text)
        {

            if (text == null || string.IsNullOrWhiteSpace(text)) return null;
            bool containSharp = text[0] == '#';

            if (!containSharp)
            {
                try
                {
                    var standardColor = System.Windows.Media.ColorConverter.ConvertFromString(text);
                    if (standardColor != null && standardColor is Color c)
                    {
                        return c;
                    }
                }
                catch { }
            }

            if (containSharp)
            {
                text = text.Substring(1, text.Length - 1);
            }

            byte? alphaValue = null;
            byte? redValue = null;
            byte? greenValue = null;
            byte? blueValue = null;

            try
            {
                if (text.Length >= 2)
                {
                    redValue = System.Convert.ToByte(text.Substring(0, 2), 16);
                }

                if (text.Length >= 4)
                {
                    greenValue = System.Convert.ToByte(text.Substring(2, 2), 16);
                }

                if (text.Length >= 6)
                {
                    blueValue = System.Convert.ToByte(text.Substring(4, 2), 16);
                }

                if (text.Length >= 8)
                {
                    alphaValue = System.Convert.ToByte(text.Substring(0, 2), 16);
                    redValue = System.Convert.ToByte(text.Substring(2, 2), 16);
                    greenValue = System.Convert.ToByte(text.Substring(4, 2), 16);
                    blueValue = System.Convert.ToByte(text.Substring(6, 2), 16);
                }

            }
            catch
            {
                return null;
            }

            if (redValue.HasValue && greenValue.HasValue && blueValue.HasValue)
            {
                return Color.FromRgb(redValue.Value, greenValue.Value, blueValue.Value);
            }
            else
            {
                return null;
            }
        }

    }
}
