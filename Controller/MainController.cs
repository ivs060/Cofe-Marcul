using System;
using System.Collections.Generic;
using System.Drawing;
using игра_для_проги.Model;

namespace игра_для_проги.Controller
{
    public class MainController
    {
        private SceneModel _model;

        public Camera3D Camera { get; private set; }

        private const double PlayerRadius = 18.0;

        private const double RoomMinX = -285;
        private const double RoomMaxX = 285;
        private const double RoomMinZ = 20;
        private const double RoomMaxZ = 585;

        private const double SpawnX = 0;
        private const double SpawnY = 0;
        private const double SpawnZ = 75;
        private const double SpawnYaw = 0;

        public MainController(SceneModel model)
        {
            _model = model;
            Camera = new Camera3D();
        }

        public void CreateTestScene()
        {
            _model.Points.Clear();
            _model.Edges.Clear();
            _model.Faces.Clear();
            _model.Colliders.Clear();

            // -------------------------------
            // 1. КОМНАТА
            // -------------------------------

            int p0 = _model.AddPoint(-300, -101, 0);
            int p1 = _model.AddPoint(300, -101, 0);
            int p2 = _model.AddPoint(300, -101, 600);
            int p3 = _model.AddPoint(-300, -101, 600);

            int p4 = _model.AddPoint(-300, 150, 0);
            int p5 = _model.AddPoint(300, 150, 0);
            int p6 = _model.AddPoint(300, 150, 600);
            int p7 = _model.AddPoint(-300, 150, 600);

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

            // Пол
            _model.AddFace(
                new List<int> { p0, p3, p2, p1 },
                "floor",
                Color.Empty,
                FaceLayer.Floor
            );

            // Потолок
            _model.AddFace(
                new List<int> { p4, p5, p6, p7 },
                "floor",
                Color.Empty,
                FaceLayer.Floor
            );

            // Дальняя стена
            _model.AddFace(
                new List<int> { p3, p7, p6, p2 },
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            // Правая стена
            _model.AddFace(
                new List<int> { p1, p2, p6, p5 },
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            // Ближняя стена
            _model.AddFace(
                new List<int> { p0, p1, p5, p4 },
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            AddLeftWallWithWindow();
            AddDoorWithGlass();
            AddRightWallServiceDoor();

            // Доп. контур окна
            int w0 = _model.AddPoint(-300, -20, 260);
            int w1 = _model.AddPoint(-300, -20, 310);
            int w2 = _model.AddPoint(-300, 60, 310);
            int w3 = _model.AddPoint(-300, 60, 260);

            AddEdge(w0, w1);
            AddEdge(w1, w2);
            AddEdge(w2, w3);
            AddEdge(w3, w0);

            // Доп. контур двери
            int d0 = _model.AddPoint(-188, -100, 600);
            int d1 = _model.AddPoint(-124, -100, 600);
            int d2 = _model.AddPoint(-124, 72, 600);
            int d3 = _model.AddPoint(-188, 72, 600);

            AddEdge(d0, d1);
            AddEdge(d1, d2);
            AddEdge(d2, d3);
            AddEdge(d3, d0);

            // -------------------------------
            // 2. ХОЛОДИЛЬНИК
            // -------------------------------

            AddBlock(
                -295, -100, 510,
                -250, 80, 595,
                "fridge",
                Color.Empty,
                FaceLayer.Furniture
            );
            AddFridgeHandle();

            // -------------------------------
            // 3. БАРНАЯ СТОЙКА
            // -------------------------------

            AddMainBarCounter();
            AddSideBarCounter();
            AddBarDecorPanels();

            // Кофемашина — только на центральном блоке
            AddCoffeeMachine(-90, -35, 400);

            // Касса + чековый аппарат — на правом блоке
            AddCashRegister(44, -35, 398);

            // Раковина — тоже на правом блоке, правее кассы
            AddBuiltInSink(134, -35, 388);

            // Люстра
            AddPendantLamp(20, 420);

            // -------------------------------
            // 4. СТОЛЫ
            // -------------------------------

            AddBlock(-185, -45, 115, -115, -40, 185, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(-180, -100, 175, -175, -46, 180, "wood_dark");
            AddBlock(-125, -100, 175, -120, -46, 180, "wood_dark");
            AddBlock(-180, -100, 120, -175, -46, 125, "wood_dark");
            AddBlock(-125, -100, 120, -120, -46, 125, "wood_dark");

            AddBlock(115, -45, 115, 185, -40, 185, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(120, -100, 175, 125, -46, 180, "wood_dark");
            AddBlock(175, -100, 175, 180, -46, 180, "wood_dark");
            AddBlock(120, -100, 120, 125, -46, 125, "wood_dark");
            AddBlock(175, -100, 120, 180, -46, 125, "wood_dark");

            // -------------------------------
            // 5. СТУЛЬЯ
            // -------------------------------

            AddBlock(-240, -60, 130, -195, -55, 170, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(-240, -25, 140, -235, -5, 160, "wood_dark");
            AddBlock(-240, -100, 160, -235, 0, 170, "wood_dark");
            AddBlock(-240, -100, 130, -235, 0, 140, "wood_dark");
            AddBlock(-200, -100, 130, -195, -61, 140, "wood_dark");
            AddBlock(-200, -100, 160, -195, -61, 170, "wood_dark");

            AddBlock(-170, -60, 195, -130, -55, 240, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(-160, -25, 235, -140, -5, 240, "wood_dark");
            AddBlock(-170, -100, 235, -160, 0, 240, "wood_dark");
            AddBlock(-130, -100, 235, -140, 0, 240, "wood_dark");
            AddBlock(-170, -100, 195, -160, -61, 200, "wood_dark");
            AddBlock(-130, -100, 195, -140, -61, 200, "wood_dark");

            AddBlock(195, -60, 130, 240, -55, 170, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(240, -25, 140, 245, -5, 160, "wood_dark");
            AddBlock(240, -100, 160, 245, 0, 170, "wood_dark");
            AddBlock(240, -100, 130, 245, 0, 140, "wood_dark");
            AddBlock(195, -100, 130, 200, -61, 140, "wood_dark");
            AddBlock(195, -100, 160, 200, -61, 170, "wood_dark");

            // -------------------------------
            // 6. ШКАФ У ХОЛОДИЛЬНИКА
            // -------------------------------

            AddOpenWallCabinet(-299, -65, 408, -250, 55, 498);

            AddCabinetSyrups(-299, -250, -65, 55, 408, 498);

        // -------------------------------
        // 7. ПОЛКА С ЧАЙНЫМИ ПАРАМИ
        // -------------------------------

        AddTeaShelf();

            // -------------------------------
            // 8. НАСТЕННОЕ МЕНЮ
            // -------------------------------

            AddBlock(
                90, -20, 593,
                270, 100, 598,
                "mid_wood",
                Color.Empty,
                FaceLayer.Wall
            );

            AddQuad(
    97, -13, 592.85,
    97, 93, 592.85,
    263, 93, 592.85,
    263, -13, 592.85,
    null,
    Color.FromArgb(42, 72, 52),
    FaceLayer.WallDetail
);

            // -------------------------------
            // 9. ТУМБА С ТЕЛЕВИЗОРОМ
            // -------------------------------

            AddBlock(-30, -100, 45, 30, -40, 5, "wood_dark");

            AddBlock(
                -20, -40, 40,
                20, -10, 10,
                "screen",
                Color.Black,
                FaceLayer.Furniture
            );

            AddManualFurnitureColliders();

            RespawnCamera();
        }

        // =========================================================
        // БАР
        // =========================================================

        private void AddMainBarCounter()
        {
            AddBlock(-225, -100, 380, -95, -35, 450, "bar", Color.Empty, FaceLayer.Furniture);
            AddBlock(-95, -100, 380, 35, -35, 450, "bar", Color.Empty, FaceLayer.Furniture);
            AddBlock(35, -100, 380, 175, -35, 450, "bar", Color.Empty, FaceLayer.Furniture);
        }

        private void AddSideBarCounter()
        {
            AddBlock(175, -100, 380, 205, -35, 490, "bar", Color.Empty, FaceLayer.Furniture);
        }

        private void AddBarDecorPanels()
        {
            Color panelColor = Color.FromArgb(112, 68, 36);

            double z = 379.4;

            AddQuad(
                -213, -95, z,
                -213, -42, z,
                -85, -42, z,
                -85, -95, z,
                null,
                panelColor,
                FaceLayer.WallDetail
            );

            AddQuad(
                -75, -95, z,
                -75, -42, z,
                43, -42, z,
                43, -95, z,
                null,
                panelColor,
                FaceLayer.WallDetail
            );

            AddQuad(
                53, -95, z,
                53, -42, z,
                167, -42, z,
                167, -95, z,
                null,
                panelColor,
                FaceLayer.WallDetail
            );
        }

        // =========================================================
        // ЭТИКЕТКИ К СИРОПАМ
        // =========================================================

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

        // =========================================================
        // ДЕТАЛИ
        // =========================================================

        private void AddFridgeHandle()
        {
            AddBlock(
                -249.2, -25, 545,
                -245.6, 55, 553,
                null,
                Color.FromArgb(168, 172, 178),
                FaceLayer.Furniture
            );
        }

        private void AddPendantLamp(double centerX, double centerZ)
        {
            Color cable = Color.FromArgb(45, 45, 45);
            Color shade = Color.FromArgb(218, 220, 224);
            Color bulb = Color.FromArgb(245, 232, 185);

            AddBlock(
                centerX - 2, 146, centerZ - 2,
                centerX + 2, 150, centerZ + 2,
                null,
                cable,
                FaceLayer.Furniture
            );

            AddBlock(
                centerX - 1, 96, centerZ - 1,
                centerX + 1, 146, centerZ + 1,
                null,
                cable,
                FaceLayer.Furniture
            );

            // Упрощенная люстра без "внутренностей"
            AddBlock(
                centerX - 24, 74, centerZ - 24,
                centerX + 24, 86, centerZ + 24,
                null,
                shade,
                FaceLayer.Furniture
            );

            AddBlock(
                centerX - 8, 67, centerZ - 8,
                centerX + 8, 74, centerZ + 8,
                null,
                bulb,
                FaceLayer.Furniture
            );
        }

        private void AddCoffeeMachine(double x, double y, double z)
        {
            Color body = Color.FromArgb(202, 205, 210);
            Color bodyDark = Color.FromArgb(168, 172, 178);
            Color bodyLight = Color.FromArgb(224, 226, 230);

            // ВАЖНО: широкая передняя полоса теперь тёмно-серая, не чёрная
            Color frontStripe = Color.FromArgb(72, 78, 86);

            // А мелкие детали остаются чёрными
            Color blackDetail = Color.FromArgb(18, 22, 28);

            Color tray = Color.FromArgb(78, 82, 88);

            // Основание
            AddBlock(
                x + 8, y + 0.8, z + 6,
                x + 120, y + 2.2, z + 28,
                null,
                bodyDark,
                FaceLayer.Furniture
            );

            // Основной закрытый корпус
            AddBlock(
                x, y + 2.2, z + 4,
                x + 128, y + 30.0, z + 28,
                null,
                body,
                FaceLayer.Furniture
            );

            // Верхний блок
            AddBlock(
                x + 10, y + 30.0, z + 7,
                x + 118, y + 39.0, z + 25,
                null,
                bodyDark,
                FaceLayer.Furniture
            );

            // Верхняя крышка
            AddBlock(
                x + 22, y + 39.0, z + 10,
                x + 106, y + 42.0, z + 23,
                null,
                bodyLight,
                FaceLayer.Furniture
            );

            // Передняя широкая полоса — теперь тёмно-серая
            AddQuad(
                x + 10, y + 10.0, z + 28.25,
                x + 118, y + 10.0, z + 28.25,
                x + 118, y + 24.5, z + 28.25,
                x + 10, y + 24.5, z + 28.25,
                null,
                frontStripe,
                FaceLayer.SmallDetail
            );

            // Светлые вставки
            AddBlock(
                x + 12, y + 11.0, z + 28.1,
                x + 18, y + 20.0, z + 29.2,
                null,
                bodyLight,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 110, y + 11.0, z + 28.1,
                x + 116, y + 20.0, z + 29.2,
                null,
                bodyLight,
                FaceLayer.SmallDetail
            );

            // Чёрные носики
            AddBlock(
                x + 42, y + 10.5, z + 28.2,
                x + 50, y + 15.5, z + 31.2,
                null,
                blackDetail,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 78, y + 10.5, z + 28.2,
                x + 86, y + 15.5, z + 31.2,
                null,
                blackDetail,
                FaceLayer.SmallDetail
            );

            // Поддон
            AddBlock(
                x + 26, y + 2.3, z + 30.0,
                x + 102, y + 3.7, z + 34.5,
                null,
                tray,
                FaceLayer.SmallDetail
            );
        }

        private void AddCashRegister(double x, double y, double z)
        {
            Color body = Color.FromArgb(58, 60, 68);
            Color bodyLight = Color.FromArgb(188, 192, 198);
            Color screen = Color.FromArgb(10, 20, 26);
            Color keyboard = Color.FromArgb(214, 217, 222);
            Color keys = Color.FromArgb(130, 136, 142);
            Color printerBody = Color.FromArgb(206, 210, 216);
            Color paper = Color.FromArgb(238, 238, 236);

            // Основной корпус кассы
            AddBlock(
                x, y, z,
                x + 52, y + 14, z + 34,
                null,
                body,
                FaceLayer.Furniture
            );

            // Клавиатурный блок
            AddBlock(
                x + 6, y + 14, z + 6,
                x + 30, y + 18, z + 28,
                null,
                keyboard,
                FaceLayer.Furniture
            );

            // Ряды клавиш
            AddBlock(
                x + 8, y + 18, z + 9,
                x + 28, y + 19.0, z + 12,
                null,
                keys,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 8, y + 18, z + 16,
                x + 28, y + 19.0, z + 19,
                null,
                keys,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 8, y + 18, z + 23,
                x + 28, y + 19.0, z + 26,
                null,
                keys,
                FaceLayer.SmallDetail
            );

            // Стойка монитора
            AddBlock(
                x + 34, y + 14, z + 14,
                x + 40, y + 25, z + 20,
                null,
                body,
                FaceLayer.Furniture
            );

            // Экранный блок
            AddBlock(
                x + 28, y + 25, z + 10,
                x + 50, y + 40, z + 24,
                null,
                bodyLight,
                FaceLayer.Furniture
            );

            // Экран
            AddQuad(
                x + 31, y + 28, z + 24.2,
                x + 47, y + 28, z + 24.2,
                x + 47, y + 37, z + 24.2,
                x + 31, y + 37, z + 24.2,
                null,
                screen,
                FaceLayer.SmallDetail
            );

            // Чековый принтер
            AddBlock(
                x + 58, y, z + 8,
                x + 80, y + 11, z + 28,
                null,
                printerBody,
                FaceLayer.Furniture
            );

            // Верхняя прорезь
            AddBlock(
                x + 62, y + 10.2, z + 15,
                x + 76, y + 11.0, z + 21,
                null,
                keys,
                FaceLayer.SmallDetail
            );

            // Чек — уже, как узкая бумажная лента
            AddBlock(
    x + 68.2, y + 11.0, z + 12.5,
    x + 70.0, y + 16.0, z + 25.5,
    null,
    paper,
    FaceLayer.SmallDetail
);
        }

        private void AddBuiltInSink(double x, double y, double z)
        {
            Color steel = Color.FromArgb(206, 209, 214);
            Color steelDark = Color.FromArgb(158, 163, 168);
            Color basinDark = Color.FromArgb(86, 92, 98);
            Color faucet = Color.FromArgb(224, 224, 228);

            // Верхняя металлическая площадка
            AddBlock(
                x, y + 0.4, z,
                x + 34, y + 2.0, z + 42,
                null,
                steel,
                FaceLayer.Furniture
            );

            // Более крупные бортики
            AddBlock(
                x + 2, y + 2.0, z + 2,
                x + 32, y + 4.8, z + 7,
                null,
                steelDark,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 2, y + 2.0, z + 35,
                x + 32, y + 4.8, z + 40,
                null,
                steelDark,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 2, y + 2.0, z + 7,
                x + 7, y + 4.8, z + 35,
                null,
                steelDark,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 27, y + 2.0, z + 7,
                x + 32, y + 4.8, z + 35,
                null,
                steelDark,
                FaceLayer.SmallDetail
            );

            // Внутренняя часть чаши
            AddQuad(
                x + 7, y + 2.1, z + 7,
                x + 27, y + 2.1, z + 7,
                x + 27, y + 2.1, z + 35,
                x + 7, y + 2.1, z + 35,
                null,
                basinDark,
                FaceLayer.SmallDetail
            );

            // Кран
            AddBlock(
                x + 14, y + 2.0, z + 6,
                x + 17, y + 18.0, z + 9,
                null,
                faucet,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 10, y + 16.0, z + 6,
                x + 21, y + 18.0, z + 21,
                null,
                faucet,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 18, y + 12.0, z + 18,
                x + 21, y + 16.0, z + 21,
                null,
                faucet,
                FaceLayer.SmallDetail
            );
        }

        private void AddOpenWallCabinet(double xBack, double yBottom, double zMin, double xFront, double yTop, double zMax)
        {
            Color wood = Color.FromArgb(95, 60, 35);

            double backThickness = 3.0;
            double sideThickness = 4.0;
            double shelfThickness = 4.0;

            AddBlock(
                xBack, yBottom, zMin,
                xBack + backThickness, yTop, zMax,
                null, wood, FaceLayer.Furniture
            );

            AddBlock(
                xBack + backThickness, yBottom, zMin,
                xFront, yTop, zMin + sideThickness,
                null, wood, FaceLayer.Furniture
            );

            AddBlock(
                xBack + backThickness, yBottom, zMax - sideThickness,
                xFront, yTop, zMax,
                null, wood, FaceLayer.Furniture
            );

            AddBlock(
                xBack + backThickness, yTop - shelfThickness, zMin + sideThickness,
                xFront, yTop, zMax - sideThickness,
                null, wood, FaceLayer.Furniture
            );

            AddBlock(
                xBack + backThickness, yBottom, zMin + sideThickness,
                xFront, yBottom + shelfThickness, zMax - sideThickness,
                null, wood, FaceLayer.Furniture
            );

            double midY = (yBottom + yTop) / 2.0 - shelfThickness / 2.0;

            AddBlock(
                xBack + backThickness, midY, zMin + sideThickness,
                xFront, midY + shelfThickness, zMax - sideThickness,
                null, wood, FaceLayer.Furniture
            );
        }

        private void AddCabinetSyrups(double xBack, double xFront, double yBottom, double yTop, double zMin, double zMax)
        {
            double shelfThickness = 4.0;

            double bottomShelfTop = yBottom + shelfThickness;
            double midY = (yBottom + yTop) / 2.0 - shelfThickness / 2.0;
            double middleShelfTop = midY + shelfThickness;

            double[] zCenters =
            {
        zMin + 14,
        zMin + 34,
        zMin + 54,
        zMin + 74
    };

            // Нижняя полка
            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, bottomShelfTop + 0.4, zCenters[0],
                Color.FromArgb(112, 76, 36),
                Color.FromArgb(245, 238, 198),
                Color.FromArgb(210, 188, 84),
                SyrupIconType.Vanilla,
                "VAN"
            );

            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, bottomShelfTop + 0.4, zCenters[1],
                Color.FromArgb(118, 72, 34),
                Color.FromArgb(236, 184, 102),
                Color.FromArgb(200, 126, 54),
                SyrupIconType.Caramel,
                "CAR"
            );

            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, bottomShelfTop + 0.4, zCenters[2],
                Color.FromArgb(96, 66, 40),
                Color.FromArgb(232, 214, 178),
                Color.FromArgb(154, 112, 72),
                SyrupIconType.Hazelnut,
                "HAZ"
            );

            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, bottomShelfTop + 0.4, zCenters[3],
                Color.FromArgb(90, 58, 36),
                Color.FromArgb(220, 168, 160),
                Color.FromArgb(132, 92, 74),
                SyrupIconType.Chocolate,
                "CHO"
            );

            // Средняя полка
            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, middleShelfTop + 0.4, zCenters[0],
                Color.FromArgb(108, 72, 44),
                Color.FromArgb(226, 142, 176),
                Color.FromArgb(186, 88, 128),
                SyrupIconType.Raspberry,
                "RAS"
            );

            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, middleShelfTop + 0.4, zCenters[1],
                Color.FromArgb(108, 72, 44),
                Color.FromArgb(214, 240, 214),
                Color.FromArgb(118, 176, 118),
                SyrupIconType.Mint,
                "MNT"
            );

            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, middleShelfTop + 0.4, zCenters[2],
                Color.FromArgb(110, 78, 48),
                Color.FromArgb(245, 241, 232),
                Color.FromArgb(214, 206, 180),
                SyrupIconType.Coconut,
                "COC"
            );

            AddSyrupBottle(
                xFront - 18.0, xFront - 8.2, middleShelfTop + 0.4, zCenters[3],
                Color.FromArgb(112, 80, 50),
                Color.FromArgb(226, 210, 182),
                Color.FromArgb(174, 144, 98),
                SyrupIconType.IrishCream,
                "IRC"
            );
        }

        private void AddSyrupBottle(
    double x1,
    double x2,
    double baseY,
    double zCenter,
    Color bottleColor,
    Color labelColor,
    Color capColor,
    SyrupIconType iconType,
    string code)
        {
            double z1 = zCenter - 5.0;
            double z2 = zCenter + 5.0;

            // Корпус бутылки
            AddBlock(
                x1, baseY, z1,
                x2, baseY + 18.0, z2,
                null,
                bottleColor,
                FaceLayer.Furniture
            );

            // Верх бутылки / горлышко
            AddBlock(
                x1 + 1.8, baseY + 18.0, zCenter - 3.2,
                x2 - 1.8, baseY + 25.0, zCenter + 3.2,
                null,
                bottleColor,
                FaceLayer.Furniture
            );

            // Крышка
            AddBlock(
                x1 + 1.0, baseY + 25.0, zCenter - 4.0,
                x2 - 1.0, baseY + 27.2, zCenter + 4.0,
                null,
                capColor,
                FaceLayer.SmallDetail
            );

            // Этикетка — чуть больше
            AddQuad(
                x2 + 0.08, baseY + 4.2, zCenter - 4.4,
                x2 + 0.08, baseY + 15.0, zCenter - 4.4,
                x2 + 0.08, baseY + 15.0, zCenter + 4.4,
                x2 + 0.08, baseY + 4.2, zCenter + 4.4,
                null,
                labelColor,
                FaceLayer.SmallDetail
            );

            // Верхняя полоска
            AddQuad(
                x2 + 0.12, baseY + 15.2, zCenter - 3.6,
                x2 + 0.12, baseY + 16.6, zCenter - 3.6,
                x2 + 0.12, baseY + 16.6, zCenter + 3.6,
                x2 + 0.12, baseY + 15.2, zCenter + 3.6,
                null,
                capColor,
                FaceLayer.SmallDetail
            );

            // Значок вкуса — чуть выше и крупнее
            AddSyrupLabelIcon(
                x2 + 0.18,
                baseY + 10.6,
                zCenter,
                iconType
            );

            // Код внизу
            AddMiniLabelCode(
                x2 + 0.20,
                baseY + 6.1,
                zCenter,
                code,
                Color.FromArgb(92, 78, 58)
            );
        }

        private void AddLabelRect(double x, double y1, double y2, double z1, double z2, Color color)
        {
            AddQuad(
                x, y1, z1,
                x, y2, z1,
                x, y2, z2,
                x, y1, z2,
                null,
                color,
                FaceLayer.SmallDetail
            );
        }

        private void AddLabelDiamond(double x, double centerY, double centerZ, double halfY, double halfZ, Color color)
        {
            AddQuad(
                x, centerY - halfY, centerZ,
                x, centerY, centerZ + halfZ,
                x, centerY + halfY, centerZ,
                x, centerY, centerZ - halfZ,
                null,
                color,
                FaceLayer.SmallDetail
            );
        }

        private void AddSyrupLabelIcon(double x, double centerY, double centerZ, SyrupIconType iconType)
        {
            Color white = Color.FromArgb(245, 242, 234);
            Color cream = Color.FromArgb(234, 224, 192);
            Color beige = Color.FromArgb(214, 190, 150);
            Color amber = Color.FromArgb(196, 126, 50);
            Color brown = Color.FromArgb(104, 66, 40);
            Color darkBrown = Color.FromArgb(82, 49, 28);
            Color green = Color.FromArgb(96, 150, 94);
            Color lightGreen = Color.FromArgb(162, 196, 146);
            Color pink = Color.FromArgb(214, 120, 142);
            Color darkPink = Color.FromArgb(176, 84, 112);
            Color nut = Color.FromArgb(146, 106, 64);
            Color nutLight = Color.FromArgb(182, 138, 92);

            switch (iconType)
            {
                case SyrupIconType.Vanilla:
                    // Крупная палочка
                    AddLabelRect(
                        x,
                        centerY - 2.0,
                        centerY + 1.3,
                        centerZ - 0.35,
                        centerZ + 0.35,
                        beige
                    );

                    // Крупный светлый цветок
                    AddLabelDiamond(x, centerY + 1.7, centerZ, 1.0, 0.8, white);
                    AddLabelDiamond(x, centerY + 1.7, centerZ, 0.65, 1.25, white);
                    AddLabelRect(
                        x,
                        centerY + 1.35,
                        centerY + 2.0,
                        centerZ - 0.25,
                        centerZ + 0.25,
                        cream
                    );
                    break;

                case SyrupIconType.Caramel:
                    // Большая янтарная капля
                    AddLabelDiamond(x, centerY + 0.6, centerZ, 2.0, 1.2, amber);
                    AddLabelRect(
                        x,
                        centerY + 1.7,
                        centerY + 2.3,
                        centerZ - 0.30,
                        centerZ + 0.30,
                        amber
                    );
                    break;

                case SyrupIconType.Chocolate:
                    // Оставляем удачный вариант
                    AddLabelRect(
                        x,
                        centerY - 1.7,
                        centerY + 1.7,
                        centerZ - 1.7,
                        centerZ + 1.7,
                        brown
                    );

                    AddLabelRect(
                        x,
                        centerY - 1.7,
                        centerY + 1.7,
                        centerZ - 0.16,
                        centerZ + 0.16,
                        darkBrown
                    );

                    AddLabelRect(
                        x,
                        centerY - 0.16,
                        centerY + 0.16,
                        centerZ - 1.7,
                        centerZ + 1.7,
                        darkBrown
                    );
                    break;

                case SyrupIconType.Mint:
                    // Крупный лист
                    AddLabelDiamond(x, centerY, centerZ, 2.0, 1.25, green);

                    // Центральная прожилка
                    AddLabelRect(
                        x,
                        centerY - 1.4,
                        centerY + 1.4,
                        centerZ - 0.14,
                        centerZ + 0.14,
                        lightGreen
                    );
                    break;

                case SyrupIconType.Raspberry:
                    // Крупная ягода: 4 большие "дольки"
                    AddLabelDiamond(x, centerY - 0.5, centerZ - 0.85, 0.95, 0.65, pink);
                    AddLabelDiamond(x, centerY - 0.5, centerZ + 0.85, 0.95, 0.65, pink);
                    AddLabelDiamond(x, centerY + 0.7, centerZ - 0.55, 0.95, 0.65, pink);
                    AddLabelDiamond(x, centerY + 0.7, centerZ + 0.55, 0.95, 0.65, pink);

                    // Нижняя часть ягоды
                    AddLabelDiamond(x, centerY - 1.45, centerZ, 0.85, 0.7, darkPink);

                    // Листик сверху
                    AddLabelRect(
                        x,
                        centerY + 1.7,
                        centerY + 2.1,
                        centerZ - 0.7,
                        centerZ + 0.7,
                        green
                    );
                    break;

                case SyrupIconType.Coconut:
                    // Крупный белый круг/кокос
                    AddLabelDiamond(x, centerY, centerZ, 1.9, 1.25, white);
                    AddLabelRect(
                        x,
                        centerY - 1.1,
                        centerY + 1.1,
                        centerZ - 1.55,
                        centerZ + 1.55,
                        white
                    );
                    AddLabelRect(
                        x,
                        centerY - 1.7,
                        centerY + 1.7,
                        centerZ - 0.28,
                        centerZ + 0.28,
                        cream
                    );
                    break;

                case SyrupIconType.IrishCream:
                    // Крупный бежевый знак
                    AddLabelRect(
                        x,
                        centerY - 1.9,
                        centerY + 1.9,
                        centerZ - 0.26,
                        centerZ + 0.26,
                        beige
                    );
                    AddLabelRect(
                        x,
                        centerY - 0.26,
                        centerY + 0.26,
                        centerZ - 1.9,
                        centerZ + 1.9,
                        beige
                    );

                    // Светлая сердцевина
                    AddLabelDiamond(x, centerY, centerZ, 0.9, 0.9, cream);
                    break;

                case SyrupIconType.Hazelnut:
                    // Крупный орех
                    AddLabelDiamond(x, centerY - 0.1, centerZ, 1.9, 1.25, nut);

                    // Светлая верхушка
                    AddLabelRect(
                        x,
                        centerY + 1.1,
                        centerY + 1.7,
                        centerZ - 0.8,
                        centerZ + 0.8,
                        nutLight
                    );
                    break;
            }
        }

        private void AddMiniLabelCode(double x, double centerY, double centerZ, string code, Color color)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            code = code.ToUpperInvariant();

            double letterWidth = 1.1;
            double letterHeight = 1.5;
            double spacing = 0.55;

            double totalWidth = code.Length * letterWidth + (code.Length - 1) * spacing;
            double startZ = centerZ - totalWidth / 2.0 + letterWidth / 2.0;

            for (int i = 0; i < code.Length; i++)
            {
                double z = startZ + i * (letterWidth + spacing);
                AddMiniGlyph(x, centerY, z, code[i], color);
            }
        }

