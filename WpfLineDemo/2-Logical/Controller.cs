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


        public void GlobalInit(MainWindow mainWindow)
        {
            Context.CurrProject = new MindMap.Data.MindMapData();
            Context.Controller = this;
            Context.MainWindow = mainWindow;
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

        public void MainWindowIsClosing()
        {
            conditionalSaveOfCurrentProject();
        }


        //****************************************


        public void TextElementEditRequested(TextElement textElement)
        {
            _activeEditor = new TextEdit(textElement.Position, textElement.Text);
            _textElementHidden = textElement;
            _textElementHidden.Visibility = Visibility.Hidden;
        }


        public void EditorToTextElement(TextEdit te)
        {
            string text = te.Text;

            double x = Canvas.GetLeft(te);
            double y = Canvas.GetTop(te);

            te.CancelEditor();

            if (_textElementHidden != null)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _textElementHidden.Text = text;
                    _items.First(i => i.Item2 == _textElementHidden).Item1.Text = text;
                }
                _textElementHidden.Visibility = Visibility.Visible;
                _textElementHidden = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    NewTextEditingFinished(x, y, text);
                }
            }
        }

        private void ShowNewEditor(Point position)
        {
            _activeEditor = new TextEdit(position);
        }

        public void NewTextEditingFinished(double x, double y, string text)
        {
            int zIndex = Context.CurrProject.GetNextMaxZindex();
            TextElement te = TextElement.CreateTextElement(Context.MainWindow, x, y, text, zIndex);
            Context.MainWindow.MyCanvas.Children.Add(te);

            ElementBaseData elementData = new ElementBaseData()
            {
                X = x,
                Y = y,
                Text = text,
                Zindex = zIndex
            };
            Context.CurrProject.Elements.Add(elementData);

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
                drawTwoElementsLine(_items.First(i =>i.Item1.ID == lineData.Element1ID), _items.First(i => i.Item1.ID == lineData.Element2ID), false);
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

        //*********************************************

        private (ElementBaseData, FrameworkElement)? _lineItem1;
        private DateTime _lineItem1ClickTime;

        private (ElementBaseData, FrameworkElement)? _delineItem1;
        private DateTime _delineItem1ClickTime;


        private void drawAllElementLines((ElementBaseData, FrameworkElement) item, bool visible)
        {
            foreach (LineData line in Context.CurrProject.Lines.Where(l => l.Element1ID == item.Item1.ID || l.Element2ID == item.Item1.ID))
            {
                drawTwoElementsLine(_items.First(i => i.Item1.ID == line.Element1ID), _items.First(i => i.Item1.ID == line.Element2ID), visible);
            }
        }

        private void drawTwoElementsLine((ElementBaseData, FrameworkElement) item1, (ElementBaseData, FrameworkElement) item2, bool visible)
        {
            double x1 = item1.Item1.X + item1.Item2.ActualWidth / 2;
            double y1 = item1.Item1.Y + item1.Item2.ActualHeight / 2;
            double x2 = item2.Item1.X + item2.Item2.ActualWidth / 2;
            double y2 = item2.Item1.Y + item2.Item2.ActualHeight / 2;

            LineHelper.DrawLine(Context.MainWindow, x1, y1, x2, y2, visible);
        }

        public void LineElementSpecified(FrameworkElement element)
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
                    Element1ID = _lineItem1.Value.Item1.ID,
                    Element2ID = lineItem2.Item1.ID
                };
                Context.CurrProject.Lines.Add(lineData);

                drawTwoElementsLine(_lineItem1.Value, lineItem2, true);

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
                    drawTwoElementsLine(_delineItem1.Value, delineItem2, false);
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
                TextElement te = TextElement.CreateTextElement(Context.MainWindow, ebd.X, ebd.Y, ebd.Text, ebd.Zindex);
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
                    drawTwoElementsLine(_items.First(i => i.Item1.ID == line.Element1ID), _items.First(i => i.Item1.ID == line.Element2ID), true);
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
                Context.CurrProject.WindowSize = new Size(Context.MainWindow.Width, Context.MainWindow.Height);
                Context.CurrProject.WindowState = Context.MainWindow.WindowState == WindowState.Minimized ? WindowState.Normal : Context.MainWindow.WindowState;

                string content = Context.CurrProject.Serialize();
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

                Context.CurrProject = mmd;
                Context.CurrFilePath = dialog.FileName;
                _items.Clear();

                drawEverythigFromData(Context.CurrProject);

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

            Context.CurrProject = new MindMapData();
            Context.CurrFilePath = null;
            _items.Clear();

            Context.MainWindow.Title = "Mind Map - New project";
        }

        private void conditionalSaveOfCurrentProject()
        {
            string content = Context.CurrProject.Serialize();
            if (content != _dataSaved && Context.CurrProject.Elements.Any())
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

    }
}
