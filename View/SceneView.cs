using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using игра_для_проги.Model;

namespace игра_для_проги.View
{
    public class SceneView : Control
    {
        private const double NearClip = 0.7;
        private const float ProjectionScale = 500f;
        private const double DepthEpsilon = 0.8;

        private struct ViewPoint
        {
            public double X;
            public double Y;
            public double Z;

            public ViewPoint(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        private class RenderFaceInfo
        {
            public Face Face;
            public PointF[] ScreenPoints;
            public double AverageZ;
            public double MinZ;
            public double MaxZ;
            public RectangleF ScreenBounds;
            public int SourceIndex;
        }

        public SceneView()
        {
            DoubleBuffered = true;
            BackColor = Color.Black;
        }

        public void Render(Graphics g, SceneModel model, Camera3D camera, int width, int height)
        {
            if (model == null || camera == null)
                return;

            if (width <= 0 || height <= 0)
                return;

            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            g.SmoothingMode = SmoothingMode.None;

            ViewPoint[] cameraPoints = new ViewPoint[model.Points.Count];

            double cosYaw = Math.Cos(camera.Yaw);
            double sinYaw = Math.Sin(camera.Yaw);

            double cosPitch = Math.Cos(camera.Pitch);
            double sinPitch = Math.Sin(camera.Pitch);

            for (int i = 0; i < model.Points.Count; i++)
            {
                cameraPoints[i] = WorldToCamera(
                    model.Points[i],
                    camera,
                    cosYaw,
                    sinYaw,
                    cosPitch,
                    sinPitch
                );
            }

            List<RenderFaceInfo> renderFaces = new List<RenderFaceInfo>();

            for (int i = 0; i < model.Faces.Count; i++)
            {
                Face face = model.Faces[i];

                if (face == null ||
                    face.PointIndices == null ||
                    face.PointIndices.Count < 3)
                {
                    continue;
                }

                List<ViewPoint> polygon = new List<ViewPoint>();
                bool invalidFace = false;

                for (int j = 0; j < face.PointIndices.Count; j++)
                {
                    int pointIndex = face.PointIndices[j];

                    if (pointIndex < 0 || pointIndex >= model.Points.Count)
                    {
                        invalidFace = true;
                        break;
                    }

                    polygon.Add(cameraPoints[pointIndex]);
                }

                if (invalidFace)
                    continue;

                List<ViewPoint> clippedPolygon = ClipPolygonNearPlane(polygon);

                if (clippedPolygon.Count < 3)
                    continue;

                // ВАЖНО:
                // Отсекаем задние грани только у мебели и мелких деталей.
                // Это помогает не видеть внутренности кофемашины, кассы, люстры и рамок.
                // Пол и стены НЕ отсекаем, чтобы они не пропадали.
                if (ShouldCullBackFace(face) && IsBackFacing(clippedPolygon))
                    continue;

                PointF[] screenPoints = new PointF[clippedPolygon.Count];

                double sumZ = 0;
                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

                for (int j = 0; j < clippedPolygon.Count; j++)
                {
                    ViewPoint point = clippedPolygon[j];

                    screenPoints[j] = Project(point, width, height);

                    sumZ += point.Z;

                    if (point.Z < minZ)
                        minZ = point.Z;

                    if (point.Z > maxZ)
                        maxZ = point.Z;
                }

                RenderFaceInfo info = new RenderFaceInfo();
                info.Face = face;
                info.ScreenPoints = screenPoints;
                info.AverageZ = sumZ / clippedPolygon.Count;
                info.MinZ = minZ;
                info.MaxZ = maxZ;
                info.ScreenBounds = GetBounds(screenPoints);
                info.SourceIndex = i;

                renderFaces.Add(info);
            }

            renderFaces.Sort(CompareFacesForRender);

            for (int i = 0; i < renderFaces.Count; i++)
            {
                DrawFace(g, renderFaces[i].Face, renderFaces[i].ScreenPoints);
            }
        }

        // =========================================================
        // СОРТИРОВКА
        // =========================================================

        private int CompareFacesForRender(RenderFaceInfo a, RenderFaceInfo b)
        {
            int groupA = GetRenderGroup(a.Face);
            int groupB = GetRenderGroup(b.Face);

            if (groupA != groupB)
                return groupA.CompareTo(groupB);

            bool overlapOnScreen = BoundsOverlap(a.ScreenBounds, b.ScreenBounds);

            if (overlapOnScreen)
            {
                // Если одна грань целиком дальше другой,
                // дальнюю рисуем раньше.
                if (a.MinZ > b.MaxZ + DepthEpsilon)
                    return -1;

                if (b.MinZ > a.MaxZ + DepthEpsilon)
                    return 1;
            }

            // Стабильный вариант:
            // сначала MaxZ, потом AverageZ, потом MinZ.
            // Этот порядок у тебя был лучше для барной стойки.
            int maxCompare = b.MaxZ.CompareTo(a.MaxZ);
            if (maxCompare != 0)
                return maxCompare;

            int avgCompare = b.AverageZ.CompareTo(a.AverageZ);
            if (avgCompare != 0)
                return avgCompare;

            int minCompare = b.MinZ.CompareTo(a.MinZ);
            if (minCompare != 0)
                return minCompare;

            int layerCompare = GetLayerTiePriority(a.Face).CompareTo(GetLayerTiePriority(b.Face));
            if (layerCompare != 0)
                return layerCompare;

            double areaA = Math.Abs(GetPolygonArea(a.ScreenPoints));
            double areaB = Math.Abs(GetPolygonArea(b.ScreenPoints));

            int areaCompare = areaB.CompareTo(areaA);
            if (areaCompare != 0)
                return areaCompare;

            return a.SourceIndex.CompareTo(b.SourceIndex);
        }

        private int GetRenderGroup(Face face)
        {
            if (face.Layer == FaceLayer.Floor)
                return 0;

            if (face.Layer == FaceLayer.Wall || face.Layer == FaceLayer.WallDetail)
                return 1;

            // Furniture и SmallDetail вместе.
            // Это важно, чтобы предметы на баре, стулья и мебель
            // сортировались по глубине, а не только по слою.
            return 2;
        }

        private int GetLayerTiePriority(Face face)
        {
            switch (face.Layer)
            {
                case FaceLayer.Floor:
                    return 0;

                case FaceLayer.Wall:
                    return 1;

                case FaceLayer.WallDetail:
                    return 2;

                case FaceLayer.Furniture:
                    return 3;

                case FaceLayer.SmallDetail:
                    return 4;

                default:
                    return 10;
            }
        }

        private bool BoundsOverlap(RectangleF a, RectangleF b)
        {
            return a.Left <= b.Right &&
                   a.Right >= b.Left &&
                   a.Top <= b.Bottom &&
                   a.Bottom >= b.Top;
        }

        private RectangleF GetBounds(PointF[] points)
        {
            if (points == null || points.Length == 0)
                return RectangleF.Empty;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].X < minX)
                    minX = points[i].X;

