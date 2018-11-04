﻿using HavenSoft.Gen3Hex.Model;
using HavenSoft.Gen3Hex.ViewModel;
using System.IO;
using System.Reflection;
using System.Windows;

[assembly: AssemblyTitle("Gen3Hex")]

namespace HavenSoft.Gen3Hex.View {
   public partial class App {
      protected override void OnStartup(StartupEventArgs e) {
         base.OnStartup(e);
         var fileName = e.Args?.Length == 1 ? e.Args[0] : string.Empty;
         var viewPort = GetViewModel(fileName);
         MainWindow = new MainWindow(viewPort);
         MainWindow.Show();
      }

      private EditorViewModel GetViewModel(string fileName) {
         var editor = new EditorViewModel(new WindowsFileSystem());
         if (!File.Exists(fileName)) return editor;

         var bytes = File.ReadAllBytes(fileName);
         var loadedFile = new LoadedFile(fileName, bytes);
         editor.Add(new ViewPort(loadedFile));
         return editor;
      }
   }
}
