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
        }

        private void MyCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Controller.CanvasClicked(e.GetPosition(MyCanvas));
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