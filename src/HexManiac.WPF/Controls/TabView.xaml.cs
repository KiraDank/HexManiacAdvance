﻿using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.WPF.Implementations;
using HavenSoft.HexManiac.WPF.Windows;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HavenSoft.HexManiac.WPF.Controls {
   public partial class TabView {

      #region ZoomLevel

      public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register(nameof(ZoomLevel), typeof(int), typeof(TabView), new FrameworkPropertyMetadata(16, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

      public int ZoomLevel {
         get => (int)GetValue(ZoomLevelProperty);
         set => SetValue(ZoomLevelProperty, value);
      }

      #endregion

      #region AnimateScroll

      public static readonly DependencyProperty AnimateScrollProperty = DependencyProperty.Register(nameof(AnimateScroll), typeof(bool), typeof(TabView), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

      public bool AnimateScroll {
         get => (bool)GetValue(AnimateScrollProperty);
         set => SetValue(AnimateScrollProperty, value);
      }

      #endregion

      public IFileSystem FileSystem => (IFileSystem)Application.Current.MainWindow.Resources["FileSystem"];
      public TabView() {
         InitializeComponent();
         CodeModeSelector.ItemsSource = Enum.GetValues(typeof(CodeMode)).Cast<CodeMode>().ToList();
         timer = new DispatcherTimer(TimeSpan.FromSeconds(.6), DispatcherPriority.ApplicationIdle, BlinkCursor, Dispatcher);
         timer.Stop();
      }

      #region Manual Selection Code

      private void HideManualSelection(object sender, EventArgs e) => ManualHighlight.Visibility = Visibility.Collapsed;

      private void ShowManualSelection(object sender, EventArgs e) {
         ManualHighlight.Visibility = Visibility.Visible;
         UpdateManualSelectionFromScroll(StringToolTextBox, EventArgs.Empty);
      }

      private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
         if (e.OldValue is IViewPort viewPortOld1) {
            viewPortOld1.PropertyChanged -= HandleViewPortScrollChanged;
            viewPortOld1.PreviewScrollChanged -= PreviewViewPortScrollChanged;
         }
         if (e.OldValue is ViewPort viewPortOld) {
            viewPortOld.Tools.StringTool.PropertyChanged -= HandleStringToolPropertyChanged;
         }
         if (e.NewValue is IViewPort viewPortNew1) {
            viewPortNew1.PropertyChanged += HandleViewPortScrollChanged;
            viewPortNew1.PreviewScrollChanged += PreviewViewPortScrollChanged;
         }
         if (e.NewValue is ViewPort viewPortNew) {
            viewPortNew.Tools.StringTool.PropertyChanged += HandleStringToolPropertyChanged;
         }
      }

      private void HandleStringToolPropertyChanged(object sender, PropertyChangedEventArgs e) {
         var tool = (PCSTool)sender;

         StringToolTextBox.SelectionChanged -= StringToolContentSelectionChanged;
         if (e.PropertyName is nameof(PCSTool.ContentIndex)) {
            StringToolTextBox.SelectionStart = tool.ContentIndex;
            UpdateManualSelection();
         }
         if (e.PropertyName is nameof(PCSTool.ContentSelectionLength)) {
            StringToolTextBox.SelectionLength = tool.ContentSelectionLength;
            UpdateManualSelection();
         }
         StringToolTextBox.SelectionChanged += StringToolContentSelectionChanged;
      }

      private void UpdateManualSelection() {
         var linesBeforeSelection = StringToolTextBox.Text.Substring(0, StringToolTextBox.SelectionStart).Split(Environment.NewLine).Length - 1;
         var lastLineIndex = StringToolTextBox.Text.Split(Environment.NewLine).Length - 1;
         var highestScroll = Math.Max(StringToolTextBox.ExtentHeight - StringToolTextBox.ViewportHeight, 0);
         var verticalOffset = linesBeforeSelection * highestScroll / lastLineIndex;
         if (StringToolTextBox.VerticalOffset != verticalOffset) {
            StringToolTextBox.ScrollToVerticalOffset(verticalOffset);
         } else {
            UpdateManualSelectionFromScroll(default, default);
         }
      }

      private void UpdateManualSelectionFromScroll(object sender, EventArgs e) {
         if (ManualHighlight.Visibility != Visibility.Visible) return;

         var tools = StringToolTextBox.DataContext as ToolTray;
         if (tools == null || tools.StringTool == null) return;
         var tool = tools.StringTool;

         var linesBeforeSelection = StringToolTextBox.Text.Substring(0, StringToolTextBox.SelectionStart).Split(Environment.NewLine).Length - 1;
         var totalLines = StringToolTextBox.Text.Split(Environment.NewLine).Length;
         var verticalOffset = StringToolTextBox.VerticalOffset;
         var lineHeight = StringToolTextBox.ExtentHeight / totalLines;
         var verticalStart = lineHeight * linesBeforeSelection - verticalOffset + 2;
         if (verticalStart < 0 || verticalStart > StringToolTextBox.ViewportHeight) {
            ManualHighlight.Opacity = 0;
            return;
         }

         var selectionStart = StringToolTextBox.Text.Substring(0, StringToolTextBox.SelectionStart).Split(Environment.NewLine).Last().Length;
         const double fontWidth = 6.6;
         var horizontalStart = selectionStart * fontWidth + 2;
         var width = tool.ContentSelectionLength * fontWidth;
         ManualHighlight.Opacity = 0.4;
         ManualHighlight.Margin = new Thickness(horizontalStart, verticalStart, StringToolTextBox.ActualWidth - horizontalStart - width, 0);
      }

      #endregion

      #region Blink Cursor Code

      private readonly DispatcherTimer timer;

      private static SolidColorBrush Brush(string name) {
         return (SolidColorBrush)Application.Current.Resources.MergedDictionaries[0][name];
      }

      private void UpdateBlinkyCursor(object sender, EventArgs e) {
         if (!(HexContent.ViewPort is ViewPort viewPort) || viewPort.UpdateInProgress) return;
         var screenPosition = HexContent.CursorLocation;
         if (screenPosition.X >= 0 && screenPosition.X < HexContent.ActualWidth && screenPosition.Y >= 0 && screenPosition.Y < HexContent.ActualHeight) {
            var dataPosition = viewPort.SelectionStart;
            var format = viewPort[dataPosition.X, dataPosition.Y].Format;
            double offset;
            if (format is UnderEdit edit) {
               offset = FormatDrawer.CalculateTextOffset(edit.CurrentText, HexContent.FontSize, HexContent.CellWidth, edit);
            } else {
               offset = FormatDrawer.CalculateTextOffset(string.Empty, HexContent.FontSize, HexContent.CellWidth, null);
            }
            BlinkyCursor.Margin = new Thickness(screenPosition.X + offset, screenPosition.Y, 0, 0);
            BlinkyCursor.Height = HexContent.CellHeight;
            BlinkyCursor.Visibility = Visibility.Visible;
         } else {
            BlinkyCursor.Visibility = Visibility.Collapsed;
         }
      }

      private void ShowCursor(object sender, RoutedEventArgs e) {
         if (!(HexContent.ViewPort is ViewPort)) return;
         BlinkyCursor.Fill = Brush(nameof(Theme.Secondary));
         timer.Start();
      }

      private void HideCursor(object sender, RoutedEventArgs e) {
         if (!(HexContent.ViewPort is ViewPort)) return;
         timer.Stop();
         BlinkyCursor.Fill = Brushes.Transparent;
      }

      private void BlinkCursor(object sender, EventArgs e) {
         if (BlinkyCursor.Fill == Brushes.Transparent) {
            BlinkyCursor.Fill = Brush(nameof(Theme.Secondary));
         } else {
            BlinkyCursor.Fill = Brushes.Transparent;
         }
      }

      #endregion

      #region Scrolling Animation Code

      private RenderTargetBitmap hexContentBitmap = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Pbgra32);
      private RenderTargetBitmap headerBitmap = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Pbgra32);

      private bool preppedForScrolling;
      private readonly Key[] arrowKeys = new[] { Key.Down, Key.Up, Key.Left, Key.Right, Key.PageDown, Key.PageUp };

      private void PreviewViewPortScrollChanged(object sender, EventArgs e) {
         if (!AnimateScroll) return;
         if (preppedForScrolling) return; // only prepare for a scroll change if we've handled a scroll since the last time we prepared.
         if (arrowKeys.Any(Keyboard.IsKeyDown)) return;

         var translate = (TranslateTransform)ScrollingHexContent.RenderTransform;
         translate.BeginAnimation(TranslateTransform.YProperty, null); // kill any existing animation
         translate.Y = 0;

         if ((int)HexContent.ActualWidth != hexContentBitmap.PixelWidth || (int)HexContent.ActualHeight != hexContentBitmap.PixelHeight) {
            hexContentBitmap = new RenderTargetBitmap((int)HexContent.ActualWidth, (int)HexContent.ActualHeight, 96, 96, PixelFormats.Pbgra32);
         }
         if ((int)HeaderRenderAreaContainer.ActualWidth != headerBitmap.PixelWidth || (int)HeaderRenderAreaContainer.ActualHeight != headerBitmap.PixelHeight) {
            headerBitmap = new RenderTargetBitmap((int)HeaderRenderAreaContainer.ActualWidth, (int)HeaderRenderAreaContainer.ActualHeight, 96, 96, PixelFormats.Pbgra32);
         }
         hexContentBitmap.Clear();
         headerBitmap.Clear();
         hexContentBitmap.Render(HexContentRenderArea);
         headerBitmap.Render(HeaderRenderArea);
         ((ImageBrush)OldContent.Fill).ImageSource = hexContentBitmap;
         ((ImageBrush)OldHeader.Fill).ImageSource = headerBitmap;
         ((TranslateTransform)OldContent.RenderTransform).Y = 0; // reset the transform to prevent visual glitching

         preppedForScrolling = true;
      }

      private void HandleViewPortScrollChanged(object sender, PropertyChangedEventArgs e) {
         if (!AnimateScroll) return;
         if (e.PropertyName != nameof(IViewPort.ScrollValue)) return;
         if (!(e is ExtendedPropertyChangedEventArgs<int>)) return;
         if (!preppedForScrolling) return;
         var viewPort = (IViewPort)sender;
         var oldValue = ((ExtendedPropertyChangedEventArgs<int>)e).OldValue;
         var newValue = viewPort.ScrollValue;
         var scrollChange = newValue - oldValue;
         if (scrollChange == 0) return;

         var translate = (TranslateTransform)ScrollingHexContent.RenderTransform;
         var currentOffset = scrollChange * HexContent.CellHeight;
         translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(currentOffset, 0, new Duration(TimeSpan.FromMilliseconds(100))));
         ((TranslateTransform)OldContent.RenderTransform).Y = -currentOffset;
         preppedForScrolling = false;
      }

      #endregion

      #region Block Interactions

      protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
         if (DataContext is IViewPort viewPort) e.Handled = viewPort.UpdateInProgress;
         base.OnPreviewMouseDown(e);
      }

      protected override void OnPreviewKeyDown(KeyEventArgs e) {
         if (DataContext is IViewPort viewPort) e.Handled = viewPort.UpdateInProgress;
         base.OnPreviewKeyDown(e);
      }

      #endregion

      #region Anchor Text Selection

      /// <summary>
      /// When a change comes in from the UI
      /// </summary>
      private void AnchorSelectionChanged(object sender, RoutedEventArgs e) {
         var viewPort = DataContext as ViewPort;
         if (viewPort == null) return;

         viewPort.PropertyChanged -= HandleViewportPropertyChanged;
         viewPort.AnchorTextSelectionStart = AnchorTextBox.SelectionStart;
         viewPort.AnchorTextSelectionLength = AnchorTextBox.SelectionLength;
         viewPort.PropertyChanged += HandleViewportPropertyChanged;
      }

      /// <summary>
      /// When a change comes in from the ViewModel
      /// </summary>
      private void HandleViewportPropertyChanged(object sender, PropertyChangedEventArgs e) {
         var viewPort = (ViewPort)sender;

         AnchorTextBox.SelectionChanged -= AnchorSelectionChanged;
         if (e.PropertyName == nameof(viewPort.AnchorTextSelectionStart)) {
            AnchorTextBox.Focus();
            AnchorTextBox.SelectionStart = viewPort.AnchorTextSelectionStart;
            NavigationCommands.NavigateJournal.Execute(AnchorTextBox, this);
         } else if (e.PropertyName == nameof(viewPort.AnchorTextSelectionLength)) {
            AnchorTextBox.SelectionLength = viewPort.AnchorTextSelectionLength;
         }
         AnchorTextBox.SelectionChanged += AnchorSelectionChanged;
      }

      #endregion

      #region ColorSquareInteraction

      private static readonly ColorConverter colorConverter = new ColorConverter();

      private void ShowColorFieldSwatch(object sender, MouseButtonEventArgs e) {
         var element = (FrameworkElement)sender;
         var field = (ColorFieldArrayElementViewModel)element.DataContext;
         var color = TileImage.Convert16BitColor(field.Color);
         var colorString = colorConverter.ConvertToString(color);
         var swatch = new Swatch { Width = 230, Height = 200, Result = colorString };
         swatch.ResultChanged += (s, e1) => {
            var newColor = (Color)ColorConverter.ConvertFromString(swatch.Result);
            field.Color = TileImage.Convert16BitColor(newColor);
         };
         var swatchPopup = new Popup { Placement = PlacementMode.Bottom, PopupAnimation = PopupAnimation.Fade, AllowsTransparency = true, StaysOpen = false, Child = swatch };
         swatchPopup.PlacementTarget = element;
         swatchPopup.IsOpen = true;
      }

      #endregion

      private void StringToolContentSelectionChanged(object sender, RoutedEventArgs e) {
         var textbox = (TextBox)sender;
         var tools = textbox.DataContext as ToolTray;
         if (tools == null || tools.StringTool == null) return;
         var tool = tools.StringTool;

         tool.PropertyChanged -= HandleStringToolPropertyChanged;
         tool.ContentIndex = textbox.SelectionStart;
         tool.ContentSelectionLength = textbox.SelectionLength;
         tool.PropertyChanged += HandleStringToolPropertyChanged;
      }

      private void CodeToolContentsSelectionChanged(object sender, RoutedEventArgs e) {
         var textbox = (TextBox)sender;
         var viewPort = DataContext as IViewPort;
         var tool = viewPort?.Tools?.CodeTool;
         if (tool == null) return;
         var codebody = textbox.DataContext as CodeBody;
         if (codebody == null) return;

         codebody.CaretPosition = textbox.SelectionStart;
         if (string.IsNullOrEmpty(codebody.HelpContent) || !textbox.IsFocused) {
            CodeContentsPopup.IsOpen = false;
            return;
         }

         var linesBeforeSelection = textbox.Text.Substring(0, textbox.SelectionStart).Split(Environment.NewLine).Length - 1;
         var totalLines = textbox.Text.Split(Environment.NewLine).Length;
         var lineHeight = textbox.ExtentHeight / totalLines;
         var verticalStart = lineHeight * (linesBeforeSelection + 1) + 2;

         CodeContentsPopup.Placement = PlacementMode.Absolute;
         var corner = textbox.PointToScreen(new System.Windows.Point(40, verticalStart));
         CodeContentsPopup.HorizontalOffset = corner.X;
         CodeContentsPopup.VerticalOffset = corner.Y;

         var helpParts = codebody.HelpContent.Split(new[] { Environment.NewLine }, 2, StringSplitOptions.None);
         var keyword = helpParts[0].Split(' ')[0];
         var args = helpParts[0].Split(new[] { ' ' }, 2).Last();
         if (args == keyword) args = string.Empty;
         CodeContentsPopupKeywordText.Text = keyword;
         CodeContentsPopupArgsText.Text = " " + args;
         if (!codebody.HelpContent.Contains("#") && codebody.HelpContent.Trim().Contains(Environment.NewLine)) {
            CodeContentsPopupKeywordText.Text = string.Empty;
            CodeContentsPopupArgsText.Text = codebody.HelpContent.Trim();
            CodeContentsPopupDocumentationText.Visibility = Visibility.Collapsed;
         } else if (helpParts.Length == 1 || string.IsNullOrWhiteSpace(helpParts[1])) {
            CodeContentsPopupDocumentationText.Visibility = Visibility.Collapsed;
         } else {
            CodeContentsPopupDocumentationText.Visibility = Visibility.Visible;
            CodeContentsPopupDocumentationText.Text = helpParts[1];
         }

         CodeContentsPopup.IsOpen = true;
         // var selectedLine = textbox.Text.Split(Environment.NewLine)[linesBeforeSelection];
         // Annotate(textbox, textbox.SelectionStart, selectedLine.Split(' ')[0], Brush(nameof(Theme.Accent)));
      }

      // attempt to write text _over_ existing text, to change its color
      // doesn't work very well: text is partially transparent, lining it up perfectly is difficult, and the selection looks wrong.
      private void Annotate(TextBox box, int index, string annotation, SolidColorBrush brush) {
         var grid = box.Parent as Grid;
         if (grid == null) return;
         if (grid.Children.Count != 2) return;
         var annotationBlock = grid.Children[1] as TextBlock;
         if (annotationBlock == null) return;

         var linesBeforeSelection = box.Text.Substring(0, index).Split(Environment.NewLine).Length - 1;
         var totalLines = box.Text.Split(Environment.NewLine).Length;
         var lineHeight = box.ExtentHeight / totalLines;
         var verticalStart = lineHeight * linesBeforeSelection;

         annotationBlock.Text = annotation;
         annotationBlock.Margin = new Thickness(3, verticalStart + 1, 0, 0);
         annotationBlock.Foreground = brush;
      }

      private void HeaderMouseDown(object sender, MouseButtonEventArgs e) {
         HexContent.RaiseEvent(e);
      }

      private readonly Popup contextMenu = new Popup();
      private void AddressShowMenu(object sender, MouseButtonEventArgs e) {
         var element = (FrameworkElement)sender;
         var viewModel = element.DataContext as ViewPort;
         if (viewModel == null) return;
         contextMenu.Child = new Button {
            Content = "Copy Address"
         }.SetEvent(Button.ClickEvent, (sender2, e2) => {
            viewModel.CopyAddress.Execute(FileSystem);
            contextMenu.IsOpen = false;
         });
         contextMenu.Placement = PlacementMode.Mouse;
         contextMenu.StaysOpen = false;
         contextMenu.IsOpen = true;
      }
      private void BytesShowMenu(object sender, MouseButtonEventArgs e) {
         var element = (FrameworkElement)sender;
         var viewModel = element.DataContext as ViewPort;
         if (viewModel == null) return;
         contextMenu.Child = new Button {
            Content = "Copy Bytes"
         }.SetEvent(Button.ClickEvent, (sender2, e2) => {
            viewModel.CopyBytes.Execute(FileSystem);
            contextMenu.IsOpen = false;
         });
         contextMenu.Placement = PlacementMode.Mouse;
         contextMenu.StaysOpen = false;
         contextMenu.IsOpen = true;
      }

      /// <summary>
      /// If the current table is selected, the ViewModel still wants
      /// to know the user input so the ViewModel can Goto the table.
      /// </summary>
      private void TableSelected(object sender, EventArgs e) {
         if (DataContext is IViewPort viewPort) {
            viewPort.Tools.TableTool.SelectedTableSection = SectionSelector.SelectedIndex;
            viewPort.Tools.TableTool.SelectedTableIndex = TableSelector.SelectedIndex;
         }
      }

      private void ActivatePalette(object sender, MouseButtonEventArgs e) {
         var viewModel = (PaletteElementViewModel)((FrameworkElement)sender).DataContext;
         viewModel.Activate();
      }

      private void ClearPopup(object sender, MouseButtonEventArgs e) => CodeContentsPopup.IsOpen = false;

      private void ScrollCodeContent(object sender, MouseWheelEventArgs e) => CodeContentsPopup.IsOpen = false;

      private void ComboBoxArrayElementViewTextInput(object sender, TextCompositionEventArgs e) {
         var element = (FrameworkElement)sender;
         var viewModel = (ComboBoxArrayElementViewModel)element.DataContext;
         var keyString = e.Text;
         var filter = false;
         if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0])) filter = true;
         else if (keyString.Length == 1 && keyString[0].IsAny(" '?\"-_".ToCharArray())) filter = true;
         viewModel.IsFiltering = filter;
      }

      private void ComboBoxTupleElementTextInput(object sender, TextCompositionEventArgs e) {
         var element = (FrameworkElement)sender;
         var viewModel = (EnumTupleElementViewModel)element.DataContext;
         var keyString = e.Text;
         var filter = false;
         if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0])) filter = true;
         else if (keyString.Length == 1 && keyString[0].IsAny(" '?\"-_".ToCharArray())) filter = true;
         viewModel.IsFiltering = filter;
      }

      private void ComboBoxArrayElementViewKeyDown(object sender, KeyEventArgs e) {
         var element = (FrameworkElement)sender;
         var viewModel = (ComboBoxArrayElementViewModel)element.DataContext;
         if (e.Key == Key.Enter) viewModel.ConfirmSelection();
      }

      /// <summary>
      /// TextBlock is a lot faster than TextBox.
      /// And we only ever really need to have the focus in a single textbox at a time.
      /// Therefore, for performance reasons, we'd rather have all the non-active textboxes just
      /// *look* like TextBoxes, and really be TextBlocks instead.
      /// </summary>
      private void UpdateFieldTextBox(object sender, RoutedEventArgs e) {
         var control = (UserControl)sender;
         var isAcitve = control.IsMouseOver || control.IsFocused || control.IsKeyboardFocusWithin;
         if (isAcitve && control.Content is TextBoxLookAlike) {
            var keyBinding = new KeyBinding { Key = Key.Enter };
            BindingOperations.SetBinding(keyBinding, InputBinding.CommandProperty, new Binding(nameof(FieldArrayElementViewModel.Accept)));
            var textBox = new TextBox { UndoLimit = 0, InputBindings = { keyBinding } };
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(FieldArrayElementViewModel.Content)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            control.Content = textBox;
            if (control.IsKeyboardFocused) {
               textBox.Loaded += (sender1, e1) => {
                  Keyboard.Focus(textBox);
                  control.Focusable = false;
               };
            } else {
               control.Focusable = false;
            }
         } else if (!isAcitve && !(control.Content is TextBoxLookAlike)) {
            control.Content = new TextBoxLookAlike();
            control.Focusable = true;
         }
      }

      private void ResetLeftToolsPane(object sender, MouseButtonEventArgs e) {
         LeftToolsPane.Width = new GridLength(275);
      }

      private void CheckboxKeyUp(object sender, KeyEventArgs e) {
         if (e.Key != Key.Enter) return;
         var box = (CheckBox)sender;
         box.IsChecked = !box.IsChecked;
         e.Handled = true;
      }
   }
}
