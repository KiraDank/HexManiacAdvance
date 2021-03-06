﻿using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using System;
using System.Collections.Generic;

namespace HavenSoft.HexManiac.Core.Models.Runs.Sprites {
   public interface ITilesetRun : ISpriteRun {

   }

   public class LzTilesetRun : LZRun, ITilesetRun {
      SpriteFormat ISpriteRun.SpriteFormat => new SpriteFormat(Format.BitsPerPixel, Width, Height, Format.PaletteHint);
      public TilesetFormat Format { get; }
      public int Pages => 1;
      public int Width { get; }
      public int Height { get; }
      public bool SupportsImport => false;
      public bool SupportsEdit => true;

      public override string FormatString => $"`lzt{Format.BitsPerPixel}" + (!string.IsNullOrEmpty(Format.PaletteHint) ? "|" + Format.PaletteHint : string.Empty) + "`";

      public LzTilesetRun(TilesetFormat format, IDataModel data, int start, SortedSpan<int> sources = null) : base(data, start, allowLengthErrors: false, sources) {
         Format = format;
         var tileSize = format.BitsPerPixel * 8;
         var uncompressedSize = data.ReadMultiByteValue(start + 1, 3);
         var tileCount = uncompressedSize / tileSize;
         var roughSize = Math.Sqrt(tileCount);
         Width = (int)Math.Ceiling(roughSize);
         Height = (int)roughSize;
         if (Width * Height < tileCount) Height += 1;
      }

      public static bool TryParseTilesetFormat(string format, out TilesetFormat tilesetFormat) {
         tilesetFormat = default;
         if (!(format.StartsWith("`lzt") && format.EndsWith("`"))) return false;
         format = format.Substring(4, format.Length - 5);

         // parse the paletteHint
         string hint = null;
         var pipeIndex = format.IndexOf('|');
         if (pipeIndex != -1) {
            hint = format.Substring(pipeIndex + 1);
            format = format.Substring(0, pipeIndex);
         }

         if (!int.TryParse(format, out int bits)) return false;
         tilesetFormat = new TilesetFormat(bits, hint);
         return true;
      }

      public int[,] GetPixels(IDataModel model, int page) {
         var data = Decompress(model, Start);
         return SpriteRun.GetPixels(data, 0, Width, Height, Format.BitsPerPixel);
      }

      public int[,] GetPixels(IDataModel model, int page, int preferredTileWidth) {
         var data = Decompress(model, Start);
         var tileSize = Format.BitsPerPixel * 8;
         var tileCount = data.Length / tileSize;
         var preferredTileHeight = (int)Math.Ceiling((double)tileCount / preferredTileWidth);
         return SpriteRun.GetPixels(data, 0, preferredTileWidth, preferredTileHeight, Format.BitsPerPixel);
      }

      public ISpriteRun SetPixels(IDataModel model, ModelDelta token, int page, int[,] pixels) {
         // TODO handle the fact that pixels[,] may contain a different number of tiles compared to the existing tileset
         var data = Decompress(model, Start);
         for (int x = 0; x < pixels.GetLength(0); x++) for (int y = 0; y < pixels.GetLength(1); y++) {
            pixels[x, y] %= (int)Math.Pow(2, Format.BitsPerPixel);
         }
         SpriteRun.SetPixels(data, 0, pixels, Format.BitsPerPixel);
         var newModelData = Compress(data, 0, data.Length);
         var newRun = model.RelocateForExpansion(token, this, newModelData.Count);
         for (int i = 0; i < newModelData.Count; i++) token.ChangeData(model, newRun.Start + i, newModelData[i]);
         for (int i = newModelData.Count; i < Length; i++) token.ChangeData(model, newRun.Start + i, 0xFF);
         newRun = new LzTilesetRun(Format, model, newRun.Start, newRun.PointerSources);
         model.ObserveRunWritten(token, newRun);
         return newRun;
      }

      int lastFormatRequested = int.MaxValue;
      public override IDataFormat CreateDataFormat(IDataModel data, int index) {
         var basicFormat = base.CreateDataFormat(data, index);
         if (!CreateForLeftEdge) return basicFormat;
         if (lastFormatRequested < index) {
            lastFormatRequested = index;
            return basicFormat;
         }

         var sprite = data.CurrentCacheScope.GetImage(this);
         var availableRows = (Length - (index - Start)) / ExpectedDisplayWidth;
         lastFormatRequested = index;
         return new SpriteDecorator(basicFormat, sprite, ExpectedDisplayWidth, availableRows);
      }

      protected override BaseRun Clone(SortedSpan<int> newPointerSources) => new LzTilesetRun(Format, Model, Start, newPointerSources);

      public ISpriteRun Duplicate(SpriteFormat format) => new LzSpriteRun(format, Model, Start, PointerSources);

      public ISpriteRun SetPixels(IDataModel model, ModelDelta token, IReadOnlyList<int[,]> tiles) {
         var tileSize = 8 * Format.BitsPerPixel;
         var data = new byte[tiles.Count * tileSize];

         for (int i = 0; i < tiles.Count; i++) {
            SpriteRun.SetPixels(data, i * tileSize, tiles[i], Format.BitsPerPixel);
         }

         var newModelData = Compress(data, 0, data.Length);
         var newRun = (LzTilesetRun)model.RelocateForExpansion(token, this, newModelData.Count);
         for (int i = 0; i < newModelData.Count; i++) token.ChangeData(model, newRun.Start + i, newModelData[i]);
         for (int i = newModelData.Count; i < Length; i++) token.ChangeData(model, newRun.Start + i, 0xFF);
         newRun = new LzTilesetRun(Format, model, newRun.Start, newRun.PointerSources);
         model.ObserveRunWritten(token, newRun);
         return newRun;
      }
   }
}
