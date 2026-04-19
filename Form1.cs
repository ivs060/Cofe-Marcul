using System;
using System.Windows.Forms;
using игра_для_проги.Model;
using игра_для_проги.View;
using игра_для_проги.Controller;

namespace игра_для_проги
{
    public partial class Form1 : Form
    {
        private SceneModel _model;
        private SceneView _view;
        private MainController _controller;

        public Form1()
        {
            InitializeComponent();

            _model = new SceneModel();
            _view = new SceneView();
            _controller = new MainController(_model);

            _model.Changed += () => this.Invalidate();

            _controller.CreateTestScene();
            this.KeyDown += Form1_KeyDown;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _view.Render(e.Graphics, _model, this.ClientSize.Width, this.ClientSize.Height);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Пустой метод для связи с конструктором
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // W и S теперь двигают нас "вглубь" и "из глубины" сцены
            if (e.KeyCode == Keys.W) _controller.MoveAll(0, 0, -1);
            if (e.KeyCode == Keys.S) _controller.MoveAll(0, 0, 1);

            // A и D двигают влево/вправо
            if (e.KeyCode == Keys.A) _controller.MoveAll(-1, 0, 0);
            if (e.KeyCode == Keys.D) _controller.MoveAll(1, 0, 0);

            // Q и E крутят комнату
            if (e.KeyCode == Keys.Q) _controller.RotateAll(-5);
            if (e.KeyCode == Keys.E) _controller.RotateAll(5);
        }
    }
}