                if (points[i].Y < minY)
                    minY = points[i].Y;

                if (points[i].X > maxX)
                    maxX = points[i].X;

                if (points[i].Y > maxY)
                    maxY = points[i].Y;
            }

            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }

        // =========================================================
        // ОТСЕЧЕНИЕ ЗАДНИХ ГРАНЕЙ
        // =========================================================

        private bool ShouldCullBackFace(Face face)
        {
            // Не трогаем пол и стены.
            // У них порядок точек может быть разный, и если их отсекать,
            // они начнут пропадать.
            if (face.Layer == FaceLayer.Floor)
                return false;

            if (face.Layer == FaceLayer.Wall)
                return false;

            // У деталей и мебели задние грани можно отсекать.
            return face.Layer == FaceLayer.WallDetail ||
                   face.Layer == FaceLayer.Furniture ||
                   face.Layer == FaceLayer.SmallDetail;
        }

        private bool IsBackFacing(List<ViewPoint> points)
        {
            if (points == null || points.Count < 3)
                return true;

            ViewPoint p0 = points[0];
            ViewPoint p1 = points[1];
            ViewPoint p2 = points[2];

            double ax = p1.X - p0.X;
            double ay = p1.Y - p0.Y;
            double az = p1.Z - p0.Z;

            double bx = p2.X - p0.X;
            double by = p2.Y - p0.Y;
            double bz = p2.Z - p0.Z;

            double nx = ay * bz - az * by;
            double ny = az * bx - ax * bz;
            double nz = ax * by - ay * bx;

            double centerX = 0;
            double centerY = 0;
            double centerZ = 0;

            for (int i = 0; i < points.Count; i++)
            {
                centerX += points[i].X;
                centerY += points[i].Y;
                centerZ += points[i].Z;
            }

            centerX /= points.Count;
            centerY /= points.Count;
            centerZ /= points.Count;

            // Камера в camera-space находится в точке 0,0,0.
            double toCameraX = -centerX;
            double toCameraY = -centerY;
            double toCameraZ = -centerZ;

            double dot =
                nx * toCameraX +
                ny * toCameraY +
                nz * toCameraZ;

            return dot <= 0;
        }

