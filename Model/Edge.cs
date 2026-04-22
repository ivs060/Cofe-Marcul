using игра_для_проги.Model;

namespace игра_для_проги.Model
{
    public class Edge
    {
        // Используем Point вместо Point3D
        public Point P1 { get; set; }
        public Point P2 { get; set; }

        public Edge(Point p1, Point p2)
        {
            P1 = p1;
            P2 = p2;
        }
    }
}