using System;
using System.Collections.Generic;

namespace игра_для_проги.Model
{
    public class SceneModel
    {
        // Списки теперь хранят объекты типа Point
        public List<Point> Points { get; private set; } = new List<Point>();
        public List<Edge> Edges { get; private set; } = new List<Edge>();

        public event EventHandler SceneChanged;

        public void AddPoint(double x, double y, double z)
        {
            Points.Add(new Point(x, y, z));
        }

        public void AddEdge(Point p1, Point p2)
        {
            Edges.Add(new Edge(p1, p2));
        }

        public void NotifyChanged()
        {
            SceneChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}