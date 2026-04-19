using System;
using System.Drawing;
using игра_для_проги.Model;

namespace игра_для_проги.View
{
    public class SceneView
    {
        public void Render(Graphics g, SceneModel model, int width, int height)
        {
            double fov = 500;

            foreach (var edge in model.Edges)
            {
                double z1 = edge.Start.Z + 400;
                double z2 = edge.End.Z + 400;

                if (z1 < 20 || z2 < 20) continue;

                double avgZ = (z1 + z2) / 2.0;
                int brightness = (int)(15000 / avgZ);

                if (brightness < 0) brightness = 0;
                if (brightness > 255) brightness = 255;

                using (Pen fogPen = new Pen(Color.FromArgb(brightness, brightness, brightness), 2))
                {
                    float x1 = (float)(edge.Start.X * (fov / z1) + width / 2);
                    float y1 = (float)(-edge.Start.Y * (fov / z1) + height / 2);
                    float x2 = (float)(edge.End.X * (fov / z2) + width / 2);
                    float y2 = (float)(-edge.End.Y * (fov / z2) + height / 2);

                    if (Math.Abs(x1) < 5000 && Math.Abs(y1) < 5000 && Math.Abs(x2) < 5000 && Math.Abs(y2) < 5000)
                    {
                        g.DrawLine(fogPen, x1, y1, x2, y2);
                    }
                }
            }
        }
    }
}