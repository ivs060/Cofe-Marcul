using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using игра_для_проги.Controller;
using игра_для_проги.Model;
using игра_для_проги.View;

namespace игра_для_проги
{
    public partial class Form1 : Form
    {
        private SceneModel _model;
        private MainController _controller;
        private SceneView _view = new SceneView();

        private bool _isMouseDown = false;
        private System.Drawing.Point _lastMousePos;

        private HashSet<Keys> _pressedKeys = new HashSet<Keys>();
        private Timer _gameTimer = new Timer();
        private Stopwatch _frameClock = new Stopwatch();

        // Скорость движения камеры в условных единицах в секунду.
        // Если медленно — ставь 320.
        // Если быстро — ставь 180 или 200.
        private const double MovementSpeed = 320.0;

        public Form1()
        {
            InitializeComponent();

            KeyPreview = true;
            DoubleBuffered = true;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true
            );

            _model = new SceneModel();
            _controller = new MainController(_model);

            _model.SceneChanged += (s, e) => Invalidate();

            _controller.CreateTestScene();

            // KeyDown уже подключен в Form1.Designer.cs.
            // KeyUp там нет, поэтому подключаем здесь.
            this.KeyUp += Form1_KeyUp;

            // Для GDI+ лучше 30 FPS, чем пытаться насильно держать 60 FPS.
            // Так меньше дерганий и меньше нагрузка на WinForms.
            _gameTimer.Interval = 40;
            _gameTimer.Tick += GameTimer_Tick;

            _frameClock.Start();
            _gameTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Заглушка для дизайнера Visual Studio.
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
                int dy = e.Y - _lastMousePos.Y;

                double sensitivity = 0.004;

                // dx — поворот влево/вправо
                // -dy — поворот вверх/вниз.
                // Минус нужен, чтобы движение мыши вверх поднимало взгляд вверх.
                _controller.RotateCamera(
                    dx * sensitivity,
                    -dy * sensitivity
                );

                _lastMousePos = e.Location;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                _controller.RespawnCamera();
                return;
            }

            _pressedKeys.Add(e.KeyCode);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            double deltaTime = _frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();

            if (deltaTime <= 0 || deltaTime > 0.1)
                deltaTime = 0.033;

            double localDx = 0;
            double localDy = 0;
            double localDz = 0;

            if (_pressedKeys.Contains(Keys.W))
                localDz += 1;

            if (_pressedKeys.Contains(Keys.S))
                localDz -= 1;

            if (_pressedKeys.Contains(Keys.A))
                localDx -= 1;

            if (_pressedKeys.Contains(Keys.D))
                localDx += 1;

            // На финальной версии лучше не давать летать.
            // Поэтому Space и Ctrl пока не используем.

            if (localDx == 0 && localDy == 0 && localDz == 0)
                return;

            double length = Math.Sqrt(
                localDx * localDx +
                localDy * localDy +
                localDz * localDz
            );

            localDx /= length;
            localDy /= length;
            localDz /= length;

            double distance = MovementSpeed * deltaTime;

            _controller.MoveCamera(
                localDx * distance,
                localDy * distance,
                localDz * distance
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            g.Clear(Color.Black);

            g.SmoothingMode = SmoothingMode.None;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.InterpolationMode = InterpolationMode.Low;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;

            _view.Render(g, _model, _controller.Camera, this.ClientSize.Width, this.ClientSize.Height);
        }
    }
}