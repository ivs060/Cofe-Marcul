using System;
using System.Collections.Generic;
using CafeMarkul.Model;

namespace CafeMarkul.Controller
{
    public class GameObjective
    {
        public string Text { get; private set; }
        public bool IsCompleted { get; set; }

        public GameObjective(string text)
        {
            Text = text;
        }

        public void SetText(string text)
        {
            Text = text;
        }
    }

    public class InteractionZone
    {
        public string Id { get; private set; }
        public string Prompt { get; private set; }
        public double X { get; private set; }
        public double Z { get; private set; }
        public double Radius { get; private set; }
        public bool RequireLook { get; private set; }
        public bool Enabled { get; set; }

        public InteractionZone(string id, string prompt, double x, double z, double radius, bool requireLook = false)
        {
            Id = id;
            Prompt = prompt;
            X = x;
            Z = z;
            Radius = radius;
            RequireLook = requireLook;
            Enabled = true;
        }

        public bool IsNear(Camera3D camera)
        {
            if (!Enabled || camera == null)
                return false;

            double dx = camera.X - X;
            double dz = camera.Z - Z;
            return dx * dx + dz * dz <= Radius * Radius;
        }

        public bool IsAvailable(Camera3D camera)
        {
            if (!IsNear(camera))
                return false;

            // Любое взаимодействие должно быть хотя бы примерно в поле зрения.
            // RequireLook оставлен для совместимости, но больше не разрешает взаимодействовать спиной.
            return IsCameraLookingAt(camera);
        }

        private bool IsCameraLookingAt(Camera3D camera)
        {
            double dx = X - camera.X;
            double dz = Z - camera.Z;
            double length = Math.Sqrt(dx * dx + dz * dz);

            if (length < 0.001)
                return true;

            dx /= length;
            dz /= length;

            // В этой сцене направление взгляда камеры по XZ:
            // X = sin(Yaw), Z = cos(Yaw).
            double forwardX = Math.Sin(camera.Yaw);
            double forwardZ = Math.Cos(camera.Yaw);
            double dot = forwardX * dx + forwardZ * dz;

            return dot >= 0.42;
        }
    }

    public class HorrorGameController
    {
        private enum StoryStage
        {
            Day1Intro,
            Day1WorkTasks,
            Day1Client1,
            Day1Client2,
            Day1EndShift,
            Day2Intro,
            Day2WorkTasks,
            Day2Client1,
            Day2Client2Scare,
            Day2EndShift,
            Day3Intro,
            Day3WorkTasks,
            Day3ClientDisappear,
            Day3RunToCorridor,
            Finished
        }

        private enum ClientDialogueStage
        {
            None,
            Greeting,
            WaitingChoice,
            PlayerAnswerPolite,
            ClientOrderPolite,
            PaymentPolite,
            PlayerAnswerRude,
            PlayerRudeWhisper,
            ClientOrderRude,
            PaymentRude,
            Finished
        }

        private readonly Camera3D _camera;
        private readonly Action<bool> _setLightSwitchVisual;
        private readonly Action<bool> _setEveningWindowLight;
        private readonly Action<bool> _setDirtyCupsTable1Visible;
        private readonly Action<bool> _setDirtyCupsTable2Visible;
        private readonly Action<bool> _setCoffeeStainsTable1Visible;
        private readonly Action<bool> _setCoffeeStainsTable2Visible;
        private readonly Action<bool> _setReturnedShelfCupsVisible;
        private readonly Action<bool> _setTakeawayShelfCupVisible;
        private readonly Action<bool> _setBigGlassShelfCupVisible;
        private readonly Action<bool, bool> _setTiaOrderExchangeVisible;
        private readonly Action<bool, bool> _setMikeOrderExchangeVisible;
        private readonly Action<bool> _setTvScreenVisible;
        private readonly Action<bool> _setCashRecipeScreenVisible;
        private readonly Action<bool, double> _setCoffeeMachineCupState;
        private readonly Action<bool> _setCoffeeMachinePaperCupMode;
        private readonly Action<double> _setRaspberryPumpPressed;
        private readonly Action<bool, double> _setFridgeOpenAnimation;
        private readonly Action<bool> _setClientVisible;
        private readonly Action<double, double, double> _setClientTransform;
        private readonly Action<bool> _setClientSpeaking;
        private readonly Action<bool> _setMikeVisible;
        private readonly Action<double, double, double> _setMikeTransform;
        private readonly Action<bool> _setMikeSpeaking;
        private readonly Action<bool> _setMikeWideEyesVisible;
        private readonly Action<bool> _setMikeHeadTrackingActive;
        private readonly Action<double> _setMikeSmileProgress;
        private readonly Action<bool> _setMikeHoldingOrderVisible;
        private readonly Action<bool> _setCoffeeBeanFrontBagVisible;
        private readonly Action<bool, double> _setCoffeeMachineRefillAnimation;
        private readonly Action<bool, double> _setIceMakerLidAnimation;
        private readonly Action<bool, double> _setSinkWashAnimation;
        private readonly Action<bool> _setTiaHoldingCupVisible;
        private readonly Action<string> _playSound;
        private readonly Action<string, double> _playSoundFor;
        private readonly Action<string> _startLoopingSound;
        private readonly Action<string> _stopLoopingSound;
        private readonly Action<string, double> _setLoopingSoundVolume;
        private readonly Action<string> _showDayTransitionOverlay;
        private readonly List<GameObjective> _objectives = new List<GameObjective>();
        private readonly List<InteractionZone> _zones = new List<InteractionZone>();

        private bool _table1CupsCollected;
        private bool _table2CupsCollected;
        private bool _table1Wiped;
        private bool _table2Wiped;
        private bool _cupsWashed;
        private bool _cupsReturnedToShelf;
        private bool _sinkWashInProgress;
        private double _sinkWashTimer;
        private bool _raspberryLatteQuestStarted;
        private bool _recipeOpenedForLatte;
        private bool _mikeEspressoQuestStarted;
        private bool _recipeOpenedForMikeEspresso;
        private bool _takeawayCupTaken;
        private bool _bigGlassTaken;
        private bool _hasTakeawayCup;
        private bool _hasBigGlassCup;
        private bool _fridgeOpen;
        private bool _fridgeAnimating;
        private bool _fridgeTargetOpen;
        private double _fridgeAnimationTimer;
        private int _coffeePortionsAdded;
        private int _milkPortionsAdded;
        private int _raspberrySyrupPortionsAdded;
        private bool _coffeeBrewingInProgress;
        private double _coffeeBrewingTimer;
        private bool _iceAddedToBigGlass;
        private bool _iceAddingInProgress;
        private double _iceAddingTimer;
        private bool _mikeOrderReadyToServe;
        private bool _mikeOrderServed;
        private bool _mikePaymentBillVisible;
        private bool _mikeServedGlassVisibleUntilThanksEnds;
        private double _raspberryPumpAnimationTimer;
        private bool _latteReadyToServe;
        private bool _coffeeBeansTaken;
        private bool _coffeeMachineRefillInProgress;
        private double _coffeeMachineRefillTimer;
        private readonly Action<bool> _setTiaBarPassageBlocked;
        private bool _tvTurnedOn;
        private bool _day1IntroLineShown;
        private bool _day1SecondIntroLineScheduled;
        private bool _day1SecondIntroLineShown;
        private double _queuedBottomTextDelay;
        private double _queuedBottomTextDuration;
        private string _queuedBottomText;
        private bool _day1VacationTextShowing;
        private bool _day1EndShiftStarted;
        private bool _eveningWindowLight;
        private bool _delayedRagSoundActive;
        private double _delayedRagSoundTimer;
        private string _pendingDayTransitionTitle;

        private StoryStage _stage;
        private double _messageTimer;
        private double _flickerTimer;
        private bool _screenFlash;
        private bool _timePassTransitionActive;
        private double _timePassTransitionTimer;
        private bool _timePassStartDelayActive;
        private double _timePassStartDelayTimer;

        private bool _clientWalking;
        private bool _clientLeaving;
        private bool _clientGreetingShown;
        private bool _clientReadyToServe;
        private bool _clientOrderFinished;
        private bool _tiaPaymentBillVisible;
        private bool _tiaPaymentCollected;
        private bool _tiaLeavingDelayActive;
        private double _tiaLeavingDelayTimer;
        private bool _tiaLeftForToday;
        private bool _cashDisplayVisible;
        private int _cashAmount;
        private bool _tiaRudeChoice;
        private bool _tiaThanksExchangePending;
        private bool _tiaServedCupVisibleUntilThanksEnds;
        private bool _recipeOverlayVisible;
        private bool _clientCountdownActive;
        private bool _returnToCounterCompleted;
        private double _clientCountdownTimer;
        private double _clientX;
        private double _clientZ;
        private double _clientYaw;
        private bool _mikeWalking;
        private bool _mikeAtCounter;
        private bool _mikeArrivalStarted;
        private bool _mikeDialogueActive;
        private bool _clientInteractionTextLockActive;
        private bool _mikeOrderTaken;
        private bool _mikeLeaving;
        private bool _mikeLeftForToday;
        private bool _mikeOrderHandoffActive;
        private bool _mikeWideEyesDelayActive;
        private double _mikeWideEyesDelayTimer;
        private bool _mikeWideEyesActive;
        private bool _mikeHeadTrackingActive;
        private bool _mikeLeavingHeadTrackingDelayActive;
        private double _mikeLeavingHeadTrackingDelayTimer;
        private bool _mikeFarewellSmileActive;
        private double _mikeFarewellSmileTimer;
        private bool _mikeAutoLeaveAfterSmileActive;
        private double _mikeAutoLeaveAfterSmileTimer;
        private bool _mikeDay1MikeMusicPlaying;
        private bool _mikePaymentCollected;
        private int _mikeDialogueIndex;
        private double _mikeX;
        private double _mikeZ;
        private double _mikeYaw;

        private ClientDialogueStage _clientDialogueStage;
        private bool _choiceActive;
        private string _choiceOption1;
        private string _choiceOption2;

        private const double ClientStartX = 252;
        private const double ClientStartZ = 300;
        // Клиентка останавливается на стороне посетителей перед стойкой,
        // в точке между кассой и ковриком welcome, ближе к нужной позиции у бара.
        private const double ClientTargetX = 5;
        private const double ClientTargetZ = 365;
        private const double ClientMoveSpeed = 58;
        private const double MikeStartX = 252;
        private const double MikeStartZ = 300;
        private const double MikeTargetX = 5;
        private const double MikeTargetZ = 365;
        private const double MikeMoveSpeed = 58;
        private const double MikeWideEyesDelayAfterIce = 1.0;
        private const double MikeGiveOrderPause = 1.5;
        private const double MikeStopLookingAfterLeavingDelay = 1.0;
        private const double MikeFarewellDuration = 2.5;
        private const double MikeFarewellSmileStretchDuration = 2.5;
        private const double MikeAutoLeaveDelayAfterSmile = 2.0;
        private const double MikeDay1MikeVolume = 0.5;
        private const double MikeDay1MikeFadeDuration = 1.0;
        private const double CoffeeBrewDuration = 3.0;
        private const double CoffeeMachineRefillDuration = 4.0;
        private const double IceAddingDuration = 1.0;
        private const double SinkWashDuration = 2.4;
        private const double RaspberryPumpAnimationDuration = 0.42;
        private const double FridgeDoorAnimationDuration = 1.0;
        private const double TiaLeavingDelayAfterPayment = 2.0;
        private const double TimePassFadeToBlackDuration = 2.0;
        private const double TimePassTextFadeDuration = 1.0;
        private const double TimePassTextHoldDuration = 2.0;

        public int CurrentDay { get; private set; }
        public bool LightOn { get; private set; }
        public bool IsLightFlickering { get; private set; }
        public string BottomText { get; private set; }
        public string CenterText { get; private set; }
        public string PromptText { get; private set; }
        public int Reputation { get; private set; }
        public bool RecipeOverlayVisible { get { return _recipeOverlayVisible; } }
        public bool MikeRecipeActive { get { return _mikeEspressoQuestStarted; } }
        public bool HasTakeawayCup { get { return _hasTakeawayCup; } }
        public bool HeldTakeawayCupVisible { get { return _hasTakeawayCup && !_coffeeBrewingInProgress; } }
        public bool HeldBigGlassVisible { get { return _hasBigGlassCup && !_coffeeBrewingInProgress; } }
        public int CoffeePortionsInCup { get { return _coffeePortionsAdded; } }
        public bool BigGlassHasIce { get { return _iceAddedToBigGlass; } }
        public int MilkPortionsInCup { get { return _milkPortionsAdded; } }
        public int RaspberrySyrupPortionsInCup { get { return _raspberrySyrupPortionsAdded; } }
        public bool IsCoffeeBrewing { get { return _coffeeBrewingInProgress; } }
        public bool IsRaspberryPumpAnimating { get { return _raspberryPumpAnimationTimer > 0; } }
        public double RaspberryPumpAnimationProgress
        {
            get
            {
                if (_raspberryPumpAnimationTimer <= 0)
                    return 0;

                double progress = 1.0 - _raspberryPumpAnimationTimer / RaspberryPumpAnimationDuration;
                if (progress < 0)
                    progress = 0;
                if (progress > 1)
                    progress = 1;
                return progress;
            }
        }
        public double CoffeeBrewingProgress
        {
            get
            {
                if (!_coffeeBrewingInProgress)
                    return 0;

                double progress = 1.0 - _coffeeBrewingTimer / CoffeeBrewDuration;
                if (progress < 0)
                    progress = 0;
                if (progress > 1)
                    progress = 1;
                return progress;
            }
        }
        public bool IsChoiceActive { get { return _choiceActive; } }
        public bool IsPlayerMovementLocked
        {
            get
            {
                return IsClientDialogueActive || _clientLeaving || _mikeLeaving;
            }
        }

        public bool IsPlayerLookLocked
        {
            get
            {
                return IsClientDialogueActive || _clientLeaving || _mikeLeaving;
            }
        }

        private bool IsClientDialogueActive
        {
            get
            {
                return
                    _mikeDialogueActive ||
                    _mikeOrderHandoffActive ||
                    _clientInteractionTextLockActive ||
                    _choiceActive ||
                    (_clientDialogueStage != ClientDialogueStage.None &&
                     _clientDialogueStage != ClientDialogueStage.Finished);
            }
        }
        public string ChoiceOption1 { get { return _choiceOption1; } }
        public string ChoiceOption2 { get { return _choiceOption2; } }
        public IReadOnlyList<GameObjective> Objectives { get { return _objectives; } }
        public bool ScreenFlash { get { return _screenFlash; } }
        public bool CashDisplayVisible { get { return _cashDisplayVisible; } }
        public int CashAmount { get { return _cashAmount; } }
        public bool TimePassTransitionVisible { get { return _timePassTransitionActive; } }
        public bool EveningWindowLight { get { return _eveningWindowLight; } }
        public string TimePassTransitionText { get { return "спустя некоторое время"; } }
        public double TimePassTransitionElapsed { get { return _timePassTransitionTimer; } }
        public double TimePassBlackAlpha
        {
            get
            {
                if (!_timePassTransitionActive)
                    return 0;

                double alpha = _timePassTransitionTimer / TimePassFadeToBlackDuration;
                if (alpha < 0)
                    alpha = 0;
                if (alpha > 1)
                    alpha = 1;
                return alpha;
            }
        }
        public double TimePassTextAlpha
        {
            get
            {
                if (!_timePassTransitionActive)
                    return 0;

                double textStart = TimePassFadeToBlackDuration;
                double alpha = (_timePassTransitionTimer - textStart) / TimePassTextFadeDuration;
                if (alpha < 0)
                    alpha = 0;
                if (alpha > 1)
                    alpha = 1;
                return alpha;
            }
        }

