using System;
using игра_для_проги.Model;

namespace игра_для_проги.Model
{
    public class Edge
    {
        // Указываем тип Point3D вместо Point
        public Point3D P1 { get; set; }
        public Point3D P2 { get; set; }

        public Edge(Point3D p1, Point3D p2)
        {
            P1 = p1;
            P2 = p2;
        }
    }
}