        private void AddMiniGlyph(double x, double centerY, double centerZ, char c, Color color)
        {
            switch (c)
            {
                case 'A': AddGlyphA(x, centerY, centerZ, color); break;
                case 'C': AddGlyphC(x, centerY, centerZ, color); break;
                case 'H': AddGlyphH(x, centerY, centerZ, color); break;
                case 'I': AddGlyphI(x, centerY, centerZ, color); break;
                case 'M': AddGlyphM(x, centerY, centerZ, color); break;
                case 'N': AddGlyphN(x, centerY, centerZ, color); break;
                case 'O': AddGlyphO(x, centerY, centerZ, color); break;
                case 'R': AddGlyphR(x, centerY, centerZ, color); break;
                case 'S': AddGlyphS(x, centerY, centerZ, color); break;
                case 'T': AddGlyphT(x, centerY, centerZ, color); break;
                case 'V': AddGlyphV(x, centerY, centerZ, color); break;
                case 'Z': AddGlyphZ(x, centerY, centerZ, color); break;
            }
        }

        private void AddMiniStroke(double x, double y1, double y2, double z1, double z2, Color color)
        {
            AddQuad(
                x, y1, z1,
                x, y2, z1,
                x, y2, z2,
                x, y1, z2,
                null,
                color,
                FaceLayer.SmallDetail
            );
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
            AddMiniStroke(x, y - 0.2, y + 0.2, z - 0.20, z + 0.20, color);
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
            AddMiniStroke(x, y + 0.9, y + 1.2, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y - 0.9, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 0.15, y + 0.15, z - 0.15, z + 0.15, color);
        }
        private void AddFlatLabelRect(double x, double y1, double y2, double z1, double z2, Color color)
        {
            AddQuad(
                x, y1, z1,
                x, y2, z1,
                x, y2, z2,
                x, y1, z2,
                null,
                color,
                FaceLayer.SmallDetail
            );
        }