        public string ConsumePendingDayTransitionTitle()
        {
            string title = _pendingDayTransitionTitle;
            _pendingDayTransitionTitle = null;
            return title;
        }

        public HorrorGameController(
            Camera3D camera,
            Action<bool> setLightSwitchVisual,
            Action<bool> setDirtyCupsTable1Visible,
            Action<bool> setDirtyCupsTable2Visible,
            Action<bool> setCoffeeStainsTable1Visible,
            Action<bool> setCoffeeStainsTable2Visible,
            Action<bool> setReturnedShelfCupsVisible,
            Action<bool> setTakeawayShelfCupVisible,
            Action<bool> setBigGlassShelfCupVisible,
            Action<bool, bool> setTiaOrderExchangeVisible,
            Action<bool, bool> setMikeOrderExchangeVisible,
            Action<bool> setTvScreenVisible,
            Action<bool> setCashRecipeScreenVisible,
            Action<bool, double> setCoffeeMachineCupState,
            Action<bool> setCoffeeMachinePaperCupMode,
            Action<double> setRaspberryPumpPressed,
            Action<bool, double> setFridgeOpenAnimation,
            Action<bool> setClientVisible,
            Action<double, double, double> setClientTransform,
            Action<bool> setClientSpeaking,
            Action<bool> setMikeVisible,
            Action<double, double, double> setMikeTransform,
            Action<bool> setMikeSpeaking,
            Action<bool> setMikeWideEyesVisible,
            Action<bool> setMikeHeadTrackingActive,
            Action<double> setMikeSmileProgress,
            Action<bool> setMikeHoldingOrderVisible,
            Action<bool> setCoffeeBeanFrontBagVisible,
            Action<bool, double> setCoffeeMachineRefillAnimation,
            Action<bool, double> setIceMakerLidAnimation,
            Action<bool, double> setSinkWashAnimation,
            Action<bool> setTiaHoldingCupVisible,
            Action<bool> setTiaBarPassageBlocked,
            Action<string> playSound = null,
            Action<string, double> playSoundFor = null,
            Action<string> startLoopingSound = null,
            Action<string> stopLoopingSound = null,
            Action<string, double> setLoopingSoundVolume = null,
            Action<bool> setEveningWindowLight = null,
            Action<string> showDayTransitionOverlay = null)
        {
            _camera = camera;
            _setLightSwitchVisual = setLightSwitchVisual;
            _setEveningWindowLight = setEveningWindowLight;
            _setDirtyCupsTable1Visible = setDirtyCupsTable1Visible;
            _setDirtyCupsTable2Visible = setDirtyCupsTable2Visible;
            _setCoffeeStainsTable1Visible = setCoffeeStainsTable1Visible;
            _setCoffeeStainsTable2Visible = setCoffeeStainsTable2Visible;
            _setReturnedShelfCupsVisible = setReturnedShelfCupsVisible;
            _setTakeawayShelfCupVisible = setTakeawayShelfCupVisible;
            _setBigGlassShelfCupVisible = setBigGlassShelfCupVisible;
            _setTiaOrderExchangeVisible = setTiaOrderExchangeVisible;
            _setMikeOrderExchangeVisible = setMikeOrderExchangeVisible;
            _setTvScreenVisible = setTvScreenVisible;
            _setCashRecipeScreenVisible = setCashRecipeScreenVisible;
            _setCoffeeMachineCupState = setCoffeeMachineCupState;
            _setCoffeeMachinePaperCupMode = setCoffeeMachinePaperCupMode;
            _setRaspberryPumpPressed = setRaspberryPumpPressed;
            _setFridgeOpenAnimation = setFridgeOpenAnimation;
            _setClientVisible = setClientVisible;
            _setClientTransform = setClientTransform;
            _setClientSpeaking = setClientSpeaking;
            _setMikeVisible = setMikeVisible;
            _setMikeTransform = setMikeTransform;
            _setMikeSpeaking = setMikeSpeaking;
            _setMikeWideEyesVisible = setMikeWideEyesVisible;
            _setMikeHeadTrackingActive = setMikeHeadTrackingActive;
            _setMikeSmileProgress = setMikeSmileProgress;
            _setMikeHoldingOrderVisible = setMikeHoldingOrderVisible;
            _setClientSpeaking?.Invoke(false);
            _setMikeSpeaking?.Invoke(false);
            _setCoffeeBeanFrontBagVisible = setCoffeeBeanFrontBagVisible;
            _setCoffeeMachineRefillAnimation = setCoffeeMachineRefillAnimation;
            _setIceMakerLidAnimation = setIceMakerLidAnimation;
            _setSinkWashAnimation = setSinkWashAnimation;
            _setTiaHoldingCupVisible = setTiaHoldingCupVisible;
            _setTiaBarPassageBlocked = setTiaBarPassageBlocked;
            _playSound = playSound;
            _playSoundFor = playSoundFor;
            _startLoopingSound = startLoopingSound;
            _stopLoopingSound = stopLoopingSound;
            _setLoopingSoundVolume = setLoopingSoundVolume;
            _showDayTransitionOverlay = showDayTransitionOverlay;
            CreateZones();
            StartDay1();
        }

        private void PlaySound(string soundName)
        {
            if (_playSound != null)
                _playSound(soundName);
        }

        private void PlaySoundFor(string soundName, double maxSeconds)
        {
            if (_playSoundFor != null)
                _playSoundFor(soundName, maxSeconds);
        }

        private void StartLoopingSound(string soundName)
        {
            if (_startLoopingSound != null)
                _startLoopingSound(soundName);
        }

        private void StopLoopingSound(string soundName)
        {
            if (_stopLoopingSound != null)
                _stopLoopingSound(soundName);
        }

        private void SetLoopingSoundVolume(string soundName, double volumeMultiplier)
        {
            if (_setLoopingSoundVolume != null)
                _setLoopingSoundVolume(soundName, volumeMultiplier);
        }

        private void StartDay1MikeMusic()
        {
            _mikeDay1MikeMusicPlaying = true;
            StartLoopingSound("Day1Mike");
            SetLoopingSoundVolume("Day1Mike", MikeDay1MikeVolume);
        }

        private void StopDay1MikeMusic()
        {
            _mikeDay1MikeMusicPlaying = false;
            StopLoopingSound("Day1Mike");
        }

        private void FinishCurrentBottomText()
        {
            if (_tiaServedCupVisibleUntilThanksEnds &&
                !string.IsNullOrWhiteSpace(BottomText) &&
                BottomText.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase))
            {
                _tiaPaymentBillVisible = true;
                _setTiaOrderExchangeVisible?.Invoke(false, true);
                _setTiaHoldingCupVisible?.Invoke(true);
                _tiaServedCupVisibleUntilThanksEnds = false;
            }

            if (_mikeServedGlassVisibleUntilThanksEnds &&
                !string.IsNullOrWhiteSpace(BottomText) &&
                BottomText.StartsWith("Майк:", StringComparison.OrdinalIgnoreCase))
            {
                _mikeServedGlassVisibleUntilThanksEnds = false;
                _mikeOrderHandoffActive = false;
                _mikeFarewellSmileActive = true;
                _mikeFarewellSmileTimer = 0;
                _setMikeSmileProgress?.Invoke(0.01);
                StartDay1MikeMusic();
            }

            BottomText = null;
            _messageTimer = 0;
            UpdateDialogueSpeakerMouths(null);
            CenterText = null;
            _screenFlash = false;
            _clientInteractionTextLockActive = false;
            HandleTimedTextFinished();
        }

        private void RequestDayTransitionOverlay(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;

            if (_showDayTransitionOverlay != null)
            {
                _pendingDayTransitionTitle = null;
                _showDayTransitionOverlay(title);
            }
            else
            {
                _pendingDayTransitionTitle = title;
            }
        }

        public void Update(double deltaTime)
        {
            if (_messageTimer > 0)
            {
                _messageTimer -= deltaTime;
                if (_messageTimer <= 0)
                    FinishCurrentBottomText();
            }

            if (_queuedBottomText != null)
            {
                _queuedBottomTextDelay -= deltaTime;

                if (_queuedBottomTextDelay <= 0)
                {
                    string queuedText = _queuedBottomText;
                    ShowBottomText(queuedText, _queuedBottomTextDuration);

                    if (_tiaThanksExchangePending &&
                        !string.IsNullOrWhiteSpace(queuedText) &&
                        queuedText.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase))
                    {
                        _setTiaOrderExchangeVisible?.Invoke(true, false);
                        _tiaThanksExchangePending = false;
                        _tiaServedCupVisibleUntilThanksEnds = true;
                    }

                    if (!string.IsNullOrWhiteSpace(queuedText) &&
                        queuedText.Equals("Ладно, пора домой. Надо выключить свет и телевизор, разгребу все завтра", StringComparison.OrdinalIgnoreCase))
                    {
                        _day1VacationTextShowing = true;
                    }

                    _queuedBottomText = null;
                    _queuedBottomTextDelay = 0;
                    _queuedBottomTextDuration = 0;
                }
            }

            if (IsLightFlickering)
            {
                _flickerTimer += deltaTime;
                // Пока это только флаг для затемнения экрана в Form1.
                // Позже можно связать с цветом лампы/полигонами сцены.
            }

