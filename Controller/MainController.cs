using System;
using System.Collections.Generic;
using System.Drawing;
using игра_для_проги.Model;

namespace игра_для_проги.Controller
{
    /// <summary>
    /// Собирает сцену кофейни, управляет камерой и проверяет столкновения.
    /// Барная стойка оставлена простой: без декоративных панелей, полос, ручек, швов и царапин.
    /// </summary>
    public class MainController
    {
        private readonly SceneModel _model;
        private readonly List<int> _lightSwitchLeverPoints = new List<int>();
        private readonly List<double> _lightSwitchLeverBaseY = new List<double>();

        // Грязные кружки стоят на двух столах в начале смены.
        // Каждый стол собирается отдельно, чтобы не пропадали все кружки сразу.
        private readonly List<int> _dirtyTable1CupPoints = new List<int>();
        private readonly List<double> _dirtyTable1CupBaseY = new List<double>();
        private readonly List<int> _dirtyTable2CupPoints = new List<int>();
        private readonly List<double> _dirtyTable2CupBaseY = new List<double>();

        // Кофейные разводы под кружками тоже привязаны к конкретному столу.
        // Они исчезают только после протирки соответствующего стола.
        private readonly List<int> _table1StainPoints = new List<int>();
        private readonly List<double> _table1StainBaseY = new List<double>();
        private readonly List<int> _table2StainPoints = new List<int>();
        private readonly List<double> _table2StainBaseY = new List<double>();

        // Три недостающие кружки на полке сначала спрятаны.
        // После мойки и постановки на место они появляются на пустом месте полки.
        private readonly List<int> _shelfReturnedCupPoints = new List<int>();
        private readonly List<double> _shelfReturnedCupBaseY = new List<double>();

        // Один одноразовый стакан на полке исчезает после того, как игрок взял его для заказа Тии.
        private readonly List<int> _takeawayShelfCupPoints = new List<int>();
        private readonly List<double> _takeawayShelfCupBaseY = new List<double>();

        // Предметы для финальной выдачи заказа Тии: сначала стакан на коврике WELCOME,
        // после реплики Тии вместо него появляется зелёная купюра 300р.
        private readonly List<int> _tiaServedCupPoints = new List<int>();
        private readonly List<double> _tiaServedCupBaseY = new List<double>();
        private readonly List<int> _tiaPaymentBillPoints = new List<int>();
        private readonly List<double> _tiaPaymentBillBaseY = new List<double>();

        // После выдачи заказа в руке Тии появляется стакан.
        private readonly List<int> _tiaHoldingCupPoints = new List<int>();
        private readonly List<double> _tiaHoldingCupLocalX = new List<double>();
        private readonly List<double> _tiaHoldingCupLocalY = new List<double>();
        private readonly List<double> _tiaHoldingCupLocalZ = new List<double>();

        // Существующая правая рука Тии (для игрока она слева) сгибается в локте.
        private readonly List<int> _tiaBentArmPoints = new List<int>();
        private readonly List<double> _tiaBentArmLocalX = new List<double>();
        private readonly List<double> _tiaBentArmLocalY = new List<double>();
        private readonly List<double> _tiaBentArmLocalZ = new List<double>();
        private bool _tiaHoldingCupVisible;

        // 3D-стакан на поддоне кофемашины появляется только во время приготовления кофе.
        private readonly List<int> _coffeeMachineCupPoints = new List<int>();
        private readonly List<double> _coffeeMachineCupBaseY = new List<double>();
        private readonly List<int> _coffeeMachineCupCoffeePoints = new List<int>();
        private readonly List<double> _coffeeMachineCupCoffeeBaseY = new List<double>();
        private readonly List<int> _coffeeMachineCupCoffeeTopPoints = new List<int>();
        private double _coffeeMachineCupCoffeeBottomY;
        private double _coffeeMachineCupCoffeeTopY;

        // Носик/головка малинового сиропа анимируются отдельной группой точек.
        private readonly List<int> _raspberryPumpPoints = new List<int>();
        private readonly List<double> _raspberryPumpBaseY = new List<double>();

        // Передний мешок с кофейными зёрнами можно временно спрятать,
        // а рядом с кофемашиной есть отдельная группа для анимации заправки.
        private readonly List<int> _frontCoffeeBeanBagPoints = new List<int>();
        private readonly List<double> _frontCoffeeBeanBagBaseY = new List<double>();
        private readonly List<int> _coffeeRefillBagPoints = new List<int>();
        private readonly List<double> _coffeeRefillBagBaseX = new List<double>();
        private readonly List<double> _coffeeRefillBagBaseY = new List<double>();
        private readonly List<double> _coffeeRefillBagBaseZ = new List<double>();
        private readonly List<int> _coffeeRefillBeansPoints = new List<int>();
        private readonly List<double> _coffeeRefillBeansBaseX = new List<double>();
        private readonly List<double> _coffeeRefillBeansBaseY = new List<double>();
        private readonly List<double> _coffeeRefillBeansBaseZ = new List<double>();

        // Пена в раковине появляется во время анимации мойки кружек.
        private readonly List<int> _sinkWashFoamPoints = new List<int>();
        private readonly List<double> _sinkWashFoamBaseX = new List<double>();
        private readonly List<double> _sinkWashFoamBaseY = new List<double>();
        private readonly List<double> _sinkWashFoamBaseZ = new List<double>();

        // Струи воды из крана во время мойки.
        private readonly List<int> _sinkWashWaterPoints = new List<int>();
        private readonly List<double> _sinkWashWaterBaseX = new List<double>();
        private readonly List<double> _sinkWashWaterBaseY = new List<double>();
        private readonly List<double> _sinkWashWaterBaseZ = new List<double>();

        // Изображение на телевизоре сначала спрятано.
        // После включения ТВ появляется кружка кофе и подпись Cafe Marcul.
        private readonly List<int> _tvScreenContentPoints = new List<int>();
        private readonly List<double> _tvScreenContentBaseY = new List<double>();
        private readonly List<int> _tvAnimatedScreenPoints = new List<int>();
        private readonly List<double> _tvAnimatedScreenBaseY = new List<double>();
        private bool _tvScreenVisible;
        private double _tvAnimationTime;

        // Экран кассы с заказом появляется после финальной фразы по заказу Тии.
        private readonly List<int> _cashRecipeScreenPoints = new List<int>();
        private readonly List<double> _cashRecipeScreenBaseY = new List<double>();

        // 3D-клиентка собрана как одна управляемая группа точек.
        // Это позволяет скрывать её, перемещать по сцене и поворачивать.
        private readonly List<int> _clientPoints = new List<int>();
        private readonly List<double> _clientLocalX = new List<double>();
        private readonly List<double> _clientLocalY = new List<double>();
        private readonly List<double> _clientLocalZ = new List<double>();
        private bool _clientVisible;
        private double _clientWorldX = 250;
        private double _clientWorldZ = 300;
        private double _clientYaw = 0;
        private double _clientAnimationPhase = 0;
        private double _clientPrevWorldX = 250;
        private double _clientPrevWorldZ = 300;
        private bool _clientWalkingNow;
        private bool _tiaBarPassageBlocked;

        public Camera3D Camera { get; private set; }

        private const double PlayerRadius = 18.0;

        private const double RoomMinX = -285;
        private const double RoomMaxX = 285;
        private const double RoomMinZ = 20;
        private const double RoomMaxZ = 585;

        // Старт игрока: около правой служебной двери без стекла.
        // Игрок стоит близко напротив двери и смотрит прямо на неё.
        private const double SpawnX = 250;
        private const double SpawnY = 0;
        private const double SpawnZ = 300;
        private const double SpawnYaw = Math.PI / 2.0;

        public MainController(SceneModel model)
        {
            _model = model;
            Camera = new Camera3D();
        }

        public void CreateTestScene()
        {
            ClearScene();

            AddRoomShell();
            AddLeftWallWithWindow();
            AddDoorWithGlass();
            AddRightWallServiceDoor();
            AddLightSwitchNearServiceDoor();
            AddRoomReferenceOutlines();

            AddReferenceStyleSurfaceFinish();
            AddFridgeArea();
            AddBarArea();
            AddTablesAndChairs();
            AddDirtyCupsAndCoffeeStainsOnTables();

            AddWallPosters();

            AddCabinetWithSyrups();
            AddTeaShelf();
            SetReturnedShelfCupsVisible(false);
            SetDirtyCupsOnTable1Visible(true);
            SetDirtyCupsOnTable2Visible(true);
            SetCoffeeStainsOnTable1Visible(true);
            SetCoffeeStainsOnTable2Visible(true);
            AddTeaShelfSign();
            AddWallMenu();
            AddHorrorTvStandAndTv();
            SetTvScreenOn(false);
            SetCoffeeMachineCupState(false, 0);
            SetRaspberryPumpPressed(0);
            SetCoffeeBeanFrontBagVisible(true);
            SetCoffeeMachineRefillAnimation(false, 0);
            SetSinkWashAnimation(false, 0);

            AddEntranceRug();
            AddWallVentOnRightWall();

            AddSubtleAtmosphereDetails();
            AddCustomerNpc();
            AddTiaHoldingCupAttachment();
            SetTiaHoldingCupVisible(false);
            SetClientVisible(false);

            AddManualFurnitureColliders();
            RespawnCamera();
        }

        private static class HorrorColor
        {
            public static readonly Color Floor = Color.FromArgb(112, 112, 108);
            public static readonly Color Ceiling = Color.FromArgb(84, 82, 78);

            public static readonly Color Wall = Color.FromArgb(142, 116, 88);
            public static readonly Color WallDark = Color.FromArgb(126, 102, 78);
            public static readonly Color DirtyGlass = Color.FromArgb(62, 91, 104);

            public static readonly Color DeepWood = Color.FromArgb(48, 25, 18);
            public static readonly Color DeadWood = Color.FromArgb(68, 36, 24);
            public static readonly Color DeadWoodLight = Color.FromArgb(94, 52, 32);

            public static readonly Color WornFurniture = Color.FromArgb(82, 60, 44);
            public static readonly Color WornFurnitureDark = Color.FromArgb(58, 40, 28);
            public static readonly Color WornFurnitureLight = Color.FromArgb(102, 76, 56);

            public static readonly Color WoodPlankA = Color.FromArgb(96, 70, 48);
            public static readonly Color WoodPlankB = Color.FromArgb(86, 62, 42);
            public static readonly Color WoodPlankC = Color.FromArgb(108, 80, 56);
            public static readonly Color WoodPlankD = Color.FromArgb(74, 52, 36);

            public static readonly Color WoodSeam = Color.FromArgb(52, 36, 24);
            public static readonly Color WoodHighlight = Color.FromArgb(124, 96, 70);

            public static readonly Color DustScratch = Color.FromArgb(72, 96, 88, 78);
            public static readonly Color DarkScratch = Color.FromArgb(80, 52, 38, 30);

            public static readonly Color DriedBlood = Color.FromArgb(90, 18, 18);
            public static readonly Color OldBlood = Color.FromArgb(58, 12, 12);

            public static readonly Color Brass = Color.FromArgb(132, 104, 54);
            public static readonly Color Rust = Color.FromArgb(122, 54, 28);
            public static readonly Color ColdSteel = Color.FromArgb(150, 154, 160);
            public static readonly Color SteelDark = Color.FromArgb(80, 86, 92);

            public static readonly Color RedGlow = Color.FromArgb(160, 18, 18);
            public static readonly Color Paper = Color.FromArgb(218, 213, 196);
        }

        private enum SyrupIconType
        {
            Vanilla,
            Caramel,
            Chocolate,
            Mint,
            Raspberry,
            Coconut,
            IrishCream,
            Hazelnut
        }

        private void ClearScene()
        {
            _model.Points.Clear();
            _model.Edges.Clear();
            _model.Faces.Clear();
            _model.Colliders.Clear();
            _lightSwitchLeverPoints.Clear();
            _lightSwitchLeverBaseY.Clear();
            _dirtyTable1CupPoints.Clear();
            _dirtyTable1CupBaseY.Clear();
            _dirtyTable2CupPoints.Clear();
            _dirtyTable2CupBaseY.Clear();
            _table1StainPoints.Clear();
            _table1StainBaseY.Clear();
            _table2StainPoints.Clear();
            _table2StainBaseY.Clear();
            _shelfReturnedCupPoints.Clear();
            _shelfReturnedCupBaseY.Clear();
            _takeawayShelfCupPoints.Clear();
            _takeawayShelfCupBaseY.Clear();
            _tiaServedCupPoints.Clear();
            _tiaServedCupBaseY.Clear();
            _tiaPaymentBillPoints.Clear();
            _tiaPaymentBillBaseY.Clear();
            _coffeeMachineCupPoints.Clear();
            _coffeeMachineCupBaseY.Clear();
            _coffeeMachineCupCoffeePoints.Clear();
            _coffeeMachineCupCoffeeBaseY.Clear();
            _coffeeMachineCupCoffeeTopPoints.Clear();
            _raspberryPumpPoints.Clear();
            _raspberryPumpBaseY.Clear();
            _frontCoffeeBeanBagPoints.Clear();
            _frontCoffeeBeanBagBaseY.Clear();
            _coffeeRefillBagPoints.Clear();
            _coffeeRefillBagBaseX.Clear();
            _coffeeRefillBagBaseY.Clear();
            _coffeeRefillBagBaseZ.Clear();
            _coffeeRefillBeansPoints.Clear();
            _coffeeRefillBeansBaseX.Clear();
            _coffeeRefillBeansBaseY.Clear();
            _coffeeRefillBeansBaseZ.Clear();
            _sinkWashFoamPoints.Clear();
            _sinkWashFoamBaseX.Clear();
            _sinkWashFoamBaseY.Clear();
            _sinkWashFoamBaseZ.Clear();
            _sinkWashWaterPoints.Clear();
            _sinkWashWaterBaseX.Clear();
            _sinkWashWaterBaseY.Clear();
            _sinkWashWaterBaseZ.Clear();
            _tvScreenContentPoints.Clear();
            _tvScreenContentBaseY.Clear();
            _tvAnimatedScreenPoints.Clear();
            _tvAnimatedScreenBaseY.Clear();
            _tvScreenVisible = false;
            _tvAnimationTime = 0;

            _cashRecipeScreenPoints.Clear();
            _cashRecipeScreenBaseY.Clear();
            _clientPoints.Clear();
            _clientLocalX.Clear();
            _clientLocalY.Clear();
            _clientLocalZ.Clear();
            _clientVisible = false;
            _clientWorldX = 250;
            _clientWorldZ = 300;
            _clientYaw = 0;
            _clientAnimationPhase = 0;
            _clientPrevWorldX = 250;
            _clientPrevWorldZ = 300;
            _clientWalkingNow = false;
        }

        private void AddRoomShell()
        {
            int p0 = _model.AddPoint(-300, -101, 0);
            int p1 = _model.AddPoint(300, -101, 0);
            int p2 = _model.AddPoint(300, -101, 600);
            int p3 = _model.AddPoint(-300, -101, 600);

            int p4 = _model.AddPoint(-300, 150, 0);
            int p5 = _model.AddPoint(300, 150, 0);
            int p6 = _model.AddPoint(300, 150, 600);
            int p7 = _model.AddPoint(-300, 150, 600);

            AddBoxEdges(p0, p1, p2, p3, p4, p5, p6, p7);

            _model.AddFace(new List<int> { p0, p3, p2, p1 }, null, HorrorColor.Floor, FaceLayer.Floor);
            _model.AddFace(new List<int> { p4, p5, p6, p7 }, null, HorrorColor.Ceiling, FaceLayer.Floor);
            _model.AddFace(new List<int> { p3, p7, p6, p2 }, null, HorrorColor.Wall, FaceLayer.Wall);
            _model.AddFace(new List<int> { p1, p2, p6, p5 }, null, HorrorColor.WallDark, FaceLayer.Wall);
            _model.AddFace(new List<int> { p0, p1, p5, p4 }, null, HorrorColor.Wall, FaceLayer.Wall);
        }

        private bool IsDustExcludedZone(double x, double z)
        {
            // Коврик у входа:
            // AddEntranceRug()
            // x = 214..286, z = 258..342
            bool entranceRug =
                x >= 208 && x <= 292 &&
                z >= 252 && z <= 348;

            // Левый барный блок:
            // AddBlock(-101, -100, 380, 29, -35, 450)
            bool leftBarBlock =
                x >= -106 && x <= 34 &&
                z >= 375 && z <= 455;

            // Средний барный блок с кассой:
            // AddBlock(29, -100, 380, 159, -35, 450)
            bool middleBarBlock =
                x >= 24 && x <= 164 &&
                z >= 375 && z <= 455;

            // Правый барный блок с раковиной:
            // AddBlock(159, -100, 380, 299, -35, 450)
            bool sinkBarBlock =
                x >= 154 && x <= 304 &&
                z >= 375 && z <= 455;

            // Боковой барный блок:
            // AddBlock(-131, -100, 380, -101, -35, 490)
            bool sideBarBlock =
                x >= -136 && x <= -96 &&
                z >= 375 && z <= 495;

            // Стойка у кофемашины:
            // AddBlock(255, -100, 456, 299, -35, 590)
            bool coffeeMachineCounter =
                x >= 250 && x <= 304 &&
                z >= 451 && z <= 595;

            return
                entranceRug ||
                leftBarBlock ||
                middleBarBlock ||
                sinkBarBlock ||
                sideBarBlock ||
                coffeeMachineCounter;
        }

        private bool IsDustRectExcluded(double x1, double z1, double x2, double z2)
        {
            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);
            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);