        private void AddFlatLabelDiag(double x, double y1, double z1, double y2, double z2, double thickness, Color color)
        {
            AddQuad(
                x, y1, z1,
                x, y2, z2,
                x, y2, z2 + thickness,
                x, y1, z1 + thickness,
                null,
                color,
                FaceLayer.SmallDetail
            );
        }

        private void AddSyrupFlavorCode(double x, double yBottom, double yTop, double zCenter, char code)
        {
            Color ink = Color.FromArgb(72, 62, 52);

            double left = zCenter - 2.2;
            double right = zCenter + 2.2;
            double middleZ = (left + right) / 2.0;

            double bottom = yBottom;
            double top = yTop;
            double middleY = (bottom + top) / 2.0;

            double t = 0.45;

            switch (char.ToUpper(code))
            {
                case 'V':
                    AddFlatLabelDiag(x, top, left, bottom, middleZ - 0.25, t, ink);
                    AddFlatLabelDiag(x, bottom, middleZ - 0.25, top, right - t, t, ink);
                    break;

                case 'K':
                    AddFlatLabelRect(x, bottom, top, left, left + t, ink);
                    AddFlatLabelDiag(x, middleY, left + t, top, right - t, t, ink);
                    AddFlatLabelDiag(x, middleY, left + t, bottom, right - t, t, ink);
                    break;

                case 'H':
                    AddFlatLabelRect(x, bottom, top, left, left + t, ink);
                    AddFlatLabelRect(x, bottom, top, right - t, right, ink);
                    AddFlatLabelRect(x, middleY - 0.25, middleY + 0.25, left, right, ink);
                    break;

                case 'C':
                    AddFlatLabelRect(x, top - 0.45, top, left, right - 0.25, ink);
                    AddFlatLabelRect(x, bottom, bottom + 0.45, left, right - 0.25, ink);
                    AddFlatLabelRect(x, bottom, top, left, left + t, ink);
                    break;

                case 'R':
                    AddFlatLabelRect(x, bottom, top, left, left + t, ink);
                    AddFlatLabelRect(x, top - 0.45, top, left, right - t, ink);
                    AddFlatLabelRect(x, middleY - 0.25, middleY + 0.25, left, right - t, ink);
                    AddFlatLabelRect(x, middleY, top, right - t, right, ink);
                    AddFlatLabelDiag(x, middleY, left + t, bottom, right - t, t, ink);
                    break;

                case 'M':
                    AddFlatLabelRect(x, bottom, top, left, left + t, ink);
                    AddFlatLabelRect(x, bottom, top, right - t, right, ink);
                    AddFlatLabelDiag(x, top, left + t, middleY, middleZ - 0.2, t, ink);
                    AddFlatLabelDiag(x, middleY, middleZ - 0.2, top, right - 2 * t, t, ink);
                    break;

                case 'O':
                    AddFlatLabelRect(x, top - 0.45, top, left, right, ink);
                    AddFlatLabelRect(x, bottom, bottom + 0.45, left, right, ink);
                    AddFlatLabelRect(x, bottom, top, left, left + t, ink);
                    AddFlatLabelRect(x, bottom, top, right - t, right, ink);
                    break;

                case 'I':
                    AddFlatLabelRect(x, top - 0.45, top, left, right, ink);
                    AddFlatLabelRect(x, bottom, bottom + 0.45, left, right, ink);
                    AddFlatLabelRect(x, bottom, top, middleZ - t / 2.0, middleZ + t / 2.0, ink);
                    break;
            }
        }

