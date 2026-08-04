using MindMap._1_Presentation.Components;
using MindMap.Data;
using MindMap.Logical;
using MindMap.Presentation.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfLineDemo;

namespace MindMap._2_Logical
{
    public class Controller
    {
        private List<(ElementBaseData, FrameworkElement)> _items = new List<(ElementBaseData, FrameworkElement)>();


        // Editor
        private TextEdit? _activeEditor;
        private TextElement? _textElementHidden;


        // Hierarchy navigation: path from the root level down to the current one.
        // owner == null marks the root; owner is the parent-level node that owns this sub-level.
        private List<(ElementBaseData? owner, MindMapData level)> _levelPath = new();

        public void GlobalInit(MainWindow mainWindow)
        {
            MindMapData root = new MindMap.Data.MindMapData();
            Context.RootProject = root;
            Context.CurrProject = root;
            Context.Controller = this;
            Context.MainWindow = mainWindow;

            _levelPath = new List<(ElementBaseData?, MindMapData)> { (null, root) };
        }

        public bool IsEditingActive
        {
            get
            {
                return _activeEditor?.IsActive ?? false;
            }
        }

        public void CanvasDoubleClicked(Point mousePosition)
        {
            if (_activeEditor != null && _activeEditor.IsActive)
            {
                EditorToTextElement(_activeEditor); // ukonči editaci aktuálního prvku
            }

            ShowNewEditor(mousePosition); // zahájí editaci nového prvku
        }

        public void NewTextBeingTyped(Point mousePosition, string text)
        {
            ShowNewEditor(mousePosition, text); // zahájí editaci nového prvku
        }

        public void MainWindowIsClosing()
        {
            conditionalSaveOfCurrentProject();
        }


        //****************************************


        public void TextElementEditRequested(TextElement textElement)
        {
            ElementBaseData data = _items.First(i => i.Item2 == textElement).Item1;
            _activeEditor = new TextEdit(textElement.Position, textElement.Text, data.FontSize, data.Bold, data.Italic);
            _textElementHidden = textElement;
            _textElementHidden.Visibility = Visibility.Hidden;
        }