        // =========================================================
        // КАМЕРА И ПРОЕКЦИЯ
        // =========================================================

        private ViewPoint WorldToCamera(
            Point3D point,
            Camera3D camera,
            double cosYaw,
            double sinYaw,
            double cosPitch,
            double sinPitch)
        {
            double dx = point.X - camera.X;
            double dy = point.Y - camera.Y;
            double dz = point.Z - camera.Z;

            double viewX = dx * cosYaw - dz * sinYaw;
            double viewZ = dx * sinYaw + dz * cosYaw;
            double viewY = dy;

            double pitchedY = viewY * cosPitch - viewZ * sinPitch;
            double pitchedZ = viewY * sinPitch + viewZ * cosPitch;

            return new ViewPoint(viewX, pitchedY, pitchedZ);
        }

        private List<ViewPoint> ClipPolygonNearPlane(List<ViewPoint> input)
        {
            List<ViewPoint> output = new List<ViewPoint>();

            if (input == null || input.Count == 0)
                return output;

            for (int i = 0; i < input.Count; i++)
            {
                ViewPoint current = input[i];
                ViewPoint next = input[(i + 1) % input.Count];

                bool currentInside = current.Z >= NearClip;
                bool nextInside = next.Z >= NearClip;

                if (currentInside && nextInside)
                {
                    output.Add(next);
                }
                else if (currentInside && !nextInside)
                {
                    output.Add(IntersectNearPlane(current, next));
                }
                else if (!currentInside && nextInside)
                {
                    output.Add(IntersectNearPlane(current, next));
                    output.Add(next);
                }
            }

            return output;
        }

        private ViewPoint IntersectNearPlane(ViewPoint a, ViewPoint b)
        {
            double dz = b.Z - a.Z;

            if (Math.Abs(dz) < 0.000001)
                return new ViewPoint(a.X, a.Y, NearClip);

            double t = (NearClip - a.Z) / dz;

            double x = a.X + (b.X - a.X) * t;
            double y = a.Y + (b.Y - a.Y) * t;

            return new ViewPoint(x, y, NearClip);
        }

        private PointF Project(ViewPoint point, int width, int height)
        {
            double z = point.Z;

            if (z < NearClip)
                z = NearClip;

            return new PointF(
                (float)(width / 2.0 + point.X * (ProjectionScale / z)),
                (float)(height / 2.0 - point.Y * (ProjectionScale / z))
            );
        }

        // =========================================================
        // ОТРИСОВКА
        // =========================================================