        // =========================================================
        // ПОЛКА С ЧАЙНЫМИ ПАРАМИ
        // =========================================================

        private void AddTeaShelf()
        {
            Color wood = Color.FromArgb(82, 49, 28);

            AddBlock(
                -7, -85, 592.5,
                7, 45, 598,
                "wood_dark",
                wood,
                FaceLayer.Furniture
            );

            AddTeaShelfBoard(-57);
            AddTeaShelfBoard(-22);
            AddTeaShelfBoard(13);

            double teaZ = 596.1;

            // Нижняя полка — обычные чашки
            AddTeaRow(
                -54.4,
                teaZ,
                Color.FromArgb(232, 231, 226),
                Color.FromArgb(220, 214, 201),
                Color.FromArgb(228, 226, 220),
                Color.FromArgb(206, 202, 192)
            );

            // Центральная полка — большие чашки под латте / капучино
            AddLatteCupRow(
                -19.4,
                teaZ
            );

            // Верхняя полка — бумажные кофейные стаканы
            AddPaperCupRow(
                15.6,
                teaZ
            );
        }

        private void AddTeaShelfBoard(double y)
        {
            Color wood = Color.FromArgb(82, 49, 28);
            Color woodLight = Color.FromArgb(96, 58, 32);

            // Полка стала примерно в 1.5 раза уже:
            // было -95..95, стало -65..65.
            double shelfX1 = -65;
            double shelfX2 = 65;

            // Основная полка
            AddBlock(
                shelfX1, y, 586.2,
                shelfX2, y + 2.8, 598.0,
                "wood_dark",
                wood,
                FaceLayer.Furniture
            );

            // Толстый передний бортик-маска.
            // Оставляем его, потому что он помогает чашкам не выглядеть проваленными.
            AddBlock(
                shelfX1, y - 6.0, 585.0,
                shelfX2, y + 0.45, 588.3,
                null,
                wood,
                FaceLayer.SmallDetail
            );

            // Верхняя тонкая кромка
            AddBlock(
                shelfX1, y + 2.1, 585.5,
                shelfX2, y + 3.0, 587.2,
                null,
                woodLight,
                FaceLayer.SmallDetail
            );
        }