        public void EditorToTextElement(TextEdit te)
        {
            string text = te.Text;
            double fontSize = te.FontSize;
            bool bold = te.FontWeight == FontWeights.Bold;
            bool italic = te.FontStyle == FontStyles.Italic;

            double x = Canvas.GetLeft(te);
            double y = Canvas.GetTop(te);

            te.CancelEditor();

            if (_textElementHidden != null)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var item = _items.First(i => i.Item2 == _textElementHidden);
                    _textElementHidden.Text = text;
                    item.Item1.Text = text;

                    // style may have changed during editing -> commit and refresh lines (bounds moved)
                    if (item.Item1.FontSize != fontSize || item.Item1.Bold != bold || item.Item1.Italic != italic)
                    {
                        drawAllElementLines(item, false);
                        item.Item1.FontSize = fontSize;
                        item.Item1.Bold = bold;
                        item.Item1.Italic = italic;
                        _textElementHidden.SetFontSize(fontSize);
                        _textElementHidden.SetBold(bold);
                        _textElementHidden.SetItalic(italic);
                        redrawAllLinesOnBackground(Context.CurrProject);
                    }
                }
                _textElementHidden.Visibility = Visibility.Visible;
                _textElementHidden = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    NewTextEditingFinished(x, y, text, fontSize, bold, italic);
                }
            }
        }

        public void StopEditingCond()
        {
            if (IsEditingActive)
            { 
                EditorToTextElement(_activeEditor!);
            }
        }

        private void ShowNewEditor(Point position, string? text = null)
        {
            _activeEditor = new TextEdit(position, text);
        }

        public void NewTextEditingFinished(double x, double y, string text, double fontSize = 12, bool bold = false, bool italic = false)
        {
            int zIndex = Context.CurrProject.GetNextMaxZindex();

            ElementBaseData elementData = new ElementBaseData()
            {
                X = x,
                Y = y,
                Text = text,
                Zindex = zIndex,
                FontSize = fontSize,
                Bold = bold,
                Italic = italic
            };
            Context.CurrProject.Elements.Add(elementData);

            TextElement te = TextElement.CreateTextElement(Context.MainWindow, x, y, text, zIndex, elementData.FontSize);
            te.SetBold(elementData.Bold);
            te.SetItalic(elementData.Italic);
            Context.MainWindow.MyCanvas.Children.Add(te);

            _items.Add((elementData, te));
        }

        public void ElementRemovalRequested(TextElement textElement)
        {
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == textElement);

            // Remove everything visually and data
            //
            List<LineData> linesToRemove = Context.CurrProject.Lines.Where(l => l.Element1ID == item.Item1.ID || l.Element2ID == item.Item1.ID).ToList();
            foreach (LineData lineData in linesToRemove)
            {
                drawTwoElementsLine(_items.First(i =>i.Item1.ID == lineData.Element1ID), _items.First(i => i.Item1.ID == lineData.Element2ID), false, lineData.Type);
                Context.CurrProject.Lines.Remove(lineData);
            }

            Context.MainWindow.MyCanvas.Children.Remove(textElement);
            Context.CurrProject.Elements.Remove(item.Item1);

            _items.Remove(item);

            redrawAllLinesOnBackground(Context.CurrProject);
        }

        public int SetElementMaxZindex(FrameworkElement element)
        {
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            int index = Context.CurrProject.GetNextMaxZindex();
            item.Item1.Zindex = index;
            return index;
        }


        //*** MOVING ******************************

        private Point _mouseStartPosition;
        private List<(ElementBaseData, FrameworkElement)> _movedBlock;

        public void ElementStartMoving(FrameworkElement element, Point mouseStartPosition)
        {
            if (isElementInSelectionBlock(element))
            {
                _movedBlock = _selectionBlock;
            }
            else
            {
                _movedBlock = new List<(ElementBaseData, FrameworkElement)>() { getItem(element) };
            }

            _mouseStartPosition = mouseStartPosition;
            
            foreach (var item in _movedBlock)
            {
                (item.Item2 as TextElement).BringToFront();
                drawAllElementLines(item, false); // schovej všechny linie
            }
        }

        public void ElementMoveStep(FrameworkElement element, Point mousePosition)
        {
            foreach (var item in _movedBlock)
            {
                Vector delta = mousePosition - _mouseStartPosition;
                Canvas.SetLeft(item.Item2, item.Item1.X + delta.X);
                Canvas.SetTop(item.Item2, item.Item1.Y + delta.Y);
            }
        }

        public void ElementStopMoving(FrameworkElement element, Point mouseEndPosition)
        {
            foreach (var item in _movedBlock)
            {
                Vector delta = mouseEndPosition - _mouseStartPosition;
                item.Item1.X = item.Item1.X + delta.X;
                item.Item1.Y = item.Item1.Y + delta.Y;
                Canvas.SetLeft(item.Item2, item.Item1.X);
                Canvas.SetTop(item.Item2, item.Item1.Y);
                //drawAllElementLines(item, true); // bohužel hned nelze, kreslí to chybné čáry
            }

            redrawAllLinesOnBackground(Context.CurrProject); // mohly být poškozeny některé jiné linie
        }

        //** BLOCK ***********************************

        private List<(ElementBaseData, FrameworkElement)> _selectionBlock = new List<(ElementBaseData, FrameworkElement)>();

        public void SetElementAsSelected(FrameworkElement element)
        {
            var item = getItem(element);
            if (!_selectionBlock.Contains(item))
            {
                _selectionBlock.Add(item);
                (element as TextElement).MarkAsSelected();
            }
        }

        public void ClearSelections()
        {
            foreach (var item in _selectionBlock)
            {
                (item.Item2 as TextElement).MarkAsUnselected();
            }
            _selectionBlock.Clear();
        }


        // UNIVERZÁLNÍ METODY *****=================================

        private (ElementBaseData, FrameworkElement) getItem(FrameworkElement element)
        {
            return _items.First(i => i.Item2 == element);
        }

        private bool isElementInSelectionBlock(FrameworkElement element)
        {
            return _selectionBlock.Any(i => i.Item2 == element);
        }

        private (ElementBaseData, FrameworkElement)? getItemUnderMouse()
        {
            Point pos = Mouse.GetPosition(Context.MainWindow.MyCanvas);
            HitTestResult result = VisualTreeHelper.HitTest(Context.MainWindow.MyCanvas, pos);
            if (result == null)
            {
                return null;
            }

            DependencyObject obj = result.VisualHit;
            while (obj != null && obj is not TextElement)
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj is TextElement te ? _items.First(i => i.Item2 == te) : null;
        }

        //*** FONT ********************************

        private const double MIN_FONT_SIZE = 4;
        private const double MAX_FONT_SIZE = 96;
        private const double DEFAULT_FONT_SIZE = 12;

        public void ChangeFontSize(double delta)
        {
            // While editing, resize only the editor; the value is committed to the node on finish.
            if (IsEditingActive)
            {
                double editorSize = Math.Clamp(_activeEditor!.FontSize + delta, MIN_FONT_SIZE, MAX_FONT_SIZE);
                _activeEditor.SetFontSize(editorSize);
                return;
            }

            restyleTargets(getStyleTargets(), item =>
            {
                double newSize = Math.Clamp(item.Item1.FontSize + delta, MIN_FONT_SIZE, MAX_FONT_SIZE);
                item.Item1.FontSize = newSize;
                (item.Item2 as TextElement).SetFontSize(newSize);
            });
        }

        public void ResetFontSize()
        {
            if (IsEditingActive)
            {
                _activeEditor!.SetFontSize(DEFAULT_FONT_SIZE);
                return;
            }

            restyleTargets(getStyleTargets(), item =>
            {
                item.Item1.FontSize = DEFAULT_FONT_SIZE;
                (item.Item2 as TextElement).SetFontSize(DEFAULT_FONT_SIZE);
            });
        }

        public void ToggleBold()
        {
            if (IsEditingActive)
            {
                _activeEditor!.ToggleBold();
                return;
            }

            var targets = getStyleTargets();
            bool newBold = !targets.All(t => t.Item1.Bold); // all already bold -> off, otherwise -> on
            restyleTargets(targets, item =>
            {
                item.Item1.Bold = newBold;
                (item.Item2 as TextElement).SetBold(newBold);
            });
        }

        public void ToggleItalic()
        {
            if (IsEditingActive)
            {
                _activeEditor!.ToggleItalic();
                return;
            }

            var targets = getStyleTargets();
            bool newItalic = !targets.All(t => t.Item1.Italic);
            restyleTargets(targets, item =>
            {
                item.Item1.Italic = newItalic;
                (item.Item2 as TextElement).SetItalic(newItalic);
            });
        }

        public void SetColor(NodeColorEnum color)
        {
            if (IsEditingActive)
            {
                return; // colors apply to displayed nodes only
            }

            foreach (var item in getStyleTargets()) // color does not change node bounds -> no line refresh
            {
                item.Item1.Color = color;
                (item.Item2 as TextElement).SetColor(color);
            }
        }

        private List<(ElementBaseData, FrameworkElement)> getStyleTargets()
        {
            if (_selectionBlock.Any())
            {
                return _selectionBlock;
            }
            var hovered = getItemUnderMouse();
            return hovered == null
                ? new List<(ElementBaseData, FrameworkElement)>()
                : new List<(ElementBaseData, FrameworkElement)> { hovered.Value };
        }

        private void restyleTargets(List<(ElementBaseData, FrameworkElement)> targets, Action<(ElementBaseData, FrameworkElement)> apply)
        {
            if (!targets.Any())
            {
                return;
            }

            foreach (var item in targets)
            {
                drawAllElementLines(item, false); // hide connected lines while nodes still have their old size
            }

            foreach (var item in targets)
            {
                apply(item);
            }

            redrawAllLinesOnBackground(Context.CurrProject); // node bounds changed -> line endpoints moved
        }

        //*********************************************

        private (ElementBaseData, FrameworkElement)? _lineItem1;
        private DateTime _lineItem1ClickTime;

        private (ElementBaseData, FrameworkElement)? _delineItem1;
        private DateTime _delineItem1ClickTime;


        private void drawAllElementLines((ElementBaseData, FrameworkElement) item, bool visible)
        {
            foreach (LineData line in Context.CurrProject.Lines.Where(l => l.Element1ID == item.Item1.ID || l.Element2ID == item.Item1.ID))
            {
                drawTwoElementsLine(_items.First(i => i.Item1.ID == line.Element1ID), _items.First(i => i.Item1.ID == line.Element2ID), visible, line.Type);
            }
        }

        private void drawTwoElementsLine((ElementBaseData, FrameworkElement) item1, (ElementBaseData, FrameworkElement) item2, bool visible, LineTypeEnum type)
        {
            double x1 = item1.Item1.X + item1.Item2.ActualWidth / 2;
            double y1 = item1.Item1.Y + item1.Item2.ActualHeight / 2;
            double x2 = item2.Item1.X + item2.Item2.ActualWidth / 2;
            double y2 = item2.Item1.Y + item2.Item2.ActualHeight / 2;

            LineHelper.DrawLine(Context.MainWindow, x1, y1, x2, y2, visible, type == LineTypeEnum.Oriented);
        }

        public void LineElementSpecified(FrameworkElement element, LineTypeEnum type)
        {
            if (_lineItem1 != null && element != _lineItem1.Value.Item2 && DateTime.Now.Subtract(_lineItem1ClickTime).TotalSeconds < 4) // magické 4 s
            {
                var lineItem2 = _items.First(i => i.Item2 == element);

                if (Context.CurrProject.Lines.Any(l => l.Element1ID == _lineItem1.Value.Item1.ID && l.Element2ID == lineItem2.Item1.ID ||
                                                       l.Element1ID == lineItem2.Item1.ID && l.Element2ID == _lineItem1.Value.Item1.ID))
                {
                    MessageBox.Show("Taková relace už existuje.");
                    return;
                }

                LineData lineData = new LineData()
                {
                    Type = type,
                    Element1ID = _lineItem1.Value.Item1.ID, // od tohoto uzlu...
                    Element2ID = lineItem2.Item1.ID         // ...k tomuto (směr šipky u orientované spojnice)
                };
                Context.CurrProject.Lines.Add(lineData);

                drawTwoElementsLine(_lineItem1.Value, lineItem2, true, type);

                _lineItem1 = null;
            }
            else
            {
                _lineItem1 = _items.First(i => i.Item2 == element);
                _lineItem1ClickTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Mazání linie
        /// </summary>
        public void DelineElementSpecified(FrameworkElement element)
        {
            if (_delineItem1 != null && element != _delineItem1.Value.Item2 && DateTime.Now.Subtract(_delineItem1ClickTime).TotalSeconds < 4) // magické 4 s
            {
                var delineItem2 = _items.First(i => i.Item2 == element);

                List<LineData> linesToRemove = Context.CurrProject.Lines.Where(l => l.Element1ID == _delineItem1.Value.Item1.ID && l.Element2ID == delineItem2.Item1.ID ||
                                                       l.Element1ID == delineItem2.Item1.ID && l.Element2ID == _delineItem1.Value.Item1.ID).ToList();
                if (!linesToRemove.Any())
                {
                    MessageBox.Show("Taková relace neexistuje.");
                    return;
                }

                foreach (LineData lineData in linesToRemove)
                {
                    Context.CurrProject.Lines.Remove(lineData);
                    // maž ve směru uložené spojnice (kvůli hrotu orientované šipky)
                    drawTwoElementsLine(_items.First(i => i.Item1.ID == lineData.Element1ID), _items.First(i => i.Item1.ID == lineData.Element2ID), false, lineData.Type);
                }
            }
            else
            {
                _delineItem1 = _items.First(i => i.Item2 == element);
                _delineItem1ClickTime = DateTime.Now;
            }
        }

        private void drawEverythigFromData(MindMapData mmd)
        {
            foreach (ElementBaseData ebd in mmd.Elements)
            {
                TextElement te = TextElement.CreateTextElement(Context.MainWindow, ebd.X, ebd.Y, ebd.Text, ebd.Zindex, ebd.FontSize);
                te.SetBold(ebd.Bold);
                te.SetItalic(ebd.Italic);
                te.SetColor(ebd.Color);
                te.SetHasChildLevel(ebd.ChildLevel != null); // ▸ indicator
                // Panel.SetZIndex(te, ebd.Zindex);
                Context.MainWindow.MyCanvas.Children.Add(te);

                _items.Add((ebd, te));
            }

            redrawAllLinesOnBackground(mmd);
        }

        private void redrawAllLinesOnBackground(MindMapData mmd)
        { 
            Task.Run(async () => { await redrawAllLines(mmd); });
        }

        private async Task redrawAllLines(MindMapData mmd)
        {
            foreach (LineData line in mmd.Lines)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // práce s UI
                    drawTwoElementsLine(_items.First(i => i.Item1.ID == line.Element1ID), _items.First(i => i.Item1.ID == line.Element2ID), true, line.Type);
                }, DispatcherPriority.ContextIdle);
            }
        }





        //*****************************************


        private string DATA_SUBFOLDER = "MindMaps";

        private string _dataSaved;

        public void SaveAs()
        {
            string path = Context.CurrFilePath;
            string directory = null;
            string fileName = null;
            if (string.IsNullOrEmpty(path))
            {
                string pathBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string fullDefaultPath = Path.Combine(pathBase, DATA_SUBFOLDER);
                if (!Directory.Exists(fullDefaultPath))
                {
                    Directory.CreateDirectory(fullDefaultPath);
                }
                directory = fullDefaultPath;
                fileName = "MyMindMap.mmd";
            }
            else
            { 
                directory = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = fileName,
                DefaultDirectory  = directory,
                InitialDirectory = directory,
                Filter = "Soubory typu MindMap (*.mmd)|*.mmd"
            };

            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Context.CurrFilePath = dialog.FileName;

                Save();
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(Context.CurrFilePath))
            {
                SaveAs();
            }
            else
            {
                // Window size is per-level: flush the current level's size; window state is a root-only concern.
                flushCurrentWindowSize();
                Context.RootProject.WindowState = Context.MainWindow.WindowState == WindowState.Minimized ? WindowState.Normal : Context.MainWindow.WindowState;

                string content = Context.RootProject.Serialize();
                File.WriteAllText(Context.CurrFilePath, content, Encoding.UTF8);
                _dataSaved = content;
                Context.MainWindow.Title = "Mind Map - " + Context.CurrFilePath;
            }
        }

        public void Open()
        {
            string pathBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string fullDefaultPath = Path.Combine(pathBase, DATA_SUBFOLDER);
            if (!Directory.Exists(fullDefaultPath))
            {
                Directory.CreateDirectory(fullDefaultPath);
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultDirectory = fullDefaultPath,
                InitialDirectory = fullDefaultPath,
                Filter = "Soubory typu MindMap (*.mmd)|*.mmd",
                Title = "Výběr souboru"
            };

            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                conditionalSaveOfCurrentProject();

                string content = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                MindMapData mmd = MindMapData.Deserialize(content);
                _dataSaved = mmd.Serialize(); // content was normalized while deserializing
                Context.MainWindow.MyCanvas.Children.Clear();

                resetToRoot(mmd); // root = current level, path reset
                Context.CurrFilePath = dialog.FileName;
                _items.Clear();

                drawEverythigFromData(Context.CurrProject);
                updateNavChrome();

                Context.MainWindow.Title = "Mind Map - " + Context.CurrFilePath;
                if (mmd.WindowSize.Width > 50 && mmd.WindowSize.Height > 50)
                {
                    Context.MainWindow.Width = mmd.WindowSize.Width;
                    Context.MainWindow.Height = mmd.WindowSize.Height;
                }
                Context.MainWindow.WindowState = mmd.WindowState;
            }
        }

        public void New()
        {
            conditionalSaveOfCurrentProject();

            Context.MainWindow.MyCanvas.Children.Clear();

            resetToRoot(new MindMapData());
            Context.CurrFilePath = null;
            _items.Clear();

            updateNavChrome();
            Context.MainWindow.Title = "Mind Map - New project";
        }

        /// <summary>Make <paramref name="root"/> both the file root and the current level; reset the navigation path.</summary>
        private void resetToRoot(MindMapData root)
        {
            Context.RootProject = root;
            Context.CurrProject = root;
            _levelPath = new List<(ElementBaseData?, MindMapData)> { (null, root) };
        }

        private void conditionalSaveOfCurrentProject()
        {
            string content = Context.RootProject.Serialize();
            if (content != _dataSaved && Context.RootProject.Elements.Any())
            {
                var result = MessageBox.Show(
                    Context.MainWindow,
                    "Chcete uložit změny?",
                    "Potvrzení",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Save();
                }
            }
        }

        // *=====================================

        public void Copy()
        {
            MindMapData mmd = new MindMapData();
            foreach (ElementBaseData element in _selectionBlock.Select(sb => sb.Item1))
            {
                mmd.Elements.Add(element.Clone());
            }
            foreach (LineData line in Context.CurrProject.Lines.Where(l => mmd.Elements.Any(e => e.ID == l.Element1ID) && mmd.Elements.Any(e => e.ID == l.Element2ID)))
            {
                mmd.Lines.Add(line.Clone());
            }

            mmd.Normalize();
            Clipboard.SetText(mmd.Serialize());
            
            ClearSelections();
        }


        public void Paste()
        {
            MindMapData mmd = null;

            try
            {
                mmd = MindMapData.Deserialize(Clipboard.GetText());
            }
            catch
            {
                MessageBox.Show("Deserializace se nezdařila!");
                return;
            }

            mmd.ShiftIDsByN(Context.CurrProject.GetNextID());
            mmd.ShiftZindexesByN(Context.CurrProject.GetNextMaxZindex());
            double minX = mmd.GetMinX();
            double minY = mmd.GetMinY();
            mmd.ShiftXsByZ(-minX); // TODO - ošetřit nulový posun
            mmd.ShiftYsByZ(-minY); // dtto

            foreach (ElementBaseData element in mmd.Elements)
            {
                Context.CurrProject.Elements.Add(element);
            }
            foreach (LineData line in mmd.Lines)
            { 
                Context.CurrProject.Lines.Add(line);
            }

            drawEverythigFromData(mmd);

            foreach (ElementBaseData element in mmd.Elements)
            {
                var item = _items.First(i => i.Item1 == element);
                SetElementAsSelected(item.Item2);
            }
        }

        //*** HIERARCHY / SUB-LEVELS ***********************************

        /// <summary>Single Ctrl+click: remember the pre-click selection (for the Ctrl+double-click decision), then add to selection.</summary>
        public void CtrlClickSelect(FrameworkElement element)
        {
            // Snapshot the selection BEFORE this click mutates it — the first click of a
            // Ctrl+double-click would otherwise poison the "was it selected?" decision.
            _ctrlGestureSnapshot = _selectionBlock.Select(s => s.Item1).ToList();
            SetElementAsSelected(element);
        }

        private List<ElementBaseData>? _ctrlGestureSnapshot;

        /// <summary>Ctrl+double-click on a node: collapse the pre-gesture selection, or plain enter/create its own sub-level.</summary>
        public void NodeCtrlDoubleClicked(FrameworkElement element)
        {
            ElementBaseData node = getItem(element).Item1;
            List<ElementBaseData> snapshot = _ctrlGestureSnapshot ?? new List<ElementBaseData>();
            _ctrlGestureSnapshot = null;

            if (snapshot.Contains(node))
            {
                // Node was already selected before the gesture -> collapse the selection into a new sub-level.
                collapseSelectionIntoSubLevel(snapshot, node);
            }
            else
            {
                // Node was not selected -> ignore the spurious first-click selection and just enter/create its sub-level.
                ClearSelections();
                enterOrCreateChildLevel(node);
            }
        }

        private void enterOrCreateChildLevel(ElementBaseData owner)
        {
            if (owner.ChildLevel == null)
            {
                // New sub-level inherits the parent's current window size, then keeps its own.
                owner.ChildLevel = new MindMapData
                {
                    WindowSize = new Size(Context.MainWindow.Width, Context.MainWindow.Height)
                };
            }
            _levelPath.Add((owner, owner.ChildLevel));
            switchToCurrentLevel();
        }

        /// <summary>The ⬅ button: go up one level. Auto-prunes an empty level (owner loses its sub-level).</summary>
        public void NavigateUp()
        {
            if (_levelPath.Count <= 1)
            {
                return; // already at root
            }

            StopEditingCond(); // commit any in-progress edit into the level we are leaving

            var leaving = _levelPath[_levelPath.Count - 1];
            if (leaving.owner != null && leaving.level.Elements.Count == 0)
            {
                leaving.owner.ChildLevel = null; // empty sub-level -> prune it (and its existence)
            }

            _levelPath.RemoveAt(_levelPath.Count - 1);
            switchToCurrentLevel();
        }

        /// <summary>Navigate to an explicit path of owner nodes from the root (used by the F2 tree).</summary>
        public void NavigateToOwnerPath(List<ElementBaseData> owners)
        {
            _levelPath = new List<(ElementBaseData?, MindMapData)> { (null, Context.RootProject) };
            MindMapData level = Context.RootProject;
            foreach (ElementBaseData owner in owners)
            {
                if (owner.ChildLevel == null)
                {
                    break; // safety: subtree changed since the tree was built
                }
                level = owner.ChildLevel;
                _levelPath.Add((owner, level));
            }
            switchToCurrentLevel();
        }

        /// <summary>Owner nodes from the root down to (but not including) the root itself — the current path.</summary>
        public List<ElementBaseData> CurrentOwnerPath()
        {
            return _levelPath.Skip(1).Select(p => p.owner!).ToList();
        }

        /// <summary>Rebuild the canvas from the level at the tip of _levelPath and refresh the nav chrome.</summary>
        private void switchToCurrentLevel()
        {
            StopEditingCond();
            ClearSelections();
            flushCurrentWindowSize(); // store the size of the level we are leaving (still Context.CurrProject here)

            // drop transient interaction state that referenced the old level's items
            _movedBlock = null;
            _lineItem1 = null;
            _delineItem1 = null;
            _ctrlGestureSnapshot = null;

            Context.CurrProject = _levelPath[_levelPath.Count - 1].level;

            Context.MainWindow.MyCanvas.Children.Clear();
            _items.Clear();
            drawEverythigFromData(Context.CurrProject);

            // per-level window size
            Size ws = Context.CurrProject.WindowSize;
            if (ws.Width > 50 && ws.Height > 50)
            {
                Context.MainWindow.Width = ws.Width;
                Context.MainWindow.Height = ws.Height;
            }

            updateNavChrome();
        }

        /// <summary>Rebuild the current level in place (after a data mutation) without touching the path.</summary>
        private void rebuildCurrentLevel()
        {
            Context.MainWindow.MyCanvas.Children.Clear();
            _items.Clear();
            drawEverythigFromData(Context.CurrProject);
        }

        private void flushCurrentWindowSize()
        {
            if (Context.CurrProject != null)
            {
                Context.CurrProject.WindowSize = new Size(Context.MainWindow.Width, Context.MainWindow.Height);
            }
        }

        private void updateNavChrome()
        {
            List<string> segments = new List<string> { "root" };
            foreach (var (owner, _) in _levelPath.Skip(1))
            {
                segments.Add(TruncateLabel(owner!.Text, 20));
            }
            Context.MainWindow.SetBreadcrumb(segments);
            Context.MainWindow.SetBackEnabled(_levelPath.Count > 1);
        }

        /// <summary>Label for breadcrumb/tree: single line, max <paramref name="max"/> chars, no mid-word cut, ends with "...".</summary>
        public static string TruncateLabel(string? text, int max)
        {
            string t = (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (t.Length == 0)
            {
                return "(prázdné)";
            }
            if (t.Length <= max)
            {
                return t;
            }
            string cut = t.Substring(0, max);
            int lastSpace = cut.LastIndexOf(' ');
            if (lastSpace >= max / 2) // only avoid a mid-word cut if it doesn't chop off too much
            {
                cut = cut.Substring(0, lastSpace);
            }
            return cut.TrimEnd() + "...";
        }

        //*** DELETE WITH SUB-LEVELS **********************************

        public void ElementDeleteRequested(TextElement textElement)
        {
            ElementBaseData data = getItem(textElement).Item1;
            int subLevels = countSubLevels(data);

            string message = subLevels == 0
                ? "Opravdu chcete smazat tento element?"
                : subLevels == 1
                    ? "Opravdu chcete smazat tento element i s jeho 1 podúrovní?"
                    : $"Opravdu chcete smazat tento element i s jeho {subLevels} podúrovněmi?";

            var result = MessageBox.Show(
                Context.MainWindow,
                message,
                "Potvrzení",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ElementRemovalRequested(textElement); // removing the node removes its whole subtree (ChildLevel is a field)
            }
        }

        /// <summary>Count of sub-levels in the whole subtree of <paramref name="e"/> (levels, not nodes; recursive).</summary>
        private int countSubLevels(ElementBaseData e)
        {
            if (e.ChildLevel == null)
            {
                return 0;
            }
            int count = 1;
            foreach (ElementBaseData child in e.ChildLevel.Elements)
            {
                count += countSubLevels(child);
            }
            return count;
        }

        //*** GROUP-COLLAPSE INTO A SUB-LEVEL ************************

        private void collapseSelectionIntoSubLevel(List<ElementBaseData> selection, ElementBaseData over)
        {
            MindMapData parent = Context.CurrProject;
            HashSet<int> selIds = new HashSet<int>(selection.Select(e => e.ID));

            // internal edge = both endpoints selected; external = exactly one endpoint selected
            List<LineData> internalEdges = parent.Lines
                .Where(l => selIds.Contains(l.Element1ID) && selIds.Contains(l.Element2ID)).ToList();
            List<LineData> externalEdges = parent.Lines
                .Where(l => selIds.Contains(l.Element1ID) ^ selIds.Contains(l.Element2ID)).ToList();

            // Group external edges by the outside node and reroute them onto the group node.
            // For an oriented edge, arrowFromGroup == true means the arrow points group -> outside.
            Dictionary<int, List<(LineTypeEnum type, bool arrowFromGroup)>> byOutside = new();
            foreach (LineData l in externalEdges)
            {
                bool firstInside = selIds.Contains(l.Element1ID);
                int outsideId = firstInside ? l.Element2ID : l.Element1ID;
                bool arrowFromGroup = l.Type == LineTypeEnum.Oriented && firstInside; // Element1 -> Element2
                if (!byOutside.TryGetValue(outsideId, out var list))
                {
                    list = new List<(LineTypeEnum, bool)>();
                    byOutside[outsideId] = list;
                }
                list.Add((l.Type, arrowFromGroup));
            }

            // Detect ambiguous duplicates BEFORE mutating anything; abort on ambiguity.
            Dictionary<int, (LineTypeEnum type, bool arrowFromGroup)> rerouted = new();
            foreach (var kv in byOutside)
            {
                bool hasSimple = kv.Value.Any(x => x.type == LineTypeEnum.Simple);
                List<bool> orientedDirs = kv.Value
                    .Where(x => x.type == LineTypeEnum.Oriented)
                    .Select(x => x.arrowFromGroup).Distinct().ToList();

                if (hasSimple && orientedDirs.Count > 0)
                {
                    MessageBox.Show(Context.MainWindow,
                        "Sbalení nelze provést: mezi skupinovým uzlem a jedním uzlem by vznikla zároveň neorientovaná i orientovaná spojnice (nejednoznačné).",
                        "Nejednoznačné relace", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (orientedDirs.Count > 1)
                {
                    MessageBox.Show(Context.MainWindow,
                        "Sbalení nelze provést: mezi skupinovým uzlem a jedním uzlem by vznikly dvě opačně orientované spojnice (nejednoznačné).",
                        "Nejednoznačné relace", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                rerouted[kv.Key] = orientedDirs.Count == 1
                    ? (LineTypeEnum.Oriented, orientedDirs[0])
                    : (LineTypeEnum.Simple, false);
            }

            // ---- validation passed: mutate the data model ----

            // Build the sub-level from copies of the selected nodes + their internal edges.
            MindMapData child = new MindMapData
            {
                WindowSize = new Size(Context.MainWindow.Width, Context.MainWindow.Height)
            };
            foreach (ElementBaseData e in selection)
            {
                child.Elements.Add(e.Clone()); // as-is (keeps positions + any own sub-levels)
            }
            foreach (LineData l in internalEdges)
            {
                child.Lines.Add(l.Clone());
            }
            child.Normalize();

            // The group node replaces the selection at the cursor node's position; it owns the sub-level.
            ElementBaseData group = new ElementBaseData
            {
                X = over.X,
                Y = over.Y,
                Text = "",
                Zindex = parent.GetNextMaxZindex(),
                ChildLevel = child
            };

            // Remove selected nodes and every edge touching them from the parent level.
            parent.Elements.RemoveAll(e => selIds.Contains(e.ID));
            parent.Lines.RemoveAll(l => selIds.Contains(l.Element1ID) || selIds.Contains(l.Element2ID));

            parent.Elements.Add(group);

            // Add the (deduplicated) rerouted external edges onto the group node.
            foreach (var kv in rerouted)
            {
                int outsideId = kv.Key;
                (LineTypeEnum type, bool arrowFromGroup) = kv.Value;
                LineData nl = new LineData { Type = type };
                if (type == LineTypeEnum.Oriented && !arrowFromGroup)
                {
                    nl.Element1ID = outsideId;  // arrow outside -> group
                    nl.Element2ID = group.ID;
                }
                else
                {
                    nl.Element1ID = group.ID;   // simple, or arrow group -> outside
                    nl.Element2ID = outsideId;
                }
                parent.Lines.Add(nl);
            }

            // Rebuild the level and open the fresh group node for editing.
            ClearSelections(); // selection block still points at the now-removed nodes
            rebuildCurrentLevel();
            var groupItem = _items.First(i => i.Item1 == group);
            TextElementEditRequested((TextElement)groupItem.Item2);
        }

    }
}
