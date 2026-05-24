using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
        private AudioManager _audio;
        private bool _musicStarted;
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
        private const double MovementSpeed = 150.0;

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
                _controller.SetTakeawayShelfCupVisible,
                _controller.SetTiaOrderExchangeVisible,
                SetTvScreenOnAndStartMusic,
                _controller.SetCashRecipeScreenVisible,
                _controller.SetCoffeeMachineCupState,
                _controller.SetRaspberryPumpPressed,
                _controller.SetClientVisible,
                _controller.SetClientTransform,
                _controller.SetCoffeeBeanFrontBagVisible,
                _controller.SetCoffeeMachineRefillAnimation,
                SetSinkWashAnimationAndSound,
                _controller.SetTiaHoldingCupVisible,
                _controller.SetTiaBarPassageBlocked,
                PlayGameSound,
                PlayGameSoundFor,
                StartGameSoundLoop,
                StopGameSoundLoop
            );

            _audio = new AudioManager();
            _audio.MusicVolume = 0.35f;
            _audio.SoundVolume = 0.85f;
            _musicStarted = false;

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


        private void SetSinkWashAnimationAndSound(bool active, double progress)
        {
            // Звук мойки жёстко привязан к видимости/активности самой анимации.
            // Как только анимация выключается, сразу останавливаем washingCups.
            _controller.SetSinkWashAnimation(active, progress);

            if (!active)
                StopGameSoundLoop("washingCups");
        }

        private void SetTvScreenOnAndStartMusic(bool visible)
        {
            _controller.SetTvScreenOn(visible);

            if (visible)
                StartBackgroundMusicOnce();
        }

        private void StartBackgroundMusicOnce()
        {
            if (_musicStarted || _audio == null)
                return;

            _musicStarted = true;
            _audio.PlayMusicLoop(Path.Combine(Application.StartupPath, "Assets", "Music", "back.mp3"));
        }

        private string GetMusicAssetPath(string fileName)
        {
            string musicFolder = Path.Combine(Application.StartupPath, "Assets", "Music");
            string exactPath = Path.Combine(musicFolder, fileName + ".mp3");

            if (File.Exists(exactPath))
                return exactPath;

            // На всякий случай поддерживаем частые опечатки в названиях файлов.
            if (fileName == "washingCups")
            {
                string cyrillicCPath = Path.Combine(musicFolder, "washingСups.mp3");
                if (File.Exists(cyrillicCPath))
                    return cyrillicCPath;
            }

            if (fileName == "generalInteraction")
            {
                string typoPath = Path.Combine(musicFolder, "generalnteraction.mp3");
                if (File.Exists(typoPath))
                    return typoPath;
            }

            if (Directory.Exists(musicFolder))
            {
                string expectedName = fileName + ".mp3";
                string[] files = Directory.GetFiles(musicFolder, "*.mp3");

                for (int i = 0; i < files.Length; i++)
                {
                    if (string.Equals(Path.GetFileName(files[i]), expectedName, StringComparison.OrdinalIgnoreCase))
                        return files[i];
                }
            }

            return exactPath;
        }

        private void PlayGameSound(string fileName)
        {
            if (_audio == null)
                return;

            _audio.PlaySound(GetMusicAssetPath(fileName));
        }

        private void PlayGameSoundFor(string fileName, double maxSeconds)
        {
            if (_audio == null)
                return;

            _audio.PlaySoundFor(GetMusicAssetPath(fileName), maxSeconds);
        }

        private void StartGameSoundLoop(string fileName)
        {
            if (_audio == null)
                return;

            float volumeMultiplier = string.Equals(fileName, "Stomp", StringComparison.OrdinalIgnoreCase) ? 0.5f : 1.0f;
            _audio.StartLoopingSound(fileName, GetMusicAssetPath(fileName), volumeMultiplier);
        }

        private void StopGameSoundLoop(string fileName)
        {
            if (_audio == null)
                return;

            _audio.StopLoopingSound(fileName);
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

            // Временная debug-кнопка: на русской раскладке клавиша "ъ"\n            // сразу переносит к цели "отдать заказ".
            if (e.KeyCode == Keys.OemCloseBrackets)
            {
                _game.DebugJumpToGiveOrderObjective();
                Invalidate();
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

            _audio?.KeepLoopsAlive();

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
            _audio?.Dispose();
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

            DrawWindowMorningLight(g);
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


        private void DrawWindowMorningLight(Graphics g)
        {
            if (_game == null)
                return;

            // Утренний свет из левого окна. Рисуем его поверх затемнения,
            // поэтому он остаётся видимым даже при выключенном свете.
            int windowAlpha = _game.LightOn ? 72 : 128;

            PointF w0;
            PointF w1;
            PointF w2;
            PointF w3;

            if (ProjectWorldPoint(-298.4, -18, 261, out w0) &&
                ProjectWorldPoint(-298.4, 58, 261, out w1) &&
                ProjectWorldPoint(-298.4, 58, 309, out w2) &&
                ProjectWorldPoint(-298.4, -18, 309, out w3))
            {
                using (SolidBrush glassGlow = new SolidBrush(Color.FromArgb(windowAlpha, 255, 238, 190)))
                using (SolidBrush glassCore = new SolidBrush(Color.FromArgb(windowAlpha / 2, 255, 250, 218)))
                {
                    g.FillPolygon(glassGlow, new PointF[] { w0, w1, w2, w3 });

                    PointF centerTop = new PointF((w1.X + w2.X) * 0.5f, (w1.Y + w2.Y) * 0.5f);
                    PointF centerBottom = new PointF((w0.X + w3.X) * 0.5f, (w0.Y + w3.Y) * 0.5f);
                    g.FillPolygon(glassCore, new PointF[]
                    {
                        new PointF((w0.X + centerBottom.X) * 0.5f, (w0.Y + centerBottom.Y) * 0.5f),
                        new PointF((w1.X + centerTop.X) * 0.5f, (w1.Y + centerTop.Y) * 0.5f),
                        new PointF((w2.X + centerTop.X) * 0.5f, (w2.Y + centerTop.Y) * 0.5f),
                        new PointF((w3.X + centerBottom.X) * 0.5f, (w3.Y + centerBottom.Y) * 0.5f)
                    });
                }
            }

            // ВАЖНО: больше не рисуем длинные экранные лучи поверх всей 3D-сцены.
            // Такие 2D-полигоны всегда лежат поверх мебели и поэтому просвечивают
            // через барную стойку. Световое пятно теперь добавлено как 3D-поверхность
            // пола в MainController и закрывается мебелью обычным порядком слоёв.
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
            DrawCashCounter(g);
            DrawKeyHints(g);
            DrawPrompt(g);
            DrawBottomText(g);
            DrawCenterText(g);
            DrawDialogueChoice(g);
            DrawHeldTakeawayCup(g);
            DrawRecipeOverlay(g);
            DrawSettingsPanel(g);
        }

        private void DrawCashCounter(Graphics g)
        {
            if (!_game.CashDisplayVisible)
                return;

            string text = $"касса : {_game.CashAmount}";

            using (Font font = new Font("Arial", 18, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Brush fillBrush = new SolidBrush(Color.FromArgb(186, 255, 206)))
            using (Brush outlineBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
            {
                SizeF size = g.MeasureString(text, font);
                float x = (ClientSize.Width - size.Width) * 0.5f;
                float y = 18f;

                g.DrawString(text, font, outlineBrush, x - 1, y);
                g.DrawString(text, font, outlineBrush, x + 1, y);
                g.DrawString(text, font, outlineBrush, x, y - 1);
                g.DrawString(text, font, outlineBrush, x, y + 1);
                g.DrawString(text, font, fillBrush, x, y);
            }
        }

        private void DrawObjectives(Graphics g)
        {
            int x = 18;
            int y = 18;
            int width = 390;
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

            string text = _game.BottomText;
            bool isClientText = text.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase);
            bool isPlayerAnswer =
                text == "Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?" ||
                text == "Ага...Чего желаете?" ||
                text == "Заказ принят, к оплате будет 300 рублей" ||
                text == "300р";
            bool isFirstPersonSpeech =
                text == "Эх... Очередная смена... Очередная неделя без выходных" ||
                text == "Надо подготовиться к началу смены, убрать и помыть все кружки" ||
                text == "заправлю кофемашину зернами. где там мешок с кофе..." ||
                text == "Заправлю кофемашину зернами. где там мешок с кофе..." ||
                text == "Как же бесят эти выскочки с утра пораньше...";

            if (isFirstPersonSpeech)
            {
                using (Font font = new Font("Arial", 24, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(235, 235, 235)))
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(145, 0, 0, 0)))
                {
                    int maxPanelWidth = Math.Min(920, ClientSize.Width - 110);
                    SizeF measured = g.MeasureString(text, font, maxPanelWidth - 56);
                    int panelWidth = Math.Min(maxPanelWidth, Math.Max(420, (int)Math.Ceiling(measured.Width) + 56));
                    int panelHeight = Math.Max(86, (int)Math.Ceiling(measured.Height) + 36);

                    int x = (ClientSize.Width - panelWidth) / 2;
                    int y = ClientSize.Height - panelHeight - 78;
                    Rectangle panel = new Rectangle(x, y, panelWidth, panelHeight);
                    g.FillRectangle(bgBrush, panel);

                    RectangleF textRect = new RectangleF(panel.X + 28, panel.Y + 18, panel.Width - 56, panel.Height - 30);
                    g.DrawString(text, font, textBrush, textRect);
                    return;
                }
            }

            if (isClientText || isPlayerAnswer)
            {
                using (Font font = new Font("Arial", 24, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(235, 235, 235)))
                {
                    int maxPanelWidth = Math.Min(860, ClientSize.Width - 110);
                    SizeF measured = g.MeasureString(text, font, maxPanelWidth - 48);
                    int panelWidth = Math.Min(maxPanelWidth, Math.Max(340, (int)Math.Ceiling(measured.Width) + 48));
                    int panelHeight = Math.Max(88, (int)Math.Ceiling(measured.Height) + 34);

                    int x;
                    if (isClientText)
                        x = 36;
                    else if (isPlayerAnswer)
                        x = ClientSize.Width - panelWidth - 36;
                    else
                        x = (ClientSize.Width - panelWidth) / 2;

                    int y = ClientSize.Height - panelHeight - 74;
                    Rectangle panel = new Rectangle(x, y, panelWidth, panelHeight);
                    Color borderColor = isClientText
                        ? Color.FromArgb(190, 236, 128, 178)
                        : Color.FromArgb(170, 230, 210, 160);

                    DrawChoiceStylePanel(g, panel, borderColor);
                    RectangleF textRect = new RectangleF(panel.X + 24, panel.Y + 16, panel.Width - 48, panel.Height - 24);
                    g.DrawString(text, font, brush, textRect);
                    return;
                }
            }

            using (Font font = new Font("Arial", 16, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(235, 235, 235)))
            {
                SizeF size = g.MeasureString(text, font);
                float centeredX = (ClientSize.Width - size.Width) / 2f;
                float centeredY = ClientSize.Height - 128;
                g.DrawString(text, font, brush, centeredX, centeredY);
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

            using (SolidBrush optionBg = new SolidBrush(Color.FromArgb(115, 28, 22, 18)))
            using (Pen optionBorder = new Pen(Color.FromArgb(135, 230, 210, 160), 1))
            using (Font titleFont = new Font("Arial", 15, FontStyle.Bold))
            using (Font optionFont = new Font("Arial", 12, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(245, 235, 210)))
            using (SolidBrush optionBrush = new SolidBrush(Color.FromArgb(235, 235, 225)))
            {
                DrawChoiceStylePanel(g, panel, Color.FromArgb(170, 230, 210, 160));
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

        private void DrawHeldTakeawayCup(Graphics g)
        {
            if (_game == null)
                return;

            if (!_game.HeldTakeawayCupVisible)
                return;

            int cupWidth = Math.Max(92, ClientSize.Width / 11);
            int cupHeight = Math.Max(136, ClientSize.Height / 4);
            int centerX = ClientSize.Width - cupWidth / 2 - 74;
            int bottomY = ClientSize.Height - 20;
            int topY = bottomY - cupHeight;

            DrawTakeawayCup(
                g,
                centerX,
                topY,
                bottomY,
                cupWidth,
                _game.CoffeePortionsInCup,
                _game.MilkPortionsInCup,
                _game.RaspberrySyrupPortionsInCup,
                0,
                true,
                false);
        }

        private void DrawCoffeeBrewingAnimation(Graphics g)
        {
            // 3D-стакан во время варки теперь отображается прямо в сцене на кофемашине,
            // поэтому отдельная 2D-оверлей-анимация больше не рисуется.
        }

        private void DrawTakeawayCup(Graphics g, int centerX, int topY, int bottomY, int cupWidth, int coffeePortions, int milkPortions, int syrupPortions, double brewingCoffeeProgress, bool drawShadow, bool underMachine)
        {
            int cupHeight = bottomY - topY;
            int topHalf = cupWidth / 2;
            int bottomHalf = Math.Max(28, cupWidth / 3);
            int lidHeight = 22;
            int sleeveTopY = topY + cupHeight / 2 - 10;
            int sleeveBottomY = sleeveTopY + 30;

            int bodyTopY = topY + lidHeight;
            int bodyBottomY = bottomY - 14;
            int leftTopX = centerX - topHalf + 4;
            int rightTopX = centerX + topHalf - 4;
            int leftBottomX = centerX - bottomHalf;
            int rightBottomX = centerX + bottomHalf;

            Func<int, int> leftAtY = currentY => leftTopX + (leftBottomX - leftTopX) * (currentY - bodyTopY) / Math.Max(1, bodyBottomY - bodyTopY);
            Func<int, int> rightAtY = currentY => rightTopX + (rightBottomX - rightTopX) * (currentY - bodyTopY) / Math.Max(1, bodyBottomY - bodyTopY);

            Point[] body = new Point[]
            {
                new Point(leftTopX, bodyTopY),
                new Point(rightTopX, bodyTopY),
                new Point(rightBottomX, bodyBottomY),
                new Point(leftBottomX, bodyBottomY)
            };

            Point[] sleeve = new Point[]
            {
                new Point(leftAtY(sleeveTopY), sleeveTopY),
                new Point(rightAtY(sleeveTopY), sleeveTopY),
                new Point(rightAtY(sleeveBottomY), sleeveBottomY),
                new Point(leftAtY(sleeveBottomY), sleeveBottomY)
            };

            int liquidTopY = bodyTopY + 6;
            int liquidBottomY = bodyBottomY - 4;
            int totalLiquidHeight = Math.Max(12, liquidBottomY - liquidTopY);

            double coffeeUnits = Math.Max(0, Math.Min(1, coffeePortions + brewingCoffeeProgress));
            double milkUnits = Math.Max(0, Math.Min(3, milkPortions));
            double syrupUnits = Math.Max(0, Math.Min(1, syrupPortions));

            int coffeeHeight = (int)Math.Round(totalLiquidHeight * (coffeeUnits / 5.0));
            int milkHeight = (int)Math.Round(totalLiquidHeight * (milkUnits / 5.0));
            int syrupHeight = (int)Math.Round(totalLiquidHeight * (syrupUnits / 5.0));

            int coffeeTopY = liquidBottomY - coffeeHeight;
            int milkTopY = coffeeTopY - milkHeight;
            int syrupTopY = milkTopY - syrupHeight;

            using (SolidBrush bodyBrush = new SolidBrush(Color.FromArgb(145, 230, 220, 206)))
            using (SolidBrush lidBrush = new SolidBrush(Color.FromArgb(236, 222, 226, 230)))
            using (SolidBrush rimInsideBrush = new SolidBrush(Color.FromArgb(150, 74, 62, 54)))
            using (SolidBrush sleeveBrush = new SolidBrush(Color.FromArgb(215, 114, 72, 46)))
            using (SolidBrush bottomBrush = new SolidBrush(Color.FromArgb(240, 214, 204, 192)))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            using (SolidBrush coffeeBrush = new SolidBrush(Color.FromArgb(255, 42, 22, 8)))
            using (SolidBrush milkBrush = new SolidBrush(Color.FromArgb(255, 252, 244, 232)))
            using (SolidBrush syrupBrush = new SolidBrush(Color.FromArgb(255, 236, 12, 120)))
            using (Pen outlinePen = new Pen(Color.FromArgb(225, 42, 36, 32), 4))
            using (Pen sleevePen = new Pen(Color.FromArgb(180, 88, 60, 38), 3))
            {
                if (drawShadow)
                    g.FillEllipse(shadowBrush, centerX - topHalf - 8, bottomY - 16, cupWidth + 16, 12);

                Rectangle lid = new Rectangle(centerX - topHalf - 2, topY, cupWidth + 4, lidHeight + 8);
                g.FillEllipse(lidBrush, lid);
                g.DrawEllipse(outlinePen, lid);
                g.FillRectangle(lidBrush, centerX - topHalf + 6, topY + 12, cupWidth - 12, lidHeight - 1);
                g.DrawRectangle(outlinePen, centerX - topHalf + 6, topY + 12, cupWidth - 12, lidHeight - 1);

                Rectangle rim = new Rectangle(centerX - topHalf + 8, topY + 13, cupWidth - 16, 15);
                g.FillEllipse(rimInsideBrush, rim);
                g.DrawEllipse(outlinePen, rim);

                if (coffeeHeight > 0)
                {
                    Point[] coffeeLayer = new Point[]
                    {
                        new Point(leftAtY(coffeeTopY), coffeeTopY),
                        new Point(rightAtY(coffeeTopY), coffeeTopY),
                        new Point(rightAtY(liquidBottomY), liquidBottomY),
                        new Point(leftAtY(liquidBottomY), liquidBottomY)
                    };
                    g.FillPolygon(coffeeBrush, coffeeLayer);
                }

                if (milkHeight > 0)
                {
                    Point[] milkLayer = new Point[]
                    {
                        new Point(leftAtY(milkTopY), milkTopY),
                        new Point(rightAtY(milkTopY), milkTopY),
                        new Point(rightAtY(coffeeTopY), coffeeTopY),
                        new Point(leftAtY(coffeeTopY), coffeeTopY)
                    };
                    g.FillPolygon(milkBrush, milkLayer);
                }

                if (syrupHeight > 0)
                {
                    Point[] syrupLayer = new Point[]
                    {
                        new Point(leftAtY(syrupTopY), syrupTopY),
                        new Point(rightAtY(syrupTopY), syrupTopY),
                        new Point(rightAtY(milkTopY), milkTopY),
                        new Point(leftAtY(milkTopY), milkTopY)
                    };
                    g.FillPolygon(syrupBrush, syrupLayer);
                }

                g.FillPolygon(bodyBrush, body);
                g.DrawPolygon(outlinePen, body);

                g.FillPolygon(sleeveBrush, sleeve);
                g.DrawPolygon(sleevePen, sleeve);

                Rectangle bottom = new Rectangle(centerX - bottomHalf - 2, bottomY - 25, bottomHalf * 2 + 4, 15);
                g.FillEllipse(bottomBrush, bottom);
                g.DrawEllipse(outlinePen, bottom);

                if (underMachine)
                {
                    using (SolidBrush trayShadowBrush = new SolidBrush(Color.FromArgb(58, 0, 0, 0)))
                        g.FillEllipse(trayShadowBrush, centerX - topHalf + 12, bottomY - 10, cupWidth - 24, 8);
                }
            }
        }

        private void DrawBrewingTakeawayCup3D(Graphics g, int centerX, int topY, int bottomY, int cupWidth, double brewingCoffeeProgress)
        {
            int cupHeight = bottomY - topY;
            int topHalf = cupWidth / 2;
            int bottomHalf = Math.Max(26, cupWidth / 3);
            int lidHeight = 20;
            int depthX = Math.Max(10, cupWidth / 7);
            int depthY = 8;
            int bodyTopY = topY + lidHeight;
            int bodyBottomY = bottomY - 16;

            int leftTopX = centerX - topHalf + 4;
            int rightTopX = centerX + topHalf - 4;
            int leftBottomX = centerX - bottomHalf;
            int rightBottomX = centerX + bottomHalf;

            Func<int, int> leftAtY = currentY => leftTopX + (leftBottomX - leftTopX) * (currentY - bodyTopY) / Math.Max(1, bodyBottomY - bodyTopY);
            Func<int, int> rightAtY = currentY => rightTopX + (rightBottomX - rightTopX) * (currentY - bodyTopY) / Math.Max(1, bodyBottomY - bodyTopY);

            Point[] frontBody = new Point[]
            {
                new Point(leftTopX, bodyTopY),
                new Point(rightTopX, bodyTopY),
                new Point(rightBottomX, bodyBottomY),
                new Point(leftBottomX, bodyBottomY)
            };

            Point[] sideBody = new Point[]
            {
                new Point(rightTopX, bodyTopY),
                new Point(rightTopX + depthX, bodyTopY - depthY),
                new Point(rightBottomX + depthX, bodyBottomY - depthY),
                new Point(rightBottomX, bodyBottomY)
            };

            Point[] topLid = new Point[]
            {
                new Point(leftTopX - 2, topY + 12),
                new Point(rightTopX + 2, topY + 12),
                new Point(rightTopX + depthX + 2, topY + 12 - depthY),
                new Point(leftTopX + depthX - 2, topY + 12 - depthY)
            };

            int sleeveTopY = topY + cupHeight / 2 - 10;
            int sleeveBottomY = sleeveTopY + 30;
            Point[] frontSleeve = new Point[]
            {
                new Point(leftAtY(sleeveTopY), sleeveTopY),
                new Point(rightAtY(sleeveTopY), sleeveTopY),
                new Point(rightAtY(sleeveBottomY), sleeveBottomY),
                new Point(leftAtY(sleeveBottomY), sleeveBottomY)
            };
            Point[] sideSleeve = new Point[]
            {
                new Point(rightAtY(sleeveTopY), sleeveTopY),
                new Point(rightAtY(sleeveTopY) + depthX, sleeveTopY - depthY),
                new Point(rightAtY(sleeveBottomY) + depthX, sleeveBottomY - depthY),
                new Point(rightAtY(sleeveBottomY), sleeveBottomY)
            };

            int liquidTopY = bodyTopY + 6;
            int liquidBottomY = bodyBottomY - 4;
            int totalLiquidHeight = Math.Max(12, liquidBottomY - liquidTopY);
            int coffeeHeight = (int)Math.Round(totalLiquidHeight * Math.Max(0, Math.Min(1, brewingCoffeeProgress)) / 5.0);
            int coffeeTopY = liquidBottomY - coffeeHeight;

            using (SolidBrush bodyFrontBrush = new SolidBrush(Color.FromArgb(228, 230, 220, 206)))
            using (SolidBrush bodySideBrush = new SolidBrush(Color.FromArgb(220, 204, 194, 182)))
            using (SolidBrush lidBrush = new SolidBrush(Color.FromArgb(236, 222, 226, 230)))
            using (SolidBrush lidSideBrush = new SolidBrush(Color.FromArgb(226, 196, 202, 206)))
            using (SolidBrush rimBrush = new SolidBrush(Color.FromArgb(150, 74, 62, 54)))
            using (SolidBrush sleeveFrontBrush = new SolidBrush(Color.FromArgb(226, 114, 72, 46)))
            using (SolidBrush sleeveSideBrush = new SolidBrush(Color.FromArgb(214, 92, 58, 36)))
            using (SolidBrush coffeeBrush = new SolidBrush(Color.FromArgb(235, 70, 40, 18)))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            using (Pen outlinePen = new Pen(Color.FromArgb(225, 42, 36, 32), 3))
            {
                g.FillEllipse(shadowBrush, centerX - topHalf - 8, bottomY - 14, cupWidth + depthX + 16, 12);

                if (coffeeHeight > 0)
                {
                    Point[] coffeeFront = new Point[]
                    {
                        new Point(leftAtY(coffeeTopY), coffeeTopY),
                        new Point(rightAtY(coffeeTopY), coffeeTopY),
                        new Point(rightAtY(liquidBottomY), liquidBottomY),
                        new Point(leftAtY(liquidBottomY), liquidBottomY)
                    };
                    g.FillPolygon(coffeeBrush, coffeeFront);
                }

                g.FillPolygon(bodySideBrush, sideBody);
                g.FillPolygon(bodyFrontBrush, frontBody);
                g.DrawPolygon(outlinePen, sideBody);
                g.DrawPolygon(outlinePen, frontBody);

                g.FillPolygon(sleeveSideBrush, sideSleeve);
                g.FillPolygon(sleeveFrontBrush, frontSleeve);
                g.DrawPolygon(outlinePen, sideSleeve);
                g.DrawPolygon(outlinePen, frontSleeve);

                g.FillPolygon(lidSideBrush, new Point[]
                {
                    new Point(rightTopX + 2, topY + 12),
                    new Point(rightTopX + depthX + 2, topY + 12 - depthY),
                    new Point(rightTopX + depthX + 2, topY + 28 - depthY),
                    new Point(rightTopX + 2, topY + 28)
                });
                g.FillPolygon(lidBrush, topLid);
                g.DrawPolygon(outlinePen, topLid);
                g.FillRectangle(lidBrush, leftTopX + 8, topY + 12, Math.Max(18, cupWidth - 16), lidHeight - 2);
                g.DrawRectangle(outlinePen, leftTopX + 8, topY + 12, Math.Max(18, cupWidth - 16), lidHeight - 2);
                g.FillEllipse(rimBrush, leftTopX + 10, topY + 13, Math.Max(18, cupWidth - 20), 13);
                g.DrawEllipse(outlinePen, leftTopX + 10, topY + 13, Math.Max(18, cupWidth - 20), 13);
            }
        }

        private void DrawRaspberryPumpAnimation(Graphics g)
        {
            if (_game == null || !_game.IsRaspberryPumpAnimating)
                return;

            double progress = _game.RaspberryPumpAnimationProgress;
            double wave = Math.Sin(progress * Math.PI);
            int pressOffset = (int)Math.Round(10 * wave);
            int x = ClientSize.Width - 210;
            int y = ClientSize.Height - 280;

            using (SolidBrush bottleBrush = new SolidBrush(Color.FromArgb(210, 68, 38, 34)))
            using (SolidBrush neckBrush = new SolidBrush(Color.FromArgb(220, 154, 58, 88)))
            using (SolidBrush pumpBrush = new SolidBrush(Color.FromArgb(225, 112, 36, 64)))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            using (SolidBrush syrupBrush = new SolidBrush(Color.FromArgb(210, 202, 54, 106)))
            using (Pen outlinePen = new Pen(Color.FromArgb(220, 30, 18, 22), 3))
            {
                g.FillEllipse(shadowBrush, x - 8, y + 116, 88, 16);

                g.FillRectangle(bottleBrush, x + 10, y + 44, 44, 68);
                g.DrawRectangle(outlinePen, x + 10, y + 44, 44, 68);
                g.FillRectangle(neckBrush, x + 20, y + 26, 24, 22);
                g.DrawRectangle(outlinePen, x + 20, y + 26, 24, 22);

                g.FillRectangle(pumpBrush, x + 18, y + 14 + pressOffset, 28, 10);
                g.DrawRectangle(outlinePen, x + 18, y + 14 + pressOffset, 28, 10);
                g.FillRectangle(pumpBrush, x + 42, y + 14 + pressOffset, 24, 7);
                g.DrawRectangle(outlinePen, x + 42, y + 14 + pressOffset, 24, 7);
                g.FillRectangle(pumpBrush, x + 30, y + 20 + pressOffset, 6, 10);
                g.DrawRectangle(outlinePen, x + 30, y + 20 + pressOffset, 6, 10);

                if (wave > 0.08)
                {
                    int streamHeight = 18 + (int)Math.Round(14 * wave);
                    g.FillRectangle(syrupBrush, x + 60, y + 22 + pressOffset, 5, streamHeight);
                    g.FillEllipse(syrupBrush, x + 56, y + 22 + pressOffset + streamHeight - 2, 12, 8);
                }
            }
        }

        private void DrawRecipeOverlay(Graphics g)
        {
            if (_game == null || !_game.RecipeOverlayVisible)
                return;

            int panelWidth = Math.Min(ClientSize.Width - 240, 980);
            int panelHeight = Math.Min(ClientSize.Height - 170, 610);
            Rectangle panel = new Rectangle(
                (ClientSize.Width - panelWidth) / 2,
                (ClientSize.Height - panelHeight) / 2,
                panelWidth,
                panelHeight
            );

            Rectangle imageArea = new Rectangle(
                panel.X + 52,
                panel.Y + 118,
                246,
                330
            );

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(255, 105, 195, 230)))
            using (Pen border = new Pen(Color.Black, 7))
            using (Pen cupLinePen = new Pen(Color.Black, 7))
            using (Font titleFont = new Font("Arial", 28, FontStyle.Bold))
            using (Font subTitleFont = new Font("Arial", 17, FontStyle.Bold))
            using (Font ingredientFont = new Font("Arial", 22, FontStyle.Bold))
            using (Font closeFont = new Font("Arial", 18, FontStyle.Bold))
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

                g.DrawString("малиновый латте", titleFont, black, panel.X + 28, panel.Y + 18);
                g.DrawString("С СОБОЙ", subTitleFont, black, panel.X + 32, panel.Y + 58);
                g.DrawString("рецепт заказа", subTitleFont, black, panel.X + 32, panel.Y + 86);

                float ingredientsX = imageArea.Right + 72;
                float ingredientsY = panel.Y + 144;
                g.DrawString("ингредиенты:", ingredientFont, black, ingredientsX, ingredientsY);
                g.DrawString("кофе x 1", ingredientFont, black, ingredientsX, ingredientsY + 64);
                g.DrawString("молоко x 3", ingredientFont, black, ingredientsX, ingredientsY + 118);
                g.DrawString("сироп малины", ingredientFont, black, ingredientsX, ingredientsY + 172);

                int cupTopY = imageArea.Y + 38;
                int cupBottomY = imageArea.Bottom - 26;
                int cupLeftTop = imageArea.X + 62;
                int cupRightTop = imageArea.Right - 62;
                int cupLeftBottom = imageArea.X + 84;
                int cupRightBottom = imageArea.Right - 84;

                Point[] cupBody = new Point[]
                {
                    new Point(cupLeftTop, cupTopY),
                    new Point(cupRightTop, cupTopY),
                    new Point(cupRightBottom, cupBottomY),
                    new Point(cupLeftBottom, cupBottomY)
                };

                g.FillEllipse(lidBrush, imageArea.X + 36, imageArea.Y + 8, imageArea.Width - 72, 42);
                g.DrawEllipse(cupLinePen, imageArea.X + 36, imageArea.Y + 8, imageArea.Width - 72, 42);
                g.FillRectangle(lidBrush, imageArea.X + 58, imageArea.Y + 24, imageArea.Width - 116, 20);
                g.DrawRectangle(cupLinePen, imageArea.X + 58, imageArea.Y + 24, imageArea.Width - 116, 20);

                g.FillRectangle(strawBrush, imageArea.X + 144, imageArea.Y - 2, 12, 104);
                g.DrawRectangle(cupLinePen, imageArea.X + 144, imageArea.Y - 2, 12, 104);

                g.FillPolygon(glassBrush, cupBody);
                g.DrawPolygon(cupLinePen, cupBody);

                int liquidTopY = cupTopY + 20;
                int liquidBottomY = cupBottomY - 14;
                int totalLiquidHeight = liquidBottomY - liquidTopY;
                int syrupTopY = liquidTopY;
                int milkTopY = liquidTopY + totalLiquidHeight / 5;
                int coffeeTopY = liquidTopY + totalLiquidHeight * 4 / 5;

                Func<int, int> leftAtY = currentY =>
                    cupLeftTop + (cupLeftBottom - cupLeftTop) * (currentY - cupTopY) / (cupBottomY - cupTopY);
                Func<int, int> rightAtY = currentY =>
                    cupRightTop + (cupRightBottom - cupRightTop) * (currentY - cupTopY) / (cupBottomY - cupTopY);

                Point[] coffeeBottom = new Point[]
                {
                    new Point(leftAtY(coffeeTopY), coffeeTopY),
                    new Point(rightAtY(coffeeTopY), coffeeTopY),
                    new Point(rightAtY(liquidBottomY), liquidBottomY),
                    new Point(leftAtY(liquidBottomY), liquidBottomY)
                };
                g.FillPolygon(coffeeBrush, coffeeBottom);

                Point[] milkMiddle = new Point[]
                {
                    new Point(leftAtY(milkTopY), milkTopY),
                    new Point(rightAtY(milkTopY), milkTopY),
                    new Point(rightAtY(coffeeTopY), coffeeTopY),
                    new Point(leftAtY(coffeeTopY), coffeeTopY)
                };
                g.FillPolygon(milkBrush, milkMiddle);

                Point[] raspberryTop = new Point[]
                {
                    new Point(leftAtY(syrupTopY), syrupTopY),
                    new Point(rightAtY(syrupTopY), syrupTopY),
                    new Point(rightAtY(milkTopY), milkTopY),
                    new Point(leftAtY(milkTopY), milkTopY)
                };
                g.FillPolygon(raspberryBrush, raspberryTop);

                Point[] highlight1 = new Point[]
                {
                    new Point(cupLeftTop + 16, cupTopY + 28),
                    new Point(cupLeftTop + 30, cupTopY + 28),
                    new Point(cupLeftBottom + 22, cupBottomY - 24),
                    new Point(cupLeftBottom + 8, cupBottomY - 24)
                };
                Point[] highlight2 = new Point[]
                {
                    new Point(cupRightTop - 28, cupTopY + 40),
                    new Point(cupRightTop - 18, cupTopY + 40),
                    new Point(cupRightBottom - 18, cupBottomY - 38),
                    new Point(cupRightBottom - 28, cupBottomY - 38)
                };
                g.FillPolygon(shineBrush, highlight1);
                using (SolidBrush shineSoft = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                    g.FillPolygon(shineSoft, highlight2);

                g.DrawString("Нажми E, чтобы закрыть рецепт", closeFont, black, panel.X + 30, panel.Bottom - 44);
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

        private void DrawChoiceStylePanel(Graphics g, Rectangle panel, Color borderColor)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (Pen border = new Pen(borderColor, 2))
            {
                g.FillRectangle(bg, panel);
                g.DrawRectangle(border, panel);
            }
        }

        private void DrawTextBackplate(Graphics g, float x, float y, float width, float height)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(82, 0, 0, 0)))
                g.FillRectangle(bg, x, y, width, height);
        }
    }
}
