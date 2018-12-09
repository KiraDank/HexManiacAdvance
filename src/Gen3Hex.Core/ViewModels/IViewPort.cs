﻿using HavenSoft.Gen3Hex.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace HavenSoft.Gen3Hex.Core.ViewModels {
   public interface IViewPort : ITabContent, INotifyCollectionChanged {
      string FileName { get; } // Name is dispayed in a tab. FileName lets us know when to call 'ConsiderReload'

      int Width { get; set; }
      int Height { get; set; }

      int MinimumScroll { get; }
      int ScrollValue { get; set; }
      int MaximumScroll { get; }
      ObservableCollection<string> Headers { get; }
      ICommand Scroll { get; } // parameter: Direction to scroll

      HexElement this[int x, int y] { get; }
      IModel Model { get; }
      bool IsSelected(Point point);

      IReadOnlyList<int> Find(string search);
      IChildViewPort CreateChildView(int offset);
      void FollowLink(int x, int y);
      void ConsiderReload(IFileSystem fileSystem);
   }
}
