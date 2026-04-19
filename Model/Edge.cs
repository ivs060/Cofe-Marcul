namespace игра_для_проги.Model
{
    // Класс для линии (ребра), которая соединяет две точки
    public class Edge
    {
        public Point3D Start { get; set; }
        public Point3D End { get; set; }

        public Edge(Point3D start, Point3D end)
        {
            Start = start;
            End = end;
        }
    }
}