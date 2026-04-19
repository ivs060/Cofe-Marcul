using System;

namespace игра_для_проги.Model
{
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        // Магия тригонометрии для поворота точки
        public void RotateY(double angle)
        {
            double rad = angle * Math.PI / 180;
            double oldX = X;
            double oldZ = Z;
            X = oldX * Math.Cos(rad) - oldZ * Math.Sin(rad);
            Z = oldX * Math.Sin(rad) + oldZ * Math.Cos(rad);
        }
    }
}