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
        private HorrorGameController _game;
        private SceneView _view = new SceneView();

        private bool _isMouseDown = false;
        private bool _ignoreNextMouseMove = false;
        private bool _mouseLookActive = false;
        private System.Drawing.Point _lastMousePos;

        private bool _settingsOpen = false;
        private int _draggedSettingsSlider = 0; // 0 = нет, 1 = чувствительность, 2 = громкость
        private double _mouseSensitivity = 0.002558923636;
        private double _volume = 1.0;
        private bool _cursorHidden = false;

        private bool _isFullscreen = false;
        private FormBorderStyle _previousBorderStyle;
        private FormWindowState _previousWindowState;
        private Rectangle _previousBounds;

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

            // Логика игры создаётся после сборки сцены и появления камеры.
            _game = new HorrorGameController(
                _controller.Camera,
                _controller.SetLightSwitchOn,
                _controller.SetDirtyCupsOnTable1Visible,
                _controller.SetDirtyCupsOnTable2Visible,
                _controller.SetCoffeeStainsOnTable1Visible,
                _controller.SetCoffeeStainsOnTable2Visible,
                _controller.SetReturnedShelfCupsVisible,
                _controller.SetTvScreenOn,
                _controller.SetCashRecipeScreenVisible,
                _controller.SetClientVisible,
                _controller.SetClientTransform
            );

            // KeyDown уже подключен в Form1.Designer.cs.
            // KeyUp там нет, поэтому подключаем здесь.
            this.KeyUp += Form1_KeyUp;
            this.MouseEnter += Form1_MouseEnter;
            this.MouseLeave += Form1_MouseLeave;

            // Для GDI+ лучше 30 FPS, чем пытаться насильно держать 60 FPS.
            // Так меньше дерганий и меньше нагрузка на WinForms.
            _gameTimer.Interval = 16;
            _gameTimer.Tick += GameTimer_Tick;

            _frameClock.Start();
            _gameTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Заглушка для дизайнера Visual Studio.
        }

        private void HideGameCursor()
        {
            if (_cursorHidden)
                return;

            Cursor.Hide();
            _cursorHidden = true;
        }

        private void ShowGameCursor()
        {
            if (!_cursorHidden)
                return;

            Cursor.Show();
            _cursorHidden = false;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();

            if (_settingsOpen)
            {
                HandleSettingsMouseDown(e.Location);
                return;
            }

            _mouseLookActive = true;
            HideGameCursor();
            CenterMouseForStableLook();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_settingsOpen)
            {
                HandleSettingsMouseMove(e.Location);
                return;
            }

            // Камеру здесь не крутим, чтобы не было дрожания от лишних MouseMove.
            if (_ignoreNextMouseMove)
                _ignoreNextMouseMove = false;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
            _draggedSettingsSlider = 0;
        }

        private void Form1_MouseEnter(object sender, EventArgs e)
        {
            this.Focus();

            if (_settingsOpen)
            {
                _mouseLookActive = false;
                ShowGameCursor();
                return;
            }

            _mouseLookActive = true;
            HideGameCursor();
            CenterMouseForStableLook();
        }

        private void Form1_MouseLeave(object sender, EventArgs e)
        {
            _mouseLookActive = false;

            if (!_settingsOpen)
                ShowGameCursor();
        }

        private void CenterMouseForStableLook()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            System.Drawing.Point center = new System.Drawing.Point(
                ClientSize.Width / 2,
                ClientSize.Height / 2
            );

            _ignoreNextMouseMove = true;
            Cursor.Position = PointToScreen(center);
            _lastMousePos = center;
        }

        private void UpdateMouseLook()
        {
            if (_settingsOpen)
                return;

            if (!_mouseLookActive)
                return;

            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            System.Drawing.Point center = new System.Drawing.Point(
                ClientSize.Width / 2,
                ClientSize.Height / 2
            );

            System.Drawing.Point mouse = PointToClient(Cursor.Position);

            int dx = mouse.X - center.X;
            int dy = mouse.Y - center.Y;

            // Мёртвая зона убирает мелкую дрожь тачпада/мыши.
            if (Math.Abs(dx) <= 2 && Math.Abs(dy) <= 2)
                return;

            if (dx > 70) dx = 70;
            if (dx < -70) dx = -70;
            if (dy > 70) dy = 70;
            if (dy < -70) dy = -70;

            double sensitivity = _mouseSensitivity;

            _controller.RotateCamera(
                dx * sensitivity,
                -dy * sensitivity
            );

            CenterMouseForStableLook();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                ToggleFullscreen();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Tab)
            {
                ToggleSettingsPanel();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (_settingsOpen)
                return;

            if (_game != null && _game.IsChoiceActive)
            {
                if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                {
                    _game.SelectDialogueChoice(1);
                    Invalidate();
                    return;
                }

                if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
                {
                    _game.SelectDialogueChoice(2);
                    Invalidate();
                    return;
                }
            }

            if (e.KeyCode == Keys.R)
            {
                _controller.RespawnCamera();
                return;
            }

            if (e.KeyCode == Keys.E)
            {
                _game.Interact();
                Invalidate();
                return;
            }

            _pressedKeys.Add(e.KeyCode);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);
        }

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _previousBorderStyle = FormBorderStyle;
                _previousWindowState = WindowState;
                _previousBounds = Bounds;

                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromControl(this).Bounds;
                TopMost = true;

                _isFullscreen = true;
            }
            else
            {
                TopMost = false;
                FormBorderStyle = _previousBorderStyle;
                WindowState = FormWindowState.Normal;
                Bounds = _previousBounds;
                WindowState = _previousWindowState;

                _isFullscreen = false;
            }

            if (_settingsOpen)
            {
                _mouseLookActive = false;
                ShowGameCursor();
            }
            else
            {
                _mouseLookActive = true;
                HideGameCursor();
                CenterMouseForStableLook();
            }

            Invalidate();
        }

        private void MoveCursorToSettingsCenter()
        {
            Rectangle panel = GetSettingsPanelBounds();

            System.Drawing.Point center = new System.Drawing.Point(
                panel.X + panel.Width / 2,
                panel.Y + panel.Height / 2
            );

            this.Activate();
            this.Focus();
            Cursor.Position = PointToScreen(center);
        }

        private void ToggleSettingsPanel()
        {
            _settingsOpen = !_settingsOpen;
            _draggedSettingsSlider = 0;

            if (_settingsOpen)
            {
                _mouseLookActive = false;
                _pressedKeys.Clear();

                ShowGameCursor();
                MoveCursorToSettingsCenter();
            }
            else
            {
                _mouseLookActive = true;
                HideGameCursor();
                CenterMouseForStableLook();
            }

            Invalidate();
        }

        private Rectangle GetSettingsPanelBounds()
        {
            int width = 430;
            int height = 250;
            int x = (ClientSize.Width - width) / 2;
            int y = (ClientSize.Height - height) / 2;
            return new Rectangle(x, y, width, height);
        }

        private Rectangle GetSensitivitySliderBounds()
        {
            Rectangle panel = GetSettingsPanelBounds();
            return new Rectangle(panel.X + 55, panel.Y + 105, panel.Width - 110, 18);
        }

        private Rectangle GetVolumeSliderBounds()
        {
            Rectangle panel = GetSettingsPanelBounds();
            return new Rectangle(panel.X + 55, panel.Y + 175, panel.Width - 110, 18);
        }

        private void HandleSettingsMouseDown(System.Drawing.Point location)
        {
            if (IsPointNearSlider(location, GetSensitivitySliderBounds()))
            {
                _draggedSettingsSlider = 1;
                SetSensitivityFromMouse(location.X);
                return;
            }

            if (IsPointNearSlider(location, GetVolumeSliderBounds()))
            {
                _draggedSettingsSlider = 2;
                SetVolumeFromMouse(location.X);
                return;
            }
        }

        private void HandleSettingsMouseMove(System.Drawing.Point location)
        {
            if (_draggedSettingsSlider == 1)
            {
                SetSensitivityFromMouse(location.X);
                return;
            }

            if (_draggedSettingsSlider == 2)
            {
                SetVolumeFromMouse(location.X);
                return;
            }
        }

        private bool IsPointNearSlider(System.Drawing.Point point, Rectangle slider)
        {
            Rectangle hitBox = slider;
            hitBox.Inflate(12, 18);
            return hitBox.Contains(point);
        }

        private void SetSensitivityFromMouse(int mouseX)
        {
            Rectangle slider = GetSensitivitySliderBounds();
            double t = (mouseX - slider.Left) / (double)slider.Width;
            t = Clamp01(t);

            // Проценты от 1 до 100, без значения 0.
            int percent = 1 + (int)Math.Round(t * 99.0);

            // Пропорциональная чувствительность:
            // 25% = в 2 раза меньше, чем 50%;
            // 50% = базовое значение;
            // 75% = в 1.5 раза больше, чем 50%;
            // 1% почти не поворачивает камеру.
            _mouseSensitivity = 0.002558923636 * (percent / 50.0);

            Invalidate();
        }

        private void SetVolumeFromMouse(int mouseX)
        {
            Rectangle slider = GetVolumeSliderBounds();
            double t = (mouseX - slider.Left) / (double)slider.Width;
            _volume = Clamp01(t);
            Invalidate();
        }

        private double Clamp01(double value)
        {
            if (value < 0)
                return 0;

            if (value > 1)
                return 1;

            return value;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            double deltaTime = _frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();

            if (deltaTime <= 0 || deltaTime > 0.1)
                deltaTime = 0.033;

            // Пока открыты настройки, игровой процесс полностью заморожен:
            // не двигаем камеру, не обновляем игровые таймеры,
            // не двигаем клиентку, не анимируем телевизор.
            if (_settingsOpen)
                return;

            UpdateMouseLook();

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
            {
                _game.Update(deltaTime);
                _controller.AnimateTvScreen(deltaTime);
                Invalidate();
                return;
            }

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

            _game.Update(deltaTime);
            _controller.AnimateTvScreen(deltaTime);
            Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ShowGameCursor();
            base.OnFormClosed(e);
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

            DrawGameEffects(g);
            DrawGameUi(g);
        }

        private void DrawGameEffects(Graphics g)
        {
            if (_game == null)
                return;

            // Простая визуальная заглушка под мигание света.
            // Потом можно заменить на изменение цветов/объектов в сцене.
            if (!_game.LightOn)
            {
                // Первоначальная темнота стала значительно сильнее.
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(185, 0, 0, 0)))
                    g.FillRectangle(brush, 0, 0, ClientSize.Width, ClientSize.Height);
            }
            else if (_game.IsLightFlickering)
            {
                // При включённом, но мигающем свете комната прыгает
                // между нынешней тёмной атмосферой и более сильной темнотой.
                int alpha = (Environment.TickCount / 120) % 2 == 0 ? 105 : 165;
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    g.FillRectangle(brush, 0, 0, ClientSize.Width, ClientSize.Height);
            }
            else
            {
                // Включённый свет теперь выглядит примерно как прежнее состояние темноты.
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(105, 0, 0, 0)))
                    g.FillRectangle(brush, 0, 0, ClientSize.Width, ClientSize.Height);
            }

            DrawBartenderLampLight(g);

            if (_game.ScreenFlash)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(95, 120, 0, 0)))
                    g.FillRectangle(brush, 0, 0, ClientSize.Width, ClientSize.Height);
            }
        }

        private void DrawBartenderLampLight(Graphics g)
        {
            if (_game == null || !_game.LightOn)
                return;

            // Отдельный тёплый свет от подвесной лампы.
            // Он рисуется поверх общей темноты, чтобы рабочая зона бармена
            // была чуть заметнее, чем остальная кофейня.
            PointF bulb;
            PointF leftWork;
            PointF rightWork;
            PointF farWork;
            PointF nearWork;

            if (!ProjectWorldPoint(20, 68, 420, out bulb))
                return;

            if (!ProjectWorldPoint(-95, -34, 382, out leftWork))
                return;

            if (!ProjectWorldPoint(255, -34, 382, out rightWork))
                return;

            if (!ProjectWorldPoint(255, -34, 448, out farWork))
                return;

            if (!ProjectWorldPoint(-95, -34, 448, out nearWork))
                return;

            using (SolidBrush beam = new SolidBrush(Color.FromArgb(10, 248, 218, 145)))
            using (SolidBrush spot = new SolidBrush(Color.FromArgb(14, 255, 228, 160)))
            {
                PointF[] cone = new PointF[]
                {
                    bulb,
                    rightWork,
                    farWork,
                    nearWork,
                    leftWork
                };

                g.FillPolygon(beam, cone);

                PointF[] workArea = new PointF[]
                {
                    leftWork,
                    rightWork,
                    farWork,
                    nearWork
                };

                g.FillPolygon(spot, workArea);
            }
        }

        private bool ProjectWorldPoint(double worldX, double worldY, double worldZ, out PointF screenPoint)
        {
            screenPoint = PointF.Empty;

            if (_controller == null || _controller.Camera == null)
                return false;

            Camera3D camera = _controller.Camera;

            double dx = worldX - camera.X;
            double dy = worldY - camera.Y;
            double dz = worldZ - camera.Z;

            double cosYaw = Math.Cos(camera.Yaw);
            double sinYaw = Math.Sin(camera.Yaw);
            double cosPitch = Math.Cos(camera.Pitch);
            double sinPitch = Math.Sin(camera.Pitch);

            double viewX = dx * cosYaw - dz * sinYaw;
            double viewZ = dx * sinYaw + dz * cosYaw;
            double viewY = dy;

            double pitchedY = viewY * cosPitch - viewZ * sinPitch;
            double pitchedZ = viewY * sinPitch + viewZ * cosPitch;

            if (pitchedZ < 0.7)
                return false;

            const double projectionScale = 500.0;

            screenPoint = new PointF(
                (float)(ClientSize.Width / 2.0 + viewX * (projectionScale / pitchedZ)),
                (float)(ClientSize.Height / 2.0 - pitchedY * (projectionScale / pitchedZ))
            );

            return true;
        }

        private void DrawGameUi(Graphics g)
        {
            if (_game == null)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawObjectives(g);
            DrawKeyHints(g);
            DrawPrompt(g);
            DrawBottomText(g);
            DrawCenterText(g);
            DrawDialogueChoice(g);
            DrawRecipeOverlay(g);
            DrawSettingsPanel(g);
        }

        private void DrawObjectives(Graphics g)
        {
            int x = 18;
            int y = 18;
            int width = 280;
            int lineHeight = 22;

            int height = 46 + _game.Objectives.Count * lineHeight;

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(75, 0, 0, 0)))
                g.FillRectangle(bg, x, y, width, height);

            using (Font titleFont = new Font("Arial", 10, FontStyle.Bold))
            using (Font textFont = new Font("Arial", 10, FontStyle.Regular))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(230, 230, 230)))
            using (SolidBrush gray = new SolidBrush(Color.FromArgb(145, 145, 145)))
            using (SolidBrush green = new SolidBrush(Color.FromArgb(70, 220, 105)))
            {
                g.DrawString("День " + _game.CurrentDay + " — цели", titleFont, white, x + 12, y + 10);

                for (int i = 0; i < _game.Objectives.Count; i++)
                {
                    GameObjective objective = _game.Objectives[i];
                    string mark = objective.IsCompleted ? "✓ " : "[ ] ";
                    SolidBrush markBrush = objective.IsCompleted ? green : white;
                    SolidBrush textBrush = objective.IsCompleted ? gray : white;

                    float lineY = y + 36 + i * lineHeight;
                    g.DrawString(mark, textFont, markBrush, x + 12, lineY);
                    SizeF markSize = g.MeasureString(mark, textFont);
                    g.DrawString(objective.Text, textFont, textBrush, x + 12 + markSize.Width - 2, lineY);
                }
            }
        }

        private void DrawPrompt(Graphics g)
        {
            if (string.IsNullOrWhiteSpace(_game.PromptText))
                return;

            using (Font font = new Font("Arial", 11, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(235, 235, 210)))
            {
                SizeF size = g.MeasureString(_game.PromptText, font);
                float x = (ClientSize.Width - size.Width) / 2f;
                float y = ClientSize.Height - 255;
                DrawTextBackplate(g, x - 12, y - 7, size.Width + 24, size.Height + 14);
                g.DrawString(_game.PromptText, font, brush, x, y);
            }
        }

        private void DrawBottomText(Graphics g)
        {
            if (string.IsNullOrWhiteSpace(_game.BottomText))
                return;

            using (Font font = new Font("Arial", 24, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(235, 235, 235)))
            {
                string text = _game.BottomText;
                SizeF size = g.MeasureString(text, font);
                float y = ClientSize.Height - 155;

                bool isClientText = text.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase);
                bool isPlayerAnswer =
                    text == "Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?" ||
                    text == "Ага...Чего желаете?" ||
                    text == "Заказ принят, к оплате будет 300 рублей" ||
                    text == "с вас 300р.";

                float x;

                if (isClientText)
                    x = 42;
                else if (isPlayerAnswer)
                    x = ClientSize.Width - size.Width - 42;
                else
                    x = (ClientSize.Width - size.Width) / 2f;

                DrawTextBackplate(g, x - 28, y - 16, size.Width + 56, size.Height + 32);
                g.DrawString(text, font, brush, x, y);
            }
        }

        private void DrawCenterText(Graphics g)
        {
            if (string.IsNullOrWhiteSpace(_game.CenterText))
                return;

            using (Font font = new Font("Arial", 40, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(245, 230, 230)))
            {
                SizeF size = g.MeasureString(_game.CenterText, font);
                float x = (ClientSize.Width - size.Width) / 2f;
                float y = (ClientSize.Height - size.Height) / 2f - 55;
                DrawTextBackplate(g, x - 36, y - 24, size.Width + 72, size.Height + 48);
                g.DrawString(_game.CenterText, font, brush, x, y);
            }
        }

        private void DrawKeyHints(Graphics g)
        {
            string line1 = "Tab - настройки";
            string line2 = "E - взаимодействие с объектом";
            string line3 = "Esc - полноэкранный режим";

            using (Font font = new Font("Arial", 11, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(230, 230, 220)))
            {
                SizeF size1 = g.MeasureString(line1, font);
                SizeF size2 = g.MeasureString(line2, font);
                SizeF size3 = g.MeasureString(line3, font);

                float width = Math.Max(size1.Width, Math.Max(size2.Width, size3.Width));
                float height = size1.Height + size2.Height + size3.Height + 16;

                float x = ClientSize.Width - width - 28;
                float y = 18;

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                    g.FillRectangle(bg, x - 10, y - 8, width + 20, height + 16);

                g.DrawString(line1, font, textBrush, x, y);
                g.DrawString(line2, font, textBrush, x, y + size1.Height + 4);
                g.DrawString(line3, font, textBrush, x, y + size1.Height + size2.Height + 8);
            }
        }

        private void DrawDialogueChoice(Graphics g)
        {
            if (_game == null || !_game.IsChoiceActive)
                return;

            string title = "Выберите ответ:";
            string option1 = "1. " + _game.ChoiceOption1;
            string option2 = "2. " + _game.ChoiceOption2;

            int panelWidth = Math.Min(940, ClientSize.Width - 80);
            int panelHeight = 215;
            int x = (ClientSize.Width - panelWidth) / 2;
            int y = ClientSize.Height - panelHeight - 70;

            Rectangle panel = new Rectangle(x, y, panelWidth, panelHeight);
            Rectangle option1Box = new Rectangle(x + 24, y + 62, panelWidth - 48, 58);
            Rectangle option2Box = new Rectangle(x + 24, y + 135, panelWidth - 48, 58);

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (SolidBrush optionBg = new SolidBrush(Color.FromArgb(115, 28, 22, 18)))
            using (Pen border = new Pen(Color.FromArgb(170, 230, 210, 160), 2))
            using (Pen optionBorder = new Pen(Color.FromArgb(135, 230, 210, 160), 1))
            using (Font titleFont = new Font("Arial", 15, FontStyle.Bold))
            using (Font optionFont = new Font("Arial", 12, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(245, 235, 210)))
            using (SolidBrush optionBrush = new SolidBrush(Color.FromArgb(235, 235, 225)))
            {
                g.FillRectangle(bg, panel);
                g.DrawRectangle(border, panel);

                g.DrawString(title, titleFont, titleBrush, x + 24, y + 18);

                g.FillRectangle(optionBg, option1Box);
                g.DrawRectangle(optionBorder, option1Box);
                g.FillRectangle(optionBg, option2Box);
                g.DrawRectangle(optionBorder, option2Box);

                RectangleF option1TextRect = new RectangleF(option1Box.X + 16, option1Box.Y + 12, option1Box.Width - 32, option1Box.Height - 18);
                RectangleF option2TextRect = new RectangleF(option2Box.X + 16, option2Box.Y + 12, option2Box.Width - 32, option2Box.Height - 18);

                g.DrawString(option1, optionFont, optionBrush, option1TextRect);
                g.DrawString(option2, optionFont, optionBrush, option2TextRect);
            }
        }

        private void DrawRecipeOverlay(Graphics g)
        {
            if (_game == null || !_game.RecipeOverlayVisible)
                return;

            int marginX = 65;
            int marginY = 55;
            Rectangle panel = new Rectangle(
                marginX,
                marginY,
                ClientSize.Width - marginX * 2,
                ClientSize.Height - marginY * 2
            );

            Rectangle imageArea = new Rectangle(
                panel.X + 110,
                panel.Y + 150,
                270,
                355
            );

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(255, 105, 195, 230)))
            using (Pen border = new Pen(Color.Black, 7))
            using (Pen cupLinePen = new Pen(Color.Black, 7))
            using (Font titleFont = new Font("Arial", 30, FontStyle.Bold))
            using (Font subTitleFont = new Font("Arial", 18, FontStyle.Bold))
            using (Font ingredientFont = new Font("Arial", 23, FontStyle.Bold))
            using (Font closeFont = new Font("Arial", 19, FontStyle.Bold))
            using (SolidBrush black = new SolidBrush(Color.Black))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(248, 244, 238)))
            using (SolidBrush lidBrush = new SolidBrush(Color.FromArgb(222, 228, 232)))
            using (SolidBrush glassBrush = new SolidBrush(Color.FromArgb(210, 250, 245, 240)))
            using (SolidBrush raspberryBrush = new SolidBrush(Color.FromArgb(202, 54, 106)))
            using (SolidBrush milkBrush = new SolidBrush(Color.FromArgb(245, 228, 211)))
            using (SolidBrush coffeeBrush = new SolidBrush(Color.FromArgb(119, 73, 47)))
            using (SolidBrush strawBrush = new SolidBrush(Color.FromArgb(244, 108, 154)))
            using (SolidBrush shineBrush = new SolidBrush(Color.FromArgb(155, 255, 255, 255)))
            {
                g.FillRectangle(bg, panel);
                g.DrawRectangle(border, panel);

                g.DrawString("малиновый латте", titleFont, black, panel.X + 36, panel.Y + 28);
                g.DrawString("рецепт заказа", subTitleFont, black, panel.X + 40, panel.Y + 78);

                float ingredientsX = imageArea.Right + 130;
                float ingredientsY = panel.Y + 172;
                g.DrawString("ингредиенты:", ingredientFont, black, ingredientsX, ingredientsY);
                g.DrawString("кофе x 1", ingredientFont, black, ingredientsX, ingredientsY + 72);
                g.DrawString("молоко x 3", ingredientFont, black, ingredientsX, ingredientsY + 132);
                g.DrawString("сироп малины", ingredientFont, black, ingredientsX, ingredientsY + 192);

                int cupTopY = imageArea.Y + 42;
                int cupBottomY = imageArea.Bottom - 32;
                int cupLeftTop = imageArea.X + 68;
                int cupRightTop = imageArea.Right - 68;
                int cupLeftBottom = imageArea.X + 92;
                int cupRightBottom = imageArea.Right - 92;

                Point[] cupBody = new Point[]
                {
                    new Point(cupLeftTop, cupTopY),
                    new Point(cupRightTop, cupTopY),
                    new Point(cupRightBottom, cupBottomY),
                    new Point(cupLeftBottom, cupBottomY)
                };

                // крышка
                g.FillEllipse(lidBrush, imageArea.X + 40, imageArea.Y + 8, imageArea.Width - 80, 44);
                g.DrawEllipse(cupLinePen, imageArea.X + 40, imageArea.Y + 8, imageArea.Width - 80, 44);
                g.FillRectangle(lidBrush, imageArea.X + 64, imageArea.Y + 24, imageArea.Width - 128, 22);
                g.DrawRectangle(cupLinePen, imageArea.X + 64, imageArea.Y + 24, imageArea.Width - 128, 22);

                // трубочка
                g.FillRectangle(strawBrush, imageArea.X + 160, imageArea.Y - 2, 12, 108);
                g.DrawRectangle(cupLinePen, imageArea.X + 160, imageArea.Y - 2, 12, 108);

                // стеклянный стакан
                g.FillPolygon(glassBrush, cupBody);
                g.DrawPolygon(cupLinePen, cupBody);

                int innerLeftTop = cupLeftTop + 10;
                int innerRightTop = cupRightTop - 10;
                int innerLeftBottom = cupLeftBottom + 10;
                int innerRightBottom = cupRightBottom - 10;

                // Малиновый сироп сверху — оставляем как есть.
                Point[] raspberryTop = new Point[]
                {
                    new Point(innerLeftTop, cupTopY + 22),
                    new Point(innerRightTop, cupTopY + 22),
                    new Point(innerRightTop - 6, cupTopY + 92),
                    new Point(innerLeftTop + 6, cupTopY + 92)
                };
                g.FillPolygon(raspberryBrush, raspberryTop);

                // Кофе — меньше, примерно 1 часть.
                Point[] coffeeMiddle = new Point[]
                {
                    new Point(innerLeftTop + 5, cupTopY + 95),
                    new Point(innerRightTop - 5, cupTopY + 95),
                    new Point(innerRightBottom - 8, cupTopY + 147),
                    new Point(innerLeftBottom + 8, cupTopY + 147)
                };
                g.FillPolygon(coffeeBrush, coffeeMiddle);

                // Молоко — значительно больше, примерно 3 части.
                Point[] milkBottom = new Point[]
                {
                    new Point(innerLeftTop + 8, cupTopY + 148),
                    new Point(innerRightTop - 8, cupTopY + 148),
                    new Point(innerRightBottom - 10, cupBottomY - 16),
                    new Point(innerLeftBottom + 10, cupBottomY - 16)
                };
                g.FillPolygon(milkBrush, milkBottom);

                // Блики на стакане.
                Point[] highlight1 = new Point[]
                {
                    new Point(cupLeftTop + 18, cupTopY + 30),
                    new Point(cupLeftTop + 34, cupTopY + 30),
                    new Point(cupLeftBottom + 26, cupBottomY - 26),
                    new Point(cupLeftBottom + 10, cupBottomY - 26)
                };
                Point[] highlight2 = new Point[]
                {
                    new Point(cupRightTop - 32, cupTopY + 44),
                    new Point(cupRightTop - 22, cupTopY + 44),
                    new Point(cupRightBottom - 22, cupBottomY - 44),
                    new Point(cupRightBottom - 32, cupBottomY - 44)
                };
                g.FillPolygon(shineBrush, highlight1);
                g.FillPolygon(new SolidBrush(Color.FromArgb(120, 255, 255, 255)), highlight2);

                g.DrawString("Нажми E, чтобы закрыть рецепт", closeFont, black, panel.X + 42, panel.Bottom - 56);
            }
        }

        private void DrawSettingsPanel(Graphics g)
        {
            if (!_settingsOpen)
                return;

            Rectangle panel = GetSettingsPanelBounds();

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(210, 8, 8, 10)))
            using (Pen border = new Pen(Color.FromArgb(180, 230, 210, 160), 2))
            using (Font titleFont = new Font("Arial", 20, FontStyle.Bold))
            using (Font labelFont = new Font("Arial", 12, FontStyle.Bold))
            using (Font smallFont = new Font("Arial", 10, FontStyle.Regular))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(245, 235, 210)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(230, 230, 220)))
            {
                g.FillRectangle(bg, panel);
                g.DrawRectangle(border, panel);

                g.DrawString("Настройки", titleFont, titleBrush, panel.X + 28, panel.Y + 22);
                g.DrawString("Tab — закрыть", smallFont, textBrush, panel.Right - 118, panel.Y + 30);

                g.DrawString(
                    "Чувствительность: " + GetSensitivityPercent() + "%",
                    labelFont,
                    textBrush,
                    panel.X + 55,
                    panel.Y + 78
                );

                DrawSettingsSlider(g, GetSensitivitySliderBounds(), (GetSensitivityPercent() - 1) / 99.0);

                g.DrawString(
                    "Громкость: " + (int)Math.Round(_volume * 100) + "%",
                    labelFont,
                    textBrush,
                    panel.X + 55,
                    panel.Y + 148
                );

                DrawSettingsSlider(g, GetVolumeSliderBounds(), _volume);

                int reputation = _game == null ? 0 : _game.Reputation;

                g.DrawString(
                    "Репутация: " + reputation,
                    labelFont,
                    textBrush,
                    panel.X + 55,
                    panel.Y + 212
                );
            }
        }

        private int GetSensitivityPercent()
        {
            double rawPercent = (_mouseSensitivity / 0.002558923636) * 50.0;
            int percent = (int)Math.Round(rawPercent);

            if (percent < 1)
                percent = 1;

            if (percent > 100)
                percent = 100;

            return percent;
        }

        private void DrawSettingsSlider(Graphics g, Rectangle slider, double value01)
        {
            value01 = Clamp01(value01);

            int trackY = slider.Y + slider.Height / 2;
            int fillWidth = (int)Math.Round(slider.Width * value01);
            int knobX = slider.Left + fillWidth;

            using (Pen trackPen = new Pen(Color.FromArgb(110, 210, 210, 210), 5))
            using (Pen fillPen = new Pen(Color.FromArgb(230, 235, 205, 120), 5))
            using (SolidBrush knobBrush = new SolidBrush(Color.FromArgb(245, 235, 190)))
            using (Pen knobBorder = new Pen(Color.FromArgb(80, 0, 0, 0), 1))
            {
                g.DrawLine(trackPen, slider.Left, trackY, slider.Right, trackY);
                g.DrawLine(fillPen, slider.Left, trackY, slider.Left + fillWidth, trackY);

                Rectangle knob = new Rectangle(knobX - 8, trackY - 8, 16, 16);
                g.FillEllipse(knobBrush, knob);
                g.DrawEllipse(knobBorder, knob);
            }
        }

        private void DrawTextBackplate(Graphics g, float x, float y, float width, float height)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(82, 0, 0, 0)))
                g.FillRectangle(bg, x, y, width, height);
        }
    }
}
