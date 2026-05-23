using System;
using System.Collections.Generic;
using игра_для_проги.Model;

namespace игра_для_проги.Controller
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

            if (!RequireLook)
                return true;

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

            return dot >= 0.58;
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
            ClientOrderRude,
            PaymentRude,
            Finished
        }

        private readonly Camera3D _camera;
        private readonly Action<bool> _setLightSwitchVisual;
        private readonly Action<bool> _setDirtyCupsTable1Visible;
        private readonly Action<bool> _setDirtyCupsTable2Visible;
        private readonly Action<bool> _setCoffeeStainsTable1Visible;
        private readonly Action<bool> _setCoffeeStainsTable2Visible;
        private readonly Action<bool> _setReturnedShelfCupsVisible;
        private readonly Action<bool> _setTakeawayShelfCupVisible;
        private readonly Action<bool> _setTvScreenVisible;
        private readonly Action<bool> _setCashRecipeScreenVisible;
        private readonly Action<bool, double> _setCoffeeMachineCupState;
        private readonly Action<double> _setRaspberryPumpPressed;
        private readonly Action<bool> _setClientVisible;
        private readonly Action<double, double, double> _setClientTransform;
        private readonly Action<bool> _setCoffeeBeanFrontBagVisible;
        private readonly Action<bool, double> _setCoffeeMachineRefillAnimation;
        private readonly Action<bool, double> _setSinkWashAnimation;
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
        private bool _takeawayCupTaken;
        private bool _hasTakeawayCup;
        private int _coffeePortionsAdded;
        private int _milkPortionsAdded;
        private int _raspberrySyrupPortionsAdded;
        private bool _coffeeBrewingInProgress;
        private double _coffeeBrewingTimer;
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

        private StoryStage _stage;
        private double _messageTimer;
        private double _flickerTimer;
        private bool _screenFlash;

        private bool _clientWalking;
        private bool _clientGreetingShown;
        private bool _clientReadyToServe;
        private bool _clientOrderFinished;
        private bool _recipeOverlayVisible;
        private bool _clientCountdownActive;
        private bool _returnToCounterCompleted;
        private double _clientCountdownTimer;
        private double _clientX;
        private double _clientZ;
        private double _clientYaw;

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
        private const double CoffeeBrewDuration = 3.0;
        private const double CoffeeMachineRefillDuration = 4.0;
        private const double SinkWashDuration = 2.4;
        private const double RaspberryPumpAnimationDuration = 0.42;

        public int CurrentDay { get; private set; }
        public bool LightOn { get; private set; }
        public bool IsLightFlickering { get; private set; }
        public string BottomText { get; private set; }
        public string CenterText { get; private set; }
        public string PromptText { get; private set; }
        public int Reputation { get; private set; }
        public bool RecipeOverlayVisible { get { return _recipeOverlayVisible; } }
        public bool HasTakeawayCup { get { return _hasTakeawayCup; } }
        public bool HeldTakeawayCupVisible { get { return _hasTakeawayCup && !_coffeeBrewingInProgress; } }
        public int CoffeePortionsInCup { get { return _coffeePortionsAdded; } }
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
        public string ChoiceOption1 { get { return _choiceOption1; } }
        public string ChoiceOption2 { get { return _choiceOption2; } }
        public IReadOnlyList<GameObjective> Objectives { get { return _objectives; } }
        public bool ScreenFlash { get { return _screenFlash; } }

        public HorrorGameController(
            Camera3D camera,
            Action<bool> setLightSwitchVisual,
            Action<bool> setDirtyCupsTable1Visible,
            Action<bool> setDirtyCupsTable2Visible,
            Action<bool> setCoffeeStainsTable1Visible,
            Action<bool> setCoffeeStainsTable2Visible,
            Action<bool> setReturnedShelfCupsVisible,
            Action<bool> setTakeawayShelfCupVisible,
            Action<bool> setTvScreenVisible,
            Action<bool> setCashRecipeScreenVisible,
            Action<bool, double> setCoffeeMachineCupState,
            Action<double> setRaspberryPumpPressed,
            Action<bool> setClientVisible,
            Action<double, double, double> setClientTransform,
            Action<bool> setCoffeeBeanFrontBagVisible,
            Action<bool, double> setCoffeeMachineRefillAnimation,
            Action<bool, double> setSinkWashAnimation,
            Action<bool> setTiaBarPassageBlocked)
        {
            _camera = camera;
            _setLightSwitchVisual = setLightSwitchVisual;
            _setDirtyCupsTable1Visible = setDirtyCupsTable1Visible;
            _setDirtyCupsTable2Visible = setDirtyCupsTable2Visible;
            _setCoffeeStainsTable1Visible = setCoffeeStainsTable1Visible;
            _setCoffeeStainsTable2Visible = setCoffeeStainsTable2Visible;
            _setReturnedShelfCupsVisible = setReturnedShelfCupsVisible;
            _setTakeawayShelfCupVisible = setTakeawayShelfCupVisible;
            _setTvScreenVisible = setTvScreenVisible;
            _setCashRecipeScreenVisible = setCashRecipeScreenVisible;
            _setCoffeeMachineCupState = setCoffeeMachineCupState;
            _setRaspberryPumpPressed = setRaspberryPumpPressed;
            _setClientVisible = setClientVisible;
            _setClientTransform = setClientTransform;
            _setCoffeeBeanFrontBagVisible = setCoffeeBeanFrontBagVisible;
            _setCoffeeMachineRefillAnimation = setCoffeeMachineRefillAnimation;
            _setSinkWashAnimation = setSinkWashAnimation;
            _setTiaBarPassageBlocked = setTiaBarPassageBlocked;
            CreateZones();
            StartDay1();
        }

        public void Update(double deltaTime)
        {
            if (_messageTimer > 0)
            {
                _messageTimer -= deltaTime;
                if (_messageTimer <= 0)
                {
                    BottomText = null;
                    CenterText = null;
                    _screenFlash = false;
                    HandleTimedTextFinished();
                }
            }

            if (_queuedBottomText != null)
            {
                _queuedBottomTextDelay -= deltaTime;

                if (_queuedBottomTextDelay <= 0)
                {
                    ShowBottomText(_queuedBottomText, _queuedBottomTextDuration);
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
            UpdateCoffeeMachineRefill(deltaTime);
            UpdateSinkWashAnimation(deltaTime);
            UpdateCoffeeBrewing(deltaTime);
            UpdateRaspberryPumpAnimation(deltaTime);
            _setCoffeeMachineCupState?.Invoke(_coffeeBrewingInProgress, CoffeeBrewingProgress);
            _setRaspberryPumpPressed?.Invoke(IsRaspberryPumpAnimating ? RaspberryPumpAnimationProgress : 0);
            UpdateReturnToCounterObjective();
            UpdateClient(deltaTime);
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
                _clientDialogueStage = ClientDialogueStage.PlayerAnswerPolite;
                ShowBottomText("Добрый день! Рады вас видеть в нашем кафе, что бы вы хотели заказать?", 5.0);
                return;
            }

            if (option == 2)
            {
                Reputation -= 1;
                _clientDialogueStage = ClientDialogueStage.PlayerAnswerRude;
                ShowBottomText("Ага...Чего желаете?", 5.0);
                return;
            }
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

                case "table1_wipe":
                    InteractWipeTable1();
                    break;

                case "table2_wipe":
                    InteractWipeTable2();
                    break;

                case "client_tia":
                    if (FindObjective("отдать заказ") != null && !FindObjective("отдать заказ").IsCompleted)
                        InteractGiveOrderToTia();
                    else
                        StartTiaDialogue();
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

                case "fridge_milk":
                    InteractFridgeMilk();
                    break;

                case "raspberry_syrup":
                    InteractRaspberrySyrup();
                    break;

                case "cash_register":
                    CompleteObjective("посчитать кассу", "Касса посчитана. Не хватает одной монеты.");
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
            _zones.Add(new InteractionZone("light_switch", "E — включить/выключить свет", 260, 248, 55, true));

            // Грязные кружки нужно именно увидеть/навести взгляд на конкретный стол, поэтому requireLook = true.
            _zones.Add(new InteractionZone("dirty_cups_table1", "E — собрать кружки с первого стола", -150, 155, 72, true));
            _zones.Add(new InteractionZone("dirty_cups_table2", "E — собрать кружку со второго стола", 150, 155, 72, true));
            _zones.Add(new InteractionZone("sink", "E — помыть собранные кружки", 180, 405, 80));
            _zones.Add(new InteractionZone("cup_shelf", "E — поставить кружки на полку", -118, 548, 150));
            _zones.Add(new InteractionZone("takeaway_cup_shelf", "E — взять стакан одноразовый стакан", 56, 596, 84));

            _zones.Add(new InteractionZone("table1_wipe", "E — протереть первый стол", -150, 155, 72, true));
            _zones.Add(new InteractionZone("table2_wipe", "E — протереть второй стол", 150, 155, 72, true));
            _zones.Add(new InteractionZone("tv", "E — включить телевизор", 0, 70, 90, true));
            _zones.Add(new InteractionZone("client_tia", "E — Обслужить клиента: Тиа", ClientTargetX, ClientTargetZ, 150, true));
            _zones.Add(new InteractionZone("counter", "E — обслужить клиента у прилавка", 60, 360, 95));
            _zones.Add(new InteractionZone("cash_recipe", "E — Посмотреть рецепт", 98, 395, 105, true));
            _zones.Add(new InteractionZone("coffee_beans", "E — взять кофейные зёрна", 138, 400, 78, true));
            _zones.Add(new InteractionZone("coffee_machine", "E — сварить кофе", 252, 522, 110, true));
            _zones.Add(new InteractionZone("fridge_milk", "E — добавить молоко", -236, 548, 115, true));
            _zones.Add(new InteractionZone("raspberry_syrup", "E — добавить малиновый сироп", -246, 426, 46, true));
            _zones.Add(new InteractionZone("cash_register", "E — посчитать кассу", 98, 395, 70));
            _zones.Add(new InteractionZone("corridor_door", "E — Дверь", -156, 565, 70));
        }

        private void StartDay1()
        {
            Reputation = 0;
            CurrentDay = 1;
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
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _takeawayCupTaken = false;
            _hasTakeawayCup = false;
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
            _clientCountdownTimer = 0;
            SetStartShiftObjectives();
            _stage = StoryStage.Day1WorkTasks;
        }

        private void StartDay2()
        {
            CurrentDay = 2;
            LightOn = false;
            _setLightSwitchVisual?.Invoke(LightOn);
            IsLightFlickering = true;
            _stage = StoryStage.Day2WorkTasks;
            ShowCenterText("День 2", 2.0);
            ResetClientState();
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _takeawayCupTaken = false;
            _hasTakeawayCup = false;
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
            _clientCountdownTimer = 0;
            SetStartShiftObjectives();
        }

        private void StartDay3()
        {
            CurrentDay = 3;
            LightOn = false;
            _setLightSwitchVisual?.Invoke(LightOn);
            IsLightFlickering = true;
            _stage = StoryStage.Day3WorkTasks;
            ShowCenterText("День 3", 2.0);
            ResetClientState();
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _takeawayCupTaken = false;
            _hasTakeawayCup = false;
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
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
            _tvTurnedOn = false;
            _clientCountdownActive = false;
            _returnToCounterCompleted = false;
            _raspberryLatteQuestStarted = false;
            _recipeOpenedForLatte = false;
            _takeawayCupTaken = false;
            _hasTakeawayCup = false;
            ResetLatteCupState();
            _setTakeawayShelfCupVisible?.Invoke(true);
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
            _objectives.Add(new GameObjective("собрать грязные чашки со стола"));
        }

        private void SetEndShiftObjectives()
        {
            _objectives.Clear();
            _objectives.Add(new GameObjective("посчитать кассу"));
            _objectives.Add(new GameObjective("выключить свет"));
        }

        private void InteractLightSwitch()
        {
            if (CurrentDay == 3 && !LightOn && _stage == StoryStage.Day3WorkTasks)
            {
                ShowCenterText("ЩЁЛК... ЩЁЛК...", 1.5);
                _screenFlash = true;
                LightOn = true;
                _setLightSwitchVisual?.Invoke(LightOn);
                CompleteObjective("включить свет", "Свет включился не сразу. В отражении будто кто-то стоял за спиной.");
                return;
            }

            LightOn = !LightOn;
            _setLightSwitchVisual?.Invoke(LightOn);

            if (LightOn)
            {
                if (CurrentDay == 1 && !_day1IntroLineShown)
                {
                    _day1IntroLineShown = true;
                    CompleteObjective("включить свет", null);
                    ShowBottomText("Эх... Очередная смена... Очередная неделя без выходных", 5.0);
                    QueueBottomText("Надо подготовиться к началу смены, убрать и помыть все кружки", 5.0, 5.0);
                    _day1SecondIntroLineScheduled = true;
                }
                else
                {
                    CompleteObjective("включить свет", "Свет включён.");
                }
            }
            else
            {
                CompleteObjective("выключить свет", "Свет выключен.");
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

            if (AllDirtyCupsCollected())
                FinishDirtyCupsCollected();
            else
                ShowBottomText("Ты собрала кружки с первого стола. Осталась кружка на втором столе.", 2.4);
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

            if (AllDirtyCupsCollected())
                FinishDirtyCupsCollected();
            else
                ShowBottomText("Ты собрала кружку со второго стола. Остались кружки на первом столе.", 2.4);
        }

        private void FinishDirtyCupsCollected()
        {
            if (FindObjective("помыть чашки") == null)
                _objectives.Add(new GameObjective("помыть чашки"));

            CompleteObjective("собрать грязные чашки со стола", null);
            ShowBottomText("Все грязные кружки собраны. Теперь их нужно помыть у раковины.", 2.4);
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

            if (AllTablesWiped())
                FinishTablesWiped();
            else
                ShowBottomText("Первый стол протёрт. На втором ещё остались следы кофе.", 2.2);
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

            if (AllTablesWiped())
                FinishTablesWiped();
            else
                ShowBottomText("Второй стол протёрт. На первом ещё остались следы кофе.", 2.2);
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
            if (FindObjective("заправить кофемашину") == null)
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
        }

        private void InteractTv()
        {
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
        }

        private void InteractCupShelf()
        {
            if (_raspberryLatteQuestStarted && _recipeOpenedForLatte && !_takeawayCupTaken && FindObjective("взять стакан одноразовый стакан") != null)
            {
                _takeawayCupTaken = true;
                _hasTakeawayCup = true;
                ResetLatteCupState();
                _setTakeawayShelfCupVisible?.Invoke(false);
                CompleteLatteSubObjective("взять стакан одноразовый стакан");

                ShowBottomText("Ты взяла одноразовый стакан.", 2.0);
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

            if (FindObjective("протереть столы") == null)
                _objectives.Add(new GameObjective("протереть столы"));

            CompleteObjective("положить чашки на полку", null);
            ShowBottomText("Теперь нужно протереть столы от разводов", 5.0);
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
            _clientWalking = false;
            _clientGreetingShown = false;
            _clientReadyToServe = false;
            _clientOrderFinished = false;
            _recipeOverlayVisible = false;
            _setCashRecipeScreenVisible?.Invoke(false);
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
            _clientCountdownActive = false;
            _clientCountdownTimer = 0;
            _clientWalking = false;
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
            _clientWalking = true;
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

        private void UpdateClient(double deltaTime)
        {
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

        private void FinishClientArrival()
        {
            _clientWalking = false;
            _clientX = ClientTargetX;
            _clientZ = ClientTargetZ;
            _clientYaw = 0;
            _setClientTransform?.Invoke(_clientX, _clientZ, _clientYaw);

            if (_clientReadyToServe || _clientGreetingShown)
                return;

            _clientReadyToServe = true;
        }

        private void StartTiaDialogue()
        {
            if (!_clientReadyToServe || _clientGreetingShown)
                return;

            _clientReadyToServe = false;
            _clientGreetingShown = true;
            _clientDialogueStage = ClientDialogueStage.Greeting;
            ShowBottomText("Тиа: Добрый день!", 4.0);
        }

        private void StartRaspberryLatteQuest()
        {
            if (_raspberryLatteQuestStarted)
                return;

            _raspberryLatteQuestStarted = true;
            _recipeOpenedForLatte = false;
            _takeawayCupTaken = false;
            _hasTakeawayCup = false;
            ResetLatteCupState();

            _objectives.Clear();
            _objectives.Add(new GameObjective("приготовить малиновый латте"));
            _objectives.Add(new GameObjective("   открыть рецепт"));

            ShowBottomText("Нужно возобновить в памяти рецепт", 3.6);
        }

        private void InteractCashRecipe()
        {
            _recipeOverlayVisible = true;

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
                ShowBottomText("Кофейные зёрна засыпаются в кофемашину...", 1.8);
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

            _coffeeBrewingInProgress = true;
            _coffeeBrewingTimer = CoffeeBrewDuration;
            ShowBottomText("Кофе варится...", 1.2);
        }

        private void InteractFridgeMilk()
        {
            if (!_raspberryLatteQuestStarted || !_takeawayCupTaken)
            {
                ShowBottomText("Сейчас холодильник не нужен.", 1.4);
                return;
            }

            if (!_hasTakeawayCup)
            {
                ShowBottomText("Сначала возьми одноразовый стакан.", 1.6);
                return;
            }

            if (_coffeePortionsAdded < 1)
            {
                ShowBottomText("Сначала нужно сварить кофе.", 1.6);
                return;
            }

            if (_milkPortionsAdded >= 3)
            {
                ShowBottomText("Молоко уже добавлено полностью.", 1.4);
                return;
            }

            _milkPortionsAdded++;

            if (_milkPortionsAdded >= 3)
            {
                EvaluateLatteQuestCompletion();
                ShowBottomText("Молоко добавлено: 3/3", 1.8);
            }
            else
            {
                ShowBottomText("Молоко добавлено: " + _milkPortionsAdded + "/3", 1.6);
            }
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

            if (_raspberrySyrupPortionsAdded >= 1)
            {
                ShowBottomText("Малиновый сироп уже добавлен.", 1.4);
                return;
            }

            _raspberrySyrupPortionsAdded = 1;
            _raspberryPumpAnimationTimer = RaspberryPumpAnimationDuration;
            EvaluateLatteQuestCompletion();
            ShowBottomText("Малиновый сироп добавлен.", 1.8);
        }

        private void UpdateSinkWashAnimation(double deltaTime)
        {
            if (!_sinkWashInProgress)
                return;

            _sinkWashTimer += deltaTime;
            double progress = _sinkWashTimer / SinkWashDuration;
            if (progress > 1.0)
                progress = 1.0;

            _setSinkWashAnimation?.Invoke(true, progress);

            if (_sinkWashTimer < SinkWashDuration)
                return;

            _sinkWashInProgress = false;
            _sinkWashTimer = 0;
            _setSinkWashAnimation?.Invoke(false, 0);
            _cupsWashed = true;

            if (FindObjective("положить чашки на полку") == null)
                _objectives.Add(new GameObjective("положить чашки на полку"));

            CompleteObjective("помыть чашки", null);
            ShowBottomText("Кружки вымыты. Теперь поставь их на полку.", 2.6);
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
            _coffeeBeansTaken = false;
            _setCoffeeMachineRefillAnimation?.Invoke(false, 0);
            _setCoffeeBeanFrontBagVisible?.Invoke(true);

            if (FindObjective("включить телевизор") == null)
                _objectives.Add(new GameObjective("включить телевизор"));

            CompleteObjective("заправить кофемашину", null);
            ShowBottomText("Кофемашина заправлена. Теперь нужно включить телевизор", 4.2);
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
            _coffeePortionsAdded = 1;
            EvaluateLatteQuestCompletion();
            ShowBottomText("Порция кофе готова.", 1.8);
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

            giveOrderObjective.IsCompleted = true;
            _latteReadyToServe = false;
            _hasTakeawayCup = false;
            ShowBottomText("Ты отдала заказ Тиа.", 2.2);
            QueueBottomText("Тиа: Спасибо.", 2.2, 2.4);
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
                QueueBottomText("Нужно отдать заказ", 2.0, 2.2);
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
                    ShowBottomText("с вас 300р.", 5.0);
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

            if (zone.Id == "client_tia" && FindObjective("отдать заказ") != null && !FindObjective("отдать заказ").IsCompleted)
            {
                PromptText = "E — отдать заказ";
                return;
            }

            if ((zone.Id == "cup_shelf" || zone.Id == "takeaway_cup_shelf") && _raspberryLatteQuestStarted && _recipeOpenedForLatte && !_takeawayCupTaken && FindObjective("взять стакан одноразовый стакан") != null)
            {
                PromptText = "E — взять стакан одноразовый стакан";
                return;
            }

            if (zone.Id == "coffee_beans")
            {
                PromptText = "E — взять кофейные зёрна";
                return;
            }

            if (zone.Id == "coffee_machine")
            {
                if (FindObjective("заправить кофемашину") != null && !FindObjective("заправить кофемашину").IsCompleted)
                {
                    PromptText = "E — заправить кофемашину";
                    return;
                }

                PromptText = "E — сварить кофе";
                return;
            }

            if (zone.Id == "fridge_milk")
            {
                PromptText = "E — добавить молоко";
                return;
            }

            if (zone.Id == "raspberry_syrup")
            {
                PromptText = "E — добавить малиновый сироп";
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

            if (zone.Id == "takeaway_cup_shelf")
                return _raspberryLatteQuestStarted && _recipeOpenedForLatte && !_takeawayCupTaken && FindObjective("взять стакан одноразовый стакан") != null;

            if (zone.Id == "table1_wipe")
                return _cupsReturnedToShelf && FindObjective("протереть столы") != null && !_table1Wiped;

            if (zone.Id == "table2_wipe")
                return _cupsReturnedToShelf && FindObjective("протереть столы") != null && !_table2Wiped;

            if (zone.Id == "tv")
                return FindObjective("включить телевизор") != null && !_tvTurnedOn;

            if (zone.Id == "coffee_beans")
                return FindObjective("заправить кофемашину") != null && !_coffeeBeansTaken && !_coffeeMachineRefillInProgress;

            if (zone.Id == "client_tia")
            {
                if (FindObjective("отдать заказ") != null && !FindObjective("отдать заказ").IsCompleted)
                {
                    double dxGive = _camera.X - ClientTargetX;
                    double dzGive = _camera.Z - ClientTargetZ;
                    return dxGive * dxGive + dzGive * dzGive <= 125 * 125;
                }

                return _clientReadyToServe && !_clientGreetingShown;
            }

            if (zone.Id == "cash_recipe")
                return _clientOrderFinished;

            if (zone.Id == "coffee_machine")
            {
                GameObjective refillObjective = FindObjective("заправить кофемашину");
                if (refillObjective != null && !refillObjective.IsCompleted)
                    return !_coffeeMachineRefillInProgress;

                return _takeawayCupTaken && !_coffeeBrewingInProgress && _coffeePortionsAdded < 1;
            }

            if (zone.Id == "fridge_milk")
                return _takeawayCupTaken && _coffeePortionsAdded >= 1 && _milkPortionsAdded < 3;

            if (zone.Id == "raspberry_syrup")
                return _takeawayCupTaken && _milkPortionsAdded >= 3 && _raspberrySyrupPortionsAdded < 1;

            if (zone.Id == "light_switch")
                return FindObjective("включить свет") != null && !FindObjective("включить свет").IsCompleted;

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

        private void ShowBottomText(string text, double seconds)
        {
            BottomText = text;
            _messageTimer = seconds;
        }

        private void ShowCenterText(string text, double seconds)
        {
            CenterText = text;
            _messageTimer = seconds;
        }
    }
}
