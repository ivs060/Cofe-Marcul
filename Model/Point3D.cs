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
            X = x;
            Y = y;
            Z = z;
        }

        public void RotateY(double angle)
        {
            // Математика поворота вокруг вертикальной оси (Y)
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            double oldX = X;
            double oldZ = Z;

            // Формула изменения координат X и Z при вращении
            X = oldX * cos + oldZ * sin;
            Z = -oldX * sin + oldZ * cos;
        }
    }
}