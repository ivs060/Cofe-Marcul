using System;
using System.Collections.Generic;
using System.Drawing;

namespace CafeMarkul.Model
{
    public enum FaceLayer
    {
        Floor = 0,
        Wall = 1,
        WallDetail = 2,

        // Детали рисунков на постерах.
        // Нужен, чтобы кружка/пар были поверх бумаги постера,
        // но не спорили с мебелью и не мерцали.
        PosterDetail = 3,

        Furniture = 4,
        SmallDetail = 5
    }

    public class Camera3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double Yaw { get; set; }
        public double Pitch { get; set; }

        public Camera3D()
        {
            X = 0;
            Y = 0;
            Z = 75;

            Yaw = 0;
            Pitch = 0;
        }

        public void Set(double x, double y, double z, double yaw)
        {
            X = x;
            Y = y;
            Z = z;

            Yaw = yaw;
            Pitch = 0;
        }
    }

    public class Face
    {
        public List<int> PointIndices { get; set; }

        public string TextureKey { get; set; }

        public Color SolidColor { get; set; }

        public FaceLayer Layer { get; set; }
        public bool TwoSided { get; set; }

        // true = грань рисуется с обеих сторон.
        // Нужно для плоских AddQuad-деталей: экранов, панелей, этикеток, масок.

        public Face(
    List<int> pointIndices,
    string textureKey = null,
    Color solidColor = default,
    FaceLayer layer = FaceLayer.Furniture,
    bool twoSided = false)
        {
            PointIndices = pointIndices ?? new List<int>();
            TextureKey = textureKey;

            if (solidColor == default)
                SolidColor = Color.Empty;
            else
                SolidColor = solidColor;

            Layer = layer;
            TwoSided = twoSided;
        }
    }

    public class BoxCollider
    {
        public double MinX { get; private set; }
        public double MinZ { get; private set; }

        public double MaxX { get; private set; }
        public double MaxZ { get; private set; }

        public string Name { get; private set; }

        public bool Enabled { get; set; }

        public BoxCollider(
            double minX,
            double minZ,
            double maxX,
            double maxZ,
            string name = null)
        {
            MinX = Math.Min(minX, maxX);
            MaxX = Math.Max(minX, maxX);

            MinZ = Math.Min(minZ, maxZ);
            MaxZ = Math.Max(minZ, maxZ);

            Name = name;
            Enabled = true;
        }
    }

    public class SceneModel
    {
        public List<Point3D> Points { get; private set; }
        public List<Edge> Edges { get; private set; }
        public List<Face> Faces { get; private set; }
        public List<BoxCollider> Colliders { get; private set; }

        public event EventHandler SceneChanged;

        public SceneModel()
        {
            Points = new List<Point3D>();
            Edges = new List<Edge>();
            Faces = new List<Face>();
            Colliders = new List<BoxCollider>();
        }

        public int AddPoint(double x, double y, double z)
        {
            Points.Add(new Point3D(x, y, z));
            return Points.Count - 1;
        }

        public void AddEdge(Point3D p1, Point3D p2)
        {
            Edges.Add(new Edge(p1, p2));
        }

        public void AddFace(
    List<int> pointIndices,
    string textureKey = null,
    Color solidColor = default,
    FaceLayer layer = FaceLayer.Furniture,
    bool twoSided = false)
        {
            Faces.Add(new Face(pointIndices, textureKey, solidColor, layer, twoSided));
        }

        public void AddCollider(
            double minX,
            double minZ,
            double maxX,
            double maxZ,
            string name = null)
        {
            Colliders.Add(new BoxCollider(minX, minZ, maxX, maxZ, name));
        }

        public void NotifyChanged()
        {
            SceneChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}