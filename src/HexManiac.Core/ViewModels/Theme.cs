﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace HavenSoft.HexManiac.Core.ViewModels {
   public class Theme : ViewModelCore {
      private string primaryColor = "#DDDDDD", backgroundColor = "#222222";
      private double hueOffset = 0.1, accentSaturation = 0.7, accentValue = 0.7, highlightBrightness = 0.6;

      public string PrimaryColor { get => primaryColor; set { if (TryUpdate(ref primaryColor, value)) UpdateTheme(); } }
      public string BackgroundColor { get => backgroundColor; set { if (TryUpdate(ref backgroundColor, value)) UpdateTheme(); } }
      public double HueOffset { get => hueOffset; set { if (TryUpdate(ref hueOffset, value)) UpdateTheme(); } }
      public double AccentSaturation { get => accentSaturation; set { if (TryUpdate(ref accentSaturation, value)) UpdateTheme(); } }
      public double AccentValue { get => accentValue; set { if (TryUpdate(ref accentValue, value)) UpdateTheme(); } }
      public double HighlightBrightness { get => highlightBrightness; set { if (TryUpdate(ref highlightBrightness, value)) UpdateTheme(); } }

      public Theme(string[] file) {
         bool acceptingEntries = false;
         foreach (var entry in file) {
            var line = entry.ToLower();
            if (line.Contains("[")) acceptingEntries = false;
            if (line.Contains("[theme]")) acceptingEntries = true;
            if (!acceptingEntries) continue;

            if (line.StartsWith("primarycolor")) primaryColor = line.Split("\"".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last();
            if (line.StartsWith("backgroundcolor")) backgroundColor = line.Split("\"".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last();
            if (line.StartsWith("hueoffset")) double.TryParse(line.Substring(line.IndexOf('=') + 1).Trim(), out hueOffset);
            if (line.StartsWith("accentsaturation")) double.TryParse(line.Substring(line.IndexOf('=') + 1).Trim(), out accentSaturation);
            if (line.StartsWith("accentvalue")) double.TryParse(line.Substring(line.IndexOf('=') + 1).Trim(), out accentValue);
            if (line.StartsWith("highlightbrightness")) double.TryParse(line.Substring(line.IndexOf('=') + 1).Trim(), out highlightBrightness);
         }
         UpdateTheme();
      }

      public string[] Serialize() {
         return new[] {
            $"[Theme]",
            $"PrimaryColor = \"{PrimaryColor}\"",
            $"BackgroundColor = \"{BackgroundColor}\"",
            $"HueOffset= {HueOffset}",
            $"AccentSaturation = {AccentSaturation}",
            $"AccentValue = {AccentValue}",
            $"HighlightBrightness = {HighlightBrightness}",
         };
      }

      public static bool TryConvertColor(string text, out (byte r, byte g, byte b) color) {
         const string hex = "0123456789ABCDEF";
         text = text.ToUpper();
         if (text.StartsWith("#")) text = text.Substring(1);
         try {
            if (text.Length == 3) text =
               text.Substring(0, 1) + text.Substring(0, 1) +
               text.Substring(0, 2) + text.Substring(0, 2) +
               text.Substring(0, 3) + text.Substring(0, 3);
            byte r = (byte)(hex.IndexOf(text[0]) * 16 + hex.IndexOf(text[1]));
            byte g = (byte)(hex.IndexOf(text[2]) * 16 + hex.IndexOf(text[3]));
            byte b = (byte)(hex.IndexOf(text[4]) * 16 + hex.IndexOf(text[5]));
            color = (r, g, b);
            return true;
         } catch {
            color = default;
            return false;
         }
      }

      private void UpdateTheme() {
         if (!TryConvertColor(primaryColor, out var uiPrimary)) return;
         if (!TryConvertColor(backgroundColor, out var uiBackground)) return;
         var hsbPrimary = ToHSB(uiPrimary.r, uiPrimary.g, uiPrimary.b);
         var hsbBackground = ToHSB(uiBackground.r, uiBackground.g, uiBackground.b);

         var hsbHighlightLight = hsbPrimary;
         var hsbHighlightDark = hsbBackground;

         var brightnessTravel = .6 + .3 * highlightBrightness;
         hsbHighlightDark.sat *= .8;
         hsbHighlightDark.bright = 1 - brightnessTravel;
         Backlight = hsbHighlightDark.ToRgb().ToHexString();

         hsbHighlightLight.sat = 0;
         hsbHighlightLight.bright = (hsbBackground.bright + hsbPrimary.bright) / 2;
         Secondary = hsbHighlightLight.ToRgb().ToHexString();

         var accent = new List<(double hue, double sat, double bright)>();
         var saturation = accentSaturation * .8 + .2;
         var accentBrightness = accentValue * .6 + .4;
         var prototype = (hue: (hueOffset - .5) / 12, sat: saturation, bright: accentBrightness);
         for (int i = 0; i < 8; i++) {
            accent.Add(prototype);
            prototype.hue += 1 / 8.0;
         }

         Error = accent[0].ToRgb().ToHexString();
         Text1 = accent[1].ToRgb().ToHexString();
         Data1 = accent[2].ToRgb().ToHexString();
         Stream2 = accent[3].ToRgb().ToHexString();
         Data2 = accent[4].ToRgb().ToHexString();
         Accent = accent[5].ToRgb().ToHexString();
         Text2 = accent[6].ToRgb().ToHexString();
         Stream1 = accent[7].ToRgb().ToHexString();
      }

      private string secondary, backlight;
      public string Secondary { get => secondary; set => TryUpdate(ref secondary, value); }
      public string Backlight { get => backlight; set => TryUpdate(ref backlight, value); }

      private string error, text1, text2, data1, data2, accent, stream1, stream2;
      public string Error { get => error; set => TryUpdate(ref error, value); }
      public string Text1 { get => text1; set => TryUpdate(ref text1, value); }
      public string Text2 { get => text2; set => TryUpdate(ref text2, value); }
      public string Data1 { get => data1; set => TryUpdate(ref data1, value); }
      public string Data2 { get => data2; set => TryUpdate(ref data2, value); }
      public string Accent { get => accent; set => TryUpdate(ref accent, value); }
      public string Stream1 { get => stream1; set => TryUpdate(ref stream1, value); }
      public string Stream2 { get => stream2; set => TryUpdate(ref stream2, value); }

      public string Primary => PrimaryColor;
      public string Background => BackgroundColor;

      public static (byte red, byte green, byte blue) FromHSB(double hue, double sat, double bright) {
         while (hue < 0) hue += 1;
         while (hue >= 1) hue -= 1;
         var c = bright * sat;
         var hue2 = hue * 6;
         while (hue2 > 2) hue2 -= 2;
         var x = c * (1 - Math.Abs(hue2 - 1));
         var m = bright - c;

         var (r, g, b) = (0.0, 0.0, 0.0);
         if (hue < 1 / 6.0) (r, g, b) = (c, x, 0);
         else if (hue < 2 / 6.0) (r, g, b) = (x, c, 0);
         else if (hue < 3 / 6.0) (r, g, b) = (0, c, x);
         else if (hue < 4 / 6.0) (r, g, b) = (0, x, c);
         else if (hue < 5 / 6.0) (r, g, b) = (x, 0, c);
         else if (hue < 6 / 6.0) (r, g, b) = (c, 0, x);

         return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
      }

      public static (double hue, double sat, double bright) ToHSB(byte red, byte green, byte blue) {
         double r = red / 255.0;
         double g = green / 255.0;
         double b = blue / 255.0;
         var set = new[] { r, g, b };
         double max = set.Max();
         double min = set.Min();
         var delta = max - min;
         double bright = max;
         double sat = delta == 0.0 ? 0 : delta / max;
         double hue = 0;
         if (delta != 0) {
            if (max == r) hue = (g - b) / delta;
            if (max == g) hue = (b - r) / delta + 2;
            if (max == b) hue = (r - g) / delta + 4;
            while (hue > 6) hue -= 6;
            while (hue < 0) hue += 6;
            hue /= 6;
         }
         return (hue, sat, bright);
      }
   }

   public static class Extensions {
      public static string ToHexString(this (byte red, byte green, byte blue) rgb) {
         var (red, green, blue) = rgb;
         return $"#{red.ToString("X2")}{green.ToString("X2")}{blue.ToString("X2")}";
      }

      public static (byte, byte, byte) ToRgb(this (double hue, double sat, double bright) hsb) => Theme.FromHSB(hsb.hue, hsb.sat, hsb.bright);
   }
}