        private void DrawFace(Graphics g, Face face, PointF[] points)
        {
            if (points == null || points.Length < 3)
                return;

            Color color = GetMaterialColor(face);

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, points);
            }

            if (points.Length == 4 && ShouldDrawPattern(face, points))
            {
                DrawMaterialPattern(g, face, points);
            }

            if (ShouldDrawOutline(face, points))
            {
                DrawSoftOutline(g, points);
            }
        }

        private bool ShouldDrawPattern(Face face, PointF[] points)
        {
            if (face.Layer == FaceLayer.SmallDetail)
                return false;

            if (face.Layer == FaceLayer.WallDetail)
                return false;

            double area = Math.Abs(GetPolygonArea(points));

            if (area < 1500)
                return false;

            return true;
        }

        private bool ShouldDrawOutline(Face face, PointF[] points)
        {
            double area = Math.Abs(GetPolygonArea(points));

            // Совсем микроскопические грани не обводим.
            // Иначе мелкие буквы, этикетки и значки превращаются в грязь.
            if (area < 45)
                return false;

            // SmallDetail — это как раз этикетки, буквы, мелкие элементы,
            // крышки и тонкие декоративные детали.
            if (face.Layer == FaceLayer.SmallDetail)
            {
                return area >= 420;
            }

            // WallDetail тоже часто плоский и декоративный.
            // На мелких деталях outline только мешает.
            if (face.Layer == FaceLayer.WallDetail)
            {
                return area >= 650;
            }

            return true;
        }

        private double GetPolygonArea(PointF[] points)
        {
            if (points == null || points.Length < 3)
                return 0;

            double area = 0;

            for (int i = 0; i < points.Length; i++)
            {
                PointF a = points[i];
                PointF b = points[(i + 1) % points.Length];

                area += a.X * b.Y - b.X * a.Y;
            }

            return area * 0.5;
        }

        private Color GetMaterialColor(Face face)
        {
            if (face.SolidColor != Color.Empty)
            {
                return Color.FromArgb(
                    255,
                    face.SolidColor.R,
                    face.SolidColor.G,
                    face.SolidColor.B
                );
            }

            switch (face.TextureKey)
            {
                case "floor":
                    return Color.FromArgb(76, 68, 58);

                case "wall":
                    return Color.FromArgb(128, 112, 92);

                case "bar":
                    return Color.FromArgb(82, 36, 26);

                case "wood_dark":
                    return Color.FromArgb(48, 25, 18);

                case "mid_wood":
                    return Color.FromArgb(72, 38, 26);

                case "fridge":
                    return Color.FromArgb(126, 132, 136);

                case "black":
                case "screen":
                    return Color.FromArgb(6, 8, 10);

                default:
                    return Color.FromArgb(58, 54, 56);
            }
        }

        private void DrawMaterialPattern(Graphics g, Face face, PointF[] points)
        {
            string key = face.TextureKey;

            if (key == null)
                return;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(points);

                GraphicsState state = g.Save();

                try
                {
                    g.SetClip(path, CombineMode.Replace);

                    switch (key)
                    {
                        case "floor":
                            DrawFloorPattern(g, points);
                            break;

                        case "wall":
                            DrawWallPattern(g, points);
                            break;

                        case "bar":
                            DrawBarPattern(g, points);
                            break;

                        case "wood_dark":
                        case "mid_wood":
                            DrawWoodPattern(g, points);
                            break;

                        case "fridge":
                            DrawMetalPattern(g, points);
                            break;

                        case "screen":
                        case "black":
                            DrawScreenPattern(g, points);
                            break;
                    }
                }
                finally
                {
                    g.Restore(state);
                }
            }
        }

        private void DrawFloorPattern(Graphics g, PointF[] p)
        {
            DrawLinesU(g, p, Color.FromArgb(45, 80, 72, 60), 8);
            DrawLinesV(g, p, Color.FromArgb(35, 80, 72, 60), 6);
        }

        private void DrawWallPattern(Graphics g, PointF[] p)
        {
            DrawLinesV(g, p, Color.FromArgb(22, 120, 106, 86), 4);
            DrawLinesU(g, p, Color.FromArgb(18, 120, 106, 86), 3);
        }

        private void DrawBarPattern(Graphics g, PointF[] p)
        {
            DrawLinesU(g, p, Color.FromArgb(55, 85, 48, 24), 6);
            DrawLinesV(g, p, Color.FromArgb(28, 85, 48, 24), 3);
        }

        private void DrawWoodPattern(Graphics g, PointF[] p)
        {
            DrawLinesU(g, p, Color.FromArgb(55, 35, 20, 10), 5);
            DrawLinesV(g, p, Color.FromArgb(25, 35, 20, 10), 3);
        }

        private void DrawMetalPattern(Graphics g, PointF[] p)
        {
            DrawLinesV(g, p, Color.FromArgb(35, 120, 125, 130), 3);
        }

        private void DrawScreenPattern(Graphics g, PointF[] p)
        {
            using (Pen pen = new Pen(Color.FromArgb(90, 40, 90, 120), 1))
            {
                g.DrawPolygon(pen, p);
            }
        }

        private void DrawLinesU(Graphics g, PointF[] p, Color color, int count)
        {
            if (p == null || p.Length != 4 || count <= 1)
                return;

            using (Pen pen = new Pen(color, 1))
            {
                for (int i = 1; i < count; i++)
                {
                    float t = i / (float)count;

                    PointF a = Lerp(p[0], p[1], t);
                    PointF b = Lerp(p[3], p[2], t);

                    g.DrawLine(pen, a, b);
                }
            }
        }

        private void DrawLinesV(Graphics g, PointF[] p, Color color, int count)
        {
            if (p == null || p.Length != 4 || count <= 1)
                return;

            using (Pen pen = new Pen(color, 1))
            {
                for (int i = 1; i < count; i++)
                {
                    float t = i / (float)count;

                    PointF a = Lerp(p[0], p[3], t);
                    PointF b = Lerp(p[1], p[2], t);

                    g.DrawLine(pen, a, b);
                }
            }
        }

        private PointF Lerp(PointF a, PointF b, float t)
        {
            return new PointF(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        private void DrawSoftOutline(Graphics g, PointF[] points)
        {
            using (Pen pen = new Pen(Color.FromArgb(42, 0, 0, 0), 1))
            {
                g.DrawPolygon(pen, points);
            }
        }
    }
}