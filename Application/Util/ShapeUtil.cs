﻿using System.Windows;
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
                new Typeface("Gill Sans Ultra Bold"),
                14,
                Brushes.Red,
                pixelsPerDip
            );
            Geometry textGeometry = formattedText.BuildGeometry(position);
            return new Path
            {
                Data = textGeometry,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
        }

        public static Shape FromRule(Rule rule)
        {
            var fillColor = (Color)ColorConverter.ConvertFromString("#FFFF7F");
            var polygon = new Polygon() { Fill = new SolidColorBrush(fillColor) { Opacity = 0.2 } };

            foreach (var point in rule.Points)
            {
                polygon.Points.Add(new Point { X = point[0], Y = point[1] });
            }

            return polygon;
        }
    }
}
