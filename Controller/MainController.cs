using игра_для_проги.Model;

namespace игра_для_проги.Controller
{
    public class MainController
    {
        private SceneModel _model;
        private double _speed = 10.0; // Скорость движения

        public MainController(SceneModel model)
        {
            _model = model;
        }

        public void CreateTestScene()
        {
            // Очистим старое, если было
            _model.Points.Clear();
            _model.Edges.Clear();

            // Создаем 8 углов комнаты (X, Y, Z)
            // Пол (Y = -100)
            Point3D p1 = new Point3D(-200, -100, -200); // Левый ближний
            Point3D p2 = new Point3D(200, -100, -200);  // Правый ближний
            Point3D p3 = new Point3D(200, -100, 200);   // Правый дальний
            Point3D p4 = new Point3D(-200, -100, 200);  // Левый дальний

            // Потолок (Y = 100)
            Point3D p5 = new Point3D(-200, 100, -200);
            Point3D p6 = new Point3D(200, 100, -200);
            Point3D p7 = new Point3D(200, 100, 200);
            Point3D p8 = new Point3D(-200, 100, 200);

            // Добавляем точки в модель
            _model.AddPoint(p1.X, p1.Y, p1.Z); _model.AddPoint(p2.X, p2.Y, p2.Z);
            _model.AddPoint(p3.X, p3.Y, p3.Z); _model.AddPoint(p4.X, p4.Y, p4.Z);
            _model.AddPoint(p5.X, p5.Y, p5.Z); _model.AddPoint(p6.X, p6.Y, p6.Z);
            _model.AddPoint(p7.X, p7.Y, p7.Z); _model.AddPoint(p8.X, p8.Y, p8.Z);

            // Соединяем линии пола
            _model.AddEdge(_model.Points[0], _model.Points[1]);
            _model.AddEdge(_model.Points[1], _model.Points[2]);
            _model.AddEdge(_model.Points[2], _model.Points[3]);
            _model.AddEdge(_model.Points[3], _model.Points[0]);

            // Соединяем линии потолка
            _model.AddEdge(_model.Points[4], _model.Points[5]);
            _model.AddEdge(_model.Points[5], _model.Points[6]);
            _model.AddEdge(_model.Points[6], _model.Points[7]);
            _model.AddEdge(_model.Points[7], _model.Points[4]);

            // Вертикальные линии (стены)
            _model.AddEdge(_model.Points[0], _model.Points[4]);
            _model.AddEdge(_model.Points[1], _model.Points[5]);
            _model.AddEdge(_model.Points[2], _model.Points[6]);
            _model.AddEdge(_model.Points[3], _model.Points[7]);

        }

        // Метод для движения "камеры" (на самом деле двигаем все точки мира)
        public void MoveAll(double dx, double dy, double dz)
        {
            // Гипотетические границы нашей комнаты
            double limitX = 200;
            double limitZ = 200;

            foreach (var p in _model.Points)
            {
                // Если мы нажмем W и это заставит точки сдвинуться так, 
                // что мы окажемся "снаружи" — мы просто не будем менять координаты.
                // Но пока у нас "летающая камера", давай просто ограничим движение.
                p.X += dx * _speed;
                p.Y += dy * _speed;
                p.Z += dz * _speed;
            }
            _model.NotifyChanged();
        }
        public void RotateAll(double angle)
        {
            foreach (var p in _model.Points)
            {
                p.RotateY(angle);
            }
            _model.NotifyChanged();
        }
    }
}