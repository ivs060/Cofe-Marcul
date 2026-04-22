using System;

namespace игра_для_проги.Model
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // --- ДОБАВЬ ИЛИ ОБНОВИ ЭТОТ МЕТОД ---
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