        private void AddTeaRow(double y, double z, params Color[] colors)
        {
            // Без центральной пары
            double[] xs = { -52, -26, 26, 52 };

            for (int i = 0; i < xs.Length && i < colors.Length; i++)
            {
                AddTeaPair(xs[i], y, z, colors[i]);
            }
        }

        private void AddTeaPair(double x, double y, double z, Color cupColor)
        {
            // Небольшой подъём над полкой, чтобы блюдце и чашка не выглядели
            // проваленными внутрь доски.
            AddSaucer(x, y + 0.6, z, Color.FromArgb(221, 219, 211));
            AddCup(x, y + 1.15, z, cupColor);
        }

        private void AddLatteCupRow(double y, double z)
        {
            double[] xs = { -52, -26, 26, 52 };

            Color[] cupColors =
            {
        Color.FromArgb(226, 222, 214),
        Color.FromArgb(214, 206, 194),
        Color.FromArgb(233, 230, 224),
        Color.FromArgb(205, 198, 186)
    };

            for (int i = 0; i < xs.Length; i++)
            {
                AddLatteCupSet(xs[i], y, z, cupColors[i]);
            }
        }

        private void AddLatteCupSet(double x, double y, double z, Color cupColor)
        {
            AddLargeSaucer(x, y + 0.6, z, Color.FromArgb(223, 221, 214));
            AddLatteCup(x, y + 1.2, z, cupColor);
        }

