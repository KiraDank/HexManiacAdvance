﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HavenSoft.HexManiac.Core.Models {
   public interface ISearchByte {
      bool Match(byte value);
   }
   public class SearchByte : ISearchByte {
      private readonly byte value;
      public static ISearchByte Wild { get; } = new WildSearchByte();
      public SearchByte(int value) => this.value = (byte)value;
      public static explicit operator SearchByte(byte value) => new SearchByte(value);
      public bool Match(byte value) => value == this.value;
   }
   public class WildSearchByte : ISearchByte {
      public bool Match(byte value) => true;
   }
   public class PCSSearchByte : ISearchByte {
      private readonly byte match1, match2;
      public PCSSearchByte(int value) {
         match1 = (byte)value;
         match2 = match1;
         if (PCSString.PCS[match1] == null) return;
         var valueAsChar = PCSString.PCS[match1][0];
         if (char.IsUpper(valueAsChar)) {
            Debug.Assert(IndexOf(PCSString.PCS, "a") - IndexOf(PCSString.PCS, "A") == 0x1A);
            match2 += 0x1A;
         }
      }
      public bool Match(byte value) => value == match1 || value == match2;
      private static int IndexOf(IReadOnlyList<string> pcs, string value) => 0x100.Range().Single(i => pcs[i] == value);
   }
}
