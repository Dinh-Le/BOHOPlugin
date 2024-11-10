using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using BOHO.Core.Entities;

namespace BOHO.Application.Util
{
    public static class ShapeUtil
    {
        public static Shape FromText(string text, Point position, double pixelsPerDip)
        {
            FormattedText formattedText = new FormattedText(
                text,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                14,
                Brushes.Red,
                pixelsPerDip
            );
            Geometry textGeometry = formattedText.BuildGeometry(position);
            return new Path
            {
                Data = textGeometry,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
            };
        }

        public static Shape FromRule(Rule rule, double scaleX = 1, double scaleY = 1)
        {
            var fillColor = (Color)ColorConverter.ConvertFromString("#FFFF7F");
            var polygon = new Polygon() { Fill = new SolidColorBrush(fillColor) { Opacity = 0.2 } };

            foreach (var point in rule.Points)
            {
                polygon.Points.Add(new Point { X = point[0] * scaleX, Y = point[1] * scaleY });
            }

            return polygon;
        }
    }
}
