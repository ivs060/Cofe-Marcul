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
            int d0 = _model.AddPoint(-200, -100, 600);
            int d1 = _model.AddPoint(-110, -100, 600);
            int d2 = _model.AddPoint(-110, 90, 600);
            int d3 = _model.AddPoint(-200, 90, 600);

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

            // Кофемашина
            AddCoffeeMachine(-150, -35, 392);

            // Касса
            AddCashRegister(0, -35, 398);

            // Раковина
            AddBuiltInSink(104, -35, 388);

            // Люстра
            AddPendantLamp(20, 420);

            // -------------------------------
            // 4. СТОЛЫ
            // -------------------------------

            AddBlock(-185, -45, 115, -115, -40, 185, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(-180, -100, 175, -175, -40, 180, "wood_dark");
            AddBlock(-125, -100, 175, -120, -40, 180, "wood_dark");
            AddBlock(-180, -100, 120, -175, -40, 125, "wood_dark");
            AddBlock(-125, -100, 120, -120, -40, 125, "wood_dark");

            AddBlock(115, -45, 115, 185, -40, 185, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(120, -100, 175, 125, -40, 180, "wood_dark");
            AddBlock(175, -100, 175, 180, -40, 180, "wood_dark");
            AddBlock(120, -100, 120, 125, -40, 125, "wood_dark");
            AddBlock(175, -100, 120, 180, -40, 125, "wood_dark");

            // -------------------------------
            // 5. СТУЛЬЯ
            // -------------------------------

            AddBlock(-240, -60, 130, -195, -55, 170, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(-240, -25, 140, -235, -5, 160, "wood_dark");
            AddBlock(-240, -100, 160, -235, 0, 170, "wood_dark");
            AddBlock(-240, -100, 130, -235, 0, 140, "wood_dark");
            AddBlock(-200, -100, 130, -195, -55, 140, "wood_dark");
            AddBlock(-200, -100, 160, -195, -55, 170, "wood_dark");

            AddBlock(-170, -60, 195, -130, -55, 240, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(-160, -25, 235, -140, -5, 240, "wood_dark");
            AddBlock(-170, -100, 235, -160, 0, 240, "wood_dark");
            AddBlock(-130, -100, 235, -140, 0, 240, "wood_dark");
            AddBlock(-170, -100, 195, -160, -55, 200, "wood_dark");
            AddBlock(-130, -100, 195, -140, -55, 200, "wood_dark");

            AddBlock(195, -60, 130, 240, -55, 170, "wood_dark", Color.Empty, FaceLayer.SmallDetail);
            AddBlock(240, -25, 140, 245, -5, 160, "wood_dark");
            AddBlock(240, -100, 160, 245, 0, 170, "wood_dark");
            AddBlock(240, -100, 130, 245, 0, 140, "wood_dark");
            AddBlock(195, -100, 130, 200, -55, 140, "wood_dark");
            AddBlock(195, -100, 160, 200, -55, 170, "wood_dark");

            // -------------------------------
            // 6. ШКАФ У ХОЛОДИЛЬНИКА
            // -------------------------------

            AddOpenWallCabinet(-299, -40, 400, -250, 80, 490);

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
            AddBlock(-250, -100, 380, -120, -35, 450, "bar", Color.Empty, FaceLayer.Furniture);
            AddBlock(-120, -100, 380, 10, -35, 450, "bar", Color.Empty, FaceLayer.Furniture);
            AddBlock(10, -100, 380, 150, -35, 450, "bar", Color.Empty, FaceLayer.Furniture);
        }

        private void AddSideBarCounter()
        {
            AddBlock(150, -100, 380, 180, -35, 435, "bar", Color.Empty, FaceLayer.Furniture);
            AddBlock(150, -100, 435, 180, -35, 490, "bar", Color.Empty, FaceLayer.Furniture);
            AddBlock(150, -100, 490, 180, -35, 550, "bar", Color.Empty, FaceLayer.Furniture);
        }

        private void AddBarDecorPanels()
        {
            Color panelColor = Color.FromArgb(112, 68, 36);

            // Только простые плоские передние панели.
            // Без боковых рамок и без конфликтующих объемных деталей.

            double z = 379.4;

            AddQuad(
                -238, -95, z,
                -238, -42, z,
                -110, -42, z,
                -110, -95, z,
                null,
                panelColor,
                FaceLayer.WallDetail
            );

            AddQuad(
                -100, -95, z,
                -100, -42, z,
                18, -42, z,
                18, -95, z,
                null,
                panelColor,
                FaceLayer.WallDetail
            );

            AddQuad(
                28, -95, z,
                28, -42, z,
                142, -42, z,
                142, -95, z,
                null,
                panelColor,
                FaceLayer.WallDetail
            );
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
            Color metal = Color.FromArgb(188, 191, 196);
            Color metalDark = Color.FromArgb(156, 160, 166);
            Color metalLight = Color.FromArgb(205, 208, 213);
            Color black = Color.FromArgb(22, 22, 24);
            Color tray = Color.FromArgb(55, 55, 61);
            Color baseMetal = Color.FromArgb(172, 176, 181);

            // 1) Видимая подставка над баром — чтобы машина не "тонула"
            AddBlock(
                x + 6, y + 0.2, z + 3,
                x + 122, y + 2.2, z + 23,
                null,
                baseMetal,
                FaceLayer.Furniture
            );

            // 2) Основной корпус — поднят над подставкой
            AddBlock(
                x, y + 2.2, z,
                x + 128, y + 34.2, z + 26,
                null,
                metal,
                FaceLayer.Furniture
            );

            // 3) Верхний задний блок
            AddBlock(
                x + 12, y + 34.2, z + 4,
                x + 116, y + 44.2, z + 22,
                null,
                metalDark,
                FaceLayer.Furniture
            );

            // 4) Верхняя крышка
            AddBlock(
                x + 24, y + 44.2, z + 7,
                x + 104, y + 47.0, z + 19,
                null,
                metalLight,
                FaceLayer.Furniture
            );

            // 5) Передняя черная рабочая зона — только плоская фронтальная грань
            // Без выступающих черных боковых деталей.
            AddQuad(
                x + 10, y + 11.0, z + 26.15,
                x + 118, y + 11.0, z + 26.15,
                x + 118, y + 27.0, z + 26.15,
                x + 10, y + 27.0, z + 26.15,
                null,
                black,
                FaceLayer.SmallDetail
            );

            // 6) Нижняя черная планка спереди
            AddQuad(
                x + 14, y + 4.6, z + 26.18,
                x + 114, y + 4.6, z + 26.18,
                x + 114, y + 6.2, z + 26.18,
                x + 14, y + 6.2, z + 26.18,
                null,
                black,
                FaceLayer.SmallDetail
            );

            // 7) Носики — только спереди
            AddBlock(
                x + 22, y + 11.5, z + 26.2,
                x + 33, y + 17.0, z + 31.8,
                null,
                black,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 58, y + 11.5, z + 26.2,
                x + 69, y + 17.0, z + 31.8,
                null,
                black,
                FaceLayer.SmallDetail
            );

            AddBlock(
                x + 94, y + 11.5, z + 26.2,
                x + 105, y + 17.0, z + 31.8,
                null,
                black,
                FaceLayer.SmallDetail
            );

            // 8) Поддон
            AddBlock(
                x + 18, y + 2.3, z + 27.2,
                x + 110, y + 3.8, z + 33.2,
                null,
                tray,
                FaceLayer.SmallDetail
            );
        }

        private void AddCashRegister(double x, double y, double z)
        {
            Color body = Color.FromArgb(63, 63, 68);
            Color screen = Color.FromArgb(8, 20, 26);
            Color lightGray = Color.FromArgb(185, 188, 194);

            AddBlock(
                x, y, z,
                x + 48, y + 13, z + 30,
                null,
                body,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 48, y, z + 8,
                x + 61, y + 12, z + 22,
                null,
                body,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 12, y + 13, z + 14,
                x + 34, y + 30, z + 22,
                null,
                lightGray,
                FaceLayer.Furniture
            );

            AddQuad(
                x + 13, y + 14, z + 22.45,
                x + 33, y + 14, z + 22.45,
                x + 33, y + 29, z + 22.45,
                x + 13, y + 29, z + 22.45,
                null,
                screen,
                FaceLayer.Furniture
            );

            AddQuad(
                x + 11.75, y + 13, z + 14,
                x + 11.75, y + 30, z + 14,
                x + 11.75, y + 30, z + 22,
                x + 11.75, y + 13, z + 22,
                null,
                lightGray,
                FaceLayer.SmallDetail
            );

            AddQuad(
                x + 34.25, y + 13, z + 14,
                x + 34.25, y + 30, z + 14,
                x + 34.25, y + 30, z + 22,
                x + 34.25, y + 13, z + 22,
                null,
                lightGray,
                FaceLayer.SmallDetail
            );
        }

        private void AddBuiltInSink(double x, double y, double z)
        {
            Color metal = Color.FromArgb(205, 207, 212);
            Color basin = Color.FromArgb(76, 82, 88);
            Color faucet = Color.FromArgb(220, 220, 224);

            AddBlock(
                x, y, z,
                x + 30, y + 2, z + 40,
                null,
                metal,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 3, y + 2, z + 4,
                x + 27, y + 10, z + 36,
                null,
                basin,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 13, y + 2, z + 6,
                x + 16, y + 18, z + 9,
                null,
                faucet,
                FaceLayer.Furniture
            );

            AddBlock(
                x + 10, y + 16, z + 7,
                x + 20, y + 18, z + 21,
                null,
                faucet,
                FaceLayer.Furniture
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

        // =========================================================
        // ПОЛКА С ЧАЙНЫМИ ПАРАМИ
        // =========================================================

        private void AddTeaShelf()
        {
            Color wood = Color.FromArgb(82, 49, 28);

            // Центральная стойка
            AddBlock(
                -7, -50, 592.5,
                7, 80, 598,
                "wood_dark",
                wood,
                FaceLayer.Furniture
            );

            AddTeaShelfBoard(50);
            AddTeaShelfBoard(15);
            AddTeaShelfBoard(-20);

            // Чашки максимально сдвинуты к стене
            double teaZ = 596.1;

            AddTeaRow(
                52.6,
                teaZ,
                Color.FromArgb(232, 231, 226),
                Color.FromArgb(220, 214, 201),
                Color.FromArgb(228, 226, 220),
                Color.FromArgb(206, 202, 192),
                Color.FromArgb(228, 224, 216),
                Color.FromArgb(214, 210, 199)
            );

            AddTeaRow(
                17.6,
                teaZ,
                Color.FromArgb(228, 226, 220),
                Color.FromArgb(217, 210, 194),
                Color.FromArgb(236, 235, 230),
                Color.FromArgb(208, 203, 194),
                Color.FromArgb(230, 227, 220),
                Color.FromArgb(214, 208, 198)
            );

            AddTeaRow(
                -17.4,
                teaZ,
                Color.FromArgb(229, 226, 221),
                Color.FromArgb(214, 208, 192),
                Color.FromArgb(233, 232, 227),
                Color.FromArgb(204, 199, 188),
                Color.FromArgb(228, 223, 216),
                Color.FromArgb(210, 205, 194)
            );
        }

        private void AddTeaShelfBoard(double y)
        {
            Color wood = Color.FromArgb(82, 49, 28);
            Color woodLight = Color.FromArgb(96, 58, 32);

            // Основная полка
            AddBlock(
                -95, y, 586.2,
                95, y + 2.8, 598.0,
                "wood_dark",
                wood,
                FaceLayer.Furniture
            );

            // Толстый передний бортик-маска
            // Он должен перекрывать чашки при взгляде снизу.
            AddBlock(
                -95, y - 6.0, 585.0,
                95, y + 0.45, 588.3,
                null,
                wood,
                FaceLayer.SmallDetail
            );

            // Верхняя тонкая кромка
            AddBlock(
                -95, y + 2.1, 585.5,
                95, y + 3.0, 587.2,
                null,
                woodLight,
                FaceLayer.SmallDetail
            );
        }

        private void AddTeaRow(double y, double z, params Color[] colors)
        {
            double[] xs = { -75, -45, -15, 15, 45, 75 };

            for (int i = 0; i < xs.Length && i < colors.Length; i++)
            {
                AddTeaPair(xs[i], y, z, colors[i]);
            }
        }

        private void AddTeaPair(double x, double y, double z, Color cupColor)
        {
            AddSaucer(x, y, z, Color.FromArgb(221, 219, 211));
            AddCup(x, y + 0.55, z, cupColor);
        }

        private void AddSaucer(double x, double y, double z, Color color)
        {
            AddBlock(
                x - 5.2, y, z - 3.0,
                x + 5.2, y + 0.75, z + 3.0,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 3.5, y + 0.75, z - 2.0,
                x + 3.5, y + 1.05, z + 2.0,
                null,
                Color.FromArgb(237, 235, 228),
                FaceLayer.Furniture
            );
        }

        private void AddCup(double x, double y, double z, Color color)
        {
            AddBlock(
                x - 3.0, y, z - 2.3,
                x + 3.0, y + 1.5, z + 2.3,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 2.7, y + 1.5, z - 2.1,
                x + 2.7, y + 3.6, z + 2.1,
                null,
                color,
                FaceLayer.Furniture
            );

            AddBlock(
                x - 2.0, y + 3.6, z - 1.5,
                x + 2.0, y + 4.2, z + 1.5,
                null,
                Color.FromArgb(
                    Math.Min(color.R + 10, 255),
                    Math.Min(color.G + 10, 255),
                    Math.Min(color.B + 10, 255)
                ),
                FaceLayer.Furniture
            );

            AddBlock(
                x + 2.9, y + 1.2, z - 0.55,
                x + 3.8, y + 3.2, z + 0.55,
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
                x + 0.6, windowBottom, windowNearZ,
                x + 0.6, windowTop, windowNearZ,
                x + 0.6, windowTop, windowFarZ,
                x + 0.6, windowBottom, windowFarZ,
                null,
                Color.FromArgb(92, 128, 150),
                FaceLayer.Wall
            );

            AddWindowFrame(x, windowBottom, windowTop, windowNearZ, windowFarZ);
        }

        private void AddWindowFrame(double wallX, double bottom, double top, double z1, double z2)
        {
            Color frameColor = Color.FromArgb(88, 56, 32);

            double depth1 = wallX + 1.0;
            double depth2 = wallX + 6.0;
            double t = 5.0;

            AddBlock(depth1, bottom - t, z1 - t, depth2, bottom, z2 + t, null, frameColor, FaceLayer.Wall);
            AddBlock(depth1, top, z1 - t, depth2, top + t, z2 + t, null, frameColor, FaceLayer.Wall);
            AddBlock(depth1, bottom, z1 - t, depth2, top, z1, null, frameColor, FaceLayer.Wall);
            AddBlock(depth1, bottom, z2, depth2, top, z2 + t, null, frameColor, FaceLayer.Wall);

            AddBlock(depth2 - 1, bottom + 8, z1 + 8, depth2 + 1, top - 8, z1 + 11, null, frameColor, FaceLayer.Wall);
            AddBlock(depth2 - 1, bottom + 8, z2 - 11, depth2 + 1, top - 8, z2 - 8, null, frameColor, FaceLayer.Wall);
            AddBlock(depth2 - 1, bottom + 8, z1 + 8, depth2 + 1, bottom + 11, z2 - 8, null, frameColor, FaceLayer.Wall);
            AddBlock(depth2 - 1, top - 11, z1 + 8, depth2 + 1, top - 8, z2 - 8, null, frameColor, FaceLayer.Wall);

            double midZ = (z1 + z2) / 2.0;
            AddBlock(depth2 - 1, bottom + 10, midZ - 1.5, depth2 + 1, top - 10, midZ + 1.5, null, frameColor, FaceLayer.Wall);

            double midY = (bottom + top) / 2.0;
            AddBlock(depth2 - 1, midY - 1.5, z1 + 10, depth2 + 1, midY + 1.5, z2 - 10, null, frameColor, FaceLayer.Wall);
        }

        private void AddDoorWithGlass()
        {
            Color frameColor = Color.FromArgb(76, 46, 24);
            Color doorColor = Color.FromArgb(96, 56, 30);
            Color innerPanel = Color.FromArgb(112, 67, 36);

            AddBlock(-204, -100, 596.7, -200, 94, 600, null, frameColor, FaceLayer.Wall);
            AddBlock(-110, -100, 596.7, -106, 94, 600, null, frameColor, FaceLayer.Wall);
            AddBlock(-204, 90, 596.7, -106, 94, 600, null, frameColor, FaceLayer.Wall);

            AddQuad(
                -200, -100, 599.0,
                -200, 90, 599.0,
                -110, 90, 599.0,
                -110, -100, 599.0,
                null,
                doorColor,
                FaceLayer.Wall
            );

            AddQuad(
                -190, -78, 598.8,
                -190, -10, 598.8,
                -120, -10, 598.8,
                -120, -78, 598.8,
                null,
                innerPanel,
                FaceLayer.Wall
            );

            AddQuad(
                -188, 10, 598.85,
                -188, 76, 598.85,
                -122, 76, 598.85,
                -122, 10, 598.85,
                null,
                Color.FromArgb(110, 145, 168),
                FaceLayer.Wall
            );

            AddBlock(-190, 8, 598.4, -120, 12, 599.3, null, frameColor, FaceLayer.Wall);
            AddBlock(-190, 74, 598.4, -120, 78, 599.3, null, frameColor, FaceLayer.Wall);
            AddBlock(-190, 10, 598.4, -186, 76, 599.3, null, frameColor, FaceLayer.Wall);
            AddBlock(-124, 10, 598.4, -120, 76, 599.3, null, frameColor, FaceLayer.Wall);

            AddBlock(-157, 10, 598.5, -153, 76, 599.25, null, frameColor, FaceLayer.Wall);
            AddBlock(-188, 31, 598.5, -122, 35, 599.25, null, frameColor, FaceLayer.Wall);
            AddBlock(-188, 52, 598.5, -122, 56, 599.25, null, frameColor, FaceLayer.Wall);

            AddBlock(-126, -14, 594.6, -118, -6, 599.5, null, Color.Goldenrod, FaceLayer.Wall);
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

            _model.AddCollider(-255, 375, 155, 455, "bar_main");
            _model.AddCollider(145, 375, 185, 555, "bar_side");
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