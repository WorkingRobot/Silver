using System;
using System.Windows;
using System.Windows.Media;

namespace SyntaxHighlighter
{
    sealed class DrawingControl : FrameworkElement {
		private VisualCollection visuals;
		private DrawingVisual visual = new DrawingVisual();

		public DrawingControl() =>
            visuals = new VisualCollection(this) { visual };

		public DrawingContext GetContext() => visual.RenderOpen();

		protected override int VisualChildrenCount => visuals.Count;

        protected override Visual GetVisualChild(int index) =>
            (index < 0 || index >= visuals.Count) ?
                throw new ArgumentOutOfRangeException() :
                visuals[index];
    }
}
