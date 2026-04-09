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





        public void CanvasClicked(Point mousePosition)
        {
            if (_activeEditor != null && _activeEditor.IsActive)
            {
                EditorToTextElement(_activeEditor); // ukonči editaci aktuálního prvku
            }

            ShowNewEditor(mousePosition); // zahájí editaci nového prvku
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
            TextElement te = TextElement.CreateTextElement(Context.MainWindow, x, y, text);
            Context.MainWindow.MyCanvas.Children.Add(te);

            ElementBaseData elementData = new ElementBaseData()
            {
                X = x,
                Y = y,
                Text = text
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

            redrawAllLinesOnBackground();
        }

        public int SetElementMaxZindex(FrameworkElement element)
        {
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            int index = Context.CurrProject.GetMaxZindex();
            index++; 
            item.Item1.Zindex = index;
            return index;
        }


        //*********************************



        public void ElementStartMoving(FrameworkElement element)
        {
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            drawAllElementLines(item, false);
        }

        public void UpdateElementCoordinates(FrameworkElement element, double x, double y)
        { 
            (ElementBaseData, FrameworkElement) item = _items.First(i => i.Item2 == element);
            item.Item1.X = x;
            item.Item1.Y = y;
            drawAllElementLines(item, true);

            redrawAllLinesOnBackground(); // mohly být poškozeny některé jiné linie
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

        private void drawEverythigFromData()
        {
            foreach (ElementBaseData ebd in Context.CurrProject.Elements)
            {
                TextElement te = TextElement.CreateTextElement(Context.MainWindow, ebd.X, ebd.Y, ebd.Text);
                Panel.SetZIndex(te, ebd.Zindex);
                Context.MainWindow.MyCanvas.Children.Add(te);

                _items.Add((ebd, te));
            }

            redrawAllLinesOnBackground();
        }

        private void redrawAllLinesOnBackground()
        { 
            Task.Run(async () => { await redrawAllLines(); });
        }

        private async Task redrawAllLines()
        {
            foreach (LineData line in Context.CurrProject.Lines)
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
                ConditionalSaveOfCurrentProject();

                string content = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                MindMapData mmd = MindMapData.Deserialize(content);
                _dataSaved = content;
                Context.MainWindow.MyCanvas.Children.Clear();

                Context.CurrProject = mmd;
                Context.CurrFilePath = dialog.FileName;
                _items.Clear();

                drawEverythigFromData();

                Context.MainWindow.Title = "Mind Map - " + Context.CurrFilePath;
            }
        }

        public void New()
        {
            ConditionalSaveOfCurrentProject();

            Context.MainWindow.MyCanvas.Children.Clear();

            Context.CurrProject = new MindMapData();
            Context.CurrFilePath = null;
            _items.Clear();

            Context.MainWindow.Title = "Mind Map - New project";
        }

        public void ConditionalSaveOfCurrentProject()
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

    }
}