            return
                RectIntersects(minX, minZ, maxX, maxZ, 208, 252, 292, 348) ||      // коврик у входа
                RectIntersects(minX, minZ, maxX, maxZ, -106, 375, 34, 455) ||      // левый барный блок
                RectIntersects(minX, minZ, maxX, maxZ, 24, 375, 164, 455) ||       // средний барный блок
                RectIntersects(minX, minZ, maxX, maxZ, 154, 375, 304, 455) ||      // блок с раковиной
                RectIntersects(minX, minZ, maxX, maxZ, -136, 375, -96, 495) ||     // боковой блок
                RectIntersects(minX, minZ, maxX, maxZ, 250, 451, 304, 595);        // стойка у кофемашины
        }

        private bool RectIntersects(
            double ax1, double az1,
            double ax2, double az2,
            double bx1, double bz1,
            double bx2, double bz2)
        {
            return ax1 <= bx2 &&
                   ax2 >= bx1 &&
                   az1 <= bz2 &&
                   az2 >= bz1;
        }

        private void AddRoomReferenceOutlines()
        {
            AddOutlineOnWall(-300, -20, 260, -300, 60, 310);
            AddOutlineOnWall(-188, -100, 600, -124, 72, 600);
        }

        private void AddReferenceStyleSurfaceFinish()
        {
            AddPlainOldWallWear();
            AddMorningWindowLightOnFloor();
            AddFineDustOnFloor();
        }

        private void AddMorningWindowLightOnFloor()
        {
            // Утренний свет от окна теперь является частью 3D-сцены, а не экранной
            // накладкой Form1. Поэтому мебель слоя Furniture, включая барную стойку,
            // рисуется поверх этого света и закрывает его как непрозрачный объект.
            double y = -100.735;

            Color warmWide = Color.FromArgb(34, 255, 231, 166);
            Color warmCore = Color.FromArgb(52, 255, 238, 184);
            Color warmSoft = Color.FromArgb(24, 255, 221, 146);

            // Основное пятно на полу перед стойкой: оно заканчивается до z=372,
            // то есть перед баром, а не за ним.
            AddQuad(
                -286, y, 252,
                -286, y, 324,
                -68, y, 366,
                -116, y, 318,
                "no_outline",
                warmWide,
                FaceLayer.Floor
            );

            // Более яркая сердцевина луча.
            AddQuad(
                -284, y + 0.004, 276,
                -284, y + 0.004, 302,
                -112, y + 0.004, 350,
                -138, y + 0.004, 326,
                "no_outline",
                warmCore,
                FaceLayer.Floor
            );

            // Мягкое боковое рассеивание рядом с окном.
            AddQuad(
                -286, y + 0.002, 306,
                -286, y + 0.002, 344,
                -172, y + 0.002, 360,
                -206, y + 0.002, 324,
                "no_outline",
                warmSoft,
                FaceLayer.Floor
            );
        }

        private void AddPlainOldWallWear()
        {
            AddBackWallPlainWear();
            AddLeftWallPlainWear();
            AddRightWallPlainWear();
        }

        private void AddBackWallPlainWear()
        {
            double z = 1.04;

            // Мягкое затемнение снизу, как грязь у пола
            AddRectOnZPlane(-300, -101, 300, -86, z,
                Color.FromArgb(116, 92, 70), FaceLayer.Wall);

            AddRectOnZPlane(-300, -86, 300, -62, z + 0.01,
                Color.FromArgb(128, 102, 78), FaceLayer.Wall);

            AddRectOnZPlane(190, -18, 286, 52, z + 0.016,
                Color.FromArgb(136, 108, 82), FaceLayer.Wall);

            // Очень редкие потёки/грязные следы
            AddScratchOnZPlane(-254, -94, -248, -26, z + 0.02,
                Color.FromArgb(92, 72, 54), 1.8, FaceLayer.Wall);

            AddScratchOnZPlane(232, -88, 226, -20, z + 0.02,
                Color.FromArgb(96, 76, 58), 1.6, FaceLayer.Wall);

            AddScratchOnZPlane(28, 128, 20, 72, z + 0.02,
                Color.FromArgb(118, 92, 68), 1.4, FaceLayer.Wall);
        }

        private void AddLeftWallPlainWear()
        {
            double x = -298.88;

            AddRectOnXPlane(x, -101, 0, -86, 600,
                Color.FromArgb(114, 90, 68), FaceLayer.Wall);

            AddRectOnXPlane(x + 0.01, -86, 0, -62, 600,
                Color.FromArgb(126, 100, 76), FaceLayer.Wall);

            AddRectOnXPlane(x + 0.012, -46, 40, 36, 160,
                Color.FromArgb(140, 112, 84), FaceLayer.Wall);

            AddScratchOnXPlane(x + 0.018, -96, 118, -24, 124,
                Color.FromArgb(94, 74, 56), 1.7, FaceLayer.Wall);

            AddScratchOnXPlane(x + 0.018, 118, 218, 56, 224,
                Color.FromArgb(118, 92, 68), 1.5, FaceLayer.Wall);
        }

        private void AddRightWallPlainWear()
        {
            double x = 298.88;

            // Служебная дверь без стекла стоит на правой стене в районе z = 265..335.
            // Раньше общие слои грязи и нижней полосы проходили прямо по двери,
            // из-за этого при взгляде вблизи появлялась "трёхслойность" и западание текстур.
            // Поэтому все широкие настенные слои делим на участки ДО двери и ПОСЛЕ двери.
            double doorZ1 = 260;
            double doorZ2 = 340;

            AddRectOnXPlane(x, -101, 0, -86, doorZ1,
                Color.FromArgb(110, 86, 66), FaceLayer.Wall);

            AddRectOnXPlane(x, -101, doorZ2, -86, 600,
                Color.FromArgb(110, 86, 66), FaceLayer.Wall);

            AddRectOnXPlane(x - 0.01, -86, 0, -62, doorZ1,
                Color.FromArgb(122, 96, 74), FaceLayer.Wall);

            AddRectOnXPlane(x - 0.01, -86, doorZ2, -62, 600,
                Color.FromArgb(122, 96, 74), FaceLayer.Wall);

            AddRectOnXPlane(x - 0.014, 12, 366, 116, 452,
                Color.FromArgb(150, 120, 90), FaceLayer.Wall);

            AddScratchOnXPlane(x - 0.018, -94, 186, -28, 192,
                Color.FromArgb(92, 72, 56), 1.6, FaceLayer.Wall);

            AddScratchOnXPlane(x - 0.018, 126, 522, 64, 528,
                Color.FromArgb(112, 88, 66), 1.5, FaceLayer.Wall);
        }

        private void AddFineDustOnFloor()
        {
            double y = -100.78;

            // ВАЖНО:
            // Не рисуем общий прямоугольник поверх всего пола.
            // Базовый серый пол уже задан в AddRoomShell через HorrorColor.Floor.
            // Пыль добавляем только мелкими частицами и только в открытых зонах.

            // Пыль вдоль левой стены
            AddDustTilesArea(-288, 18, -260, 584, y + 0.002, 5.5, 3);

            // Пыль вдоль правой стены, но НЕ под стойкой у кофемашины
            AddDustTilesArea(260, 18, 288, 446, y + 0.002, 5.5, 3);

            // Пыль у передней стены
            AddDustTilesArea(-286, 18, 286, 44, y + 0.002, 5.5, 4);

            // Пыль у дальней стены:
            // слева можно до конца, справа останавливаем до зоны кофемашины/бара
            AddDustTilesArea(-286, 560, 252, 586, y + 0.002, 5.5, 4);

            // Центральная затоптанная зона, не доходит до барной стойки
            AddDustTilesArea(-132, 92, 126, 356, y + 0.005, 6.5, 5);
            AddDustTilesArea(-84, 112, 84, 332, y + 0.006, 7.0, 6);

            // Локальная пыль только в открытом правом переднем углу
            AddDustTilesArea(176, 44, 284, 148, y + 0.007, 6.0, 5);

            // Редкие частицы по комнате, но с пропуском зоны бара
            AddRandomDustBits(y + 0.009);
        }

        private void AddDustTilesArea(
    double x1, double z1,
    double x2, double z2,
    double y,
    double step,
    int density)
        {
            Color[] palette =
            {
        Color.FromArgb(124, 122, 116),
        Color.FromArgb(118, 116, 110),
        Color.FromArgb(108, 106, 100),
        Color.FromArgb(132, 130, 122)
    };

            int ix = 0;

            for (double x = x1; x <= x2; x += step)
            {
                int iz = 0;

                for (double z = z1; z <= z2; z += step)
                {
                    int h =
                        Math.Abs((int)(x * 13.0)) +
                        Math.Abs((int)(z * 17.0)) +
                        ix * 19 +
                        iz * 23;

                    if (h % density != 0)
                    {
                        iz++;
                        continue;
                    }

                    double w = 1.1 + (h % 3) * 0.45;
                    double d = 1.0 + (h % 4) * 0.35;

                    double ox = ((h / 3) % 7) * 0.12;
                    double oz = ((h / 5) % 7) * 0.12;

                    Color c = palette[h % palette.Length];

                    double dx1 = x + ox;
                    double dz1 = z + oz;
                    double dx2 = dx1 + w;
                    double dz2 = dz1 + d;

                    if (IsDustRectExcluded(dx1, dz1, dx2, dz2))
                    {
                        iz++;
                        continue;
                    }

                    AddDustRect(
                        dx1,
                        dz1,
                        dx2,
                        dz2,
                        y + (h % 11) * 0.0002,
                        c
                    );

                    iz++;
                }

                ix++;
            }
        }

        private void AddRandomDustBits(double y)
        {
            Color[] palette =
            {
        Color.FromArgb(120, 118, 112),
        Color.FromArgb(108, 106, 100),
        Color.FromArgb(130, 128, 120)
    };

            for (int i = 0; i < 90; i++)
            {
                double x = -270 + ((i * 73) % 540);
                double z = 28 + ((i * 97) % 540);

                double w = 0.8 + (i % 3) * 0.35;
                double d = 0.8 + (i % 4) * 0.28;

                Color c = palette[i % palette.Length];

                double dx1 = x;
                double dz1 = z;
                double dx2 = x + w;
                double dz2 = z + d;

                if (IsDustRectExcluded(dx1, dz1, dx2, dz2))
                    continue;

                AddDustRect(
                    dx1,
                    dz1,
                    dx2,
                    dz2,
                    y + i * 0.0001,
                    c
                );
            }
        }
        private void AddFridgeArea()
        {
            Color body = Color.FromArgb(128, 134, 138);
            Color sideDark = Color.FromArgb(92, 98, 102);
            Color panelLight = Color.FromArgb(148, 152, 154);
            Color panelDark = Color.FromArgb(104, 110, 114);
            Color rubber = Color.FromArgb(48, 52, 54);
            Color dirt = Color.FromArgb(88, 82, 74);

            AddBlock(-295, -100, 510, -250, 80, 595, null, body, FaceLayer.Furniture);

            AddRectOnXPlane(-249.75, -96, 514, 76, 591, sideDark, FaceLayer.WallDetail);
            AddRectOnXPlane(-249.55, -94, 516, 76, 589, panelLight, FaceLayer.WallDetail);
            AddRectOnXPlane(-249.48, 22, 520, 72, 585, Color.FromArgb(138, 142, 144), FaceLayer.WallDetail);
            AddRectOnXPlane(-249.47, -90, 520, 18, 585, Color.FromArgb(132, 136, 138), FaceLayer.WallDetail);
            AddRectOnXPlane(-249.40, 18, 520, 21, 585, rubber, FaceLayer.WallDetail);
            AddRectOnXPlane(-249.38, -90, 522, -84, 583, panelDark, FaceLayer.WallDetail);

            for (int i = 0; i < 4; i++)
            {
                double z1 = 530 + i * 11;
                AddRectOnXPlane(-249.32, -89, z1, -85, z1 + 5, rubber, FaceLayer.WallDetail);
            }

            AddFridgeHandle();
            AddMilkCartonOnFridge();

            AddRectOnXPlane(-249.25, -27, 542, 58, 545, Color.FromArgb(74, 78, 80), FaceLayer.WallDetail);
            AddRectOnXPlane(-249.20, 48, 526, 56, 540, dirt, FaceLayer.WallDetail);
            AddRectOnXPlane(-249.20, -38, 560, -30, 575, Color.FromArgb(96, 90, 82), FaceLayer.WallDetail);
        }

        private void AddFridgeHandle()
        {
            // Ручку смещаем чуть правее по дверце, чтобы она не пересекалась
            // с изображением пакета молока.
            AddBlock(-249.2, -25, 556, -245.6, 55, 564, null, HorrorColor.ColdSteel, FaceLayer.Furniture);
            AddBlock(-245.8, -22, 563.2, -244.8, 52, 564.6, null, HorrorColor.SteelDark, FaceLayer.SmallDetail);
            AddBlock(-249.4, -22, 557.0, -248.8, 52, 563.0, null, Color.FromArgb(178, 180, 182), FaceLayer.SmallDetail);
        }

        private void AddMilkCartonOnFridge()
        {
            // Упрощённая и более читаемая плоская упаковка молока:
            // общий силуэт пакета, большой синий нижний блок и компактная надпись Milk.
            double xPlane = -249.14;
            double yBottom = -22.0;
            double yTop = 24.0;
            double zLeft = 521.0;
            double zRight = 539.0;

            Color outline = Color.FromArgb(110, 104, 98);
            Color body = Color.FromArgb(247, 245, 239);
            Color fold = Color.FromArgb(252, 250, 246);
            Color blue = Color.FromArgb(82, 148, 220);
            Color text = Color.FromArgb(76, 112, 160);
            Color cap = Color.FromArgb(214, 92, 120);

            AddRectOnXPlane(xPlane, yBottom, zLeft, yTop, zRight, body, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.01, yBottom + 1.0, zLeft + 1.0, yBottom + 14.0, zRight - 1.0, blue, FaceLayer.SmallDetail);
            AddQuad(xPlane + 0.02, yTop, zLeft + 2.8, xPlane + 0.02, yTop, zRight - 2.8, xPlane + 0.02, yTop + 5.4, zRight - 7.0, xPlane + 0.02, yTop + 5.4, zLeft + 7.0, null, fold, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.03, yTop + 0.7, zRight - 7.0, yTop + 5.4, zRight - 4.3, cap, FaceLayer.SmallDetail);

            // Компактная надпись MILK: ещё меньше и левее, чтобы не вылезала за край пакета.
            double y1 = yBottom + 18.4;
            double y2 = yBottom + 24.4;
            // M
            AddRectOnXPlane(xPlane + 0.05, y1, zLeft + 2.8, y2, zLeft + 3.35, text, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.05, y1, zLeft + 5.15, y2, zLeft + 5.70, text, FaceLayer.SmallDetail);
            AddQuad(xPlane + 0.05, y2, zLeft + 3.35, xPlane + 0.05, y2, zLeft + 3.85, xPlane + 0.05, y1 + 2.7, zLeft + 4.25, xPlane + 0.05, y1 + 2.7, zLeft + 3.75, null, text, FaceLayer.SmallDetail);
            AddQuad(xPlane + 0.05, y1 + 2.7, zLeft + 4.25, xPlane + 0.05, y1 + 2.7, zLeft + 4.75, xPlane + 0.05, y2, zLeft + 5.15, xPlane + 0.05, y2, zLeft + 4.65, null, text, FaceLayer.SmallDetail);
            // I
            AddRectOnXPlane(xPlane + 0.05, y1, zLeft + 7.05, y2, zLeft + 7.60, text, FaceLayer.SmallDetail);
            // L
            AddRectOnXPlane(xPlane + 0.05, y1, zLeft + 9.05, y2, zLeft + 9.60, text, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.05, y1, zLeft + 9.05, y1 + 0.75, zLeft + 11.25, text, FaceLayer.SmallDetail);
            // K — делаем толще и читаемее, чтобы диагонали не были слишком тонкими.
            AddRectOnXPlane(xPlane + 0.05, y1, zLeft + 12.15, y2, zLeft + 12.95, text, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.05, y1 + 2.50, zLeft + 12.75, y1 + 3.45, zLeft + 13.65, text, FaceLayer.SmallDetail);
            AddQuad(xPlane + 0.05, y1 + 3.05, zLeft + 12.85, xPlane + 0.05, y1 + 3.75, zLeft + 13.65, xPlane + 0.05, y2, zLeft + 15.00, xPlane + 0.05, y2 - 0.80, zLeft + 14.20, null, text, FaceLayer.SmallDetail);
            AddQuad(xPlane + 0.05, y1 + 2.90, zLeft + 12.85, xPlane + 0.05, y1 + 2.20, zLeft + 13.65, xPlane + 0.05, y1, zLeft + 15.00, xPlane + 0.05, y1 + 0.80, zLeft + 14.20, null, text, FaceLayer.SmallDetail);

            AddRectOnXPlane(xPlane + 0.08, yBottom, zLeft, yBottom + 0.9, zRight, outline, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.08, yTop - 0.9, zLeft, yTop, zRight, outline, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.08, yBottom, zLeft, yTop, zLeft + 0.9, outline, FaceLayer.SmallDetail);
            AddRectOnXPlane(xPlane + 0.08, yBottom, zRight - 0.9, yTop, zRight, outline, FaceLayer.SmallDetail);
        }

        private void AddCoffeeMachineWallCounter()
        {
            AddBlock(255, -100, 456, 299, -35, 590, null, HorrorColor.DeadWood, FaceLayer.Furniture);
            AddCoffeeMachineFacingLeft(259, -35, 462);
        }

        private void AddCoffeeBeanBagsOnCashBlock()
        {
            // Второй центральный блок бара:
            // x = 29..159, z = 380..450
            //
            // Касса занимает примерно:
            // x = 36..120, z = 390..432
            //
            // Поэтому мешки ставим справа от кассы, на свободное место.

            // Дальний мешок остаётся на месте.
            AddCoffeeBeanBag(
                126, -34.7, 394,
                148, -10.5, 406,
                Color.FromArgb(136, 104, 72),
                Color.FromArgb(104, 78, 54),
                Color.FromArgb(206, 194, 166),
                Color.FromArgb(78, 56, 38)
            );

            // Ближний к игроку мешок — именно его берём в анимации заправки.
            int frontBagStart = _model.Points.Count;
            AddCoffeeBeanBag(
                126, -34.7, 410,
                148, -9.0, 422,
                Color.FromArgb(88, 64, 44),
                Color.FromArgb(66, 46, 30),
                Color.FromArgb(212, 198, 170),
                Color.FromArgb(92, 66, 42)
            );
            AddPointRangeToGroup(frontBagStart, _model.Points.Count, _frontCoffeeBeanBagPoints, _frontCoffeeBeanBagBaseY);

            AddCoffeeMachineRefillProps();
        }

        private void AddCoffeeBeanBag(
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            Color body,
            Color bodyDark,
            Color label,
            Color beanColor)
        {
            double midX = (x1 + x2) * 0.5;

            // Нижняя часть мешка
            AddBlock(x1, y1, z1, x2, y2 - 6.0, z2, null, body, FaceLayer.Furniture);

            // Верх мешка чуть уже
            AddBlock(x1 + 1.5, y2 - 6.0, z1 + 0.7, x2 - 1.5, y2 - 1.6, z2 - 0.7, null, body, FaceLayer.Furniture);

            // Сложенный верх
            AddBlock(x1 + 2.2, y2 - 1.6, z1 + 1.4, x2 - 2.2, y2 + 1.2, z2 - 1.4, null, bodyDark, FaceLayer.SmallDetail);

            // Центральный шов / складка сверху
            AddBlock(midX - 0.8, y2 - 1.2, z1 + 1.8, midX + 0.8, y2 + 2.0, z2 - 1.8, null, bodyDark, FaceLayer.SmallDetail);

            // Лицевая этикетка (смотрит в сторону бармена, по +Z)
            AddRectOnZPlane(x1 + 4.2, y1 + 6.0, x2 - 4.2, y1 + 16.5, z2 + 0.10, label, FaceLayer.SmallDetail);

            // Маленький значок "зерно" на этикетке
            AddLabelOvalOnZPlane(z2 + 0.14, midX, y1 + 11.2, 3.0, 4.2, beanColor, FaceLayer.SmallDetail);
            AddScratchOnZPlane(midX, y1 + 8.6, midX, y1 + 13.8, z2 + 0.16, Color.FromArgb(140, 116, 84), 0.22);

            // Небольшие складки на мешке
            AddScratchOnZPlane(x1 + 3.0, y1 + 4.5, x1 + 4.8, y2 - 5.0, z2 + 0.08, bodyDark, 0.20);
            AddScratchOnZPlane(x2 - 3.0, y1 + 5.0, x2 - 4.8, y2 - 5.5, z2 + 0.08, bodyDark, 0.20);

            // Лёгкие боковые тени
            AddRectOnXPlane(x1 - 0.08, y1 + 2.0, z1 + 1.0, y2 - 3.0, z2 - 1.0, bodyDark, FaceLayer.SmallDetail);
            AddRectOnXPlane(x2 + 0.08, y1 + 2.0, z1 + 1.0, y2 - 3.0, z2 - 1.0, bodyDark, FaceLayer.SmallDetail);

            // Небольшое высветление спереди
            AddRectOnZPlane(x1 + 2.0, y1 + 2.5, x2 - 2.0, y1 + 5.0, z2 + 0.06, Color.FromArgb(
                Math.Min(body.R + 18, 255),
                Math.Min(body.G + 18, 255),
                Math.Min(body.B + 18, 255)), FaceLayer.SmallDetail);
        }

        private void AddBarArea()
        {
            AddMainBarCounter();
            AddSideBarCounter();
            AddCoffeeMachineWallCounter();

            // Касса на краю центрального блока бара
            AddCashRegister(36, -35, 398);

            // 2 мешка с кофейными зернами на свободной части блока с кассой
            AddCoffeeBeanBagsOnCashBlock();

            AddBuiltInSink(162, -35, 388);

            // Увеличенный генератор льда на стойке, ближе к бармену
            AddCounterTopIceMakerNearSink();

            // Коврик WELCOME на левом пустом блоке бара
            AddWelcomeMatOnLeftBarBlock();
            AddTiaOrderExchangeProps();
            SetTiaOrderExchangeVisible(false, false);

            AddPendantLamp(20, 420);
        }

        private void AddCounterTopIceMakerNearSink()
        {
            // Раковина: x = 162..196, z = 388..430
            // Ставим ледогенератор справа от раковины,
            // делаем его больше и подвигаем ближе к бармену (в сторону +Z).

            double x1 = 204;
            double x2 = 248;

            double z1 = 402;
            double z2 = 436;

            double y1 = -34.7;   // стоит на столешнице
            double y2 = -2.5;    // стал выше и крупнее

            Color body = Color.FromArgb(150, 154, 158);
            Color bodyDark = Color.FromArgb(86, 92, 98);
            Color bodyLight = Color.FromArgb(186, 188, 190);
            Color panelDark = Color.FromArgb(38, 42, 46);
            Color panelLight = Color.FromArgb(78, 84, 90);
            Color iceTint = Color.FromArgb(176, 196, 208);
            Color iceTint2 = Color.FromArgb(164, 186, 200);
            Color green = Color.FromArgb(68, 156, 118);
            Color amber = Color.FromArgb(178, 128, 56);

            // Основной корпус
            AddBlock(x1, y1, z1, x2, y2, z2, null, body, FaceLayer.Furniture);

            // Верхняя крышка
            AddBlock(x1 + 1.5, y2, z1 + 1.5, x2 - 1.5, y2 + 1.8, z2 - 1.5, null, bodyLight, FaceLayer.SmallDetail);

            // Верхняя панель управления ближе к бармену
            AddBlock(x1 + 4.0, y2 - 0.2, z2 - 10.0, x2 - 4.0, y2 + 2.2, z2 - 2.4, null, panelLight, FaceLayer.SmallDetail);

            // Передняя лицевая панель
            AddRectOnZPlane(x1 + 1.4, y1 + 2.2, x2 - 1.4, y2 - 1.8, z2 + 0.10, bodyDark, FaceLayer.SmallDetail);

            // Окошко контейнера льда
            AddRectOnZPlane(x1 + 7.0, y1 + 8.0, x2 - 7.0, y1 + 20.0, z2 + 0.16, panelDark, FaceLayer.SmallDetail);
            AddRectOnZPlane(x1 + 8.2, y1 + 9.2, x2 - 8.2, y1 + 18.4, z2 + 0.20, Color.FromArgb(52, 68, 80), FaceLayer.SmallDetail);

            // Лёд внутри
            AddRectOnZPlane(x1 + 9.5, y1 + 10.0, x1 + 17.5, y1 + 13.6, z2 + 0.22, iceTint, FaceLayer.SmallDetail);
            AddRectOnZPlane(x1 + 18.5, y1 + 11.0, x1 + 26.5, y1 + 15.0, z2 + 0.22, iceTint2, FaceLayer.SmallDetail);
            AddRectOnZPlane(x1 + 27.5, y1 + 9.8, x2 - 9.8, y1 + 13.4, z2 + 0.22, iceTint, FaceLayer.SmallDetail);

            // Вертикальная ручка справа
            AddBlock(x2 - 2.6, y1 + 9.0, z2 - 7.0, x2 - 1.0, y1 + 21.0, z2 + 0.4, null, bodyLight, FaceLayer.SmallDetail);

            // Индикаторы / кнопки
            AddBlock(x1 + 8.0, y2 + 0.1, z2 - 7.0, x1 + 9.6, y2 + 0.9, z2 - 5.4, null, green, FaceLayer.SmallDetail);
            AddBlock(x1 + 10.6, y2 + 0.1, z2 - 7.0, x1 + 12.2, y2 + 0.9, z2 - 5.4, null, amber, FaceLayer.SmallDetail);
            AddBlock(x1 + 13.2, y2 + 0.1, z2 - 7.0, x1 + 14.8, y2 + 0.9, z2 - 5.4, null, bodyDark, FaceLayer.SmallDetail);

            // Боковая вентиляция слева
            AddRectOnXPlane(x1 - 0.08, y1 + 5.0, z1 + 5.0, y1 + 20.0, z1 + 25.0, panelDark, FaceLayer.SmallDetail);

            for (int i = 0; i < 5; i++)
            {
                double sy1 = y1 + 6.0 + i * 2.8;
                double sy2 = sy1 + 0.8;

                AddRectOnXPlane(x1 - 0.10, sy1, z1 + 6.5, sy2, z1 + 23.5, bodyLight, FaceLayer.SmallDetail);
            }

            // Ножки
            AddBlock(x1 + 2.2, y1 - 0.8, z1 + 2.2, x1 + 4.8, y1, z1 + 4.8, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(x2 - 4.8, y1 - 0.8, z1 + 2.2, x2 - 2.2, y1, z1 + 4.8, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(x1 + 2.2, y1 - 0.8, z2 - 4.8, x1 + 4.8, y1, z2 - 2.2, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(x2 - 4.8, y1 - 0.8, z2 - 4.8, x2 - 2.2, y1, z2 - 2.2, null, bodyDark, FaceLayer.SmallDetail);
        }

        private void AddMainBarCounter()
        {
            AddBlock(-101, -100, 380, 29, -35, 450, null, HorrorColor.DeadWood, FaceLayer.Furniture);
            AddBlock(29, -100, 380, 159, -35, 450, null, HorrorColor.DeadWood, FaceLayer.Furniture);
            AddBlock(159, -100, 380, 299, -35, 450, null, HorrorColor.DeadWood, FaceLayer.Furniture);
        }

        private void AddSideBarCounter()
        {
            AddBlock(-131, -100, 380, -101, -35, 490, null, HorrorColor.DeadWood, FaceLayer.Furniture);
        }

        private void AddBuiltInIceMakerUnderCoffeeMachine()
        {
            // Более компактный встроенный ледогенератор
            // и меньше выступает из стойки, чтобы не проваливаться в текстуры.

            double faceX = 254.70;   // лицевая часть: совсем чуть-чуть выступает перед стойкой
            double bodyBackX = 282.0;

            double y1 = -92;
            double y2 = -50;

            double z1 = 492;
            double z2 = 548;

            Color steel = Color.FromArgb(156, 160, 164);
            Color steelDark = Color.FromArgb(84, 90, 96);
            Color steelLight = Color.FromArgb(188, 190, 192);
            Color black = Color.FromArgb(22, 24, 26);
            Color ventDark = Color.FromArgb(56, 60, 64);
            Color ventLight = Color.FromArgb(126, 130, 134);
            Color indicatorGreen = Color.FromArgb(62, 154, 112);
            Color indicatorAmber = Color.FromArgb(168, 124, 54);
            Color iceTint = Color.FromArgb(176, 196, 210);

            // Основной внутренний корпус
            AddBlock(255.0, y1, z1, bodyBackX, y2, z2, null, steelDark, FaceLayer.SmallDetail);

            // Наружная рамка
            AddBlock(faceX, y1 + 1.0, z1 + 1.5, 255.45, y2 - 1.0, z2 - 1.5, null, steel, FaceLayer.SmallDetail);

            // Светлая верхняя кромка
            AddBlock(faceX - 0.05, y2 - 1.7, z1 + 2.2, faceX + 0.45, y2 - 0.8, z2 - 2.2, null, steelLight, FaceLayer.SmallDetail);

            // Тёмная нижняя кромка
            AddBlock(faceX - 0.05, y1 + 0.7, z1 + 2.2, faceX + 0.45, y1 + 1.5, z2 - 2.2, null, steelDark, FaceLayer.SmallDetail);

            // Основная дверца
            double doorX1 = faceX + 0.10;
            double doorX2 = 255.20;
            double doorY1 = -89;
            double doorY2 = -53;
            double doorZ1 = 495;
            double doorZ2 = 545;

            AddBlock(doorX1, doorY1, doorZ1, doorX2, doorY2, doorZ2, null, steel, FaceLayer.SmallDetail);

            // Внутреннее углубление дверцы
            AddRectOnXPlane(doorX1 - 0.06, doorY1 + 2.2, doorZ1 + 3, doorY2 - 2.2, doorZ2 - 3, Color.FromArgb(138, 144, 148), FaceLayer.SmallDetail);

            // Верхняя светлая полоска
            AddRectOnXPlane(doorX1 - 0.08, doorY2 - 4.0, doorZ1 + 4, doorY2 - 2.8, doorZ2 - 4, steelLight, FaceLayer.SmallDetail);

            // Нижняя тень
            AddRectOnXPlane(doorX1 - 0.08, doorY1 + 2.0, doorZ1 + 4, doorY1 + 3.2, doorZ2 - 4, steelDark, FaceLayer.SmallDetail);

            // Окно / отсек со льдом
            AddRectOnXPlane(doorX1 - 0.12, -75, 505, -62, 535, black, FaceLayer.SmallDetail);
            AddRectOnXPlane(doorX1 - 0.15, -73.5, 507, -63.5, 533, Color.FromArgb(44, 56, 66), FaceLayer.SmallDetail);

            // Лёд внутри
            AddRectOnXPlane(doorX1 - 0.18, -72.0, 509, -68.5, 516, iceTint, FaceLayer.SmallDetail);
            AddRectOnXPlane(doorX1 - 0.18, -71.0, 519, -67.3, 526, Color.FromArgb(166, 188, 204), FaceLayer.SmallDetail);
            AddRectOnXPlane(doorX1 - 0.18, -72.3, 528, -68.7, 532, iceTint, FaceLayer.SmallDetail);

            // Ручка справа
            AddBlock(faceX - 1.3, -76, 538, faceX + 0.15, -61, 541.5, null, steelLight, FaceLayer.SmallDetail);
            AddBlock(faceX - 1.7, -74, 538.7, faceX - 1.1, -63, 540.8, null, steelDark, FaceLayer.SmallDetail);

            // Панель управления сверху
            AddBlock(faceX + 0.12, -53.5, 498, 255.10, -49.5, 513, null, Color.FromArgb(64, 68, 74), FaceLayer.SmallDetail);
            AddRectOnXPlane(faceX + 0.06, -52.5, 500, -51.0, 510, Color.Black, FaceLayer.SmallDetail);

            // Индикаторы
            AddBlock(faceX - 0.08, -52.1, 501.2, faceX + 0.35, -51.0, 502.4, null, indicatorGreen, FaceLayer.SmallDetail);
            AddBlock(faceX - 0.08, -52.1, 504.2, faceX + 0.35, -51.0, 505.4, null, indicatorAmber, FaceLayer.SmallDetail);
            AddBlock(faceX - 0.08, -52.1, 507.2, faceX + 0.35, -51.0, 508.4, null, Color.FromArgb(96, 100, 106), FaceLayer.SmallDetail);

            // Нижняя вентиляционная решётка
            double ventY1 = -87.5;
            double ventY2 = -80.5;
            double ventZ1 = 502;
            double ventZ2 = 538;

            AddRectOnXPlane(faceX + 0.02, ventY1, ventZ1, ventY2, ventZ2, ventDark, FaceLayer.SmallDetail);

            int ventCount = 4;
            double ventGap = (ventY2 - ventY1) / ventCount;

            for (int i = 0; i < ventCount; i++)
            {
                double sy1 = ventY1 + i * ventGap + 0.25;
                double sy2 = sy1 + 0.65;

                AddRectOnXPlane(faceX - 0.02, sy1, ventZ1 + 2.5, sy2, ventZ2 - 2.5, ventLight, FaceLayer.SmallDetail);
            }

            // Ножки / нижний цоколь
            AddBlock(faceX + 0.05, -94.0, 497, 255.20, -92.0, 501, null, steelDark, FaceLayer.SmallDetail);
            AddBlock(faceX + 0.05, -94.0, 539, 255.20, -92.0, 543, null, steelDark, FaceLayer.SmallDetail);
        }

        private void AddPendantLamp(double centerX, double centerZ)
        {
            Color cable = Color.FromArgb(28, 28, 30);
            Color shade = Color.FromArgb(118, 122, 124);
            Color bulb = Color.FromArgb(255, 244, 170);
            Color sunnyBase = Color.FromArgb(255, 250, 214);

            AddBlock(centerX - 2, 146, centerZ - 2, centerX + 2, 150, centerZ + 2, null, cable, FaceLayer.Furniture);
            AddBlock(centerX - 1, 96, centerZ - 1, centerX + 1, 146, centerZ + 1, null, cable, FaceLayer.Furniture);
            AddBlock(centerX - 24, 74, centerZ - 24, centerX + 24, 86, centerZ + 24, null, shade, FaceLayer.Furniture);
            AddBlock(centerX - 8, 67, centerZ - 8, centerX + 8, 74, centerZ + 8, null, bulb, FaceLayer.Furniture);
            AddBlock(centerX - 20, 73.4, centerZ - 20, centerX + 20, 74.2, centerZ + 20, null, sunnyBase, FaceLayer.SmallDetail);
        }

        private void AddCoffeeMachineFacingLeft(double x, double y, double z)
        {
            Color steel = Color.FromArgb(142, 146, 150);
            Color steelDark = Color.FromArgb(76, 82, 88);
            Color steelLight = Color.FromArgb(176, 178, 180);
            Color anthracite = Color.FromArgb(34, 36, 42);
            Color glass = Color.FromArgb(10, 14, 16);
            Color display = Color.FromArgb(42, 112, 92);
            Color metal = Color.FromArgb(152, 154, 156);
            Color black = Color.FromArgb(14, 16, 18);

            double xFront = x + 4;
            double xBack = x + 30;
            double z1 = z + 2;
            double z2 = z + 126;
            double frontPlane = xFront - 0.35;

            AddBlock(xFront, y + 2.0, z1, xBack, y + 30.0, z2, null, steel, FaceLayer.Furniture);
            AddBlock(xFront - 1.2, y + 0.4, z1 + 6, xBack + 1.2, y + 2.4, z2 - 6, null, steelDark, FaceLayer.Furniture);
            AddBlock(xFront + 2.5, y + 30.0, z1 + 8, xBack - 2.5, y + 37.5, z2 - 8, null, steelDark, FaceLayer.Furniture);
            AddBlock(xFront + 6.0, y + 37.5, z1 + 18, xBack - 6.0, y + 40.5, z2 - 18, null, steelLight, FaceLayer.Furniture);

            for (int i = 0; i < 4; i++)
            {
                double slotZ1 = z1 + 22 + i * 22;
                AddBlock(xFront + 8.0, y + 38.0, slotZ1, xBack - 8.0, y + 38.5, slotZ1 + 10, null, anthracite, FaceLayer.SmallDetail);
            }

            AddBlock(xFront + 5.5, y + 38.5, z1 + 16, xFront + 7.0, y + 40.2, z2 - 16, null, steelLight, FaceLayer.SmallDetail);
            AddBlock(xBack - 7.0, y + 38.5, z1 + 16, xBack - 5.5, y + 40.2, z2 - 16, null, steelLight, FaceLayer.SmallDetail);

            AddQuad(frontPlane, y + 4.0, z1 + 4, frontPlane, y + 28.8, z1 + 4, frontPlane, y + 28.8, z2 - 4, frontPlane, y + 4.0, z2 - 4, null, steelLight, FaceLayer.SmallDetail);
            AddQuad(frontPlane - 0.08, y + 18.5, z1 + 12, frontPlane - 0.08, y + 26.0, z1 + 12, frontPlane - 0.08, y + 26.0, z2 - 12, frontPlane - 0.08, y + 18.5, z2 - 12, null, glass, FaceLayer.SmallDetail);
            AddQuad(frontPlane - 0.14, y + 21.0, z1 + 48, frontPlane - 0.14, y + 24.0, z1 + 48, frontPlane - 0.14, y + 24.0, z2 - 48, frontPlane - 0.14, y + 21.0, z2 - 48, null, display, FaceLayer.SmallDetail);

            for (int i = 0; i < 3; i++)
            {
                double leftButtonZ = z1 + 18 + i * 10;
                double rightButtonZ = z2 - 23 - i * 10;

                AddQuad(frontPlane - 0.12, y + 19.8, leftButtonZ, frontPlane - 0.12, y + 21.4, leftButtonZ, frontPlane - 0.12, y + 21.4, leftButtonZ + 5, frontPlane - 0.12, y + 19.8, leftButtonZ + 5, null, steelLight, FaceLayer.SmallDetail);
                AddQuad(frontPlane - 0.12, y + 19.8, rightButtonZ, frontPlane - 0.12, y + 21.4, rightButtonZ, frontPlane - 0.12, y + 21.4, rightButtonZ + 5, frontPlane - 0.12, y + 19.8, rightButtonZ + 5, null, steelLight, FaceLayer.SmallDetail);
            }

            AddQuad(frontPlane - 0.06, y + 17.2, z1 + 10, frontPlane - 0.06, y + 17.8, z1 + 10, frontPlane - 0.06, y + 17.8, z2 - 10, frontPlane - 0.06, y + 17.2, z2 - 10, null, steelDark, FaceLayer.SmallDetail);

            // Передние чёрные блоки и вертикальные серые детали делаем компактнее,
            // сохраняя их верхнее выравнивание.
            AddBlock(xFront - 3.4, y + 14.1, z1 + 27.0, xFront + 0.1, y + 17.0, z1 + 38.0, null, anthracite, FaceLayer.SmallDetail);
            AddBlock(xFront - 3.4, y + 14.1, z2 - 38.0, xFront + 0.1, y + 17.0, z2 - 27.0, null, anthracite, FaceLayer.SmallDetail);

            AddBlock(xFront - 5.0, y + 10.9, z1 + 30.0, xFront - 3.8, y + 15.8, z1 + 31.3, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 5.0, y + 10.9, z1 + 33.7, xFront - 3.8, y + 15.8, z1 + 35.0, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 5.0, y + 10.9, z2 - 35.0, xFront - 3.8, y + 15.8, z2 - 33.7, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 5.0, y + 10.9, z2 - 31.3, xFront - 3.8, y + 15.8, z2 - 30.0, null, metal, FaceLayer.SmallDetail);

            AddBlock(xFront - 2.2, y + 4.0, z1 + 16, xFront + 7.0, y + 6.4, z2 - 16, null, Color.FromArgb(32, 34, 38), FaceLayer.SmallDetail);
            AddBlock(xFront - 2.6, y + 3.2, z1 + 16, xFront - 1.6, y + 6.4, z2 - 16, null, black, FaceLayer.SmallDetail);
            AddBlock(xFront - 1.2, y + 5.05, z1 + 21, xFront + 5.8, y + 5.35, z2 - 21, null, steelDark, FaceLayer.SmallDetail);

            AddBlock(xFront - 6.4, y + 9.5, z1 + 10, xFront - 5.0, y + 18.0, z1 + 11.4, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 8.2, y + 8.8, z1 + 10.0, xFront - 6.4, y + 9.9, z1 + 11.2, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 6.4, y + 9.5, z2 - 11.4, xFront - 5.0, y + 18.0, z2 - 10.0, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 8.2, y + 8.8, z2 - 11.2, xFront - 6.4, y + 9.9, z2 - 10.0, null, metal, FaceLayer.SmallDetail);

            AddBlock(xFront + 1.0, y, z1 + 10, xFront + 4.0, y + 1.0, z1 + 15, null, anthracite, FaceLayer.SmallDetail);
            AddBlock(xFront + 1.0, y, z2 - 15, xFront + 4.0, y + 1.0, z2 - 10, null, anthracite, FaceLayer.SmallDetail);
            AddBlock(xBack - 4.0, y, z1 + 10, xBack - 1.0, y + 1.0, z1 + 15, null, anthracite, FaceLayer.SmallDetail);
            AddBlock(xBack - 4.0, y, z2 - 15, xBack - 1.0, y + 1.0, z2 - 10, null, anthracite, FaceLayer.SmallDetail);

            AddCoffeeMachineBrewingCup(xFront - 3.0, y + 5.35, z1 + 33.5);
        }

        private void AddCoffeeMachineBrewingCup(double x, double y, double z)
        {
            Color cupBody = Color.FromArgb(144, 132, 118);
            Color coffee = Color.FromArgb(119, 73, 47);

            int cupStart = _model.Points.Count;
            AddPaperCup(x, y, z, cupBody);
            AddPointRangeToGroup(cupStart, _model.Points.Count, _coffeeMachineCupPoints, _coffeeMachineCupBaseY);

            int coffeeStart = _model.Points.Count;
            _coffeeMachineCupCoffeeBottomY = y + 0.35;
            _coffeeMachineCupCoffeeTopY = y + 4.65;
            AddBlock(x - 2.35, _coffeeMachineCupCoffeeBottomY, z - 1.55, x + 2.35, _coffeeMachineCupCoffeeTopY, z + 1.55, null, coffee, FaceLayer.SmallDetail);
            AddPointRangeToGroup(coffeeStart, _model.Points.Count, _coffeeMachineCupCoffeePoints, _coffeeMachineCupCoffeeBaseY);
            _coffeeMachineCupCoffeeTopPoints.Add(coffeeStart + 4);
            _coffeeMachineCupCoffeeTopPoints.Add(coffeeStart + 5);
            _coffeeMachineCupCoffeeTopPoints.Add(coffeeStart + 6);
            _coffeeMachineCupCoffeeTopPoints.Add(coffeeStart + 7);
        }

        private void AddCoffeeMachineRefillProps()
        {
            // Для анимации используем тот же тип мешка, что и на баре.
            int bagStart = _model.Points.Count;
            AddCoffeeBeanBag(
                240.0, 6.0, 490.0,
                262.0, 30.2, 502.0,
                Color.FromArgb(88, 64, 44),
                Color.FromArgb(66, 46, 30),
                Color.FromArgb(212, 198, 170),
                Color.FromArgb(92, 66, 42)
            );
            AddPointRangeToTransformGroup(bagStart, _model.Points.Count, _coffeeRefillBagPoints, _coffeeRefillBagBaseX, _coffeeRefillBagBaseY, _coffeeRefillBagBaseZ);

            // ВРЕМЕННО: очень большой запас крупных зёрен для отладки видимости потока.
            // Зёрна создаются заранее, а во время анимации выносятся в широкую внешнюю полосу
            // перед мешком, чтобы они не прятались внутри него или за кофемашиной.
            for (int xi = 0; xi < 8; xi++)
            {
                for (int zi = 0; zi < 5; zi++)
                {
                    for (int yi = 0; yi < 3; yi++)
                    {
                        double bx = 250.0 + xi * 0.75 + (zi % 2) * 0.22;
                        double by = 25.0 + yi * 1.10 + (xi % 2) * 0.12;
                        double bz = 488.0 + zi * 0.76 + yi * 0.10;
                        AddCoffeeBeanCluster(bx, by, bz);
                    }
                }
            }
        }

        private void AddCoffeeBeanCluster(double x, double y, double z)
        {
            int start = _model.Points.Count;
            Color bean = Color.FromArgb(120, 72, 40);
            Color beanMid = Color.FromArgb(156, 98, 56);
            Color beanLight = Color.FromArgb(194, 134, 82);

            // ВРЕМЕННО крупный и контрастный кластер: нужен не финальный реализм, а уверенная видимость.
            AddBlock(x - 1.05, y, z - 0.62, x + 1.05, y + 1.25, z + 0.62, null, bean, FaceLayer.SmallDetail);
            AddBlock(x - 0.48, y + 1.10, z - 0.34, x + 0.48, y + 1.92, z + 0.34, null, beanMid, FaceLayer.SmallDetail);
            AddBlock(x - 0.24, y + 1.95, z - 0.20, x + 0.24, y + 2.35, z + 0.20, null, beanLight, FaceLayer.SmallDetail);
            AddPointRangeToTransformGroup(start, _model.Points.Count, _coffeeRefillBeansPoints, _coffeeRefillBeansBaseX, _coffeeRefillBeansBaseY, _coffeeRefillBeansBaseZ);
        }

        private void AddCashRegister(double x, double y, double z)
        {
            Color bodyDark = Color.FromArgb(34, 34, 40);
            Color body = Color.FromArgb(50, 54, 62);
            Color bodyLight = Color.FromArgb(120, 124, 130);
            Color accent = Color.FromArgb(96, 68, 54);
            Color screenBezel = Color.FromArgb(16, 18, 22);
            Color keyboardBase = Color.FromArgb(132, 132, 130);
            Color keyLight = Color.FromArgb(184, 184, 176);
            Color keyMid = Color.FromArgb(112, 112, 110);
            Color printerBody = Color.FromArgb(142, 142, 138);
            Color printerDark = Color.FromArgb(66, 70, 74);
            Color terminalBody = Color.FromArgb(36, 38, 44);

            // Касса и чековый аппарат поменяны местами:
            // чековый аппарат теперь слева, касса с экраном — справа.
            double printerX = x;
            double cashX = x + 28;

            AddBlock(cashX, y, z, cashX + 56, y + 11, z + 34, null, bodyDark, FaceLayer.Furniture);
            AddBlock(cashX + 2, y + 11, z + 2, cashX + 54, y + 16, z + 32, null, body, FaceLayer.Furniture);
            AddBlock(cashX + 5, y + 16, z + 5, cashX + 31, y + 18.5, z + 27, null, keyboardBase, FaceLayer.Furniture);

            AddQuad(cashX + 3, y + 6.3, z + 34.15, cashX + 53, y + 6.3, z + 34.15, cashX + 53, y + 7.2, z + 34.15, cashX + 3, y + 7.2, z + 34.15, null, accent, FaceLayer.SmallDetail);
            AddBlock(cashX + 23, y + 4.2, z + 34.05, cashX + 33, y + 6.2, z + 34.8, null, accent, FaceLayer.SmallDetail);
            AddBlock(cashX + 46, y + 11, z + 6, cashX + 54, y + 15, z + 28, null, bodyLight, FaceLayer.SmallDetail);

            AddCashKeyboard(cashX, y, z, keyLight, keyMid);

            AddBlock(cashX + 36, y + 16, z + 14, cashX + 42, y + 28, z + 20, null, bodyDark, FaceLayer.Furniture);
            AddBlock(cashX + 34, y + 27, z + 12, cashX + 44, y + 30, z + 22, null, bodyLight, FaceLayer.SmallDetail);
            AddBlock(cashX + 28, y + 30, z + 8, cashX + 52, y + 47, z + 26, null, bodyLight, FaceLayer.Furniture);
            AddBlock(cashX + 29, y + 46.2, z + 9, cashX + 51, y + 47.2, z + 25, null, accent, FaceLayer.SmallDetail);

            AddQuad(cashX + 30, y + 32, z + 26.2, cashX + 50, y + 32, z + 26.2, cashX + 50, y + 45, z + 26.2, cashX + 30, y + 45, z + 26.2, null, screenBezel, FaceLayer.SmallDetail);
            AddQuad(cashX + 32, y + 34, z + 26.35, cashX + 48, y + 34, z + 26.35, cashX + 48, y + 43, z + 26.35, cashX + 32, y + 43, z + 26.35, null, Color.Black, FaceLayer.SmallDetail);

            AddCashRecipeScreenContent(cashX, y, z);
            SetCashRecipeScreenVisible(false);

            AddBlock(printerX, y, z + 8, printerX + 24, y + 12, z + 28, null, printerBody, FaceLayer.Furniture);
            AddBlock(printerX + 2, y + 12, z + 10, printerX + 22, y + 15, z + 26, null, printerBody, FaceLayer.Furniture);
            AddQuad(printerX + 1, y + 4.5, z + 28.15, printerX + 23, y + 4.5, z + 28.15, printerX + 23, y + 10.2, z + 28.15, printerX + 1, y + 10.2, z + 28.15, null, printerDark, FaceLayer.SmallDetail);
            AddBlock(printerX + 6.0, y + 14.2, z + 16.8, printerX + 18.0, y + 14.8, z + 19.2, null, printerDark, FaceLayer.SmallDetail);
            AddBlock(printerX + 6.8, y + 14.9, z + 17.1, printerX + 17.2, y + 20.5, z + 18.9, null, HorrorColor.Paper, FaceLayer.SmallDetail);
            AddBlock(printerX + 19.2, y + 15.0, z + 22.2, printerX + 21.0, y + 15.8, z + 24.0, null, HorrorColor.RedGlow, FaceLayer.SmallDetail);

            AddBlock(cashX + 11, y + 18.2, z - 8, cashX + 22, y + 20.2, z + 1, null, terminalBody, FaceLayer.SmallDetail);
            AddBlock(cashX + 14, y + 20.2, z - 6.5, cashX + 19, y + 26.5, z - 1.5, null, terminalBody, FaceLayer.SmallDetail);
            AddQuad(cashX + 14.6, y + 21.8, z - 1.35, cashX + 18.4, y + 21.8, z - 1.35, cashX + 18.4, y + 25.0, z - 1.35, cashX + 14.6, y + 25.0, z - 1.35, null, Color.Black, FaceLayer.SmallDetail);

            AddBlock(cashX + 3, y - 0.2, z + 3, cashX + 7, y + 0.8, z + 7, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(cashX + 49, y - 0.2, z + 3, cashX + 53, y + 0.8, z + 7, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(cashX + 3, y - 0.2, z + 27, cashX + 7, y + 0.8, z + 31, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(cashX + 49, y - 0.2, z + 27, cashX + 53, y + 0.8, z + 31, null, bodyDark, FaceLayer.SmallDetail);
        }

        private void AddCashRecipeScreenContent(double x, double y, double z)
        {
            int startIndex = _model.Points.Count;

            double screenZ = z + 26.42;

            Color blue = Color.FromArgb(112, 206, 238);
            Color blueDark = Color.FromArgb(58, 136, 174);
            Color blueLight = Color.FromArgb(168, 228, 248);
            Color outline = Color.FromArgb(70, 98, 112);
            Color lid = Color.FromArgb(224, 232, 236);
            Color straw = Color.FromArgb(233, 106, 156);
            Color coffee = Color.FromArgb(119, 73, 47);
            Color milk = Color.FromArgb(245, 228, 211);
            Color raspberry = Color.FromArgb(202, 54, 106);
            Color cupBody = Color.FromArgb(190, 234, 224, 210);
            Color sleeve = Color.FromArgb(164, 108, 78);

            AddQuad(x + 32.1, y + 34.1, screenZ, x + 47.9, y + 34.1, screenZ, x + 47.9, y + 42.9, screenZ, x + 32.1, y + 42.9, screenZ, null, blue, FaceLayer.SmallDetail);
            AddQuad(x + 32.1, y + 42.2, screenZ + 0.02, x + 47.9, y + 42.2, screenZ + 0.02, x + 47.9, y + 42.9, screenZ + 0.02, x + 32.1, y + 42.9, screenZ + 0.02, null, blueLight, FaceLayer.SmallDetail);
            AddQuad(x + 32.1, y + 34.1, screenZ + 0.02, x + 47.9, y + 34.1, screenZ + 0.02, x + 47.9, y + 34.7, screenZ + 0.02, x + 32.1, y + 34.7, screenZ + 0.02, null, blueDark, FaceLayer.SmallDetail);

            // Крупный стакан на экране кассы: оставляем только силуэт напитка и крышку.
            // Нижняя граница силуэта заканчивается на кофейном слое — без дополнительного тёмного основания,
            // без боковых тёмных стенок и без отдельного прямоугольного подложечного блока.
            AddQuad(x + 37.72, y + 40.00, screenZ + 0.08, x + 42.28, y + 40.00, screenZ + 0.08, x + 42.02, y + 40.60, screenZ + 0.08, x + 37.98, y + 40.60, screenZ + 0.08, null, lid, FaceLayer.SmallDetail);
            AddQuad(x + 39.05, y + 40.58, screenZ + 0.09, x + 40.95, y + 40.58, screenZ + 0.09, x + 40.82, y + 40.86, screenZ + 0.09, x + 39.18, y + 40.86, screenZ + 0.09, null, lid, FaceLayer.SmallDetail);
            AddQuad(x + 40.12, y + 40.72, screenZ + 0.10, x + 40.46, y + 40.72, screenZ + 0.10, x + 40.28, y + 41.95, screenZ + 0.10, x + 39.94, y + 41.95, screenZ + 0.10, null, straw, FaceLayer.SmallDetail);
            AddQuad(x + 38.08, y + 35.48, screenZ + 0.11, x + 41.92, y + 35.48, screenZ + 0.11, x + 41.68, y + 36.86, screenZ + 0.11, x + 38.32, y + 36.86, screenZ + 0.11, null, coffee, FaceLayer.SmallDetail);
            AddQuad(x + 38.12, y + 36.86, screenZ + 0.11, x + 41.88, y + 36.86, screenZ + 0.11, x + 42.04, y + 39.02, screenZ + 0.11, x + 37.96, y + 39.02, screenZ + 0.11, null, milk, FaceLayer.SmallDetail);
            AddQuad(x + 37.96, y + 39.02, screenZ + 0.11, x + 42.04, y + 39.02, screenZ + 0.11, x + 42.14, y + 39.86, screenZ + 0.11, x + 37.86, y + 39.86, screenZ + 0.11, null, raspberry, FaceLayer.SmallDetail);

            AddPointRangeToGroup(startIndex, _model.Points.Count, _cashRecipeScreenPoints, _cashRecipeScreenBaseY);
        }

        private void AddCashKeyboard(double x, double y, double z, Color keyLight, Color keyMid)
        {
            double keyStartX = x + 7;
            double keyStartZ = z + 8;
            double keyW = 4.0;
            double keyD = 3.2;
            double gapX = 1.3;
            double gapZ = 1.8;

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    double kx1 = keyStartX + col * (keyW + gapX);
                    double kz1 = keyStartZ + row * (keyD + gapZ);
                    Color keyColor = (row == 3 && col >= 2) ? keyMid : keyLight;
                    AddBlock(kx1, y + 18.5, kz1, kx1 + keyW, y + 19.6, kz1 + keyD, null, keyColor, FaceLayer.SmallDetail);
                }
            }

            AddBlock(x + 25, y + 18.5, z + 9, x + 29, y + 19.7, z + 13, null, Color.FromArgb(92, 122, 84), FaceLayer.SmallDetail);
            AddBlock(x + 25, y + 18.5, z + 15, x + 29, y + 19.7, z + 19, null, Color.FromArgb(142, 112, 58), FaceLayer.SmallDetail);
            AddBlock(x + 25, y + 18.5, z + 21, x + 29, y + 19.7, z + 25, null, HorrorColor.DriedBlood, FaceLayer.SmallDetail);
        }

        private void AddBuiltInSink(double x, double y, double z)
        {
            Color steel = Color.FromArgb(134, 138, 142);
            Color steelDark = Color.FromArgb(72, 78, 82);
            Color basinDark = Color.FromArgb(28, 34, 38);
            Color faucet = Color.FromArgb(158, 160, 164);

            AddBlock(x, y + 0.4, z, x + 34, y + 2.0, z + 42, null, steel, FaceLayer.Furniture);

            AddBlock(x + 2, y + 2.0, z + 2, x + 32, y + 4.8, z + 7, null, steelDark, FaceLayer.SmallDetail);
            AddBlock(x + 2, y + 2.0, z + 35, x + 32, y + 4.8, z + 40, null, steelDark, FaceLayer.SmallDetail);
            AddBlock(x + 2, y + 2.0, z + 7, x + 7, y + 4.8, z + 35, null, steelDark, FaceLayer.SmallDetail);
            AddBlock(x + 27, y + 2.0, z + 7, x + 32, y + 4.8, z + 35, null, steelDark, FaceLayer.SmallDetail);

            AddQuad(x + 7, y + 2.1, z + 7, x + 27, y + 2.1, z + 7, x + 27, y + 2.1, z + 35, x + 7, y + 2.1, z + 35, null, basinDark, FaceLayer.SmallDetail);
            AddQuad(x + 9, y + 2.2, z + 11, x + 25, y + 2.2, z + 11, x + 25, y + 2.2, z + 31, x + 9, y + 2.2, z + 31, null, Color.FromArgb(20, 32, 64), FaceLayer.SmallDetail);
            AddQuad(x + 12, y + 2.24, z + 15, x + 22, y + 2.24, z + 15, x + 22, y + 2.24, z + 27, x + 12, y + 2.24, z + 27, null, Color.FromArgb(34, 52, 92), FaceLayer.SmallDetail);

            int foamStart = _model.Points.Count;
            Color foam = Color.FromArgb(214, 236, 240, 245);
            Color foamMid = Color.FromArgb(226, 244, 247, 250);
            Color foamBright = Color.FromArgb(236, 252, 253, 254);
            Color foamShadow = Color.FromArgb(188, 224, 231, 238);

            // Более детализированная и объёмная пена: несколько больших масс,
            // поверх которых лежат меньшие пузырьковые "шапки" и гребни.
            AddBlock(x + 10.0, y + 2.20, z + 14.2, x + 14.2, y + 4.65, z + 17.8, null, foamShadow, FaceLayer.SmallDetail);
            AddBlock(x + 12.6, y + 2.26, z + 14.8, x + 17.1, y + 5.05, z + 19.2, null, foam, FaceLayer.SmallDetail);
            AddBlock(x + 16.0, y + 2.24, z + 15.1, x + 21.4, y + 5.35, z + 19.4, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 18.8, y + 2.20, z + 14.6, x + 22.4, y + 4.70, z + 18.1, null, foamMid, FaceLayer.SmallDetail);

            AddBlock(x + 10.8, y + 2.22, z + 19.1, x + 15.6, y + 4.98, z + 22.8, null, foamMid, FaceLayer.SmallDetail);
            AddBlock(x + 14.3, y + 2.28, z + 18.6, x + 19.1, y + 5.45, z + 23.4, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 18.2, y + 2.22, z + 19.6, x + 22.5, y + 4.82, z + 23.8, null, foam, FaceLayer.SmallDetail);

            AddBlock(x + 11.5, y + 2.22, z + 22.9, x + 16.5, y + 5.10, z + 26.2, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 15.6, y + 2.18, z + 23.6, x + 20.8, y + 4.78, z + 27.8, null, foamMid, FaceLayer.SmallDetail);
            AddBlock(x + 19.2, y + 2.18, z + 22.8, x + 22.4, y + 4.35, z + 25.4, null, foamShadow, FaceLayer.SmallDetail);

            // Мелкие пузырьковые выступы сверху.
            AddBlock(x + 11.0, y + 4.55, z + 15.0, x + 12.5, y + 5.70, z + 16.4, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 13.0, y + 4.85, z + 16.1, x + 14.6, y + 6.00, z + 17.5, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 16.0, y + 4.88, z + 17.4, x + 17.8, y + 6.15, z + 19.0, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 18.6, y + 4.75, z + 16.4, x + 20.4, y + 5.86, z + 18.0, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 12.2, y + 4.70, z + 20.1, x + 13.8, y + 5.85, z + 21.4, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 15.0, y + 5.15, z + 20.2, x + 16.8, y + 6.30, z + 21.8, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 17.8, y + 4.95, z + 21.1, x + 19.6, y + 6.05, z + 22.7, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 12.7, y + 4.80, z + 24.1, x + 14.5, y + 5.95, z + 25.5, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 16.2, y + 4.42, z + 25.0, x + 18.2, y + 5.50, z + 26.5, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 18.8, y + 4.18, z + 23.9, x + 20.1, y + 5.05, z + 25.1, null, foamBright, FaceLayer.SmallDetail);

            // Небольшие гребни/полосы на поверхности пены.
            AddBlock(x + 11.8, y + 3.95, z + 18.4, x + 14.8, y + 4.45, z + 18.9, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 15.8, y + 4.25, z + 22.9, x + 19.3, y + 4.72, z + 23.4, null, foamBright, FaceLayer.SmallDetail);
            AddBlock(x + 13.8, y + 3.78, z + 26.0, x + 18.8, y + 4.28, z + 26.4, null, foamBright, FaceLayer.SmallDetail);

            AddPointRangeToTransformGroup(foamStart, _model.Points.Count, _sinkWashFoamPoints, _sinkWashFoamBaseX, _sinkWashFoamBaseY, _sinkWashFoamBaseZ);

            int waterStart = _model.Points.Count;
            Color water = Color.FromArgb(122, 170, 214, 236);
            Color waterBright = Color.FromArgb(152, 208, 234, 248);
            Color waterCore = Color.FromArgb(182, 232, 246, 252);

            // Несколько тонких струй воды именно из носика крана (правый выпуск крана над чашей).
            AddBlock(x + 19.00, y + 3.1, z + 18.80, x + 19.35, y + 12.9, z + 19.12, null, waterBright, FaceLayer.SmallDetail);
            AddBlock(x + 19.38, y + 3.0, z + 18.96, x + 19.74, y + 13.2, z + 19.26, null, waterCore, FaceLayer.SmallDetail);
            AddBlock(x + 19.78, y + 3.2, z + 19.06, x + 20.08, y + 12.7, z + 19.34, null, water, FaceLayer.SmallDetail);
            AddBlock(x + 20.12, y + 3.4, z + 19.14, x + 20.38, y + 12.3, z + 19.38, null, waterBright, FaceLayer.SmallDetail);

            // Брызги и небольшое водяное пятно прямо под носиком.
            AddBlock(x + 18.8, y + 2.38, z + 18.6, x + 20.8, y + 2.78, z + 20.3, null, waterBright, FaceLayer.SmallDetail);
            AddBlock(x + 18.5, y + 2.55, z + 17.9, x + 19.2, y + 3.30, z + 18.6, null, waterCore, FaceLayer.SmallDetail);
            AddBlock(x + 20.2, y + 2.50, z + 18.3, x + 20.9, y + 3.25, z + 19.0, null, waterCore, FaceLayer.SmallDetail);
            AddBlock(x + 19.3, y + 2.50, z + 19.9, x + 20.0, y + 3.08, z + 20.6, null, waterBright, FaceLayer.SmallDetail);
            AddPointRangeToTransformGroup(waterStart, _model.Points.Count, _sinkWashWaterPoints, _sinkWashWaterBaseX, _sinkWashWaterBaseY, _sinkWashWaterBaseZ);

            AddBlock(x + 14, y + 2.0, z + 6, x + 17, y + 18.0, z + 9, null, faucet, FaceLayer.Furniture);
            AddBlock(x + 10, y + 16.0, z + 6, x + 21, y + 18.0, z + 21, null, faucet, FaceLayer.Furniture);
            AddBlock(x + 18, y + 12.0, z + 18, x + 21, y + 16.0, z + 21, null, faucet, FaceLayer.SmallDetail);
        }

        private void AddWelcomeMatOnLeftBarBlock()
        {
            double y = -34.72;

            // Левый свободный блок бара: x от -101 до 29, z от 380 до 450.
            // Коврик чуть меньше столешницы, чтобы не залезал на края.
            double x1 = -92;
            double x2 = 20;
            double z1 = 389;
            double z2 = 441;

            Color border = Color.FromArgb(88, 52, 34);
            Color baseColor = Color.FromArgb(58, 34, 24);
            Color inner = Color.FromArgb(72, 44, 30);

            Color textMain = Color.FromArgb(224, 218, 198);
            Color textOutline = Color.FromArgb(128, 18, 18);

            AddMatRect(x1, z1, x2, z2, y, border);
            AddMatRect(x1 + 4, z1 + 4, x2 - 4, z2 - 4, y + 0.02, baseColor);
            AddMatRect(x1 + 11, z1 + 10, x2 - 11, z2 - 10, y + 0.04, inner);

            AddMatTextOnYPlane(
    (x1 + x2) * 0.5,
    (z1 + z2) * 0.5,
    "WELCOME",
    y,
    textMain,
    textOutline
);
        }

        private void AddTiaOrderExchangeProps()
        {
            // Стоит на коврике WELCOME на верхней плоскости барной стойки, рядом с Тией.
            // Координаты коврика: примерно X -92..20, Z 389..441, Y -34.72.
            double cupX = 6.0;
            double cupY = -33.95;
            double cupZ = 414.0;

            int cupStart = _model.Points.Count;
            AddPaperCup(cupX, cupY, cupZ, Color.FromArgb(144, 132, 118));

            // Внутри стакана видно светло-малиновую поверхность напитка.
            Color raspberryFill = Color.FromArgb(231, 188, 206);
            Color raspberryFillDark = Color.FromArgb(214, 154, 178);
            AddBlock(cupX - 2.25, cupY + 4.85, cupZ - 1.45, cupX + 2.25, cupY + 6.00, cupZ + 1.45,
                null, raspberryFillDark, FaceLayer.SmallDetail);
            AddBlock(cupX - 2.00, cupY + 6.02, cupZ - 1.30, cupX + 2.00, cupY + 6.16, cupZ + 1.30,
                null, raspberryFill, FaceLayer.SmallDetail);

            // Непрозрачная белая крышка поверх стакана.
            Color lidWhite = Color.FromArgb(255, 255, 255);
            Color lidShadow = Color.FromArgb(228, 228, 222);
            AddBlock(cupX - 4.2, cupY + 6.35, cupZ - 3.0, cupX + 4.2, cupY + 7.10, cupZ + 3.0,
                null, lidWhite, FaceLayer.SmallDetail);
            AddBlock(cupX - 2.3, cupY + 7.10, cupZ - 1.55, cupX + 2.3, cupY + 7.70, cupZ + 1.55,
                null, lidWhite, FaceLayer.SmallDetail);
            AddBlock(cupX - 3.7, cupY + 6.25, cupZ - 2.65, cupX + 3.7, cupY + 6.42, cupZ + 2.65,
                null, lidShadow, FaceLayer.SmallDetail);

            AddPointRangeToGroup(cupStart, _model.Points.Count, _tiaServedCupPoints, _tiaServedCupBaseY);

            int billStart = _model.Points.Count;

            // Купюра стала примерно в 5 раз меньше прежней большой плашки:
            // прежний размер был около 28x22, теперь около 6x4.4.
            double billCenterX = 5.8;
            double billCenterZ = 414.0;
            double billW = 6.0;
            double billD = 4.4;
            double billY = -34.22;
            double billX1 = billCenterX - billW * 0.5;
            double billX2 = billCenterX + billW * 0.5;
            double billZ1 = billCenterZ - billD * 0.5;
            double billZ2 = billCenterZ + billD * 0.5;

            Color billGreen = Color.FromArgb(48, 138, 72);
            Color billLight = Color.FromArgb(118, 208, 124);
            Color billDark = Color.FromArgb(24, 96, 46);
            Color billInk = Color.FromArgb(236, 250, 222);

            AddBlock(billX1, billY, billZ1, billX2, billY + 0.12, billZ2, null, billGreen, FaceLayer.SmallDetail);
            AddBlock(billX1 + 0.25, billY + 0.13, billZ1 + 0.25, billX2 - 0.25, billY + 0.20, billZ2 - 0.25, null, billLight, FaceLayer.SmallDetail);
            AddBlock(billX1 + 0.55, billY + 0.21, billZ1 + 0.55, billX1 + 1.25, billY + 0.30, billZ1 + 1.05, null, billDark, FaceLayer.SmallDetail);
            AddBlock(billX2 - 1.25, billY + 0.21, billZ2 - 1.05, billX2 - 0.55, billY + 0.30, billZ2 - 0.55, null, billDark, FaceLayer.SmallDetail);

            // Надпись "300р" на маленькой купюре.
            // Пишем её простыми светлыми штрихами, чтобы читалась как номинал оплаты.
            double textY = billY + 0.34;
            double baseX = billX1 + 1.15;
            double baseZ = billZ1 + 1.00;
            double stroke = 0.18;
            double digitH = 2.05;
            double digitW = 0.72;
            double gap = 0.30;

            // 3
            AddBlock(baseX, textY, baseZ + digitH - stroke, baseX + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(baseX, textY, baseZ + digitH * 0.5 - stroke * 0.5, baseX + digitW, textY + 0.08, baseZ + digitH * 0.5 + stroke * 0.5, null, billInk, FaceLayer.SmallDetail);
            AddBlock(baseX, textY, baseZ, baseX + digitW, textY + 0.08, baseZ + stroke, null, billInk, FaceLayer.SmallDetail);
            AddBlock(baseX + digitW - stroke, textY, baseZ, baseX + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);

            // 0
            double x0 = baseX + digitW + gap;
            AddBlock(x0, textY, baseZ, x0 + stroke, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(x0 + digitW - stroke, textY, baseZ, x0 + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(x0, textY, baseZ + digitH - stroke, x0 + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(x0, textY, baseZ, x0 + digitW, textY + 0.08, baseZ + stroke, null, billInk, FaceLayer.SmallDetail);

            // 0
            double x1 = x0 + digitW + gap;
            AddBlock(x1, textY, baseZ, x1 + stroke, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(x1 + digitW - stroke, textY, baseZ, x1 + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(x1, textY, baseZ + digitH - stroke, x1 + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(x1, textY, baseZ, x1 + digitW, textY + 0.08, baseZ + stroke, null, billInk, FaceLayer.SmallDetail);

            // р
            double xr = x1 + digitW + gap;
            AddBlock(xr, textY, baseZ, xr + stroke, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(xr, textY, baseZ + digitH - stroke, xr + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(xr + digitW - stroke, textY, baseZ + digitH * 0.48, xr + digitW, textY + 0.08, baseZ + digitH, null, billInk, FaceLayer.SmallDetail);
            AddBlock(xr, textY, baseZ + digitH * 0.48 - stroke * 0.5, xr + digitW, textY + 0.08, baseZ + digitH * 0.48 + stroke * 0.5, null, billInk, FaceLayer.SmallDetail);

            AddPointRangeToGroup(billStart, _model.Points.Count, _tiaPaymentBillPoints, _tiaPaymentBillBaseY);
        }

        private void AddTablesAndChairs()
        {
            AddCafeTable(-185, -115, 115, 185);
            AddCafeTable(115, 185, 115, 185);

            AddChairFrontLeft(-240, 130);
            AddChairBackLeft(-170, 195);
            AddChairRight(195, 130);

            AddTableTopOcclusionMask(-185, -115, 115, 185);
            AddTableTopOcclusionMask(115, 185, 115, 185);
        }

        private void AddCafeTable(double x1, double x2, double z1, double z2)
        {
            Color wood = HorrorColor.WornFurniture;

            // Столешница С КОНТУРОМ
            AddBlock(x1, -45, z1, x2, -40, z2, null, wood, FaceLayer.Furniture);

            double legTop = -44.95;

            // Ножки БЕЗ КОНТУРА
            AddBlock(x1 + 5, -100, z2 - 10, x1 + 10, legTop, z2 - 5, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x2 - 10, -100, z2 - 10, x2 - 5, legTop, z2 - 5, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x1 + 5, -100, z1 + 5, x1 + 10, legTop, z1 + 10, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x2 - 10, -100, z1 + 5, x2 - 5, legTop, z1 + 10, "no_outline", wood, FaceLayer.Furniture);

            AddScratchOnYPlane(x1 + 18, z1 + 16, x1 + 34, z1 + 24, -39.68, HorrorColor.DustScratch, 0.12);
            AddScratchOnYPlane(x2 - 35, z2 - 18, x2 - 22, z2 - 10, -39.68, HorrorColor.DarkScratch, 0.10);
        }

        private void AddTableTopOcclusionMask(double x1, double x2, double z1, double z2)
        {
            Color wood = HorrorColor.WornFurniture;

            // Чуть выше настоящей столешницы.
            // Это не новый стол, а тонкая верхняя "крышка",
            // которая перекрывает просвечивающие грани стула.
            double y = -39.52;

            AddRectOnYPlane(
                x1,
                z1,
                x2,
                z2,
                y,
                wood,
                FaceLayer.SmallDetail
            );

            // Возвращаем лёгкие царапины сверху, чтобы маска не выглядела плоской.
            AddScratchOnYPlane(x1 + 18, z1 + 16, x1 + 34, z1 + 24, y + 0.02, HorrorColor.DustScratch, 0.12);
            AddScratchOnYPlane(x2 - 35, z2 - 18, x2 - 22, z2 - 10, y + 0.03, HorrorColor.DarkScratch, 0.10);
        }
        private void AddChairFrontLeft(double x, double z)
        {
            Color wood = HorrorColor.WornFurniture;

            // Сиденье и спинка С КОНТУРОМ
            AddBlock(x, -60, z, x + 45, -55, z + 40, null, wood, FaceLayer.Furniture);
            AddBlock(x, -25, z + 10, x + 5, -5, z + 30, null, wood, FaceLayer.Furniture);

            // Ножки БЕЗ КОНТУРА
            AddBlock(x, -100, z + 30, x + 5, 0, z + 40, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x, -100, z, x + 5, 0, z + 10, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x + 40, -100, z, x + 45, -61, z + 10, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x + 40, -100, z + 30, x + 45, -61, z + 40, "no_outline", wood, FaceLayer.Furniture);

            AddScratchOnYPlane(x + 11, z + 13, x + 25, z + 21, -54.68, HorrorColor.DustScratch, 0.10);
        }

        private void AddChairBackLeft(double x, double z)
        {
            Color wood = HorrorColor.WornFurniture;

            // Сиденье и спинка С КОНТУРОМ
            AddBlock(x, -60, z, x + 40, -55, z + 45, null, wood, FaceLayer.Furniture);
            AddBlock(x + 10, -25, z + 40, x + 30, -5, z + 45, null, wood, FaceLayer.Furniture);

            // Ножки БЕЗ КОНТУРА
            AddBlock(x, -100, z + 40, x + 10, 0, z + 45, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x + 30, -100, z + 40, x + 40, 0, z + 45, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x, -100, z, x + 10, -61, z + 5, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x + 30, -100, z, x + 40, -61, z + 5, "no_outline", wood, FaceLayer.Furniture);

            AddScratchOnYPlane(x + 10, z + 15, x + 22, z + 25, -54.68, HorrorColor.DustScratch, 0.10);
        }

        private void AddChairRight(double x, double z)
        {
            Color wood = HorrorColor.WornFurniture;

            // Сиденье и спинка С КОНТУРОМ
            AddBlock(x, -60, z, x + 45, -55, z + 40, null, wood, FaceLayer.Furniture);
            AddBlock(x + 45, -25, z + 10, x + 50, -5, z + 30, null, wood, FaceLayer.Furniture);

            // Ножки БЕЗ КОНТУРА
            AddBlock(x + 45, -100, z + 30, x + 50, 0, z + 40, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x + 45, -100, z, x + 50, 0, z + 10, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x, -100, z, x + 5, -61, z + 10, "no_outline", wood, FaceLayer.Furniture);
            AddBlock(x, -100, z + 30, x + 5, -61, z + 40, "no_outline", wood, FaceLayer.Furniture);

            AddScratchOnYPlane(x + 13, z + 12, x + 28, z + 19, -54.68, HorrorColor.DustScratch, 0.10);
        }

        private void AddCabinetWithSyrups()
        {
            AddOpenWallCabinet(-299, -65, 408, -250, 55, 498);
            AddCabinetSyrups(-299, -250, -65, 55, 408, 498);
        }

        private void AddOpenWallCabinet(double xBack, double yBottom, double zMin, double xFront, double yTop, double zMax)
        {
            Color wood = HorrorColor.WornFurniture;
            Color dark = HorrorColor.WornFurnitureDark;

            double backThickness = 3.0;
            double sideThickness = 4.0;
            double shelfThickness = 4.0;

            AddBlock(xBack, yBottom, zMin, xBack + backThickness, yTop, zMax, null, dark, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yBottom, zMin, xFront, yTop, zMin + sideThickness, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yBottom, zMax - sideThickness, xFront, yTop, zMax, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yTop - shelfThickness, zMin + sideThickness, xFront, yTop, zMax - sideThickness, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yBottom, zMin + sideThickness, xFront, yBottom + shelfThickness, zMax - sideThickness, null, wood, FaceLayer.Furniture);

            double midY = (yBottom + yTop) / 2.0 - shelfThickness / 2.0;
            AddBlock(xBack + backThickness, midY, zMin + sideThickness, xFront, midY + shelfThickness, zMax - sideThickness, null, wood, FaceLayer.Furniture);

            AddNaturalBoardsOnYPlane(xBack + backThickness + 1.0, zMin + sideThickness + 1.0, xFront - 1.0, zMax - sideThickness - 1.0, yBottom + shelfThickness + 0.04, zMin + 28, zMin + 50, zMin + 70);
            AddNaturalBoardsOnYPlane(xBack + backThickness + 1.0, zMin + sideThickness + 1.0, xFront - 1.0, zMax - sideThickness - 1.0, midY + shelfThickness + 0.04, zMin + 26, zMin + 48, zMin + 68);

            AddNaturalBoardsOnZPlane(xBack + backThickness + 0.5, yBottom + 1.0, xFront - 0.5, yTop - 1.0, zMin + sideThickness + 0.05, xBack + 18, xBack + 34);
            AddNaturalBoardsOnZPlane(xBack + backThickness + 0.5, yBottom + 1.0, xFront - 0.5, yTop - 1.0, zMax - sideThickness - 0.05, xBack + 18, xBack + 34);
        }

        private void AddCabinetSyrups(double xBack, double xFront, double yBottom, double yTop, double zMin, double zMax)
        {
            double shelfThickness = 4.0;
            double bottleX1 = xFront - 20.0;
            double bottleX2 = xFront - 6.6;
            double bottomShelfTop = yBottom + shelfThickness;
            double midY = (yBottom + yTop) / 2.0 - shelfThickness / 2.0;
            double middleShelfTop = midY + shelfThickness;
            double[] zCenters = { zMin + 14, zMin + 34, zMin + 54, zMin + 74 };

            AddSyrupBottle(bottleX1, bottleX2, bottomShelfTop + 0.4, zCenters[0], Color.FromArgb(72, 48, 28), Color.FromArgb(186, 174, 126), Color.FromArgb(154, 126, 54), SyrupIconType.Vanilla, "VANI");
            AddSyrupBottle(bottleX1, bottleX2, bottomShelfTop + 0.4, zCenters[1], Color.FromArgb(76, 42, 24), Color.FromArgb(174, 114, 68), Color.FromArgb(134, 74, 40), SyrupIconType.Caramel, "CARA");
            AddSyrupBottle(bottleX1, bottleX2, bottomShelfTop + 0.4, zCenters[2], Color.FromArgb(62, 42, 28), Color.FromArgb(166, 144, 112), Color.FromArgb(104, 72, 48), SyrupIconType.Hazelnut, "HAZE");
            AddSyrupBottle(bottleX1, bottleX2, bottomShelfTop + 0.4, zCenters[3], Color.FromArgb(56, 34, 24), Color.FromArgb(148, 82, 82), Color.FromArgb(84, 48, 36), SyrupIconType.Chocolate, "CHOC");

            AddSyrupBottle(bottleX1, bottleX2, middleShelfTop + 0.4, zCenters[0] + 4.0, Color.FromArgb(68, 38, 34), Color.FromArgb(154, 58, 88), Color.FromArgb(112, 36, 64), SyrupIconType.Raspberry, "RASP");
            AddSyrupBottle(bottleX1, bottleX2, middleShelfTop + 0.4, zCenters[1], Color.FromArgb(50, 56, 36), Color.FromArgb(116, 154, 110), Color.FromArgb(64, 116, 70), SyrupIconType.Mint, "MINT");
            AddSyrupBottle(bottleX1, bottleX2, middleShelfTop + 0.4, zCenters[2], Color.FromArgb(70, 52, 34), Color.FromArgb(184, 178, 162), Color.FromArgb(138, 130, 104), SyrupIconType.Coconut, "COCO");
            AddSyrupBottle(bottleX1, bottleX2, middleShelfTop + 0.4, zCenters[3], Color.FromArgb(72, 50, 34), Color.FromArgb(156, 134, 104), Color.FromArgb(104, 80, 52), SyrupIconType.IrishCream, "COOK");
        }

        private void AddSyrupBottle(double x1, double x2, double baseY, double zCenter, Color bottleColor, Color labelColor, Color capColor, SyrupIconType iconType, string code)
        {
            double z1 = zCenter - 6.0;
            double z2 = zCenter + 6.0;
            double xMid = (x1 + x2) / 2.0;

            AddBlock(x1, baseY, z1, x2, baseY + 20.5, z2, null, bottleColor, FaceLayer.Furniture);
            AddBlock(x1 + 1.2, baseY + 20.5, z1 + 0.9, x2 - 1.2, baseY + 23.2, z2 - 0.9, null, bottleColor, FaceLayer.Furniture);
            AddBlock(x1 + 2.2, baseY + 23.2, zCenter - 3.0, x2 - 2.2, baseY + 25.2, zCenter + 3.0, null, capColor, FaceLayer.SmallDetail);
            AddBlock(xMid - 0.75, baseY + 25.2, zCenter - 0.85, xMid + 0.75, baseY + 28.4, zCenter + 0.85, null, capColor, FaceLayer.SmallDetail);
            int pumpStartIndex = _model.Points.Count;
            AddBlock(xMid - 1.5, baseY + 28.4, zCenter - 2.2, x2 + 1.2, baseY + 29.8, zCenter + 2.2, null, capColor, FaceLayer.SmallDetail);
            AddBlock(x2 + 1.0, baseY + 27.2, zCenter - 0.55, x2 + 2.8, baseY + 28.0, zCenter + 0.55, null, capColor, FaceLayer.SmallDetail);
            AddBlock(x2 + 2.1, baseY + 26.1, zCenter - 0.50, x2 + 2.8, baseY + 27.2, zCenter + 0.50, null, capColor, FaceLayer.SmallDetail);
            if (iconType == SyrupIconType.Raspberry)
                AddPointRangeToGroup(pumpStartIndex, _model.Points.Count, _raspberryPumpPoints, _raspberryPumpBaseY);

            AddQuad(x2 + 0.08, baseY + 4.0, zCenter - 4.8, x2 + 0.08, baseY + 17.2, zCenter - 4.8, x2 + 0.08, baseY + 17.2, zCenter + 4.8, x2 + 0.08, baseY + 4.0, zCenter + 4.8, null, labelColor, FaceLayer.SmallDetail);
            AddQuad(x2 + 0.12, baseY + 17.4, zCenter - 3.9, x2 + 0.12, baseY + 18.8, zCenter - 3.9, x2 + 0.12, baseY + 18.8, zCenter + 3.9, x2 + 0.12, baseY + 17.4, zCenter + 3.9, null, capColor, FaceLayer.SmallDetail);

            AddSyrupLabelIcon(x2 + 0.18, baseY + 12.4, zCenter, iconType);
            AddMiniLabelCode(x2 + 0.20, baseY + 5.5, zCenter, code, Color.FromArgb(62, 50, 42));
        }

        private void AddLabelRect(double x, double y1, double y2, double z1, double z2, Color color)
        {
            AddQuad(x, y1, z1, x, y2, z1, x, y2, z2, x, y1, z2, null, color, FaceLayer.SmallDetail);
        }

        private void AddLabelDiamond(double x, double centerY, double centerZ, double halfY, double halfZ, Color color)
        {
            AddQuad(x, centerY - halfY, centerZ, x, centerY, centerZ + halfZ, x, centerY + halfY, centerZ, x, centerY, centerZ - halfZ, null, color, FaceLayer.SmallDetail);
        }

        private void AddLabelTriangle(double x, double y1, double z1, double y2, double z2, double y3, double z3, Color color)
        {
            int p1 = _model.AddPoint(x, y1, z1);
            int p2 = _model.AddPoint(x, y2, z2);
            int p3 = _model.AddPoint(x, y3, z3);
            _model.AddFace(new List<int> { p1, p2, p3 }, null, color, FaceLayer.SmallDetail);
            _model.AddFace(new List<int> { p1, p3, p2 }, null, color, FaceLayer.SmallDetail);
        }

        private void AddLabelSquare(double x, double centerY, double centerZ, double halfSize, Color color)
        {
            AddLabelRect(x, centerY - halfSize, centerY + halfSize, centerZ - halfSize, centerZ + halfSize, color);
        }

        private void AddLabelOutlinedSquare(double x, double centerY, double centerZ, double halfSize, Color outlineColor, Color fillColor)
        {
            AddLabelSquare(x, centerY, centerZ, halfSize, outlineColor);
            AddLabelSquare(x, centerY, centerZ, halfSize - 0.35, fillColor);
        }

        private void AddLabelOval(double x, double centerY, double centerZ, double halfY, double halfZ, Color color)
        {
            AddLabelRect(x, centerY - halfY * 0.92, centerY + halfY * 0.92, centerZ - halfZ * 0.40, centerZ + halfZ * 0.40, color);
            AddLabelRect(x, centerY - halfY * 0.72, centerY + halfY * 0.72, centerZ - halfZ * 0.72, centerZ + halfZ * 0.72, color);
            AddLabelRect(x, centerY - halfY * 0.45, centerY + halfY * 0.45, centerZ - halfZ, centerZ + halfZ, color);
        }

        private void AddLabelOvalOnZPlane(double z, double centerX, double centerY, double halfX, double halfY, Color color, FaceLayer layer = FaceLayer.WallDetail)
        {
            AddRectOnZPlane(centerX - halfX * 0.40, centerY - halfY * 0.92, centerX + halfX * 0.40, centerY + halfY * 0.92, z, color, layer);
            AddRectOnZPlane(centerX - halfX * 0.72, centerY - halfY * 0.72, centerX + halfX * 0.72, centerY + halfY * 0.72, z, color, layer);
            AddRectOnZPlane(centerX - halfX, centerY - halfY * 0.45, centerX + halfX, centerY + halfY * 0.45, z, color, layer);
        }

        private void AddSyrupLabelIcon(double x, double centerY, double centerZ, SyrupIconType iconType)
        {
            Color red = HorrorColor.DriedBlood;
            Color darkRed = HorrorColor.OldBlood;
            Color green = Color.FromArgb(72, 126, 78);
            Color pale = Color.FromArgb(166, 152, 122);
            Color brown = Color.FromArgb(86, 54, 34);

            switch (iconType)
            {
                case SyrupIconType.Vanilla:
                    AddLabelRect(x, centerY - 0.22, centerY + 0.22, centerZ - 3.00, centerZ + 3.00, brown);
                    AddLabelTriangle(x, centerY + 3.15, centerZ, centerY - 1.15, centerZ - 2.75, centerY - 1.15, centerZ + 2.75, Color.FromArgb(160, 132, 56));
                    AddLabelTriangle(x, centerY - 3.15, centerZ, centerY + 1.15, centerZ - 2.75, centerY + 1.15, centerZ + 2.75, Color.FromArgb(160, 132, 56));
                    break;

                case SyrupIconType.Caramel:
                    AddLabelOutlinedSquare(x, centerY + 1.05, centerZ - 0.95, 1.85, brown, Color.FromArgb(154, 86, 42));
                    AddLabelOutlinedSquare(x, centerY - 0.30, centerZ + 0.95, 1.85, brown, Color.FromArgb(154, 86, 42));
                    break;

                case SyrupIconType.Chocolate:
                    AddLabelRect(x, centerY - 2.25, centerY + 2.25, centerZ - 2.25, centerZ + 2.25, brown);
                    AddLabelRect(x, centerY - 2.25, centerY + 2.25, centerZ - 0.22, centerZ + 0.22, Color.FromArgb(44, 26, 18));
                    AddLabelRect(x, centerY - 0.22, centerY + 0.22, centerZ - 2.25, centerZ + 2.25, Color.FromArgb(44, 26, 18));
                    break;

                case SyrupIconType.Mint:
                    AddLabelOval(x, centerY, centerZ, 3.05, 2.05, green);
                    AddLabelOval(x + 0.01, centerY, centerZ, 1.85, 1.05, Color.FromArgb(104, 154, 96));
                    AddLabelRect(x + 0.02, centerY - 1.85, centerY + 1.85, centerZ - 0.10, centerZ + 0.10, Color.FromArgb(138, 180, 128));
                    break;

                case SyrupIconType.Raspberry:
                    AddLabelOval(x, centerY + 1.15, centerZ, 1.05, 1.05, darkRed);
                    AddLabelOval(x, centerY + 0.10, centerZ - 1.20, 1.05, 1.05, red);
                    AddLabelOval(x, centerY + 0.10, centerZ + 1.20, 1.05, 1.05, red);
                    AddLabelOval(x, centerY - 1.05, centerZ, 1.05, 1.05, darkRed);
                    AddLabelRect(x, centerY + 2.05, centerY + 2.50, centerZ - 1.25, centerZ + 1.25, green);
                    break;

                case SyrupIconType.Coconut:
                    AddLabelOval(x, centerY, centerZ, 2.65, 2.10, brown);
                    AddLabelOval(x, centerY, centerZ, 1.82, 1.42, pale);
                    break;

                case SyrupIconType.IrishCream:
                    AddLabelOval(x, centerY, centerZ, 2.65, 2.10, Color.FromArgb(130, 104, 72));
                    AddLabelOval(x, centerY + 1.00, centerZ - 0.90, 0.30, 0.30, brown);
                    AddLabelOval(x, centerY - 0.35, centerZ - 1.10, 0.28, 0.28, brown);
                    AddLabelOval(x, centerY - 1.00, centerZ + 0.10, 0.32, 0.32, brown);
                    AddLabelOval(x, centerY + 0.25, centerZ + 1.05, 0.28, 0.28, brown);
                    AddLabelOval(x, centerY + 1.05, centerZ + 0.35, 0.26, 0.26, brown);
                    break;

                case SyrupIconType.Hazelnut:
                    AddLabelOval(x, centerY, centerZ - 1.05, 2.25, 1.65, Color.FromArgb(132, 92, 58));
                    AddLabelOval(x, centerY, centerZ - 1.05, 1.48, 1.05, Color.FromArgb(174, 150, 118));
                    AddLabelOval(x, centerY, centerZ - 1.05, 0.45, 0.45, brown);
                    AddLabelOval(x, centerY - 0.05, centerZ + 1.40, 1.95, 1.35, Color.FromArgb(104, 72, 48));
                    AddLabelDiamond(x, centerY + 2.00, centerZ + 2.35, 1.15, 0.65, green);
                    AddLabelOval(x, centerY + 1.95, centerZ + 2.35, 0.75, 0.42, green);
                    break;
            }
        }

        private void AddMiniLabelCode(double x, double centerY, double centerZ, string code, Color color)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            code = code.ToUpperInvariant();

            double letterWidth = 1.05;
            double spacing = 0.72;
            double totalWidth = code.Length * letterWidth + (code.Length - 1) * spacing;
            double startZ = centerZ - totalWidth / 2.0 + letterWidth / 2.0;

            for (int i = 0; i < code.Length; i++)
                AddMiniGlyph(x, centerY, startZ + i * (letterWidth + spacing), code[i], color);
        }

        private void AddMiniGlyph(double x, double centerY, double centerZ, char c, Color color)
        {
            switch (c)
            {
                case 'A': AddGlyphA(x, centerY, centerZ, color); break;
                case 'C': AddGlyphC(x, centerY, centerZ, color); break;
                case 'E': AddGlyphE(x, centerY, centerZ, color); break;
                case 'H': AddGlyphH(x, centerY, centerZ, color); break;
                case 'I': AddGlyphI(x, centerY, centerZ, color); break;
                case 'K': AddGlyphK(x, centerY, centerZ, color); break;
                case 'M': AddGlyphM(x, centerY, centerZ, color); break;
                case 'N': AddGlyphN(x, centerY, centerZ, color); break;
                case 'O': AddGlyphO(x, centerY, centerZ, color); break;
                case 'P': AddGlyphP(x, centerY, centerZ, color); break;
                case 'R': AddGlyphR(x, centerY, centerZ, color); break;
                case 'S': AddGlyphS(x, centerY, centerZ, color); break;
                case 'T': AddGlyphT(x, centerY, centerZ, color); break;
                case 'V': AddGlyphV(x, centerY, centerZ, color); break;
                case 'Z': AddGlyphZ(x, centerY, centerZ, color); break;
            }
        }

        private void AddMiniStroke(double x, double y1, double y2, double z1, double z2, Color color)
        {
            double padY = 0.08;
            double padZ = 0.08;
            AddQuad(x, y1 - padY, z1 - padZ, x, y2 + padY, z1 - padZ, x, y2 + padY, z2 + padZ, x, y1 - padY, z2 + padZ, null, color, FaceLayer.SmallDetail);
        }

        private void AddGlyphA(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y - 1.2, y + 1.2, z + 0.30, z + 0.55, color);
            AddMiniStroke(x, y + 0.85, y + 1.15, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 0.1, y + 0.2, z - 0.42, z + 0.42, color);
        }

        private void AddGlyphC(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.28, color);
            AddMiniStroke(x, y + 0.9, y + 1.2, z - 0.55, z + 0.50, color);
            AddMiniStroke(x, y - 1.2, y - 0.9, z - 0.55, z + 0.50, color);
        }

        private void AddGlyphE(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y + 0.95, y + 1.2, z - 0.55, z + 0.50, color);
            AddMiniStroke(x, y - 0.12, y + 0.12, z - 0.55, z + 0.35, color);
            AddMiniStroke(x, y - 1.2, y - 0.95, z - 0.55, z + 0.50, color);
        }

        private void AddGlyphP(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y + 0.95, y + 1.2, z - 0.55, z + 0.45, color);
            AddMiniStroke(x, y - 0.05, y + 0.20, z - 0.55, z + 0.45, color);
            AddMiniStroke(x, y + 0.15, y + 1.2, z + 0.30, z + 0.55, color);
        }

        private void AddGlyphK(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y + 0.30, y + 0.65, z - 0.05, z + 0.25, color);
            AddMiniStroke(x, y + 0.65, y + 1.05, z + 0.20, z + 0.55, color);
            AddMiniStroke(x, y - 0.65, y - 0.30, z - 0.05, z + 0.25, color);
            AddMiniStroke(x, y - 1.05, y - 0.65, z + 0.20, z + 0.55, color);
        }

        private void AddGlyphH(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y - 1.2, y + 1.2, z + 0.30, z + 0.55, color);
            AddMiniStroke(x, y - 0.15, y + 0.15, z - 0.55, z + 0.55, color);
        }

        private void AddGlyphI(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.12, z + 0.12, color);
            AddMiniStroke(x, y + 0.95, y + 1.2, z - 0.45, z + 0.45, color);
            AddMiniStroke(x, y - 1.2, y - 0.95, z - 0.45, z + 0.45, color);
        }

        private void AddGlyphM(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.32, color);
            AddMiniStroke(x, y - 1.2, y + 1.2, z + 0.32, z + 0.55, color);
            AddMiniStroke(x, y + 0.45, y + 1.0, z - 0.18, z + 0.18, color);
        }

        private void AddGlyphN(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y - 1.2, y + 1.2, z + 0.30, z + 0.55, color);
            AddMiniStroke(x, y + 0.75, y + 1.05, z - 0.35, z - 0.05, color);
            AddMiniStroke(x, y + 0.25, y + 0.55, z - 0.15, z + 0.15, color);
            AddMiniStroke(x, y - 0.25, y + 0.05, z + 0.05, z + 0.35, color);
            AddMiniStroke(x, y - 0.75, y - 0.45, z + 0.25, z + 0.55, color);
        }

        private void AddGlyphO(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y - 1.2, y + 1.2, z + 0.30, z + 0.55, color);
            AddMiniStroke(x, y + 0.9, y + 1.2, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y - 0.9, z - 0.55, z + 0.55, color);
        }

        private void AddGlyphR(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y + 0.9, y + 1.2, z - 0.55, z + 0.45, color);
            AddMiniStroke(x, y + 0.05, y + 0.35, z - 0.15, z + 0.45, color);
            AddMiniStroke(x, y - 0.15, y + 1.0, z + 0.30, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y - 0.75, z + 0.15, z + 0.40, color);
        }

        private void AddGlyphS(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y + 0.9, y + 1.2, z - 0.50, z + 0.50, color);
            AddMiniStroke(x, y - 0.15, y + 0.15, z - 0.50, z + 0.50, color);
            AddMiniStroke(x, y - 1.2, y - 0.9, z - 0.50, z + 0.50, color);
            AddMiniStroke(x, y + 0.1, y + 1.2, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y - 1.2, y - 0.1, z + 0.30, z + 0.55, color);
        }

        private void AddGlyphT(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y + 0.95, y + 1.2, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y + 1.2, z - 0.12, z + 0.12, color);
        }

        private void AddGlyphV(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y - 1.2, y + 0.9, z - 0.55, z - 0.30, color);
            AddMiniStroke(x, y - 1.2, y + 0.9, z + 0.30, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y - 0.9, z - 0.15, z + 0.15, color);
        }

        private void AddGlyphZ(double x, double y, double z, Color color)
        {
            AddMiniStroke(x, y + 0.90, y + 1.2, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y - 0.90, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y + 0.45, y + 0.75, z + 0.18, z + 0.52, color);
            AddMiniStroke(x, y + 0.05, y + 0.35, z - 0.05, z + 0.28, color);
            AddMiniStroke(x, y - 0.35, y - 0.05, z - 0.28, z + 0.05, color);
            AddMiniStroke(x, y - 0.75, y - 0.45, z - 0.52, z - 0.18, color);
        }

        private void AddTeaShelf()
        {
            AddBlock(-7, -85, 592.5, 7, 45, 598, null, HorrorColor.DeepWood, FaceLayer.Furniture);

            AddTeaShelfBoard(-57);
            AddTeaShelfBoard(-22);
            AddTeaShelfBoard(13);

            double teaZ = 596.1;

            // Верхняя полка: 6 чайных чашек (по 3 слева и справа)
            AddTeaRow(
                -54.4,
                teaZ,
                Color.FromArgb(168, 162, 150),
                Color.FromArgb(144, 136, 124),
                Color.FromArgb(170, 166, 156),
                Color.FromArgb(150, 144, 132),
                Color.FromArgb(170, 166, 156),
                Color.FromArgb(130, 124, 114)
            );

            // Средняя полка: 6 латте-чашек
            AddLatteCupRow(-19.4, teaZ);

            // Нижняя полка: 6 бумажных стаканов
            AddPaperCupRow(15.6, teaZ);
        }

        private void AddTeaShelfBoard(double y)
        {
            double shelfX1 = -65;
            double shelfX2 = 65;

            AddBlock(shelfX1, y, 586.2, shelfX2, y + 2.8, 598.0, null, HorrorColor.DeepWood, FaceLayer.Furniture);
            AddBlock(shelfX1, y - 6.0, 585.0, shelfX2, y + 0.45, 588.3, null, HorrorColor.DeepWood, FaceLayer.SmallDetail);
            AddBlock(shelfX1, y + 2.1, 585.5, shelfX2, y + 3.0, 587.2, null, HorrorColor.DeadWoodLight, FaceLayer.SmallDetail);
        }

        private void AddTeaRow(double y, double z, params Color[] colors)
        {
            // По 3 предмета слева и справа
            double[] xs = { -56, -36, -16, 16, 36, 56 };

            for (int i = 0; i < xs.Length && i < colors.Length; i++)
            {
                // Первые три места на верхней полке пустые в начале смены:
                // эти кружки перенесены на столы как грязные.
                if (i < 3)
                {
                    AddReturnedShelfTeaPair(xs[i], y, z, colors[i]);
                    continue;
                }

                AddTeaPair(xs[i], y, z, colors[i]);
            }
        }

        private void AddReturnedShelfTeaPair(double x, double y, double z, Color cupColor)
        {
            int startIndex = _model.Points.Count;

            AddTeaPair(x, y, z, cupColor);

            for (int i = startIndex; i < _model.Points.Count; i++)
            {
                _shelfReturnedCupPoints.Add(i);
                _shelfReturnedCupBaseY.Add(_model.Points[i].Y);
            }
        }

        private void AddTeaPair(double x, double y, double z, Color cupColor)
        {
            AddSaucer(x, y + 0.6, z, Color.FromArgb(144, 140, 130));
            AddCup(x, y + 1.15, z, cupColor);
        }

        private void AddLatteCupRow(double y, double z)
        {
            double[] xs = { -56, -36, -16, 16, 36, 56 };

            Color[] cupColors =
            {
        Color.FromArgb(150, 144, 132),
        Color.FromArgb(130, 122, 112),
        Color.FromArgb(164, 158, 148),
        Color.FromArgb(148, 142, 130),
        Color.FromArgb(164, 158, 148),
        Color.FromArgb(120, 114, 104)
    };

            for (int i = 0; i < xs.Length; i++)
                AddLatteCupSet(xs[i], y, z, cupColors[i]);
        }

        private void AddLatteCupSet(double x, double y, double z, Color cupColor)
        {
            AddLargeSaucer(x, y + 0.6, z, Color.FromArgb(138, 134, 124));
            AddLatteCup(x, y + 1.2, z, cupColor);
        }

        private void AddLargeSaucer(double x, double y, double z, Color color)
        {
            AddBlock(x - 7.0, y, z - 4.0, x + 7.0, y + 0.95, z + 4.0, null, color, FaceLayer.Furniture);
            AddBlock(x - 4.8, y + 0.95, z - 2.8, x + 4.8, y + 1.3, z + 2.8, null, Color.FromArgb(166, 160, 148), FaceLayer.Furniture);
        }

        private void AddLatteCup(double x, double y, double z, Color color)
        {
            Color rimColor = Color.FromArgb(Math.Min(color.R + 10, 255), Math.Min(color.G + 10, 255), Math.Min(color.B + 10, 255));
            Color handleColor = Color.FromArgb(Math.Max(color.R - 8, 0), Math.Max(color.G - 8, 0), Math.Max(color.B - 8, 0));

            AddBlock(x - 4.2, y, z - 3.2, x + 4.2, y + 2.4, z + 3.2, null, color, FaceLayer.Furniture);
            AddBlock(x - 3.8, y + 2.4, z - 3.0, x + 3.8, y + 5.8, z + 3.0, null, color, FaceLayer.Furniture);
            AddBlock(x - 2.8, y + 5.8, z - 2.2, x + 2.8, y + 6.6, z + 2.2, null, rimColor, FaceLayer.Furniture);
            AddBlock(x + 4.0, y + 2.0, z - 0.9, x + 5.3, y + 5.0, z + 0.9, null, handleColor, FaceLayer.Furniture);
        }

        private void AddPaperCupRow(double y, double z)
        {
            double[] xs = { -56, -36, -16, 16, 36, 56 };

            Color[] bodyColors =
            {
        Color.FromArgb(158, 150, 136),
        Color.FromArgb(138, 126, 112),
        Color.FromArgb(170, 162, 150),
        Color.FromArgb(152, 142, 128),
        Color.FromArgb(170, 162, 150),
        Color.FromArgb(144, 132, 118)
    };

            for (int i = 0; i < xs.Length; i++)
            {
                int startIndex = _model.Points.Count;
                AddPaperCup(xs[i], y + 0.8, z, bodyColors[i]);

                // Последний стакан делаем управляемым: когда игрок берёт стакан с полки,
                // именно эта стопка/чашка визуально пропадает.
                if (i == xs.Length - 1)
                    AddPointRangeToGroup(startIndex, _model.Points.Count, _takeawayShelfCupPoints, _takeawayShelfCupBaseY);
            }
        }

        private void AddPaperCup(double x, double y, double z, Color bodyColor)
        {
            AddBlock(x - 3.2, y, z - 2.2, x + 3.2, y + 3.0, z + 2.2, null, bodyColor, FaceLayer.Furniture);
            AddBlock(x - 2.8, y + 3.0, z - 1.9, x + 2.8, y + 5.7, z + 1.9, null, bodyColor, FaceLayer.Furniture);
            AddBlock(x - 2.9, y + 1.8, z - 2.0, x + 2.9, y + 3.3, z + 2.0, null, HorrorColor.DeepWood, FaceLayer.SmallDetail);
            AddBlock(x - 3.5, y + 5.7, z - 2.5, x + 3.5, y + 6.5, z + 2.5, null, Color.FromArgb(156, 154, 148), FaceLayer.Furniture);
        }

        private void AddSaucer(double x, double y, double z, Color color)
        {
            AddBlock(x - 6.0, y, z - 3.5, x + 6.0, y + 0.85, z + 3.5, null, color, FaceLayer.Furniture);
            AddBlock(x - 4.2, y + 0.85, z - 2.4, x + 4.2, y + 1.2, z + 2.4, null, Color.FromArgb(166, 160, 148), FaceLayer.Furniture);
        }

        private void AddCup(double x, double y, double z, Color color)
        {
            AddBlock(x - 3.4, y, z - 2.6, x + 3.4, y + 1.8, z + 2.6, null, color, FaceLayer.Furniture);
            AddBlock(x - 3.1, y + 1.8, z - 2.4, x + 3.1, y + 4.2, z + 2.4, null, color, FaceLayer.Furniture);
            AddBlock(x - 2.3, y + 4.2, z - 1.8, x + 2.3, y + 4.9, z + 1.8, null, Color.FromArgb(Math.Min(color.R + 10, 255), Math.Min(color.G + 10, 255), Math.Min(color.B + 10, 255)), FaceLayer.Furniture);
            AddBlock(x + 3.2, y + 1.4, z - 0.7, x + 4.2, y + 3.6, z + 0.7, null, Color.FromArgb(Math.Max(color.R - 6, 0), Math.Max(color.G - 6, 0), Math.Max(color.B - 6, 0)), FaceLayer.Furniture);
        }

        private void AddDirtyCupsAndCoffeeStainsOnTables()
        {
            // Эти 3 кружки стоят на столах в начале смены.
            // На первом столе две кружки, на втором одна кружка.
            // Под каждой кружкой есть неидеальный кофейный развод.

            AddCoffeeStainOnTable(-158, -39.47, 145, _table1StainPoints, _table1StainBaseY);
            AddDirtyTableCup(-158, -38.9, 145, Color.FromArgb(166, 160, 150), _dirtyTable1CupPoints, _dirtyTable1CupBaseY);

            AddCoffeeStainOnTable(-128, -39.46, 168, _table1StainPoints, _table1StainBaseY);
            AddDirtyTableCup(-128, -38.9, 168, Color.FromArgb(144, 136, 124), _dirtyTable1CupPoints, _dirtyTable1CupBaseY);

            AddCoffeeStainOnTable(145, -39.47, 145, _table2StainPoints, _table2StainBaseY);
            AddDirtyTableCup(145, -38.9, 145, Color.FromArgb(170, 166, 156), _dirtyTable2CupPoints, _dirtyTable2CupBaseY);
        }

        private void AddDirtyTableCup(
            double x,
            double y,
            double z,
            Color cupColor,
            List<int> pointGroup,
            List<double> baseYGroup)
        {
            int startIndex = _model.Points.Count;

            AddLargeSaucer(x, y, z, Color.FromArgb(122, 116, 106));
            AddLatteCup(x, y + 0.6, z, cupColor);

            // Грязь от кофе на самой кружке: неровные тёмные следы спереди и сверху.
            Color coffee = Color.FromArgb(74, 42, 24);
            Color coffeeLight = Color.FromArgb(96, 58, 32);

            AddRectOnZPlane(x - 2.8, y + 4.2, x + 2.4, y + 7.8, z + 3.15, coffee, FaceLayer.SmallDetail);
            AddRectOnZPlane(x - 1.2, y + 7.4, x + 3.1, y + 9.6, z + 3.18, coffeeLight, FaceLayer.SmallDetail);
            AddRectOnZPlane(x + 2.4, y + 3.2, x + 3.4, y + 6.4, z + 3.20, coffee, FaceLayer.SmallDetail);
            AddRectOnYPlane(x - 2.3, z - 1.3, x + 2.6, z + 1.4, y + 7.35, Color.FromArgb(82, 45, 24), FaceLayer.SmallDetail);

            AddPointRangeToGroup(startIndex, _model.Points.Count, pointGroup, baseYGroup);
        }

        private void AddCoffeeStainOnTable(
            double x,
            double y,
            double z,
            List<int> pointGroup,
            List<double> baseYGroup)
        {
            int startIndex = _model.Points.Count;

            // Кофейный развод под кружкой: не россыпь квадратиков, а мягкое кольцо,
            // небольшая лужица сбоку и пара тонких подтёков.
            // Alpha оставляем заметным, чтобы след не терялся на текстуре столешницы.
            Color ringDark = Color.FromArgb(170, 70, 36, 18);
            Color ringMid = Color.FromArgb(145, 92, 50, 24);
            Color wet = Color.FromArgb(125, 58, 30, 16);
            Color drip = Color.FromArgb(160, 64, 32, 16);

            // Неровное кольцо от дна кружки.
            AddRectOnYPlane(x - 7.2, z - 5.9, x + 5.8, z - 4.3, y, ringDark, FaceLayer.SmallDetail);
            AddRectOnYPlane(x - 7.8, z + 4.1, x + 5.0, z + 5.8, y + 0.01, ringDark, FaceLayer.SmallDetail);
            AddRectOnYPlane(x - 7.9, z - 4.2, x - 6.1, z + 4.1, y + 0.02, ringMid, FaceLayer.SmallDetail);
            AddRectOnYPlane(x + 4.7, z - 3.8, x + 6.4, z + 3.9, y + 0.03, ringMid, FaceLayer.SmallDetail);

            // Небольшая растёкшаяся часть, будто кофе вытек из-под кружки.
            AddRectOnYPlane(x + 5.2, z + 1.8, x + 11.8, z + 4.6, y + 0.04, wet, FaceLayer.SmallDetail);
            AddRectOnYPlane(x + 7.4, z + 4.2, x + 10.2, z + 8.4, y + 0.05, wet, FaceLayer.SmallDetail);

            // Два тонких подтёка, но без пестроты и без огромного количества деталей.
            AddRectOnYPlane(x + 8.8, z + 7.5, x + 10.0, z + 13.5, y + 0.06, drip, FaceLayer.SmallDetail);
            AddRectOnYPlane(x - 6.6, z - 10.5, x - 5.4, z - 5.5, y + 0.07, drip, FaceLayer.SmallDetail);

            AddPointRangeToGroup(startIndex, _model.Points.Count, pointGroup, baseYGroup);
        }

        private void AddPointRangeToGroup(
            int startIndex,
            int endIndex,
            List<int> pointGroup,
            List<double> baseYGroup)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                pointGroup.Add(i);
                baseYGroup.Add(_model.Points[i].Y);
            }
        }

        private void AddPointRangeToTransformGroup(
            int startIndex,
            int endIndex,
            List<int> pointGroup,
            List<double> baseXGroup,
            List<double> baseYGroup,
            List<double> baseZGroup)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                pointGroup.Add(i);
                baseXGroup.Add(_model.Points[i].X);
                baseYGroup.Add(_model.Points[i].Y);
                baseZGroup.Add(_model.Points[i].Z);
            }
        }

        private void MovePointGroupByY(List<int> pointIndices, List<double> baseY, bool visible)
        {
            double offsetY = visible ? 0.0 : -10000.0;

            for (int i = 0; i < pointIndices.Count; i++)
            {
                int index = pointIndices[i];

                if (index < 0 || index >= _model.Points.Count || i >= baseY.Count)
                    continue;

                _model.Points[index].Y = baseY[i] + offsetY;
            }
        }

        private void SetPointGroupYOffset(List<int> pointIndices, List<double> baseY, double offsetY)
        {
            for (int i = 0; i < pointIndices.Count; i++)
            {
                int index = pointIndices[i];
                if (index < 0 || index >= _model.Points.Count || i >= baseY.Count)
                    continue;

                _model.Points[index].Y = baseY[i] + offsetY;
            }
        }

        public void SetCoffeeMachineCupState(bool visible, double fillProgress)
        {
            MovePointGroupByY(_coffeeMachineCupPoints, _coffeeMachineCupBaseY, visible);
            MovePointGroupByY(_coffeeMachineCupCoffeePoints, _coffeeMachineCupCoffeeBaseY, visible);

            if (visible)
            {
                double clamped = Math.Max(0, Math.Min(1, fillProgress));
                double topY = _coffeeMachineCupCoffeeBottomY + (_coffeeMachineCupCoffeeTopY - _coffeeMachineCupCoffeeBottomY) * clamped;
                for (int i = 0; i < _coffeeMachineCupCoffeeTopPoints.Count; i++)
                {
                    int pointIndex = _coffeeMachineCupCoffeeTopPoints[i];
                    if (pointIndex >= 0 && pointIndex < _model.Points.Count)
                        _model.Points[pointIndex].Y = topY;
                }
            }

            _model.NotifyChanged();
        }

        public void SetRaspberryPumpPressed(double progress)
        {
            double clamped = Math.Max(0, Math.Min(1, progress));
            double pressOffset = -1.35 * Math.Sin(clamped * Math.PI);
            SetPointGroupYOffset(_raspberryPumpPoints, _raspberryPumpBaseY, pressOffset);
            _model.NotifyChanged();
        }

        public void SetCoffeeBeanFrontBagVisible(bool visible)
        {
            MovePointGroupByY(_frontCoffeeBeanBagPoints, _frontCoffeeBeanBagBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetCoffeeMachineRefillAnimation(bool visible, double progress)
        {
            double hiddenOffset = visible ? 0.0 : -10000.0;
            double clamped = Math.Max(0, Math.Min(1, progress));

            // Делаем ОГРОМНЫЙ, максимально заметный поток зёрен.
            // Мешок трясётся, а масса маленьких зёрен высыпается СЛЕВА от 1-го лица
            // и падает до верхней крышки кофемашины, где зёрна исчезают и появляются заново сверху.
            double shakeFast = Math.Sin(clamped * Math.PI * 8.5);
            double shakeSlow = Math.Sin(clamped * Math.PI * 4.0 + 0.55);
            double shakeTiny = Math.Sin(clamped * Math.PI * 14.0 + 1.1);
            double openPulse = 1.00 + 0.32 * Math.Abs(Math.Sin(clamped * Math.PI * 6.0));

            double yaw = -90.0 * Math.PI / 180.0; // этикетка с зерном к игроку
            double rollLeft = (35.0 + 3.4 * shakeFast) * Math.PI / 180.0;
            double cosYaw = Math.Cos(yaw);
            double sinYaw = Math.Sin(yaw);
            double cosRollLeft = Math.Cos(rollLeft);
            double sinRollLeft = Math.Sin(rollLeft);

            const double bagBaseCenterX = 251.0;
            const double bagBaseCenterY = 18.1;
            const double bagBaseCenterZ = 496.0;

            double bagTargetCenterX = 264.9 + 0.18 * shakeFast;
            double bagTargetCenterY = 20.8 + 0.16 * Math.Abs(shakeSlow) + 0.08 * shakeTiny;
            double bagTargetCenterZ = 496.9 + 0.16 * shakeSlow;

            void TransformBagPoint(double localX, double localY, double localZ, out double worldX, out double worldY, out double worldZ)
            {
                if (localY > 8.0)
                {
                    localY += 0.36 * openPulse;
                    if (localZ > 1.0)
                    {
                        localZ += 2.25 * openPulse;
                        localY += 0.20 * openPulse;
                    }
                    else if (localZ < -1.0)
                    {
                        localZ -= 0.45 * openPulse;
                    }
                }

                double x1 = localX * cosYaw + localZ * sinYaw;
                double z1 = -localX * sinYaw + localZ * cosYaw;
                double y1 = localY * cosRollLeft - z1 * sinRollLeft;
                double z2 = localY * sinRollLeft + z1 * cosRollLeft;

                worldX = bagTargetCenterX + x1;
                worldY = bagTargetCenterY + y1;
                worldZ = bagTargetCenterZ + z2;
            }

            for (int i = 0; i < _coffeeRefillBagPoints.Count; i++)
            {
                int index = _coffeeRefillBagPoints[i];
                if (index < 0 || index >= _model.Points.Count)
                    continue;

                double localX = _coffeeRefillBagBaseX[i] - bagBaseCenterX;
                double localY = _coffeeRefillBagBaseY[i] - bagBaseCenterY;
                double localZ = _coffeeRefillBagBaseZ[i] - bagBaseCenterZ;
                TransformBagPoint(localX, localY, localZ, out double worldX, out double worldY, out double worldZ);

                _model.Points[index].X = worldX + hiddenOffset;
                _model.Points[index].Y = worldY + hiddenOffset;
                _model.Points[index].Z = worldZ + hiddenOffset;
            }

            // Источник потока поставлен прямо в место кончика красной стрелки на скрине.
            // Предыдущая привязка к углу всё ещё давала старт слишком правее/выше.
            // Поэтому здесь задаём отдельную контрольную точку у края мешка:
            // Условное "лево" теперь считаем как сторону стены с настенным меню.
            // Поэтому смещаем поток левее по Z примерно на 15 координат и слегка поднимаем по Y.
            double bagShakeX = 0.10 * shakeFast;
            double bagShakeY = 0.10 * Math.Abs(shakeSlow);
            double bagShakeZ = 0.08 * shakeSlow;

            double arrowTipX = 268.8 + bagShakeX;
            double arrowTipY = 22.3 + bagShakeY;
            double arrowTipZ = 512.2 + bagShakeZ;

            // Старт делаем компактным, чтобы верх потока был именно точкой выхода из мешка,
            // а не отдельной широкой колонной сбоку.
            double[] sourceXs = { arrowTipX - 0.9, arrowTipX - 0.6, arrowTipX - 0.3, arrowTipX, arrowTipX + 0.3, arrowTipX + 0.6, arrowTipX + 0.9, arrowTipX + 1.2 };
            double[] sourceYs = { arrowTipY - 0.5, arrowTipY - 0.2, arrowTipY, arrowTipY + 0.2, arrowTipY + 0.3, arrowTipY + 0.1, arrowTipY - 0.2, arrowTipY - 0.4 };
            double[] sourceZs = { arrowTipZ - 0.5, arrowTipZ - 0.3, arrowTipZ - 0.1, arrowTipZ, arrowTipZ + 0.1, arrowTipZ + 0.3, arrowTipZ + 0.5, arrowTipZ + 0.7 };

            // Низ потока идёт от этой точки вниз и чуть вправо к отсеку кофемашины.
            double[] targetXs = { arrowTipX + 4.0, arrowTipX + 5.2, arrowTipX + 6.4, arrowTipX + 7.6, arrowTipX + 8.8, arrowTipX + 10.0, arrowTipX + 11.2, arrowTipX + 12.4, arrowTipX + 13.6, arrowTipX + 14.8 };
            double[] targetYs = { 5.9, 5.8, 5.7, 5.6, 5.7, 5.8, 5.7, 5.6, 5.7, 5.8 };
            double[] targetZs = { arrowTipZ - 0.4, arrowTipZ, arrowTipZ + 0.4, arrowTipZ + 0.8, arrowTipZ + 1.2, arrowTipZ + 1.6, arrowTipZ + 2.0, arrowTipZ + 2.4, arrowTipZ + 2.8, arrowTipZ + 3.2 };

            int pointsPerCluster = 24;
            int beanGroupCount = Math.Max(1, _coffeeRefillBeansPoints.Count / pointsPerCluster);

            for (int clusterIndex = 0; clusterIndex < beanGroupCount; clusterIndex++)
            {
                double fall = (clamped * 18.0 + clusterIndex * 0.037) % 1.0;

                int sourceLane = clusterIndex % sourceXs.Length;
                double sourceX = sourceXs[sourceLane] + bagShakeX;
                double sourceY = sourceYs[sourceLane] + bagShakeY;
                double sourceZ = sourceZs[sourceLane] + bagShakeZ;

                int targetLane = clusterIndex % targetXs.Length;
                double targetX = targetXs[targetLane];
                double targetY = targetYs[targetLane];
                double targetZ = targetZs[targetLane];

                int columnXIndex = clusterIndex % 10;
                int columnZIndex = (clusterIndex / 10) % 8;
                double spread = 0.06 + 2.00 * fall;
                double sideSpread = (columnXIndex - 4.5) * 0.36 * spread;
                double depthSpread = (columnZIndex - 3.5) * 0.28 * spread;
                double wobbleX = Math.Sin((clamped * Math.PI * 12.0) + clusterIndex * 0.39) * 0.22;
                double wobbleZ = Math.Cos((clamped * Math.PI * 11.4) + clusterIndex * 0.31) * 0.18;
                double wobbleY = Math.Sin((clamped * Math.PI * 13.0) + clusterIndex * 0.27) * 0.20;

                double lineX = sourceX + (targetX - sourceX) * fall;
                double lineY = sourceY + (targetY - sourceY) * fall;
                double lineZ = sourceZ + (targetZ - sourceZ) * fall;

                int clusterStart = clusterIndex * pointsPerCluster;
                int clusterEnd = Math.Min(clusterStart + pointsPerCluster, _coffeeRefillBeansPoints.Count);

                double minBaseX = double.MaxValue;
                double maxBaseX = double.MinValue;
                double minBaseY = double.MaxValue;
                double minBaseZ = double.MaxValue;
                double maxBaseZ = double.MinValue;

                for (int j = clusterStart; j < clusterEnd; j++)
                {
                    if (_coffeeRefillBeansBaseX[j] < minBaseX) minBaseX = _coffeeRefillBeansBaseX[j];
                    if (_coffeeRefillBeansBaseX[j] > maxBaseX) maxBaseX = _coffeeRefillBeansBaseX[j];
                    if (_coffeeRefillBeansBaseY[j] < minBaseY) minBaseY = _coffeeRefillBeansBaseY[j];
                    if (_coffeeRefillBeansBaseZ[j] < minBaseZ) minBaseZ = _coffeeRefillBeansBaseZ[j];
                    if (_coffeeRefillBeansBaseZ[j] > maxBaseZ) maxBaseZ = _coffeeRefillBeansBaseZ[j];
                }

                double localCenterX = (minBaseX + maxBaseX) * 0.5;
                double localBottomY = minBaseY;
                double localCenterZ = (minBaseZ + maxBaseZ) * 0.5;

                for (int j = clusterStart; j < clusterEnd; j++)
                {
                    int index = _coffeeRefillBeansPoints[j];
                    if (index < 0 || index >= _model.Points.Count)
                        continue;

                    double localX = _coffeeRefillBeansBaseX[j] - localCenterX;
                    double localY = _coffeeRefillBeansBaseY[j] - localBottomY;
                    double localZ = _coffeeRefillBeansBaseZ[j] - localCenterZ;

                    _model.Points[index].X = lineX + sideSpread + wobbleX + localX + hiddenOffset;
                    _model.Points[index].Y = lineY + wobbleY + localY + hiddenOffset;
                    _model.Points[index].Z = lineZ + depthSpread + wobbleZ + localZ + hiddenOffset;
                }
            }

            _model.NotifyChanged();
        }

        public void SetSinkWashAnimation(bool visible, double progress)
        {
            double hiddenOffset = visible ? 0.0 : -10000.0;
            double clamped = Math.Max(0, Math.Min(1, progress));

            for (int i = 0; i < _sinkWashFoamPoints.Count; i++)
            {
                int index = _sinkWashFoamPoints[i];
                if (index < 0 || index >= _model.Points.Count)
                    continue;

                int clusterIndex = i / 8;
                double bubblePhase = clamped * Math.PI * 6.3 + clusterIndex * 0.76;
                double rise = 0.34 * Math.Sin(bubblePhase) + 0.08 * Math.Cos(bubblePhase * 0.58);
                double driftX = 0.06 * Math.Cos(bubblePhase * 0.90);
                double driftZ = 0.05 * Math.Sin(bubblePhase * 1.05);

                _model.Points[index].X = _sinkWashFoamBaseX[i] + hiddenOffset + driftX;
                _model.Points[index].Y = _sinkWashFoamBaseY[i] + hiddenOffset + rise;
                _model.Points[index].Z = _sinkWashFoamBaseZ[i] + hiddenOffset + driftZ;
            }

            for (int i = 0; i < _sinkWashWaterPoints.Count; i++)
            {
                int index = _sinkWashWaterPoints[i];
                if (index < 0 || index >= _model.Points.Count)
                    continue;

                int streamIndex = i / 8;
                double waterPhase = clamped * Math.PI * 8.2 + streamIndex * 0.42;
                double swayX = 0.05 * Math.Sin(waterPhase);
                double swayZ = 0.035 * Math.Cos(waterPhase * 1.15);
                double rippleY = 0.12 * Math.Sin(waterPhase * 0.9);

                _model.Points[index].X = _sinkWashWaterBaseX[i] + hiddenOffset + swayX;
                _model.Points[index].Y = _sinkWashWaterBaseY[i] + hiddenOffset + rippleY;
                _model.Points[index].Z = _sinkWashWaterBaseZ[i] + hiddenOffset + swayZ;
            }

            _model.NotifyChanged();
        }

        public void SetDirtyCupsOnTable1Visible(bool visible)
        {
            MovePointGroupByY(_dirtyTable1CupPoints, _dirtyTable1CupBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetDirtyCupsOnTable2Visible(bool visible)
        {
            MovePointGroupByY(_dirtyTable2CupPoints, _dirtyTable2CupBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetCoffeeStainsOnTable1Visible(bool visible)
        {
            MovePointGroupByY(_table1StainPoints, _table1StainBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetCoffeeStainsOnTable2Visible(bool visible)
        {
            MovePointGroupByY(_table2StainPoints, _table2StainBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetReturnedShelfCupsVisible(bool visible)
        {
            MovePointGroupByY(_shelfReturnedCupPoints, _shelfReturnedCupBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetTakeawayShelfCupVisible(bool visible)
        {
            MovePointGroupByY(_takeawayShelfCupPoints, _takeawayShelfCupBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetTiaOrderExchangeVisible(bool cupVisible, bool billVisible)
        {
            MovePointGroupByY(_tiaServedCupPoints, _tiaServedCupBaseY, cupVisible);
            MovePointGroupByY(_tiaPaymentBillPoints, _tiaPaymentBillBaseY, billVisible);
            _model.NotifyChanged();
        }

        public void SetTiaHoldingCupVisible(bool visible)
        {
            _tiaHoldingCupVisible = visible;
            ApplyClientTransform();
            _model.NotifyChanged();
        }

        public void SetTvScreenOn(bool visible)
        {
            _tvScreenVisible = visible;
            MovePointGroupByY(_tvScreenContentPoints, _tvScreenContentBaseY, visible);
            _model.NotifyChanged();
        }

        public void SetCashRecipeScreenVisible(bool visible)
        {
            MovePointGroupByY(_cashRecipeScreenPoints, _cashRecipeScreenBaseY, visible);
            _model.NotifyChanged();
        }

        public void AnimateTvScreen(double deltaTime)
        {
            if (!_tvScreenVisible || _tvAnimatedScreenPoints.Count == 0)
                return;

            _tvAnimationTime += deltaTime;

            for (int i = 0; i < _tvAnimatedScreenPoints.Count; i++)
            {
                int index = _tvAnimatedScreenPoints[i];

                if (index < 0 || index >= _model.Points.Count || i >= _tvAnimatedScreenBaseY.Count)
                    continue;

                double wave = Math.Sin(_tvAnimationTime * 3.0 + i * 0.55);
                double slowWave = Math.Sin(_tvAnimationTime * 1.25 + i * 0.23);

                // Небольшое движение вверх-вниз: пар заметнее,
                // надпись двигается мягко вместе с экранной заставкой.
                _model.Points[index].Y = _tvAnimatedScreenBaseY[i] + wave * 0.25 + slowWave * 0.18;
            }

            _model.NotifyChanged();
        }

        public void SetClientVisible(bool visible)
        {
            _clientVisible = visible;

            if (!visible)
            {
                _clientWalkingNow = false;
                _clientAnimationPhase = 0;
            }

            ApplyClientTransform();
            _model.NotifyChanged();
        }

        public void SetClientTransform(double worldX, double worldZ, double yaw)
        {
            double dx = worldX - _clientWorldX;
            double dz = worldZ - _clientWorldZ;
            double moveDistance = Math.Sqrt(dx * dx + dz * dz);

            _clientPrevWorldX = _clientWorldX;
            _clientPrevWorldZ = _clientWorldZ;
            _clientWorldX = worldX;
            _clientWorldZ = worldZ;
            _clientYaw = yaw;

            if (moveDistance > 0.02)
            {
                _clientWalkingNow = true;
                _clientAnimationPhase += moveDistance * 0.18;
            }
            else
            {
                _clientWalkingNow = false;
                _clientAnimationPhase += 0.08;
            }

            ApplyClientTransform();
            _model.NotifyChanged();
        }

        public void SetTiaBarPassageBlocked(bool blocked)
        {
            _tiaBarPassageBlocked = blocked;
            _model.NotifyChanged();
        }

        private void AddCustomerNpc()
        {
            // Финальная базовая позиция Тиа в пространстве сцены.
            _clientWorldX = 250;
            _clientWorldZ = 300;
            _clientYaw = -1.45;

            Func<double, double> sy = y => -100.0 + (y + 100.0) * 0.833333333333;

            Color skin = Color.FromArgb(239, 219, 214);
            Color skinShade = Color.FromArgb(224, 198, 192);
            Color skinLight = Color.FromArgb(248, 230, 224);
            Color skinDark = Color.FromArgb(205, 176, 171);
            Color cheek = Color.FromArgb(231, 192, 194);
            Color hair = Color.FromArgb(78, 42, 40);
            Color hairDark = Color.FromArgb(52, 28, 27);
            Color hairLight = Color.FromArgb(103, 61, 57);
            Color dress = Color.FromArgb(72, 76, 84);
            Color dressLight = Color.FromArgb(96, 101, 110);
            Color skirt = Color.FromArgb(64, 68, 78);
            Color skirtDark = Color.FromArgb(42, 45, 54);
            Color tights = Color.FromArgb(76, 76, 84);
            Color pants = Color.FromArgb(34, 36, 42);
            Color pantsLight = Color.FromArgb(48, 52, 60);
            Color shoes = Color.FromArgb(20, 20, 24);
            Color eyeWhite = Color.FromArgb(244, 244, 244);
            Color irisOuter = Color.FromArgb(106, 140, 138);
            Color irisMid = Color.FromArgb(124, 156, 150);
            Color irisCore = Color.FromArgb(88, 116, 112);
            Color pupil = Color.FromArgb(20, 20, 22);
            Color brow = Color.FromArgb(88, 66, 60);
            Color lip = Color.FromArgb(226, 162, 170);
            Color lipDark = Color.FromArgb(190, 116, 132);
            Color lipLight = Color.FromArgb(244, 190, 198);
            Color nail = Color.FromArgb(228, 198, 198);
            Color noseShadow = Color.FromArgb(196, 166, 160);

            // =====================================================
            // ОБУВЬ / НОГИ / БЁДРА — ШИРЕ
            // =====================================================
            AddClientBlockLocal(-7.3, sy(-100.0), -7.9, -1.0, sy(-96.0), 9.9, shoes, FaceLayer.SmallDetail);
            AddClientBlockLocal(1.0, sy(-100.0), -7.9, 7.3, sy(-96.0), 9.9, shoes, FaceLayer.SmallDetail);
            AddClientBlockLocal(-5.6, sy(-96.4), 5.7, -1.7, sy(-95.0), 10.0, shoes, FaceLayer.SmallDetail);
            AddClientBlockLocal(1.7, sy(-96.4), 5.7, 5.6, sy(-95.0), 10.0, shoes, FaceLayer.SmallDetail);

            AddClientBlockLocal(-4.7, sy(-96.0), -3.1, -0.9, sy(-90.0), 3.8, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.9, sy(-96.0), -3.1, 4.7, sy(-90.0), 3.8, tights, FaceLayer.SmallDetail);

            AddClientBlockLocal(-6.1, sy(-90.2), -3.7, -0.7, sy(-80.8), 4.9, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.7, sy(-90.2), -3.7, 6.1, sy(-80.8), 4.9, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.0, sy(-86.5), -4.3, -0.4, sy(-72.8), 6.0, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.4, sy(-86.5), -4.3, 7.0, sy(-72.8), 6.0, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(-6.6, sy(-78.8), -4.4, -0.5, sy(-68.8), 5.6, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.5, sy(-78.8), -4.4, 6.6, sy(-68.8), 5.6, tights, FaceLayer.SmallDetail);

            AddClientBlockLocal(-6.6, sy(-69.0), -4.0, -0.6, sy(-62.2), 5.3, skinShade, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.6, sy(-69.0), -4.0, 6.6, sy(-62.2), 5.3, skinShade, FaceLayer.SmallDetail);

            AddClientBlockLocal(-9.2, sy(-63.0), -5.0, -0.7, sy(-48.0), 6.9, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.7, sy(-63.0), -5.0, 9.2, sy(-48.0), 6.9, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(-9.8, sy(-56.0), -5.5, -0.5, sy(-39.0), 7.4, tights, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.5, sy(-56.0), -5.5, 9.8, sy(-39.0), 7.4, tights, FaceLayer.SmallDetail);
            // Добавляем объём бёдрам и голеням.
            AddClientQuadLocal(-8.6, sy(-63.0), 5.8, -1.2, sy(-63.0), 5.8, -0.6, sy(-39.5), 7.0, -8.8, sy(-39.5), 7.0, tights, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.2, sy(-63.0), 5.8, 8.6, sy(-63.0), 5.8, 8.8, sy(-39.5), 7.0, 0.6, sy(-39.5), 7.0, tights, FaceLayer.SmallDetail);
            AddClientQuadLocal(-7.0, sy(-88.0), 4.2, -1.0, sy(-88.0), 4.2, -0.9, sy(-72.0), 5.5, -7.2, sy(-72.0), 5.5, tights, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.0, sy(-88.0), 4.2, 7.0, sy(-88.0), 4.2, 7.2, sy(-72.0), 5.5, 0.9, sy(-72.0), 5.5, tights, FaceLayer.SmallDetail);

            // Нижняя часть образа без юбки: оставляем штаны и убираем расширяющийся силуэт.
            // Две отдельные штанины + аккуратная зона таза, чтобы силуэт оставался человеческим,
            // но не выглядел как платье или юбка.
            AddClientBlockLocal(-8.8, sy(-47.0), -5.6, -0.7, sy(-17.0), 7.2, pants, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.7, sy(-47.0), -5.6, 8.8, sy(-17.0), 7.2, pants, FaceLayer.SmallDetail);
            AddClientBlockLocal(-9.8, sy(-39.0), -5.9, -0.5, sy(-22.0), 7.5, pantsLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.5, sy(-39.0), -5.9, 9.8, sy(-22.0), 7.5, pantsLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-6.2, sy(-22.0), -4.8, 6.2, sy(-16.8), 6.2, pantsLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-8.4, sy(-47.0), 6.1, -1.2, sy(-47.0), 6.1, -0.8, sy(-17.2), 6.8, -7.8, sy(-17.2), 6.8, pantsLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.2, sy(-47.0), 6.1, 8.4, sy(-47.0), 6.1, 7.8, sy(-17.2), 6.8, 0.8, sy(-17.2), 6.8, pantsLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.7, sy(-20.8), 6.3, -1.1, sy(-20.8), 6.6, -1.2, sy(-16.8), 6.9, -5.5, sy(-16.8), 6.8, pants, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.1, sy(-20.8), 6.6, 5.7, sy(-20.8), 6.3, 5.5, sy(-16.8), 6.8, 1.2, sy(-16.8), 6.9, pants, FaceLayer.SmallDetail);

            // =====================================================
            // ТОРС
            // =====================================================
            AddClientBlockLocal(-6.8, sy(-16.8), -4.4, 6.8, sy(-10.8), 5.0, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.8, sy(-10.8), -5.0, 7.8, sy(-3.0), 5.7, dress, FaceLayer.SmallDetail);
            AddClientBlockLocal(-8.0, sy(-3.0), -6.0, 8.0, sy(5.6), 6.7, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-9.0, sy(5.6), -5.2, 9.0, sy(10.5), 6.0, dress, FaceLayer.SmallDetail);

            AddClientBlockLocal(-5.6, sy(-1.5), 4.8, -0.7, sy(4.2), 7.8, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.7, sy(-1.5), 4.8, 5.6, sy(4.2), 7.8, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-2.8, sy(-2.0), 5.2, 2.8, sy(3.6), 7.5, dressLight, FaceLayer.SmallDetail);

            AddClientQuadLocal(-3.4, sy(9.0), 6.4, 3.4, sy(9.0), 6.4, 1.9, sy(5.4), 7.2, -1.9, sy(5.4), 7.2, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.2, sy(10.2), 5.9, -3.1, sy(10.2), 5.9, -2.5, sy(4.6), 6.6, -4.8, sy(4.6), 6.6, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.1, sy(10.2), 5.9, 5.2, sy(10.2), 5.9, 4.8, sy(4.6), 6.6, 2.5, sy(4.6), 6.6, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-9.4, sy(8.6), -4.6, 9.4, sy(12.3), 5.2, dress, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.9, sy(9.8), 5.2, 7.9, sy(11.8), 6.0, dressLight, FaceLayer.SmallDetail);
            // Дополнительная детализация тела: талия, грудь, ключицы, бёдра.
            AddClientQuadLocal(-8.9, sy(9.8), 5.9, -5.6, sy(9.8), 6.2, -4.1, sy(-2.8), 6.8, -7.0, sy(-8.6), 6.2, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.6, sy(9.8), 6.2, 8.9, sy(9.8), 5.9, 7.0, sy(-8.6), 6.2, 4.1, sy(-2.8), 6.8, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.1, sy(4.8), 6.7, 4.1, sy(4.8), 6.7, 2.8, sy(-1.8), 7.45, -2.8, sy(-1.8), 7.45, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.8, sy(-7.8), 6.5, 5.8, sy(-7.8), 6.5, 6.8, sy(-18.8), 7.0, -6.8, sy(-18.8), 7.0, dress, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.2, sy(10.5), 6.3, 3.2, sy(10.5), 6.3, 1.7, sy(7.0), 7.0, -1.7, sy(7.0), 7.0, skinLight, FaceLayer.SmallDetail);

            // =====================================================
            // РУКИ + РУКАВА
            // =====================================================
            // Плечи / рукава — увеличены примерно в 2 раза по ширине и толщине,
            // чтобы плечевой пояс соответствовал размеру основных рук.
            AddClientBlockLocal(-15.2, sy(1.0), -4.8, -8.0, sy(11.0), 5.4, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(8.0, sy(1.0), -4.8, 15.2, sy(11.0), 5.4, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-15.4, sy(-2.2), -4.2, -9.0, sy(3.2), 4.8, dress, FaceLayer.SmallDetail);
            AddClientBlockLocal(9.0, sy(-2.2), -4.2, 15.4, sy(3.2), 4.8, dress, FaceLayer.SmallDetail);
            AddClientBlockLocal(-14.8, sy(4.6), 3.9, -8.2, sy(11.4), 6.6, dressLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(8.2, sy(4.6), 3.9, 14.8, sy(11.4), 6.6, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-7.9, sy(10.0), 5.9, -15.8, sy(9.6), 4.5, -15.0, sy(3.2), 4.7, -8.6, sy(3.8), 5.9, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(15.8, sy(9.6), 4.5, 7.9, sy(10.0), 5.9, 8.6, sy(3.8), 5.9, 15.0, sy(3.2), 4.7, dressLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-15.4, sy(2.2), -4.4, -9.0, sy(2.4), -4.6, -8.2, sy(10.4), -3.8, -15.0, sy(9.8), -3.7, dress, FaceLayer.SmallDetail);
            AddClientQuadLocal(9.0, sy(2.4), -4.6, 15.4, sy(2.2), -4.4, 15.0, sy(9.8), -3.7, 8.2, sy(10.4), -3.8, dress, FaceLayer.SmallDetail);

            // Кожа рук ниже рукава.
            AddClientBlockLocal(-14.5, sy(-6.0), -2.9, -10.0, sy(0.8), 3.6, skinShade, FaceLayer.SmallDetail);
            int tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(10.0, sy(-6.0), -2.9, 14.5, sy(0.8), 3.6, skinShade, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            AddClientBlockLocal(-14.4, sy(-10.0), -3.0, -9.9, sy(-6.0), 3.7, skinDark, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(9.9, sy(-10.0), -3.0, 14.4, sy(-6.0), 3.7, skinDark, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            AddClientBlockLocal(-14.1, sy(-22.0), -2.7, -10.3, sy(-10.0), 3.4, skin, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(10.3, sy(-22.0), -2.7, 14.1, sy(-10.0), 3.4, skin, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            AddClientBlockLocal(-13.2, sy(-25.6), -2.1, -10.5, sy(-22.0), 2.8, skinShade, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(10.5, sy(-25.6), -2.1, 13.2, sy(-22.0), 2.8, skinShade, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            AddClientBlockLocal(-13.6, sy(-29.8), -1.8, -10.0, sy(-25.6), 2.9, skinShade, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(10.0, sy(-29.8), -1.8, 13.6, sy(-25.6), 2.9, skinShade, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            // Сглаживаем руки и добавляем чуть более человеческий объём локтям и предплечьям.
            AddClientQuadLocal(-13.6, sy(-9.4), 2.9, -10.9, sy(-9.4), 2.9, -11.1, sy(-16.4), 3.2, -13.4, sy(-16.4), 3.2, skinLight, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientQuadLocal(10.9, sy(-9.4), 2.9, 13.6, sy(-9.4), 2.9, 13.4, sy(-16.4), 3.2, 11.1, sy(-16.4), 3.2, skinLight, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            AddClientQuadLocal(-13.2, sy(-16.2), 3.1, -11.0, sy(-16.2), 3.1, -11.4, sy(-24.8), 2.8, -12.9, sy(-24.8), 2.8, skin, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientQuadLocal(11.0, sy(-16.2), 3.1, 13.2, sy(-16.2), 3.1, 12.9, sy(-24.8), 2.8, 11.4, sy(-24.8), 2.8, skin, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            AddClientBlockLocal(-13.5, sy(-32.2), 1.0, -12.8, sy(-30.0), 2.7, nail, FaceLayer.SmallDetail);
            AddClientBlockLocal(-12.6, sy(-32.3), 1.0, -11.9, sy(-30.0), 2.7, nail, FaceLayer.SmallDetail);
            AddClientBlockLocal(-11.7, sy(-32.2), 1.0, -11.0, sy(-30.0), 2.7, nail, FaceLayer.SmallDetail);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(11.0, sy(-32.2), 1.0, 11.7, sy(-30.0), 2.7, nail, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(11.9, sy(-32.3), 1.0, 12.6, sy(-30.0), 2.7, nail, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);
            tiaRightForearmSegmentStart = _model.Points.Count;
            AddClientBlockLocal(12.8, sy(-32.2), 1.0, 13.5, sy(-30.0), 2.7, nail, FaceLayer.SmallDetail);
            RegisterLocalPoints(tiaRightForearmSegmentStart, _model.Points.Count, _tiaBentArmPoints, _tiaBentArmLocalX, _tiaBentArmLocalY, _tiaBentArmLocalZ);

            // =====================================================
            // ШЕЯ И ГОЛОВА
            // =====================================================
            AddClientBlockLocal(-2.2, sy(12.4), -2.4, 2.2, sy(17.0), 2.6, skinShade, FaceLayer.SmallDetail);

            // Задняя часть головы.
            AddClientBlockLocal(-4.8, sy(17.2), -5.2, 4.8, sy(30.8), 2.0, skin, FaceLayer.SmallDetail);

            // Лицо разбито на ещё более мелкие ступени, чтобы убрать квадратную челюсть.
            // Нижняя часть собирается в плавный овал: подбородок уже, затем щеки постепенно расширяются.
            AddClientBlockLocal(-2.15, sy(16.35), 5.16, 2.15, sy(17.35), 6.00, skin, FaceLayer.SmallDetail); // низ подбородка
            AddClientBlockLocal(-2.70, sy(17.30), 5.13, 2.70, sy(18.30), 6.00, skin, FaceLayer.SmallDetail); // подбородок
            AddClientBlockLocal(-3.20, sy(18.25), 5.10, 3.20, sy(19.35), 6.04, skin, FaceLayer.SmallDetail); // мягкий переход от подбородка
            AddClientBlockLocal(-3.70, sy(19.30), 5.06, 3.70, sy(20.45), 6.10, skin, FaceLayer.SmallDetail); // нижняя челюсть без углов
            AddClientBlockLocal(-4.10, sy(20.40), 5.02, 4.10, sy(21.70), 6.16, skin, FaceLayer.SmallDetail); // челюсть и нижние щёки
            AddClientBlockLocal(-4.45, sy(21.65), 4.99, 4.45, sy(23.10), 6.22, skin, FaceLayer.SmallDetail); // щёки плавнее
            AddClientBlockLocal(-4.75, sy(23.00), 4.97, 4.75, sy(24.65), 6.28, skin, FaceLayer.SmallDetail); // средние щёки
            AddClientBlockLocal(-4.98, sy(24.55), 4.95, 4.98, sy(26.20), 6.30, skin, FaceLayer.SmallDetail); // верх щёк
            AddClientBlockLocal(-5.10, sy(26.10), 4.90, 5.10, sy(27.75), 6.18, skin, FaceLayer.SmallDetail); // скулы мягче
            AddClientBlockLocal(-4.96, sy(27.70), 4.84, 4.96, sy(29.35), 5.98, skin, FaceLayer.SmallDetail); // виски
            AddClientBlockLocal(-4.86, sy(29.35), 4.72, 4.86, sy(31.45), 5.66, skin, FaceLayer.SmallDetail); // лоб

            // Дополнительные боковые грани лица для цельного овального силуэта,
            // заполненных скул без разрывов и более мягкой линии щёк/подбородка.
            AddClientQuadLocal(-5.15, sy(26.0), 6.18, -3.95, sy(23.8), 6.10, -2.95, sy(20.4), 5.92, -4.35, sy(21.2), 5.96, Color.FromArgb(241, 227, 223), FaceLayer.SmallDetail);
            AddClientQuadLocal(3.95, sy(23.8), 6.10, 5.15, sy(26.0), 6.18, 4.35, sy(21.2), 5.96, 2.95, sy(20.4), 5.92, Color.FromArgb(241, 227, 223), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.75, sy(23.10), 6.12, -3.45, sy(20.20), 6.00, -2.65, sy(18.05), 5.78, -3.95, sy(18.55), 5.84, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.45, sy(20.20), 6.00, 4.75, sy(23.10), 6.12, 3.95, sy(18.55), 5.84, 2.65, sy(18.05), 5.78, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.45, sy(19.50), 5.88, -3.10, sy(17.00), 5.64, -2.05, sy(15.45), 5.30, -3.55, sy(15.70), 5.42, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.10, sy(17.00), 5.64, 4.45, sy(19.50), 5.88, 3.55, sy(15.70), 5.42, 2.05, sy(15.45), 5.30, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.25, sy(27.2), 6.02, -5.35, sy(29.5), 5.82, -4.85, sy(31.5), 5.38, -5.35, sy(28.0), 5.92, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.35, sy(29.5), 5.82, 6.25, sy(27.2), 6.02, 5.35, sy(28.0), 5.92, 4.85, sy(31.5), 5.38, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.65, sy(24.85), 6.26, -4.65, sy(22.10), 6.08, -3.75, sy(18.75), 5.92, -5.10, sy(19.50), 5.98, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.65, sy(22.10), 6.08, 5.65, sy(24.85), 6.26, 5.10, sy(19.50), 5.98, 3.75, sy(18.75), 5.92, skin, FaceLayer.SmallDetail);

            // Уши.
            AddClientBlockLocal(-7.0, sy(22.6), -1.3, -5.9, sy(26.5), 1.8, skinShade, FaceLayer.SmallDetail);
            AddClientBlockLocal(5.9, sy(22.6), -1.3, 7.0, sy(26.5), 1.8, skinShade, FaceLayer.SmallDetail);

            // Боковой профиль лица: заполняем щёку, скулу, челюсть и переход к шее,
            // чтобы сбоку лицо не выглядело отдельной плоской маской.
            AddClientQuadLocal(-6.35, sy(27.0), 1.0, -5.05, sy(26.7), 5.85, -4.45, sy(23.0), 6.15, -6.45, sy(22.5), 1.05, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.05, sy(26.7), 5.85, 6.35, sy(27.0), 1.0, 6.45, sy(22.5), 1.05, 4.45, sy(23.0), 6.15, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.45, sy(22.6), 1.05, -4.45, sy(23.0), 6.15, -3.25, sy(19.0), 5.82, -5.95, sy(18.2), 0.9, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.45, sy(23.0), 6.15, 6.45, sy(22.6), 1.05, 5.95, sy(18.2), 0.9, 3.25, sy(19.0), 5.82, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.55, sy(24.5), 0.7, -5.20, sy(24.2), 4.4, -5.05, sy(22.2), 4.7, -6.65, sy(21.8), 0.55, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.20, sy(24.2), 4.4, 6.55, sy(24.5), 0.7, 6.65, sy(21.8), 0.55, 5.05, sy(22.2), 4.7, skinLight, FaceLayer.SmallDetail);
            // Аккуратная деталь профиля только на щеке/скуле, без удлинения шеи вниз.
            AddClientQuadLocal(-5.70, sy(21.8), 4.60, -4.50, sy(21.4), 6.10, -3.55, sy(19.0), 5.82, -5.25, sy(19.0), 4.20, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.50, sy(21.4), 6.10, 5.70, sy(21.8), 4.60, 5.25, sy(19.0), 4.20, 3.55, sy(19.0), 5.82, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.95, sy(21.2), 5.92, -4.65, sy(21.2), 6.55, -4.25, sy(19.9), 6.45, -5.55, sy(19.7), 5.60, lipDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.65, sy(21.2), 6.55, 5.95, sy(21.2), 5.92, 5.55, sy(19.7), 5.60, 4.25, sy(19.9), 6.45, lipDark, FaceLayer.SmallDetail);
            // Мелкие skin-плитки для округления подбородка, нижней челюсти и скул.
            AddClientQuadLocal(-3.25, sy(18.6), 7.18, -2.25, sy(18.25), 7.22, -1.95, sy(16.95), 7.27, -2.95, sy(17.20), 7.24, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.25, sy(18.25), 7.22, 3.25, sy(18.6), 7.18, 2.95, sy(17.20), 7.24, 1.95, sy(16.95), 7.27, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.45, sy(17.35), 7.25, -0.95, sy(17.00), 7.30, -0.55, sy(15.90), 7.34, -1.85, sy(15.85), 7.32, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(0.95, sy(17.00), 7.30, 2.45, sy(17.35), 7.25, 1.85, sy(15.85), 7.32, 0.55, sy(15.90), 7.34, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.55, sy(24.65), 7.04, -4.60, sy(24.40), 7.10, -4.10, sy(22.55), 7.13, -5.20, sy(22.20), 7.08, Color.FromArgb(243, 229, 225), FaceLayer.SmallDetail);
            AddClientQuadLocal(4.60, sy(24.40), 7.10, 5.55, sy(24.65), 7.04, 5.20, sy(22.20), 7.08, 4.10, sy(22.55), 7.13, Color.FromArgb(243, 229, 225), FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.20, sy(22.15), 7.08, -4.30, sy(21.75), 7.12, -3.60, sy(20.10), 7.14, -4.70, sy(19.80), 7.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.30, sy(21.75), 7.12, 5.20, sy(22.15), 7.08, 4.70, sy(19.80), 7.10, 3.60, sy(20.10), 7.14, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.20, sy(25.4), 5.0, -5.65, sy(25.6), 6.25, -5.50, sy(22.6), 6.20, -6.05, sy(22.4), 4.8, noseShadow, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.65, sy(25.6), 6.25, 6.20, sy(25.4), 5.0, 6.05, sy(22.4), 4.8, 5.50, sy(22.6), 6.20, noseShadow, FaceLayer.SmallDetail);

            // Дополнительный слой мелких плиток: они перекрывают резкие ступени
            // нижней челюсти и собирают подбородок/скулы в почти ровный овал.
            AddClientQuadLocal(-0.30, sy(14.95), 7.44, 0.30, sy(14.95), 7.44, 0.22, sy(14.62), 7.46, -0.22, sy(14.62), 7.46, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.82, sy(15.35), 7.45, 0.82, sy(15.35), 7.45, 0.60, sy(14.95), 7.47, -0.60, sy(14.95), 7.47, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.34, sy(15.82), 7.46, 1.34, sy(15.82), 7.46, 1.05, sy(15.32), 7.48, -1.05, sy(15.32), 7.48, Color.FromArgb(250, 236, 232), FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.90, sy(16.35), 7.46, 1.90, sy(16.35), 7.46, 1.55, sy(15.78), 7.49, -1.55, sy(15.78), 7.49, Color.FromArgb(248, 234, 231), FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.42, sy(16.95), 7.47, 2.42, sy(16.95), 7.47, 2.05, sy(16.32), 7.50, -2.05, sy(16.32), 7.50, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.92, sy(17.62), 7.47, 2.92, sy(17.62), 7.47, 2.55, sy(16.90), 7.50, -2.55, sy(16.90), 7.50, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.35, sy(18.32), 7.46, 3.35, sy(18.32), 7.46, 3.02, sy(17.58), 7.49, -3.02, sy(17.58), 7.49, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.72, sy(19.05), 7.44, -2.30, sy(19.05), 7.44, -2.64, sy(18.18), 7.48, -3.50, sy(18.18), 7.48, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.30, sy(19.05), 7.44, 3.72, sy(19.05), 7.44, 3.50, sy(18.18), 7.48, 2.64, sy(18.18), 7.48, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.18, sy(19.92), 7.39, -3.10, sy(19.92), 7.39, -3.36, sy(18.95), 7.45, -4.02, sy(18.95), 7.45, Color.FromArgb(242, 225, 222), FaceLayer.SmallDetail);
            AddClientQuadLocal(3.10, sy(19.92), 7.39, 4.18, sy(19.92), 7.39, 4.02, sy(18.95), 7.45, 3.36, sy(18.95), 7.45, Color.FromArgb(242, 225, 222), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.76, sy(21.00), 7.33, -3.92, sy(21.00), 7.33, -4.08, sy(19.82), 7.40, -4.62, sy(19.82), 7.40, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.92, sy(21.00), 7.33, 4.76, sy(21.00), 7.33, 4.62, sy(19.82), 7.40, 4.08, sy(19.82), 7.40, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.18, sy(22.35), 7.27, -4.46, sy(22.35), 7.27, -4.50, sy(20.92), 7.34, -5.00, sy(20.92), 7.34, Color.FromArgb(244, 228, 224), FaceLayer.SmallDetail);
            AddClientQuadLocal(4.46, sy(22.35), 7.27, 5.18, sy(22.35), 7.27, 5.00, sy(20.92), 7.34, 4.50, sy(20.92), 7.34, Color.FromArgb(244, 228, 224), FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.60, sy(24.10), 7.20, -4.96, sy(24.10), 7.20, -4.86, sy(22.28), 7.29, -5.36, sy(22.28), 7.29, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.96, sy(24.10), 7.20, 5.60, sy(24.10), 7.20, 5.36, sy(22.28), 7.29, 4.86, sy(22.28), 7.29, skinLight, FaceLayer.SmallDetail);

            // Усиленное скругление нижней части лица поверх предыдущей формы:
            // добавляем много мелких плиток, чтобы подбородок, щеки, скулы и нижняя челюсть
            // читались как плавный овал без острых диагоналей.
            AddClientQuadLocal(-0.42, sy(14.62), 7.45, 0.42, sy(14.62), 7.45, 0.32, sy(14.26), 7.47, -0.32, sy(14.26), 7.47, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.96, sy(15.02), 7.46, 0.96, sy(15.02), 7.46, 0.76, sy(14.62), 7.48, -0.76, sy(14.62), 7.48, Color.FromArgb(249, 235, 231), FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.54, sy(15.42), 7.47, 1.54, sy(15.42), 7.47, 1.28, sy(14.98), 7.49, -1.28, sy(14.98), 7.49, Color.FromArgb(249, 235, 231), FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.12, sy(15.90), 7.48, 2.12, sy(15.90), 7.48, 1.84, sy(15.40), 7.50, -1.84, sy(15.40), 7.50, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.66, sy(16.45), 7.48, 2.66, sy(16.45), 7.48, 2.36, sy(15.90), 7.51, -2.36, sy(15.90), 7.51, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.18, sy(17.02), 7.48, 3.18, sy(17.02), 7.48, 2.88, sy(16.42), 7.51, -2.88, sy(16.42), 7.51, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.66, sy(17.66), 7.47, 3.66, sy(17.66), 7.47, 3.36, sy(17.00), 7.50, -3.36, sy(17.00), 7.50, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.08, sy(18.36), 7.45, 4.08, sy(18.36), 7.45, 3.82, sy(17.62), 7.49, -3.82, sy(17.62), 7.49, Color.FromArgb(246, 230, 226), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.46, sy(19.12), 7.42, 4.46, sy(19.12), 7.42, 4.22, sy(18.34), 7.47, -4.22, sy(18.34), 7.47, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.82, sy(19.98), 7.38, 4.82, sy(19.98), 7.38, 4.60, sy(19.14), 7.44, -4.60, sy(19.14), 7.44, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.12, sy(20.94), 7.34, 5.12, sy(20.94), 7.34, 4.92, sy(20.04), 7.41, -4.92, sy(20.04), 7.41, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.36, sy(21.96), 7.29, 5.36, sy(21.96), 7.29, 5.16, sy(21.02), 7.36, -5.16, sy(21.02), 7.36, Color.FromArgb(244, 228, 224), FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.58, sy(23.08), 7.24, -4.96, sy(23.08), 7.24, -4.96, sy(21.96), 7.31, -5.38, sy(21.96), 7.31, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.96, sy(23.08), 7.24, 5.58, sy(23.08), 7.24, 5.38, sy(21.96), 7.31, 4.96, sy(21.96), 7.31, skinLight, FaceLayer.SmallDetail);

            // Дополнительные боковые смягчающие плитки убирают "угол" между щекой и челюстью.
            AddClientQuadLocal(-4.92, sy(20.22), 7.18, -3.84, sy(19.92), 7.26, -3.24, sy(18.00), 7.30, -4.36, sy(17.78), 7.24, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.84, sy(19.92), 7.26, 4.92, sy(20.22), 7.18, 4.36, sy(17.78), 7.24, 3.24, sy(18.00), 7.30, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.22, sy(22.38), 7.12, -4.18, sy(22.10), 7.20, -3.54, sy(20.20), 7.25, -4.70, sy(19.96), 7.18, Color.FromArgb(243, 229, 225), FaceLayer.SmallDetail);
            AddClientQuadLocal(4.18, sy(22.10), 7.20, 5.22, sy(22.38), 7.12, 4.70, sy(19.96), 7.18, 3.54, sy(20.20), 7.25, Color.FromArgb(243, 229, 225), FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.42, sy(24.28), 7.06, -4.56, sy(24.12), 7.15, -4.08, sy(22.38), 7.20, -5.02, sy(22.24), 7.12, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.56, sy(24.12), 7.15, 5.42, sy(24.28), 7.06, 5.02, sy(22.24), 7.12, 4.08, sy(22.38), 7.20, skinLight, FaceLayer.SmallDetail);
            // Ещё один слой мелких перекрытий срезает квадратные углы у челюсти и делает низ лица овальнее.
            AddClientQuadLocal(-4.34, sy(20.78), 7.26, -3.24, sy(20.42), 7.30, -2.42, sy(17.92), 7.34, -3.52, sy(17.48), 7.28, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.24, sy(20.42), 7.30, 4.34, sy(20.78), 7.26, 3.52, sy(17.48), 7.28, 2.42, sy(17.92), 7.34, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.64, sy(22.28), 7.20, -3.56, sy(21.96), 7.24, -2.86, sy(19.68), 7.30, -3.96, sy(19.22), 7.23, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.56, sy(21.96), 7.24, 4.64, sy(22.28), 7.20, 3.96, sy(19.22), 7.23, 2.86, sy(19.68), 7.30, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.82, sy(23.92), 7.13, -3.94, sy(23.70), 7.18, -3.36, sy(21.84), 7.24, -4.28, sy(21.58), 7.18, Color.FromArgb(245, 230, 226), FaceLayer.SmallDetail);
            AddClientQuadLocal(3.94, sy(23.70), 7.18, 4.82, sy(23.92), 7.13, 4.28, sy(21.58), 7.18, 3.36, sy(21.84), 7.24, Color.FromArgb(245, 230, 226), FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.08, sy(17.10), 7.36, 2.08, sy(17.10), 7.36, 1.72, sy(16.30), 7.40, -1.72, sy(16.30), 7.40, Color.FromArgb(248, 234, 230), FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.34, sy(16.30), 7.40, 1.34, sy(16.30), 7.40, 1.06, sy(15.45), 7.43, -1.06, sy(15.45), 7.43, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.82, sy(15.52), 7.42, 0.82, sy(15.52), 7.42, 0.58, sy(14.86), 7.45, -0.58, sy(14.86), 7.45, skinLight, FaceLayer.SmallDetail);
            // Два узких кожных перекрытия в промежутке между глазами и бровями:
            // закрывают коричневые вертикальные просветы у внешних краёв глаз.
            AddClientQuadLocal(-5.34, sy(27.78), 7.50, -4.76, sy(27.78), 7.50, -4.78, sy(27.10), 7.50, -5.32, sy(27.10), 7.50, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.76, sy(27.78), 7.50, 5.34, sy(27.78), 7.50, 5.32, sy(27.10), 7.50, 4.78, sy(27.10), 7.50, skin, FaceLayer.SmallDetail);

            // =====================================================
            // ВОЛОСЫ — ГОРАЗДО БОЛЬШЕ И КРУГЛЕЕ
            // =====================================================
            // Большая шапка волос сверху и сзади.
            AddClientBlockLocal(-8.4, sy(30.2), -9.8, 8.4, sy(35.0), 4.8, hairDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(-8.0, sy(26.0), -10.8, 8.0, sy(31.0), -3.0, hairDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.6, sy(20.0), -10.5, 7.6, sy(26.8), -4.0, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.0, sy(11.0), -9.6, 7.0, sy(20.5), -4.4, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(-6.2, sy(2.0), -8.7, 6.2, sy(11.0), -3.7, hair, FaceLayer.SmallDetail);

            // Усиленный затылок и задние пряди: закрываем заметную залысину сзади.
            // Делали объём глубже по Z и в несколько слоёв, чтобы волосы не терялись
            // в соседних текстурах при уходе Тии спиной к игроку.
            AddClientBlockLocal(-8.1, sy(25.4), -13.6, 8.1, sy(35.4), -5.2, hairDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.8, sy(16.8), -13.2, 7.8, sy(26.2), -5.7, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(-7.0, sy(8.0), -12.8, 7.0, sy(18.0), -5.8, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(-6.2, sy(0.8), -11.6, 6.2, sy(9.2), -5.7, hairDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-7.1, sy(27.2), -5.55, 7.1, sy(27.2), -5.55, 6.4, sy(23.6), -8.95, -6.4, sy(23.6), -8.95, hairDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.8, sy(17.8), -5.85, 6.8, sy(17.8), -5.85, 7.0, sy(13.0), -9.85, -7.0, sy(13.0), -9.85, hair, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.0, sy(8.8), -5.95, 6.0, sy(8.8), -5.95, 6.3, sy(4.0), -8.85, -6.3, sy(4.0), -8.85, hairDark, FaceLayer.SmallDetail);

            // Боковые задние секции, чтобы при повороте головы не было просветов между
            // висками, боковыми волосами и затылком.
            AddClientBlockLocal(-9.2, sy(13.5), -11.8, -6.8, sy(31.8), -4.9, hairDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(6.8, sy(13.5), -11.8, 9.2, sy(31.8), -4.9, hairDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-8.9, sy(30.8), -4.9, -6.9, sy(30.8), -4.9, -7.2, sy(24.0), -8.3, -9.0, sy(24.0), -8.3, hair, FaceLayer.SmallDetail);
            AddClientQuadLocal(6.9, sy(30.8), -4.9, 8.9, sy(30.8), -4.9, 9.0, sy(24.0), -8.3, 7.2, sy(24.0), -8.3, hair, FaceLayer.SmallDetail);

            // Скругление макушки.
            AddClientBlockLocal(-7.2, sy(32.0), -7.0, 7.2, sy(36.2), 2.8, hairLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-5.8, sy(34.2), -5.2, 5.8, sy(37.0), 1.8, hairLight, FaceLayer.SmallDetail);

            // Боковые массы волос: НИЖЕ ГЛАЗ волосы полностью уведены назад,
            // чтобы спереди и сбоку у лица были видны щека, скула и линия подбородка.
            // Ниже глаз рядом с лицом больше нет никаких передних прядей.
            AddClientBlockLocal(-9.6, sy(21.0), -8.2, -7.2, sy(31.8), -1.4, hairDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(7.2, sy(21.0), -8.2, 9.6, sy(31.8), -1.4, hairDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(-9.3, sy(14.8), -8.8, -7.0, sy(24.5), -2.4, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(7.0, sy(14.8), -8.8, 9.3, sy(24.5), -2.4, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(-8.5, sy(8.5), -8.2, -6.8, sy(17.5), -2.8, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(6.8, sy(8.5), -8.2, 8.5, sy(17.5), -2.8, hair, FaceLayer.SmallDetail);

            // Над глазами / над лбом оставляем только верхнюю линию волос.
            // На щёки, скулы и область ниже глаз волосы не заходят вообще.
            AddClientQuadLocal(-5.8, sy(32.6), 6.15, 5.8, sy(32.6), 6.15, 4.1, sy(30.6), 6.75, -4.1, sy(30.6), 6.75, hairLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.7, sy(31.1), 6.10, -3.5, sy(30.9), 6.55, -3.8, sy(29.7), 6.62, -5.9, sy(29.7), 6.18, hairLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.5, sy(30.9), 6.55, 5.7, sy(31.1), 6.10, 5.9, sy(29.7), 6.18, 3.8, sy(29.7), 6.62, hairLight, FaceLayer.SmallDetail);

            // =====================================================
            // ЛИЦО
            // =====================================================
            // Нос — ещё меньше.
            AddClientBlockLocal(-0.22, sy(22.9), 6.8, 0.22, sy(25.0), 7.6, noseShadow, FaceLayer.SmallDetail);
            AddClientBlockLocal(-0.14, sy(22.2), 6.8, 0.14, sy(22.9), 7.2, skinDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(-0.30, sy(22.0), 6.9, -0.05, sy(22.4), 7.3, skinDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(0.05, sy(22.0), 6.9, 0.30, sy(22.4), 7.3, skinDark, FaceLayer.SmallDetail);

            // Глаза и брови ещё немного ниже.
            AddClientQuadLocal(-4.65, sy(27.15), 7.08, -2.25, sy(27.15), 7.08, -2.25, sy(24.15), 7.08, -4.65, sy(24.15), 7.08, eyeWhite, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.15, sy(26.55), 7.09, -4.65, sy(26.55), 7.09, -4.65, sy(24.75), 7.09, -5.15, sy(24.75), 7.09, eyeWhite, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.25, sy(26.55), 7.09, -1.75, sy(26.55), 7.09, -1.75, sy(24.75), 7.09, -2.25, sy(24.75), 7.09, eyeWhite, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.15, sy(27.15), 7.10, -4.65, sy(27.15), 7.10, -4.65, sy(26.55), 7.10, -5.15, sy(26.55), 7.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.25, sy(27.15), 7.10, -1.75, sy(27.15), 7.10, -1.75, sy(26.55), 7.10, -2.25, sy(26.55), 7.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.15, sy(24.75), 7.10, -4.65, sy(24.75), 7.10, -4.65, sy(24.15), 7.10, -5.15, sy(24.15), 7.10, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.25, sy(24.75), 7.10, -1.75, sy(24.75), 7.10, -1.75, sy(24.15), 7.10, -2.25, sy(24.15), 7.10, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.25, sy(27.15), 7.08, 4.65, sy(27.15), 7.08, 4.65, sy(24.15), 7.08, 2.25, sy(24.15), 7.08, eyeWhite, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.75, sy(26.55), 7.09, 2.25, sy(26.55), 7.09, 2.25, sy(24.75), 7.09, 1.75, sy(24.75), 7.09, eyeWhite, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.65, sy(26.55), 7.09, 5.15, sy(26.55), 7.09, 5.15, sy(24.75), 7.09, 4.65, sy(24.75), 7.09, eyeWhite, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.75, sy(27.15), 7.10, 2.25, sy(27.15), 7.10, 2.25, sy(26.55), 7.10, 1.75, sy(26.55), 7.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.65, sy(27.15), 7.10, 5.15, sy(27.15), 7.10, 5.15, sy(26.55), 7.10, 4.65, sy(26.55), 7.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.75, sy(24.75), 7.10, 2.25, sy(24.75), 7.10, 2.25, sy(24.15), 7.10, 1.75, sy(24.15), 7.10, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.65, sy(24.75), 7.10, 5.15, sy(24.75), 7.10, 5.15, sy(24.15), 7.10, 4.65, sy(24.15), 7.10, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.5, sy(26.4), 7.17, -2.5, sy(26.4), 7.17, -2.5, sy(24.7), 7.17, -4.5, sy(24.7), 7.17, irisOuter, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.5, sy(26.4), 7.17, 4.5, sy(26.4), 7.17, 4.5, sy(24.7), 7.17, 2.5, sy(24.7), 7.17, irisOuter, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.2, sy(26.1), 7.22, -2.9, sy(26.1), 7.22, -2.9, sy(24.9), 7.22, -4.2, sy(24.9), 7.22, irisMid, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.9, sy(26.1), 7.22, 4.2, sy(26.1), 7.22, 4.2, sy(24.9), 7.22, 2.9, sy(24.9), 7.22, irisMid, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.95, sy(25.95), 7.27, -3.2, sy(25.95), 7.27, -3.2, sy(25.15), 7.27, -3.95, sy(25.15), 7.27, irisCore, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.2, sy(25.95), 7.27, 3.95, sy(25.95), 7.27, 3.95, sy(25.15), 7.27, 3.2, sy(25.15), 7.27, irisCore, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.90, sy(25.92), 7.33, -3.32, sy(25.92), 7.33, -3.32, sy(25.28), 7.33, -3.90, sy(25.28), 7.33, pupil, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.32, sy(25.92), 7.33, 3.90, sy(25.92), 7.33, 3.90, sy(25.28), 7.33, 3.32, sy(25.28), 7.33, pupil, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.28, sy(26.38), 7.37, -4.02, sy(26.38), 7.37, -4.02, sy(26.05), 7.37, -4.28, sy(26.05), 7.37, Color.White, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.72, sy(26.38), 7.37, 3.98, sy(26.38), 7.37, 3.98, sy(26.05), 7.37, 3.72, sy(26.05), 7.37, Color.White, FaceLayer.SmallDetail);
            // Ресницы.
            AddClientQuadLocal(-5.05, sy(27.00), 7.40, -2.10, sy(27.00), 7.40, -2.35, sy(26.55), 7.40, -4.80, sy(26.55), 7.40, brow, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.10, sy(27.00), 7.40, 5.05, sy(27.00), 7.40, 4.80, sy(26.55), 7.40, 2.35, sy(26.55), 7.40, brow, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.20, sy(26.55), 7.38, -4.95, sy(26.55), 7.38, -4.95, sy(26.15), 7.38, -5.20, sy(26.15), 7.38, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.20, sy(26.55), 7.38, -1.95, sy(26.55), 7.38, -1.95, sy(26.15), 7.38, -2.20, sy(26.15), 7.38, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.95, sy(26.55), 7.38, 2.20, sy(26.55), 7.38, 2.20, sy(26.15), 7.38, 1.95, sy(26.15), 7.38, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.95, sy(26.55), 7.38, 5.20, sy(26.55), 7.38, 5.20, sy(26.15), 7.38, 4.95, sy(26.15), 7.38, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.05, sy(28.52), 6.96, -2.45, sy(28.88), 6.96, -2.78, sy(28.12), 6.96, -4.82, sy(27.82), 6.96, brow, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.45, sy(28.88), 6.96, 5.05, sy(28.52), 6.96, 4.82, sy(27.82), 6.96, 2.78, sy(28.12), 6.96, brow, FaceLayer.SmallDetail);
            // Поверхностные кожные заплатки в зазоре между глазом и бровью: убирают
            // коричневые палочки, если боковые текстуры/ресницы просвечивают под углом.
            AddClientQuadLocal(-5.38, sy(27.78), 7.52, -4.72, sy(27.78), 7.52, -4.74, sy(27.12), 7.52, -5.36, sy(27.12), 7.52, Color.FromArgb(246, 229, 225), FaceLayer.SmallDetail);
            AddClientQuadLocal(4.72, sy(27.78), 7.52, 5.38, sy(27.78), 7.52, 5.36, sy(27.12), 7.52, 4.74, sy(27.12), 7.52, Color.FromArgb(246, 229, 225), FaceLayer.SmallDetail);

            // Щёки.
            AddClientQuadLocal(-5.8, sy(23.8), 6.9, -4.6, sy(23.8), 6.9, -4.6, sy(22.5), 6.9, -5.8, sy(22.5), 6.9, cheek, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.6, sy(23.8), 6.9, 5.8, sy(23.8), 6.9, 5.8, sy(22.5), 6.9, 4.6, sy(22.5), 6.9, cheek, FaceLayer.SmallDetail);

            // Губы — меньше и детальнее.
            AddClientQuadLocal(-1.95, sy(20.55), 7.08, -0.35, sy(20.55), 7.08, -0.55, sy(20.00), 7.08, -1.55, sy(20.00), 7.08, lip, FaceLayer.SmallDetail);
            AddClientQuadLocal(0.35, sy(20.55), 7.08, 1.95, sy(20.55), 7.08, 1.55, sy(20.00), 7.08, 0.55, sy(20.00), 7.08, lip, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.55, sy(20.58), 7.10, 0.55, sy(20.58), 7.10, 0.20, sy(20.05), 7.10, -0.20, sy(20.05), 7.10, lipDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.65, sy(20.00), 7.14, 1.65, sy(20.00), 7.14, 1.15, sy(19.15), 7.18, -1.15, sy(19.15), 7.18, lipLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.25, sy(19.55), 7.21, 1.25, sy(19.55), 7.21, 0.88, sy(18.78), 7.23, -0.88, sy(18.78), 7.23, lipDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.82, sy(19.78), 7.20, 0.82, sy(19.78), 7.20, 0.42, sy(19.42), 7.22, -0.42, sy(19.42), 7.22, lipLight, FaceLayer.SmallDetail);

            // =====================================================
            // ДОПОЛНИТЕЛЬНАЯ ДЕТАЛИЗАЦИЯ ЛИЦА ТИА
            // =====================================================

            // Скулы, виски и более узкая челюсть — чтобы лицо выглядело менее квадратным.
            AddClientQuadLocal(-6.15, sy(30.1), 6.70, -5.10, sy(30.1), 6.85, -5.25, sy(27.9), 6.98, -6.25, sy(27.6), 6.82, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.10, sy(30.1), 6.85, 6.15, sy(30.1), 6.70, 6.25, sy(27.6), 6.82, 5.25, sy(27.9), 6.98, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.50, sy(24.7), 6.70, -5.15, sy(24.7), 6.93, -4.90, sy(21.4), 7.08, -6.05, sy(21.0), 6.86, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.15, sy(24.7), 6.93, 6.50, sy(24.7), 6.70, 6.05, sy(21.0), 6.86, 4.90, sy(21.4), 7.08, skinShade, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.00, sy(18.6), 7.12, -1.05, sy(18.6), 7.18, -0.72, sy(16.95), 7.24, -2.15, sy(16.0), 7.18, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.05, sy(18.6), 7.18, 3.00, sy(18.6), 7.12, 2.15, sy(16.0), 7.18, 0.72, sy(16.95), 7.24, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.25, sy(17.85), 7.20, 1.25, sy(17.85), 7.20, 0.62, sy(16.05), 7.32, -0.62, sy(16.05), 7.32, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.70, sy(16.70), 7.30, 0.70, sy(16.70), 7.30, 0.18, sy(15.10), 7.38, -0.18, sy(15.10), 7.38, Color.FromArgb(244, 228, 224), FaceLayer.SmallDetail);

            // Переносица, кончик носа, ноздри.
            AddClientBlockLocal(-0.14, sy(26.3), 6.78, 0.14, sy(29.0), 7.05, skinDark, FaceLayer.SmallDetail);
            AddClientBlockLocal(-0.28, sy(23.5), 6.96, 0.28, sy(24.9), 7.38, noseShadow, FaceLayer.SmallDetail);
            AddClientBlockLocal(-0.42, sy(22.7), 7.02, -0.12, sy(23.3), 7.28, Color.FromArgb(182, 150, 145), FaceLayer.SmallDetail);
            AddClientBlockLocal(0.12, sy(22.7), 7.02, 0.42, sy(23.3), 7.28, Color.FromArgb(182, 150, 145), FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.62, sy(22.15), 7.07, 0.62, sy(22.15), 7.07, 0.40, sy(21.76), 7.10, -0.40, sy(21.76), 7.10, skinLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(-0.48, sy(22.15), 7.13, -0.26, sy(21.86), 7.28, Color.FromArgb(132, 94, 96), FaceLayer.SmallDetail);
            AddClientBlockLocal(0.26, sy(22.15), 7.13, 0.48, sy(21.86), 7.28, Color.FromArgb(132, 94, 96), FaceLayer.SmallDetail);

            // Веки, тени под глазами и дополнительные блики для более круглых глаз.
            AddClientQuadLocal(-4.95, sy(27.05), 7.34, -2.00, sy(27.05), 7.34, -2.32, sy(26.62), 7.34, -4.70, sy(26.62), 7.34, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.00, sy(27.05), 7.34, 4.95, sy(27.05), 7.34, 4.70, sy(26.62), 7.34, 2.32, sy(26.62), 7.34, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.85, sy(24.60), 7.16, -2.18, sy(24.60), 7.16, -2.34, sy(24.12), 7.13, -4.68, sy(24.12), 7.13, Color.FromArgb(215, 188, 184), FaceLayer.SmallDetail);
            AddClientQuadLocal(2.18, sy(24.60), 7.16, 4.85, sy(24.60), 7.16, 4.68, sy(24.12), 7.13, 2.34, sy(24.12), 7.13, Color.FromArgb(215, 188, 184), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.48, sy(26.20), 7.26, -2.62, sy(26.20), 7.26, -2.62, sy(24.90), 7.26, -4.48, sy(24.90), 7.26, Color.FromArgb(132, 170, 162), FaceLayer.SmallDetail);
            AddClientQuadLocal(2.62, sy(26.20), 7.26, 4.48, sy(26.20), 7.26, 4.48, sy(24.90), 7.26, 2.62, sy(24.90), 7.26, Color.FromArgb(132, 170, 162), FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.74, sy(25.82), 7.31, -3.36, sy(25.82), 7.31, -3.36, sy(25.42), 7.31, -3.74, sy(25.42), 7.31, Color.FromArgb(150, 196, 186), FaceLayer.SmallDetail);
            AddClientQuadLocal(3.36, sy(25.82), 7.31, 3.74, sy(25.82), 7.31, 3.74, sy(25.42), 7.31, 3.36, sy(25.42), 7.31, Color.FromArgb(150, 196, 186), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.56, sy(25.55), 7.38, -4.28, sy(25.55), 7.38, -4.28, sy(25.10), 7.38, -4.56, sy(25.10), 7.38, Color.White, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.28, sy(25.55), 7.38, 4.56, sy(25.55), 7.38, 4.56, sy(25.10), 7.38, 4.28, sy(25.10), 7.38, Color.White, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.35, sy(26.82), 7.43, -4.95, sy(26.82), 7.43, -4.95, sy(26.32), 7.43, -5.35, sy(26.32), 7.43, brow, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.10, sy(26.82), 7.43, -1.65, sy(26.82), 7.43, -1.65, sy(26.32), 7.43, -2.10, sy(26.32), 7.43, brow, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.65, sy(26.82), 7.43, 2.10, sy(26.82), 7.43, 2.10, sy(26.32), 7.43, 1.65, sy(26.32), 7.43, brow, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.95, sy(26.82), 7.43, 5.35, sy(26.82), 7.43, 5.35, sy(26.32), 7.43, 4.95, sy(26.32), 7.43, brow, FaceLayer.SmallDetail);

            // Дополнительное скругление и детализация глаз — больше мелких сегментов.
            AddClientQuadLocal(-4.92, sy(26.90), 7.36, -4.55, sy(26.90), 7.36, -4.35, sy(26.50), 7.36, -4.70, sy(26.42), 7.36, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.35, sy(26.92), 7.36, -2.00, sy(26.84), 7.36, -2.18, sy(26.48), 7.36, -2.52, sy(26.52), 7.36, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.85, sy(24.52), 7.20, -4.52, sy(24.60), 7.20, -4.30, sy(24.90), 7.20, -4.62, sy(24.98), 7.20, Color.FromArgb(228, 206, 202), FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.45, sy(24.60), 7.20, -2.12, sy(24.52), 7.20, -2.36, sy(24.98), 7.20, -2.70, sy(24.90), 7.20, Color.FromArgb(228, 206, 202), FaceLayer.SmallDetail);
            AddClientQuadLocal(4.55, sy(26.90), 7.36, 4.92, sy(26.90), 7.36, 4.70, sy(26.42), 7.36, 4.35, sy(26.50), 7.36, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.00, sy(26.84), 7.36, 2.35, sy(26.92), 7.36, 2.52, sy(26.52), 7.36, 2.18, sy(26.48), 7.36, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.52, sy(24.60), 7.20, 4.85, sy(24.52), 7.20, 4.62, sy(24.98), 7.20, 4.30, sy(24.90), 7.20, Color.FromArgb(228, 206, 202), FaceLayer.SmallDetail);
            AddClientQuadLocal(2.12, sy(24.52), 7.20, 2.45, sy(24.60), 7.20, 2.70, sy(24.90), 7.20, 2.36, sy(24.98), 7.20, Color.FromArgb(228, 206, 202), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.34, sy(26.30), 7.28, -2.70, sy(26.30), 7.28, -2.70, sy(24.72), 7.28, -4.34, sy(24.72), 7.28, Color.FromArgb(120, 162, 154), FaceLayer.SmallDetail);
            AddClientQuadLocal(2.70, sy(26.30), 7.28, 4.34, sy(26.30), 7.28, 4.34, sy(24.72), 7.28, 2.70, sy(24.72), 7.28, Color.FromArgb(120, 162, 154), FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.02, sy(26.02), 7.31, -3.02, sy(26.02), 7.31, -3.02, sy(25.00), 7.31, -4.02, sy(25.00), 7.31, Color.FromArgb(152, 200, 190), FaceLayer.SmallDetail);
            AddClientQuadLocal(3.02, sy(26.02), 7.31, 4.02, sy(26.02), 7.31, 4.02, sy(25.00), 7.31, 3.02, sy(25.00), 7.31, Color.FromArgb(152, 200, 190), FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.84, sy(25.76), 7.34, -3.26, sy(25.76), 7.34, -3.26, sy(25.20), 7.34, -3.84, sy(25.20), 7.34, irisCore, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.26, sy(25.76), 7.34, 3.84, sy(25.76), 7.34, 3.84, sy(25.20), 7.34, 3.26, sy(25.20), 7.34, irisCore, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.74, sy(25.66), 7.38, -3.36, sy(25.66), 7.38, -3.36, sy(25.28), 7.38, -3.74, sy(25.28), 7.38, pupil, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.36, sy(25.66), 7.38, 3.74, sy(25.66), 7.38, 3.74, sy(25.28), 7.38, 3.36, sy(25.28), 7.38, pupil, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.20, sy(25.32), 7.40, -3.98, sy(25.32), 7.40, -3.98, sy(25.02), 7.40, -4.20, sy(25.02), 7.40, Color.White, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.98, sy(25.32), 7.40, 4.20, sy(25.32), 7.40, 4.20, sy(25.02), 7.40, 3.98, sy(25.02), 7.40, Color.White, FaceLayer.SmallDetail);

            // Губы: контур, впадинка над губой, более мягкая форма.
            AddClientQuadLocal(-1.92, sy(19.86), 7.06, 1.92, sy(19.86), 7.06, 1.45, sy(19.30), 7.09, -1.45, sy(19.30), 7.09, lipDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.18, sy(19.64), 7.18, -0.12, sy(19.64), 7.18, -0.30, sy(19.24), 7.22, -0.92, sy(19.24), 7.22, lipLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(0.12, sy(19.64), 7.18, 1.18, sy(19.64), 7.18, 0.92, sy(19.24), 7.22, 0.30, sy(19.24), 7.22, lipLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.18, sy(20.02), 7.08, 0.18, sy(20.02), 7.08, 0.08, sy(19.62), 7.13, -0.08, sy(19.62), 7.13, Color.FromArgb(185, 145, 146), FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.34, sy(18.90), 7.22, 1.34, sy(18.90), 7.22, 0.92, sy(18.32), 7.26, -0.92, sy(18.32), 7.26, lip, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.78, sy(18.70), 7.25, 0.78, sy(18.70), 7.25, 0.38, sy(18.42), 7.29, -0.38, sy(18.42), 7.29, lipLight, FaceLayer.SmallDetail);

            // Мелкие детали лица: подбородок, румянец, височные тени.
            AddClientQuadLocal(-1.20, sy(18.05), 7.16, 1.20, sy(18.05), 7.16, 0.62, sy(17.00), 7.22, -0.62, sy(17.00), 7.22, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.85, sy(22.55), 6.98, -4.85, sy(22.55), 6.98, -4.70, sy(21.45), 7.02, -5.65, sy(21.45), 7.02, Color.FromArgb(238, 196, 200), FaceLayer.SmallDetail);
            AddClientQuadLocal(4.85, sy(22.55), 6.98, 5.85, sy(22.55), 6.98, 5.65, sy(21.45), 7.02, 4.70, sy(21.45), 7.02, Color.FromArgb(238, 196, 200), FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.35, sy(29.6), 6.55, -5.55, sy(29.6), 6.55, -5.55, sy(26.1), 6.58, -6.35, sy(26.1), 6.58, Color.FromArgb(209, 180, 176), FaceLayer.SmallDetail);
            AddClientQuadLocal(5.55, sy(29.6), 6.55, 6.35, sy(29.6), 6.55, 6.35, sy(26.1), 6.58, 5.55, sy(26.1), 6.58, Color.FromArgb(209, 180, 176), FaceLayer.SmallDetail);

            // Дополнительная детализация нижней части лица: мягкая челюсть, овальный подбородок, светлая центральная зона.
            AddClientQuadLocal(-4.20, sy(21.35), 6.92, -2.45, sy(21.35), 7.02, -1.55, sy(19.05), 7.11, -3.35, sy(18.45), 7.04, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.45, sy(21.35), 7.02, 4.20, sy(21.35), 6.92, 3.35, sy(18.45), 7.04, 1.55, sy(19.05), 7.11, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.10, sy(20.10), 7.00, 3.10, sy(20.10), 7.00, 2.05, sy(18.10), 7.12, -2.05, sy(18.10), 7.12, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.30, sy(18.05), 7.12, 2.30, sy(18.05), 7.12, 1.45, sy(16.35), 7.22, -1.45, sy(16.35), 7.22, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.05, sy(20.55), 6.98, -2.00, sy(19.82), 7.06, -1.25, sy(17.92), 7.13, -2.45, sy(17.48), 7.09, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.00, sy(19.82), 7.06, 3.05, sy(20.55), 6.98, 2.45, sy(17.48), 7.09, 1.25, sy(17.92), 7.13, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.58, sy(18.30), 7.18, 0.58, sy(18.30), 7.18, 0.18, sy(16.95), 7.23, -0.18, sy(16.95), 7.23, skin, FaceLayer.SmallDetail);

            // Дополнительное скругление нижней челюсти и мягкой линии подбородка.
            AddClientQuadLocal(-2.75, sy(18.75), 7.07, -1.20, sy(18.00), 7.15, -0.75, sy(16.45), 7.22, -2.10, sy(16.65), 7.20, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(1.20, sy(18.00), 7.15, 2.75, sy(18.75), 7.07, 2.10, sy(16.65), 7.20, 0.75, sy(16.45), 7.22, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.10, sy(17.20), 7.18, 2.10, sy(17.20), 7.18, 1.25, sy(15.80), 7.26, -1.25, sy(15.80), 7.26, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.85, sy(24.8), 6.86, -4.05, sy(24.8), 6.86, -3.30, sy(20.8), 6.98, -5.15, sy(20.2), 6.94, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.05, sy(24.8), 6.86, 5.85, sy(24.8), 6.86, 5.15, sy(20.2), 6.94, 3.30, sy(20.8), 6.98, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.60, sy(21.15), 6.95, -3.05, sy(20.55), 7.03, -1.85, sy(17.80), 7.15, -3.65, sy(17.05), 7.08, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.05, sy(20.55), 7.03, 4.60, sy(21.15), 6.95, 3.65, sy(17.05), 7.08, 1.85, sy(17.80), 7.15, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.00, sy(18.95), 7.06, -2.30, sy(18.42), 7.14, -1.40, sy(16.30), 7.21, -3.10, sy(15.92), 7.16, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.30, sy(18.42), 7.14, 4.00, sy(18.95), 7.06, 3.10, sy(15.92), 7.16, 1.40, sy(16.30), 7.21, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.45, sy(16.72), 7.19, 2.45, sy(16.72), 7.19, 1.30, sy(15.50), 7.26, -1.30, sy(15.50), 7.26, skin, FaceLayer.SmallDetail);

            // Срезаем квадратные углы снизу лица: выстраиваем более плавный овальный силуэт.
            AddClientQuadLocal(-4.58, sy(22.20), 7.00, -3.18, sy(21.20), 7.06, -2.35, sy(18.62), 7.18, -3.82, sy(17.92), 7.11, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.18, sy(21.20), 7.06, 4.58, sy(22.20), 7.00, 3.82, sy(17.92), 7.11, 2.35, sy(18.62), 7.18, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.78, sy(18.92), 7.10, -2.18, sy(18.25), 7.18, -1.18, sy(16.18), 7.26, -2.92, sy(15.82), 7.20, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.18, sy(18.25), 7.18, 3.78, sy(18.92), 7.10, 2.92, sy(15.82), 7.20, 1.18, sy(16.18), 7.26, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.92, sy(16.72), 7.22, 1.92, sy(16.72), 7.22, 1.08, sy(15.26), 7.30, -1.08, sy(15.26), 7.30, skinLight, FaceLayer.SmallDetail);

            // Дополнительное округление именно челюсти: ещё сильнее срезаем нижние углы,
            // чтобы квадратная масса по бокам визуально превращалась в мягкий овал.
            AddClientQuadLocal(-5.12, sy(21.72), 6.98, -3.45, sy(20.98), 7.05, -2.10, sy(17.18), 7.18, -4.12, sy(16.52), 7.08, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(3.45, sy(20.98), 7.05, 5.12, sy(21.72), 6.98, 4.12, sy(16.52), 7.08, 2.10, sy(17.18), 7.18, skinLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-4.18, sy(18.28), 7.10, -2.54, sy(17.74), 7.19, -1.46, sy(15.84), 7.28, -3.06, sy(15.40), 7.20, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.54, sy(17.74), 7.19, 4.18, sy(18.28), 7.10, 3.06, sy(15.40), 7.20, 1.46, sy(15.84), 7.28, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-2.38, sy(16.34), 7.24, 2.38, sy(16.34), 7.24, 1.26, sy(14.96), 7.32, -1.26, sy(14.96), 7.32, skinLight, FaceLayer.SmallDetail);

            // Боковой профиль лица: заполняем стык головы и лица, чтобы сбоку лицо не выглядело отдельно летящим.
            AddClientQuadLocal(-6.30, sy(25.4), 3.10, -5.00, sy(24.9), 4.78, -4.10, sy(20.5), 5.98, -6.15, sy(20.9), 3.95, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.00, sy(24.9), 4.78, 6.30, sy(25.4), 3.10, 6.15, sy(20.9), 3.95, 4.10, sy(20.5), 5.98, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.10, sy(21.0), 3.95, -4.15, sy(20.7), 5.98, -3.15, sy(16.95), 6.32, -5.55, sy(17.10), 4.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.15, sy(20.7), 5.98, 6.10, sy(21.0), 3.95, 5.55, sy(17.10), 4.10, 3.15, sy(16.95), 6.32, skin, FaceLayer.SmallDetail);

            // Усиленное боковое крепление лица к объёму головы:
            // при повороте вбок эти skin-переходы соединяют переднюю маску лица
            // с боковой частью головы, чтобы лицо не выглядело отдельно съехавшей плоскостью.
            AddClientQuadLocal(-6.25, sy(28.8), 1.8, -5.05, sy(28.5), 6.15, -4.95, sy(24.2), 6.55, -6.35, sy(24.0), 1.55, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.05, sy(28.5), 6.15, 6.25, sy(28.8), 1.8, 6.35, sy(24.0), 1.55, 4.95, sy(24.2), 6.55, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.35, sy(24.2), 1.55, -4.95, sy(24.2), 6.55, -4.35, sy(20.3), 6.70, -6.25, sy(20.0), 1.35, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.95, sy(24.2), 6.55, 6.35, sy(24.2), 1.55, 6.25, sy(20.0), 1.35, 4.35, sy(20.3), 6.70, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.25, sy(20.0), 1.35, -4.35, sy(20.3), 6.70, -3.55, sy(16.3), 6.55, -5.75, sy(16.0), 1.20, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.35, sy(20.3), 6.70, 6.25, sy(20.0), 1.35, 5.75, sy(16.0), 1.20, 3.55, sy(16.3), 6.55, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-6.10, sy(31.0), 1.6, -5.00, sy(30.8), 5.85, -5.05, sy(27.4), 6.30, -6.25, sy(27.6), 1.55, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.00, sy(30.8), 5.85, 6.10, sy(31.0), 1.6, 6.25, sy(27.6), 1.55, 5.05, sy(27.4), 6.30, skin, FaceLayer.SmallDetail);

            // Боковые объёмные вставки закрывают щель между лицом и волосами/затылком
            // в профильном ракурсе, но оставляем их уже и глубже, чтобы спереди не было квадратной маски.
            AddClientBlockLocal(-5.40, sy(19.0), 0.9, -4.82, sy(27.8), 5.72, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(4.82, sy(19.0), 0.9, 5.40, sy(27.8), 5.72, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(-5.02, sy(17.2), 1.0, -4.10, sy(20.2), 5.98, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(4.10, sy(17.2), 1.0, 5.02, sy(20.2), 5.98, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(-5.70, sy(27.0), 1.1, -5.00, sy(31.0), 5.55, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(5.00, sy(27.0), 1.1, 5.70, sy(31.0), 5.55, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.10, sy(24.9), 6.02, -4.45, sy(24.2), 5.82, -4.20, sy(19.6), 5.98, -4.95, sy(19.1), 6.10, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.45, sy(24.2), 5.82, 5.10, sy(24.9), 6.02, 4.95, sy(19.1), 6.10, 4.20, sy(19.6), 5.98, skin, FaceLayer.SmallDetail);

            // Тёмная масса волос остаётся позади соединителя и маскирует задний зазор.
            AddClientBlockLocal(-8.05, sy(16.0), 0.2, -6.05, sy(31.2), 4.0, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(6.05, sy(16.0), 0.2, 8.05, sy(31.2), 4.0, hair, FaceLayer.SmallDetail);

            // Дополнительная прокладка между лицом и объёмом головы: помогает держать лицо
            // прикреплённым к черепу даже при отдалении и боковом ракурсе.
            AddClientBlockLocal(-5.62, sy(18.4), 1.2, -4.02, sy(31.2), 6.45, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(4.02, sy(18.4), 1.2, 5.62, sy(31.2), 6.45, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.60, sy(29.6), 6.32, -4.50, sy(29.2), 6.58, -4.10, sy(24.0), 6.72, -5.45, sy(24.1), 6.44, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.50, sy(29.2), 6.58, 5.60, sy(29.6), 6.32, 5.45, sy(24.1), 6.44, 4.10, sy(24.0), 6.72, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.42, sy(22.8), 6.44, -4.08, sy(22.8), 6.72, -3.62, sy(18.0), 6.70, -5.20, sy(18.0), 6.36, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(4.08, sy(22.8), 6.72, 5.42, sy(22.8), 6.44, 5.20, sy(18.0), 6.36, 3.62, sy(18.0), 6.70, skin, FaceLayer.SmallDetail);
            AddClientBlockLocal(-6.65, sy(16.0), 0.8, -5.85, sy(31.4), 5.25, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(5.85, sy(16.0), 0.8, 6.65, sy(31.4), 5.25, hair, FaceLayer.SmallDetail);

            // Выравниваем тон нижней части лица, чтобы он совпадал с основным тоном кожи
            // и не выглядел отдельно более тёмным или более светлым.
            AddClientQuadLocal(-4.10, sy(21.10), 7.30, 4.10, sy(21.10), 7.30, 3.08, sy(18.74), 7.36, -3.08, sy(18.74), 7.36, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-3.08, sy(18.92), 7.34, 3.08, sy(18.92), 7.34, 2.02, sy(16.58), 7.40, -2.02, sy(16.58), 7.40, skin, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.84, sy(17.02), 7.38, 1.84, sy(17.02), 7.38, 1.10, sy(15.56), 7.43, -1.10, sy(15.56), 7.43, skinLight, FaceLayer.SmallDetail);

            // Повторно выводим губы поверх слоёв скругления лица,
            // чтобы они не терялись в skin-текстурах и выглядели чуть выразительнее.
            AddClientQuadLocal(-2.08, sy(19.92), 7.44, 2.08, sy(19.92), 7.44, 1.56, sy(19.28), 7.47, -1.56, sy(19.28), 7.47, lipDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.34, sy(19.68), 7.48, -0.16, sy(19.68), 7.48, -0.38, sy(19.20), 7.50, -1.02, sy(19.20), 7.50, lipLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(0.16, sy(19.68), 7.48, 1.34, sy(19.68), 7.48, 1.02, sy(19.20), 7.50, 0.38, sy(19.20), 7.50, lipLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.28, sy(20.06), 7.50, 0.28, sy(20.06), 7.50, 0.12, sy(19.58), 7.52, -0.12, sy(19.58), 7.52, lipDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.48, sy(18.98), 7.51, 1.48, sy(18.98), 7.51, 1.02, sy(18.30), 7.54, -1.02, sy(18.30), 7.54, lip, FaceLayer.SmallDetail);
            AddClientQuadLocal(-0.90, sy(18.74), 7.54, 0.90, sy(18.74), 7.54, 0.44, sy(18.44), 7.56, -0.44, sy(18.44), 7.56, lipLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-1.72, sy(19.58), 7.46, 1.72, sy(19.58), 7.46, 1.22, sy(19.12), 7.49, -1.22, sy(19.12), 7.49, Color.FromArgb(204, 128, 142), FaceLayer.SmallDetail);

            // Больше волос по бокам, сверху и возле лица.
            AddClientBlockLocal(-8.95, sy(31.8), -4.2, -6.45, sy(21.0), 0.9, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(6.45, sy(31.8), -4.2, 8.95, sy(21.0), 0.9, hair, FaceLayer.SmallDetail);
            AddClientBlockLocal(-8.15, sy(33.0), -1.2, -6.35, sy(24.2), 2.2, hairLight, FaceLayer.SmallDetail);
            AddClientBlockLocal(6.35, sy(33.0), -1.2, 8.15, sy(24.2), 2.2, hairLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-7.55, sy(36.2), 0.2, -5.55, sy(36.2), 1.8, -6.05, sy(31.2), 4.0, -7.85, sy(31.0), 2.6, hairDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(5.55, sy(36.2), 1.8, 7.55, sy(36.2), 0.2, 7.85, sy(31.0), 2.6, 6.05, sy(31.2), 4.0, hairDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(-8.45, sy(30.0), 1.20, -6.95, sy(30.0), 1.80, -6.80, sy(23.6), 1.55, -8.05, sy(23.3), 0.95, hair, FaceLayer.SmallDetail);
            AddClientQuadLocal(6.95, sy(30.0), 1.80, 8.45, sy(30.0), 1.20, 8.05, sy(23.3), 0.95, 6.80, sy(23.6), 1.55, hair, FaceLayer.SmallDetail);
            AddClientQuadLocal(-5.90, sy(33.2), 6.65, -2.60, sy(33.2), 6.92, -2.95, sy(30.2), 7.00, -6.15, sy(30.2), 6.74, hairLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(2.60, sy(33.2), 6.92, 5.90, sy(33.2), 6.65, 6.15, sy(30.2), 6.74, 2.95, sy(30.2), 7.00, hairLight, FaceLayer.SmallDetail);
            AddClientQuadLocal(-7.45, sy(18.0), 0.65, -6.15, sy(18.0), 1.05, -6.35, sy(5.4), 0.30, -7.55, sy(5.6), -0.10, hairDark, FaceLayer.SmallDetail);
            AddClientQuadLocal(6.15, sy(18.0), 1.05, 7.45, sy(18.0), 0.65, 7.55, sy(5.6), -0.10, 6.35, sy(5.4), 0.30, hairDark, FaceLayer.SmallDetail);

            ApplyClientTransform();
        }

        private void AddTiaHoldingCupAttachment()
        {
            Color skin = Color.FromArgb(239, 219, 214);
            Color skinShade = Color.FromArgb(224, 198, 192);
            Color skinLight = Color.FromArgb(247, 231, 226);
            Color dress = Color.FromArgb(88, 92, 100);
            Color dressLight = Color.FromArgb(118, 124, 132);
            Color nail = Color.FromArgb(170, 110, 128);
            Color cupBody = Color.FromArgb(144, 132, 118);
            Color cupShadow = HorrorColor.DeepWood;
            Color lidWhite = Color.FromArgb(255, 255, 255);
            Color lidShadow = Color.FromArgb(228, 228, 222);
            Color raspberryFill = Color.FromArgb(231, 188, 206);
            Color raspberryFillDark = Color.FromArgb(214, 154, 178);

            // Рука должна выходить из рукава, а не висеть перед одеждой.
            // Поэтому верхнюю часть руки уводим глубже к телу и закрываем плечо/верх руки рукавом.
            AddClientAttachmentBlockLocal(9.6, -21.8, 4.4, 12.6, -10.9, 6.6, skin, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(10.1, -20.8, 4.6, 12.0, -11.8, 6.2, skinLight, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(8.3, -18.2, 6.9, 13.8, -6.8, 9.8, dress, FaceLayer.SmallDetail); // рукав сверху
            AddClientAttachmentBlockLocal(8.7, -17.2, 7.2, 13.3, -7.6, 9.3, dressLight, FaceLayer.SmallDetail); // свет рукава

            // Локоть и переход к предплечью — тоже чуть глубже, чтобы локоть не жил отдельно.
            AddClientAttachmentBlockLocal(8.5, -24.0, 5.0, 12.3, -20.8, 7.2, skinShade, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(7.5, -24.8, 5.4, 10.2, -21.8, 7.1, skin, FaceLayer.SmallDetail);

            // Горизонтальное предплечье: начало слегка глубже и с нахлёстом на локоть.
            AddClientAttachmentBlockLocal(1.8, -24.8, 6.0, 8.9, -21.9, 8.0, skin, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(2.4, -24.3, 6.2, 8.1, -22.4, 7.7, skinLight, FaceLayer.SmallDetail);

            // Горизонтальная кисть — ладонь и пальцы вперёд.
            AddClientAttachmentBlockLocal(-2.8, -24.7, 6.2, 2.0, -22.0, 8.5, skin, FaceLayer.SmallDetail); // ладонь
            AddClientAttachmentBlockLocal(-3.5, -24.6, 6.2, -2.8, -24.0, 8.5, skinLight, FaceLayer.SmallDetail); // палец 1
            AddClientAttachmentBlockLocal(-3.6, -23.9, 6.2, -2.9, -23.3, 8.5, skinLight, FaceLayer.SmallDetail); // палец 2
            AddClientAttachmentBlockLocal(-3.4, -23.2, 6.2, -2.7, -22.6, 8.5, skinLight, FaceLayer.SmallDetail); // палец 3
            AddClientAttachmentBlockLocal(-1.0, -24.2, 8.2, 0.6, -22.7, 9.4, skinShade, FaceLayer.SmallDetail); // большой палец сбоку
            AddClientAttachmentBlockLocal(-3.5, -24.6, 8.4, -2.9, -24.2, 9.0, nail, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(-3.6, -23.9, 8.4, -3.0, -23.5, 9.0, nail, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(-3.4, -23.2, 8.4, -2.8, -22.8, 9.0, nail, FaceLayer.SmallDetail);

            // Стакан остаётся в кисти, чуть перед телом.
            double cupX = -2.7;
            double cupY = -25.0;
            double cupZ = 8.7;

            AddClientAttachmentBlockLocal(cupX - 3.0, cupY, cupZ - 2.0, cupX + 3.0, cupY + 3.0, cupZ + 2.0, cupBody, FaceLayer.Furniture);
            AddClientAttachmentBlockLocal(cupX - 2.6, cupY + 3.0, cupZ - 1.7, cupX + 2.6, cupY + 5.8, cupZ + 1.7, cupBody, FaceLayer.Furniture);
            AddClientAttachmentBlockLocal(cupX - 2.15, cupY + 4.90, cupZ - 1.30, cupX + 2.15, cupY + 6.00, cupZ + 1.30, raspberryFillDark, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(cupX - 1.90, cupY + 6.02, cupZ - 1.15, cupX + 1.90, cupY + 6.16, cupZ + 1.15, raspberryFill, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(cupX - 2.7, cupY + 1.8, cupZ - 1.8, cupX + 2.7, cupY + 3.4, cupZ + 1.8, cupShadow, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(cupX - 3.3, cupY + 5.8, cupZ - 2.3, cupX + 3.3, cupY + 6.6, cupZ + 2.3, lidWhite, FaceLayer.Furniture);
            AddClientAttachmentBlockLocal(cupX - 2.2, cupY + 6.60, cupZ - 1.35, cupX + 2.2, cupY + 7.10, cupZ + 1.35, lidWhite, FaceLayer.SmallDetail);
            AddClientAttachmentBlockLocal(cupX - 2.8, cupY + 5.65, cupZ - 1.90, cupX + 2.8, cupY + 5.82, cupZ + 1.90, lidShadow, FaceLayer.SmallDetail);
        }

        private void AddClientAttachmentBlockLocal(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2,
            Color color,
            FaceLayer layer)
        {
            int startIndex = _model.Points.Count;
            AddBlock(
                _clientWorldX + x1,
                y1,
                _clientWorldZ + z1,
                _clientWorldX + x2,
                y2,
                _clientWorldZ + z2,
                "no_outline",
                color,
                layer);
            RegisterLocalPoints(startIndex, _model.Points.Count, _tiaHoldingCupPoints, _tiaHoldingCupLocalX, _tiaHoldingCupLocalY, _tiaHoldingCupLocalZ);
        }

        private void AddClientAttachmentQuadLocal(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2,
            double x3,
            double y3,
            double z3,
            double x4,
            double y4,
            double z4,
            Color color,
            FaceLayer layer)
        {
            int startIndex = _model.Points.Count;
            AddQuad(
                _clientWorldX + x1,
                y1,
                _clientWorldZ + z1,
                _clientWorldX + x2,
                y2,
                _clientWorldZ + z2,
                _clientWorldX + x3,
                y3,
                _clientWorldZ + z3,
                _clientWorldX + x4,
                y4,
                _clientWorldZ + z4,
                "no_outline",
                color,
                layer);
            RegisterLocalPoints(startIndex, _model.Points.Count, _tiaHoldingCupPoints, _tiaHoldingCupLocalX, _tiaHoldingCupLocalY, _tiaHoldingCupLocalZ);
        }

        private void AddClientBlockLocal(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2,
            Color color,
            FaceLayer layer)
        {
            int startIndex = _model.Points.Count;
            AddBlock(
                _clientWorldX + x1,
                y1,
                _clientWorldZ + z1,
                _clientWorldX + x2,
                y2,
                _clientWorldZ + z2,
                "no_outline",
                color,
                layer);
            RegisterClientPoints(startIndex, _model.Points.Count);
        }

        private void AddClientQuadLocal(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2,
            double x3,
            double y3,
            double z3,
            double x4,
            double y4,
            double z4,
            Color color,
            FaceLayer layer)
        {
            int startIndex = _model.Points.Count;
            AddQuad(
                _clientWorldX + x1,
                y1,
                _clientWorldZ + z1,
                _clientWorldX + x2,
                y2,
                _clientWorldZ + z2,
                _clientWorldX + x3,
                y3,
                _clientWorldZ + z3,
                _clientWorldX + x4,
                y4,
                _clientWorldZ + z4,
                "no_outline",
                color,
                layer);
            RegisterClientPoints(startIndex, _model.Points.Count);
        }

        private void RegisterClientPoints(int startIndex, int endIndex)
        {
            RegisterLocalPoints(startIndex, endIndex, _clientPoints, _clientLocalX, _clientLocalY, _clientLocalZ);
        }

        private void RegisterLocalPoints(
            int startIndex,
            int endIndex,
            List<int> points,
            List<double> localX,
            List<double> localY,
            List<double> localZ)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                Point3D point = _model.Points[i];
                points.Add(i);
                localX.Add(point.X - _clientWorldX);
                localY.Add(point.Y);
                localZ.Add(point.Z - _clientWorldZ);
            }
        }

        private void ApplyAttachmentTransform(
            List<int> points,
            List<double> localX,
            List<double> localY,
            List<double> localZ,
            double cos,
            double sin,
            double hiddenOffsetY,
            double globalBob,
            double torsoSway)
        {
            if (points.Count == 0 || points.Count != localX.Count)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                int index = points[i];
                if (index < 0 || index >= _model.Points.Count)
                    continue;

                double finalLocalX = localX[i] + torsoSway * 0.9;
                double finalLocalY = localY[i] + globalBob;
                double finalLocalZ = localZ[i];
                Point3D point = _model.Points[index];

                point.X = _clientWorldX + finalLocalX * cos + finalLocalZ * sin;
                point.Y = finalLocalY + hiddenOffsetY;
                point.Z = _clientWorldZ - finalLocalX * sin + finalLocalZ * cos;
            }

        }

        private void ApplyClientTransform()
        {
            if (_clientPoints.Count == 0 || _clientPoints.Count != _clientLocalX.Count)
                return;

            double cos = Math.Cos(_clientYaw);
            double sin = Math.Sin(_clientYaw);
            double hiddenOffsetY = _clientVisible ? 0.0 : -10000.0;
            double t = _clientAnimationPhase;

            double globalBob = _clientWalkingNow
                ? Math.Sin(t * 1.7) * 0.8
                : Math.Sin(t * 0.55) * 0.2;

            double torsoSway = _clientWalkingNow
                ? Math.Sin(t * 0.85) * 0.42
                : Math.Sin(t * 0.45) * 0.16;

            for (int i = 0; i < _clientPoints.Count; i++)
            {
                int index = _clientPoints[i];
                if (index < 0 || index >= _model.Points.Count)
                    continue;

                double localX = _clientLocalX[i];
                double localY = _clientLocalY[i];
                double localZ = _clientLocalZ[i];

                double animX = 0;
                double animY = globalBob;
                double animZ = 0;

                bool leftSide = localX < 0;
                bool isHeadRegion = localY >= -24.0;
                bool isNeckRegion = localY >= -30.0 && localY < -24.0;
                // Весь объём головы (включая подбородок, щёки, скулы и все волосы у головы)
                // полностью исключён из деформации при ходьбе.

                // Голова и лицо не деформируются при ходьбе: только двигаются как цельная форма.
                if (!isHeadRegion)
                {
                    // Ноги двигаются во время ходьбы.
                    if (localY <= 6)
                    {
                        double legWeight = localY <= -58 ? 1.0 : localY <= -30 ? 0.7 : 0.35;
                        double legPhase = leftSide ? 0 : Math.PI;

                        if (_clientWalkingNow)
                        {
                            animZ += Math.Sin(t * 1.7 + legPhase) * 2.4 * legWeight;
                            animY += Math.Abs(Math.Cos(t * 1.7 + legPhase)) * 0.85 * legWeight;
                        }
                        else
                        {
                            animX += Math.Sin(t * 0.55 + legPhase) * 0.10 * legWeight;
                        }
                    }

                    // Руки двигаются в противофазе с ногами.
                    if (Math.Abs(localX) >= 10 && localY >= -42 && localY <= 12)
                    {
                        double armWeight = localY <= -18 ? 1.0 : 0.7;
                        double armPhase = leftSide ? Math.PI : 0;

                        if (_clientWalkingNow)
                        {
                            animZ += Math.Sin(t * 1.7 + armPhase) * 1.8 * armWeight;
                            animX += Math.Sin(t * 1.7 + armPhase) * 0.38 * armWeight;
                        }
                        else
                        {
                            animZ += Math.Sin(t * 0.6 + armPhase) * 0.18 * armWeight;
                        }
                    }

                    // Торс слегка покачивается, а шея — совсем немного.
                    if (localY > 6)
                    {
                        double upperWeight = isNeckRegion ? 0.30 : (localY >= 10 ? 1.0 : 0.55);
                        animX += torsoSway * upperWeight;
                    }

                    // Плечи чуть понижаются в момент шага.
                    if (localY >= 6 && localY <= 12 && _clientWalkingNow)
                    {
                        animY -= Math.Abs(Math.Sin(t * 1.7)) * 0.18;
                    }
                }

                double finalLocalX = localX + animX;
                double finalLocalY = localY + animY;
                double finalLocalZ = localZ + animZ;
                Point3D point = _model.Points[index];

                point.X = _clientWorldX + finalLocalX * cos + finalLocalZ * sin;
                point.Y = finalLocalY + hiddenOffsetY;
                point.Z = _clientWorldZ - finalLocalX * sin + finalLocalZ * cos;
            }

            if (_tiaHoldingCupVisible && _tiaBentArmPoints.Count > 0)
            {
                // Прячем старое нижнее предплечье/кисть Тии и вместо него показываем
                // отдельную нормальную согнутую часть руки как attachment перед туловищем.
                for (int i = 0; i < _tiaBentArmPoints.Count; i++)
                {
                    int index = _tiaBentArmPoints[i];
                    if (index < 0 || index >= _model.Points.Count)
                        continue;

                    _model.Points[index].Y = -10000.0;
                }
            }

            double attachmentHiddenOffsetY = (_clientVisible && _tiaHoldingCupVisible) ? 0.0 : -10000.0;
            ApplyAttachmentTransform(
                _tiaHoldingCupPoints,
                _tiaHoldingCupLocalX,
                _tiaHoldingCupLocalY,
                _tiaHoldingCupLocalZ,
                cos,
                sin,
                attachmentHiddenOffsetY,
                globalBob,
                torsoSway);
        }

        private void AddTeaShelfSign()
        {
            double z = 592.20;

            // Общие размеры вывески над полкой с чашками
            double x1 = -98;
            double x2 = 98;
            double y1 = 78;
            double y2 = 132;

            Color frameDark = Color.FromArgb(54, 34, 24);
            Color frameLight = Color.FromArgb(82, 54, 38);
            Color board = Color.FromArgb(74, 52, 38);
            Color boardDark = Color.FromArgb(56, 38, 28);
            Color boardLight = Color.FromArgb(94, 68, 48);

            Color textMain = Color.FromArgb(214, 204, 182);
            Color textShadow = Color.FromArgb(120, 24, 24);

            Color metal = Color.FromArgb(112, 104, 96);
            Color rust = Color.FromArgb(96, 52, 34);

            // Внешняя рамка
            AddRectOnZPlane(
                x1,
                y1,
                x2,
                y2,
                z,
                frameDark,
                FaceLayer.WallDetail
            );

            // Внутренняя основа
            AddRectOnZPlane(
                x1 + 4,
                y1 + 4,
                x2 - 4,
                y2 - 4,
                z + 0.02,
                board,
                FaceLayer.WallDetail
            );

            // Горизонтальные деревянные доски
            AddRectOnZPlane(
                x1 + 6,
                y1 + 6,
                x2 - 6,
                y1 + 20,
                z + 0.04,
                boardLight,
                FaceLayer.WallDetail
            );

            AddRectOnZPlane(
                x1 + 6,
                y1 + 21,
                x2 - 6,
                y1 + 35,
                z + 0.04,
                board,
                FaceLayer.WallDetail
            );

            AddRectOnZPlane(
                x1 + 6,
                y1 + 36,
                x2 - 6,
                y2 - 6,
                z + 0.04,
                boardDark,
                FaceLayer.WallDetail
            );

            // Швы между досками
            AddRectOnZPlane(
                x1 + 8,
                y1 + 20,
                x2 - 8,
                y1 + 21.5,
                z + 0.06,
                HorrorColor.WoodSeam,
                FaceLayer.WallDetail
            );

            AddRectOnZPlane(
                x1 + 8,
                y1 + 35,
                x2 - 8,
                y1 + 36.5,
                z + 0.06,
                HorrorColor.WoodSeam,
                FaceLayer.WallDetail
            );

            // Светлая верхняя кромка
            AddRectOnZPlane(
                x1 + 6,
                y2 - 10,
                x2 - 6,
                y2 - 7,
                z + 0.07,
                frameLight,
                FaceLayer.WallDetail
            );

            // Небольшая нижняя тень
            AddRectOnZPlane(
                x1 + 6,
                y1 + 6,
                x2 - 6,
                y1 + 10,
                z + 0.07,
                Color.FromArgb(48, 30, 22),
                FaceLayer.WallDetail
            );

            // Болты / крепления
            AddRectOnZPlane(x1 + 10, y2 - 12, x1 + 18, y2 - 4, z + 0.08, metal, FaceLayer.WallDetail);
            AddRectOnZPlane(x2 - 18, y2 - 12, x2 - 10, y2 - 4, z + 0.08, metal, FaceLayer.WallDetail);
            AddRectOnZPlane(x1 + 11.7, y2 - 10.2, x1 + 16.3, y2 - 5.8, z + 0.09, rust, FaceLayer.WallDetail);
            AddRectOnZPlane(x2 - 16.3, y2 - 10.2, x2 - 11.7, y2 - 5.8, z + 0.09, rust, FaceLayer.WallDetail);

            // Текстовая тень
            AddSignTextOnZPlane(
                0.0,
                97.5,
                "CAFE MARCUL",
                z + 0.10,
                textShadow,
                textShadow
            );

            // Основной текст
            AddSignTextOnZPlane(
                0.0,
                99.0,
                "CAFE MARCUL",
                z + 0.14,
                textMain,
                textShadow
            );

            // Лёгкие крупные потёртости, без мусора и мерцания
            AddScratchOnZPlane(-76, 88, -60, 94, z + 0.11, Color.FromArgb(56, 42, 34), 0.22);
            AddScratchOnZPlane(58, 118, 76, 112, z + 0.11, Color.FromArgb(56, 42, 34), 0.22);
        }

        private void AddSignTextOnZPlane(double centerX, double bottomY, string text, double z, Color mainColor, Color shadowColor)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            text = text.ToUpperInvariant();

            double spacing = 2.6;
            double totalWidth = 0.0;

            for (int i = 0; i < text.Length; i++)
            {
                totalWidth += GetSignGlyphWidth(text[i]);
                if (i < text.Length - 1)
                    totalWidth += spacing;
            }

            double x = centerX - totalWidth / 2.0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                double w = GetSignGlyphWidth(c);

                if (c != ' ')
                {
                    // Тень немного ниже и правее, и чуть "глубже"
                    AddSignGlyphOnZPlane(x + 0.9, bottomY - 0.9, z + 0.05, c, shadowColor);
                    AddSignGlyphOnZPlane(x, bottomY, z, c, mainColor);
                }

                x += w + spacing;
            }
        }

        private double GetSignGlyphWidth(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'M': return 12.0;
                case ' ': return 7.0;
                default: return 10.0;
            }
        }

        private void AddSignGlyphOnZPlane(double left, double bottom, double z, char c, Color color)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'A': AddSignGlyphA(left, bottom, z, color); break;
                case 'C': AddSignGlyphC(left, bottom, z, color); break;
                case 'E': AddSignGlyphE(left, bottom, z, color); break;
                case 'F': AddSignGlyphF(left, bottom, z, color); break;
                case 'L': AddSignGlyphL(left, bottom, z, color); break;
                case 'M': AddSignGlyphM(left, bottom, z, color); break;
                case 'O': AddSignGlyphO(left, bottom, z, color); break;
                case 'R': AddSignGlyphR(left, bottom, z, color); break;
                case 'U': AddSignGlyphU(left, bottom, z, color); break;
            }
        }

        private void AddSignStrokeOnZPlane(double x1, double y1, double x2, double y2, double z, Color color)
        {
            AddRectOnZPlane(x1, y1, x2, y2, z, color, FaceLayer.SmallDetail);
        }

        private void AddSignGlyphC(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, y, x + w, y + s, z, color);
        }

        private void AddSignGlyphO(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x + w - s, y, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, y, x + w, y + s, z, color);
        }

        private void AddSignGlyphF(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;
            double mid = y + 10.5;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, mid - s / 2.0, x + w * 0.80, mid + s / 2.0, z, color);
        }

        private void AddSignGlyphE(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;
            double mid = y + 10.5;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, mid - s / 2.0, x + w * 0.82, mid + s / 2.0, z, color);
            AddSignStrokeOnZPlane(x, y, x + w, y + s, z, color);
        }

        private void AddSignGlyphM(double x, double y, double z, Color color)
        {
            double w = 12.0;
            double h = 22.0;
            double s = 2.0;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x + w - s, y, x + w, y + h, z, color);

            AddSignStrokeOnZPlane(x + 3.3, y + 8.5, x + 5.1, y + h, z, color);
            AddSignStrokeOnZPlane(x + 6.9, y + 8.5, x + 8.7, y + h, z, color);

            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
        }

        private void AddSignGlyphA(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;
            double mid = y + 10.5;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x + w - s, y, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, mid - s / 2.0, x + w, mid + s / 2.0, z, color);
        }

        private void AddSignGlyphR(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;
            double mid = y + 10.5;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x, y + h - s, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, mid - s / 2.0, x + w * 0.82, mid + s / 2.0, z, color);
            AddSignStrokeOnZPlane(x + w - s, mid, x + w, y + h, z, color);

            // Нижняя "ножка" R
            AddSignStrokeOnZPlane(x + 6.0, y, x + 8.2, mid - 0.6, z, color);
        }

        private void AddSignGlyphU(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x + w - s, y, x + w, y + h, z, color);
            AddSignStrokeOnZPlane(x, y, x + w, y + s, z, color);
        }

        private void AddSignGlyphL(double x, double y, double z, Color color)
        {
            double w = 10.0;
            double h = 22.0;
            double s = 2.0;

            AddSignStrokeOnZPlane(x, y, x + s, y + h, z, color);
            AddSignStrokeOnZPlane(x, y, x + w, y + s, z, color);
        }

        // =========================================================
        // ОКНО И ДВЕРИ
        // =========================================================
        private void AddLeftWallWithWindow()
        {
            double x = -300;
            double wallBottom = -101;
            double wallTop = 150;
            double wallNearZ = 0;
            double wallFarZ = 600;
            double windowBottom = -20;
            double windowTop = 60;
            double windowNearZ = 260;
            double windowFarZ = 310;

            AddQuad(x, wallBottom, wallNearZ, x, windowBottom, wallNearZ, x, windowBottom, wallFarZ, x, wallBottom, wallFarZ, null, HorrorColor.Wall, FaceLayer.Wall);
            AddQuad(x, windowTop, wallNearZ, x, wallTop, wallNearZ, x, wallTop, wallFarZ, x, windowTop, wallFarZ, null, HorrorColor.Wall, FaceLayer.Wall);
            AddQuad(x, windowBottom, wallNearZ, x, windowTop, wallNearZ, x, windowTop, windowNearZ, x, windowBottom, windowNearZ, null, HorrorColor.Wall, FaceLayer.Wall);
            AddQuad(x, windowBottom, windowFarZ, x, windowTop, windowFarZ, x, windowTop, wallFarZ, x, windowBottom, wallFarZ, null, HorrorColor.Wall, FaceLayer.Wall);

            AddQuad(x + 1.2, windowBottom + 1.0, windowNearZ + 1.0, x + 1.2, windowTop - 1.0, windowNearZ + 1.0, x + 1.2, windowTop - 1.0, windowFarZ - 1.0, x + 1.2, windowBottom + 1.0, windowFarZ - 1.0, null, HorrorColor.DirtyGlass, FaceLayer.Wall);
            AddWindowFrame(x, windowBottom, windowTop, windowNearZ, windowFarZ);
        }

        private void AddWindowFrame(double wallX, double bottom, double top, double z1, double z2)
        {
            Color frame = HorrorColor.DeepWood;
            double outerX1 = wallX + 0.8;
            double outerX2 = wallX + 6.0;
            double t = 5.0;

            AddBlock(outerX1, bottom - t, z1 - t, outerX2, bottom, z2 + t, null, frame, FaceLayer.Wall);
            AddBlock(outerX1, top, z1 - t, outerX2, top + t, z2 + t, null, frame, FaceLayer.Wall);
            AddBlock(outerX1, bottom, z1 - t, outerX2, top, z1, null, frame, FaceLayer.Wall);
            AddBlock(outerX1, bottom, z2, outerX2, top, z2 + t, null, frame, FaceLayer.Wall);

            double innerX1 = wallX + 1.0;
            double innerX2 = wallX + 5.2;
            double innerBottom = bottom + 1.0;
            double innerTop = top - 1.0;
            double innerZ1 = z1 + 1.0;
            double innerZ2 = z2 - 1.0;
            double sashT = 2.4;

            AddBlock(innerX1, innerBottom, innerZ1, innerX2, innerBottom + sashT, innerZ2, null, frame, FaceLayer.Wall);
            AddBlock(innerX1, innerTop - sashT, innerZ1, innerX2, innerTop, innerZ2, null, frame, FaceLayer.Wall);
            AddBlock(innerX1, innerBottom + sashT, innerZ1, innerX2, innerTop - sashT, innerZ1 + sashT, null, frame, FaceLayer.Wall);
            AddBlock(innerX1, innerBottom + sashT, innerZ2 - sashT, innerX2, innerTop - sashT, innerZ2, null, frame, FaceLayer.Wall);

            double midZ = (innerZ1 + innerZ2) / 2.0;
            double midY = (innerBottom + innerTop) / 2.0;

            AddBlock(innerX1, innerBottom + sashT, midZ - 1.2, innerX2, innerTop - sashT, midZ + 1.2, null, frame, FaceLayer.Wall);
            AddBlock(innerX1, midY - 1.2, innerZ1 + sashT, innerX2, midY + 1.2, innerZ2 - sashT, null, frame, FaceLayer.Wall);
        }

        private void AddDoorWithGlass()
        {
            Color frame = HorrorColor.DeepWood;
            Color door = Color.FromArgb(62, 32, 24);
            Color panel = Color.FromArgb(82, 38, 28);
            Color panelDark = Color.FromArgb(46, 24, 18);
            Color glass = HorrorColor.DirtyGlass;
            Color glassDark = Color.FromArgb(42, 68, 78);
            Color scratch = Color.FromArgb(80, 108, 100, 90);

            double left = -188;
            double right = -124;
            double bottom = -100;
            double top = 72;

            AddBlock(left - 4, bottom, 596.7, left, top + 4, 600, null, frame, FaceLayer.Wall);
            AddBlock(right, bottom, 596.7, right + 4, top + 4, 600, null, frame, FaceLayer.Wall);
            AddBlock(left - 4, top, 596.7, right + 4, top + 4, 600, null, frame, FaceLayer.Wall);

            AddQuad(left, bottom, 599.0, left, top, 599.0, right, top, 599.0, right, bottom, 599.0, null, door, FaceLayer.Wall);

            AddQuad(left + 8, -82, 598.82, left + 8, -24, 598.82, right - 8, -24, 598.82, right - 8, -82, 598.82, null, panel, FaceLayer.WallDetail);
            AddQuad(left + 14, -74, 598.76, left + 14, -34, 598.76, right - 14, -34, 598.76, right - 14, -74, 598.76, null, panelDark, FaceLayer.WallDetail);

            AddQuad(left + 10, 0, 598.85, left + 10, 60, 598.85, right - 10, 60, 598.85, right - 10, 0, 598.85, null, glass, FaceLayer.Wall);
            AddQuad(left + 12, 3, 598.78, left + 12, 18, 598.78, right - 12, 18, 598.78, right - 12, 3, 598.78, null, glassDark, FaceLayer.WallDetail);

            AddBlock(left + 8, -2, 598.4, right - 8, 2, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(left + 8, 58, 598.4, right - 8, 62, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(left + 8, 0, 598.4, left + 12, 60, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(right - 12, 0, 598.4, right - 8, 60, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(left + 30, 0, 598.5, left + 34, 60, 599.25, null, frame, FaceLayer.Wall);

            AddScratchOnZPlane(left + 17, 42, left + 28, 34, 598.72, scratch, 0.28);
            AddScratchOnZPlane(right - 30, 20, right - 18, 12, 598.72, scratch, 0.24);

            AddQuad(left + 4, -98, 598.72, left + 4, -90, 598.72, right - 4, -90, 598.72, right - 4, -98, 598.72, null, panelDark, FaceLayer.WallDetail);

            AddDoorHandleOnZPlane(right - 5.0, -5.0, 598.95);
        }

        private void AddRightWallServiceDoor()
        {
            Color frame = HorrorColor.DeepWood;
            Color door = Color.FromArgb(64, 40, 30);
            Color panel = Color.FromArgb(84, 52, 36);
            Color panelDark = Color.FromArgb(46, 30, 22);
            Color panelLight = Color.FromArgb(92, 58, 40);
            Color oldScratch = Color.FromArgb(82, 62, 48, 42);

            double xDoor = 299.0;
            double xFrame1 = 296.5;
            double zLeft = 265;
            double zRight = 335;
            double bottom = -100;
            double top = 72;

            AddBlock(xFrame1, bottom, zLeft - 4, 300, top + 4, zLeft, null, frame, FaceLayer.Wall);
            AddBlock(xFrame1, bottom, zRight, 300, top + 4, zRight + 4, null, frame, FaceLayer.Wall);
            AddBlock(xFrame1, top, zLeft - 4, 300, top + 4, zRight + 4, null, frame, FaceLayer.Wall);

            // Дверь теперь без наложенных крупных панелей поверх полотна.
            // Так она не спорит по глубине с настенными слоями и не выглядит "утопленной".
            AddQuad(xDoor, bottom, zLeft, xDoor, top, zLeft, xDoor, top, zRight, xDoor, bottom, zRight, null, door, FaceLayer.Wall);

            // Оставляем только очень тонкие неактивные следы на самой двери.
            AddScratchOnXPlane(xDoor - 0.22, -42, zLeft + 22, -30, zLeft + 34, oldScratch, 0.18, FaceLayer.WallDetail);
            AddScratchOnXPlane(xDoor - 0.22, 42, zRight - 28, 54, zRight - 18, oldScratch, 0.16, FaceLayer.WallDetail);

            AddDoorHandleOnXPlane(xDoor, -6.0, zRight - 11.0);
        }

        private void AddLightSwitchNearServiceDoor()
        {
            // Выключатель около правой двери без стекла.
            // Дверь стоит на правой стене: x примерно 299, z = 265..335.
            // Выключатель вынесен слева от двери, чтобы до него можно было дотянуться.

            double x = 298.55;
            double z = 248.0;

            Color basePlate = Color.FromArgb(88, 76, 66);
            Color innerPlate = Color.FromArgb(116, 104, 92);
            Color switchDark = Color.FromArgb(42, 38, 34);
            Color switchLight = Color.FromArgb(176, 166, 146);
            Color screw = Color.FromArgb(44, 38, 32);

            // Задняя пластина на правой стене.
            AddRectOnXPlane(x, -18, z - 8, 18, z + 8, basePlate, FaceLayer.WallDetail);

            // Внутренняя светлая часть.
            AddRectOnXPlane(x - 0.04, -14, z - 5.5, 14, z + 5.5, innerPlate, FaceLayer.WallDetail);

            // Тёмное углубление/корпус тумблера НЕ двигается.
            AddBlock(x - 3.2, -5.5, z - 2.3, x - 0.4, 6.5, z + 2.3, null, switchDark, FaceLayer.SmallDetail);

            // Двигается только белая деталь тумблера.
            AddMovableLightSwitchLeverBlock(x - 3.6, 0.5, z - 1.7, x - 0.7, 7.5, z + 1.7, switchLight);

            SetLightSwitchOn(false);

            // Два винта на пластине.
            AddRectOnXPlane(x - 0.08, 10.5, z - 1.2, 12.8, z + 1.2, screw, FaceLayer.SmallDetail);
            AddRectOnXPlane(x - 0.08, -12.8, z - 1.2, -10.5, z + 1.2, screw, FaceLayer.SmallDetail);

            // Лёгкая грязь вокруг выключателя, чтобы он не выглядел новым.
            AddScratchOnXPlane(x - 0.12, 18, z - 10, 9, z - 14, Color.FromArgb(76, 58, 46), 0.25, FaceLayer.WallDetail);
            AddScratchOnXPlane(x - 0.12, -16, z + 9, -8, z + 14, Color.FromArgb(72, 54, 42), 0.22, FaceLayer.WallDetail);
        }

        private void AddMovableLightSwitchLeverBlock(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2,
            Color color)
        {
            int startIndex = _model.Points.Count;

            AddBlock(x1, y1, z1, x2, y2, z2, null, color, FaceLayer.SmallDetail);

            for (int i = startIndex; i < startIndex + 8; i++)
            {
                _lightSwitchLeverPoints.Add(i);
                _lightSwitchLeverBaseY.Add(_model.Points[i].Y);
            }
        }

        public void SetLightSwitchOn(bool isOn)
        {
            if (_lightSwitchLeverPoints.Count == 0)
                return;

            // У выключателя на правой стене ось "вверх/вниз" — это Y.
            // OFF: тумблер ниже. ON: тумблер выше.
            double shiftY = isOn ? 0.0 : -7.0;

            for (int i = 0; i < _lightSwitchLeverPoints.Count; i++)
            {
                int index = _lightSwitchLeverPoints[i];

                if (index < 0 || index >= _model.Points.Count || i >= _lightSwitchLeverBaseY.Count)
                    continue;

                Point3D point = _model.Points[index];
                point.Y = _lightSwitchLeverBaseY[i] + shiftY;
            }

            _model.NotifyChanged();
        }

        private void AddDoorHandleOnZPlane(double centerX, double centerY, double doorZ)
        {
            AddBlock(centerX - 0.9, centerY - 12.0, doorZ - 0.25, centerX + 0.9, centerY + 12.0, doorZ + 0.55, null, HorrorColor.Brass, FaceLayer.WallDetail);
            AddBlock(centerX - 0.45, centerY + 5.8, doorZ - 2.2, centerX + 0.45, centerY + 8.0, doorZ - 0.25, null, HorrorColor.Brass, FaceLayer.WallDetail);
            AddBlock(centerX - 0.45, centerY - 8.0, doorZ - 2.2, centerX + 0.45, centerY - 5.8, doorZ - 0.25, null, HorrorColor.Brass, FaceLayer.WallDetail);
            AddBlock(centerX - 1.0, centerY - 6.0, doorZ - 4.8, centerX + 1.0, centerY + 6.0, doorZ - 2.2, null, HorrorColor.Rust, FaceLayer.WallDetail);
        }

        private void AddDoorHandleOnXPlane(double doorX, double centerY, double centerZ)
        {
            AddBlock(doorX - 0.55, centerY - 12.0, centerZ - 0.9, doorX + 0.25, centerY + 12.0, centerZ + 0.9, null, HorrorColor.Brass, FaceLayer.WallDetail);
            AddBlock(doorX - 2.2, centerY + 5.8, centerZ - 0.45, doorX - 0.25, centerY + 8.0, centerZ + 0.45, null, HorrorColor.Brass, FaceLayer.WallDetail);
            AddBlock(doorX - 2.2, centerY - 8.0, centerZ - 0.45, doorX - 0.25, centerY - 5.8, centerZ + 0.45, null, HorrorColor.Brass, FaceLayer.WallDetail);
            AddBlock(doorX - 4.8, centerY - 6.0, centerZ - 1.0, doorX - 2.2, centerY + 6.0, centerZ + 1.0, null, HorrorColor.Rust, FaceLayer.WallDetail);
        }

        private void AddWallMenu()
        {
            AddBlock(90, -20, 593, 270, 100, 598, null, HorrorColor.DeepWood, FaceLayer.Wall);

            AddQuad(97, -13, 592.85, 97, 93, 592.85, 263, 93, 592.85, 263, -13, 592.85, null, Color.FromArgb(28, 48, 34), FaceLayer.WallDetail);
            AddQuad(105, 80, 592.70, 105, 86, 592.70, 255, 86, 592.70, 255, 80, 592.70, null, HorrorColor.OldBlood, FaceLayer.WallDetail);
        }

        private void AddHorrorTvStandAndTv()
        {
            Color stand = HorrorColor.WornFurniture;
            Color standTop = HorrorColor.WornFurnitureLight;
            Color standDark = HorrorColor.WornFurnitureDark;

            Color tvBody = Color.FromArgb(72, 68, 62);
            Color tvBodyLight = Color.FromArgb(92, 88, 82);
            Color tvBack = Color.FromArgb(48, 46, 44);
            Color bezel = Color.FromArgb(18, 18, 20);
            Color vent = Color.FromArgb(54, 54, 56);
            Color knob = Color.FromArgb(126, 112, 88);
            Color metal = Color.FromArgb(94, 98, 104);

            AddBlock(-58, -100, 10, 58, -60, 48, null, stand, FaceLayer.Furniture);
            AddBlock(-60, -60, 8, 60, -55, 50, null, standTop, FaceLayer.Furniture);

            AddQuad(-54, -96, 48.18, -54, -63, 48.18, 54, -63, 48.18, 54, -96, 48.18, null, stand, FaceLayer.WallDetail);
            AddQuad(-58.15, -96, 12, -58.15, -63, 12, -58.15, -63, 46, -58.15, -96, 46, null, standDark, FaceLayer.WallDetail);
            AddQuad(58.15, -96, 12, 58.15, -63, 12, 58.15, -63, 46, 58.15, -96, 46, null, standDark, FaceLayer.WallDetail);

            AddNaturalBoardsOnZPlane(-54, -96, 54, -63, 48.22, -28, -2, 24);
            AddNaturalBoardsOnYPlane(-56, 12, 56, 46, -54.95, 22, 33);
            AddNaturalBoardsOnXPlane(-58.18, -96, 12, -63, 46, 22, 33);
            AddNaturalBoardsOnXPlane(58.18, -96, 12, -63, 46, 24, 36);

            AddBlock(-6, -82, 47.8, 6, -78, 49.0, null, Color.FromArgb(116, 92, 58), FaceLayer.SmallDetail);

            AddBlock(-48, -106, 14, -40, -100, 22, null, standDark, FaceLayer.SmallDetail);
            AddBlock(40, -106, 14, 48, -100, 22, null, standDark, FaceLayer.SmallDetail);
            AddBlock(-48, -106, 36, -40, -100, 44, null, standDark, FaceLayer.SmallDetail);
            AddBlock(40, -106, 36, 48, -100, 44, null, standDark, FaceLayer.SmallDetail);

            AddBlock(-40, -55, 12, 40, 10, 60, null, tvBody, FaceLayer.Furniture);
            AddBlock(-30, -47, 2, 30, -3, 18, null, tvBack, FaceLayer.Furniture);
            AddBlock(-36, 10, 16, 36, 14, 56, null, tvBodyLight, FaceLayer.Furniture);

            AddQuad(-41, -56, 60.16, -41, 11, 60.16, 41, 11, 60.16, 41, -56, 60.16, null, tvBody, FaceLayer.SmallDetail);
            AddQuad(-30, -44, 60.30, -30, -2, 60.30, 30, -2, 60.30, 30, -44, 60.30, null, bezel, FaceLayer.SmallDetail);
            AddQuad(-23, -37, 60.44, -23, -8, 60.44, 23, -8, 60.44, 23, -37, 60.44, null, Color.Black, FaceLayer.SmallDetail);
            AddTvScreenContent();

            AddScratchOnZPlane(-15, -15, 4, -28, 60.56, Color.FromArgb(56, 108, 110, 110), 0.08);
            AddScratchOnZPlane(-8, -32, 10, -22, 60.56, Color.FromArgb(48, 92, 96, 96), 0.08);

            AddQuad(-25, -50.2, 60.26, -25, -45.0, 60.26, 25, -45.0, 60.26, 25, -50.2, 60.26, null, Color.FromArgb(48, 46, 42), FaceLayer.SmallDetail);

            for (int i = 0; i < 6; i++)
            {
                double x1 = -21 + i * 4.4;
                AddBlock(x1, -49.2, 60.0, x1 + 1.8, -46.5, 61.0, null, vent, FaceLayer.SmallDetail);
            }

            AddBlock(14, -49.2, 60.0, 17.5, -45.8, 61.2, null, knob, FaceLayer.SmallDetail);
            AddBlock(19, -49.2, 60.0, 22.5, -45.8, 61.2, null, knob, FaceLayer.SmallDetail);

            AddBlock(-1.0, 14.0, 32.0, 1.0, 20.0, 34.0, null, metal, FaceLayer.Furniture);
            AddBlock(-1.0, 19.8, 32.6, -12.0, 20.8, 33.4, null, metal, FaceLayer.SmallDetail);
            AddBlock(1.0, 19.8, 32.6, 12.0, 20.8, 33.4, null, metal, FaceLayer.SmallDetail);
        }

        private void AddTvScreenContent()
        {
            int startIndex = _model.Points.Count;
            double z = 60.64;

            Color glow = Color.FromArgb(42, 60, 44);
            Color cup = Color.FromArgb(218, 208, 178);
            Color coffee = Color.FromArgb(76, 42, 24);
            Color steam = Color.FromArgb(170, 160, 135);
            Color text = Color.FromArgb(226, 216, 184);

            // Тёплый фон включённого экрана.
            AddRectOnZPlane(-22.0, -36.2, 22.0, -8.8, z, glow, FaceLayer.SmallDetail);

            // Надпись теперь находится сверху над кружкой.
            // Текст рисуется зеркально по X в геометрии, чтобы при взгляде на лицевую сторону ТВ
            // он читался нормально, а не отражался.
            int animatedStartIndex = _model.Points.Count;
            AddTvTextCenteredReadableOnZPlane(0.0, -12.6, z + 0.07, "CAFE MARCUL", text, 0.72);

            // Пар над кружкой, ниже надписи.
            AddRectOnZPlane(-6.0, -16.8, -5.0, -14.0, z + 0.05, steam, FaceLayer.SmallDetail);
            AddRectOnZPlane(-1.0, -17.2, 0.0, -14.2, z + 0.05, steam, FaceLayer.SmallDetail);
            AddRectOnZPlane(4.0, -16.8, 5.0, -14.0, z + 0.05, steam, FaceLayer.SmallDetail);
            AddPointRangeToGroup(animatedStartIndex, _model.Points.Count, _tvAnimatedScreenPoints, _tvAnimatedScreenBaseY);

            // Кружка кофе на экране.
            AddRectOnZPlane(-8.5, -28.0, 7.0, -18.0, z + 0.02, cup, FaceLayer.SmallDetail);
            AddRectOnZPlane(-6.5, -25.5, 5.0, -20.0, z + 0.04, coffee, FaceLayer.SmallDetail);
            AddRectOnZPlane(6.6, -25.8, 10.8, -20.2, z + 0.03, cup, FaceLayer.SmallDetail);
            AddRectOnZPlane(8.2, -24.5, 9.2, -21.5, z + 0.05, glow, FaceLayer.SmallDetail);
            AddRectOnZPlane(-10.5, -29.9, 9.0, -28.0, z + 0.04, cup, FaceLayer.SmallDetail);

            AddPointRangeToGroup(startIndex, _model.Points.Count, _tvScreenContentPoints, _tvScreenContentBaseY);
        }

        private void AddTvTextCenteredReadableOnZPlane(double centerX, double bottom, double z, string text, Color color, double scale)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            double totalWidth = 0.0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                    totalWidth += 2.2 * scale;
                else
                    totalWidth += 3.1 * scale;
            }

            double visualLeft = centerX - totalWidth / 2.0;
            double cursor = visualLeft;

            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);

                if (c == ' ')
                {
                    cursor += 2.2 * scale;
                    continue;
                }

                double cellWidth = 2.15 * scale;
                double mirroredRight = -(cursor);
                AddTvGlyphReadableOnZPlane(mirroredRight, bottom, z, c, color, scale);
                cursor += 3.1 * scale;
            }
        }

        private void AddTvTextOnZPlane(double left, double bottom, double z, string text, Color color, double scale)
        {
            double x = left;
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);
                if (c == ' ')
                {
                    x += 2.2 * scale;
                    continue;
                }

                AddTvGlyphOnZPlane(x, bottom, z, c, color, scale);
                x += 3.1 * scale;
            }
        }

        private void AddTvStrokeOnZPlane(double x, double y, double w, double h, double z, Color color)
        {
            AddRectOnZPlane(x, y, x + w, y + h, z, color, FaceLayer.SmallDetail);
        }

        private void AddTvStrokeReadableOnZPlane(double right, double y, double localX, double localY, double w, double h, double z, Color color)
        {
            double x2 = right - localX;
            double x1 = x2 - w;
            AddRectOnZPlane(x1, y + localY, x2, y + localY + h, z, color, FaceLayer.SmallDetail);
        }

        private void AddTvGlyphReadableOnZPlane(double right, double y, double z, char c, Color color, double scale)
        {
            double s = 0.45 * scale;
            double w = 2.15 * scale;
            double h = 2.75 * scale;
            double mid = h * 0.48;

            Action<double, double, double, double> stroke = (localX, localY, sw, sh) =>
                AddTvStrokeReadableOnZPlane(right, y, localX, localY, sw, sh, z, color);

            switch (c)
            {
                case 'A':
                    stroke(0, 0, s, h);
                    stroke(w - s, 0, s, h);
                    stroke(0, h - s, w, s);
                    stroke(0, mid, w, s);
                    break;
                case 'C':
                    stroke(0, 0, s, h);
                    stroke(0, h - s, w, s);
                    stroke(0, 0, w, s);
                    break;
                case 'E':
                    stroke(0, 0, s, h);
                    stroke(0, h - s, w, s);
                    stroke(0, mid, w * 0.82, s);
                    stroke(0, 0, w, s);
                    break;
                case 'F':
                    stroke(0, 0, s, h);
                    stroke(0, h - s, w, s);
                    stroke(0, mid, w * 0.82, s);
                    break;
                case 'L':
                    stroke(0, 0, s, h);
                    stroke(0, 0, w, s);
                    break;
                case 'M':
                    stroke(0, 0, s, h);
                    stroke(w - s, 0, s, h);
                    stroke(w * 0.38, h * 0.45, s, h * 0.55);
                    break;
                case 'R':
                    stroke(0, 0, s, h);
                    stroke(0, h - s, w, s);
                    stroke(0, mid, w * 0.88, s);
                    stroke(w - s, mid, s, h - mid);
                    stroke(w * 0.55, 0, s, mid);
                    break;
                case 'U':
                    stroke(0, 0, s, h);
                    stroke(w - s, 0, s, h);
                    stroke(0, 0, w, s);
                    break;
            }
        }

        private void AddTvGlyphOnZPlane(double x, double y, double z, char c, Color color, double scale)
        {
            double s = 0.45 * scale;
            double w = 2.15 * scale;
            double h = 2.75 * scale;
            double mid = y + h * 0.48;

            switch (c)
            {
                case 'A':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x + w - s, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y + h - s, w, s, z, color);
                    AddTvStrokeOnZPlane(x, mid, w, s, z, color);
                    break;
                case 'C':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y + h - s, w, s, z, color);
                    AddTvStrokeOnZPlane(x, y, w, s, z, color);
                    break;
                case 'E':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y + h - s, w, s, z, color);
                    AddTvStrokeOnZPlane(x, mid, w * 0.82, s, z, color);
                    AddTvStrokeOnZPlane(x, y, w, s, z, color);
                    break;
                case 'F':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y + h - s, w, s, z, color);
                    AddTvStrokeOnZPlane(x, mid, w * 0.82, s, z, color);
                    break;
                case 'L':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y, w, s, z, color);
                    break;
                case 'M':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x + w - s, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x + w * 0.38, y + h * 0.45, s, h * 0.55, z, color);
                    break;
                case 'R':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y + h - s, w, s, z, color);
                    AddTvStrokeOnZPlane(x, mid, w * 0.88, s, z, color);
                    AddTvStrokeOnZPlane(x + w - s, mid, s, h - (mid - y), z, color);
                    AddTvStrokeOnZPlane(x + w * 0.55, y, s, mid - y, z, color);
                    break;
                case 'U':
                    AddTvStrokeOnZPlane(x, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x + w - s, y, s, h, z, color);
                    AddTvStrokeOnZPlane(x, y, w, s, z, color);
                    break;
            }
        }

        private void AddEntranceRug()
        {
            double y = -100.86;

            AddRectOnYPlane(214, 258, 286, 342, y, Color.FromArgb(80, 44, 30), FaceLayer.SmallDetail);
            AddRectOnYPlane(220, 264, 280, 336, y + 0.02, Color.FromArgb(58, 34, 24), FaceLayer.SmallDetail);
            AddRectOnYPlane(230, 276, 270, 324, y + 0.04, Color.FromArgb(44, 24, 20), FaceLayer.SmallDetail);
            AddRectOnYPlane(224, 296, 278, 304, y + 0.06, Color.FromArgb(92, 74, 52), FaceLayer.SmallDetail);
        }

        private void AddWallVentOnRightWall()
        {
            double xFront = 298.82;

            Color frameOuter = Color.FromArgb(104, 100, 94);
            Color frameInner = Color.FromArgb(82, 78, 74);
            Color cavity = Color.FromArgb(18, 20, 22);
            Color slat = Color.FromArgb(150, 146, 138);
            Color slatShadow = Color.FromArgb(96, 92, 86);
            Color rust = Color.FromArgb(88, 42, 26);

            double y1 = 82;
            double y2 = 124;
            double z1 = 462;
            double z2 = 548;

            // Внешняя рамка с небольшим объёмом
            AddBlock(297.0, y1, z1, 299.0, y2, z2, null, frameOuter, FaceLayer.WallDetail);

            // Внутренняя рамка
            AddBlock(297.6, y1 + 3, z1 + 3, 298.92, y2 - 3, z2 - 3, null, frameInner, FaceLayer.WallDetail);

            // Тёмная внутренняя ниша
            AddBlock(296.9, y1 + 7, z1 + 7, 298.35, y2 - 7, z2 - 7, null, cavity, FaceLayer.WallDetail);

            // Верхняя и нижняя внутренняя кромка для глубины
            AddBlock(297.2, y2 - 8, z1 + 8, 298.65, y2 - 6, z2 - 8, null, frameInner, FaceLayer.WallDetail);
            AddBlock(297.2, y1 + 6, z1 + 8, 298.65, y1 + 8, z2 - 8, null, frameInner, FaceLayer.WallDetail);

            // Горизонтальные ламели
            int count = 5;
            double innerTop = y2 - 11;
            double innerBottom = y1 + 11;
            double totalHeight = innerTop - innerBottom;
            double gap = totalHeight / count;

            for (int i = 0; i < count; i++)
            {
                double sy1 = innerBottom + i * gap + 0.8;
                double sy2 = sy1 + 2.6;

                AddBlock(297.25, sy1, z1 + 11, 298.72, sy2, z2 - 11, null, slat, FaceLayer.WallDetail);
                AddBlock(297.00, sy1 - 0.15, z1 + 11, 297.25, sy2 + 0.15, z2 - 11, null, slatShadow, FaceLayer.WallDetail);
            }

            // Небольшие ржавые пятна
            AddRectOnXPlane(xFront - 0.10, y2 - 9, z1 + 14, y2 - 5, z1 + 24, rust, FaceLayer.WallDetail);
            AddRectOnXPlane(xFront - 0.10, y1 + 5, z2 - 26, y1 + 9, z2 - 15, rust, FaceLayer.WallDetail);
        }

        private void AddWallPosters()
        {
            AddPosterCoffeeCupBackWall();
            AddPosterOddDonutBackWall();
            AddPosterMothRightWall();
        }

        private void AddPosterBaseOnXPlane(double x, double y1, double z1, double y2, double z2, Color frame, Color paper, Color tape)
        {
            AddRectOnXPlane(x, y1, z1, y2, z2, frame, FaceLayer.WallDetail);
            AddRectOnXPlane(x + 0.02, y1 + 3, z1 + 3, y2 - 3, z2 - 3, paper, FaceLayer.WallDetail);
            AddLabelRect(x + 0.04, y2 - 8, y2 - 2, z1 + 5, z1 + 13, tape);
            AddLabelRect(x + 0.04, y1 + 2, y1 + 8, z2 - 13, z2 - 5, tape);
        }

        private void AddPosterBaseOnZPlane(double z, double x1, double y1, double x2, double y2, Color frame, Color paper, Color tape)
        {
            AddRectOnZPlane(x1, y1, x2, y2, z, frame, FaceLayer.WallDetail);
            AddRectOnZPlane(x1 + 3, y1 + 3, x2 - 3, y2 - 3, z + 0.02, paper, FaceLayer.WallDetail);
            AddRectOnZPlane(x1 + 5, y2 - 8, x1 + 13, y2 - 2, z + 0.04, tape, FaceLayer.WallDetail);
            AddRectOnZPlane(x2 - 13, y1 + 2, x2 - 5, y1 + 8, z + 0.04, tape, FaceLayer.WallDetail);
        }

        private void AddPosterMothRightWall()
        {
            double x = 298.82;
            double y1 = -2;
            double y2 = 66;
            double z1 = 96;
            double z2 = 164;

            Color frame = Color.FromArgb(70, 50, 42);
            Color paper = Color.FromArgb(206, 198, 182);
            Color tape = Color.FromArgb(166, 154, 130);

            Color wing = Color.FromArgb(126, 118, 102);
            Color body = Color.FromArgb(82, 74, 64);
            Color accent = Color.FromArgb(150, 138, 108);
            Color antenna = Color.FromArgb(94, 86, 74);

            AddPosterBaseOnXPlane(x, y1, z1, y2, z2, frame, paper, tape);

            double cy = 30;
            double cz = 130;

            AddLabelDiamond(x - 0.08, cy, cz, 9.5, 3.0, body);

            AddLabelTriangle(x - 0.06, cy + 6.0, cz - 2.0, cy + 16.0, cz - 16.0, cy - 1.0, cz - 11.0, wing);
            AddLabelTriangle(x - 0.06, cy + 6.0, cz + 2.0, cy + 16.0, cz + 16.0, cy - 1.0, cz + 11.0, wing);

            AddLabelTriangle(x - 0.06, cy - 2.0, cz - 2.0, cy - 12.0, cz - 13.0, cy + 3.0, cz - 10.0, wing);
            AddLabelTriangle(x - 0.06, cy - 2.0, cz + 2.0, cy - 12.0, cz + 13.0, cy + 3.0, cz + 10.0, wing);

            AddLabelOval(x - 0.04, cy + 5.5, cz - 9.5, 2.0, 2.8, accent);
            AddLabelOval(x - 0.04, cy + 5.5, cz + 9.5, 2.0, 2.8, accent);
            AddLabelOval(x - 0.04, cy - 4.0, cz - 7.5, 1.7, 2.4, accent);
            AddLabelOval(x - 0.04, cy - 4.0, cz + 7.5, 1.7, 2.4, accent);

            AddScratchOnXPlane(x - 0.02, cy + 9.0, cz - 1.2, cy + 15.0, cz - 4.2, antenna, 0.42);
            AddScratchOnXPlane(x - 0.02, cy + 9.0, cz + 1.2, cy + 15.0, cz + 4.2, antenna, 0.42);
        }

        private void AddPosterCoffeeCupBackWall()
        {
            double z = 1.20;

            double x1 = -270;
            double x2 = -180;
            double y1 = -8;
            double y2 = 86;

            Color frame = Color.FromArgb(76, 52, 40);
            Color paper = Color.FromArgb(206, 198, 182);
            Color tape = Color.FromArgb(166, 154, 130);

            Color stain = Color.FromArgb(194, 184, 164);
            Color saucer = Color.FromArgb(140, 132, 122);
            Color saucerLight = Color.FromArgb(164, 156, 144);
            Color saucerShadow = Color.FromArgb(96, 88, 80);
            Color cupDark = Color.FromArgb(98, 90, 82);
            Color cup = Color.FromArgb(124, 116, 106);
            Color cupLight = Color.FromArgb(146, 138, 126);
            Color rim = Color.FromArgb(162, 152, 140);
            Color coffee = Color.FromArgb(62, 40, 26);
            Color steam = Color.FromArgb(118, 124, 116);
            Color steamLight = Color.FromArgb(142, 148, 138);

            AddPosterBaseOnZPlane(z, x1, y1, x2, y2, frame, paper, tape);

            double cx = -225;
            double cy = 31;

            AddLabelOvalOnZPlane(z + 0.04, cx, cy + 1, 22, 25, stain);

            AddLabelOvalOnZPlane(z + 0.05, cx, 10.8, 18.0, 2.4, saucerShadow);
            AddLabelOvalOnZPlane(z + 0.06, cx, 13.2, 16.2, 3.8, saucer);
            AddLabelOvalOnZPlane(z + 0.07, cx, 14.0, 11.2, 2.0, saucerLight);

            AddRectOnZPlane(cx - 15.0, 20.0, cx - 8.5, 46.0, z + 0.08, cupDark, FaceLayer.WallDetail);
            AddRectOnZPlane(cx - 8.5, 20.0, cx + 6.5, 46.0, z + 0.08, cup, FaceLayer.WallDetail);
            AddRectOnZPlane(cx + 6.5, 20.0, cx + 13.0, 46.0, z + 0.08, cupLight, FaceLayer.WallDetail);
            AddRectOnZPlane(cx - 12.0, 18.4, cx + 10.0, 20.0, z + 0.09, cupDark, FaceLayer.WallDetail);

            AddRectOnZPlane(cx - 16.0, 46.0, cx + 14.0, 49.8, z + 0.10, rim, FaceLayer.WallDetail);
            AddLabelOvalOnZPlane(z + 0.11, cx - 0.5, 48.0, 11.4, 1.2, coffee);

            AddRectOnZPlane(cx + 13.0, 27.0, cx + 21.0, 42.0, z + 0.09, cup, FaceLayer.WallDetail);
            AddRectOnZPlane(cx + 15.2, 30.0, cx + 18.8, 39.0, z + 0.10, paper, FaceLayer.WallDetail);

            AddScratchOnZPlane(cx - 8.5, 51.5, cx - 9.2, 61.0, z + 0.12, steam, 0.65);
            AddScratchOnZPlane(cx, 51.5, cx, 63.0, z + 0.12, steam, 0.68);
            AddScratchOnZPlane(cx + 8.5, 51.5, cx + 9.2, 61.0, z + 0.12, steam, 0.65);

            AddLabelOvalOnZPlane(z + 0.13, cx - 10.0, 66.0, 4.6, 4.0, steam);
            AddLabelOvalOnZPlane(z + 0.14, cx - 4.0, 71.0, 4.0, 3.5, steamLight);
            AddLabelOvalOnZPlane(z + 0.13, cx + 1.0, 69.0, 5.0, 4.4, steam);
            AddLabelOvalOnZPlane(z + 0.14, cx + 6.0, 74.0, 4.2, 3.7, steamLight);
            AddLabelOvalOnZPlane(z + 0.13, cx + 10.0, 66.0, 4.4, 3.8, steam);
        }

        private void AddPosterOddDonutBackWall()
        {
            double z = 1.20;

            double x1 = -165;
            double x2 = -95;
            double y1 = 0;
            double y2 = 68;

            Color frame = Color.FromArgb(72, 48, 38);
            Color paper = Color.FromArgb(210, 200, 182);
            Color tape = Color.FromArgb(170, 158, 132);

            Color stain = Color.FromArgb(196, 186, 168);
            Color donutShadow = Color.FromArgb(102, 78, 66);
            Color donut = Color.FromArgb(138, 96, 82);
            Color donutLight = Color.FromArgb(154, 114, 96);
            Color icing = Color.FromArgb(168, 122, 132);
            Color icingDark = Color.FromArgb(146, 102, 112);
            Color face = Color.FromArgb(84, 58, 48);
            Color sprinkleA = Color.FromArgb(116, 146, 122);
            Color sprinkleB = Color.FromArgb(168, 146, 92);
            Color sprinkleC = Color.FromArgb(146, 124, 170);

            AddPosterBaseOnZPlane(z, x1, y1, x2, y2, frame, paper, tape);

            double cx = -130;
            double cy = 32;

            AddLabelOvalOnZPlane(z + 0.04, cx, cy, 16.5, 13.2, stain);
            AddLabelOvalOnZPlane(z + 0.05, cx, cy - 0.8, 13.8, 10.2, donutShadow);
            AddLabelOvalOnZPlane(z + 0.06, cx, cy, 13.0, 10.0, donut);
            AddLabelOvalOnZPlane(z + 0.07, cx - 2.0, cy + 1.4, 7.0, 5.0, donutLight);
            AddLabelOvalOnZPlane(z + 0.08, cx, cy + 1.8, 10.0, 6.4, icing);

            AddRectOnZPlane(cx - 7.2, cy - 2.0, cx - 4.5, cy + 2.6, z + 0.085, icingDark, FaceLayer.WallDetail);
            AddRectOnZPlane(cx + 4.6, cy - 2.3, cx + 7.1, cy + 2.2, z + 0.085, icingDark, FaceLayer.WallDetail);

            AddLabelOvalOnZPlane(z + 0.09, cx, cy, 5.7, 4.4, Color.FromArgb(120, 88, 76));
            AddLabelOvalOnZPlane(z + 0.10, cx, cy, 4.2, 3.2, paper);

            AddRectOnZPlane(cx - 5.0, cy + 1.0, cx - 3.1, cy + 3.1, z + 0.11, face, FaceLayer.WallDetail);
            AddRectOnZPlane(cx + 3.1, cy + 0.6, cx + 5.0, cy + 2.7, z + 0.11, face, FaceLayer.WallDetail);
            AddScratchOnZPlane(cx - 4.6, cy - 3.0, cx + 4.6, cy - 4.2, z + 0.11, face, 0.68);

            AddRectOnZPlane(cx - 8.0, cy + 5.0, cx - 5.8, cy + 6.4, z + 0.12, sprinkleA, FaceLayer.WallDetail);
            AddRectOnZPlane(cx - 1.0, cy + 5.7, cx + 1.2, cy + 7.0, z + 0.12, sprinkleC, FaceLayer.WallDetail);
            AddRectOnZPlane(cx + 5.4, cy + 3.6, cx + 7.6, cy + 4.9, z + 0.12, sprinkleB, FaceLayer.WallDetail);
        }

        private void AddSubtleAtmosphereDetails()
        {
            Color scratchDark = Color.FromArgb(86, 62, 48, 38);
            Color scratchMid = Color.FromArgb(72, 96, 90, 82);
            Color dust = Color.FromArgb(56, 104, 98, 88);

            AddScratchOnXPlane(-249.10, 42.0, 523.0, 58.5, 534.5, scratchMid, 0.22);
            AddScratchOnXPlane(-249.10, 12.0, 553.0, 20.0, 560.0, scratchDark, 0.20);

            AddScratchOnYPlane(-170.0, 136.0, -158.0, 147.0, -39.65, dust, 0.16);
            AddScratchOnYPlane(-150.0, 155.0, -136.0, 163.0, -39.65, scratchDark, 0.14);
            AddScratchOnYPlane(128.0, 149.0, 142.0, 161.0, -39.65, dust, 0.16);
            AddScratchOnYPlane(151.0, 130.0, 166.0, 140.0, -39.65, scratchDark, 0.14);

            AddScratchOnYPlane(-229.0, 142.5, -219.0, 153.0, -54.65, dust, 0.14);
            AddScratchOnYPlane(-154.0, 208.0, -146.0, 221.0, -54.65, dust, 0.14);
            AddScratchOnYPlane(206.0, 141.0, 215.0, 152.0, -54.65, dust, 0.14);

            AddDetailedCobwebOnXPlane(-249.12, 49.0, 414.0, 12.0, true, true);
            AddDetailedCobwebOnXPlane(-249.12, 50.0, 490.0, 10.0, true, false);

            AddDetailedCobwebOnZPlane(-59.0, 44.0, 596.16, 8.5, true, true);
            AddDetailedCobwebOnZPlane(60.0, 44.0, 596.16, 7.5, false, true);

            AddDetailedCobwebOnXPlane(-298.75, 58.0, 262.0, 10.0, true, true);
            AddDetailedCobwebOnXPlane(-298.75, 58.0, 308.0, 9.0, true, false);

            AddDetailedCobwebOnZPlane(52.0, -58.0, 48.30, 9.5, false, false);
            AddDetailedCobwebOnZPlane(-52.0, -58.0, 48.30, 8.5, true, false);
            AddDetailedCobwebOnZPlane(38.0, 40.0, 62.22, 7.0, false, true);

            AddDetailedCobwebOnZPlane(260.0, 92.0, 592.78, 7.0, false, false);
        }

        private void AddWebThreadOnZPlane(double x1, double y1, double x2, double y2, double z, Color color, double width = 0.14)
        {
            AddScratchOnZPlane(x1, y1, x2, y2, z, color, width);
        }

        private void AddWebThreadOnXPlane(double x, double y1, double z1, double y2, double z2, Color color, double width = 0.14)
        {
            AddScratchOnXPlane(x, y1, z1, y2, z2, color, width);
        }

        private void AddDetailedCobwebOnZPlane(double cornerX, double cornerY, double z, double size, bool toRight, bool toDown)
        {
            int sx = toRight ? 1 : -1;
            int sy = toDown ? -1 : 1;

            Color main = Color.FromArgb(95, 132, 128, 120);
            Color thin = Color.FromArgb(70, 150, 146, 138);

            double[] rx = { 0.12, 0.24, 0.39, 0.58, 0.82 };
            double[] ry = { 0.92, 0.80, 0.63, 0.40, 0.16 };

            AddWebThreadOnZPlane(cornerX, cornerY, cornerX + sx * size, cornerY, z, thin, 0.10);
            AddWebThreadOnZPlane(cornerX, cornerY, cornerX, cornerY + sy * size, z, thin, 0.10);

            for (int i = 0; i < rx.Length; i++)
                AddWebThreadOnZPlane(cornerX, cornerY, cornerX + sx * size * rx[i], cornerY + sy * size * ry[i], z, main, 0.13);

            double[] rings = { 0.22, 0.40, 0.59, 0.79 };

            for (int r = 0; r < rings.Length; r++)
            {
                double t = rings[r];

                for (int i = 0; i < rx.Length - 1; i++)
                {
                    if ((r == 1 && i == 1) || (r == 3 && i == 0))
                        continue;

                    double ax = cornerX + sx * size * rx[i] * t;
                    double ay = cornerY + sy * size * ry[i] * t;
                    double bx = cornerX + sx * size * rx[i + 1] * t;
                    double by = cornerY + sy * size * ry[i + 1] * t;

                    AddWebThreadOnZPlane(ax, ay, bx, by, z, thin, 0.10);
                }
            }

            AddWebThreadOnZPlane(cornerX + sx * size * 0.36, cornerY + sy * size * 0.48, cornerX + sx * size * 0.42, cornerY + sy * size * 0.66, z, thin, 0.09);
        }

        private void AddDetailedCobwebOnXPlane(double x, double cornerY, double cornerZ, double size, bool toDown, bool toRightZ)
        {
            int sy = toDown ? -1 : 1;
            int sz = toRightZ ? 1 : -1;

            Color main = Color.FromArgb(95, 132, 128, 120);
            Color thin = Color.FromArgb(70, 150, 146, 138);

            double[] ry = { 0.92, 0.80, 0.63, 0.40, 0.16 };
            double[] rz = { 0.12, 0.24, 0.39, 0.58, 0.82 };

            AddWebThreadOnXPlane(x, cornerY, cornerZ, cornerY + sy * size, cornerZ, thin, 0.10);
            AddWebThreadOnXPlane(x, cornerY, cornerZ, cornerY, cornerZ + sz * size, thin, 0.10);

            for (int i = 0; i < ry.Length; i++)
                AddWebThreadOnXPlane(x, cornerY + sy * size * ry[i], cornerZ + sz * size * rz[i], cornerY, cornerZ, main, 0.13);

            double[] rings = { 0.22, 0.40, 0.59, 0.79 };

            for (int r = 0; r < rings.Length; r++)
            {
                double t = rings[r];

                for (int i = 0; i < ry.Length - 1; i++)
                {
                    if ((r == 0 && i == 2) || (r == 2 && i == 0))
                        continue;

                    double ay = cornerY + sy * size * ry[i] * t;
                    double az = cornerZ + sz * size * rz[i] * t;
                    double by = cornerY + sy * size * ry[i + 1] * t;
                    double bz = cornerZ + sz * size * rz[i + 1] * t;

                    AddWebThreadOnXPlane(x, ay, az, by, bz, thin, 0.10);
                }
            }

            AddWebThreadOnXPlane(x, cornerY + sy * size * 0.46, cornerZ + sz * size * 0.32, cornerY + sy * size * 0.68, cornerZ + sz * size * 0.36, thin, 0.09);
        }

        private void AddScratchOnZPlane(
    double x1, double y1,
    double x2, double y2,
    double z,
    Color color,
    double width = 0.28,
    FaceLayer layer = FaceLayer.SmallDetail)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);

            if (len < 0.001)
                return;

            double ox = -dy / len * width * 0.5;
            double oy = dx / len * width * 0.5;

            AddQuad(
                x1 - ox, y1 - oy, z,
                x1 + ox, y1 + oy, z,
                x2 + ox, y2 + oy, z,
                x2 - ox, y2 - oy, z,
                null,
                color,
                layer
            );
        }

        private void AddScratchOnXPlane(
    double x,
    double y1, double z1,
    double y2, double z2,
    Color color,
    double width = 0.28,
    FaceLayer layer = FaceLayer.SmallDetail)
        {
            double dy = y2 - y1;
            double dz = z2 - z1;
            double len = Math.Sqrt(dy * dy + dz * dz);

            if (len < 0.001)
                return;

            double oy = -dz / len * width * 0.5;
            double oz = dy / len * width * 0.5;

            AddQuad(
                x, y1 - oy, z1 - oz,
                x, y1 + oy, z1 + oz,
                x, y2 + oy, z2 + oz,
                x, y2 - oy, z2 - oz,
                null,
                color,
                layer
            );
        }

        private void AddScratchOnYPlane(
    double x1, double z1,
    double x2, double z2,
    double y,
    Color color,
    double width = 0.22,
    FaceLayer layer = FaceLayer.SmallDetail)
        {
            double dx = x2 - x1;
            double dz = z2 - z1;
            double len = Math.Sqrt(dx * dx + dz * dz);

            if (len < 0.001)
                return;

            double ox = -dz / len * width * 0.5;
            double oz = dx / len * width * 0.5;

            AddQuad(
                x1 - ox, y, z1 - oz,
                x1 + ox, y, z1 + oz,
                x2 + ox, y, z2 + oz,
                x2 - ox, y, z2 - oz,
                null,
                color,
                layer
            );
        }

        private void AddNaturalBoardsOnYPlane(double x1, double z1, double x2, double z2, double y, params double[] cutsZ)
        {
            Color[] tones =
            {
                HorrorColor.WoodPlankA,
                HorrorColor.WoodPlankB,
                HorrorColor.WoodPlankC,
                HorrorColor.WoodPlankD
            };

            double prevZ = z1;
            int plankIndex = 0;

            for (int i = 0; i <= cutsZ.Length; i++)
            {
                double nextZ = (i < cutsZ.Length) ? cutsZ[i] : z2;

                if (nextZ - prevZ < 0.6)
                    continue;

                Color tone = tones[plankIndex % tones.Length];

                AddQuad(x1, y, prevZ, x2, y, prevZ, x2, y, nextZ, x1, y, nextZ, null, tone, FaceLayer.SmallDetail);

                double highlightEnd = prevZ + (nextZ - prevZ) * 0.22;
                if (highlightEnd > prevZ + 0.15)
                    AddQuad(x1 + 0.8, y + 0.02, prevZ + 0.15, x2 - 0.8, y + 0.02, prevZ + 0.15, x2 - 0.8, y + 0.02, highlightEnd, x1 + 0.8, y + 0.02, highlightEnd, null, HorrorColor.WoodHighlight, FaceLayer.SmallDetail);

                if (i < cutsZ.Length)
                {
                    double seamZ = cutsZ[i];
                    AddQuad(x1, y + 0.03, seamZ - 0.12, x2, y + 0.03, seamZ - 0.12, x2, y + 0.03, seamZ + 0.12, x1, y + 0.03, seamZ + 0.12, null, HorrorColor.WoodSeam, FaceLayer.SmallDetail);
                }

                prevZ = nextZ;
                plankIndex++;
            }
        }

        private void AddNaturalBoardsOnXPlane(double x, double y1, double z1, double y2, double z2, params double[] cutsZ)
        {
            Color[] tones =
            {
                HorrorColor.WoodPlankA,
                HorrorColor.WoodPlankB,
                HorrorColor.WoodPlankC,
                HorrorColor.WoodPlankD
            };

            double prevZ = z1;
            int plankIndex = 0;

            for (int i = 0; i <= cutsZ.Length; i++)
            {
                double nextZ = (i < cutsZ.Length) ? cutsZ[i] : z2;

                if (nextZ - prevZ < 0.6)
                    continue;

                Color tone = tones[plankIndex % tones.Length];

                AddQuad(x, y1, prevZ, x, y2, prevZ, x, y2, nextZ, x, y1, nextZ, null, tone, FaceLayer.SmallDetail);

                double highlightEnd = prevZ + (nextZ - prevZ) * 0.20;
                if (highlightEnd > prevZ + 0.15)
                    AddQuad(x + 0.02, y1 + 1.0, prevZ + 0.15, x + 0.02, y2 - 1.0, prevZ + 0.15, x + 0.02, y2 - 1.0, highlightEnd, x + 0.02, y1 + 1.0, highlightEnd, null, HorrorColor.WoodHighlight, FaceLayer.SmallDetail);

                if (i < cutsZ.Length)
                {
                    double seamZ = cutsZ[i];
                    AddQuad(x + 0.03, y1, seamZ - 0.12, x + 0.03, y2, seamZ - 0.12, x + 0.03, y2, seamZ + 0.12, x + 0.03, y1, seamZ + 0.12, null, HorrorColor.WoodSeam, FaceLayer.SmallDetail);
                }

                prevZ = nextZ;
                plankIndex++;
            }
        }

        private void AddNaturalBoardsOnZPlane(double x1, double y1, double x2, double y2, double z, params double[] cutsX)
        {
            Color[] tones =
            {
                HorrorColor.WoodPlankA,
                HorrorColor.WoodPlankB,
                HorrorColor.WoodPlankC,
                HorrorColor.WoodPlankD
            };

            double prevX = x1;
            int plankIndex = 0;

            for (int i = 0; i <= cutsX.Length; i++)
            {
                double nextX = (i < cutsX.Length) ? cutsX[i] : x2;

                if (nextX - prevX < 0.6)
                    continue;

                Color tone = tones[plankIndex % tones.Length];

                AddQuad(prevX, y1, z, prevX, y2, z, nextX, y2, z, nextX, y1, z, null, tone, FaceLayer.SmallDetail);

                double highlightEnd = prevX + (nextX - prevX) * 0.18;
                if (highlightEnd > prevX + 0.15)
                    AddQuad(prevX + 0.15, y1 + 1.0, z + 0.02, prevX + 0.15, y2 - 1.0, z + 0.02, highlightEnd, y2 - 1.0, z + 0.02, highlightEnd, y1 + 1.0, z + 0.02, null, HorrorColor.WoodHighlight, FaceLayer.SmallDetail);

                if (i < cutsX.Length)
                {
                    double seamX = cutsX[i];
                    AddQuad(seamX - 0.12, y1, z + 0.03, seamX - 0.12, y2, z + 0.03, seamX + 0.12, y2, z + 0.03, seamX + 0.12, y1, z + 0.03, null, HorrorColor.WoodSeam, FaceLayer.SmallDetail);
                }

                prevX = nextX;
                plankIndex++;
            }
        }

        private void AddRectOnZPlane(double x1, double y1, double x2, double y2, double z, Color color, FaceLayer layer = FaceLayer.WallDetail)
        {
            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);
            double minY = Math.Min(y1, y2);
            double maxY = Math.Max(y1, y2);

            AddQuad(minX, minY, z, minX, maxY, z, maxX, maxY, z, maxX, minY, z, null, color, layer);
        }

        private void AddRectOnXPlane(double x, double y1, double z1, double y2, double z2, Color color, FaceLayer layer = FaceLayer.WallDetail)
        {
            double minY = Math.Min(y1, y2);
            double maxY = Math.Max(y1, y2);
            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);

            AddQuad(x, minY, minZ, x, maxY, minZ, x, maxY, maxZ, x, minY, maxZ, null, color, layer);
        }

        private void AddRectOnYPlane(double x1, double z1, double x2, double z2, double y, Color color, FaceLayer layer = FaceLayer.SmallDetail)
        {
            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);
            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);

            AddQuad(minX, y, minZ, maxX, y, minZ, maxX, y, maxZ, minX, y, maxZ, null, color, layer);
        }

        private void AddMatTextOnYPlane(
    double centerX,
    double centerZ,
    string text,
    double y,
    Color mainColor,
    Color outlineColor)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            text = text.ToUpperInvariant();

            double cell = 1.55;
            double gap = 1.55;
            double glyphWidth = 5 * cell;
            double glyphHeight = 7 * cell;

            double totalWidth = 0;

            for (int i = 0; i < text.Length; i++)
            {
                totalWidth += glyphWidth;

                if (i < text.Length - 1)
                    totalWidth += gap;
            }

            double startX = centerX - totalWidth * 0.5;
            double startZ = centerZ - glyphHeight * 0.5;

            DrawMatTextLayer(startX - 0.65, startZ, text, y + 0.060, outlineColor, cell, gap);
            DrawMatTextLayer(startX + 0.65, startZ, text, y + 0.061, outlineColor, cell, gap);
            DrawMatTextLayer(startX, startZ - 0.65, text, y + 0.062, outlineColor, cell, gap);
            DrawMatTextLayer(startX, startZ + 0.65, text, y + 0.063, outlineColor, cell, gap);

            DrawMatTextLayer(startX, startZ, text, y + 0.080, mainColor, cell, gap);
        }

        private void DrawMatTextLayer(
            double startX,
            double startZ,
            string text,
            double y,
            Color color,
            double cell,
            double gap)
        {
            double cursorX = startX;

            for (int i = 0; i < text.Length; i++)
            {
                DrawMatGlyph(cursorX, startZ, text[i], y, color, cell);
                cursorX += 5 * cell + gap;
            }
        }

        private void DrawMatGlyph(
    double x,
    double z,
    char c,
    double y,
    Color color,
    double cell)
        {
            string[] pattern = GetMatGlyphPattern(c);

            int rows = pattern.Length;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < pattern[row].Length; col++)
                {
                    if (pattern[row][col] != '1')
                        continue;

                    double x1 = x + col * cell;
                    double x2 = x1 + cell * 0.88;

                    // ВАЖНО:
                    // Переворачиваем строки по Z.
                    // Верх буквы должен быть дальше от игрока, низ буквы ближе к игроку.
                    double z1 = z + (rows - 1 - row) * cell;
                    double z2 = z1 + cell * 0.88;

                    AddMatRect(x1, z1, x2, z2, y, color);
                }
            }
        }

        private string[] GetMatGlyphPattern(char c)
        {
            switch (c)
            {
                case 'W':
                    return new[]
                    {
                "10001",
                "10001",
                "10001",
                "10101",
                "10101",
                "11011",
                "10001"
            };

                case 'E':
                    return new[]
                    {
                "11111",
                "10000",
                "10000",
                "11110",
                "10000",
                "10000",
                "11111"
            };

                case 'L':
                    return new[]
                    {
                "10000",
                "10000",
                "10000",
                "10000",
                "10000",
                "10000",
                "11111"
            };

                case 'C':
                    return new[]
                    {
                "11111",
                "10000",
                "10000",
                "10000",
                "10000",
                "10000",
                "11111"
            };

                case 'O':
                    return new[]
                    {
                "11111",
                "10001",
                "10001",
                "10001",
                "10001",
                "10001",
                "11111"
            };

                case 'M':
                    return new[]
                    {
                "10001",
                "11011",
                "10101",
                "10101",
                "10001",
                "10001",
                "10001"
            };

                default:
                    return new[]
                    {
                "00000",
                "00000",
                "00000",
                "00000",
                "00000",
                "00000",
                "00000"
            };
            }
        }

        private void AddMatRect(double x1, double z1, double x2, double z2, double y, Color color)
        {
            AddRectOnYPlane(
                x1,
                z1,
                x2,
                z2,
                y,
                color,
                FaceLayer.SmallDetail
            );
        }

        private void AddDustRect(double x1, double z1, double x2, double z2, double y, Color color)
        {
            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);
            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);

            AddQuad(
                minX, y, minZ,
                maxX, y, minZ,
                maxX, y, maxZ,
                minX, y, maxZ,
                "no_outline",
                color,
                FaceLayer.SmallDetail
            );
        }

        private void AddEdge(int startIndex, int endIndex)
        {
            _model.AddEdge(_model.Points[startIndex], _model.Points[endIndex]);
        }

        private void AddBoxEdges(int p0, int p1, int p2, int p3, int p4, int p5, int p6, int p7)
        {
            AddEdge(p0, p1);
            AddEdge(p1, p2);
            AddEdge(p2, p3);
            AddEdge(p3, p0);

            AddEdge(p4, p5);
            AddEdge(p5, p6);
            AddEdge(p6, p7);
            AddEdge(p7, p4);

            AddEdge(p0, p4);
            AddEdge(p1, p5);
            AddEdge(p2, p6);
            AddEdge(p3, p7);
        }

        private void AddOutlineOnWall(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            int p0 = _model.AddPoint(x1, y1, z1);
            int p1 = _model.AddPoint(x2, y1, z1 == z2 ? z1 : z2);
            int p2 = _model.AddPoint(x2, y2, z2);
            int p3 = _model.AddPoint(x1, y2, z1);

            AddEdge(p0, p1);
            AddEdge(p1, p2);
            AddEdge(p2, p3);
            AddEdge(p3, p0);
        }

        private void AddBlock(double x1, double y1, double z1, double x2, double y2, double z2, string textureKey = null, Color color = default, FaceLayer layer = FaceLayer.Furniture)
        {
            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);

            double minY = Math.Min(y1, y2);
            double maxY = Math.Max(y1, y2);

            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);

            Color finalColor = color == default ? Color.Empty : color;

            int s = _model.Points.Count;

            _model.AddPoint(minX, minY, minZ);
            _model.AddPoint(maxX, minY, minZ);
            _model.AddPoint(maxX, minY, maxZ);
            _model.AddPoint(minX, minY, maxZ);

            _model.AddPoint(minX, maxY, minZ);
            _model.AddPoint(maxX, maxY, minZ);
            _model.AddPoint(maxX, maxY, maxZ);
            _model.AddPoint(minX, maxY, maxZ);

            AddEdge(s + 0, s + 1);
            AddEdge(s + 1, s + 2);
            AddEdge(s + 2, s + 3);
            AddEdge(s + 3, s + 0);

            AddEdge(s + 4, s + 5);
            AddEdge(s + 5, s + 6);
            AddEdge(s + 6, s + 7);
            AddEdge(s + 7, s + 4);

            AddEdge(s + 0, s + 4);
            AddEdge(s + 1, s + 5);
            AddEdge(s + 2, s + 6);
            AddEdge(s + 3, s + 7);

            _model.AddFace(new List<int> { s + 0, s + 1, s + 2, s + 3 }, textureKey, finalColor, layer);
            _model.AddFace(new List<int> { s + 4, s + 7, s + 6, s + 5 }, textureKey, finalColor, layer);
            _model.AddFace(new List<int> { s + 0, s + 4, s + 5, s + 1 }, textureKey, finalColor, layer);
            _model.AddFace(new List<int> { s + 3, s + 2, s + 6, s + 7 }, textureKey, finalColor, layer);
            _model.AddFace(new List<int> { s + 0, s + 3, s + 7, s + 4 }, textureKey, finalColor, layer);
            _model.AddFace(new List<int> { s + 1, s + 5, s + 6, s + 2 }, textureKey, finalColor, layer);

            TryAddColliderForBlock(minX, minY, minZ, maxX, maxY, maxZ, layer, textureKey);
        }

        private void AddQuad(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3, double x4, double y4, double z4, string textureKey, Color color, FaceLayer layer)
        {
            int p1 = _model.AddPoint(x1, y1, z1);
            int p2 = _model.AddPoint(x2, y2, z2);
            int p3 = _model.AddPoint(x3, y3, z3);
            int p4 = _model.AddPoint(x4, y4, z4);

            _model.AddFace(new List<int> { p1, p2, p3, p4 }, textureKey, color, layer, true);
        }

        private void TryAddColliderForBlock(double minX, double minY, double minZ, double maxX, double maxY, double maxZ, FaceLayer layer, string name)
        {
            if (layer != FaceLayer.Furniture)
                return;

            if (maxX - minX < 12 || maxZ - minZ < 12)
                return;

            if (maxY < -90)
                return;

            _model.AddCollider(minX, minZ, maxX, maxZ, name);
        }

        private void AddManualFurnitureColliders()
        {
            _model.AddCollider(-190, 110, -110, 190, "table_left");
            _model.AddCollider(110, 110, 190, 190, "table_right");

            _model.AddCollider(-245, 125, -190, 175, "chair_left_front");
            _model.AddCollider(-175, 190, -125, 245, "chair_left_back");
            _model.AddCollider(190, 125, 245, 175, "chair_right_front");

            _model.AddCollider(255, 456, 299, 590, "coffee_machine_wall_counter");
        }

        public void MoveCamera(double localDx, double localDy, double localDz)
        {
            double cos = Math.Cos(Camera.Yaw);
            double sin = Math.Sin(Camera.Yaw);

            double worldDx = localDx * cos + localDz * sin;
            double worldDz = -localDx * sin + localDz * cos;

            double oldX = Camera.X;
            double oldZ = Camera.Z;

            double targetX = oldX + worldDx;
            double targetZ = oldZ + worldDz;

            bool moved = false;

            if (!IsBlocked(targetX, targetZ))
            {
                Camera.X = targetX;
                Camera.Z = targetZ;
                moved = true;
            }
            else
            {
                if (!IsBlocked(targetX, oldZ))
                {
                    Camera.X = targetX;
                    moved = true;
                }

                if (!IsBlocked(Camera.X, targetZ))
                {
                    Camera.Z = targetZ;
                    moved = true;
                }
            }

            double newY = Clamp(Camera.Y + localDy, -20, 50);

            if (Math.Abs(newY - Camera.Y) > 0.001)
            {
                Camera.Y = newY;
                moved = true;
            }

            if (moved)
                _model.NotifyChanged();
        }

        public void RotateCamera(double yawDelta)
        {
            RotateCamera(yawDelta, 0);
        }

        public void RotateCamera(double yawDelta, double pitchDelta)
        {
            Camera.Yaw += yawDelta;

            if (Camera.Yaw > Math.PI * 2)
                Camera.Yaw -= Math.PI * 2;

            if (Camera.Yaw < -Math.PI * 2)
                Camera.Yaw += Math.PI * 2;

            Camera.Pitch = Clamp(Camera.Pitch + pitchDelta, -0.85, 0.85);
            _model.NotifyChanged();
        }

        public void RespawnCamera()
        {
            if (!IsBlocked(SpawnX, SpawnZ))
            {
                Camera.Set(SpawnX, SpawnY, SpawnZ, SpawnYaw);
                _model.NotifyChanged();
                return;
            }

            double[,] candidates =
            {
                { 250, 300 },
                { 240, 300 },
                { 230, 300 },
                { 220, 300 },
                { 0, 120 }
            };

            for (int i = 0; i < candidates.GetLength(0); i++)
            {
                double x = candidates[i, 0];
                double z = candidates[i, 1];

                if (!IsBlocked(x, z))
                {
                    Camera.Set(x, SpawnY, z, SpawnYaw);
                    _model.NotifyChanged();
                    return;
                }
            }

            Camera.Set(0, 0, 100, 0);
            _model.NotifyChanged();
        }

        private bool IsBlocked(double x, double z)
        {
            if (x < RoomMinX + PlayerRadius || x > RoomMaxX - PlayerRadius)
                return true;

            if (z < RoomMinZ + PlayerRadius || z > RoomMaxZ - PlayerRadius)
                return true;

            if (_tiaBarPassageBlocked)
            {
                // Невидимая сплошная стенка на выходе из-за бара в гостевую зону
                // около левого прохода рядом с зоной сиропов.
                // Делается через полноценную проверку коллизии, чтобы игрок не мог
                // "проскочить" сквозь неё ни на кадр.
                if (CircleIntersectsBox(x, z, PlayerRadius, -270, 330, -92, 392))
                    return true;
            }

            for (int i = 0; i < _model.Colliders.Count; i++)
            {
                BoxCollider collider = _model.Colliders[i];

                if (collider.Enabled && CircleIntersectsBox(x, z, PlayerRadius, collider))
                    return true;
            }

            return false;
        }

        private bool CircleIntersectsBox(double cx, double cz, double radius, double minX, double minZ, double maxX, double maxZ)
        {
            double closestX = Clamp(cx, minX, maxX);
            double closestZ = Clamp(cz, minZ, maxZ);

            double dx = cx - closestX;
            double dz = cz - closestZ;

            return dx * dx + dz * dz < radius * radius;
        }

        private bool CircleIntersectsBox(double cx, double cz, double radius, BoxCollider box)
        {
            double closestX = Clamp(cx, box.MinX, box.MaxX);
            double closestZ = Clamp(cz, box.MinZ, box.MaxZ);

            double dx = cx - closestX;
            double dz = cz - closestZ;

            return dx * dx + dz * dz < radius * radius;
        }

        private double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}