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
        private double _volume = 0.5;
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

        private enum StartScreenStage
        {
            ContinueMenu,
            Logo,
            Menu,
            Loading,
            DayIntro,
            Game
        }

        private StartScreenStage _startScreenStage = StartScreenStage.Logo;
        private double _startScreenTimer = 0;
        private Rectangle _startButtonBounds = Rectangle.Empty;
        private Rectangle _continueButtonBounds = Rectangle.Empty;
        private Rectangle _newGameButtonBounds = Rectangle.Empty;
        private bool _startScreenMusicStarted;
        private bool _timePassAutosaveWrittenThisRun;
        private bool _dayTransitionOverlayActive;
        private double _dayTransitionOverlayTimer;
        private string _dayTransitionOverlayText = string.Empty;
        private bool _showToBeContinuedAfterDayTransition;
        private bool _toBeContinuedScreenActive;
        private int _lastKnownGameDay;
        private bool _sceneDirty = true;
        private int _loadingGirlStepIndex = -1;
        private int _loadingDragStepIndex = -1;
        private bool _loadingDeathSoundPlayed;

        private const double LogoScreenDuration = 5.0;
        private const double LoadingScreenDuration = 18.0;
        private const double DayTitleFadeDuration = 3.0;
        private const double DayTitleHoldDuration = 2.0;
        private const double DayTitleScreenDuration = DayTitleFadeDuration + DayTitleHoldDuration;

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

            _model.SceneChanged += (s, e) => _sceneDirty = true;

            _controller.CreateTestScene();
            _sceneDirty = true;

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
                _controller.SetBigGlassShelfCupVisible,
                _controller.SetTiaOrderExchangeVisible,
                _controller.SetMikeOrderExchangeVisible,
                SetTvScreenOnAndStartMusic,
                SetCashRecipeScreenForCurrentOrder,
                _controller.SetCoffeeMachineCupState,
                _controller.SetCoffeeMachinePaperCupMode,
                _controller.SetRaspberryPumpPressed,
                _controller.SetFridgeOpenAnimation,
                _controller.SetClientVisible,
                _controller.SetClientTransform,
                _controller.SetClientSpeaking,
                _controller.SetMikeVisible,
                _controller.SetMikeTransform,
                _controller.SetMikeSpeaking,
                _controller.SetMikeWideEyesVisible,
                _controller.SetMikeHeadTrackingActive,
                _controller.SetMikeSmileProgress,
                _controller.SetMikeHoldingOrderVisible,
                _controller.SetCoffeeBeanFrontBagVisible,
                _controller.SetCoffeeMachineRefillAnimation,
                _controller.SetIceMakerLidAnimation,
                SetSinkWashAnimationAndSound,
                _controller.SetTiaHoldingCupVisible,
                _controller.SetTiaBarPassageBlocked,
                PlayGameSound,
                PlayGameSoundFor,
                StartGameSoundLoop,
                StopGameSoundLoop,
                SetGameSoundLoopVolume,
                _controller.SetEveningWindowLight,
                StartDayTransitionOverlay
            );

            _audio = new AudioManager();
            _musicStarted = false;
            ApplyAudioVolumeFromSlider();
            StartStartScreenMusicOnce();
            _mouseLookActive = false;
            ShowGameCursor();
            _lastKnownGameDay = 1;
            if (HasAutosave())
            {
                _startScreenStage = StartScreenStage.ContinueMenu;
                _startScreenTimer = 0;
                _mouseLookActive = false;
                ShowGameCursor();
            }

            // KeyDown уже подключен в Form1.Designer.cs.
            // KeyUp там нет, поэтому подключаем здесь.
            this.KeyUp += Form1_KeyUp;
            this.MouseEnter += Form1_MouseEnter;
            this.MouseLeave += Form1_MouseLeave;

            // Для GDI+ лучше 30 FPS, чем пытаться насильно держать 60 FPS.
            // Так меньше дерганий и меньше нагрузка на WinForms.
            _gameTimer.Interval = 33;
            _gameTimer.Tick += GameTimer_Tick;

            _frameClock.Start();
            _gameTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Заглушка для дизайнера Visual Studio.
        }



        private string GetAutosavePath()
        {
            string folder = Path.Combine(Application.UserAppDataPath, "Save");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "autosave_timepass.txt");
        }

        private bool HasAutosave()
        {
            return File.Exists(GetAutosavePath());
        }

        private void WriteTimePassAutosave()
        {
            try
            {
                File.WriteAllText(GetAutosavePath(), "timepass_day1_v1");
            }
            catch
            {
            }
        }

        private void DeleteAutosave()
        {
            try
            {
                string path = GetAutosavePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private void ContinueFromAutosave()
        {
            StopStartScreenMusic();
            _game.RestoreAtTimePassSavePoint();
            _controller.SetCameraAtTimePassSavePoint();
            _startScreenStage = StartScreenStage.Game;
            _startScreenTimer = 0;
            _pressedKeys.Clear();
            _mouseLookActive = true;
            _lastKnownGameDay = _game.CurrentDay;
            HideGameCursor();
            CenterMouseForStableLook();
            Invalidate();
        }

        private void StartNewGameFromContinueMenu()
        {
            DeleteAutosave();
            _timePassAutosaveWrittenThisRun = false;
            _dayTransitionOverlayActive = false;
            _dayTransitionOverlayTimer = 0;
            _dayTransitionOverlayText = string.Empty;
            _showToBeContinuedAfterDayTransition = false;
            _toBeContinuedScreenActive = false;
            _lastKnownGameDay = 1;
            _startScreenStage = StartScreenStage.Logo;
            _startScreenTimer = 0;
            _pressedKeys.Clear();
            _mouseLookActive = false;
            ShowGameCursor();
            Invalidate();
        }

        private void StartStartScreenMusicOnce()
        {
            if (_startScreenMusicStarted || _audio == null)
                return;

            _startScreenMusicStarted = true;
            StartGameSoundLoop("Day1Mike");
        }

        private void StopStartScreenMusic()
        {
            if (!_startScreenMusicStarted)
                return;

            _startScreenMusicStarted = false;
            StopGameSoundLoop("Day1Mike");
        }

        private void ResetLoadingSceneAudioState()
        {
            _loadingGirlStepIndex = -1;
            _loadingDragStepIndex = -1;
            _loadingDeathSoundPlayed = false;
        }


        private void UpdateLoadingSceneAudio(double seconds)
        {
            const double girlStepInterval = 0.7;
            const double knifeRevealMoment = 10.72;
            const double dragStart = 13.05;
            double[] dragStepTimes = new double[] { dragStart + 0.18, dragStart + 0.88, dragStart + 1.58 };

            int girlStepIndex = (int)Math.Floor(seconds / girlStepInterval);
            if (girlStepIndex > _loadingGirlStepIndex && seconds < knifeRevealMoment)
            {
                _loadingGirlStepIndex = girlStepIndex;
                if (girlStepIndex >= 1 && girlStepIndex <= 15)
                    PlayGameSound("step");
            }

            if (!_loadingDeathSoundPlayed && seconds >= knifeRevealMoment)
            {
                _loadingDeathSoundPlayed = true;
                PlayGameSound("death");
            }

            while (_loadingDragStepIndex + 1 < dragStepTimes.Length && seconds >= dragStepTimes[_loadingDragStepIndex + 1])
            {
                _loadingDragStepIndex++;
                PlayGameSound("step");
            }
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

        private void SetCashRecipeScreenForCurrentOrder(bool visible)
        {
            bool mikeEspresso = _game != null && _game.MikeRecipeActive;
            _controller.SetCashRecipeScreenMode(visible, mikeEspresso);
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

            float volumeMultiplier = 1.0f;
            if (string.Equals(fileName, "Stomp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "Day1Mike", StringComparison.OrdinalIgnoreCase))
                volumeMultiplier = 0.5f;

            _audio.StartLoopingSound(fileName, GetMusicAssetPath(fileName), volumeMultiplier);
        }

        private void StopGameSoundLoop(string fileName)
        {
            if (_audio == null)
                return;

            if (string.Equals(fileName, "back", StringComparison.OrdinalIgnoreCase))
            {
                _audio.StopMusic();
                _musicStarted = false;
                return;
            }

            _audio.StopLoopingSound(fileName);
        }

        private void SetGameSoundLoopVolume(string fileName, double volumeMultiplier)
        {
            if (_audio == null)
                return;

            _audio.SetLoopingSoundVolume(fileName, (float)volumeMultiplier);
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

            if (_startScreenStage != StartScreenStage.Game)
            {
                if (_startScreenStage == StartScreenStage.ContinueMenu && e.Button == MouseButtons.Left)
                {
                    if (_continueButtonBounds.Contains(e.Location))
                    {
                        PlayGameSound("start");
                        ContinueFromAutosave();
                        return;
                    }

                    if (_newGameButtonBounds.Contains(e.Location))
                    {
                        PlayGameSound("start");
                        StartNewGameFromContinueMenu();
                        return;
                    }
                }

                if (_startScreenStage == StartScreenStage.Menu &&
                    e.Button == MouseButtons.Left &&
                    _startButtonBounds.Contains(e.Location))
                {
                    PlayGameSound("start");
                    ResetLoadingSceneAudioState();
                    _startScreenStage = StartScreenStage.Loading;
                    _startScreenTimer = 0;
                    _pressedKeys.Clear();
                    Invalidate();
                }

                _mouseLookActive = false;
                ShowGameCursor();
                return;
            }

            if (_dayTransitionOverlayActive)
            {
                _mouseLookActive = false;
                ShowGameCursor();
                return;
            }

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
            if (_startScreenStage != StartScreenStage.Game)
            {
                ShowGameCursor();
                return;
            }

            if (_dayTransitionOverlayActive)
            {
                ShowGameCursor();
                return;
            }

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

            if (_startScreenStage != StartScreenStage.Game)
            {
                _mouseLookActive = false;
                ShowGameCursor();
                return;
            }

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
            if (_startScreenStage != StartScreenStage.Game)
                return;

            if (_dayTransitionOverlayActive)
                return;

            if (_settingsOpen)
                return;

            if (_game != null && _game.IsPlayerLookLocked)
            {
                CenterMouseForStableLook();
                return;
            }

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

            if (_startScreenStage != StartScreenStage.Game)
                return;

            if (_dayTransitionOverlayActive)
                return;

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

            // Временная debug-кнопка: на русской раскладке клавиша "ъ"
            // сразу переносит к цели "Отдать заказ: Майк".
            if (e.KeyCode == Keys.OemCloseBrackets)
            {
                _game.DebugJumpToGiveOrderObjective();
                Invalidate();
                return;
            }

            if (e.KeyCode == Keys.Space)
            {
                _game?.SkipDialogueMessage();
                Invalidate();
                return;
            }

            if (e.KeyCode == Keys.E)
            {
                if (_game != null && _game.IsPlayerMovementLocked)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                _game.Interact();
                CheckForDayTransitionOverlay();
                Invalidate();
                return;
            }

            if (_game != null && _game.IsPlayerMovementLocked &&
                (e.KeyCode == Keys.W || e.KeyCode == Keys.A || e.KeyCode == Keys.S || e.KeyCode == Keys.D))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
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
            ApplyAudioVolumeFromSlider();
            Invalidate();
        }

        private void ApplyAudioVolumeFromSlider()
        {
            if (_audio == null)
                return;

            double scale = GetVolumeScaleFromSlider();
            _audio.MusicVolume = (float)(0.35 * scale);
            _audio.SoundVolume = (float)(0.85 * scale);
        }

        private double GetVolumeScaleFromSlider()
        {
            double percent = Clamp01(_volume) * 100.0;

            if (percent <= 0.0)
                return 0.0;

            if (percent <= 25.0)
                return (percent / 25.0) * (4.0 / 3.0);

            if (percent <= 50.0)
                return (4.0 / 3.0) + ((percent - 25.0) / 25.0) * (2.0 / 3.0);

            if (percent <= 75.0)
                return 2.0 + ((percent - 50.0) / 25.0);

            return 3.0 + ((percent - 75.0) / 25.0);
        }

        private double Clamp01(double value)
        {
            if (value < 0)
                return 0;

            if (value > 1)
                return 1;

            return value;
        }

        private bool ShouldRepaintGameFrame(bool movementRequested)
        {
            if (_sceneDirty || movementRequested || _dayTransitionOverlayActive)
                return true;

            if (_game == null)
                return false;

            return
                _game.IsLightFlickering ||
                _game.TimePassTransitionVisible ||
                _game.ScreenFlash ||
                !string.IsNullOrEmpty(_game.BottomText) ||
                !string.IsNullOrEmpty(_game.CenterText) ||
                !string.IsNullOrEmpty(_game.PromptText) ||
                _game.IsChoiceActive ||
                _game.CashDisplayVisible;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            double deltaTime = _frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();

            if (deltaTime <= 0 || deltaTime > 0.1)
                deltaTime = 0.033;

            _audio?.KeepLoopsAlive();

            if (_startScreenStage != StartScreenStage.Game)
            {
                UpdateStartScreen(deltaTime);
                Invalidate();
                return;
            }

            if (_dayTransitionOverlayActive)
            {
                _dayTransitionOverlayTimer += deltaTime;
                if (_dayTransitionOverlayTimer >= DayTitleScreenDuration)
                {
                    _dayTransitionOverlayActive = false;
                    _dayTransitionOverlayTimer = 0;

                    if (_showToBeContinuedAfterDayTransition)
                    {
                        _showToBeContinuedAfterDayTransition = false;
                        _toBeContinuedScreenActive = true;
                        _pressedKeys.Clear();
                        _mouseLookActive = false;
                        ShowGameCursor();
                        Invalidate();
                        return;
                    }

                    if (!_settingsOpen)
                    {
                        _mouseLookActive = true;
                        HideGameCursor();
                        CenterMouseForStableLook();
                    }
                }

                Invalidate();
                return;
            }

            if (_toBeContinuedScreenActive)
            {
                _pressedKeys.Clear();
                _mouseLookActive = false;
                ShowGameCursor();
                Invalidate();
                return;
            }

            if (_settingsOpen)
                return;

            UpdateMouseLook();

            double localDx = 0;
            double localDy = 0;
            double localDz = 0;
            bool movementLocked = _game != null && _game.IsPlayerMovementLocked;

            if (!movementLocked)
            {
                if (_pressedKeys.Contains(Keys.W))
                    localDz += 1;

                if (_pressedKeys.Contains(Keys.S))
                    localDz -= 1;

                if (_pressedKeys.Contains(Keys.A))
                    localDx -= 1;

                if (_pressedKeys.Contains(Keys.D))
                    localDx += 1;
            }

            bool movementRequested = localDx != 0 || localDy != 0 || localDz != 0;
            if (movementRequested)
            {
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

            _game.Update(deltaTime);
            if (_game.IsAtTimePassSavePoint && !_timePassAutosaveWrittenThisRun)
            {
                WriteTimePassAutosave();
                _timePassAutosaveWrittenThisRun = true;
            }
            _controller.AnimateTvScreen(deltaTime);
            _controller.AnimateNpcBlink(deltaTime);
            CheckForDayTransitionOverlay();

            if (ShouldRepaintGameFrame(movementRequested))
            {
                Invalidate();
                _sceneDirty = false;
            }
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

            if (_startScreenStage != StartScreenStage.Game)
            {
                DrawStartScreen(g);
                return;
            }

            _view.Render(g, _model, _controller.Camera, this.ClientSize.Width, this.ClientSize.Height);

            DrawGameEffects(g);
            DrawGameUi(g);
            DrawTimePassTransition(g);
            DrawDayTransitionOverlay(g);
            DrawToBeContinuedScreen(g);
        }

        private void UpdateStartScreen(double deltaTime)
        {
            if (_startScreenStage == StartScreenStage.ContinueMenu)
                return;

            if (_startScreenStage == StartScreenStage.Logo && !_isFullscreen)
                return;

            _startScreenTimer += deltaTime;

            if (_startScreenStage == StartScreenStage.Loading)
                UpdateLoadingSceneAudio(_startScreenTimer);

            if (_startScreenStage == StartScreenStage.Logo && _startScreenTimer >= LogoScreenDuration)
            {
                _startScreenStage = StartScreenStage.Menu;
                _startScreenTimer = 0;
                _mouseLookActive = false;
                ShowGameCursor();
                return;
            }

            if (_startScreenStage == StartScreenStage.Loading && _startScreenTimer >= LoadingScreenDuration)
            {
                ResetLoadingSceneAudioState();
                _startScreenStage = StartScreenStage.DayIntro;
                _startScreenTimer = 0;
                _pressedKeys.Clear();
                _mouseLookActive = false;
                ShowGameCursor();
                return;
            }

            if (_startScreenStage == StartScreenStage.DayIntro && _startScreenTimer >= DayTitleScreenDuration)
            {
                StopStartScreenMusic();
                _startScreenStage = StartScreenStage.Game;
                _startScreenTimer = 0;
                _pressedKeys.Clear();
                _mouseLookActive = true;
                HideGameCursor();
                CenterMouseForStableLook();
                _lastKnownGameDay = _game != null && _game.CurrentDay > 0 ? _game.CurrentDay : 1;
            }
        }

        private void DrawStartScreen(Graphics g)
        {
            using (SolidBrush black = new SolidBrush(Color.Black))
                g.FillRectangle(black, 0, 0, ClientSize.Width, ClientSize.Height);

            SmoothingMode oldSmoothing = g.SmoothingMode;
            InterpolationMode oldInterpolation = g.InterpolationMode;
            PixelOffsetMode oldPixelOffset = g.PixelOffsetMode;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            try
            {
                if (_startScreenStage == StartScreenStage.ContinueMenu)
                {
                    DrawContinueMenu(g);
                    return;
                }

                if (_startScreenStage == StartScreenStage.Logo)
                {
                    DrawCafeMarkulLogo(g);
                    return;
                }

                if (_startScreenStage == StartScreenStage.Menu)
                {
                    DrawStartMenu(g);
                    return;
                }

                if (_startScreenStage == StartScreenStage.DayIntro)
                {
                    DrawDayTitleCard(g, "День 1", _startScreenTimer);
                    return;
                }

                DrawLoadingAnimation(g);
            }
            finally
            {
                g.SmoothingMode = oldSmoothing;
                g.InterpolationMode = oldInterpolation;
                g.PixelOffsetMode = oldPixelOffset;
            }
        }

        private Font CreateFittedFont(Graphics g, string text, FontStyle style, int maxWidth, int maxHeight, float maxSize)
        {
            float size = maxSize;
            if (size < 10f)
                size = 10f;

            Font font = new Font("Arial", size, style, GraphicsUnit.Pixel);
            while (size > 10f)
            {
                SizeF measured = g.MeasureString(text, font);
                if (measured.Width <= maxWidth && measured.Height <= maxHeight)
                    return font;

                font.Dispose();
                size -= 3f;
                font = new Font("Arial", size, style, GraphicsUnit.Pixel);
            }

            return font;
        }

        private void DrawCenteredString(Graphics g, string text, Font font, Brush brush, float centerX, float centerY)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, centerX - size.Width / 2f, centerY - size.Height / 2f);
        }

        private void DrawCafeMarkulLogo(Graphics g)
        {
            double fade = _startScreenTimer / 3.0;
            if (fade < 0) fade = 0;
            if (fade > 1) fade = 1;

            int alpha = (int)(fade * 255.0);
            string title = "Cafe Markul";

            using (Font font = CreateFittedFont(g, title, FontStyle.Bold, Math.Max(40, ClientSize.Width - 60), Math.Max(40, ClientSize.Height / 2), Math.Max(64, ClientSize.Width / 7f)))
            using (SolidBrush glow = new SolidBrush(Color.FromArgb((int)(alpha * 0.38), 100, 0, 0)))
            using (SolidBrush red = new SolidBrush(Color.FromArgb(alpha, 176, 0, 0)))
            {
                DrawCenteredString(g, title, font, glow, ClientSize.Width / 2f + 4f, ClientSize.Height / 2f + 5f);
                DrawCenteredString(g, title, font, red, ClientSize.Width / 2f, ClientSize.Height / 2f);
            }
        }

        private void DrawContinueMenu(Graphics g)
        {
            int buttonWidth = Math.Max(260, ClientSize.Width / 4);
            int buttonHeight = Math.Max(58, ClientSize.Height / 13);
            int buttonX = (ClientSize.Width - buttonWidth) / 2;
            int firstY = (ClientSize.Height - buttonHeight * 2 - 28) / 2;
            _continueButtonBounds = new Rectangle(buttonX, firstY, buttonWidth, buttonHeight);
            _newGameButtonBounds = new Rectangle(buttonX, firstY + buttonHeight + 28, buttonWidth, buttonHeight);

            DrawSaveChoiceButton(g, _continueButtonBounds, "Продолжить игру");
            DrawSaveChoiceButton(g, _newGameButtonBounds, "Начать сначала");
        }

        private void DrawSaveChoiceButton(Graphics g, Rectangle bounds, string text)
        {
            bool hover = bounds.Contains(PointToClient(Cursor.Position));
            Color buttonColor = hover ? Color.FromArgb(168, 38, 4, 8) : Color.FromArgb(202, 98, 24, 30);
            Color borderColor = hover ? Color.FromArgb(245, 170, 58, 66) : Color.FromArgb(226, 128, 36, 42);

            using (SolidBrush brush = new SolidBrush(buttonColor))
            using (Pen border = new Pen(borderColor, 3f))
            {
                g.FillRectangle(brush, bounds);
                g.DrawRectangle(border, bounds);
            }

            using (Font buttonFont = new Font("Arial", Math.Max(22f, bounds.Height * 0.42f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush white = new SolidBrush(Color.White))
                DrawCenteredString(g, text, buttonFont, white, bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        }

        private void DrawStartMenu(Graphics g)
        {
            string title = "Cafe Markul";
            using (Font titleFont = CreateFittedFont(g, title, FontStyle.Bold, Math.Max(40, ClientSize.Width - 80), Math.Max(40, ClientSize.Height / 3), Math.Max(54, ClientSize.Width / 9f)))
            using (SolidBrush red = new SolidBrush(Color.FromArgb(210, 150, 0, 0)))
                DrawCenteredString(g, title, titleFont, red, ClientSize.Width / 2f, ClientSize.Height * 0.32f);

            int buttonWidth = Math.Max(180, ClientSize.Width / 5);
            int buttonHeight = Math.Max(58, ClientSize.Height / 13);
            int buttonX = (ClientSize.Width - buttonWidth) / 2;
            int buttonY = (ClientSize.Height - buttonHeight) / 2 + ClientSize.Height / 8;
            _startButtonBounds = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

            bool hover = _startButtonBounds.Contains(PointToClient(Cursor.Position));
            Color buttonColor = hover ? Color.FromArgb(228, 52, 6, 10) : Color.FromArgb(202, 98, 24, 30);
            Color borderColor = hover ? Color.FromArgb(245, 170, 58, 66) : Color.FromArgb(226, 128, 36, 42);

            using (SolidBrush brush = new SolidBrush(buttonColor))
            using (Pen border = new Pen(borderColor, 3f))
            {
                g.FillRectangle(brush, _startButtonBounds);
                g.DrawRectangle(border, _startButtonBounds);
            }

            using (Font buttonFont = new Font("Arial", Math.Max(22f, buttonHeight * 0.42f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush white = new SolidBrush(Color.White))
                DrawCenteredString(g, "Начать", buttonFont, white, _startButtonBounds.Left + _startButtonBounds.Width / 2f, _startButtonBounds.Top + _startButtonBounds.Height / 2f);
        }

        private void DrawLoadingAnimation(Graphics g)
        {
            double seconds = _startScreenTimer;
            if (seconds < 0) seconds = 0;
            if (seconds > LoadingScreenDuration) seconds = LoadingScreenDuration;

            using (Font loadingFont = new Font("Arial", Math.Max(36f, ClientSize.Height / 12.5f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font subFont = new Font("Arial", Math.Max(14f, ClientSize.Height / 38f), FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush white = new SolidBrush(Color.White))
            using (SolidBrush dimRed = new SolidBrush(Color.FromArgb(190, 135, 18, 24)))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(170, 214, 214, 214)))
            {
                DrawCenteredString(g, "Loading", loadingFont, dimRed, ClientSize.Width / 2f + 2f, ClientSize.Height * 0.13f + 3f);
                DrawCenteredString(g, "Loading", loadingFont, white, ClientSize.Width / 2f, ClientSize.Height * 0.13f);
                DrawCenteredString(g, "Подготовка...", subFont, subBrush, ClientSize.Width / 2f, ClientSize.Height * 0.185f);
            }

            int animLeft = (int)(ClientSize.Width * 0.045);
            int animTop = (int)(ClientSize.Height * 0.24);
            int animWidth = (int)(ClientSize.Width * 0.91);
            int animHeight = (int)(ClientSize.Height * 0.64);
            int groundY = animTop + (int)(animHeight * 0.80);
            float collisionX = animLeft + animWidth * 0.82f;
            float rightExitX = animLeft + animWidth + 130f;
            int floorLineY = groundY + 18;
            int characterGroundY = floorLineY;

            DrawLoadingBackdrop(g, animLeft, animTop, animWidth, animHeight, groundY);

            GraphicsState clipState = g.Save();
            g.SetClip(new Rectangle(animLeft + 2, animTop + 2, animWidth - 4, animHeight - 4));

            float womanStartX = animLeft - 160f;
            float attackerStartX = animLeft + animWidth + 165f;
            double attackerVisibleFrom = 9.0;
            double killMoment = 11.80;
            double dragStart = 13.05;

            double womanMove = Clamp01(seconds / killMoment);
            double attackerMove = Clamp01((seconds - attackerVisibleFrom) / (killMoment - attackerVisibleFrom));
            float womanX = LerpFloat(womanStartX, collisionX, womanMove);
            float attackerX = LerpFloat(attackerStartX, collisionX + 14f, attackerMove);

            double fallProgress = Clamp01((seconds - killMoment) / 0.92);
            double coffeeFlightProgress = Clamp01((seconds - killMoment) / 1.18);
            double dragProgress = Clamp01((seconds - dragStart) / (LoadingScreenDuration - dragStart));
            double bloodProgress = Clamp01((seconds - 11.94) / 1.55);
            double floodProgress = Clamp01((seconds - 12.90) / 5.10);
            bool attackerVisible = seconds >= attackerVisibleFrom;
            bool killed = seconds >= killMoment;
            bool dragging = seconds >= dragStart;

            float visibleWomanX = dragging ? LerpFloat(collisionX - 18f, rightExitX - 104f, dragProgress) : Math.Min(womanX, collisionX);
            DrawShadowEllipse(g, visibleWomanX + 4f, characterGroundY + 2f, 88f, 12f, 42);
            if (attackerVisible)
            {
                float attackerShadowX = dragging ? visibleWomanX + 154f : Math.Max(attackerX, collisionX + 8f);
                DrawShadowEllipse(g, attackerShadowX + 4f, characterGroundY + 2f, 82f, 12f, 40);
            }

            if (!killed)
            {
                DrawDetailedWomanWithCoffee(g, Math.Min(womanX, collisionX), characterGroundY, seconds * (Math.PI / 0.7), true);
            }
            else if (!dragging)
            {
                DrawVictimOnGround(g, collisionX + 10f, floorLineY, fallProgress, false, 0.0);
            }

            if (attackerVisible && !dragging)
            {
                double knifeDraw = Clamp01((seconds - (killMoment - 0.08)) / 0.06);
                double stabThrust = Clamp01((seconds - killMoment) / 0.10);
                DrawDetailedAttackerHuman(g, Math.Max(attackerX, collisionX + 10f), characterGroundY, seconds * (Math.PI / 0.7), knifeDraw, stabThrust, killed);
            }

            if (seconds >= killMoment)
            {
                if (coffeeFlightProgress < 1.0)
                    DrawCoffeeSplashDetailed(g, collisionX + 42f, groundY - 68f, coffeeFlightProgress);
                DrawFlyingCupRight(g, collisionX + 58f, groundY - 34f, coffeeFlightProgress, floorLineY);
            }

            if (dragging)
            {
                float draggedVictimX = LerpFloat(collisionX - 18f, rightExitX - 108f, dragProgress);
                float draggingAttackerX = draggedVictimX + 154f;
                DrawVictimOnGround(g, draggedVictimX, floorLineY, 1.0, true, dragProgress);
                DrawDraggingAttacker(g, draggingAttackerX, characterGroundY, seconds * (Math.PI / 0.7), dragProgress);
            }

            if (bloodProgress > 0)
                DrawBloodPoolDetailed(g, collisionX + 22f, floorLineY + 1f, bloodProgress, dragging ? dragProgress : 0.0);

            g.Restore(clipState);

            if (floodProgress > 0)
                DrawBloodFlood(g, floodProgress, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void DrawLoadingBackdrop(Graphics g, int left, int top, int width, int height, int groundY)
        {
            using (LinearGradientBrush wall = new LinearGradientBrush(
                new Rectangle(left, top, width, height),
                Color.FromArgb(38, 42, 44, 52),
                Color.FromArgb(12, 10, 10, 12),
                90f))
            using (LinearGradientBrush floor = new LinearGradientBrush(
                new Rectangle(left, groundY - 10, width, Math.Max(18, top + height - (groundY - 10))),
                Color.FromArgb(48, 74, 16, 20),
                Color.FromArgb(16, 24, 6, 8),
                90f))
            using (SolidBrush vignette = new SolidBrush(Color.FromArgb(18, 0, 0, 0)))
            using (Pen border = new Pen(Color.FromArgb(86, 170, 26, 34), 3f))
            using (Pen floorLine = new Pen(Color.FromArgb(118, 116, 34, 40), 5f))
            using (Pen tilePen = new Pen(Color.FromArgb(28, 210, 210, 214), 1f))
            using (SolidBrush silhouette = new SolidBrush(Color.FromArgb(62, 20, 18, 24)))
            using (SolidBrush lampGlow = new SolidBrush(Color.FromArgb(38, 180, 24, 32)))
            {
                g.FillRectangle(wall, left, top, width, height);
                g.FillRectangle(floor, left, groundY + 18, width, Math.Max(18, top + height - (groundY + 18)));
                g.FillRectangle(vignette, left, top, width, height);
                g.DrawRectangle(border, left, top, width, height);
                g.DrawLine(floorLine, left, groundY + 18, left + width, groundY + 18);

                int tileTop = groundY + 18;
                for (int x = left + 24; x < left + width; x += 46)
                    g.DrawLine(tilePen, x, tileTop, x, top + height);
                for (int y = tileTop + 22; y < top + height; y += 24)
                    g.DrawLine(tilePen, left, y, left + width, y);

                g.FillRectangle(silhouette, left + width / 12, top + height / 2, width / 7, height / 4);
                g.FillRectangle(silhouette, left + width / 4, top + height / 2 + 20, width / 5, height / 5);
                g.FillRectangle(silhouette, left + (int)(width * 0.62), top + height / 2 + 14, width / 8, height / 4);

                g.FillEllipse(lampGlow, left + width / 2 - 90, top + 8, 180, 86);
                using (Pen cable = new Pen(Color.FromArgb(80, 50, 50, 56), 2f))
                using (SolidBrush lamp = new SolidBrush(Color.FromArgb(96, 120, 22, 28)))
                {
                    g.DrawLine(cable, left + width / 2, top, left + width / 2, top + 18);
                    g.FillEllipse(lamp, left + width / 2 - 18, top + 15, 36, 18);
                }
            }
        }

        private static PointF MakePoint(float x, float y)
        {
            return new PointF(x, y);
        }

        private static PointF BendJoint(PointF root, PointF end, float firstLen, float secondLen, float bendSign)
        {
            float dx = end.X - root.X;
            float dy = end.Y - root.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.001f)
                return new PointF(root.X, root.Y + firstLen * 0.6f);

            float maxReach = firstLen + secondLen - 0.5f;
            if (distance > maxReach)
                distance = maxReach;

            float minReach = Math.Abs(firstLen - secondLen) + 0.5f;
            if (distance < minReach)
                distance = minReach;

            float ux = dx / (float)Math.Sqrt(dx * dx + dy * dy);
            float uy = dy / (float)Math.Sqrt(dx * dx + dy * dy);
            float along = (firstLen * firstLen - secondLen * secondLen + distance * distance) / (2f * distance);
            float heightSq = Math.Max(0f, firstLen * firstLen - along * along);
            float height = (float)Math.Sqrt(heightSq);

            float px = -uy * bendSign;
            float py = ux * bendSign;
            return new PointF(root.X + ux * along + px * height, root.Y + uy * along + py * height);
        }

        private static PointF KeepEndInReach(PointF root, PointF wantedEnd, float maxReach)
        {
            float dx = wantedEnd.X - root.X;
            float dy = wantedEnd.Y - root.Y;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d <= maxReach || d < 0.001f)
                return wantedEnd;

            float scale = maxReach / d;
            return new PointF(root.X + dx * scale, root.Y + dy * scale);
        }

        private static PointF EnsureLeftSideFoot(PointF hip, float footX, float footY, float minSeparation, float maxReach)
        {
            float clampedX = Math.Min(footX, hip.X - minSeparation);
            return KeepEndInReach(hip, new PointF(clampedX, footY), maxReach);
        }

        private static PointF EnsureRightSideFoot(PointF hip, float footX, float footY, float minSeparation, float maxReach)
        {
            float clampedX = Math.Max(footX, hip.X + minSeparation);
            return KeepEndInReach(hip, new PointF(clampedX, footY), maxReach);
        }


        private void DrawDetailedWomanWithCoffee(Graphics g, float x, int groundY, double phase, bool smiling)
        {
            float headW = Math.Max(33f, ClientSize.Width * 0.018f);
            float headH = headW * 1.26f;
            float torsoTop = groundY - Math.Max(128f, ClientSize.Height * 0.212f);
            float headTop = torsoTop - headH * 0.38f;
            float shoulderY = torsoTop + 27f;
            float chestY = torsoTop + 46f;
            float waistY = torsoTop + 72f;
            float hipY = torsoTop + 104f;

            double leftPhase = Math.Sin(phase);
            double rightPhase = Math.Sin(phase + Math.PI);
            float leftStride = (float)(leftPhase * 7.0);
            float rightStride = (float)(rightPhase * 7.0);
            float leftLift = (float)(Math.Max(0.0, leftPhase) * 13.0);
            float rightLift = (float)(Math.Max(0.0, rightPhase) * 13.0);
            float leftArmSwing = (float)(-leftPhase * 8.0);
            float rightArmSwing = (float)(leftPhase * 4.2);

            using (SolidBrush skin = new SolidBrush(Color.FromArgb(244, 232, 214)))
            using (SolidBrush cheek = new SolidBrush(Color.FromArgb(80, 224, 145, 138)))
            using (SolidBrush hair = new SolidBrush(Color.FromArgb(232, 88, 54, 34)))
            using (SolidBrush hairDark = new SolidBrush(Color.FromArgb(220, 58, 34, 20)))
            using (SolidBrush shirt = new SolidBrush(Color.FromArgb(236, 132, 44, 54)))
            using (SolidBrush shirtShadow = new SolidBrush(Color.FromArgb(224, 100, 30, 40)))
            using (SolidBrush apron = new SolidBrush(Color.FromArgb(238, 234, 234, 236)))
            using (SolidBrush apronShade = new SolidBrush(Color.FromArgb(220, 198, 198, 202)))
            using (SolidBrush skirt = new SolidBrush(Color.FromArgb(236, 46, 50, 62)))
            using (SolidBrush tights = new SolidBrush(Color.FromArgb(230, 42, 34, 38)))
            using (SolidBrush shoe = new SolidBrush(Color.FromArgb(238, 18, 18, 20)))
            using (SolidBrush cup = new SolidBrush(Color.FromArgb(245, 238, 232, 224)))
            using (SolidBrush coffee = new SolidBrush(Color.FromArgb(228, 90, 52, 24)))
            using (SolidBrush lip = new SolidBrush(Color.FromArgb(220, 160, 82, 88)))
            using (Pen outline = new Pen(Color.FromArgb(220, 30, 16, 18), 2.0f))
            using (Pen facePen = new Pen(Color.FromArgb(188, 62, 30, 34), 1.4f))
            using (Pen browPen = new Pen(Color.FromArgb(190, 78, 40, 24), 1.7f))
            using (Pen lashPen = new Pen(Color.FromArgb(180, 32, 20, 18), 1.1f))
            using (Pen mouthPen = new Pen(lip.Color, 1.9f))
            using (Pen upperArmPen = new Pen(Color.FromArgb(236, 124, 40, 50), 8.6f))
            using (Pen forearmPen = new Pen(Color.FromArgb(236, 214, 198, 190), 6.9f))
            using (Pen thighPen = new Pen(Color.FromArgb(226, 54, 40, 44), 8.9f))
            using (Pen calfPen = new Pen(Color.FromArgb(236, 214, 198, 190), 6.6f))
            {
                PointF[] backHair = new PointF[]
                {
                    new PointF(x - headW * 0.52f, headTop + headH * 0.16f),
                    new PointF(x - headW * 0.74f, headTop + headH * 0.58f),
                    new PointF(x - headW * 0.68f, waistY + 10f),
                    new PointF(x - 7f, hipY - 2f),
                    new PointF(x + headW * 0.26f, waistY + 6f),
                    new PointF(x + headW * 0.38f, headTop + headH * 0.30f)
                };
                g.FillPolygon(hairDark, backHair);

                PointF[] facePoints = new PointF[]
                {
                    new PointF(x - headW * 0.28f, headTop + 2f),
                    new PointF(x + headW * 0.28f, headTop + 2f),
                    new PointF(x + headW * 0.43f, headTop + headH * 0.27f),
                    new PointF(x + headW * 0.38f, headTop + headH * 0.58f),
                    new PointF(x + headW * 0.24f, headTop + headH * 0.82f),
                    new PointF(x + headW * 0.05f, headTop + headH * 0.98f),
                    new PointF(x - headW * 0.05f, headTop + headH * 0.98f),
                    new PointF(x - headW * 0.24f, headTop + headH * 0.82f),
                    new PointF(x - headW * 0.38f, headTop + headH * 0.58f),
                    new PointF(x - headW * 0.43f, headTop + headH * 0.27f)
                };
                using (GraphicsPath facePath = new GraphicsPath())
                {
                    facePath.AddPolygon(facePoints);
                    g.FillPath(skin, facePath);
                    g.DrawPath(outline, facePath);
                }

                g.FillEllipse(skin, x - headW * 0.56f, headTop + headH * 0.40f, 5f, 8f);
                g.FillEllipse(skin, x + headW * 0.44f, headTop + headH * 0.40f, 5f, 8f);
                g.FillPie(hair, x - headW * 0.64f, headTop - 5f, headW * 1.28f, headH * 0.92f, 180, 180);

                PointF[] leftBang = new PointF[]
                {
                    new PointF(x - headW * 0.40f, headTop + headH * 0.16f),
                    new PointF(x - headW * 0.70f, headTop + headH * 0.56f),
                    new PointF(x - headW * 0.18f, headTop + headH * 0.40f)
                };
                PointF[] rightBang = new PointF[]
                {
                    new PointF(x + headW * 0.28f, headTop + headH * 0.12f),
                    new PointF(x + headW * 0.66f, headTop + headH * 0.44f),
                    new PointF(x + headW * 0.10f, headTop + headH * 0.36f)
                };
                PointF[] leftPony = new PointF[]
                {
                    new PointF(x - headW * 0.26f, headTop + headH * 0.38f),
                    new PointF(x - headW * 0.58f, shoulderY + 4f),
                    new PointF(x - headW * 0.84f, chestY + 6f),
                    new PointF(x - headW * 0.74f, waistY + 2f),
                    new PointF(x - headW * 0.30f, chestY + 14f)
                };
                PointF[] rightPony = new PointF[]
                {
                    new PointF(x + headW * 0.22f, headTop + headH * 0.38f),
                    new PointF(x + headW * 0.54f, shoulderY + 6f),
                    new PointF(x + headW * 0.80f, chestY + 10f),
                    new PointF(x + headW * 0.68f, waistY + 4f),
                    new PointF(x + headW * 0.26f, chestY + 15f)
                };
                g.FillPolygon(hair, leftBang);
                g.FillPolygon(hairDark, rightBang);
                g.FillPolygon(hair, leftPony);
                g.FillPolygon(hairDark, rightPony);

                PointF[] neck = new PointF[]
                {
                    new PointF(x - 1.8f, torsoTop + 13f),
                    new PointF(x + 1.8f, torsoTop + 13f),
                    new PointF(x + 1.5f, shoulderY - 12f),
                    new PointF(x - 1.5f, shoulderY - 12f)
                };
                g.FillPolygon(skin, neck);

                g.FillEllipse(Brushes.White, x - 10.4f, headTop + headH * 0.39f, 7.2f, 4.8f);
                g.FillEllipse(Brushes.White, x + 3.2f, headTop + headH * 0.39f, 7.2f, 4.8f);
                g.FillEllipse(Brushes.DarkSlateGray, x - 8.1f, headTop + headH * 0.42f, 3.0f, 3.0f);
                g.FillEllipse(Brushes.DarkSlateGray, x + 5.5f, headTop + headH * 0.42f, 3.0f, 3.0f);
                g.FillEllipse(Brushes.Black, x - 7.1f, headTop + headH * 0.43f, 1.6f, 1.6f);
                g.FillEllipse(Brushes.Black, x + 6.5f, headTop + headH * 0.43f, 1.6f, 1.6f);
                g.DrawArc(browPen, x - 12.2f, headTop + headH * 0.25f, 9f, 4.2f, 196, 124);
                g.DrawArc(browPen, x + 2.2f, headTop + headH * 0.25f, 9f, 4.2f, 220, 124);
                g.DrawLine(lashPen, x - 3.2f, headTop + headH * 0.39f, x - 1.4f, headTop + headH * 0.36f);
                g.DrawLine(lashPen, x - 2.8f, headTop + headH * 0.42f, x - 1.0f, headTop + headH * 0.41f);
                g.DrawLine(lashPen, x - 3.8f, headTop + headH * 0.41f, x - 1.8f, headTop + headH * 0.44f);
                g.DrawLine(lashPen, x + 11.0f, headTop + headH * 0.39f, x + 12.8f, headTop + headH * 0.36f);
                g.DrawLine(lashPen, x + 10.6f, headTop + headH * 0.42f, x + 12.3f, headTop + headH * 0.41f);
                g.DrawLine(lashPen, x + 10.2f, headTop + headH * 0.41f, x + 12.0f, headTop + headH * 0.44f);
                g.FillEllipse(cheek, x - 13f, headTop + headH * 0.58f, 5.6f, 3.2f);
                g.FillEllipse(cheek, x + 7.6f, headTop + headH * 0.58f, 5.6f, 3.2f);
                g.DrawLine(facePen, x + 0.2f, headTop + headH * 0.45f, x - 0.8f, headTop + headH * 0.64f);
                g.DrawArc(facePen, x - 2.8f, headTop + headH * 0.60f, 5.4f, 3.8f, 10, 154);
                if (smiling)
                {
                    g.DrawArc(mouthPen, x - 8f, headTop + headH * 0.70f, 16f, 7.5f, 10, 160);
                    g.DrawArc(facePen, x - 6f, headTop + headH * 0.68f, 12f, 5f, 18, 144);
                }
                else
                {
                    g.DrawArc(mouthPen, x - 8f, headTop + headH * 0.72f, 16f, 7f, 188, 164);
                }

                PointF[] collar = new PointF[]
                {
                    new PointF(x - 12f, shoulderY + 3f),
                    new PointF(x, shoulderY + 14f),
                    new PointF(x + 12f, shoulderY + 3f),
                    new PointF(x + 6f, shoulderY + 16f),
                    new PointF(x - 6f, shoulderY + 16f)
                };
                PointF[] bust = new PointF[]
                {
                    new PointF(x - 32f, shoulderY),
                    new PointF(x + 32f, shoulderY),
                    new PointF(x + 26f, chestY),
                    new PointF(x + 18f, chestY + 9f),
                    new PointF(x - 18f, chestY + 9f),
                    new PointF(x - 26f, chestY)
                };
                PointF[] waist = new PointF[]
                {
                    new PointF(x - 18f, chestY + 9f),
                    new PointF(x + 18f, chestY + 9f),
                    new PointF(x + 15f, waistY),
                    new PointF(x - 15f, waistY)
                };
                PointF[] pelvis = new PointF[]
                {
                    new PointF(x - 20f, waistY - 1f),
                    new PointF(x + 20f, waistY - 1f),
                    new PointF(x + 30f, hipY),
                    new PointF(x - 30f, hipY)
                };
                g.FillPolygon(shirt, bust);
                g.FillPolygon(shirtShadow, waist);
                g.FillPolygon(skirt, pelvis);
                g.DrawPolygon(outline, bust);
                g.DrawPolygon(outline, waist);
                g.DrawPolygon(outline, pelvis);
                g.FillPolygon(apron, new PointF[]
                {
                    new PointF(x - 14f, shoulderY + 12f),
                    new PointF(x + 14f, shoulderY + 12f),
                    new PointF(x + 12f, hipY - 6f),
                    new PointF(x - 12f, hipY - 6f)
                });
                g.FillRectangle(apronShade, x + 7f, shoulderY + 14f, 4f, hipY - shoulderY - 22f);
                g.DrawPolygon(outline, new PointF[]
                {
                    new PointF(x - 14f, shoulderY + 12f),
                    new PointF(x + 14f, shoulderY + 12f),
                    new PointF(x + 12f, hipY - 6f),
                    new PointF(x - 12f, hipY - 6f)
                });
                g.DrawLine(outline, x - 10f, shoulderY + 6f, x - 18f, shoulderY + 24f);
                g.DrawLine(outline, x + 10f, shoulderY + 6f, x + 18f, shoulderY + 24f);
                g.DrawPolygon(outline, collar);
                for (int i = 0; i < 3; i++)
                    g.FillEllipse(shirtShadow, x - 3f, shoulderY + 20f + i * 12f, 3.2f, 3.2f);
                g.DrawLine(outline, x - 12f, waistY - 6f, x + 12f, waistY - 6f);
                g.DrawLine(outline, x - 14f, hipY - 10f, x + 14f, hipY - 10f);

                PointF leftShoulder = new PointF(x - 28f, shoulderY + 7f);
                PointF rightShoulder = new PointF(x + 28f, shoulderY + 7f);
                PointF leftHand = KeepEndInReach(leftShoulder, new PointF(x - 33f + leftArmSwing * 0.30f, shoulderY + 60f + leftArmSwing * 0.14f), 59f);
                PointF rightHand = KeepEndInReach(rightShoulder, new PointF(x + 50f + rightArmSwing * 0.18f, shoulderY + 56f + rightArmSwing * 0.10f), 61f);
                PointF leftElbow = BendJoint(leftShoulder, leftHand, 28f, 31f, -1f);
                PointF rightElbow = BendJoint(rightShoulder, rightHand, 29f, 31f, 1f);
                g.DrawLine(upperArmPen, leftShoulder, leftElbow);
                g.DrawLine(upperArmPen, rightShoulder, rightElbow);
                g.DrawLine(forearmPen, leftElbow, leftHand);
                g.DrawLine(forearmPen, rightElbow, rightHand);
                g.FillEllipse(skin, leftHand.X - 5.2f, leftHand.Y - 5.2f, 10.4f, 10.4f);
                g.FillEllipse(skin, rightHand.X - 5.2f, rightHand.Y - 5.2f, 10.4f, 10.4f);
                g.FillEllipse(shirtShadow, leftShoulder.X - 4.5f, leftShoulder.Y - 4.5f, 9f, 9f);
                g.FillEllipse(shirtShadow, rightShoulder.X - 4.5f, rightShoulder.Y - 4.5f, 9f, 9f);
                g.FillEllipse(skin, leftElbow.X - 3.4f, leftElbow.Y - 3.4f, 6.8f, 6.8f);
                g.FillEllipse(skin, rightElbow.X - 3.4f, rightElbow.Y - 3.4f, 6.8f, 6.8f);

                GraphicsState cupState = g.Save();
                g.TranslateTransform(rightHand.X + 12f, rightHand.Y + 1f);
                g.RotateTransform(-9f + rightArmSwing * 0.24f);
                g.FillRectangle(cup, -10f, -18f, 20f, 26f);
                g.FillRectangle(coffee, -8f, -16f, 16f, 5f);
                g.FillRectangle(Brushes.Gainsboro, -11f, -20f, 22f, 5f);
                using (SolidBrush label = new SolidBrush(Color.FromArgb(220, 164, 118, 82)))
                    g.FillRectangle(label, -8f, -3f, 16f, 9f);
                g.DrawRectangle(outline, -10f, -18f, 20f, 26f);
                g.Restore(cupState);

                PointF leftHip = new PointF(x - 13f, hipY);
                PointF rightHip = new PointF(x + 13f, hipY);
                PointF leftFoot = EnsureLeftSideFoot(leftHip, x - 18f + leftStride, groundY - leftLift * 0.10f, 5.5f, 74f);
                PointF rightFoot = EnsureRightSideFoot(rightHip, x + 18f + rightStride, groundY - rightLift * 0.10f, 5.5f, 74f);
                PointF leftKnee = BendJoint(leftHip, leftFoot, 34f, 40f, -1f);
                PointF rightKnee = BendJoint(rightHip, rightFoot, 34f, 40f, -1f);
                g.DrawLine(thighPen, leftHip, leftKnee);
                g.DrawLine(thighPen, rightHip, rightKnee);
                g.DrawLine(calfPen, leftKnee, leftFoot);
                g.DrawLine(calfPen, rightKnee, rightFoot);
                g.FillEllipse(tights, leftKnee.X - 3f, leftKnee.Y - 3f, 6f, 6f);
                g.FillEllipse(tights, rightKnee.X - 3f, rightKnee.Y - 3f, 6f, 6f);
                g.FillRectangle(tights, leftFoot.X - 2f, leftFoot.Y - 9f, 4f, 6f);
                g.FillRectangle(tights, rightFoot.X - 2f, rightFoot.Y - 9f, 4f, 6f);
                g.FillEllipse(shoe, leftFoot.X - 5f, leftFoot.Y - 4f, 18f, 7f);
                g.FillEllipse(shoe, rightFoot.X - 5f, rightFoot.Y - 4f, 18f, 7f);
            }
        }


        private void DrawDetailedAttackerHuman(Graphics g, float x, int groundY, double phase, double knifeDraw, double stabThrust, bool holdingPosition)
        {
            float headW = Math.Max(33f, ClientSize.Width * 0.018f);
            float headH = headW * 1.24f;
            float torsoTop = groundY - Math.Max(132f, ClientSize.Height * 0.216f);
            float headTop = torsoTop - headH * 0.98f;
            float shoulderY = torsoTop + 29f;
            float chestY = torsoTop + 48f;
            float waistY = torsoTop + 76f;
            float hipY = torsoTop + 106f;

            float movementScale = holdingPosition ? 0.18f : 1.0f;
            double leftPhase = Math.Sin(phase);
            double rightPhase = Math.Sin(phase + Math.PI);
            float leftStride = (float)(leftPhase * 6.5f * movementScale);
            float rightStride = (float)(rightPhase * 6.5f * movementScale);
            float leftLift = (float)(Math.Max(0.0, leftPhase) * 10.0 * movementScale);
            float rightLift = (float)(Math.Max(0.0, rightPhase) * 10.0 * movementScale);
            float leftArmSwing = (float)(-leftPhase * 6.0 * movementScale);
            float rightArmReach = 16f + (float)(knifeDraw * 12.0 + stabThrust * 20.0);

            using (SolidBrush skin = new SolidBrush(Color.FromArgb(230, 202, 190, 182)))
            using (SolidBrush hair = new SolidBrush(Color.FromArgb(236, 20, 20, 24)))
            using (SolidBrush hoodie = new SolidBrush(Color.FromArgb(238, 46, 48, 58)))
            using (SolidBrush hoodieDark = new SolidBrush(Color.FromArgb(226, 34, 36, 46)))
            using (SolidBrush hood = new SolidBrush(Color.FromArgb(222, 30, 32, 40)))
            using (SolidBrush pants = new SolidBrush(Color.FromArgb(236, 24, 26, 32)))
            using (SolidBrush shoe = new SolidBrush(Color.FromArgb(240, 10, 10, 14)))
            using (SolidBrush handle = new SolidBrush(Color.FromArgb(236, 76, 46, 22)))
            using (SolidBrush blade = new SolidBrush(Color.FromArgb(240, 212, 216, 222)))
            using (Pen outline = new Pen(Color.FromArgb(226, 8, 8, 10), 2.0f))
            using (Pen facePen = new Pen(Color.FromArgb(180, 36, 20, 20), 1.4f))
            using (Pen browPen = new Pen(Color.FromArgb(200, 24, 18, 18), 1.8f))
            using (Pen sleevePen = new Pen(Color.FromArgb(232, 60, 62, 74), 8.7f))
            using (Pen forearmPen = new Pen(Color.FromArgb(220, 196, 182, 176), 6.5f))
            using (Pen thighPen = new Pen(Color.FromArgb(232, 46, 48, 58), 8.9f))
            using (Pen calfPen = new Pen(Color.FromArgb(214, 188, 176, 170), 6.2f))
            {
                PointF[] facePoints = new PointF[]
                {
                    new PointF(x - headW * 0.26f, headTop + 2f),
                    new PointF(x + headW * 0.26f, headTop + 2f),
                    new PointF(x + headW * 0.42f, headTop + headH * 0.28f),
                    new PointF(x + headW * 0.36f, headTop + headH * 0.60f),
                    new PointF(x + headW * 0.22f, headTop + headH * 0.84f),
                    new PointF(x + headW * 0.04f, headTop + headH * 0.98f),
                    new PointF(x - headW * 0.04f, headTop + headH * 0.98f),
                    new PointF(x - headW * 0.22f, headTop + headH * 0.84f),
                    new PointF(x - headW * 0.36f, headTop + headH * 0.60f),
                    new PointF(x - headW * 0.42f, headTop + headH * 0.28f)
                };
                using (GraphicsPath facePath = new GraphicsPath())
                {
                    facePath.AddPolygon(facePoints);
                    g.FillPath(skin, facePath);
                    g.DrawPath(outline, facePath);
                }
                g.FillEllipse(skin, x - headW * 0.56f, headTop + headH * 0.42f, 5f, 8f);
                g.FillEllipse(skin, x + headW * 0.44f, headTop + headH * 0.42f, 5f, 8f);
                g.FillPie(hair, x - headW * 0.60f, headTop - 3f, headW * 1.20f, headH * 0.80f, 180, 180);
                g.FillRectangle(skin, x - 4f, torsoTop - 2f, 8f, 6f);
                g.FillEllipse(Brushes.White, x - 10.0f, headTop + headH * 0.40f, 6.8f, 4.8f);
                g.FillEllipse(Brushes.White, x + 3.2f, headTop + headH * 0.40f, 6.8f, 4.8f);
                g.FillEllipse(Brushes.DimGray, x - 7.5f, headTop + headH * 0.43f, 2.8f, 2.8f);
                g.FillEllipse(Brushes.DimGray, x + 5.8f, headTop + headH * 0.43f, 2.8f, 2.8f);
                g.FillEllipse(Brushes.Black, x - 6.5f, headTop + headH * 0.44f, 1.5f, 1.5f);
                g.FillEllipse(Brushes.Black, x + 6.8f, headTop + headH * 0.44f, 1.5f, 1.5f);
                g.DrawArc(browPen, x - 12.4f, headTop + headH * 0.28f, 9.2f, 4.2f, 190, 120);
                g.DrawArc(browPen, x + 2.0f, headTop + headH * 0.28f, 9.2f, 4.2f, 230, 120);
                g.DrawLine(facePen, x + 0.2f, headTop + headH * 0.48f, x - 0.8f, headTop + headH * 0.66f);
                g.DrawArc(facePen, x - 3f, headTop + headH * 0.63f, 5.4f, 3.8f, 8, 156);
                g.DrawArc(facePen, x - 9f, headTop + headH * 0.75f, 18f, 8f, 6, 168);
                g.DrawLine(facePen, x - 5f, headTop + headH * 0.80f, x - 2f, headTop + headH * 0.77f);
                g.DrawLine(facePen, x - 1f, headTop + headH * 0.81f, x + 2f, headTop + headH * 0.77f);
                g.DrawLine(facePen, x + 3f, headTop + headH * 0.80f, x + 6f, headTop + headH * 0.77f);

                PointF[] hoodPoly = new PointF[]
                {
                    new PointF(x - 30f, shoulderY + 2f), new PointF(x - 13f, torsoTop + 2f),
                    new PointF(x + 12f, torsoTop + 8f), new PointF(x + 29f, shoulderY + 7f),
                    new PointF(x + 20f, shoulderY + 20f), new PointF(x - 22f, shoulderY + 17f)
                };
                PointF[] upperTorso = new PointF[]
                {
                    new PointF(x - 31f, shoulderY), new PointF(x + 31f, shoulderY),
                    new PointF(x + 25f, chestY), new PointF(x + 20f, waistY),
                    new PointF(x - 20f, waistY), new PointF(x - 25f, chestY)
                };
                PointF[] lowerTorso = new PointF[]
                {
                    new PointF(x - 18f, waistY - 1f), new PointF(x + 18f, waistY - 1f),
                    new PointF(x + 20f, hipY), new PointF(x - 20f, hipY)
                };
                g.FillPolygon(hood, hoodPoly);
                g.FillPolygon(hoodie, upperTorso);
                g.FillPolygon(hoodieDark, lowerTorso);
                g.DrawPolygon(outline, upperTorso);
                g.DrawPolygon(outline, lowerTorso);
                g.FillRectangle(hoodieDark, x - 4f, shoulderY + 4f, 8f, hipY - shoulderY - 7f);
                g.DrawLine(outline, x - 13f, shoulderY + 18f, x + 13f, shoulderY + 18f);
                g.DrawLine(outline, x - 16f, waistY - 3f, x + 16f, waistY - 3f);
                g.FillRectangle(pants, x - 19f, hipY - 2f, 38f, groundY - hipY + 2f);
                g.DrawLine(outline, x, hipY, x, groundY - 2f);

                PointF leftShoulder = new PointF(x - 28f, shoulderY + 7f);
                PointF rightShoulder = new PointF(x + 28f, shoulderY + 7f);
                PointF leftHand = KeepEndInReach(leftShoulder, new PointF(x - 28f + leftArmSwing * 0.26f, shoulderY + 61f + leftArmSwing * 0.16f), 58f);
                PointF rightHand = KeepEndInReach(rightShoulder, new PointF(x + 1f - rightArmReach, shoulderY + 37f + (float)(stabThrust * 9.0)), 57f);
                PointF leftElbow = BendJoint(leftShoulder, leftHand, 28f, 30f, -1f);
                PointF rightElbow = BendJoint(rightShoulder, rightHand, 28f, 30f, 1f);
                g.DrawLine(sleevePen, leftShoulder, leftElbow);
                g.DrawLine(sleevePen, rightShoulder, rightElbow);
                g.DrawLine(forearmPen, leftElbow, leftHand);
                g.DrawLine(forearmPen, rightElbow, rightHand);
                g.FillEllipse(skin, leftHand.X - 5f, leftHand.Y - 5f, 10f, 10f);
                g.FillEllipse(skin, rightHand.X - 5f, rightHand.Y - 5f, 10f, 10f);
                g.FillEllipse(hoodieDark, leftShoulder.X - 4.5f, leftShoulder.Y - 4.5f, 9f, 9f);
                g.FillEllipse(hoodieDark, rightShoulder.X - 4.5f, rightShoulder.Y - 4.5f, 9f, 9f);
                g.FillEllipse(skin, leftElbow.X - 3.4f, leftElbow.Y - 3.4f, 6.8f, 6.8f);
                g.FillEllipse(skin, rightElbow.X - 3.4f, rightElbow.Y - 3.4f, 6.8f, 6.8f);

                if (knifeDraw > 0.01)
                {
                    GraphicsState knifeState = g.Save();
                    g.TranslateTransform(rightHand.X + 2f, rightHand.Y + 2f);
                    g.RotateTransform(-10f + (float)(stabThrust * 24.0));
                    g.FillRectangle(handle, -2f, -3f, 14f, 6f);
                    PointF[] knife = new PointF[] { new PointF(10f, -3f), new PointF(40f, 2f), new PointF(10f, 6f) };
                    g.FillPolygon(blade, knife);
                    g.DrawPolygon(outline, knife);
                    g.Restore(knifeState);
                }

                PointF leftHip = new PointF(x - 12f, hipY);
                PointF rightHip = new PointF(x + 12f, hipY);
                PointF leftFoot = EnsureLeftSideFoot(leftHip, x - 17f + leftStride, groundY - leftLift * 0.08f, 5.2f, 74f);
                PointF rightFoot = EnsureRightSideFoot(rightHip, x + 17f + rightStride, groundY - rightLift * 0.08f, 5.2f, 74f);
                PointF leftKnee = BendJoint(leftHip, leftFoot, 35f, 40f, 1f);
                PointF rightKnee = BendJoint(rightHip, rightFoot, 35f, 40f, 1f);
                g.DrawLine(thighPen, leftHip, leftKnee);
                g.DrawLine(thighPen, rightHip, rightKnee);
                g.DrawLine(calfPen, leftKnee, leftFoot);
                g.DrawLine(calfPen, rightKnee, rightFoot);
                g.FillEllipse(pants, leftKnee.X - 3.2f, leftKnee.Y - 3.2f, 6.4f, 6.4f);
                g.FillEllipse(pants, rightKnee.X - 3.2f, rightKnee.Y - 3.2f, 6.4f, 6.4f);
                g.FillRectangle(pants, leftFoot.X - 2.2f, leftFoot.Y - 9f, 4.4f, 6f);
                g.FillRectangle(pants, rightFoot.X - 2.2f, rightFoot.Y - 9f, 4.4f, 6f);
                g.FillEllipse(shoe, leftFoot.X - 5f, leftFoot.Y - 4f, 18f, 7f);
                g.FillEllipse(shoe, rightFoot.X - 5f, rightFoot.Y - 4f, 18f, 7f);
            }
        }


        private void DrawVictimOnGround(Graphics g, float x, int groundY, double fallProgress, bool beingDragged, double dragProgress)
        {
            using (SolidBrush skin = new SolidBrush(Color.FromArgb(244, 232, 214)))
            using (SolidBrush hair = new SolidBrush(Color.FromArgb(232, 86, 54, 34)))
            using (SolidBrush darkHair = new SolidBrush(Color.FromArgb(214, 58, 34, 20)))
            using (SolidBrush shirt = new SolidBrush(Color.FromArgb(236, 132, 44, 54)))
            using (SolidBrush shirtShadow = new SolidBrush(Color.FromArgb(224, 100, 30, 40)))
            using (SolidBrush apron = new SolidBrush(Color.FromArgb(236, 232, 232, 232)))
            using (SolidBrush skirt = new SolidBrush(Color.FromArgb(236, 46, 50, 62)))
            using (SolidBrush tights = new SolidBrush(Color.FromArgb(230, 42, 34, 38)))
            using (SolidBrush shoe = new SolidBrush(Color.FromArgb(238, 18, 18, 20)))
            using (Pen outline = new Pen(Color.FromArgb(220, 30, 16, 18), 1.9f))
            using (Pen armPen = new Pen(Color.FromArgb(236, 214, 198, 190), 6.8f))
            using (Pen upperArmPen = new Pen(Color.FromArgb(236, 124, 40, 50), 8.2f))
            using (Pen legPen = new Pen(Color.FromArgb(226, 54, 40, 44), 8.6f))
            using (Pen calfPen = new Pen(Color.FromArgb(236, 214, 198, 190), 6.2f))
            using (Pen facePen = new Pen(Color.FromArgb(186, 86, 28, 32), 1.5f))
            using (Pen browPen = new Pen(Color.FromArgb(188, 92, 36, 28), 1.6f))
            {
                if (fallProgress < 0.98)
                {
                    GraphicsState state = g.Save();
                    float bodyY = groundY - 6f - (float)(16.0 * (1.0 - fallProgress));
                    float bodyRotation = 8f + (float)(fallProgress * 82.0);
                    g.TranslateTransform(x, bodyY);
                    g.RotateTransform(bodyRotation);

                    PointF[] face = new PointF[]
                    {
                        new PointF(34f, -72f), new PointF(55f, -72f), new PointF(63f, -62f),
                        new PointF(60f, -46f), new PointF(52f, -37f), new PointF(39f, -39f), new PointF(31f, -52f)
                    };
                    using (GraphicsPath facePath = new GraphicsPath())
                    {
                        facePath.AddPolygon(face);
                        g.FillPath(skin, facePath);
                        g.DrawPath(outline, facePath);
                    }
                    g.FillPie(hair, 29f, -76f, 36f, 22f, 180, 180);
                    PointF[] leftPony = new PointF[]
                    {
                        new PointF(38f, -56f), new PointF(26f, -44f), new PointF(20f, -18f), new PointF(33f, -4f), new PointF(45f, -24f)
                    };
                    PointF[] rightPony = new PointF[]
                    {
                        new PointF(56f, -56f), new PointF(69f, -42f), new PointF(76f, -18f), new PointF(64f, -5f), new PointF(50f, -27f)
                    };
                    g.FillPolygon(hair, leftPony);
                    g.FillPolygon(darkHair, rightPony);
                    g.FillEllipse(Brushes.White, 40f, -59f, 5.2f, 4.2f);
                    g.FillEllipse(Brushes.White, 51f, -59f, 5.2f, 4.2f);
                    g.FillEllipse(Brushes.Black, 41.8f, -57f, 2.0f, 2.0f);
                    g.FillEllipse(Brushes.Black, 52.8f, -57f, 2.0f, 2.0f);
                    g.DrawArc(browPen, 38f, -63f, 8f, 3.5f, 200, 120);
                    g.DrawArc(browPen, 49f, -63f, 8f, 3.5f, 220, 120);
                    g.DrawLine(facePen, 48f, -53f, 47f, -47f);
                    g.DrawArc(facePen, 41f, -45f, 13f, 6f, 12, 150);

                    PointF[] fallingUpperTorso = new PointF[]
                    {
                        new PointF(-34f, -40f), new PointF(18f, -40f), new PointF(24f, -23f),
                        new PointF(22f, -8f), new PointF(-30f, -8f), new PointF(-36f, -24f)
                    };
                    PointF[] fallingLowerTorso = new PointF[]
                    {
                        new PointF(-24f, -8f), new PointF(18f, -8f), new PointF(24f, 16f), new PointF(-22f, 16f)
                    };
                    g.FillPolygon(shirt, fallingUpperTorso);
                    g.FillPolygon(skirt, fallingLowerTorso);
                    g.FillPolygon(shirtShadow, new PointF[] { new PointF(-12f, -12f), new PointF(12f, -12f), new PointF(9f, 8f), new PointF(-10f, 8f) });
                    g.DrawPolygon(outline, fallingUpperTorso);
                    g.DrawPolygon(outline, fallingLowerTorso);
                    g.FillRectangle(apron, -12f, -30f, 22f, 24f);
                    g.DrawRectangle(outline, -12f, -30f, 22f, 24f);
                    g.DrawLine(outline, -6f, -34f, -14f, -18f);
                    g.DrawLine(outline, 6f, -34f, 14f, -18f);
                    g.DrawLine(outline, -8f, -10f, 8f, -10f);

                    PointF leftShoulder = new PointF(-27f, -28f);
                    PointF rightShoulder = new PointF(12f, -27f);
                    PointF leftElbow = new PointF(-38f, -12f);
                    PointF rightElbow = new PointF(24f, -14f);
                    PointF leftHand = new PointF(-48f, -6f);
                    PointF rightHand = new PointF(35f, -8f);
                    g.DrawLine(upperArmPen, leftShoulder, leftElbow);
                    g.DrawLine(upperArmPen, rightShoulder, rightElbow);
                    g.DrawLine(armPen, leftElbow, leftHand);
                    g.DrawLine(armPen, rightElbow, rightHand);
                    g.FillEllipse(skin, leftElbow.X - 3.4f, leftElbow.Y - 3.4f, 6.8f, 6.8f);
                    g.FillEllipse(skin, rightElbow.X - 3.4f, rightElbow.Y - 3.4f, 6.8f, 6.8f);
                    g.FillEllipse(skin, leftHand.X - 4.6f, leftHand.Y - 4.6f, 9.2f, 9.2f);
                    g.FillEllipse(skin, rightHand.X - 4.6f, rightHand.Y - 4.6f, 9.2f, 9.2f);

                    PointF leftHip = new PointF(-3f, 10f);
                    PointF rightHip = new PointF(15f, 9f);
                    PointF leftKnee = new PointF(-14f, 22f);
                    PointF rightKnee = new PointF(28f, 17f);
                    PointF leftFoot = new PointF(-28f, 26f);
                    PointF rightFoot = new PointF(43f, 20f);
                    g.DrawLine(legPen, leftHip, leftKnee);
                    g.DrawLine(legPen, rightHip, rightKnee);
                    g.DrawLine(calfPen, leftKnee, leftFoot);
                    g.DrawLine(calfPen, rightKnee, rightFoot);
                    g.FillEllipse(tights, leftKnee.X - 3f, leftKnee.Y - 3f, 6f, 6f);
                    g.FillEllipse(tights, rightKnee.X - 3f, rightKnee.Y - 3f, 6f, 6f);
                    g.FillEllipse(shoe, leftFoot.X - 8f, leftFoot.Y - 2f, 16f, 6f);
                    g.FillEllipse(shoe, rightFoot.X - 8f, rightFoot.Y - 2f, 16f, 6f);
                    g.Restore(state);
                    return;
                }

                float bodyLeft = x - 74f;
                float bodyTop = groundY - 40f;
                PointF[] headFace = new PointF[]
                {
                    new PointF(bodyLeft + 10f, bodyTop + 2f), new PointF(bodyLeft + 34f, bodyTop + 2f),
                    new PointF(bodyLeft + 42f, bodyTop + 14f), new PointF(bodyLeft + 36f, bodyTop + 32f),
                    new PointF(bodyLeft + 20f, bodyTop + 40f), new PointF(bodyLeft + 7f, bodyTop + 28f)
                };
                using (GraphicsPath facePath = new GraphicsPath())
                {
                    facePath.AddPolygon(headFace);
                    g.FillPath(skin, facePath);
                    g.DrawPath(outline, facePath);
                }
                g.FillPie(hair, bodyLeft + 2f, bodyTop - 3f, 44f, 22f, 180, 180);
                PointF[] leftTail = new PointF[]
                {
                    new PointF(bodyLeft + 10f, bodyTop + 16f), new PointF(bodyLeft + 0f, bodyTop + 30f),
                    new PointF(bodyLeft + 6f, bodyTop + 44f), new PointF(bodyLeft + 18f, bodyTop + 34f), new PointF(bodyLeft + 20f, bodyTop + 22f)
                };
                PointF[] rightTail = new PointF[]
                {
                    new PointF(bodyLeft + 35f, bodyTop + 16f), new PointF(bodyLeft + 45f, bodyTop + 30f),
                    new PointF(bodyLeft + 40f, bodyTop + 44f), new PointF(bodyLeft + 28f, bodyTop + 35f), new PointF(bodyLeft + 25f, bodyTop + 22f)
                };
                g.FillPolygon(hair, leftTail);
                g.FillPolygon(darkHair, rightTail);
                g.DrawLine(facePen, bodyLeft + 13f, bodyTop + 14f, bodyLeft + 19f, bodyTop + 20f);
                g.DrawLine(facePen, bodyLeft + 19f, bodyTop + 14f, bodyLeft + 13f, bodyTop + 20f);
                g.DrawLine(facePen, bodyLeft + 25f, bodyTop + 14f, bodyLeft + 31f, bodyTop + 20f);
                g.DrawLine(facePen, bodyLeft + 31f, bodyTop + 14f, bodyLeft + 25f, bodyTop + 20f);
                g.DrawLine(facePen, bodyLeft + 22f, bodyTop + 22f, bodyLeft + 20f, bodyTop + 28f);
                g.DrawArc(facePen, bodyLeft + 12f, bodyTop + 28f, 20f, 11f, 20, 140);

                PointF[] upperTorso = new PointF[]
                {
                    new PointF(bodyLeft + 42f, bodyTop + 8f), new PointF(bodyLeft + 108f, bodyTop + 8f),
                    new PointF(bodyLeft + 116f, bodyTop + 22f), new PointF(bodyLeft + 114f, bodyTop + 38f),
                    new PointF(bodyLeft + 46f, bodyTop + 38f)
                };
                PointF[] lowerBody = new PointF[]
                {
                    new PointF(bodyLeft + 100f, bodyTop + 10f), new PointF(bodyLeft + 156f, bodyTop + 10f),
                    new PointF(bodyLeft + 164f, bodyTop + 24f), new PointF(bodyLeft + 162f, bodyTop + 38f), new PointF(bodyLeft + 102f, bodyTop + 38f)
                };
                g.FillPolygon(shirt, upperTorso);
                g.FillPolygon(skirt, lowerBody);
                g.FillPolygon(shirtShadow, new PointF[] { new PointF(bodyLeft + 78f, bodyTop + 10f), new PointF(bodyLeft + 102f, bodyTop + 10f), new PointF(bodyLeft + 102f, bodyTop + 36f), new PointF(bodyLeft + 78f, bodyTop + 36f) });
                g.DrawPolygon(outline, upperTorso);
                g.DrawPolygon(outline, lowerBody);
                g.FillRectangle(apron, bodyLeft + 66f, bodyTop + 12f, 26f, 23f);
                g.DrawRectangle(outline, bodyLeft + 66f, bodyTop + 12f, 26f, 23f);

                PointF leftShoulder2 = new PointF(bodyLeft + 48f, bodyTop + 18f);
                PointF rightShoulder2 = new PointF(bodyLeft + 106f, bodyTop + 18f);
                PointF leftElbow2 = new PointF(bodyLeft + 36f, bodyTop + 28f);
                PointF leftHand2 = new PointF(bodyLeft + 24f, bodyTop + 32f);
                PointF rightElbow2 = new PointF(beingDragged ? bodyLeft + 132f : bodyLeft + 116f, beingDragged ? bodyTop + 20f : bodyTop + 25f);
                PointF rightHand2 = new PointF(beingDragged ? bodyLeft + 170f : bodyLeft + 124f, beingDragged ? bodyTop + 24f : bodyTop + 28f);
                g.DrawLine(upperArmPen, leftShoulder2, leftElbow2);
                g.DrawLine(armPen, leftElbow2, leftHand2);
                g.DrawLine(upperArmPen, rightShoulder2, rightElbow2);
                g.DrawLine(armPen, rightElbow2, rightHand2);
                g.FillEllipse(skin, leftHand2.X - 5f, leftHand2.Y - 5f, 10f, 10f);
                g.FillEllipse(skin, rightHand2.X - 5f, rightHand2.Y - 5f, 10f, 10f);

                PointF leftHip2 = new PointF(bodyLeft + 150f, bodyTop + 18f);
                PointF rightHip2 = new PointF(bodyLeft + 150f, bodyTop + 32f);
                PointF leftKnee2 = new PointF(bodyLeft + 166f, bodyTop + 16f);
                PointF rightKnee2 = new PointF(bodyLeft + 166f, bodyTop + 34f);
                PointF leftFoot2 = new PointF(bodyLeft + 184f, bodyTop + 16f);
                PointF rightFoot2 = new PointF(bodyLeft + 184f, bodyTop + 34f);
                g.DrawLine(legPen, leftHip2, leftKnee2);
                g.DrawLine(calfPen, leftKnee2, leftFoot2);
                g.DrawLine(legPen, rightHip2, rightKnee2);
                g.DrawLine(calfPen, rightKnee2, rightFoot2);
                g.FillEllipse(shoe, bodyLeft + 176f, bodyTop + 13f, 18f, 7f);
                g.FillEllipse(shoe, bodyLeft + 176f, bodyTop + 31f, 18f, 7f);
            }
        }




        private void DrawDraggingAttacker(Graphics g, float x, int groundY, double phase, double dragProgress)
        {
            float headW = Math.Max(31f, ClientSize.Width * 0.0168f);
            float headH = headW * 1.22f;
            float shoulderY = groundY - 92f;
            float chestY = groundY - 74f;
            float waistY = groundY - 50f;
            float hipY = groundY - 26f;
            float bodyX = x - 18f;
            double leftPhase = Math.Sin(phase);
            double rightPhase = Math.Sin(phase + Math.PI);
            float leftStride = (float)(leftPhase * 3.8f);
            float rightStride = (float)(rightPhase * 3.8f);
            float leftLift = (float)(Math.Max(0.0, leftPhase) * 2.8f);
            float rightLift = (float)(Math.Max(0.0, rightPhase) * 2.8f);

            using (SolidBrush skin = new SolidBrush(Color.FromArgb(230, 202, 190, 182)))
            using (SolidBrush hair = new SolidBrush(Color.FromArgb(236, 20, 20, 24)))
            using (SolidBrush hoodie = new SolidBrush(Color.FromArgb(238, 46, 48, 58)))
            using (SolidBrush hoodieDark = new SolidBrush(Color.FromArgb(226, 34, 36, 46)))
            using (SolidBrush hood = new SolidBrush(Color.FromArgb(222, 30, 32, 40)))
            using (SolidBrush pants = new SolidBrush(Color.FromArgb(236, 24, 26, 32)))
            using (SolidBrush shoe = new SolidBrush(Color.FromArgb(240, 10, 10, 14)))
            using (Pen outline = new Pen(Color.FromArgb(226, 8, 8, 10), 2.0f))
            using (Pen facePen = new Pen(Color.FromArgb(180, 36, 20, 20), 1.4f))
            using (Pen browPen = new Pen(Color.FromArgb(200, 24, 18, 18), 1.7f))
            using (Pen sleevePen = new Pen(Color.FromArgb(232, 60, 62, 74), 8.2f))
            using (Pen forearmPen = new Pen(Color.FromArgb(220, 196, 182, 176), 6.0f))
            using (Pen thighPen = new Pen(Color.FromArgb(232, 46, 48, 58), 8.4f))
            using (Pen calfPen = new Pen(Color.FromArgb(214, 188, 176, 170), 5.8f))
            {
                PointF[] hoodPoly = new PointF[]
                {
                    new PointF(bodyX - 14f, shoulderY - 5f),
                    new PointF(bodyX - 2f, shoulderY - 28f),
                    new PointF(bodyX + 18f, shoulderY - 24f),
                    new PointF(bodyX + 26f, shoulderY - 3f),
                    new PointF(bodyX + 5f, shoulderY + 4f),
                    new PointF(bodyX - 13f, shoulderY + 3f)
                };
                PointF[] torso = new PointF[]
                {
                    new PointF(bodyX - 22f, shoulderY + 2f),
                    new PointF(bodyX + 34f, shoulderY - 8f),
                    new PointF(bodyX + 52f, chestY + 4f),
                    new PointF(bodyX + 40f, waistY + 10f),
                    new PointF(bodyX - 2f, waistY + 11f),
                    new PointF(bodyX - 26f, chestY + 10f)
                };
                PointF[] abdomen = new PointF[]
                {
                    new PointF(bodyX + 2f, waistY + 6f),
                    new PointF(bodyX + 40f, waistY + 4f),
                    new PointF(bodyX + 45f, hipY + 8f),
                    new PointF(bodyX + 4f, hipY + 10f)
                };
                g.FillPolygon(hood, hoodPoly);
                g.FillPolygon(hoodie, torso);
                g.FillPolygon(hoodieDark, abdomen);
                g.DrawPolygon(outline, torso);
                g.DrawPolygon(outline, abdomen);
                g.DrawLine(outline, bodyX + 21f, shoulderY + 8f, bodyX + 26f, hipY + 7f);

                PointF leftShoulder = new PointF(bodyX - 18f, shoulderY + 10f);
                PointF rightShoulder = new PointF(bodyX + 28f, shoulderY + 4f);
                PointF leftHand = KeepEndInReach(leftShoulder, new PointF(x - 56f, groundY - 27f), 60f);
                PointF rightHand = KeepEndInReach(rightShoulder, new PointF(x - 48f, groundY - 23f), 60f);
                PointF leftElbow = BendJoint(leftShoulder, leftHand, 29f, 31f, 1f);
                PointF rightElbow = BendJoint(rightShoulder, rightHand, 29f, 31f, 1f);
                g.DrawLine(sleevePen, leftShoulder, leftElbow);
                g.DrawLine(sleevePen, rightShoulder, rightElbow);
                g.DrawLine(forearmPen, leftElbow, leftHand);
                g.DrawLine(forearmPen, rightElbow, rightHand);
                g.FillEllipse(skin, leftHand.X - 5f, leftHand.Y - 5f, 10f, 10f);
                g.FillEllipse(skin, rightHand.X - 5f, rightHand.Y - 5f, 10f, 10f);
                g.FillEllipse(skin, leftElbow.X - 3.2f, leftElbow.Y - 3.2f, 6.4f, 6.4f);
                g.FillEllipse(skin, rightElbow.X - 3.2f, rightElbow.Y - 3.2f, 6.4f, 6.4f);

                PointF leftHip = new PointF(bodyX + 10f, hipY + 8f);
                PointF rightHip = new PointF(bodyX + 26f, hipY + 8f);
                PointF leftFoot = EnsureLeftSideFoot(leftHip, bodyX + 2f + leftStride, groundY - leftLift * 0.08f, 4.5f, 72f);
                PointF rightFoot = EnsureRightSideFoot(rightHip, bodyX + 32f + rightStride, groundY - rightLift * 0.08f, 4.5f, 72f);
                PointF leftKnee = BendJoint(leftHip, leftFoot, 33f, 39f, 1f);
                PointF rightKnee = BendJoint(rightHip, rightFoot, 33f, 39f, 1f);
                g.DrawLine(thighPen, leftHip, leftKnee);
                g.DrawLine(calfPen, leftKnee, leftFoot);
                g.DrawLine(thighPen, rightHip, rightKnee);
                g.DrawLine(calfPen, rightKnee, rightFoot);
                g.FillEllipse(pants, leftKnee.X - 3.2f, leftKnee.Y - 3.2f, 6.4f, 6.4f);
                g.FillEllipse(pants, rightKnee.X - 3.2f, rightKnee.Y - 3.2f, 6.4f, 6.4f);
                g.FillEllipse(shoe, leftFoot.X - 5f, leftFoot.Y - 4f, 18f, 7f);
                g.FillEllipse(shoe, rightFoot.X - 5f, rightFoot.Y - 4f, 18f, 7f);

                float headCenterX = bodyX + 1f;
                float headCenterY = shoulderY - 24f;
                PointF[] facePoints = new PointF[]
                {
                    new PointF(headCenterX - headW * 0.24f, headCenterY - headH * 0.48f),
                    new PointF(headCenterX + headW * 0.24f, headCenterY - headH * 0.48f),
                    new PointF(headCenterX + headW * 0.40f, headCenterY - headH * 0.20f),
                    new PointF(headCenterX + headW * 0.36f, headCenterY + headH * 0.18f),
                    new PointF(headCenterX + headW * 0.22f, headCenterY + headH * 0.42f),
                    new PointF(headCenterX, headCenterY + headH * 0.52f),
                    new PointF(headCenterX - headW * 0.22f, headCenterY + headH * 0.42f),
                    new PointF(headCenterX - headW * 0.36f, headCenterY + headH * 0.18f),
                    new PointF(headCenterX - headW * 0.40f, headCenterY - headH * 0.20f)
                };
                g.FillRectangle(skin, headCenterX - 3.0f, shoulderY - 12f, 6.0f, 9f);
                using (GraphicsPath facePath = new GraphicsPath())
                {
                    facePath.AddPolygon(facePoints);
                    g.FillPath(skin, facePath);
                    g.DrawPath(outline, facePath);
                }
                g.FillEllipse(skin, headCenterX - headW * 0.53f, headCenterY - 2f, 5f, 8f);
                g.FillEllipse(skin, headCenterX + headW * 0.43f, headCenterY - 2f, 5f, 8f);
                g.FillPie(hair, headCenterX - headW * 0.60f, headCenterY - headH * 0.62f, headW * 1.20f, headH * 0.82f, 180, 180);
                g.FillEllipse(Brushes.White, headCenterX - 10.0f, headCenterY - 2.5f, 6.8f, 4.8f);
                g.FillEllipse(Brushes.White, headCenterX + 3.2f, headCenterY - 2.5f, 6.8f, 4.8f);
                g.FillEllipse(Brushes.DimGray, headCenterX - 7.5f, headCenterY + 0.3f, 2.8f, 2.8f);
                g.FillEllipse(Brushes.DimGray, headCenterX + 5.8f, headCenterY + 0.3f, 2.8f, 2.8f);
                g.FillEllipse(Brushes.Black, headCenterX - 6.5f, headCenterY + 1.2f, 1.5f, 1.5f);
                g.FillEllipse(Brushes.Black, headCenterX + 6.8f, headCenterY + 1.2f, 1.5f, 1.5f);
                g.DrawArc(browPen, headCenterX - 12.4f, headCenterY - 14f, 9.2f, 4.2f, 190, 120);
                g.DrawArc(browPen, headCenterX + 2.0f, headCenterY - 14f, 9.2f, 4.2f, 230, 120);
                g.DrawLine(facePen, headCenterX + 0.2f, headCenterY + 2f, headCenterX - 0.8f, headCenterY + 10f);
                g.DrawArc(facePen, headCenterX - 3f, headCenterY + 8f, 5.4f, 3.8f, 8, 156);
                g.DrawArc(facePen, headCenterX - 9f, headCenterY + 14f, 18f, 8f, 6, 168);
                g.DrawLine(facePen, headCenterX - 5f, headCenterY + 19f, headCenterX - 2f, headCenterY + 16f);
                g.DrawLine(facePen, headCenterX - 1f, headCenterY + 20f, headCenterX + 2f, headCenterY + 16f);
                g.DrawLine(facePen, headCenterX + 3f, headCenterY + 19f, headCenterX + 6f, headCenterY + 16f);
            }
        }

        private void DrawFlyingCupRight(Graphics g, float startX, float startY, double progress, int groundY)
        {
            if (progress < 0) progress = 0;
            if (progress > 1) progress = 1;

            float eased = (float)EaseOutCubic(progress);
            float x = LerpFloat(startX, startX + 116f, eased);
            float arcY = LerpFloat(startY, groundY - 10f, (float)progress);
            float lift = (float)(Math.Sin(progress * Math.PI) * 30.0);
            float y = progress >= 1.0 ? groundY - 10f : arcY - lift;
            float rotation = progress >= 1.0 ? 94f : -22f + (float)(progress * 116.0);
            using (SolidBrush cup = new SolidBrush(Color.FromArgb(245, 238, 232, 224)))
            using (SolidBrush coffee = new SolidBrush(Color.FromArgb(228, 90, 52, 24)))
            using (Pen outline = new Pen(Color.FromArgb(220, 30, 16, 18), 2f))
            {
                GraphicsState state = g.Save();
                g.TranslateTransform(x, y);
                g.RotateTransform(rotation);
                g.FillRectangle(cup, -9f, -17f, 18f, 24f);
                g.FillRectangle(coffee, -7f, -15f, 14f, 5f);
                g.FillRectangle(Brushes.Gainsboro, -10f, -19f, 20f, 4f);
                g.DrawRectangle(outline, -9f, -17f, 18f, 24f);
                g.Restore(state);
            }
        }

        private void DrawCoffeeSplashDetailed(Graphics g, float x, float y, double progress)
        {
            using (SolidBrush coffee = new SolidBrush(Color.FromArgb(196, 98, 58, 28)))
            using (SolidBrush foam = new SolidBrush(Color.FromArgb(110, 226, 200, 166)))
            using (Pen splash = new Pen(Color.FromArgb(210, 140, 86, 50), 2f))
            {
                for (int i = 0; i < 9; i++)
                {
                    float dx = 8f + i * 10f;
                    float dy = (float)(Math.Sin(i * 0.8 + progress * 3.1) * 10.0);
                    float size = 3.5f + (i % 4);
                    g.FillEllipse(coffee, x + dx * (float)progress, y + dy - (float)(progress * 8f), size + 1f, size + 1f);
                    if (i % 2 == 0)
                        g.FillEllipse(foam, x + dx * (float)progress + 1f, y + dy - (float)(progress * 8f) + 1f, size * 0.55f, size * 0.45f);
                }
                g.DrawArc(splash, x - 4f, y - 10f, 62f, 28f, 196, 84);
            }
        }

        private void DrawBloodPoolDetailed(Graphics g, float x, float y, double progress, double dragProgress)
        {
            float width = 78f + (float)(progress * 84f);
            float height = 18f + (float)(progress * 24f);
            using (SolidBrush dark = new SolidBrush(Color.FromArgb(170, 84, 0, 0)))
            using (SolidBrush core = new SolidBrush(Color.FromArgb(226, 168, 6, 10)))
            using (SolidBrush edge = new SolidBrush(Color.FromArgb(150, 120, 0, 4)))
            using (SolidBrush shine = new SolidBrush(Color.FromArgb(74, 255, 106, 106)))
            using (SolidBrush trail = new SolidBrush(Color.FromArgb(170, 132, 0, 4)))
            {
                g.FillEllipse(dark, x - width * 0.58f, y - height * 0.46f, width * 1.10f, height * 0.96f);
                g.FillEllipse(core, x - width * 0.50f, y - height * 0.40f, width, height);
                g.FillEllipse(edge, x + width * 0.18f, y - height * 0.10f, width * 0.34f, height * 0.26f);
                g.FillEllipse(edge, x - width * 0.46f, y + height * 0.04f, width * 0.18f, height * 0.16f);
                g.FillEllipse(shine, x - width * 0.18f, y - height * 0.14f, width * 0.24f, height * 0.18f);
                g.FillEllipse(shine, x + width * 0.10f, y + height * 0.02f, width * 0.14f, height * 0.10f);

                if (dragProgress > 0)
                {
                    float trailLength = 26f + (float)(dragProgress * 120.0);
                    for (int i = 0; i < 6; i++)
                    {
                        float blobX = x + 24f + trailLength * i / 5f;
                        float blobY = y + 2f + (i % 2 == 0 ? 0f : 5f);
                        float blobW = 18f + i * 6f;
                        float blobH = 8f + i * 1.8f;
                        g.FillEllipse(trail, blobX, blobY, blobW, blobH);
                    }
                }
            }
        }

        private void DrawBloodFlood(Graphics g, double progress, int left, int top, int width, int height)
        {
            int alpha = (int)(236 * progress);
            float floodHeight = (float)(height * Clamp01(0.04 + progress * 1.02));
            float floodTop = top + height - floodHeight;
            PointF[] wave = new PointF[13];
            for (int i = 0; i < wave.Length; i++)
            {
                float px = left + width * i / (float)(wave.Length - 1);
                float py = floodTop + (float)(Math.Sin(i * 0.72 + progress * 8.0) * (10.0 + progress * 18.0));
                wave[i] = new PointF(px, py);
            }

            List<PointF> darkPoly = new List<PointF>();
            darkPoly.Add(new PointF(left, top + height));
            darkPoly.AddRange(wave);
            darkPoly.Add(new PointF(left + width, top + height));

            using (SolidBrush dark = new SolidBrush(Color.FromArgb((int)(alpha * 0.86), 104, 0, 0)))
            using (SolidBrush main = new SolidBrush(Color.FromArgb(alpha, 182, 0, 8)))
            using (SolidBrush shine = new SolidBrush(Color.FromArgb((int)(alpha * 0.24), 255, 118, 118)))
            using (SolidBrush dense = new SolidBrush(Color.FromArgb((int)(alpha * 0.34), 132, 0, 4)))
            {
                g.FillPolygon(dark, darkPoly.ToArray());

                PointF[] innerWave = new PointF[wave.Length + 2];
                innerWave[0] = new PointF(left, top + height);
                for (int i = 0; i < wave.Length; i++)
                    innerWave[i + 1] = new PointF(wave[i].X, wave[i].Y + 12f);
                innerWave[innerWave.Length - 1] = new PointF(left + width, top + height);
                g.FillPolygon(main, innerWave);

                for (int i = 0; i < 8; i++)
                {
                    float smearX = left + width * (0.03f + i * 0.115f);
                    float smearY = floodTop - (float)(progress * 20f) + (i % 2 == 0 ? 0f : 7f);
                    g.FillEllipse(shine, smearX, smearY, 56f, 16f);
                    g.FillEllipse(dense, smearX + 16f, smearY + 10f, 44f, 18f);
                }
            }
        }

        private void DrawShadowEllipse(Graphics g, float x, float y, float width, float height, int alpha)
        {
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                g.FillEllipse(shadow, x - width / 2f, y - height / 2f, width, height);
        }

        private float LerpFloat(float start, float end, double t)
        {
            return (float)(start + (end - start) * t);
        }

        private double EaseInOut(double t)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            return t * t * (3.0 - 2.0 * t);
        }

        private double EaseOutCubic(double t)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            double k = 1.0 - t;
            return 1.0 - k * k * k;
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

            // Свет из левого окна. После перехода времени он становится вечерним и темнее.
            bool evening = _game != null && _game.EveningWindowLight;
            int windowAlpha = evening ? (_game.LightOn ? 22 : 42) : (_game.LightOn ? 72 : 128);
            Color windowGlowColor = evening ? Color.FromArgb(windowAlpha, 86, 60, 118) : Color.FromArgb(windowAlpha, 255, 238, 190);
            Color windowCoreColor = evening ? Color.FromArgb(Math.Max(8, windowAlpha / 2), 52, 38, 88) : Color.FromArgb(windowAlpha / 2, 255, 250, 218);

            PointF w0;
            PointF w1;
            PointF w2;
            PointF w3;

            if (ProjectWorldPoint(-298.4, -18, 261, out w0) &&
                ProjectWorldPoint(-298.4, 58, 261, out w1) &&
                ProjectWorldPoint(-298.4, 58, 309, out w2) &&
                ProjectWorldPoint(-298.4, -18, 309, out w3))
            {
                using (SolidBrush glassGlow = new SolidBrush(windowGlowColor))
                using (SolidBrush glassCore = new SolidBrush(windowCoreColor))
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


        private void CheckForDayTransitionOverlay()
        {
            if (_game == null || _dayTransitionOverlayActive)
                return;

            string pendingTitle = _game.ConsumePendingDayTransitionTitle();
            if (!string.IsNullOrWhiteSpace(pendingTitle))
            {
                _lastKnownGameDay = _game.CurrentDay > 0 ? _game.CurrentDay : _lastKnownGameDay;
                StartDayTransitionOverlay(pendingTitle);
                return;
            }

            int currentDay = _game.CurrentDay;
            if (currentDay <= 1)
            {
                if (_lastKnownGameDay <= 0)
                    _lastKnownGameDay = 1;
                return;
            }

            if (currentDay != _lastKnownGameDay)
            {
                _lastKnownGameDay = currentDay;
                StartDayTransitionOverlay("День " + currentDay.ToString());
            }
        }

        private void StartDayTransitionOverlay(string text)
        {
            _dayTransitionOverlayActive = true;
            _dayTransitionOverlayTimer = 0;
            _dayTransitionOverlayText = text ?? string.Empty;
            _showToBeContinuedAfterDayTransition = string.Equals(_dayTransitionOverlayText, "День 2", StringComparison.OrdinalIgnoreCase);
            _sceneDirty = true;
            _pressedKeys.Clear();
            _mouseLookActive = false;
            ShowGameCursor();
            Invalidate();
        }

        private void DrawDayTransitionOverlay(Graphics g)
        {
            if (!_dayTransitionOverlayActive)
                return;

            DrawDayTitleCard(g, _dayTransitionOverlayText, _dayTransitionOverlayTimer);
        }

        private void DrawToBeContinuedScreen(Graphics g)
        {
            if (!_toBeContinuedScreenActive)
                return;

            using (SolidBrush black = new SolidBrush(Color.Black))
                g.FillRectangle(black, 0, 0, ClientSize.Width, ClientSize.Height);

            string text = "Продолжение следует";
            int maxWidth = (int)(ClientSize.Width * 0.86);
            int maxHeight = (int)(ClientSize.Height * 0.18);

            using (Font font = CreateFittedFont(g, text, FontStyle.Bold, maxWidth, maxHeight, Math.Max(42f, ClientSize.Height / 8.0f)))
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(215, 0, 0, 0)))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 250, 250, 250)))
            {
                DrawCenteredString(g, text, font, shadow, ClientSize.Width / 2f + 3f, ClientSize.Height / 2f + 4f);
                DrawCenteredString(g, text, font, brush, ClientSize.Width / 2f, ClientSize.Height / 2f);
            }
        }

        private void DrawDayTitleCard(Graphics g, string text, double timer)
        {
            using (SolidBrush black = new SolidBrush(Color.Black))
                g.FillRectangle(black, 0, 0, ClientSize.Width, ClientSize.Height);

            double alpha01 = Clamp01(timer / DayTitleFadeDuration);
            int alpha = (int)Math.Round(255.0 * alpha01);
            if (alpha < 0)
                alpha = 0;
            if (alpha > 255)
                alpha = 255;

            int maxWidth = (int)(ClientSize.Width * 0.82);
            int maxHeight = (int)(ClientSize.Height * 0.20);
            using (Font font = CreateFittedFont(g, text, FontStyle.Bold, maxWidth, maxHeight, Math.Max(50f, ClientSize.Height / 6.2f)))
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(Math.Max(0, alpha - 40), 0, 0, 0)))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 250, 250, 250)))
            {
                DrawCenteredString(g, text, font, shadow, ClientSize.Width / 2f + 3f, ClientSize.Height / 2f + 4f);
                DrawCenteredString(g, text, font, brush, ClientSize.Width / 2f, ClientSize.Height / 2f);
            }
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
            DrawHeldBigGlass(g);
            DrawRecipeOverlay(g);
            DrawSettingsPanel(g);
        }

        private void DrawTimePassTransition(Graphics g)
        {
            if (_game == null || !_game.TimePassTransitionVisible)
                return;

            int blackAlpha = (int)Math.Round(_game.TimePassBlackAlpha * 255.0);
            if (blackAlpha < 0)
                blackAlpha = 0;
            if (blackAlpha > 255)
                blackAlpha = 255;

            using (SolidBrush black = new SolidBrush(Color.FromArgb(blackAlpha, 0, 0, 0)))
                g.FillRectangle(black, 0, 0, ClientSize.Width, ClientSize.Height);

            double textAlpha01 = _game.TimePassTextAlpha;
            if (textAlpha01 <= 0)
                return;

            int textAlpha = (int)Math.Round(textAlpha01 * 245.0);
            if (textAlpha < 0)
                textAlpha = 0;
            if (textAlpha > 245)
                textAlpha = 245;

            string text = _game.TimePassTransitionText;
            using (Font font = new Font("Arial", 30, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(textAlpha, 235, 235, 235)))
            {
                SizeF size = g.MeasureString(text, font);
                float x = (ClientSize.Width - size.Width) / 2f;
                float y = (ClientSize.Height - size.Height) / 2f - 18f;
                g.DrawString(text, font, brush, x, y);

                int dotCount = 1 + ((int)Math.Floor(Math.Max(0.0, _game.TimePassTransitionElapsed)) % 3);
                string autosave = "Автосохранение" + new string('.', dotCount);
                using (Font autoFont = new Font("Arial", 20, FontStyle.Bold))
                using (SolidBrush autoBrush = new SolidBrush(Color.FromArgb(Math.Min(255, textAlpha + 10), 220, 52, 58)))
                {
                    SizeF autoSize = g.MeasureString(autosave, autoFont);
                    float ax = (ClientSize.Width - autoSize.Width) / 2f;
                    float ay = y + size.Height + 12f;
                    g.DrawString(autosave, autoFont, autoBrush, ax, ay);
                }
            }
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
            int lineHeight = 31;
            int horizontalPadding = 14;
            int extraRightPadding = 10;
            int topPadding = 14;
            int bottomPadding = 10;

            using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
            using (Font textFont = new Font("Arial", 15, FontStyle.Regular))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(78, 0, 0, 0)))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(230, 230, 230)))
            using (SolidBrush gray = new SolidBrush(Color.FromArgb(145, 145, 145)))
            using (SolidBrush green = new SolidBrush(Color.FromArgb(70, 220, 105)))
            {
                string title = "День " + _game.CurrentDay + " — цели";
                float maxWidth = g.MeasureString(title, titleFont).Width;
                int titleHeight = (int)Math.Ceiling(g.MeasureString(title, titleFont).Height);

                for (int i = 0; i < _game.Objectives.Count; i++)
                {
                    GameObjective objective = _game.Objectives[i];
                    string mark = objective.IsCompleted ? "✓ " : "[ ] ";
                    float markWidth = g.MeasureString(mark, textFont).Width - 2f;
                    float lineWidth = markWidth + g.MeasureString(objective.Text, textFont).Width;
                    if (lineWidth > maxWidth)
                        maxWidth = lineWidth;
                }

                int width = (int)Math.Ceiling(maxWidth + horizontalPadding * 2 + extraRightPadding);
                int height = topPadding + titleHeight + 14 + _game.Objectives.Count * lineHeight + bottomPadding;

                g.FillRectangle(bg, x, y, width, height);
                g.DrawString(title, titleFont, white, x + horizontalPadding, y + topPadding);

                float linesTop = y + topPadding + titleHeight + 12f;
                for (int i = 0; i < _game.Objectives.Count; i++)
                {
                    GameObjective objective = _game.Objectives[i];
                    string mark = objective.IsCompleted ? "✓ " : "[ ] ";
                    SolidBrush markBrush = objective.IsCompleted ? green : white;
                    SolidBrush textBrush = objective.IsCompleted ? gray : white;

                    float lineY = linesTop + i * lineHeight;
                    g.DrawString(mark, textFont, markBrush, x + horizontalPadding - 2, lineY);
                    float markWidth = g.MeasureString(mark, textFont).Width - 2f;
                    g.DrawString(objective.Text, textFont, textBrush, x + horizontalPadding - 2 + markWidth, lineY);
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
            bool isTiaText = text.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase);
            bool isMikeText = text.StartsWith("Майк:", StringComparison.OrdinalIgnoreCase);
            bool isClientText = isTiaText || isMikeText;
            bool isPlayerAnswer =
                text == "Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?" ||
                text == "Ага...Чего желаете?" ||
                text == "Заказ принят, к оплате будет 300 рублей" ||
                text == "300р" ||
                text == "Здравствуйте!" ||
                text == "Какой кофе вы бы хотели попробовать?" ||
                text == "Извините, я могу вам чем-то помочь?" ||
                text == "Д-да... А что?" ||
                text == "Хорошо... К оплате будет 250р";
            bool isFirstPersonSpeech =
                text == "Эх... Очередная смена... Очередная неделя без выходных" ||
                text == "Надо подготовиться к началу смены, убрать и помыть все кружки" ||
                text == "заправлю кофемашину зернами. где там мешок с кофе..." ||
                text == "Заправлю кофемашину зернами. где там мешок с кофе..." ||
                text == "Как же бесят эти выскочки с утра пораньше..." ||
                text == "Какой-то странный тип" ||
                text == "Нужно возобновить в памяти рецепт" ||
                text == "Ладно, пора домой. Надо выключить свет и телевизор, разгребу все завтра" ||
                text == "Кажется я заработалась... Нужно в ближайшем времени поговорить с начальством по поводу отпуска.";

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
                    Color borderColor;
                    if (isMikeText)
                        borderColor = Color.FromArgb(205, 220, 45, 45);
                    else if (isTiaText)
                        borderColor = Color.FromArgb(190, 236, 128, 178);
                    else
                        borderColor = Color.FromArgb(170, 230, 210, 160);

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
            string line3 = "Space - следующая реплика";
            string line4 = "Esc - полноэкранный режим";

            using (Font font = new Font("Arial", 11, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(230, 230, 220)))
            {
                SizeF size1 = g.MeasureString(line1, font);
                SizeF size2 = g.MeasureString(line2, font);
                SizeF size3 = g.MeasureString(line3, font);
                SizeF size4 = g.MeasureString(line4, font);

                float width = Math.Max(Math.Max(size1.Width, size2.Width), Math.Max(size3.Width, size4.Width));
                float height = size1.Height + size2.Height + size3.Height + size4.Height + 20;

                float x = ClientSize.Width - width - 28;
                float y = 18;

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                    g.FillRectangle(bg, x - 10, y - 8, width + 20, height + 16);

                g.DrawString(line1, font, textBrush, x, y);
                g.DrawString(line2, font, textBrush, x, y + size1.Height + 4);
                g.DrawString(line3, font, textBrush, x, y + size1.Height + size2.Height + 8);
                g.DrawString(line4, font, textBrush, x, y + size1.Height + size2.Height + size3.Height + 12);
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

        private void DrawHeldBigGlass(Graphics g)
        {
            if (_game == null || !_game.HeldBigGlassVisible)
                return;

            int cupWidth = Math.Max(92, ClientSize.Width / 12);
            int cupHeight = Math.Max(142, ClientSize.Height / 4);
            int centerX = ClientSize.Width - cupWidth / 2 - 78;
            int bottomY = ClientSize.Height - 18;
            int topY = bottomY - cupHeight;

            DrawBigGlassCup2D(g, centerX, topY, bottomY, cupWidth, _game.CoffeePortionsInCup, 0, false, _game.BigGlassHasIce);
        }

        private void DrawBigGlassCup2D(Graphics g, int centerX, int topY, int bottomY, int cupWidth, int coffeePortions, double brewingProgress, bool underMachine, bool iceAdded)
        {
            int cupHeight = bottomY - topY;
            int topHalf = cupWidth / 2;
            int bottomHalf = Math.Max(30, cupWidth / 3);
            int bodyTopY = topY + 8;
            int bodyBottomY = bottomY - 12;
            int leftTopX = centerX - topHalf;
            int rightTopX = centerX + topHalf;
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

            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
            using (SolidBrush glassBrush = new SolidBrush(Color.FromArgb(92, 226, 238, 244)))
            using (SolidBrush glassSideBrush = new SolidBrush(Color.FromArgb(82, 190, 210, 218)))
            using (SolidBrush rimBrush = new SolidBrush(Color.FromArgb(142, 232, 244, 250)))
            using (SolidBrush coffeeBrush = new SolidBrush(Color.FromArgb(242, 166, 88, 42)))
            using (SolidBrush iceBrush = new SolidBrush(Color.FromArgb(120, 232, 248, 255)))
            using (Pen outlinePen = new Pen(Color.FromArgb(210, 28, 34, 38), 4))
            using (Pen shinePen = new Pen(Color.FromArgb(125, 255, 255, 255), 5))
            {
                g.FillEllipse(shadowBrush, centerX - topHalf - 8, bottomY - 12, cupWidth + 16, 10);

                int liquidBottomY = bodyBottomY - 5;
                double fillFraction = Math.Min(1.0, Math.Max(0.0, (coffeePortions + brewingProgress) / 3.0));
                if (iceAdded && fillFraction > 0)
                    fillFraction = Math.Min(0.82, fillFraction + 0.11);
                int liquidTopY = liquidBottomY - (int)Math.Round((bodyBottomY - bodyTopY - 14) * fillFraction);
                if (coffeePortions > 0 || brewingProgress > 0)
                {
                    Point[] coffeeLayer = new Point[]
                    {
                        new Point(leftAtY(liquidTopY), liquidTopY),
                        new Point(rightAtY(liquidTopY), liquidTopY),
                        new Point(rightAtY(liquidBottomY), liquidBottomY),
                        new Point(leftAtY(liquidBottomY), liquidBottomY)
                    };
                    g.FillPolygon(coffeeBrush, coffeeLayer);
                }

                if (iceAdded)
                {
                    int safeLeftTop = leftAtY(Math.Max(bodyTopY + 14, liquidTopY + 6)) + 8;
                    int safeRightTop = rightAtY(Math.Max(bodyTopY + 14, liquidTopY + 6)) - 8;
                    Rectangle[] iceCubes = new Rectangle[]
                    {
                        new Rectangle(Math.Max(safeLeftTop, centerX - cupWidth / 3), Math.Max(bodyTopY + 16, liquidTopY + 6), 28, 24),
                        new Rectangle(Math.Min(safeRightTop - 22, centerX + cupWidth / 10), Math.Max(bodyTopY + 27, liquidTopY + 16), 30, 25),
                        new Rectangle(centerX - 12, Math.Min(liquidBottomY - 32, liquidTopY + 42), 32, 26),
                        new Rectangle(centerX - cupWidth / 4, Math.Min(liquidBottomY - 18, liquidTopY + 66), 28, 24),
                        new Rectangle(centerX + cupWidth / 9, Math.Min(liquidBottomY - 20, liquidTopY + 76), 29, 25)
                    };

                    using (Pen icePen = new Pen(Color.FromArgb(96, 174, 205, 220), 2))
                    {
                        foreach (Rectangle cube in iceCubes)
                        {
                            if (cube.Left <= leftAtY(cube.Top) + 3 || cube.Right >= rightAtY(cube.Top) - 3)
                                continue;
                            if (cube.Top <= bodyTopY + 4 || cube.Bottom >= bodyBottomY - 4)
                                continue;

                            g.FillRectangle(iceBrush, cube);
                            g.DrawRectangle(icePen, cube);
                        }
                    }
                }

                using (SolidBrush clearOverlay = new SolidBrush(Color.FromArgb(58, 230, 246, 252)))
                    g.FillPolygon(clearOverlay, body);

                if (iceAdded)
                {
                    Rectangle[] visibleIceCubes = new Rectangle[]
                    {
                        new Rectangle(centerX - cupWidth / 3, Math.Max(bodyTopY + 16, liquidTopY + 6), 28, 24),
                        new Rectangle(centerX + cupWidth / 10, Math.Max(bodyTopY + 27, liquidTopY + 16), 30, 25),
                        new Rectangle(centerX - 12, Math.Min(liquidBottomY - 32, liquidTopY + 42), 32, 26),
                        new Rectangle(centerX - cupWidth / 4, Math.Min(liquidBottomY - 18, liquidTopY + 66), 28, 24),
                        new Rectangle(centerX + cupWidth / 9, Math.Min(liquidBottomY - 20, liquidTopY + 76), 29, 25)
                    };

                    using (Pen visibleIcePen = new Pen(Color.FromArgb(100, 166, 198, 216), 2))
                    {
                        foreach (Rectangle cube in visibleIceCubes)
                        {
                            if (cube.Left <= leftAtY(cube.Top) + 3 || cube.Right >= rightAtY(cube.Top) - 3)
                                continue;
                            if (cube.Top <= bodyTopY + 4 || cube.Bottom >= bodyBottomY - 4)
                                continue;

                            g.FillRectangle(iceBrush, cube);
                            g.DrawRectangle(visibleIcePen, cube);
                        }
                    }
                }

                g.DrawPolygon(outlinePen, body);
                g.FillRectangle(rimBrush, leftTopX - 3, bodyTopY - 4, cupWidth + 6, 8);
                g.DrawRectangle(outlinePen, leftTopX - 3, bodyTopY - 4, cupWidth + 6, 8);
                g.FillRectangle(rimBrush, leftBottomX - 2, bodyBottomY - 4, rightBottomX - leftBottomX + 4, 7);
                g.FillPolygon(glassSideBrush, new Point[]
                {
                    new Point(leftTopX + 6, bodyTopY + 10),
                    new Point(leftTopX + 14, bodyTopY + 10),
                    new Point(leftBottomX + 10, bodyBottomY - 10),
                    new Point(leftBottomX + 4, bodyBottomY - 10)
                });
                if (underMachine)
                    g.FillEllipse(shadowBrush, centerX - topHalf + 12, bottomY - 10, cupWidth - 24, 8);
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
            using (SolidBrush coffeeBrush = new SolidBrush(Color.FromArgb(255, 92, 54, 30)))
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
                if (coffeeHeight > 0)
                {
                    Point[] visibleCoffeeLayer = new Point[]
                    {
                        new Point(leftAtY(coffeeTopY), coffeeTopY),
                        new Point(rightAtY(coffeeTopY), coffeeTopY),
                        new Point(rightAtY(liquidBottomY), liquidBottomY),
                        new Point(leftAtY(liquidBottomY), liquidBottomY)
                    };
                    using (SolidBrush visibleCoffeeBrush = new SolidBrush(Color.FromArgb(255, 92, 54, 30)))
                        g.FillPolygon(visibleCoffeeBrush, visibleCoffeeLayer);
                }
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
            using (SolidBrush coffeeBrush = new SolidBrush(Color.FromArgb(255, 92, 54, 30)))
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

            if (_game.MikeRecipeActive)
            {
                DrawMikeRecipeOverlay(g);
                return;
            }

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

        private void DrawMikeRecipeOverlay(Graphics g)
        {
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
            using (Font titleFont = new Font("Arial", 26, FontStyle.Bold))
            using (Font subTitleFont = new Font("Arial", 17, FontStyle.Bold))
            using (Font ingredientFont = new Font("Arial", 22, FontStyle.Bold))
            using (Font closeFont = new Font("Arial", 18, FontStyle.Bold))
            using (SolidBrush black = new SolidBrush(Color.Black))
            using (SolidBrush glassBrush = new SolidBrush(Color.FromArgb(205, 248, 250, 255)))
            using (SolidBrush espressoBrush = new SolidBrush(Color.FromArgb(82, 48, 31)))
            using (SolidBrush espressoLightBrush = new SolidBrush(Color.FromArgb(132, 84, 48)))
            using (SolidBrush iceBrush = new SolidBrush(Color.FromArgb(220, 242, 252, 255)))
            using (SolidBrush shineBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
            {
                g.FillRectangle(bg, panel);
                g.DrawRectangle(border, panel);

                g.DrawString("двойной эспрессо со льдом", titleFont, black, panel.X + 28, panel.Y + 20);
                g.DrawString("рецепт заказа", subTitleFont, black, panel.X + 32, panel.Y + 76);

                float ingredientsX = imageArea.Right + 72;
                float ingredientsY = panel.Y + 144;
                g.DrawString("ингредиенты:", ingredientFont, black, ingredientsX, ingredientsY);
                g.DrawString("эспрессо x 2", ingredientFont, black, ingredientsX, ingredientsY + 64);
                g.DrawString("лед", ingredientFont, black, ingredientsX, ingredientsY + 118);

                int glassTopY = imageArea.Y + 45;
                int glassBottomY = imageArea.Bottom - 28;
                int glassLeftTop = imageArea.X + 62;
                int glassRightTop = imageArea.Right - 62;
                int glassLeftBottom = imageArea.X + 82;
                int glassRightBottom = imageArea.Right - 82;

                Point[] glass = new Point[]
                {
                    new Point(glassLeftTop, glassTopY),
                    new Point(glassRightTop, glassTopY),
                    new Point(glassRightBottom, glassBottomY),
                    new Point(glassLeftBottom, glassBottomY)
                };

                g.FillPolygon(glassBrush, glass);
                g.DrawPolygon(cupLinePen, glass);

                int liquidTop = glassTopY + 62;
                int liquidBottom = glassBottomY - 14;

                Func<int, int> leftAtY = currentY =>
                    glassLeftTop + (glassLeftBottom - glassLeftTop) * (currentY - glassTopY) / (glassBottomY - glassTopY);
                Func<int, int> rightAtY = currentY =>
                    glassRightTop + (glassRightBottom - glassRightTop) * (currentY - glassTopY) / (glassBottomY - glassTopY);

                Point[] espresso = new Point[]
                {
                    new Point(leftAtY(liquidTop), liquidTop),
                    new Point(rightAtY(liquidTop), liquidTop),
                    new Point(rightAtY(liquidBottom), liquidBottom),
                    new Point(leftAtY(liquidBottom), liquidBottom)
                };
                g.FillPolygon(espressoBrush, espresso);

                int foamY = liquidTop;
                Point[] topLayer = new Point[]
                {
                    new Point(leftAtY(foamY), foamY),
                    new Point(rightAtY(foamY), foamY),
                    new Point(rightAtY(foamY + 22), foamY + 22),
                    new Point(leftAtY(foamY + 22), foamY + 22)
                };
                g.FillPolygon(espressoLightBrush, topLayer);

                Rectangle[] iceCubes = new Rectangle[]
                {
                    new Rectangle(imageArea.X + 94, imageArea.Y + 82, 32, 30),
                    new Rectangle(imageArea.X + 132, imageArea.Y + 88, 30, 28),
                    new Rectangle(imageArea.X + 105, imageArea.Y + 138, 34, 32),
                    new Rectangle(imageArea.X + 140, imageArea.Y + 160, 30, 28),
                    new Rectangle(imageArea.X + 96, imageArea.Y + 214, 30, 28),
                    new Rectangle(imageArea.X + 130, imageArea.Y + 228, 32, 30)
                };

                using (Pen icePen = new Pen(Color.FromArgb(170, 190, 215, 230), 3))
                {
                    foreach (Rectangle cube in iceCubes)
                    {
                        g.FillRectangle(iceBrush, cube);
                        g.DrawRectangle(icePen, cube);
                    }
                }

                Point[] highlight1 = new Point[]
                {
                    new Point(glassLeftTop + 14, glassTopY + 18),
                    new Point(glassLeftTop + 28, glassTopY + 18),
                    new Point(glassLeftBottom + 20, glassBottomY - 22),
                    new Point(glassLeftBottom + 7, glassBottomY - 22)
                };
                Point[] highlight2 = new Point[]
                {
                    new Point(glassRightTop - 30, glassTopY + 32),
                    new Point(glassRightTop - 20, glassTopY + 32),
                    new Point(glassRightBottom - 20, glassBottomY - 38),
                    new Point(glassRightBottom - 31, glassBottomY - 38)
                };
                g.FillPolygon(shineBrush, highlight1);
                using (SolidBrush shineSoft = new SolidBrush(Color.FromArgb(110, 255, 255, 255)))
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