        private void AddLargeSaucer(double x, double y, double z, Color color)
        {
            AddBlock(
                x - 7.0, y, z - 4.0,
                x + 7.0, y + 0.95, z + 4.0,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 4.8, y + 0.95, z - 2.8,
                x + 4.8, y + 1.3, z + 2.8,
                null,
                Color.FromArgb(238, 236, 230),
                FaceLayer.Furniture
            );
        }

        private void AddLatteCup(double x, double y, double z, Color color)
        {
            Color rimColor = Color.FromArgb(
                Math.Min(color.R + 10, 255),
                Math.Min(color.G + 10, 255),
                Math.Min(color.B + 10, 255)
            );

            Color handleColor = Color.FromArgb(
                Math.Max(color.R - 8, 0),
                Math.Max(color.G - 8, 0),
                Math.Max(color.B - 8, 0)
            );

            AddBlock(
                x - 4.2, y, z - 3.2,
                x + 4.2, y + 2.4, z + 3.2,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 3.8, y + 2.4, z - 3.0,
                x + 3.8, y + 5.8, z + 3.0,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 2.8, y + 5.8, z - 2.2,
                x + 2.8, y + 6.6, z + 2.2,
                null,
                rimColor,
                FaceLayer.Furniture
            );

            // Ручка
            AddBlock(
                x + 4.0, y + 2.0, z - 0.9,
                x + 5.3, y + 5.0, z + 0.9,
                null,
                handleColor,
                FaceLayer.Furniture
            );
        }

        private void AddPaperCupRow(double y, double z)
        {
            double[] xs = { -52, -26, 26, 52 };

            Color[] bodyColors =
            {
        Color.FromArgb(229, 224, 214),
        Color.FromArgb(214, 203, 188),
        Color.FromArgb(236, 231, 224),
        Color.FromArgb(220, 210, 196)
    };

            for (int i = 0; i < xs.Length; i++)
            {
                AddPaperCup(xs[i], y + 0.8, z, bodyColors[i]);
            }
        }

        private void AddPaperCup(double x, double y, double z, Color bodyColor)
        {
            Color lidColor = Color.FromArgb(232, 232, 228);
            Color sleeveColor = Color.FromArgb(120, 86, 54);

            AddBlock(
                x - 3.2, y, z - 2.2,
                x + 3.2, y + 3.0, z + 2.2,
                null,
                bodyColor,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 2.8, y + 3.0, z - 1.9,
                x + 2.8, y + 5.7, z + 1.9,
                null,
                bodyColor,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 2.9, y + 1.8, z - 2.0,
                x + 2.9, y + 3.3, z + 2.0,
                null,
                sleeveColor,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x - 3.5, y + 5.7, z - 2.5,
                x + 3.5, y + 6.5, z + 2.5,
                null,
                lidColor,
                FaceLayer.Furniture
            );
        }

