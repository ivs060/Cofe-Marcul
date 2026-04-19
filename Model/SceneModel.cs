using System;
using System.Collections.Generic;

namespace игра_для_проги.Model
{
    public class SceneModel
    {
        public List<Point3D> Points { get; private set; } = new List<Point3D>();
        public List<Edge> Edges { get; private set; } = new List<Edge>();

        public event Action Changed;

        public void AddPoint(double x, double y, double z)
        {
            Points.Add(new Point3D(x, y, z));
            NotifyChanged();
        }

        public void AddEdge(Point3D start, Point3D end)
        {
            Edges.Add(new Edge(start, end));
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}