            UpdateClientCountdown(deltaTime);
            UpdateTiaLeavingDelay(deltaTime);
            UpdateTimePassStartDelay(deltaTime);
            UpdateDelayedRagSound(deltaTime);
            UpdateCoffeeMachineRefill(deltaTime);
            UpdateSinkWashAnimation(deltaTime);
            UpdateCoffeeBrewing(deltaTime);
            UpdateIceAddingAnimation(deltaTime);
            UpdateRaspberryPumpAnimation(deltaTime);
            UpdateFridgeAnimation(deltaTime);
            double machineFillProgress = _coffeeBrewingInProgress
                ? (_coffeePortionsAdded + CoffeeBrewingProgress) / 3.0
                : _coffeePortionsAdded / 3.0;
            _setCoffeeMachineCupState?.Invoke(_coffeeBrewingInProgress, machineFillProgress);
            _setRaspberryPumpPressed?.Invoke(IsRaspberryPumpAnimating ? RaspberryPumpAnimationProgress : 0);
            UpdateReturnToCounterObjective();
            UpdateClient(deltaTime);
            UpdateMike(deltaTime);
            UpdateMikeLeavingHeadTrackingDelay(deltaTime);
            UpdateMikeWideEyesDelay(deltaTime);
            UpdateMikeHeadTracking(deltaTime);
            UpdateMikeFarewellSmile(deltaTime);
            UpdateMikeAutoLeaveAfterSmile(deltaTime);
            UpdateLeavingCameraFollow(deltaTime);
            UpdateTimePassTransition(deltaTime);
            UpdatePrompt();
        }

        public bool IsAtTimePassSavePoint
        {
            get { return _timePassTransitionActive; }
        }

        public void RestoreAtTimePassSavePoint()
        {
            CurrentDay = 1;
            _stage = StoryStage.Day1EndShift;
            LightOn = true;
            IsLightFlickering = false;
            _setLightSwitchVisual?.Invoke(true);
            _setEveningWindowLight?.Invoke(false);
            _tvTurnedOn = true;
            _setTvScreenVisible?.Invoke(true);
            _cashDisplayVisible = false;
            _cashAmount = 0;
            _recipeOverlayVisible = false;
            _objectives.Clear();

            _table1CupsCollected = true;
            _table2CupsCollected = true;
            _table1Wiped = true;
            _table2Wiped = true;
            _cupsWashed = true;
            _cupsReturnedToShelf = true;
            _sinkWashInProgress = false;
            _sinkWashTimer = 0;
            _coffeeBeansTaken = true;
            _coffeeMachineRefillInProgress = false;
            _coffeeMachineRefillTimer = 0;
            _setDirtyCupsTable1Visible?.Invoke(false);
            _setDirtyCupsTable2Visible?.Invoke(false);
            _setCoffeeStainsTable1Visible?.Invoke(false);
            _setCoffeeStainsTable2Visible?.Invoke(false);
            _setReturnedShelfCupsVisible?.Invoke(true);
            _setSinkWashAnimation?.Invoke(false, 0);
            _setCoffeeBeanFrontBagVisible?.Invoke(false);
            _setCoffeeMachineRefillAnimation?.Invoke(false, 0);

            _raspberryLatteQuestStarted = true;
            _recipeOpenedForLatte = true;
            _takeawayCupTaken = true;
            _hasTakeawayCup = false;
            _coffeePortionsAdded = 1;
            _milkPortionsAdded = 3;
            _raspberrySyrupPortionsAdded = 1;
            _latteReadyToServe = false;
            _coffeeBrewingInProgress = false;
            _coffeeBrewingTimer = 0;
            _setTakeawayShelfCupVisible?.Invoke(false);
            _setCoffeeMachineCupState?.Invoke(false, 0);
            _setCoffeeMachinePaperCupMode?.Invoke(false);
            _fridgeOpen = false;
            _fridgeAnimating = false;
            _fridgeTargetOpen = false;
            _fridgeAnimationTimer = 0;
            _setFridgeOpenAnimation?.Invoke(false, 0);

            _clientCountdownActive = false;
            _clientWalking = false;
            _clientLeaving = false;
            _clientGreetingShown = true;
            _clientReadyToServe = false;
            _clientOrderFinished = true;
            _clientDialogueStage = ClientDialogueStage.Finished;
            _choiceActive = false;
            _setClientVisible?.Invoke(false);
            _setClientSpeaking?.Invoke(false);
            _setTiaBarPassageBlocked?.Invoke(true);
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            _setTiaHoldingCupVisible?.Invoke(false);
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = true;
            _tiaLeftForToday = true;

            _mikeWalking = false;
            _mikeLeaving = false;
            _mikeAtCounter = false;
            _mikeArrivalStarted = false;
            _mikeLeftForToday = false;
            _mikeOrderTaken = false;
            _mikeEspressoQuestStarted = false;
            _mikeOrderReadyToServe = false;
            _mikeOrderServed = false;
            _setMikeVisible?.Invoke(false);
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeHoldingOrderVisible?.Invoke(false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);

            BottomText = null;
            CenterText = null;
            PromptText = null;
            _messageTimer = 0;
            _queuedBottomText = null;
            _queuedBottomTextDelay = 0;
            _timePassStartDelayActive = false;
            _timePassStartDelayTimer = 0;
            _timePassTransitionActive = true;
            _timePassTransitionTimer = 0;
        }

        public void DebugJumpToGiveOrderObjective()
        {
            StopLoopingSound("CoffeeIsPouringOut");
            StopLoopingSound("coffeeMachine");
            StopLoopingSound("washingCups");
            StopLoopingSound("Stomp");
            _delayedRagSoundActive = false;
            _delayedRagSoundTimer = 0;

            CurrentDay = 1;
            _stage = StoryStage.Day1WorkTasks;
            LightOn = true;
            IsLightFlickering = false;
            _setLightSwitchVisual?.Invoke(true);
            _setEveningWindowLight?.Invoke(true);
            _cashDisplayVisible = true;
            _cashAmount = 250;
            _recipeOverlayVisible = false;
            _setCashRecipeScreenVisible?.Invoke(true);

            _table1CupsCollected = true;
            _table2CupsCollected = true;
            _table1Wiped = true;
            _table2Wiped = true;
            _cupsWashed = true;
            _cupsReturnedToShelf = true;
            _sinkWashInProgress = false;
            _sinkWashTimer = 0;
            _coffeeBeansTaken = true;
            _coffeeMachineRefillInProgress = false;
            _coffeeMachineRefillTimer = 0;
            _setDirtyCupsTable1Visible?.Invoke(false);
            _setDirtyCupsTable2Visible?.Invoke(false);
            _setCoffeeStainsTable1Visible?.Invoke(false);
            _setCoffeeStainsTable2Visible?.Invoke(false);
            _setReturnedShelfCupsVisible?.Invoke(true);
            _setSinkWashAnimation?.Invoke(false, 0);
            _setCoffeeBeanFrontBagVisible?.Invoke(false);
            _setCoffeeMachineRefillAnimation?.Invoke(false, 0);

            _clientCountdownActive = false;
            _clientCountdownTimer = 0;
            _clientWalking = false;
            _clientLeaving = false;
            _clientGreetingShown = true;
            _clientReadyToServe = false;
            _clientOrderFinished = true;
            _clientDialogueStage = ClientDialogueStage.Finished;
            _choiceActive = false;
            _choiceOption1 = null;
            _choiceOption2 = null;
            _clientX = ClientTargetX;
            _clientZ = ClientTargetZ;
            _clientYaw = 0;
            _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
            _setClientVisible?.Invoke(false);
            _setTiaBarPassageBlocked?.Invoke(false);
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            _setTiaHoldingCupVisible?.Invoke(false);
            _tiaRudeChoice = false;
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = false;
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = false;
            _tiaLeavingDelayActive = false;
            _tiaLeavingDelayTimer = 0;
            _tiaLeftForToday = true;
            _latteReadyToServe = false;
            _takeawayCupTaken = false;
            _hasTakeawayCup = false;
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);

            _mikeWalking = false;
            _mikeLeaving = false;
            _mikeAtCounter = true;
            _mikeArrivalStarted = true;
            _mikeLeftForToday = false;
            _mikeDialogueActive = false;
            _clientInteractionTextLockActive = false;
            _mikeOrderTaken = true;
            _mikeEspressoQuestStarted = true;
            _recipeOpenedForMikeEspresso = true;
            _bigGlassTaken = true;
            _hasBigGlassCup = true;
            _coffeePortionsAdded = 2;
            _milkPortionsAdded = 0;
            _raspberrySyrupPortionsAdded = 0;
            _fridgeOpen = false;
            _fridgeAnimating = false;
            _fridgeTargetOpen = false;
            _fridgeAnimationTimer = 0;
            _setFridgeOpenAnimation?.Invoke(false, 0);
            _coffeeBrewingInProgress = false;
            _coffeeBrewingTimer = 0;
            _iceAddedToBigGlass = true;
            _iceAddingInProgress = false;
            _iceAddingTimer = 0;
            _mikeOrderReadyToServe = true;
            _mikeOrderServed = false;
            _mikePaymentBillVisible = false;
            _mikeServedGlassVisibleUntilThanksEnds = false;
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeWideEyesActive = false;
            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeOrderHandoffActive = false;
            _mikeHeadTrackingActive = false;
            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = 0;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikePaymentCollected = false;
            _setMikeSmileProgress?.Invoke(0);
            _setMikeHoldingOrderVisible?.Invoke(false);
            StopDay1MikeMusic();
            _mikeDialogueIndex = 0;
            _mikeX = MikeTargetX;
            _mikeZ = MikeTargetZ;
            _mikeYaw = 0;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            _setMikeVisible?.Invoke(true);
            _setBigGlassShelfCupVisible?.Invoke(false);
            _setCoffeeMachineCupState?.Invoke(false, 0.0);
            _setIceMakerLidAnimation?.Invoke(false, 0);
            _setRaspberryPumpPressed?.Invoke(0);
            _day1VacationTextShowing = false;
            _day1EndShiftStarted = false;
            _timePassTransitionActive = false;
            _timePassTransitionTimer = 0;
            _timePassStartDelayActive = false;
            _timePassStartDelayTimer = 0;
            _returnToCounterCompleted = true;

            if (_camera != null)
                _camera.Set(380, 0, 470, Math.PI);

            _objectives.Clear();
            _objectives.Add(new GameObjective("включить свет") { IsCompleted = true });
            _objectives.Add(new GameObjective("собрать грязные чашки со стола") { IsCompleted = true });
            _objectives.Add(new GameObjective("помыть чашки") { IsCompleted = true });
            _objectives.Add(new GameObjective("положить чашки на полку") { IsCompleted = true });
            _objectives.Add(new GameObjective("протереть столы") { IsCompleted = true });
            _objectives.Add(new GameObjective("заправить кофемашину") { IsCompleted = true });
            _objectives.Add(new GameObjective("Вернись за прилавок") { IsCompleted = true });
            _objectives.Add(new GameObjective("Приготовить двойной эспрессо") { IsCompleted = true });
            _objectives.Add(new GameObjective("   Посмотреть рецепт") { IsCompleted = true });
            _objectives.Add(new GameObjective("   Взять большой стакан") { IsCompleted = true });
            _objectives.Add(new GameObjective("   Положить лёд в стакан") { IsCompleted = true });
            _objectives.Add(new GameObjective("   Приготовить 2 порции эспрессо") { IsCompleted = true });
            _objectives.Add(new GameObjective("Отдать заказ"));

            BottomText = "DEBUG: перенесла к цели «Отдать заказ: Майк».";
            CenterText = null;
            _messageTimer = 1.8;
            UpdatePrompt();
        }

        public void SelectDialogueChoice(int option)
        {
            if (!_choiceActive || _clientDialogueStage != ClientDialogueStage.WaitingChoice)
                return;

            _choiceActive = false;
            _choiceOption1 = null;
            _choiceOption2 = null;

            if (option == 1)
            {
                _tiaRudeChoice = false;
                _clientDialogueStage = ClientDialogueStage.PlayerAnswerPolite;
                ShowBottomText("Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?", 5.0);
                return;
            }

            if (option == 2)
            {
                _tiaRudeChoice = true;
                Reputation -= 1;
                _clientDialogueStage = ClientDialogueStage.PlayerAnswerRude;
                ShowBottomText("Ага...Чего желаете?", 5.0);
                return;
            }
        }

        public void SkipDialogueMessage()
        {
            if (_choiceActive)
                return;

            if (_messageTimer <= 0 || string.IsNullOrWhiteSpace(BottomText))
                return;

            if (BottomText.Trim().Equals("Майк: До скорой встречи", StringComparison.OrdinalIgnoreCase))
                return;

            bool dialogueTextActive = _clientInteractionTextLockActive ||
                _mikeDialogueActive ||
                (_clientDialogueStage != ClientDialogueStage.None && _clientDialogueStage != ClientDialogueStage.Finished);

            if (!dialogueTextActive)
                return;

            _messageTimer = 0;
            FinishCurrentBottomText();
        }

        public void Interact()
        {
            if (_recipeOverlayVisible)
            {
                _recipeOverlayVisible = false;
                return;
            }

            InteractionZone zone = GetNearestZone();

            if (zone == null)
            {
                ShowBottomText("Здесь не с чем взаимодействовать.", 1.4);
                return;
            }

            switch (zone.Id)
            {
                case "light_switch":
                    InteractLightSwitch();
                    break;

                case "dirty_cups_table1":
                    InteractDirtyCupsTable1();
                    break;

                case "dirty_cups_table2":
                    InteractDirtyCupsTable2();
                    break;

                case "sink":
                    InteractSink();
                    break;

                case "cup_shelf":
                    InteractCupShelf();
                    break;

                case "takeaway_cup_shelf":
                    InteractCupShelf();
                    break;

                case "big_glass_shelf":
                    InteractBigGlassShelf();
                    break;

                case "table1_wipe":
                    InteractWipeTable1();
                    break;

                case "table2_wipe":
                    InteractWipeTable2();
                    break;

                case "client_tia":
                    // У Тии и Майка зона у стойки почти в одной точке.
                    // Если сейчас готов заказ Майку, не даём старой зоне Тии перехватить E
                    // и требовать малиновый латте вместо двойного эспрессо.
                    if (_mikeAtCounter && _mikeOrderReadyToServe && !_mikeOrderServed)
                    {
                        InteractGiveOrderToMike();
                    }
                    else if (FindObjective("отдать заказ") != null && !FindObjective("отдать заказ").IsCompleted)
                    {
                        InteractGiveOrderToTia();
                    }
                    else
                    {
                        StartTiaDialogue();
                    }
                    break;

                case "client_mike":
                    if (_mikeOrderReadyToServe && !_mikeOrderServed)
                        InteractGiveOrderToMike();
                    else
                        StartMikeDialogue();
                    break;

                case "cash_recipe":
                    InteractCashRecipe();
                    break;

                case "coffee_beans":
                    InteractCoffeeBeans();
                    break;

                case "coffee_machine":
                    InteractCoffeeMachine();
                    break;

                case "ice_maker":
                    InteractIceMaker();
                    break;

                case "fridge_milk":
                    InteractFridgeMilk();
                    break;

                case "raspberry_syrup":
                    InteractRaspberrySyrup();
                    break;

                case "tia_payment":
                    InteractTakeTiaPayment();
                    break;

                case "mike_payment":
                    InteractTakeMikePayment();
                    break;

                case "tv":
                    InteractTv();
                    break;

                case "corridor_door":
                    InteractCorridorDoor();
                    break;
            }
        }

        private void CreateZones()
        {
            // Координаты примерные и привязаны к текущей сцене:
            // выключатель около правой служебной двери без стекла, раковина на баре, столы, прилавок, касса, дверь со стеклом.
            // Сам выключатель нарисован на стене около x=299, z=248,
            // но центр зоны чуть сдвинут внутрь комнаты, чтобы игрок мог дотянуться.
            _zones.Add(new InteractionZone("light_switch", "E — включить/выключить свет", 260, 248, 92, true));

            // Грязные кружки нужно именно увидеть/навести взгляд на конкретный стол, поэтому requireLook = true.
            _zones.Add(new InteractionZone("dirty_cups_table1", "E — собрать кружки с первого стола", -150, 155, 72, true));
            _zones.Add(new InteractionZone("dirty_cups_table2", "E — собрать кружку со второго стола", 150, 155, 72, true));
            _zones.Add(new InteractionZone("sink", "E — помыть собранные кружки", 180, 405, 80, true));
            _zones.Add(new InteractionZone("cup_shelf", "E — поставить кружки на полку", -36, 596, 142, true));
            _zones.Add(new InteractionZone("takeaway_cup_shelf", "E — взять стакан одноразовый стакан", 56, 596, 84, true));
            _zones.Add(new InteractionZone("big_glass_shelf", "E — Взять большой стакан", 56, 596, 84, true));

            _zones.Add(new InteractionZone("table1_wipe", "E — протереть первый стол", -150, 155, 72, true));
            _zones.Add(new InteractionZone("table2_wipe", "E — протереть второй стол", 150, 155, 72, true));
            _zones.Add(new InteractionZone("tv", "E — включить телевизор", 0, 70, 90, true));
            _zones.Add(new InteractionZone("client_tia", "E — Обслужить клиента: Тиа", ClientTargetX, ClientTargetZ, 150, true));
            _zones.Add(new InteractionZone("client_mike", "E — Принять заказ: Майк", MikeTargetX, MikeTargetZ, 150, true));
            _zones.Add(new InteractionZone("tia_payment", "E — забрать деньги", 6, 414, 84, true));
            _zones.Add(new InteractionZone("mike_payment", "E — Забрать деньги", 6, 414, 84, true));
            _zones.Add(new InteractionZone("counter", "E — обслужить клиента у прилавка", 60, 360, 95, true));
            _zones.Add(new InteractionZone("cash_recipe", "E — Посмотреть рецепт", 98, 395, 105, true));
            _zones.Add(new InteractionZone("coffee_beans", "E — взять кофейные зёрна", 138, 400, 78, true));
            _zones.Add(new InteractionZone("coffee_machine", "E — сварить кофе", 252, 522, 110, true));
            _zones.Add(new InteractionZone("ice_maker", "E — Добавить лед", 226, 436, 82, true));
            _zones.Add(new InteractionZone("fridge_milk", "E — холодильник", -236, 548, 170, true));
            _zones.Add(new InteractionZone("raspberry_syrup", "E — добавить малиновый сироп", -246, 426, 46, true));
            _zones.Add(new InteractionZone("corridor_door", "E — Дверь", -156, 565, 48, true));
        }

        private void StartDay1()
        {
            Reputation = 0;
            CurrentDay = 1;
            _eveningWindowLight = false;
            _setEveningWindowLight?.Invoke(false);
            LightOn = false;
            _setLightSwitchVisual?.Invoke(LightOn);
            IsLightFlickering = false;
            _stage = StoryStage.Day1Intro;
            _day1IntroLineShown = false;
            _day1SecondIntroLineScheduled = false;
            _day1SecondIntroLineShown = false;
            ClearQueuedBottomText();
            ResetClientState();
            _setCashRecipeScreenVisible?.Invoke(false);
            _recipeOverlayVisible = false;
            _cashDisplayVisible = false;
            _cashAmount = 0;
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = false;
            _tiaLeavingDelayActive = false;
            _tiaLeavingDelayTimer = 0;
            _tiaLeftForToday = false;
            _timePassTransitionActive = false;
            _timePassTransitionTimer = 0;
            _timePassStartDelayActive = false;
            _timePassStartDelayTimer = 0;
            _mikeWalking = false;
            _mikeLeaving = false;
            _mikeAtCounter = false;
            _mikeArrivalStarted = false;
            _mikeLeftForToday = false;
            _mikeDialogueActive = false;
            _clientInteractionTextLockActive = false;
            _mikeOrderTaken = false;
            _mikeOrderReadyToServe = false;
            _mikeOrderServed = false;
            _mikePaymentBillVisible = false;
            _mikeServedGlassVisibleUntilThanksEnds = false;
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeWideEyesActive = false;
            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeOrderHandoffActive = false;
            _mikeHeadTrackingActive = false;
            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = 0;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikePaymentCollected = false;
            _setMikeSmileProgress?.Invoke(0);
            _setMikeHoldingOrderVisible?.Invoke(false);
            StopDay1MikeMusic();
            _mikeDialogueIndex = 0;
            _mikeX = MikeStartX;
            _mikeZ = MikeStartZ;
            _mikeYaw = -1.45;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            _setMikeVisible?.Invoke(false);
            _setTiaHoldingCupVisible?.Invoke(false);
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _mikeEspressoQuestStarted = false;
            _recipeOpenedForMikeEspresso = false;
            _takeawayCupTaken = false;
            _bigGlassTaken = false;
            _hasTakeawayCup = false;
            _hasBigGlassCup = false;
            _tiaRudeChoice = false;
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = false;
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = false;
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            _setTiaHoldingCupVisible?.Invoke(false);
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
            _setBigGlassShelfCupVisible?.Invoke(true);
            _clientCountdownTimer = 0;
            SetStartShiftObjectives();
            _stage = StoryStage.Day1WorkTasks;
        }

        private void StartDay2()
        {
            CurrentDay = 2;
            RequestDayTransitionOverlay("День 2");
            _eveningWindowLight = false;
            _setEveningWindowLight?.Invoke(false);
            LightOn = false;
            _setLightSwitchVisual?.Invoke(LightOn);
            IsLightFlickering = true;
            _stage = StoryStage.Day2WorkTasks;
            ResetClientState();
            _mikeWalking = false;
            _mikeLeaving = false;
            _mikeAtCounter = false;
            _mikeArrivalStarted = false;
            _mikeLeftForToday = false;
            _mikeDialogueActive = false;
            _clientInteractionTextLockActive = false;
            _mikeOrderTaken = false;
            _mikeOrderReadyToServe = false;
            _mikeOrderServed = false;
            _mikePaymentBillVisible = false;
            _mikeServedGlassVisibleUntilThanksEnds = false;
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeWideEyesActive = false;
            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeOrderHandoffActive = false;
            _mikeHeadTrackingActive = false;
            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = 0;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikePaymentCollected = false;
            _setMikeSmileProgress?.Invoke(0);
            _setMikeHoldingOrderVisible?.Invoke(false);
            StopDay1MikeMusic();
            _mikeDialogueIndex = 0;
            _mikeX = MikeStartX;
            _mikeZ = MikeStartZ;
            _mikeYaw = -1.45;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            _setMikeVisible?.Invoke(false);
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _mikeEspressoQuestStarted = false;
            _recipeOpenedForMikeEspresso = false;
            _takeawayCupTaken = false;
            _bigGlassTaken = false;
            _hasTakeawayCup = false;
            _hasBigGlassCup = false;
            _tiaRudeChoice = false;
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = false;
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeWideEyesActive = false;
            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeOrderHandoffActive = false;
            _mikeHeadTrackingActive = false;
            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = 0;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikePaymentCollected = false;
            _setMikeSmileProgress?.Invoke(0);
            _setMikeHoldingOrderVisible?.Invoke(false);
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
            _setBigGlassShelfCupVisible?.Invoke(true);
            _clientCountdownTimer = 0;
            SetStartShiftObjectives();
        }

        private void StartDay3()
        {
            CurrentDay = 3;
            RequestDayTransitionOverlay("День 3");
            _eveningWindowLight = false;
            _setEveningWindowLight?.Invoke(false);
            LightOn = false;
            _setLightSwitchVisual?.Invoke(LightOn);
            IsLightFlickering = true;
            _stage = StoryStage.Day3WorkTasks;
            ResetClientState();
            _mikeWalking = false;
            _mikeLeaving = false;
            _mikeAtCounter = false;
            _mikeArrivalStarted = false;
            _mikeLeftForToday = false;
            _mikeDialogueActive = false;
            _clientInteractionTextLockActive = false;
            _mikeOrderTaken = false;
            _mikeOrderReadyToServe = false;
            _mikeOrderServed = false;
            _mikePaymentBillVisible = false;
            _mikeServedGlassVisibleUntilThanksEnds = false;
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeWideEyesActive = false;
            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeOrderHandoffActive = false;
            _mikeHeadTrackingActive = false;
            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = 0;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikePaymentCollected = false;
            _setMikeSmileProgress?.Invoke(0);
            _setMikeHoldingOrderVisible?.Invoke(false);
            StopDay1MikeMusic();
            _mikeDialogueIndex = 0;
            _mikeX = MikeStartX;
            _mikeZ = MikeStartZ;
            _mikeYaw = -1.45;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            _setMikeVisible?.Invoke(false);
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _mikeEspressoQuestStarted = false;
            _recipeOpenedForMikeEspresso = false;
            _takeawayCupTaken = false;
            _bigGlassTaken = false;
            _hasTakeawayCup = false;
            _hasBigGlassCup = false;
            _tiaRudeChoice = false;
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = false;
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
            _setBigGlassShelfCupVisible?.Invoke(true);
            _clientCountdownTimer = 0;
            SetStartShiftObjectives();
        }

        private void ResetLatteCupState()
        {
            _coffeePortionsAdded = 0;
            _milkPortionsAdded = 0;
            _raspberrySyrupPortionsAdded = 0;
            _coffeeBrewingInProgress = false;
            _coffeeBrewingTimer = 0;
            _iceAddedToBigGlass = false;
            _iceAddingInProgress = false;
            _iceAddingTimer = 0;
            _setIceMakerLidAnimation?.Invoke(false, 0);
            _raspberryPumpAnimationTimer = 0;
            _latteReadyToServe = false;
        }

        private void ResetCoffeeMachineRefillState()
        {
            _coffeeBeansTaken = false;
            _coffeeMachineRefillInProgress = false;
            _coffeeMachineRefillTimer = 0;
            _setCoffeeBeanFrontBagVisible?.Invoke(true);
            _setCoffeeMachineRefillAnimation?.Invoke(false, 0);
        }

        private void SetStartShiftObjectives()
        {
            _table1CupsCollected = false;
            _table2CupsCollected = false;
            _table1Wiped = false;
            _table2Wiped = false;
            _cupsWashed = false;
            _cupsReturnedToShelf = false;
            _sinkWashInProgress = false;
            _sinkWashTimer = 0;
            _setSinkWashAnimation?.Invoke(false, 0);
            StopLoopingSound("washingCups");
            _delayedRagSoundActive = false;
            _delayedRagSoundTimer = 0;
            _tvTurnedOn = false;
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _mikeEspressoQuestStarted = false;
            _recipeOpenedForMikeEspresso = false;
            _takeawayCupTaken = false;
            _bigGlassTaken = false;
            _hasTakeawayCup = false;
            _hasBigGlassCup = false;
            _tiaRudeChoice = false;
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = false;
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
            _setBigGlassShelfCupVisible?.Invoke(true);
            _clientCountdownTimer = 0;
            ResetCoffeeMachineRefillState();
            _day1SecondIntroLineScheduled = false;
            _day1SecondIntroLineShown = false;
            ClearQueuedBottomText();
            _setDirtyCupsTable1Visible?.Invoke(true);
            _setDirtyCupsTable2Visible?.Invoke(true);
            _setCoffeeStainsTable1Visible?.Invoke(true);
            _setCoffeeStainsTable2Visible?.Invoke(true);
            _setReturnedShelfCupsVisible?.Invoke(false);
            _setTvScreenVisible?.Invoke(false);
            _setClientVisible?.Invoke(false);
            _setClientTransform?.Invoke(ClientStartX, ClientStartZ, -1.35);

            _objectives.Clear();
            _objectives.Add(new GameObjective("включить свет"));
        }

        private void AddDirtyCupsObjectiveAfterLight()
        {
            if (FindObjective("собрать грязные чашки со стола") == null)
                _objectives.Add(new GameObjective("собрать грязные чашки со стола"));
        }

        private void SetEndShiftObjectives()
        {
            _objectives.Clear();
            _objectives.Add(new GameObjective("выключить телевизор"));
        }

        private void InteractLightSwitch()
        {
            if (CurrentDay == 3 && !LightOn && _stage == StoryStage.Day3WorkTasks)
            {
                ShowCenterText("ЩЁЛК... ЩЁЛК...", 1.5);
                _screenFlash = true;
                LightOn = true;
                _setLightSwitchVisual?.Invoke(LightOn);
                PlaySound("lamp");
                CompleteObjective("включить свет", "Свет включился не сразу. В отражении будто кто-то стоял за спиной.");
                AddDirtyCupsObjectiveAfterLight();
                return;
            }

            LightOn = !LightOn;
            _setLightSwitchVisual?.Invoke(LightOn);
            PlaySound("lamp");

            if (LightOn)
            {
                if (CurrentDay == 1 && !_day1IntroLineShown)
                {
                    _day1IntroLineShown = true;
                    CompleteObjective("включить свет", null);
                    AddDirtyCupsObjectiveAfterLight();
                    ShowBottomText("Эх... Очередная смена... Очередная неделя без выходных", 5.0);
                    QueueBottomText("Надо подготовиться к началу смены, убрать и помыть все кружки", 5.0, 5.0);
                    _day1SecondIntroLineScheduled = true;
                }
                else
                {
                    CompleteObjective("включить свет", "Свет включён.");
                    AddDirtyCupsObjectiveAfterLight();
                }
            }
            else
            {
                GameObjective turnOffLightObjective = FindObjective("выключить свет");
                bool shouldStartDay2AfterLight =
                    CurrentDay == 1 &&
                    turnOffLightObjective != null &&
                    !turnOffLightObjective.IsCompleted;

                CompleteObjective("выключить свет", "Свет выключен.");

                if (shouldStartDay2AfterLight && CurrentDay == 1)
                    StartDay2();
            }
        }

        private void InteractDirtyCupsTable1()
        {
            if (FindObjective("собрать грязные чашки со стола") == null)
            {
                ShowBottomText("Сейчас кружки трогать не нужно.", 1.5);
                return;
            }

            if (_table1CupsCollected)
            {
                ShowBottomText("Кружки с первого стола уже собраны.", 1.5);
                return;
            }

            _table1CupsCollected = true;
            _setDirtyCupsTable1Visible?.Invoke(false);
            PlaySoundFor("cup", 1.0);

            if (AllDirtyCupsCollected())
                FinishDirtyCupsCollected();
        }

        private void InteractDirtyCupsTable2()
        {
            if (FindObjective("собрать грязные чашки со стола") == null)
            {
                ShowBottomText("Сейчас кружки трогать не нужно.", 1.5);
                return;
            }

            if (_table2CupsCollected)
            {
                ShowBottomText("Кружка со второго стола уже собрана.", 1.5);
                return;
            }

            _table2CupsCollected = true;
            _setDirtyCupsTable2Visible?.Invoke(false);
            PlaySoundFor("cup", 1.0);

            if (AllDirtyCupsCollected())
                FinishDirtyCupsCollected();
        }

        private void FinishDirtyCupsCollected()
        {
            if (FindObjective("помыть чашки") == null)
                _objectives.Add(new GameObjective("помыть чашки"));

            CompleteObjective("собрать грязные чашки со стола", null);
        }

        private bool AllDirtyCupsCollected()
        {
            return _table1CupsCollected && _table2CupsCollected;
        }

        private bool AllTablesWiped()
        {
            return _table1Wiped && _table2Wiped;
        }

        private void InteractWipeTable1()
        {
            if (!_cupsReturnedToShelf || FindObjective("протереть столы") == null)
            {
                ShowBottomText("Сначала нужно вернуть чистые кружки на полку.", 1.6);
                return;
            }

            if (_table1Wiped)
            {
                ShowBottomText("Первый стол уже протёрт.", 1.3);
                return;
            }

            _table1Wiped = true;
            _setCoffeeStainsTable1Visible?.Invoke(false);
            ScheduleDelayedRagSound();

            if (AllTablesWiped())
                FinishTablesWiped();
        }

        private void InteractWipeTable2()
        {
            if (!_cupsReturnedToShelf || FindObjective("протереть столы") == null)
            {
                ShowBottomText("Сначала нужно вернуть чистые кружки на полку.", 1.6);
                return;
            }

            if (_table2Wiped)
            {
                ShowBottomText("Второй стол уже протёрт.", 1.3);
                return;
            }

            _table2Wiped = true;
            _setCoffeeStainsTable2Visible?.Invoke(false);
            ScheduleDelayedRagSound();

            if (AllTablesWiped())
                FinishTablesWiped();
        }

        private void FinishTablesWiped()
        {
            // Сначала появляется новая цель по заправке кофемашины,
            // затем уже отмечается протирка столов.
            if (FindObjective("заправить кофемашину") == null)
                _objectives.Add(new GameObjective("заправить кофемашину"));

            CompleteObjective("протереть столы", null);
            ShowBottomText("заправлю кофемашину зернами. где там мешок с кофе...", 3.8);
        }

        private void InteractCoffeeBeans()
        {
            GameObjective refillObjective = FindObjective("заправить кофемашину");
            if (refillObjective == null || refillObjective.IsCompleted)
            {
                ShowBottomText("Сейчас кофейные зёрна брать не нужно.", 1.5);
                return;
            }

            if (_coffeeMachineRefillInProgress)
            {
                ShowBottomText("Сначала дождись, пока кофемашина заправится.", 1.6);
                return;
            }

            if (_coffeeBeansTaken)
            {
                ShowBottomText("Кофейные зёрна уже у тебя. Подойди к кофемашине.", 1.7);
                return;
            }

            _coffeeBeansTaken = true;
            _setCoffeeBeanFrontBagVisible?.Invoke(false);
            PlaySound("generalInteraction");
        }

        private void InteractTv()
        {
            GameObjective turnOffTv = FindObjective("выключить телевизор");
            if (turnOffTv != null && !turnOffTv.IsCompleted)
            {
                if (FindObjective("выключить свет") == null)
                    _objectives.Add(new GameObjective("выключить свет"));

                if (!_tvTurnedOn)
                {
                    CompleteObjective("выключить телевизор", null);
                    return;
                }

                _tvTurnedOn = false;
                _setTvScreenVisible?.Invoke(false);
                StopLoopingSound("back");
                CompleteObjective("выключить телевизор", null);
                return;
            }

            if (FindObjective("включить телевизор") == null)
            {
                ShowBottomText("Сейчас телевизор не нужен.", 1.4);
                return;
            }

            if (_tvTurnedOn)
            {
                ShowBottomText("Телевизор уже включён.", 1.2);
                return;
            }

            _tvTurnedOn = true;
            _setTvScreenVisible?.Invoke(true);

            // Сначала добавляем последнюю цель в эту же табличку,
            // потом ставим галочку на телевизор.
            if (FindObjective("Вернись за прилавок") == null)
                _objectives.Add(new GameObjective("Вернись за прилавок. Мы открыли смену"));

            _clientCountdownActive = false;
            _clientCountdownTimer = 0;

            CompleteObjective("включить телевизор", "Телевизор включился. На экране появилась старая заставка Cafe Marcul.");
        }

        private void InteractSink()
        {
            if (FindObjective("помыть чашки") == null)
            {
                ShowBottomText("Сейчас раковина не нужна.", 1.4);
                return;
            }

            if (!AllDirtyCupsCollected())
            {
                ShowBottomText("Сначала собери грязные кружки с обоих столов.", 1.8);
                return;
            }

            if (_sinkWashInProgress)
            {
                ShowBottomText("Ты уже моешь кружки у раковины.", 1.4);
                return;
            }

            if (_cupsWashed)
            {
                ShowBottomText("Кружки уже чистые. Осталось поставить их на полку.", 1.8);
                return;
            }

            _sinkWashInProgress = true;
            _sinkWashTimer = 0;
            _setSinkWashAnimation?.Invoke(true, 0);
            StartLoopingSound("washingCups");
        }

        private void InteractCupShelf()
        {
            if (_raspberryLatteQuestStarted && _recipeOpenedForLatte && !_takeawayCupTaken && FindObjective("взять стакан одноразовый стакан") != null)
            {
                _takeawayCupTaken = true;
                _hasTakeawayCup = true;
                ResetLatteCupState();
                _setTakeawayShelfCupVisible?.Invoke(false);
                PlaySound("generalInteraction");
                CompleteLatteSubObjective("взять стакан одноразовый стакан");

                if (_tiaRudeChoice)
                    ShowBottomText("Как же бесят эти выскочки с утра пораньше...", 3.2);
                return;
            }

            if (FindObjective("положить чашки на полку") == null)
            {
                ShowBottomText("Сейчас полку трогать не нужно.", 1.4);
                return;
            }

            if (!AllDirtyCupsCollected())
            {
                ShowBottomText("На полке пустые места. Сначала собери кружки с обоих столов.", 1.8);
                return;
            }

            if (!_cupsWashed)
            {
                ShowBottomText("Нельзя ставить грязные кружки на полку. Сначала помой их.", 1.8);
                return;
            }

            if (_cupsReturnedToShelf)
            {
                ShowBottomText("Кружки уже стоят на месте.", 1.2);
                return;
            }

            _cupsReturnedToShelf = true;
            _setReturnedShelfCupsVisible?.Invoke(true);
            PlaySoundFor("cup", 1.0);

            if (FindObjective("протереть столы") == null)
                _objectives.Add(new GameObjective("протереть столы"));

            CompleteObjective("положить чашки на полку", null);
        }

        private void InteractBigGlassShelf()
        {
            GameObjective objective = FindObjective("Взять большой стакан");
            if (!_mikeEspressoQuestStarted || !_recipeOpenedForMikeEspresso || objective == null)
            {
                ShowBottomText("Сейчас большой стакан не нужен.", 1.4);
                return;
            }

            if (_bigGlassTaken)
            {
                ShowBottomText("Большой стакан уже взят.", 1.2);
                return;
            }

            _bigGlassTaken = true;
            _hasBigGlassCup = true;
            objective.IsCompleted = true;
            _setBigGlassShelfCupVisible?.Invoke(false);
            PlaySoundFor("cup", 1.0);
            ShowBottomText("Какой-то странный тип", 3.2);
        }

        private void InteractCounter()
        {
            if (!AllObjectivesCompleted())
            {
                ShowBottomText("Сначала нужно закончить текущие задачи.", 1.8);
                return;
            }

            if (_stage == StoryStage.Day1WorkTasks)
            {
                _stage = StoryStage.Day1Client1;
                ShowBottomText("Клиент: Один американо. Без сахара.", 3.0);
                return;
            }

            if (_stage == StoryStage.Day1Client1)
            {
                _stage = StoryStage.Day1Client2;
                ShowBottomText("Клиент: Латте. И... не смотрите на дверь.", 3.0);
                return;
            }

            if (_stage == StoryStage.Day1Client2)
            {
                _stage = StoryStage.Day1EndShift;
                ShowCenterText("Конец смены", 2.0);
                SetEndShiftObjectives();
                return;
            }

            if (_stage == StoryStage.Day2WorkTasks)
            {
                _stage = StoryStage.Day2Client1;
                ShowBottomText("Клиент: Капучино. Побыстрее, пожалуйста.", 3.0);
                return;
            }

            if (_stage == StoryStage.Day2Client1)
            {
                _stage = StoryStage.Day2Client2Scare;
                _screenFlash = true;
                IsLightFlickering = true;
                ShowCenterText("!!!", 0.8);
                ShowBottomText("Клиент: Второй заказ... ты уже знаешь.", 3.0);
                return;
            }

            if (_stage == StoryStage.Day2Client2Scare)
            {
                _stage = StoryStage.Day2EndShift;
                ShowCenterText("Конец смены", 2.0);
                SetEndShiftObjectives();
                return;
            }

            if (_stage == StoryStage.Day3WorkTasks)
            {
                _stage = StoryStage.Day3ClientDisappear;
                _screenFlash = true;
                LightOn = false;
                _setLightSwitchVisual?.Invoke(LightOn);
                ShowBottomText("Свет погас. Клиент исчез.", 3.0);
                return;
            }

            if (_stage == StoryStage.Day3ClientDisappear)
            {
                _stage = StoryStage.Day3RunToCorridor;
                IsLightFlickering = true;
                ShowBottomText("Новый клиент стоит слишком близко. Нужно бежать к двери со стеклом.", 4.0);
                _objectives.Clear();
                _objectives.Add(new GameObjective("выбежать в коридор"));
                return;
            }
        }

        private void InteractCorridorDoor()
        {
            ShowBottomText("Не думаю, что нам туда нужно", 2.0);
        }

        private void CompleteObjective(string objectivePart, string message)
        {
            GameObjective objective = FindObjective(objectivePart);

            if (objective == null)
            {
                ShowBottomText("Сейчас это не является целью.", 1.5);
                return;
            }

            if (objective.IsCompleted)
            {
                ShowBottomText("Это уже сделано.", 1.2);
                return;
            }

            objective.IsCompleted = true;

            if (!string.IsNullOrWhiteSpace(message))
                ShowBottomText(message, 2.3);

            if (AllObjectivesCompleted())
                OnObjectivesCompleted();
        }

        private void OnObjectivesCompleted()
        {
            if (_stage == StoryStage.Day1EndShift)
            {
                StartDay2();
                return;
            }

            if (_stage == StoryStage.Day2EndShift)
            {
                StartDay3();
                return;
            }

            // До последней цели ничего не пересобираем:
            // просто оставляем старые строки и галочки на их местах.
            if (_stage == StoryStage.Day1WorkTasks)
            {
                if (_returnToCounterCompleted)
                {
                    ShowBottomText("Смена открыта. Ждём первого клиента.", 3.0);
                    _objectives.Clear();
                }

                return;
            }

            if (_stage == StoryStage.Day2WorkTasks || _stage == StoryStage.Day3WorkTasks)
            {
                _objectives.Clear();
            }
        }

        private void ResetClientState()
        {
            StopLoopingSound("CoffeeIsPouringOut");
            StopLoopingSound("coffeeMachine");
            StopLoopingSound("washingCups");
            StopLoopingSound("Stomp");
            _delayedRagSoundActive = false;
            _delayedRagSoundTimer = 0;

            _clientWalking = false;
            _clientLeaving = false;
            _clientGreetingShown = false;
            _clientReadyToServe = false;
            _clientOrderFinished = false;
            _recipeOverlayVisible = false;
            _setCashRecipeScreenVisible?.Invoke(false);
            _cashDisplayVisible = false;
            _cashAmount = 0;
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = false;
            _setTiaHoldingCupVisible?.Invoke(false);
            _clientCountdownActive = false;
            _clientDialogueStage = ClientDialogueStage.None;
            _choiceActive = false;
            _choiceOption1 = null;
            _choiceOption2 = null;
            _clientCountdownTimer = 0;
            _setCoffeeMachineRefillAnimation?.Invoke(false, 0);
            _setCoffeeBeanFrontBagVisible?.Invoke(true);
            _clientX = ClientStartX;
            _clientZ = ClientStartZ;
            _clientYaw = -1.45;
            _setTiaBarPassageBlocked?.Invoke(false);
            _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
            _setClientVisible?.Invoke(false);
        }

        private void StartClientArrival()
        {
            if (_tiaLeftForToday)
                return;

            _clientCountdownActive = false;
            _clientCountdownTimer = 0;
            _clientWalking = false;
            _clientLeaving = false;
            _clientGreetingShown = false;
            _clientReadyToServe = false;
            _clientOrderFinished = false;
            _recipeOverlayVisible = false;
            _clientDialogueStage = ClientDialogueStage.None;
            _choiceActive = false;
            _choiceOption1 = null;
            _choiceOption2 = null;
            _clientX = ClientStartX;
            _clientZ = ClientStartZ;
            _clientYaw = -1.45;
            _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
            _setClientVisible?.Invoke(true);
            PlaySoundFor("door", 1.5);
            StartLoopingSound("Stomp");
            _clientWalking = true;
            _cashDisplayVisible = true;
            _cashAmount = 0;
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = false;
            _tiaLeavingDelayActive = false;
            _tiaLeavingDelayTimer = 0;
            _setTiaHoldingCupVisible?.Invoke(false);
            _stage = StoryStage.Day1Client1;
        }

        private void UpdateClientCountdown(double deltaTime)
        {
            if (!_clientCountdownActive || _clientWalking || _clientGreetingShown)
                return;

            _clientCountdownTimer -= deltaTime;

            if (_clientCountdownTimer <= 0)
            {
                StartClientArrival();
            }
        }

        private void UpdateReturnToCounterObjective()
        {
            if (_returnToCounterCompleted)
                return;

            if (FindObjective("Вернись за прилавок") == null)
                return;

            if (!IsPlayerAtSideBarEntrance())
                return;

            _returnToCounterCompleted = true;
            CompleteObjective("Вернись за прилавок", null);
            _setTiaBarPassageBlocked?.Invoke(true);
            _clientCountdownActive = true;
            _clientCountdownTimer = 5.0;
        }

        private bool IsPlayerAtSideBarEntrance()
        {
            if (_camera == null)
                return false;

            // Цель должна закрываться только тогда, когда игрок уже обошёл маленький боковой блок
            // и реально вошёл во внутреннюю зону за баром, а не просто подошёл к проходу снаружи.
            return _camera.X >= -96 &&
                   _camera.X <= 246 &&
                   _camera.Z >= 456 &&
                   _camera.Z <= 592;
        }

        private void ScheduleDelayedRagSound()
        {
            _delayedRagSoundActive = false;
            _delayedRagSoundTimer = 0;
            PlaySoundFor("rag", 1.0);
        }

        private void UpdateDelayedRagSound(double deltaTime)
        {
            if (!_delayedRagSoundActive)
                return;

            _delayedRagSoundTimer -= deltaTime;
            if (_delayedRagSoundTimer > 0)
                return;

            _delayedRagSoundActive = false;
            _delayedRagSoundTimer = 0;
            PlaySoundFor("rag", 1.0);
        }

        private void UpdateTiaLeavingDelay(double deltaTime)
        {
            if (!_tiaLeavingDelayActive || _clientLeaving || _tiaLeftForToday)
                return;

            _tiaLeavingDelayTimer -= deltaTime;
            if (_tiaLeavingDelayTimer <= 0)
            {
                _tiaLeavingDelayActive = false;
                _tiaLeavingDelayTimer = 0;
                StartTiaLeaving();
            }
        }

        private void StartMikeArrival()
        {
            _mikeArrivalStarted = true;
            _mikeAtCounter = false;
            _mikeWalking = true;
            _mikeX = MikeStartX;
            _mikeZ = MikeStartZ;
            _mikeYaw = -1.45;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            _setMikeVisible?.Invoke(true);
            StopDay1MikeMusic();
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            PlaySoundFor("door", 1.5);
            StartLoopingSound("Stomp");
        }

        private void UpdateMike(double deltaTime)
        {
            if (_mikeLeaving)
            {
                StartLoopingSound("Stomp");

                double dx = MikeStartX - _mikeX;
                double dz = MikeStartZ - _mikeZ;
                double dist = Math.Sqrt(dx * dx + dz * dz);

                UpdateDay1MikeMusicFadeDuringLeaving(dist);

                if (dist <= 1.0)
                {
                    FinishMikeLeaving();
                    return;
                }

                double step = MikeMoveSpeed * deltaTime;
                if (step >= dist)
                {
                    _mikeX = MikeStartX;
                    _mikeZ = MikeStartZ;
                }
                else
                {
                    _mikeX += dx / dist * step;
                    _mikeZ += dz / dist * step;
                }

                _mikeYaw = Math.Atan2(MikeStartX - _mikeX, MikeStartZ - _mikeZ);
                _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);

                if (step >= dist)
                    FinishMikeLeaving();

                return;
            }

            if (!_mikeWalking)
                return;

            double dxToTarget = MikeTargetX - _mikeX;
            double dzToTarget = MikeTargetZ - _mikeZ;
            double distToTarget = Math.Sqrt(dxToTarget * dxToTarget + dzToTarget * dzToTarget);

            if (distToTarget <= 1.0)
            {
                FinishMikeArrival();
                return;
            }

            double moveStep = MikeMoveSpeed * deltaTime;
            if (moveStep >= distToTarget)
            {
                _mikeX = MikeTargetX;
                _mikeZ = MikeTargetZ;
            }
            else
            {
                _mikeX += dxToTarget / distToTarget * moveStep;
                _mikeZ += dzToTarget / distToTarget * moveStep;
            }

            _mikeYaw = Math.Atan2(MikeTargetX - _mikeX, MikeTargetZ - _mikeZ);
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);

            if (moveStep >= distToTarget)
                FinishMikeArrival();
        }

        private void FinishMikeArrival()
        {
            _mikeWalking = false;
            _mikeAtCounter = true;
            _mikeX = MikeTargetX;
            _mikeZ = MikeTargetZ;
            _mikeYaw = 0;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            StopLoopingSound("Stomp");
        }

        private void StartMikeLeaving()
        {
            if (_mikeLeaving || _mikeLeftForToday)
                return;

            _mikeLeaving = true;
            _mikeWalking = false;
            _mikeAtCounter = false;
            _mikeLeftForToday = true;
            _mikeLeavingHeadTrackingDelayActive = _mikeHeadTrackingActive;
            _mikeLeavingHeadTrackingDelayTimer = MikeStopLookingAfterLeavingDelay;
            StartLoopingSound("Stomp");
        }

        private void FinishMikeLeaving()
        {
            _mikeLeaving = false;
            _mikeWalking = false;
            _mikeAtCounter = false;
            _mikeX = MikeStartX;
            _mikeZ = MikeStartZ;
            _mikeYaw = -1.45;
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
            _setMikeVisible?.Invoke(false);
            _setMikeHoldingOrderVisible?.Invoke(false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _setTiaBarPassageBlocked?.Invoke(false);
            StopLoopingSound("Stomp");
            StopDay1MikeMusic();
            PlaySoundFor("door", 1.5);

            if (!_day1EndShiftStarted)
            {
                _day1EndShiftStarted = true;
                ShowBottomText("Кажется я заработалась... Нужно в ближайшем времени поговорить с начальством по поводу отпуска.", 5.5);
                QueueBottomText("Ладно, пора домой. Надо выключить свет и телевизор, разгребу все завтра", 6.5, 5.0);
            }
        }

        private void UpdateTimePassStartDelay(double deltaTime)
        {
            if (!_timePassStartDelayActive)
                return;

            _timePassStartDelayTimer -= deltaTime;
            if (_timePassStartDelayTimer > 0)
                return;

            _timePassStartDelayActive = false;
            _timePassStartDelayTimer = 0;

            if (!_timePassTransitionActive)
                StartTimePassTransition();
        }

        private void StartTimePassTransition()
        {
            _timePassTransitionActive = true;
            _timePassTransitionTimer = 0;
        }

        private void UpdateTimePassTransition(double deltaTime)
        {
            if (!_timePassTransitionActive)
                return;

            _timePassTransitionTimer += deltaTime;

            double totalDuration =
                TimePassFadeToBlackDuration +
                TimePassTextFadeDuration +
                TimePassTextHoldDuration;

            if (_timePassTransitionTimer >= totalDuration)
            {
                _timePassTransitionActive = false;
                _timePassTransitionTimer = 0;
                _cashDisplayVisible = true;
                _cashAmount = 10500;
                _eveningWindowLight = true;
                _setEveningWindowLight?.Invoke(true);

                if (!_mikeArrivalStarted)
                    StartMikeArrival();
            }
        }

        private void UpdateClient(double deltaTime)
        {
            if (_clientWalking || _clientLeaving)
                StartLoopingSound("Stomp");

            if (_clientLeaving)
            {
                double dx = ClientStartX - _clientX;
                double dz = ClientStartZ - _clientZ;
                double distance = Math.Sqrt(dx * dx + dz * dz);

                if (distance <= 0.001)
                {
                    FinishTiaLeaving();
                }
                else
                {
                    double speedFactor = distance < 40 ? 0.55 + distance / 80.0 : 1.0;
                    double step = ClientMoveSpeed * speedFactor * deltaTime;

                    if (step >= distance)
                    {
                        _clientX = ClientStartX;
                        _clientZ = ClientStartZ;
                        _clientYaw = Math.Atan2(dx, dz);
                        _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
                        FinishTiaLeaving();
                    }
                    else
                    {
                        dx /= distance;
                        dz /= distance;
                        _clientX += dx * step;
                        _clientZ += dz * step;
                        _clientYaw = Math.Atan2(dx, dz);
                        _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
                    }
                }

                return;
            }

            if (_clientWalking)
            {
                double dx = ClientTargetX - _clientX;
                double dz = ClientTargetZ - _clientZ;
                double distance = Math.Sqrt(dx * dx + dz * dz);

                if (distance <= 0.001)
                {
                    FinishClientArrival();
                }
                else
                {
                    double speedFactor = distance < 40 ? 0.55 + distance / 80.0 : 1.0;
                    double step = ClientMoveSpeed * speedFactor * deltaTime;

                    if (step >= distance)
                    {
                        _clientX = ClientTargetX;
                        _clientZ = ClientTargetZ;
                        _clientYaw = Math.Atan2(dx, dz);
                        FinishClientArrival();
                    }
                    else
                    {
                        dx /= distance;
                        dz /= distance;
                        _clientX += dx * step;
                        _clientZ += dz * step;
                        _clientYaw = Math.Atan2(dx, dz);
                    }
                }
            }

            // Даже в покое продолжаем обновлять трансформацию,
            // чтобы работала анимация ожидания у стойки.
            if (_clientGreetingShown || _clientWalking)
                _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
        }

        private void StartTiaLeaving()
        {
            if (_clientLeaving || _tiaLeftForToday)
                return;

            _tiaLeavingDelayActive = false;
            _tiaLeavingDelayTimer = 0;
            _tiaLeftForToday = true;
            _clientLeaving = true;
            _clientWalking = false;
            _clientReadyToServe = false;
            _clientGreetingShown = true;
            _clientOrderFinished = true;
            _choiceActive = false;
            _choiceOption1 = null;
            _choiceOption2 = null;
            _setTiaOrderExchangeVisible?.Invoke(false, _tiaPaymentBillVisible && !_tiaPaymentCollected);
            _setTiaHoldingCupVisible?.Invoke(true);
            _recipeOverlayVisible = false;
            _setCashRecipeScreenVisible?.Invoke(false);
            _objectives.Clear();
            StartLoopingSound("Stomp");
            _timePassStartDelayActive = true;
            _timePassStartDelayTimer = 2.0;
        }

        private void StartTiaLeavingDelay()
        {
            if (_tiaLeftForToday || _clientLeaving)
                return;

            _tiaLeavingDelayActive = true;
            _tiaLeavingDelayTimer = TiaLeavingDelayAfterPayment;
        }

        private void FinishTiaLeaving()
        {
            _clientLeaving = false;
            _clientWalking = false;
            _tiaLeavingDelayActive = false;
            _tiaLeavingDelayTimer = 0;
            _clientReadyToServe = false;
            _clientGreetingShown = false;
            _clientX = ClientStartX;
            _clientZ = ClientStartZ;
            _clientYaw = -1.45;
            _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);
            _setTiaHoldingCupVisible?.Invoke(false);
            StopLoopingSound("Stomp");
            PlaySoundFor("door", 1.5);
            _setClientVisible?.Invoke(false);
        }

        private void FinishClientArrival()
        {
            _clientWalking = false;
            StopLoopingSound("Stomp");
            _clientX = ClientTargetX;
            _clientZ = ClientTargetZ;
            _clientYaw = 0;
            _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);

            if (_clientReadyToServe || _clientGreetingShown)
                return;

            _clientReadyToServe = true;
        }

        private void LookAtClient(double targetX, double targetZ, double targetY)
        {
            double dx = targetX - _camera.X;
            double dz = targetZ - _camera.Z;
            _camera.Yaw = Math.Atan2(dx, dz);

            double horizontal = Math.Sqrt(dx * dx + dz * dz);
            if (horizontal < 1.0)
                horizontal = 1.0;

            _camera.Pitch = Math.Atan2(targetY - _camera.Y, horizontal);
            if (_camera.Pitch < -0.85)
                _camera.Pitch = -0.85;
            if (_camera.Pitch > 0.85)
                _camera.Pitch = 0.85;
        }

        private void UpdateLeavingCameraFollow(double deltaTime)
        {
            if (_clientLeaving)
                SmoothLookAtTarget(_clientX, _clientZ, 18.0, deltaTime);
            else if (_mikeLeaving)
                SmoothLookAtTarget(_mikeX, _mikeZ, 18.0, deltaTime);
        }

        private void SmoothLookAtTarget(double targetX, double targetZ, double targetY, double deltaTime)
        {
            double dx = targetX - _camera.X;
            double dz = targetZ - _camera.Z;
            double targetYaw = Math.Atan2(dx, dz);

            double horizontal = Math.Sqrt(dx * dx + dz * dz);
            if (horizontal < 1.0)
                horizontal = 1.0;

            double targetPitch = Math.Atan2(targetY - _camera.Y, horizontal);
            if (targetPitch < -0.85)
                targetPitch = -0.85;
            if (targetPitch > 0.85)
                targetPitch = 0.85;

            double follow = 1.0 - Math.Exp(-deltaTime * 4.0);
            double yawDelta = NormalizeAngle(targetYaw - _camera.Yaw);
            _camera.Yaw += yawDelta * follow;
            _camera.Pitch += (targetPitch - _camera.Pitch) * follow;
        }

        private double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
                angle -= Math.PI * 2.0;
            while (angle < -Math.PI)
                angle += Math.PI * 2.0;
            return angle;
        }

        private void StartTiaDialogue()
        {
            LookAtClient(_clientX, _clientZ, 18.0);
            if (!_clientReadyToServe || _clientGreetingShown)
                return;

            _clientReadyToServe = false;
            _clientGreetingShown = true;
            _clientDialogueStage = ClientDialogueStage.Greeting;
            ShowBottomText("Тиа: Добрый день!", 4.0);
        }

        private void StartMikeDialogue()
        {
            if (!_mikeAtCounter || _mikeWalking || _mikeDialogueActive || _mikeOrderTaken)
                return;

            LookAtClient(_mikeX, _mikeZ, 18.0);
            PlaySound("generalInteraction");
            _mikeDialogueActive = true;
            _mikeDialogueIndex = 0;
            ShowNextMikeDialogueLine();
        }

        private void ShowNextMikeDialogueLine()
        {
            string line = GetMikeDialogueLine(_mikeDialogueIndex);
            if (line == null)
            {
                _mikeDialogueActive = false;
                _mikeOrderTaken = true;
                _mikeDialogueIndex = 0;
                StartMikeEspressoQuest();
                return;
            }

            _mikeDialogueIndex++;
            ShowBottomText(line, GetMikeDialogueDuration(line));
        }

        private string GetMikeDialogueLine(int index)
        {
            switch (index)
            {
                case 0:
                    return "Здравствуйте!";
                case 1:
                    return "Майк: ...";
                case 2:
                    return "Какой кофе вы бы хотели попробовать?";
                case 3:
                    return "Майк: ...";
                case 4:
                    return "Извините, я могу вам чем-то помочь?";
                case 5:
                    return "Майк: ...";
                case 6:
                    return "Майк: Одна работаете?";
                case 7:
                    return "Д-да... А что?";
                case 8:
                    return "Майк: Мило. Я буду двойной эспрессо со льдом.";
                case 9:
                    return "Хорошо... К оплате будет 250р";
            }

            return null;
        }

        private double GetMikeDialogueDuration(string line)
        {
            if (line == "Майк: ...")
                return 2.0;

            if (line.StartsWith("Майк:", StringComparison.OrdinalIgnoreCase))
                return 4.0;

            return 3.2;
        }

        private void StartRaspberryLatteQuest()
        {
            if (_raspberryLatteQuestStarted)
                return;

            _raspberryLatteQuestStarted = true;
            _recipeOpenedForLatte = false;
            _mikeEspressoQuestStarted = false;
            _recipeOpenedForMikeEspresso = false;
            _takeawayCupTaken = false;
            _bigGlassTaken = false;
            _hasTakeawayCup = false;
            _hasBigGlassCup = false;
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = false;
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            ResetLatteCupState();

            _objectives.Clear();
            _objectives.Add(new GameObjective("приготовить малиновый латте"));
            _objectives.Add(new GameObjective("   открыть рецепт"));

            ShowBottomText("Нужно возобновить в памяти рецепт", 3.6);
        }

        private void StartMikeEspressoQuest()
        {
            if (_mikeEspressoQuestStarted)
                return;

            _mikeEspressoQuestStarted = true;
            _recipeOpenedForMikeEspresso = false;
            _bigGlassTaken = false;
            _hasBigGlassCup = false;
            _coffeePortionsAdded = 0;
            _milkPortionsAdded = 0;
            _raspberrySyrupPortionsAdded = 0;
            _coffeeBrewingInProgress = false;
            _coffeeBrewingTimer = 0;
            _iceAddedToBigGlass = false;
            _iceAddingInProgress = false;
            _iceAddingTimer = 0;
            _mikeOrderReadyToServe = false;
            _mikeOrderServed = false;
            _mikePaymentBillVisible = false;
            _mikeServedGlassVisibleUntilThanksEnds = false;
            _setMikeOrderExchangeVisible?.Invoke(false, false);
            _setMikeWideEyesVisible?.Invoke(false);
            _setMikeHeadTrackingActive?.Invoke(false);
            _mikeWideEyesActive = false;
            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeOrderHandoffActive = false;
            _mikeHeadTrackingActive = false;
            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = 0;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikePaymentCollected = false;
            _setMikeSmileProgress?.Invoke(0);
            _setMikeHoldingOrderVisible?.Invoke(false);
            StopDay1MikeMusic();
            _setIceMakerLidAnimation?.Invoke(false, 0);
            _setCoffeeMachineCupState?.Invoke(false, 0);
            _setBigGlassShelfCupVisible?.Invoke(true);
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _clientOrderFinished = true;
            _recipeOverlayVisible = false;
            _fridgeOpen = false;
            _fridgeAnimating = false;
            _fridgeTargetOpen = false;
            _fridgeAnimationTimer = 0;
            _setFridgeOpenAnimation?.Invoke(false, 0.0);
            _setCashRecipeScreenVisible?.Invoke(true);

            _objectives.Clear();
            _objectives.Add(new GameObjective("Приготовить двойной эспрессо"));
            _objectives.Add(new GameObjective("   Посмотреть рецепт"));
        }

        private void InteractCashRecipe()
        {
            _recipeOverlayVisible = true;
            PlaySound("generalInteraction");

            GameObjective mikeRecipeObjective = FindObjective("Посмотреть рецепт");
            if (_mikeEspressoQuestStarted && mikeRecipeObjective != null && !mikeRecipeObjective.IsCompleted)
            {
                mikeRecipeObjective.IsCompleted = true;
                _recipeOpenedForMikeEspresso = true;

                if (FindObjective("Взять большой стакан") == null)
                    _objectives.Add(new GameObjective("   Взять большой стакан"));
                return;
            }

            GameObjective recipeObjective = FindObjective("открыть рецепт");
            if (_raspberryLatteQuestStarted && recipeObjective != null && !recipeObjective.IsCompleted)
            {
                recipeObjective.IsCompleted = true;
                _recipeOpenedForLatte = true;

                if (FindObjective("взять стакан одноразовый стакан") == null)
                    _objectives.Add(new GameObjective("   взять стакан одноразовый стакан"));
            }
        }

        private void InteractCoffeeMachine()
        {
            GameObjective refillObjective = FindObjective("заправить кофемашину");
            if (refillObjective != null && !refillObjective.IsCompleted)
            {
                if (_coffeeMachineRefillInProgress)
                {
                    ShowBottomText("Кофемашина уже заправляется.", 1.4);
                    return;
                }

                if (!_coffeeBeansTaken)
                {
                    ShowBottomText("Сначала возьми кофейные зёрна у мешков.", 1.8);
                    return;
                }

                _coffeeMachineRefillInProgress = true;
                _coffeeMachineRefillTimer = CoffeeMachineRefillDuration;
                _setCoffeeMachineRefillAnimation?.Invoke(true, 0);
                StartLoopingSound("CoffeeIsPouringOut");
                return;
            }

            if (_mikeEspressoQuestStarted && _bigGlassTaken)
            {
                if (!_hasBigGlassCup)
                {
                    ShowBottomText("Сначала возьми большой стакан.", 1.6);
                    return;
                }

                if (_coffeeBrewingInProgress)
                {
                    ShowBottomText("Кофе уже варится.", 1.2);
                    return;
                }

                if (_coffeePortionsAdded >= 2)
                {
                    ShowBottomText("Двойной эспрессо уже сварен.", 1.4);
                    return;
                }

                _setCoffeeMachinePaperCupMode?.Invoke(false);
                _coffeeBrewingInProgress = true;
                _coffeeBrewingTimer = CoffeeBrewDuration;
                StartLoopingSound("coffeeMachine");
                return;
            }

            if (!_raspberryLatteQuestStarted || !_takeawayCupTaken)
            {
                ShowBottomText("Сейчас кофемашина не нужна.", 1.4);
                return;
            }

            if (!_hasTakeawayCup)
            {
                ShowBottomText("Сначала возьми одноразовый стакан.", 1.6);
                return;
            }

            if (_coffeeBrewingInProgress)
            {
                ShowBottomText("Кофе уже варится.", 1.2);
                return;
            }

            if (_coffeePortionsAdded >= 1)
            {
                ShowBottomText("Порция кофе уже добавлена.", 1.4);
                return;
            }

            _setCoffeeMachinePaperCupMode?.Invoke(true);
            _coffeeBrewingInProgress = true;
            _coffeeBrewingTimer = CoffeeBrewDuration;
            StartLoopingSound("coffeeMachine");
        }

        private void InteractIceMaker()
        {
            if (!_mikeEspressoQuestStarted || !_bigGlassTaken || !_hasBigGlassCup)
            {
                ShowBottomText("Сейчас лед не нужен.", 1.4);
                return;
            }

            if (_coffeePortionsAdded < 2)
            {
                ShowBottomText("Сначала свари двойной эспрессо.", 1.6);
                return;
            }

            if (_iceAddingInProgress)
            {
                ShowBottomText("Лед уже добавляется.", 1.2);
                return;
            }

            if (_iceAddedToBigGlass)
            {
                ShowBottomText("Лед уже добавлен.", 1.4);
                return;
            }

            _iceAddingInProgress = true;
            _iceAddingTimer = IceAddingDuration;
            _setIceMakerLidAnimation?.Invoke(true, 0);
            PlaySound("generalInteraction");
        }

        private void InteractFridgeMilk()
        {
            if (_mikeEspressoQuestStarted || !_raspberryLatteQuestStarted)
            {
                ShowBottomText("Сейчас холодильник не нужен.", 1.4);
                return;
            }

            if (!_takeawayCupTaken || _coffeePortionsAdded < 1)
            {
                ShowBottomText("Сейчас молоко не нужно.", 1.4);
                return;
            }

            if (_fridgeAnimating)
                return;

            if (!_fridgeOpen)
            {
                StartFridgeAnimation(true);
                PlaySound("generalInteraction");
                return;
            }

            if (_milkPortionsAdded < 3)
            {
                _milkPortionsAdded++;
                PlaySound("milk");

                if (_milkPortionsAdded >= 3)
                {
                    GameObjective milkObjective = FindObjective("добавить молоко");
                    if (milkObjective != null && !milkObjective.IsCompleted)
                        milkObjective.IsCompleted = true;

                    ShowBottomText("Молоко добавлено: 3/3. Можно закрыть холодильник.", 1.6);
                }
                else
                {
                    ShowBottomText("Молоко добавлено: " + _milkPortionsAdded + "/3", 1.6);
                }

                return;
            }

            StartFridgeAnimation(false);
            PlaySound("generalInteraction");
        }

        private void StartFridgeAnimation(bool open)
        {
            _fridgeAnimating = true;
            _fridgeTargetOpen = open;
            _fridgeAnimationTimer = 0;
        }

        private void UpdateFridgeAnimation(double deltaTime)
        {
            if (!_fridgeAnimating)
                return;

            _fridgeAnimationTimer += deltaTime;
            double progress = _fridgeAnimationTimer / FridgeDoorAnimationDuration;
            if (progress < 0)
                progress = 0;
            if (progress > 1)
                progress = 1;

            double visualProgress = _fridgeTargetOpen ? progress : 1.0 - progress;
            _setFridgeOpenAnimation?.Invoke(visualProgress > 0.01, visualProgress);

            if (_fridgeAnimationTimer < FridgeDoorAnimationDuration)
                return;

            _fridgeAnimating = false;
            _fridgeOpen = _fridgeTargetOpen;
            _fridgeAnimationTimer = 0;
            _setFridgeOpenAnimation?.Invoke(_fridgeOpen, _fridgeOpen ? 1.0 : 0.0);
        }

        private void InteractRaspberrySyrup()
        {
            if (!_raspberryLatteQuestStarted || !_takeawayCupTaken)
            {
                ShowBottomText("Сейчас этот сироп не нужен.", 1.4);
                return;
            }

            if (!_hasTakeawayCup)
            {
                ShowBottomText("Сначала возьми одноразовый стакан.", 1.6);
                return;
            }

            if (_milkPortionsAdded < 3)
            {
                ShowBottomText("Сначала добавь всё молоко по рецепту.", 1.8);
                return;
            }

            if (_fridgeOpen || _fridgeAnimating)
            {
                ShowBottomText("Сначала закрой холодильник.", 1.6);
                return;
            }

            if (_raspberrySyrupPortionsAdded >= 1)
            {
                ShowBottomText("Малиновый сироп уже добавлен.", 1.4);
                return;
            }

            _raspberrySyrupPortionsAdded = 1;
            _raspberryPumpAnimationTimer = RaspberryPumpAnimationDuration;
            PlaySound("syrup");
            EvaluateLatteQuestCompletion();
        }

        private void UpdateSinkWashAnimation(double deltaTime)
        {
            if (!_sinkWashInProgress)
            {
                StopLoopingSound("washingCups");
                return;
            }

            _sinkWashTimer += deltaTime;
            double progress = _sinkWashTimer / SinkWashDuration;
            if (progress > 1.0)
                progress = 1.0;

            _setSinkWashAnimation?.Invoke(true, progress);

            if (_sinkWashTimer < SinkWashDuration)
                return;

            _sinkWashInProgress = false;
            _sinkWashTimer = 0;
            StopLoopingSound("washingCups");
            _setSinkWashAnimation?.Invoke(false, 0);
            StopLoopingSound("washingCups");
            _cupsWashed = true;

            if (FindObjective("положить чашки на полку") == null)
                _objectives.Add(new GameObjective("положить чашки на полку"));

            CompleteObjective("помыть чашки", null);
        }

        private void UpdateIceAddingAnimation(double deltaTime)
        {
            if (!_iceAddingInProgress)
                return;

            _iceAddingTimer -= deltaTime;
            double progress = 1.0 - _iceAddingTimer / IceAddingDuration;
            if (progress < 0)
                progress = 0;
            if (progress > 1)
                progress = 1;

            _setIceMakerLidAnimation?.Invoke(true, progress);

            if (_iceAddingTimer > 0)
                return;

            _iceAddingInProgress = false;
            _iceAddingTimer = 0;
            _iceAddedToBigGlass = true;
            _mikeOrderReadyToServe = true;
            if (FindObjective("Отдать заказ") == null)
                _objectives.Add(new GameObjective("   Отдать заказ"));
            _setIceMakerLidAnimation?.Invoke(false, 0);
        }

        private void UpdateMikeWideEyesDelay(double deltaTime)
        {
            if (!_mikeWideEyesDelayActive)
                return;

            _mikeWideEyesDelayTimer -= deltaTime;
            if (_mikeWideEyesDelayTimer > 0)
                return;

            _mikeWideEyesDelayActive = false;
            _mikeWideEyesDelayTimer = 0;
            _mikeWideEyesActive = true;
            _setMikeWideEyesVisible?.Invoke(true);
        }

        private void UpdateDay1MikeMusicFadeDuringLeaving(double distanceToDoor)
        {
            if (!_mikeDay1MikeMusicPlaying)
                return;

            double fadeDistance = MikeMoveSpeed * MikeDay1MikeFadeDuration;
            double volume = MikeDay1MikeVolume;

            if (fadeDistance > 0 && distanceToDoor < fadeDistance)
            {
                volume = MikeDay1MikeVolume * distanceToDoor / fadeDistance;
                if (volume < 0)
                    volume = 0;
                if (volume > MikeDay1MikeVolume)
                    volume = MikeDay1MikeVolume;
            }

            SetLoopingSoundVolume("Day1Mike", volume);
        }

        private void UpdateMikeHeadTracking(double deltaTime)
        {
            if (!_mikeHeadTrackingActive || !_mikeArrivalStarted || _mikeLeftForToday || _mikeWalking || _mikeLeaving)
                return;

            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
        }

        private void UpdateMikeLeavingHeadTrackingDelay(double deltaTime)
        {
            if (!_mikeLeavingHeadTrackingDelayActive)
                return;

            _mikeLeavingHeadTrackingDelayTimer -= deltaTime;
            if (_mikeLeavingHeadTrackingDelayTimer > 0)
                return;

            _mikeLeavingHeadTrackingDelayActive = false;
            _mikeLeavingHeadTrackingDelayTimer = 0;
            _mikeHeadTrackingActive = false;
            _setMikeHeadTrackingActive?.Invoke(false);
            _setMikeTransform?.Invoke(_mikeX, _mikeZ, _mikeYaw);
        }

        private void UpdateMikeFarewellSmile(double deltaTime)
        {
            if (!_mikeFarewellSmileActive)
                return;

            _mikeFarewellSmileTimer += deltaTime;
            double progress = _mikeFarewellSmileTimer / MikeFarewellSmileStretchDuration;
            if (progress > 1.0)
                progress = 1.0;

            _setMikeSmileProgress?.Invoke(progress);

            if (_mikeFarewellSmileTimer < MikeFarewellSmileStretchDuration)
                return;

            _mikeFarewellSmileActive = false;
            _mikeFarewellSmileTimer = MikeFarewellSmileStretchDuration;
            _setMikeSmileProgress?.Invoke(1.0);

            _mikePaymentBillVisible = true;
            _mikePaymentCollected = false;
            _setMikeOrderExchangeVisible?.Invoke(false, true);
            _setMikeHoldingOrderVisible?.Invoke(true);
            CompleteMikeEspressoAfterPaymentAppears();

            _mikeAutoLeaveAfterSmileActive = true;
            _mikeAutoLeaveAfterSmileTimer = MikeAutoLeaveDelayAfterSmile;
        }

        private void UpdateMikeAutoLeaveAfterSmile(double deltaTime)
        {
            if (!_mikeAutoLeaveAfterSmileActive || _mikeLeaving || _mikeLeftForToday)
                return;

            _mikeAutoLeaveAfterSmileTimer -= deltaTime;
            if (_mikeAutoLeaveAfterSmileTimer > 0)
                return;

            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            StartMikeLeaving();
        }

        private void CompleteMikeEspressoAfterPaymentAppears()
        {
            GameObjective mainObjective = FindObjective("Приготовить двойной эспрессо");
            if (mainObjective != null && !mainObjective.IsCompleted)
                mainObjective.IsCompleted = true;

            if (FindObjective("Забрать деньги") == null)
                _objectives.Add(new GameObjective("   Забрать деньги"));
        }

        private void UpdateCoffeeMachineRefill(double deltaTime)
        {
            if (!_coffeeMachineRefillInProgress)
                return;

            _coffeeMachineRefillTimer -= deltaTime;
            double progress = 1.0 - _coffeeMachineRefillTimer / CoffeeMachineRefillDuration;
            if (progress < 0)
                progress = 0;
            if (progress > 1)
                progress = 1;

            _setCoffeeMachineRefillAnimation?.Invoke(true, progress);

            if (_coffeeMachineRefillTimer > 0)
                return;

            _coffeeMachineRefillInProgress = false;
            _coffeeMachineRefillTimer = 0;
            _coffeeBeansTaken = true;
            _setCoffeeMachineRefillAnimation?.Invoke(false, 0);
            StopLoopingSound("CoffeeIsPouringOut");
            _setCoffeeBeanFrontBagVisible?.Invoke(false);

            if (FindObjective("включить телевизор") == null)
                _objectives.Add(new GameObjective("включить телевизор"));

            CompleteObjective("заправить кофемашину", null);
        }

        private void UpdateCoffeeBrewing(double deltaTime)
        {
            if (!_coffeeBrewingInProgress)
                return;

            _coffeeBrewingTimer -= deltaTime;
            if (_coffeeBrewingTimer > 0)
                return;

            _coffeeBrewingInProgress = false;
            _coffeeBrewingTimer = 0;
            StopLoopingSound("coffeeMachine");

            if (_mikeEspressoQuestStarted && _hasBigGlassCup)
            {
                if (_coffeePortionsAdded < 2)
                    _coffeePortionsAdded++;

                if (_coffeePortionsAdded >= 2)
                {
                    if (!_mikeWideEyesActive)
                    {
                        _mikeWideEyesDelayActive = false;
                        _mikeWideEyesDelayTimer = 0;
                        _mikeWideEyesActive = true;
                        _setMikeWideEyesVisible?.Invoke(true);
                    }

                    if (!_mikeHeadTrackingActive)
                    {
                        _mikeHeadTrackingActive = true;
                        _setMikeHeadTrackingActive?.Invoke(true);
                    }
                }

                return;
            }

            _coffeePortionsAdded = 1;
            EvaluateLatteQuestCompletion();
        }

        private void UpdateRaspberryPumpAnimation(double deltaTime)
        {
            if (_raspberryPumpAnimationTimer <= 0)
                return;

            _raspberryPumpAnimationTimer -= deltaTime;
            if (_raspberryPumpAnimationTimer < 0)
                _raspberryPumpAnimationTimer = 0;
        }

        private void InteractGiveOrderToTia()
        {
            GameObjective giveOrderObjective = FindObjective("отдать заказ");
            if (giveOrderObjective == null)
            {
                ShowBottomText("Сейчас заказ отдавать не нужно.", 1.4);
                return;
            }

            if (!_latteReadyToServe)
            {
                ShowBottomText("Сначала приготовь малиновый латте.", 1.6);
                return;
            }

            LookAtClient(_clientX, _clientZ, 18.0);
            giveOrderObjective.IsCompleted = true;
            PlaySound("generalInteraction");
            _latteReadyToServe = false;
            _hasTakeawayCup = false;
            _tiaPaymentBillVisible = false;
            _tiaPaymentCollected = false;
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            _setTiaHoldingCupVisible?.Invoke(false);
            _tiaThanksExchangePending = true;
            _tiaServedCupVisibleUntilThanksEnds = false;

            string tiaThanksText = _tiaRudeChoice ? "Тиа: Благодарю." : "Тиа: Огромное спасибо!";
            _setTiaOrderExchangeVisible?.Invoke(true, false);
            _tiaThanksExchangePending = false;
            _tiaServedCupVisibleUntilThanksEnds = true;
            ShowBottomText(tiaThanksText, 2.4);
        }

        private void InteractGiveOrderToMike()
        {
            if (!_mikeOrderReadyToServe || _mikeOrderServed)
            {
                ShowBottomText("Сейчас заказ Майку отдавать не нужно.", 1.4);
                return;
            }

            if (!_iceAddedToBigGlass || _coffeePortionsAdded < 2)
            {
                ShowBottomText("Сначала доделай двойной эспрессо со льдом.", 1.6);
                return;
            }

            LookAtClient(_mikeX, _mikeZ, 18.0);
            PlaySound("generalInteraction");
            GameObjective giveOrderObjective = FindObjective("Отдать заказ");
            if (giveOrderObjective != null && !giveOrderObjective.IsCompleted)
                giveOrderObjective.IsCompleted = true;
            _mikeOrderServed = true;
            _mikeOrderReadyToServe = false;
            _hasBigGlassCup = false;
            _mikePaymentCollected = false;
            _mikeAutoLeaveAfterSmileActive = false;
            _mikeAutoLeaveAfterSmileTimer = 0;
            _mikeOrderHandoffActive = true;
            _setMikeOrderExchangeVisible?.Invoke(true, false);
            _mikeServedGlassVisibleUntilThanksEnds = false;
            _mikePaymentBillVisible = false;

            _queuedBottomText = "Майк: До скорой встречи";
            _queuedBottomTextDelay = MikeGiveOrderPause;
            _queuedBottomTextDuration = MikeFarewellDuration;
            _mikeServedGlassVisibleUntilThanksEnds = true;
        }

        private void InteractTakeTiaPayment()
        {
            if (!_tiaPaymentBillVisible || _tiaPaymentCollected)
            {
                ShowBottomText("Сейчас на стойке нечего забирать.", 1.4);
                return;
            }

            _tiaPaymentCollected = true;
            PlaySound("generalInteraction");
            _tiaPaymentBillVisible = false;
            _cashAmount = 300;
            _recipeOverlayVisible = false;
            _setCashRecipeScreenVisible?.Invoke(false);
            _setTiaOrderExchangeVisible?.Invoke(false, false);
            _setTiaHoldingCupVisible?.Invoke(true);
            StartTiaLeaving();
        }

        private void InteractTakeMikePayment()
        {
            if (!_mikePaymentBillVisible || !_mikeOrderServed || _mikePaymentCollected)
            {
                ShowBottomText("Сейчас на стойке нечего забирать.", 1.4);
                return;
            }

            PlaySound("generalInteraction");
            _mikePaymentCollected = true;
            _mikePaymentBillVisible = false;
            _cashAmount += 250;
            _setMikeOrderExchangeVisible?.Invoke(false, false);

            GameObjective takeMoneyObjective = FindObjective("Забрать деньги");
            if (takeMoneyObjective != null && !takeMoneyObjective.IsCompleted)
                takeMoneyObjective.IsCompleted = true;
        }

        private void EvaluateLatteQuestCompletion()
        {
            if (_coffeePortionsAdded < 1 || _milkPortionsAdded < 3 || _raspberrySyrupPortionsAdded < 1)
                return;

            _latteReadyToServe = true;

            GameObjective mainObjective = FindObjective("приготовить малиновый латте");
            if (mainObjective != null && !mainObjective.IsCompleted)
            {
                mainObjective.IsCompleted = true;

                if (FindObjective("отдать заказ") == null)
                    _objectives.Add(new GameObjective("отдать заказ"));

                ShowBottomText("Малиновый латте готов.", 2.0);
            }
        }

        private void CompleteLatteSubObjective(string objectivePart)
        {
            GameObjective objective = FindObjective(objectivePart);
            if (objective != null)
                objective.IsCompleted = true;

            // Сам латте пока не готов: стакан только подготовлен для следующих ингредиентов.
        }

        private void HandleTimedTextFinished()
        {
            if (_day1VacationTextShowing)
            {
                _day1VacationTextShowing = false;
                if (FindObjective("выключить телевизор") == null)
                    _objectives.Add(new GameObjective("выключить телевизор"));
                return;
            }

            if (_mikeDialogueActive)
            {
                ShowNextMikeDialogueLine();
                return;
            }

            switch (_clientDialogueStage)
            {
                case ClientDialogueStage.Greeting:
                    ShowClientChoice();
                    break;

                case ClientDialogueStage.PlayerAnswerPolite:
                    _clientDialogueStage = ClientDialogueStage.ClientOrderPolite;
                    ShowBottomText("Тиа: Я бы хотела малиновый латте с собой", 5.0);
                    break;

                case ClientDialogueStage.ClientOrderPolite:
                    _clientDialogueStage = ClientDialogueStage.PaymentPolite;
                    ShowBottomText("Заказ принят, к оплате будет 300 рублей", 5.0);
                    break;

                case ClientDialogueStage.PaymentPolite:
                    _clientDialogueStage = ClientDialogueStage.Finished;
                    _clientOrderFinished = true;
                    _setCashRecipeScreenVisible?.Invoke(true);
                    StartRaspberryLatteQuest();
                    break;

                case ClientDialogueStage.PlayerAnswerRude:
                    _clientDialogueStage = ClientDialogueStage.ClientOrderRude;
                    ShowBottomText("Тиа: ... Малиновый латте пожалуй.", 5.0);
                    break;

                case ClientDialogueStage.ClientOrderRude:
                    _clientDialogueStage = ClientDialogueStage.PaymentRude;
                    ShowBottomText("300р", 5.0);
                    break;

                case ClientDialogueStage.PaymentRude:
                    _clientDialogueStage = ClientDialogueStage.Finished;
                    _clientOrderFinished = true;
                    _setCashRecipeScreenVisible?.Invoke(true);
                    StartRaspberryLatteQuest();
                    break;
            }
        }

        private void ShowClientChoice()
        {
            _clientDialogueStage = ClientDialogueStage.WaitingChoice;
            _choiceActive = true;
            _choiceOption1 = "Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?";
            _choiceOption2 = "Ага...Чего желаете?";
        }

        private GameObjective FindObjective(string objectivePart)
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i].Text.IndexOf(objectivePart, StringComparison.OrdinalIgnoreCase) >= 0)
                    return _objectives[i];
            }

            return null;
        }

        private bool AllObjectivesCompleted()
        {
            if (_objectives.Count == 0)
                return true;

            for (int i = 0; i < _objectives.Count; i++)
            {
                if (!_objectives[i].IsCompleted)
                    return false;
            }

            return true;
        }

        private void UpdatePrompt()
        {
            if (_timePassTransitionActive)
            {
                PromptText = null;
                return;
            }

            if (_recipeOverlayVisible)
            {
                PromptText = null;
                return;
            }

            InteractionZone zone = GetNearestZone();
            if (zone == null)
            {
                PromptText = null;
                return;
            }

            if (zone.Id == "client_tia" &&
                !(_mikeAtCounter && _mikeOrderReadyToServe && !_mikeOrderServed) &&
                FindObjective("отдать заказ") != null && !FindObjective("отдать заказ").IsCompleted)
            {
                PromptText = "E — отдать заказ";
                return;
            }

            if (zone.Id == "tia_payment")
            {
                PromptText = "E — забрать деньги";
                return;
            }

            if (zone.Id == "big_glass_shelf" && _mikeEspressoQuestStarted && _recipeOpenedForMikeEspresso && !_bigGlassTaken && FindObjective("Взять большой стакан") != null)
            {
                PromptText = "E — Взять большой стакан";
                return;
            }

            if ((zone.Id == "cup_shelf" || zone.Id == "takeaway_cup_shelf") && _raspberryLatteQuestStarted && _recipeOpenedForLatte && !_takeawayCupTaken && FindObjective("взять стакан одноразовый стакан") != null)
            {
                PromptText = "E — взять стакан одноразовый стакан";
                return;
            }

            if (zone.Id == "coffee_beans")
            {
                GameObjective refillObjective = FindObjective("заправить кофемашину");
                if (refillObjective != null && !refillObjective.IsCompleted)
                {
                    PromptText = "E — взять кофейные зёрна";
                    return;
                }
            }

            if (zone.Id == "tv")
            {
                GameObjective turnOffTv = FindObjective("выключить телевизор");
                if (turnOffTv != null && !turnOffTv.IsCompleted)
                {
                    PromptText = "E — Выключить телевизор";
                    return;
                }

                GameObjective turnOnTv = FindObjective("включить телевизор");
                if (turnOnTv != null && !turnOnTv.IsCompleted)
                {
                    PromptText = "E — включить телевизор";
                    return;
                }
            }

            if (zone.Id == "light_switch")
            {
                GameObjective turnOffLight = FindObjective("выключить свет");
                if (turnOffLight != null && !turnOffLight.IsCompleted)
                {
                    PromptText = "E — Выключить свет";
                    return;
                }

                GameObjective turnOnLight = FindObjective("включить свет");
                if (turnOnLight != null && !turnOnLight.IsCompleted)
                {
                    PromptText = "E — Включить свет";
                    return;
                }
            }

            if (zone.Id == "coffee_machine")
            {
                if (FindObjective("заправить кофемашину") != null && !FindObjective("заправить кофемашину").IsCompleted)
                {
                    PromptText = "E — заправить кофемашину";
                    return;
                }

                if (_mikeEspressoQuestStarted && _bigGlassTaken)
                    PromptText = "E — Сварить кофе";
                else
                    PromptText = "E — сварить кофе";
                return;
            }

            if (zone.Id == "ice_maker")
            {
                PromptText = "E — Добавить лед";
                return;
            }

            if (zone.Id == "fridge_milk")
            {
                if (_fridgeAnimating)
                {
                    PromptText = null;
                    return;
                }

                if (!_fridgeOpen)
                    PromptText = "E — Открыть холодильник";
                else if (_milkPortionsAdded < 3)
                    PromptText = "E — Добавить молоко";
                else
                    PromptText = "E — Закрыть холодильник";
                return;
            }

            if (zone.Id == "raspberry_syrup")
            {
                if (_fridgeOpen || _fridgeAnimating)
                {
                    PromptText = null;
                    return;
                }

                PromptText = "E — добавить малиновый сироп";
                return;
            }

            if (zone.Id == "client_mike")
            {
                if (_mikeOrderReadyToServe && !_mikeOrderServed)
                    PromptText = "E — Отдать заказ: Майк";
                else
                    PromptText = "E — Принять заказ: Майк";
                return;
            }

            PromptText = zone.Prompt;
        }

        private bool ShouldShowZonePrompt(InteractionZone zone)
        {
            if (zone == null)
                return false;

            if (zone.Id == "dirty_cups_table1")
                return FindObjective("собрать грязные чашки со стола") != null && !_table1CupsCollected;

            if (zone.Id == "dirty_cups_table2")
                return FindObjective("собрать грязные чашки со стола") != null && !_table2CupsCollected;

            if (zone.Id == "sink")
                return FindObjective("помыть чашки") != null && AllDirtyCupsCollected() && !_cupsWashed && !_sinkWashInProgress;

            if (zone.Id == "cup_shelf")
                return FindObjective("положить чашки на полку") != null && AllDirtyCupsCollected() && _cupsWashed && !_cupsReturnedToShelf;

            if (zone.Id == "big_glass_shelf")
                return _mikeEspressoQuestStarted && _recipeOpenedForMikeEspresso && !_bigGlassTaken && FindObjective("Взять большой стакан") != null;

            if (zone.Id == "takeaway_cup_shelf")
                return _raspberryLatteQuestStarted && _recipeOpenedForLatte && !_takeawayCupTaken && FindObjective("взять стакан одноразовый стакан") != null;

            if (zone.Id == "table1_wipe")
                return _cupsReturnedToShelf && FindObjective("протереть столы") != null && !_table1Wiped;

            if (zone.Id == "table2_wipe")
                return _cupsReturnedToShelf && FindObjective("протереть столы") != null && !_table2Wiped;

            if (zone.Id == "tv")
            {
                GameObjective turnOffTv = FindObjective("выключить телевизор");
                if (turnOffTv != null && !turnOffTv.IsCompleted)
                    return _tvTurnedOn;

                return FindObjective("включить телевизор") != null && !_tvTurnedOn;
            }

            if (zone.Id == "coffee_beans")
            {
                GameObjective refillObjective = FindObjective("заправить кофемашину");
                return refillObjective != null && !refillObjective.IsCompleted && !_coffeeBeansTaken && !_coffeeMachineRefillInProgress;
            }

            if (zone.Id == "client_tia")
            {
                if (_clientLeaving)
                    return false;

                // Когда готов двойной эспрессо Майку, зона Тии не должна показываться
                // и не должна перехватывать взаимодействие у Майка.
                if (_mikeAtCounter && _mikeOrderReadyToServe && !_mikeOrderServed)
                    return false;

                if (FindObjective("отдать заказ") != null && !FindObjective("отдать заказ").IsCompleted)
                {
                    double dxGive = _camera.X - ClientTargetX;
                    double dzGive = _camera.Z - ClientTargetZ;
                    return dxGive * dxGive + dzGive * dzGive <= 125 * 125;
                }

                return _clientReadyToServe && !_clientGreetingShown;
            }

            if (zone.Id == "client_mike")
            {
                if (!_mikeAtCounter || _mikeWalking || _mikeDialogueActive)
                    return false;

                bool canTakeOrder = !_mikeOrderTaken;
                bool canGiveOrder = _mikeOrderReadyToServe && !_mikeOrderServed;
                if (!canTakeOrder && !canGiveOrder)
                    return false;

                double dxMike = _camera.X - MikeTargetX;
                double dzMike = _camera.Z - MikeTargetZ;
                return dxMike * dxMike + dzMike * dzMike <= 130 * 130;
            }

            if (zone.Id == "tia_payment")
                return _tiaPaymentBillVisible && !_tiaPaymentCollected;

            if (zone.Id == "mike_payment")
                return _mikePaymentBillVisible && _mikeOrderServed && !_mikePaymentCollected;

            if (zone.Id == "cash_recipe")
                return (_raspberryLatteQuestStarted && !_tiaLeftForToday) || _mikeEspressoQuestStarted;

            if (zone.Id == "coffee_machine")
            {
                GameObjective refillObjective = FindObjective("заправить кофемашину");
                if (refillObjective != null && !refillObjective.IsCompleted)
                    return !_coffeeMachineRefillInProgress;

                if (_mikeEspressoQuestStarted && _bigGlassTaken && _hasBigGlassCup && !_coffeeBrewingInProgress && _coffeePortionsAdded < 2)
                    return true;

                return _takeawayCupTaken && !_coffeeBrewingInProgress && _coffeePortionsAdded < 1;
            }

            if (zone.Id == "ice_maker")
                return _mikeEspressoQuestStarted && _bigGlassTaken && _hasBigGlassCup && !_coffeeBrewingInProgress && !_iceAddingInProgress && _coffeePortionsAdded >= 2 && !_iceAddedToBigGlass;

            if (zone.Id == "fridge_milk")
                return !_mikeEspressoQuestStarted && _raspberryLatteQuestStarted && _takeawayCupTaken && _coffeePortionsAdded >= 1 && (_milkPortionsAdded < 3 || _fridgeOpen || _fridgeAnimating);

            if (zone.Id == "raspberry_syrup")
                return !_mikeEspressoQuestStarted && _raspberryLatteQuestStarted && _takeawayCupTaken && _milkPortionsAdded >= 3 && !_fridgeOpen && !_fridgeAnimating && _raspberrySyrupPortionsAdded < 1;

            if (zone.Id == "light_switch")
            {
                GameObjective turnOnLight = FindObjective("включить свет");
                if (turnOnLight != null && !turnOnLight.IsCompleted)
                    return true;

                GameObjective turnOffLight = FindObjective("выключить свет");
                return turnOffLight != null && !turnOffLight.IsCompleted;
            }

            if (zone.Id == "counter")
                return false;

            return true;
        }

        private InteractionZone GetNearestZone()
        {
            InteractionZone nearest = null;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < _zones.Count; i++)
            {
                InteractionZone zone = _zones[i];

                if (!zone.IsAvailable(_camera))
                    continue;

                if (!ShouldShowZonePrompt(zone))
                    continue;

                double dx = _camera.X - zone.X;
                double dz = _camera.Z - zone.Z;
                double distance = dx * dx + dz * dz;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = zone;
                }
            }

            return nearest;
        }

        private void QueueBottomText(string text, double delaySeconds, double durationSeconds)
        {
            _queuedBottomText = text;
            _queuedBottomTextDelay = delaySeconds;
            _queuedBottomTextDuration = durationSeconds;
        }

        private void ClearQueuedBottomText()
        {
            _queuedBottomText = null;
            _queuedBottomTextDelay = 0;
            _queuedBottomTextDuration = 0;
        }

        private void UpdateDialogueSpeakerMouths(string text)
        {
            bool clientSpeaking = false;
            bool mikeSpeaking = false;

            if (!string.IsNullOrWhiteSpace(text))
            {
                string trimmed = text.TrimStart();
                if (trimmed.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Клиент:", StringComparison.OrdinalIgnoreCase))
                {
                    clientSpeaking = true;
                }
                else if (IsMikeMouthLine(trimmed))
                {
                    mikeSpeaking = true;
                }
            }

            _setClientSpeaking?.Invoke(clientSpeaking);
            _setMikeSpeaking?.Invoke(mikeSpeaking);
        }

        private void ShowBottomText(string text, double seconds)
        {
            BottomText = text;
            _messageTimer = seconds;
            _clientInteractionTextLockActive = IsClientDialogueText(text);
            UpdateDialogueSpeakerMouths(text);

            if (ShouldPlayDialogueSound(text))
                PlaySound("dialogue");
        }

        private bool IsMikeMouthLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.TrimStart();

            if (trimmed.Equals("Майк: ...", StringComparison.OrdinalIgnoreCase))
                return false;

            return trimmed.StartsWith("Майк:", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsClientDialogueText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (text.StartsWith("Майк:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (text.StartsWith("Клиент:", StringComparison.OrdinalIgnoreCase))
                return true;

            return
                text == "Здравствуйте!" ||
                text == "Какой кофе вы бы хотели попробовать?" ||
                text == "Извините, я могу вам чем-то помочь?" ||
                text == "Д-да... А что?" ||
                text == "Хорошо... К оплате будет 250р" ||
                text == "Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?" ||
                text == "Ага...Чего желаете?" ||
                text == "Заказ принят, к оплате будет 300 рублей" ||
                text == "300р";
        }

        private bool ShouldPlayDialogueSound(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.StartsWith("Тиа:", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.StartsWith("Клиент:", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.StartsWith("Майк:", StringComparison.OrdinalIgnoreCase))
                return true;

            return
                text == "Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?" ||
                text == "Ага...Чего желаете?" ||
                text == "Заказ принят, к оплате будет 300 рублей" ||
                text == "300р" ||
                text == "Здравствуйте!" ||
                text == "Какой кофе вы бы хотели попробовать?" ||
                text == "Извините, я могу вам чем-то помочь?" ||
                text == "Д-да... А что?" ||
                text == "Хорошо... К оплате будет 250р";
        }

        private void ShowCenterText(string text, double seconds)
        {
            CenterText = text;
            _messageTimer = seconds;
        }
    }
}