        private void AddMugRow(double y, double z)
        {
            double[] xs = { -52, -26, 26, 52 };

            Color[] mugColors =
            {
        Color.FromArgb(214, 205, 190),
        Color.FromArgb(196, 207, 214),
        Color.FromArgb(228, 221, 210),
        Color.FromArgb(204, 196, 182)
    };

            for (int i = 0; i < xs.Length; i++)
            {
                AddMug(xs[i], y + 0.7, z, mugColors[i]);
            }
        }

        private void AddMug(double x, double y, double z, Color color)
        {
            Color rimColor = Color.FromArgb(
                Math.Min(color.R + 10, 255),
                Math.Min(color.G + 10, 255),
                Math.Min(color.B + 10, 255)
            );

            Color handleColor = Color.FromArgb(
                Math.Max(color.R - 8, 0),
                Math.Max(color.G - 8, 0),
                Math.Max(color.B - 8, 0)
            );

            // Основание кружки
            AddBlock(
                x - 3.8, y, z - 2.8,
                x + 3.8, y + 3.2, z + 2.8,
                null,
                color,
                FaceLayer.Furniture
            );

            // Верхняя часть
            AddBlock(
                x - 3.4, y + 3.2, z - 2.5,
                x + 3.4, y + 4.1, z + 2.5,
                null,
                rimColor,
                FaceLayer.Furniture
            );

            // Ручка
            AddBlock(
                x + 3.4, y + 1.2, z - 0.8,
                x + 4.7, y + 3.4, z + 0.8,
                null,
                handleColor,
                FaceLayer.Furniture
            );
        }

        private void AddSaucer(double x, double y, double z, Color color)
        {
            AddBlock(
                x - 6.0, y, z - 3.5,
                x + 6.0, y + 0.85, z + 3.5,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 4.2, y + 0.85, z - 2.4,
                x + 4.2, y + 1.2, z + 2.4,
                null,
                Color.FromArgb(237, 235, 228),
                FaceLayer.Furniture
            );
        }

        private void AddCup(double x, double y, double z, Color color)
        {
            AddBlock(
                x - 3.4, y, z - 2.6,
                x + 3.4, y + 1.8, z + 2.6,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 3.1, y + 1.8, z - 2.4,
                x + 3.1, y + 4.2, z + 2.4,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 2.3, y + 4.2, z - 1.8,
                x + 2.3, y + 4.9, z + 1.8,
                null,
                Color.FromArgb(
                    Math.Min(color.R + 10, 255),
                    Math.Min(color.G + 10, 255),
                    Math.Min(color.B + 10, 255)
                ),
                FaceLayer.Furniture
            );

            // Ручка
            AddBlock(
                x + 3.2, y + 1.4, z - 0.7,
                x + 4.2, y + 3.6, z + 0.7,
                null,
                Color.FromArgb(
                    Math.Max(color.R - 6, 0),
                    Math.Max(color.G - 6, 0),
                    Math.Max(color.B - 6, 0)
                ),
                FaceLayer.Furniture
            );
        }

        // =========================================================
        // ОКНО / ДВЕРЬ
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

