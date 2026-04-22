using System;
using System.Drawing;
using System.Windows.Forms;
using игра_для_проги.Controller;
using игра_для_проги.Model;
using игра_для_проги.View;

// Создаем псевдоним, чтобы не писать длинные имена
using Point3D = игра_для_проги.Model.Point;

namespace игра_для_проги
{
    public partial class Form1 : Form
    {
        private SceneModel _model;
        private MainController _controller;
        private SceneView _view = new SceneView();

        private bool _isMouseDown = false;
        // Теперь это точно точка Windows (System.Drawing.Point)
        private System.Drawing.Point _lastMousePos;

        public Form1()
        {
            InitializeComponent();

            _model = new SceneModel();
            _controller = new MainController(_model);

            _model.SceneChanged += (s, e) => this.Invalidate();
            _controller.CreateTestScene();
            _controller.MoveAll(0, 0, 50);

            this.DoubleBuffered = true;

            // Важно: подписываемся на события здесь, если форма их не «видит»
            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;
            this.MouseUp += Form1_MouseUp;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Оставь это место пустым. 
            // Это просто "заглушка", чтобы дизайнер не выдавал ошибку.
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
                _lastMousePos = e.Location;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                int dx = e.X - _lastMousePos.X;
                _controller.RotateAll(dx * 0.005);
                _lastMousePos = e.Location;
                // Мы не вызываем Invalidate здесь вручную, 
                // так как это делает событие SceneChanged выше
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 1. Вызываем базовый метод (важно для корректной работы Windows)
            base.OnPaint(e);

            // 2. Получаем объект для рисования
            Graphics g = e.Graphics;

            // 3. Закрашиваем фон черным (стиль хоррор-игр)
            g.Clear(Color.Black);

            // 4. Включаем сглаживание, чтобы линии не были «лесенкой»
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 5. ГЛАВНОЕ: передаем управление нашему 3D-движку
            // Мы отдаем ему холст (g), данные (model) и размеры окна
            _view.Render(g, _model, this.ClientSize.Width, this.ClientSize.Height);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Это стандартный шаг перемещения
            double step = 10.0;

            switch (e.KeyCode)
            {
                case Keys.W:
                    _controller.MoveAll(0, 0, -step);
                    break;
                case Keys.S:
                    _controller.MoveAll(0, 0, step);
                    break;
                case Keys.A:
                    _controller.MoveAll(step, 0, 0);
                    break;
                case Keys.D:
                    _controller.MoveAll(-step, 0, 0);
                    break;
                case Keys.Space:
                    _controller.MoveAll(0, 1, 0);
                    break;
                case Keys.ControlKey:
                    _controller.MoveAll(0, -1, 0);
                    break;
            }
        }
    }
}