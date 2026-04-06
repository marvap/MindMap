using MindMap._1_Presentation.Components;
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
        // Editor
        private TextEdit? _activeEditor;

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

            Context.CurrProject = new MindMap.Data.MindMapData();
            Context.Controller = new MindMap._2_Logical.Controller();
            Context.MainWindow = this;
        }

        private void MyCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeEditor != null && _activeEditor.IsActive)
            {
                EditorToTextElement(_activeEditor); // ukonči editaci aktuálního prvku
            }

            var pos = e.GetPosition(MyCanvas);
            ShowNewEditor(pos); // zahájí editaci nového prvku
        }

        public void EditorToTextElement(TextEdit te)
        {
            string text = te.Text;

            double x = Canvas.GetLeft(te);
            double y = Canvas.GetTop(te);

            te.CancelEditor();

            if (!string.IsNullOrWhiteSpace(text))
            {
                Context.Controller.NewTextEditingFinished(x, y, text);
            }
        }

        private void ShowNewEditor(Point position)
        {
            _activeEditor = new TextEdit(position);
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
            }

            _keyPressed = e.Key;
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            _keyPressed = null;
        }
    }
}