            AddQuad(
                x, wallBottom, wallNearZ,
                x, windowBottom, wallNearZ,
                x, windowBottom, wallFarZ,
                x, wallBottom, wallFarZ,
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            AddQuad(
                x, windowTop, wallNearZ,
                x, wallTop, wallNearZ,
                x, wallTop, wallFarZ,
                x, windowTop, wallFarZ,
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            AddQuad(
                x, windowBottom, wallNearZ,
                x, windowTop, wallNearZ,
                x, windowTop, windowNearZ,
                x, windowBottom, windowNearZ,
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            AddQuad(
                x, windowBottom, windowFarZ,
                x, windowTop, windowFarZ,
                x, windowTop, wallFarZ,
                x, windowBottom, wallFarZ,
                "wall",
                Color.Empty,
                FaceLayer.Wall
            );

            AddQuad(
    x + 1.2, windowBottom + 1.0, windowNearZ + 1.0,
    x + 1.2, windowTop - 1.0, windowNearZ + 1.0,
    x + 1.2, windowTop - 1.0, windowFarZ - 1.0,
    x + 1.2, windowBottom + 1.0, windowFarZ - 1.0,
    null,
    Color.FromArgb(94, 126, 148),
    FaceLayer.Wall
);

            AddWindowFrame(x, windowBottom, windowTop, windowNearZ, windowFarZ);
        }

        private void AddWindowFrame(double wallX, double bottom, double top, double z1, double z2)
        {
            Color frameColor = Color.FromArgb(88, 56, 32);

            // Наружная рама — как раньше, вокруг проёма
            double outerX1 = wallX + 0.8;
            double outerX2 = wallX + 6.0;
            double t = 5.0;

            AddBlock(outerX1, bottom - t, z1 - t, outerX2, bottom, z2 + t, null, frameColor, FaceLayer.Wall);
            AddBlock(outerX1, top, z1 - t, outerX2, top + t, z2 + t, null, frameColor, FaceLayer.Wall);
            AddBlock(outerX1, bottom, z1 - t, outerX2, top, z1, null, frameColor, FaceLayer.Wall);
            AddBlock(outerX1, bottom, z2, outerX2, top, z2 + t, null, frameColor, FaceLayer.Wall);

            // Внутренняя рамка почти по размеру окна:
            // отступ от проёма по 1 единице с каждой стороны
            double innerX1 = wallX + 1.0;
            double innerX2 = wallX + 5.2;

            double innerBottom = bottom + 1.0;
            double innerTop = top - 1.0;
            double innerZ1 = z1 + 1.0;
            double innerZ2 = z2 - 1.0;

            double sashT = 2.4;

            // Внутренний контур — без большого "чёрного фона" между рамой и стеклом
            AddBlock(innerX1, innerBottom, innerZ1, innerX2, innerBottom + sashT, innerZ2, null, frameColor, FaceLayer.Wall);
            AddBlock(innerX1, innerTop - sashT, innerZ1, innerX2, innerTop, innerZ2, null, frameColor, FaceLayer.Wall);
            AddBlock(innerX1, innerBottom + sashT, innerZ1, innerX2, innerTop - sashT, innerZ1 + sashT, null, frameColor, FaceLayer.Wall);
            AddBlock(innerX1, innerBottom + sashT, innerZ2 - sashT, innerX2, innerTop - sashT, innerZ2, null, frameColor, FaceLayer.Wall);

            // Вертикальная перекладина
            double midZ = (innerZ1 + innerZ2) / 2.0;
            AddBlock(
                innerX1,
                innerBottom + sashT,
                midZ - 1.2,
                innerX2,
                innerTop - sashT,
                midZ + 1.2,
                null,
                frameColor,
                FaceLayer.Wall
            );

            // Горизонтальная перекладина
            double midY = (innerBottom + innerTop) / 2.0;
            AddBlock(
                innerX1,
                midY - 1.2,
                innerZ1 + sashT,
                innerX2,
                midY + 1.2,
                innerZ2 - sashT,
                null,
                frameColor,
                FaceLayer.Wall
            );
        }

        private void AddDoorWithGlass()
        {
            Color frameColor = Color.FromArgb(76, 46, 24);
            Color doorColor = Color.FromArgb(96, 56, 30);
            Color innerPanel = Color.FromArgb(112, 67, 36);

            double left = -188;
            double right = -124;
            double bottom = -100;
            double top = 72;

            // Внешняя рамка
            AddBlock(left - 4, bottom, 596.7, left, top + 4, 600, null, frameColor, FaceLayer.Wall);
            AddBlock(right, bottom, 596.7, right + 4, top + 4, 600, null, frameColor, FaceLayer.Wall);
            AddBlock(left - 4, top, 596.7, right + 4, top + 4, 600, null, frameColor, FaceLayer.Wall);

            // Полотно двери
            AddQuad(
                left, bottom, 599.0,
                left, top, 599.0,
                right, top, 599.0,
                right, bottom, 599.0,
                null,
                doorColor,
                FaceLayer.Wall
            );

            // Нижняя вставка
            AddQuad(
                left + 8, -78, 598.8,
                left + 8, -20, 598.8,
                right - 8, -20, 598.8,
                right - 8, -78, 598.8,
                null,
                innerPanel,
                FaceLayer.Wall
            );

            // Стекло
            AddQuad(
                left + 10, 0, 598.85,
                left + 10, 60, 598.85,
                right - 10, 60, 598.85,
                right - 10, 0, 598.85,
                null,
                Color.FromArgb(110, 145, 168),
                FaceLayer.Wall
            );

            // Рамка стекла
            AddBlock(left + 8, -2, 598.4, right - 8, 2, 599.3, null, frameColor, FaceLayer.Wall);
            AddBlock(left + 8, 58, 598.4, right - 8, 62, 599.3, null, frameColor, FaceLayer.Wall);
            AddBlock(left + 8, 0, 598.4, left + 12, 60, 599.3, null, frameColor, FaceLayer.Wall);
            AddBlock(right - 12, 0, 598.4, right - 8, 60, 599.3, null, frameColor, FaceLayer.Wall);

            // Вертикальная перегородка
            AddBlock(left + 30, 0, 598.5, left + 34, 60, 599.25, null, frameColor, FaceLayer.Wall);

            // Ручка
            AddBlock(right - 8, -12, 594.8, right - 2, -4, 599.5, null, Color.Goldenrod, FaceLayer.Wall);
        }

        private void AddRightWallServiceDoor()
        {
            Color frameColor = Color.FromArgb(78, 48, 26);
            Color doorColor = Color.FromArgb(108, 72, 44);
            Color innerPanel = Color.FromArgb(124, 84, 52);
            Color handleColor = Color.Goldenrod;

            double xDoor = 299.0;      // плоскость двери чуть внутри комнаты
            double xFrame1 = 296.5;    // толщина рамки

            double zLeft = 470;
            double zRight = 540;

            double bottom = -100;
            double top = 72;

            // Рамка двери
            AddBlock(
                xFrame1, bottom, zLeft - 4,
                300, top + 4, zLeft,
                null,
                frameColor,
                FaceLayer.Wall
            );

            AddBlock(
                xFrame1, bottom, zRight,
                300, top + 4, zRight + 4,
                null,
                frameColor,
                FaceLayer.Wall
            );

            AddBlock(
                xFrame1, top,
                zLeft - 4,
                300, top + 4,
                zRight + 4,
                null,
                frameColor,
                FaceLayer.Wall
            );

            // Полотно двери
            AddQuad(
                xDoor, bottom, zLeft,
                xDoor, top, zLeft,
                xDoor, top, zRight,
                xDoor, bottom, zRight,
                null,
                doorColor,
                FaceLayer.Wall
            );

            // Внутренняя декоративная панель
            AddQuad(
                xDoor - 0.15, -78, zLeft + 10,
                xDoor - 0.15, 20, zLeft + 10,
                xDoor - 0.15, 20, zRight - 10,
                xDoor - 0.15, -78, zRight - 10,
                null,
                innerPanel,
                FaceLayer.WallDetail
            );

            // Ручка
            AddBlock(
                294.8, -16, zRight - 14,
                299.2, -6, zRight - 8,
                null,
                handleColor,
                FaceLayer.WallDetail
            );
        }

        // =========================================================
        // БАЗОВАЯ ГЕОМЕТРИЯ
        // =========================================================

        private void AddEdge(int startIndex, int endIndex)
        {
            _model.AddEdge(_model.Points[startIndex], _model.Points[endIndex]);
        }

        private void AddBlock(
            double x1,
            double y1,
            double z1,
            double x2,
            double y2,
            double z2,
            string textureKey = null,
            Color color = default,
            FaceLayer layer = FaceLayer.Furniture)
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

        private void AddQuad(
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double x3, double y3, double z3,
            double x4, double y4, double z4,
            string textureKey,
            Color color,
            FaceLayer layer)
        {
            int p1 = _model.AddPoint(x1, y1, z1);
            int p2 = _model.AddPoint(x2, y2, z2);
            int p3 = _model.AddPoint(x3, y3, z3);
            int p4 = _model.AddPoint(x4, y4, z4);

            _model.AddFace(
                new List<int> { p1, p2, p3, p4 },
                textureKey,
                color,
                layer
            );
        }

        private void TryAddColliderForBlock(
            double minX,
            double minY,
            double minZ,
            double maxX,
            double maxY,
            double maxZ,
            FaceLayer layer,
            string name)
        {
            if (layer != FaceLayer.Furniture)
                return;

            double sizeX = maxX - minX;
            double sizeZ = maxZ - minZ;

            if (sizeX < 12 || sizeZ < 12)
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

            _model.AddCollider(-230, 375, 180, 455, "bar_main");
        }

        // =========================================================
        // КАМЕРА / КОЛЛИЗИИ
        // =========================================================

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

            Camera.Pitch += pitchDelta;
            Camera.Pitch = Clamp(Camera.Pitch, -0.85, 0.85);

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
                { 0, 90 },
                { 0, 120 },
                { -70, 80 },
                { 70, 80 },
                { 0, 160 }
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
            if (x < RoomMinX + PlayerRadius)
                return true;

            if (x > RoomMaxX - PlayerRadius)
                return true;

            if (z < RoomMinZ + PlayerRadius)
                return true;

            if (z > RoomMaxZ - PlayerRadius)
                return true;

            for (int i = 0; i < _model.Colliders.Count; i++)
            {
                BoxCollider collider = _model.Colliders[i];

                if (!collider.Enabled)
                    continue;

                if (CircleIntersectsBox(x, z, PlayerRadius, collider))
                    return true;
            }

            return false;
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