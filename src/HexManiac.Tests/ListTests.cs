﻿using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class ListTests : BaseViewModelTestClass {
      [Fact]
      public void MetadataCanLoadList() {
         var lines = new List<string>();
         lines.Add("[[List]]");
         lines.Add("Name = '''moveeffects'''");
         lines.Add("1 = [");
         lines.Add("   '''abc''',");
         lines.Add("   '''\"def\"''',");
         lines.Add("]");
         lines.Add("5 = ['''xyz''']");
         lines.Add("7 = '''bob'''");

         var metadata = new StoredMetadata(lines.ToArray());

         var moveEffects = metadata.Lists.Single(list => list.Name == "moveeffects");
         Assert.Equal(8, moveEffects.Count);
         Assert.Equal("0", moveEffects[0]);
         Assert.Equal("abc", moveEffects[1]);
         Assert.Equal("\"def\"", moveEffects[2]);
         Assert.Equal("3", moveEffects[3]);
         Assert.Equal("4", moveEffects[4]);
         Assert.Equal("xyz", moveEffects[5]);
         Assert.Equal("6", moveEffects[6]);
         Assert.Equal("bob", moveEffects[7]);
      }

      [Fact]
      public void MetadataCanSaveList() {
         var input = new List<string> {
            "bob",
            "tom",
            null,
            "sam",
            "fry",
            "kevin",
            null,
            null,
            "carl",
         };
         var list = new StoredList("Input", input);

         var lines = new List<string>();
         list.AppendContents(lines);

         Assert.Equal(@"[[List]]
Name = '''Input'''
0 = [
   '''bob''',
   '''tom''',
]
3 = [
   '''sam''',
   '''fry''',
   '''kevin''',
]
8 = '''carl'''
".Split(Environment.NewLine).ToList(), lines);
      }

      [Fact]
      public void ListLengthedArraysCannotBeExpanded() {
         var input = new List<string> { "bob", "tom", "steve" };
         Model.SetList("list", input);
         ViewPort.Edit("^table[content:]list ");

         var run = (ITableRun)Model.GetNextRun(0);
         Assert.Equal(3, run.ElementCount);
         Assert.False(run.CanAppend);

         ViewPort.Edit("@06 +");
         Assert.Single(Errors);
      }

      [Fact]
      public void ListWithPipe_WritePipeInEnum_EditWorks() {
         Model.SetList("list", new List<string> { "some|text", "other|text", "content" });
         ViewPort.Edit("^table[a:list]2 ");

         ViewPort.Edit("other|text ");

         Assert.Equal(1, Model.ReadMultiByteValue(0, 2));
      }

      [Fact]
      public void ListWithSpaceAndComma_WriteInEnum_EditWorks() {
         Model.SetList("list", new List<string> { "some|text", "other, text", "content" });
         ViewPort.Edit("^table[a:list]2 ");

         ViewPort.Edit("\"other, text\" ");

         Assert.Equal(1, Model.ReadMultiByteValue(0, 2));
         Assert.Equal(2, ViewPort.ConvertViewPointToAddress(ViewPort.SelectionStart));
      }
   }
}

