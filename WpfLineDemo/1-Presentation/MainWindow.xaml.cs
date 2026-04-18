using MindMap._1_Presentation.Components;
using MM = MindMap.Presentation.Components;
using MindMap.Logical;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfLineDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Key? _keyPressed;


        public Key? KeyPressed
        {
            get
            {
                return _keyPressed;
            }
        }

        public Canvas Canvas
        {
            get
            {
                return MyCanvas;
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            new MindMap._2_Logical.Controller().GlobalInit(this);

            this.Closing += MainWindow_Closing;

            ctor2();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Context.Controller.MainWindowIsClosing();
        }

        private void MyCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Context.Controller.CanvasDoubleClicked(e.GetPosition(MyCanvas));
            }
            else
            {
                mouseleftbutton2(sender, e);
            }
        }

        private void ArrowButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kliknuto na šipku");
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_keyPressed == Key.LeftCtrl || _keyPressed == Key.RightCtrl)
            {
                if (e.Key == Key.A)
                {
                    Context.Controller.SaveAs();
                }
                else if (e.Key == Key.S)
                { 
                    Context.Controller.Save();
                }
                else if (e.Key == Key.O)
                {
                    Context.Controller.Open();
                }
                else if (e.Key == Key.N)
                {
                    Context.Controller.New();
                }
                else if (e.Key == Key.C)
                {
                    Context.Controller.Copy();
                }
                else if (e.Key == Key.V && !Context.Controller.IsEditingActive)
                {
                    Context.Controller.Paste();
                }
            }
            if (e.Key == Key.Escape && !Context.Controller.IsEditingActive)
            {
                Context.Controller.ClearSelections();
            }

            _keyPressed = e.Key;
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            _keyPressed = null;
        }






        private Point _dragStart;
        private bool _isSelecting;

        private AdornerLayer? _adornerLayer;
        private SelectionAdorner? _selectionAdorner;

        private void ctor2()
        {
            Loaded += (_, _) =>
            {
                _adornerLayer = AdornerLayer.GetAdornerLayer(MyCanvas);
            };

            //MyCanvas.MouseLeftButtonDown += MyCanvas_MouseLeftButtonDown;
            MyCanvas.MouseMove += MyCanvas_MouseMove;
            MyCanvas.MouseLeftButtonUp += MyCanvas_MouseLeftButtonUp;
        }

        private void mouseleftbutton2(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(MyCanvas);
            _isSelecting = true;

            if (_adornerLayer != null && _selectionAdorner == null)
            {
                _selectionAdorner = new SelectionAdorner(MyCanvas);
                _adornerLayer.Add(_selectionAdorner);
            }

            if (_selectionAdorner != null)
            {
                _selectionAdorner.SelectionRect = new Rect(_dragStart, _dragStart);
                _selectionAdorner.InvalidateVisual();
            }

            MyCanvas.CaptureMouse();
        }

        private void MyCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || _selectionAdorner == null)
                return;

            Point current = e.GetPosition(MyCanvas);

            Rect rect = new Rect(
                new Point(Math.Min(_dragStart.X, current.X), Math.Min(_dragStart.Y, current.Y)),
                new Point(Math.Max(_dragStart.X, current.X), Math.Max(_dragStart.Y, current.Y)));

            _selectionAdorner.SelectionRect = rect;
            _selectionAdorner.InvalidateVisual();
        }

        private void MyCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting)
                return;

            _isSelecting = false;
            MyCanvas.ReleaseMouseCapture();

            Rect selectionRect = _selectionAdorner?.SelectionRect ?? Rect.Empty;

            if (_selectionAdorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_selectionAdorner);
                _selectionAdorner = null;
            }

            SelectItems(selectionRect);
        }

        private void SelectItems(Rect selectionRect)
        {
            foreach (UIElement child in MyCanvas.Children)
            {
                if (child is MM.TextElement fe)
                {
                    Rect bounds = GetElementBounds(fe);
                    bool isSelected = selectionRect.IntersectsWith(bounds);

                    // sem si dej svoji logiku výběru
                    if (isSelected)
                    {
                        Context.Controller.SetElementAsSelected(fe);
                    }
                }
            }
        }

        private Rect GetElementBounds(FrameworkElement element)
        {
            double left = Canvas.GetLeft(element);
            double top = Canvas.GetTop(element);

            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            return new Rect(left, top, element.ActualWidth, element.ActualHeight);
        }
    }
}