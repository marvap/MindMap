using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MindMap._1_Presentation.Components
{
    public class SelectionAdorner : Adorner
    {
        public Rect SelectionRect { get; set; } = Rect.Empty;

        private readonly Pen _pen;
        private readonly Brush _fill;

        public SelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;

            _pen = new Pen(Brushes.DodgerBlue, 1);
            _pen.Freeze();

            _fill = new SolidColorBrush(Color.FromArgb(20, 30, 144, 255));
            _fill.Freeze();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (SelectionRect.IsEmpty || SelectionRect.Width <= 0 || SelectionRect.Height <= 0)
                return;

            drawingContext.DrawRectangle(_fill, _pen, SelectionRect);
        }
    }
}
