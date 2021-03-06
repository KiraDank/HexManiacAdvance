﻿using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Core.ViewModels.Visitors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace HavenSoft.HexManiac.Core.ViewModels {
   public interface IViewPort : ITabContent, INotifyCollectionChanged {
      event EventHandler PreviewScrollChanged;

      string FileName { get; } // Name is dispayed in a tab. FileName lets us know when to call 'ConsiderReload'
      string FullFileName { get; } // FullFileName is displayed when hovering over the tab.

      int Width { get; set; }
      int Height { get; set; }

      bool AutoAdjustDataWidth { get; set; }
      bool StretchData { get; set; }
      bool AllowMultipleElementsPerLine { get; set; }

      bool UseCustomHeaders { get; set; }
      int MinimumScroll { get; }
      int ScrollValue { get; set; }
      int MaximumScroll { get; }
      ObservableCollection<string> Headers { get; }
      ObservableCollection<HeaderRow> ColumnHeaders { get; }
      int DataOffset { get; }
      ICommand Scroll { get; } // parameter: Direction to scroll

      double Progress { get; }
      bool UpdateInProgress { get; }

      string SelectedAddress { get; }
      string SelectedBytes { get; }
      string AnchorText { get; }
      bool AnchorTextVisible { get; }

      HexElement this[int x, int y] { get; }
      IDataModel Model { get; }
      bool IsSelected(Point point);
      bool IsTable(Point point);

      IReadOnlyList<(int start, int end)> Find(string search);
      IChildViewPort CreateChildView(int startAddress, int endAddress);
      void FollowLink(int x, int y);
      void ExpandSelection(int x, int y);
      void ConsiderReload(IFileSystem fileSystem);
      void FindAllSources(int x, int y);
      void ValidateMatchedWords(); // should raise OnMessage if a MatchedWord's value does not match expected.

      bool HasTools { get; }
      ChangeHistory<ModelDelta> ChangeHistory { get; }
      IToolTrayViewModel Tools { get; }

      IReadOnlyList<IContextItem> GetContextMenuItems(Point point);
   }

   public interface IEditableViewPort : IViewPort {
      Point SelectionStart { get; }
      Point SelectionEnd { get; }
      void Edit(string input);
      void Edit(ConsoleKey key);
   }
}
