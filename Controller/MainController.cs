using System;
using System.Collections.Generic;
using System.Drawing;
using игра_для_проги.Model;

namespace игра_для_проги.Controller
{
    /// <summary>
    /// Собирает сцену кофейни, управляет камерой и проверяет столкновения.
    /// Геометрия мебели сохранена: хоррор-эффект сделан цветами, световыми акцентами и декоративными плоскими деталями.
    /// </summary>
    /// 
    // Furniture — крупный закрытый предмет:
    // корпус, ножка, спинка, шкаф, стойка, холодильник, телевизор, дверь.


    // SmallDetail — мелкая декоративная деталь:
    // кнопка, экран, трещина, ручка, потёк, полоска, накладка, этикетка.


    // Wall / WallDetail — стены и плоские детали на стенах:
    // меню, окно, дверь, декоративная панель на стене.
    public class MainController
    {
        private readonly SceneModel _model;

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

        /// <summary>
        /// Полностью пересобирает сцену. Порядок добавления объектов важен для текущего SceneView.
        /// </summary>
        public void CreateTestScene()
        {
            ClearScene();

            AddRoomShell();
            AddLeftWallWithWindow();
            AddDoorWithGlass();
            AddRightWallServiceDoor();
            AddRoomReferenceOutlines();

            AddFridgeArea();
            AddBarArea();
            AddTablesAndChairs();
            AddCabinetWithSyrups();
            AddTeaShelf();
            AddWallMenu();
            AddHorrorTvStandAndTv();

            AddManualFurnitureColliders();
            RespawnCamera();
        }

        // =========================================================
        // ЦВЕТОВАЯ ПАЛИТРА ХОРРОР-СЦЕНЫ
        // =========================================================

        /// <summary>
        /// Единая палитра, чтобы менять настроение сцены в одном месте.
        /// </summary>
        private static class HorrorColor
        {
            public static readonly Color Floor = Color.FromArgb(82, 74, 65);
            public static readonly Color Ceiling = Color.FromArgb(72, 66, 60);
            public static readonly Color Wall = Color.FromArgb(126, 112, 96);
            public static readonly Color WallDark = Color.FromArgb(86, 72, 62);
            public static readonly Color DirtyGlass = Color.FromArgb(62, 91, 104);
            public static readonly Color DeepWood = Color.FromArgb(48, 25, 18);
            public static readonly Color DeadWood = Color.FromArgb(68, 36, 24);
            public static readonly Color DeadWoodLight = Color.FromArgb(94, 52, 32);
            public static readonly Color DriedBlood = Color.FromArgb(90, 18, 18);
            public static readonly Color OldBlood = Color.FromArgb(58, 12, 12);
            public static readonly Color Brass = Color.FromArgb(132, 104, 54);
            public static readonly Color Rust = Color.FromArgb(122, 54, 28);
            public static readonly Color ColdSteel = Color.FromArgb(150, 154, 160);
            public static readonly Color SteelDark = Color.FromArgb(80, 86, 92);
            public static readonly Color ScreenBlack = Color.FromArgb(6, 8, 10);
            public static readonly Color SickGreen = Color.FromArgb(54, 122, 94);
            public static readonly Color GhostGreen = Color.FromArgb(78, 126, 112);
            public static readonly Color RedGlow = Color.FromArgb(160, 18, 18);
            public static readonly Color Shadow = Color.FromArgb(18, 16, 18);
            public static readonly Color Bone = Color.FromArgb(192, 185, 166);
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

        // =========================================================
        // СЦЕНА: ОБЩИЕ БЛОКИ
        // =========================================================

        private void ClearScene()
        {
            _model.Points.Clear();
            _model.Edges.Clear();
            _model.Faces.Clear();
            _model.Colliders.Clear();
        }

        /// <summary>
        /// Создаёт коробку комнаты: пол, потолок и три базовые стены.
        /// Левая стена собирается отдельно, потому что в ней есть окно.
        /// </summary>
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

        private void AddRoomReferenceOutlines()
        {
            AddOutlineOnWall(-300, -20, 260, -300, 60, 310);
            AddOutlineOnWall(-188, -100, 600, -124, 72, 600);
        }

        private void AddFridgeArea()
        {
            AddBlock(-295, -100, 510, -250, 80, 595, null, Color.FromArgb(128, 134, 138), FaceLayer.Furniture);
            AddFridgeHandle();
        }

        private void AddBarArea()
        {
            AddMainBarCounter();
            AddSideBarCounter();
            AddCoffeeMachineWallCounter();
            AddBarDecorPanels();

            AddCashRegister(68, -35, 398);
            AddBuiltInSink(162, -35, 388);
            AddPendantLamp(20, 420);
        }

        private void AddTablesAndChairs()
        {
            AddCafeTable(-185, -115, 115, 185);
            AddCafeTable(115, 185, 115, 185);

            AddChairFrontLeft(-240, 130);
            AddChairBackLeft(-170, 195);
            AddChairRight(195, 130);
        }

        private void AddCabinetWithSyrups()
        {
            AddOpenWallCabinet(-299, -65, 408, -250, 55, 498);
            AddCabinetSyrups(-299, -250, -65, 55, 408, 498);
        }

        private void AddWallMenu()
        {
            AddBlock(90, -20, 593, 270, 100, 598, null, HorrorColor.DeepWood, FaceLayer.Wall);

            AddQuad(
                97, -13, 592.85,
                97, 93, 592.85,
                263, 93, 592.85,
                263, -13, 592.85,
                null,
                Color.FromArgb(28, 48, 34),
                FaceLayer.WallDetail
            );

            // Тусклая красная полоса делает меню частью хоррор-атмосферы, но не меняет его форму.
            AddQuad(
                105, 80, 592.70,
                105, 86, 592.70,
                255, 86, 592.70,
                255, 80, 592.70,
                null,
                HorrorColor.OldBlood,
                FaceLayer.WallDetail
            );
        }

        // =========================================================
        // БАРНАЯ СТОЙКА
        // =========================================================

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

        private void AddCoffeeMachineWallCounter()
        {
            AddBlock(255, -100, 456, 299, -35, 590, null, HorrorColor.DeadWood, FaceLayer.Furniture);
            AddCoffeeMachineFacingLeft(259, -35, 462);
        }

        private void AddBarDecorPanels()
        {
            Color panel = Color.FromArgb(82, 31, 24);
            double z = 379.4;

            AddQuad(-89, -95, z, -89, -42, z, 39, -42, z, 39, -95, z, null, panel, FaceLayer.WallDetail);
            AddQuad(49, -95, z, 49, -42, z, 167, -42, z, 167, -95, z, null, panel, FaceLayer.WallDetail);
            AddQuad(177, -95, z, 177, -42, z, 291, -42, z, 291, -95, z, null, panel, FaceLayer.WallDetail);
            AddQuad(-130.4, -95, 390, -130.4, -42, 390, -130.4, -42, 480, -130.4, -95, 480, null, panel, FaceLayer.WallDetail);
            AddQuad(254.4, -95, 466, 254.4, -42, 466, 254.4, -42, 586, 254.4, -95, 586, null, panel, FaceLayer.WallDetail);

            // Узкие потёки на фасаде стойки: это только плоский декор поверх старой формы.
            AddBloodStreak(-45, -95, z - 0.15, 8);
            AddBloodStreak(86, -95, z - 0.15, 11);
            AddBloodStreak(224, -95, z - 0.15, 7);
        }

        private void AddBloodStreak(double x, double topY, double z, double length)
        {
            AddQuad(
                x, topY, z,
                x, topY - length, z,
                x + 2.5, topY - length, z,
                x + 2.5, topY, z,
                null,
                HorrorColor.OldBlood,
                FaceLayer.SmallDetail
            );
        }

        // =========================================================
        // ТЕХНИКА И ДЕТАЛИ БАРА
        // =========================================================

        private void AddFridgeHandle()
        {
            AddBlock(-249.2, -25, 545, -245.6, 55, 553, null, HorrorColor.ColdSteel, FaceLayer.Furniture);
        }

        private void AddPendantLamp(double centerX, double centerZ)
        {
            Color cable = Color.FromArgb(28, 28, 30);
            Color shade = Color.FromArgb(118, 122, 124);
            Color bulb = Color.FromArgb(190, 164, 90);
            Color redTint = Color.FromArgb(120, 32, 24);

            AddBlock(centerX - 2, 146, centerZ - 2, centerX + 2, 150, centerZ + 2, null, cable, FaceLayer.Furniture);
            AddBlock(centerX - 1, 96, centerZ - 1, centerX + 1, 146, centerZ + 1, null, cable, FaceLayer.Furniture);
            AddBlock(centerX - 24, 74, centerZ - 24, centerX + 24, 86, centerZ + 24, null, shade, FaceLayer.Furniture);
            AddBlock(centerX - 8, 67, centerZ - 8, centerX + 8, 74, centerZ + 8, null, bulb, FaceLayer.Furniture);

            // Тёмно-красная нижняя кромка визуально делает свет тревожным.
            AddBlock(centerX - 20, 73.4, centerZ - 20, centerX + 20, 74.2, centerZ + 20, null, redTint, FaceLayer.SmallDetail);
        }

        private void AddCoffeeMachineFacingLeft(double x, double y, double z)
        {
            // Цвета кофемашины.
            // steel / steelDark / steelLight — разные оттенки металлического корпуса.
            // anthracite — тёмные технические элементы.
            // glass — чёрная панель управления.
            // display — зелёный экран.
            // tray — поддон под чашки.
            // metal — трубки и носики.
            // black — тёмная передняя кромка.
            Color steel = Color.FromArgb(142, 146, 150);
            Color steelDark = Color.FromArgb(76, 82, 88);
            Color steelLight = Color.FromArgb(176, 178, 180);
            Color anthracite = Color.FromArgb(34, 36, 42);
            Color glass = Color.FromArgb(10, 14, 16);
            Color display = Color.FromArgb(42, 112, 92);
            Color tray = Color.FromArgb(46, 48, 52);
            Color metal = Color.FromArgb(152, 154, 156);
            Color black = Color.FromArgb(14, 16, 18);

            // Кофемашина стоит на правом пристенном блоке.
            // Меньший X — лицевая сторона, которая смотрит внутрь комнаты, к бармену.
            double xFront = x + 4;
            double xBack = x + 30;

            // z1 и z2 — левая/правая границы кофемашины вдоль стены.
            double z1 = z + 2;
            double z2 = z + 126;

            // Плоскость, на которую кладутся плоские лицевые детали:
            // панель, экран, кнопки. Она чуть вынесена вперёд, чтобы не мерцала с корпусом.
            double frontPlane = xFront - 0.35;

            // =====================================================
            // 1. ОСНОВНОЙ КОРПУС КОФЕМАШИНЫ
            // =====================================================

            // Главный металлический корпус кофемашины.
            // Это самая большая закрытая коробка.
            // ВАЖНО: это Furniture, а не SmallDetail, чтобы корпус нормально сортировался.
            AddBlock(
                xFront, y + 2.0, z1,
                xBack, y + 30.0, z2,
                null,
                steel,
                FaceLayer.Furniture
            );

            // Нижнее тяжёлое основание кофемашины.
            // Чуть выступает за основной корпус.
            AddBlock(
                xFront - 1.2, y + 0.4, z1 + 6,
                xBack + 1.2, y + 2.4, z2 - 6,
                null,
                steelDark,
                FaceLayer.Furniture
            );

            // Верхний технический блок.
            // Выглядит как высокая верхняя часть кофемашины.
            AddBlock(
                xFront + 2.5, y + 30.0, z1 + 8,
                xBack - 2.5, y + 37.5, z2 - 8,
                null,
                steelDark,
                FaceLayer.Furniture
            );

            // Верхняя светлая крышка.
            // Это верхняя плита, на которой условно могут стоять чашки.
            AddBlock(
                xFront + 6.0, y + 37.5, z1 + 18,
                xBack - 6.0, y + 40.5, z2 - 18,
                null,
                steelLight,
                FaceLayer.Furniture
            );

            // =====================================================
            // 2. ВЕРХНЯЯ ПЛОЩАДКА ДЛЯ ЧАШЕК
            // =====================================================

            // Тёмные полосы сверху — прорези/решётка подогрева чашек.
            for (int i = 0; i < 4; i++)
            {
                double slotZ1 = z1 + 22 + i * 22;

                AddBlock(
                    xFront + 8.0, y + 38.0, slotZ1,
                    xBack - 8.0, y + 38.5, slotZ1 + 10,
                    null,
                    anthracite,
                    FaceLayer.SmallDetail
                );
            }

            // Левый верхний бортик площадки.
            AddBlock(
                xFront + 5.5, y + 38.5, z1 + 16,
                xFront + 7.0, y + 40.2, z2 - 16,
                null,
                steelLight,
                FaceLayer.SmallDetail
            );

            // Правый верхний бортик площадки.
            AddBlock(
                xBack - 7.0, y + 38.5, z1 + 16,
                xBack - 5.5, y + 40.2, z2 - 16,
                null,
                steelLight,
                FaceLayer.SmallDetail
            );

            // =====================================================
            // 3. ЛИЦЕВАЯ ПАНЕЛЬ
            // =====================================================

            // Большая передняя металлическая маска.
            // Она закрывает лицевую сторону корпуса и не даёт видеть "внутренности".
            AddQuad(
                frontPlane, y + 4.0, z1 + 4,
                frontPlane, y + 28.8, z1 + 4,
                frontPlane, y + 28.8, z2 - 4,
                frontPlane, y + 4.0, z2 - 4,
                null,
                steelLight,
                FaceLayer.SmallDetail
            );

            // Чёрная верхняя панель управления.
            AddQuad(
                frontPlane - 0.08, y + 18.5, z1 + 12,
                frontPlane - 0.08, y + 26.0, z1 + 12,
                frontPlane - 0.08, y + 26.0, z2 - 12,
                frontPlane - 0.08, y + 18.5, z2 - 12,
                null,
                glass,
                FaceLayer.SmallDetail
            );

            // Зелёный экран на панели управления.
            AddQuad(
                frontPlane - 0.14, y + 21.0, z1 + 48,
                frontPlane - 0.14, y + 24.0, z1 + 48,
                frontPlane - 0.14, y + 24.0, z2 - 48,
                frontPlane - 0.14, y + 21.0, z2 - 48,
                null,
                display,
                FaceLayer.SmallDetail
            );

            // Маленькие кнопки слева и справа от экрана.
            for (int i = 0; i < 3; i++)
            {
                double leftButtonZ = z1 + 18 + i * 10;
                double rightButtonZ = z2 - 23 - i * 10;

                // Левая кнопка панели.
                AddQuad(
                    frontPlane - 0.12, y + 19.8, leftButtonZ,
                    frontPlane - 0.12, y + 21.4, leftButtonZ,
                    frontPlane - 0.12, y + 21.4, leftButtonZ + 5,
                    frontPlane - 0.12, y + 19.8, leftButtonZ + 5,
                    null,
                    steelLight,
                    FaceLayer.SmallDetail
                );

                // Правая кнопка панели.
                AddQuad(
                    frontPlane - 0.12, y + 19.8, rightButtonZ,
                    frontPlane - 0.12, y + 21.4, rightButtonZ,
                    frontPlane - 0.12, y + 21.4, rightButtonZ + 5,
                    frontPlane - 0.12, y + 19.8, rightButtonZ + 5,
                    null,
                    steelLight,
                    FaceLayer.SmallDetail
                );
            }

            // Тонкий горизонтальный шов под панелью управления.
            AddQuad(
                frontPlane - 0.06, y + 17.2, z1 + 10,
                frontPlane - 0.06, y + 17.8, z1 + 10,
                frontPlane - 0.06, y + 17.8, z2 - 10,
                frontPlane - 0.06, y + 17.2, z2 - 10,
                null,
                steelDark,
                FaceLayer.SmallDetail
            );

            // =====================================================
            // 4. ГРУППЫ ПОДАЧИ КОФЕ
            // =====================================================

            // Левая тёмная группа подачи кофе.
            AddBlock(
                xFront - 4.6, y + 11.2, z1 + 24,
                xFront + 0.1, y + 17.0, z1 + 43,
                null,
                anthracite,
                FaceLayer.SmallDetail
            );

            // Правая тёмная группа подачи кофе.
            AddBlock(
                xFront - 4.6, y + 11.2, z2 - 43,
                xFront + 0.1, y + 17.0, z2 - 24,
                null,
                anthracite,
                FaceLayer.SmallDetail
            );

            // Два носика левой группы.
            AddBlock(xFront - 7.2, y + 8.1, z1 + 30, xFront - 4.8, y + 13.0, z1 + 32, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 7.2, y + 8.1, z1 + 35, xFront - 4.8, y + 13.0, z1 + 37, null, metal, FaceLayer.SmallDetail);

            // Два носика правой группы.
            AddBlock(xFront - 7.2, y + 8.1, z2 - 37, xFront - 4.8, y + 13.0, z2 - 35, null, metal, FaceLayer.SmallDetail);
            AddBlock(xFront - 7.2, y + 8.1, z2 - 32, xFront - 4.8, y + 13.0, z2 - 30, null, metal, FaceLayer.SmallDetail);

            // =====================================================
            // 5. ПОДДОН И РЕШЁТКА
            // =====================================================

            // Широкий поддон под чашки.
            AddBlock(
                xFront - 2.2, y + 4.0, z1 + 16,
                xFront + 7.0, y + 7.0, z2 - 16,
                null,
                tray,
                FaceLayer.SmallDetail
            );

            // Полосы решётки на поддоне.
            for (int i = 0; i < 5; i++)
            {
                double rz1 = z1 + 19 + i * 18;

                AddBlock(
                    xFront - 1.7, y + 5.0, rz1,
                    xFront + 6.4, y + 5.3, rz1 + 9,
                    null,
                    steelDark,
                    FaceLayer.SmallDetail
                );
            }

            // Передняя чёрная кромка поддона.
            AddBlock(
                xFront - 2.6, y + 3.2, z1 + 16,
                xFront - 1.6, y + 7.0, z2 - 16,
                null,
                black,
                FaceLayer.SmallDetail
            );

            // =====================================================
            // 6. ПАРОВЫЕ ТРУБКИ
            // =====================================================

            // Левая вертикальная паровая трубка.
            AddBlock(xFront - 6.4, y + 9.5, z1 + 10, xFront - 5.0, y + 18.0, z1 + 11.4, null, metal, FaceLayer.SmallDetail);

            // Нижний загнутый конец левой трубки.
            AddBlock(xFront - 8.2, y + 8.8, z1 + 10.0, xFront - 6.4, y + 9.9, z1 + 11.2, null, metal, FaceLayer.SmallDetail);

            // Правая вертикальная паровая трубка.
            AddBlock(xFront - 6.4, y + 9.5, z2 - 11.4, xFront - 5.0, y + 18.0, z2 - 10.0, null, metal, FaceLayer.SmallDetail);

            // Нижний загнутый конец правой трубки.
            AddBlock(xFront - 8.2, y + 8.8, z2 - 11.2, xFront - 6.4, y + 9.9, z2 - 10.0, null, metal, FaceLayer.SmallDetail);

            // =====================================================
            // 7. НОЖКИ КОФЕМАШИНЫ
            // =====================================================

            // Передняя левая ножка.
            AddBlock(xFront + 1.0, y, z1 + 10, xFront + 4.0, y + 1.0, z1 + 15, null, anthracite, FaceLayer.SmallDetail);

            // Передняя правая ножка.
            AddBlock(xFront + 1.0, y, z2 - 15, xFront + 4.0, y + 1.0, z2 - 10, null, anthracite, FaceLayer.SmallDetail);

            // Задняя левая ножка.
            AddBlock(xBack - 4.0, y, z1 + 10, xBack - 1.0, y + 1.0, z1 + 15, null, anthracite, FaceLayer.SmallDetail);

            // Задняя правая ножка.
            AddBlock(xBack - 4.0, y, z2 - 15, xBack - 1.0, y + 1.0, z2 - 10, null, anthracite, FaceLayer.SmallDetail);
        }

        private void AddCashRegister(double x, double y, double z)
        {
            Color bodyDark = Color.FromArgb(34, 34, 40);
            Color body = Color.FromArgb(50, 54, 62);
            Color bodyLight = Color.FromArgb(120, 124, 130);
            Color accent = Color.FromArgb(96, 68, 54);
            Color screenBezel = Color.FromArgb(16, 18, 22);
            Color screen = Color.FromArgb(10, 26, 20);
            Color screenGlow = Color.FromArgb(74, 150, 112);
            Color keyboardBase = Color.FromArgb(132, 132, 130);
            Color keyLight = Color.FromArgb(184, 184, 176);
            Color keyMid = Color.FromArgb(112, 112, 110);
            Color printerBody = Color.FromArgb(142, 142, 138);
            Color printerDark = Color.FromArgb(66, 70, 74);
            Color paper = HorrorColor.Paper;
            Color terminalBody = Color.FromArgb(36, 38, 44);
            Color terminalScreen = Color.FromArgb(18, 42, 44);

            AddBlock(x, y, z, x + 56, y + 11, z + 34, null, bodyDark, FaceLayer.Furniture);
            AddBlock(x + 2, y + 11, z + 2, x + 54, y + 16, z + 32, null, body, FaceLayer.Furniture);
            AddBlock(x + 5, y + 16, z + 5, x + 31, y + 18.5, z + 27, null, keyboardBase, FaceLayer.Furniture);

            AddQuad(x + 3, y + 6.3, z + 34.15, x + 53, y + 6.3, z + 34.15, x + 53, y + 7.2, z + 34.15, x + 3, y + 7.2, z + 34.15, null, accent, FaceLayer.SmallDetail);
            AddBlock(x + 23, y + 4.2, z + 34.05, x + 33, y + 6.2, z + 34.8, null, accent, FaceLayer.SmallDetail);
            AddBlock(x + 46, y + 11, z + 6, x + 54, y + 15, z + 28, null, bodyLight, FaceLayer.SmallDetail);

            AddCashKeyboard(x, y, z, keyLight, keyMid);

            AddBlock(x + 36, y + 16, z + 14, x + 42, y + 28, z + 20, null, bodyDark, FaceLayer.Furniture);
            AddBlock(x + 34, y + 27, z + 12, x + 44, y + 30, z + 22, null, bodyLight, FaceLayer.SmallDetail);
            AddBlock(x + 28, y + 30, z + 8, x + 52, y + 47, z + 26, null, bodyLight, FaceLayer.Furniture);
            AddBlock(x + 29, y + 46.2, z + 9, x + 51, y + 47.2, z + 25, null, accent, FaceLayer.SmallDetail);

            AddQuad(x + 30, y + 32, z + 26.2, x + 50, y + 32, z + 26.2, x + 50, y + 45, z + 26.2, x + 30, y + 45, z + 26.2, null, screenBezel, FaceLayer.SmallDetail);
            AddQuad(x + 32, y + 34, z + 26.35, x + 48, y + 34, z + 26.35, x + 48, y + 43, z + 26.35, x + 32, y + 43, z + 26.35, null, screen, FaceLayer.SmallDetail);
            AddQuad(x + 33.2, y + 39.2, z + 26.45, x + 46.8, y + 39.2, z + 26.45, x + 46.8, y + 40.6, z + 26.45, x + 33.2, y + 40.6, z + 26.45, null, screenGlow, FaceLayer.SmallDetail);
            AddQuad(x + 34.0, y + 41.6, z + 26.45, x + 44.0, y + 41.6, z + 26.45, x + 44.0, y + 42.4, z + 26.45, x + 34.0, y + 42.4, z + 26.45, null, HorrorColor.RedGlow, FaceLayer.SmallDetail);

            AddBlock(x + 60, y, z + 8, x + 84, y + 12, z + 28, null, printerBody, FaceLayer.Furniture);
            AddBlock(x + 62, y + 12, z + 10, x + 82, y + 15, z + 26, null, printerBody, FaceLayer.Furniture);
            AddQuad(x + 61, y + 4.5, z + 28.15, x + 83, y + 4.5, z + 28.15, x + 83, y + 10.2, z + 28.15, x + 61, y + 10.2, z + 28.15, null, printerDark, FaceLayer.SmallDetail);
            AddBlock(x + 66.0, y + 14.2, z + 16.8, x + 78.0, y + 14.8, z + 19.2, null, printerDark, FaceLayer.SmallDetail);
            AddBlock(x + 66.8, y + 14.9, z + 17.1, x + 77.2, y + 20.5, z + 18.9, null, paper, FaceLayer.SmallDetail);
            AddBlock(x + 79.2, y + 15.0, z + 22.2, x + 81.0, y + 15.8, z + 24.0, null, HorrorColor.RedGlow, FaceLayer.SmallDetail);

            AddBlock(x + 11, y + 18.2, z - 8, x + 22, y + 20.2, z + 1, null, terminalBody, FaceLayer.SmallDetail);
            AddBlock(x + 14, y + 20.2, z - 6.5, x + 19, y + 26.5, z - 1.5, null, terminalBody, FaceLayer.SmallDetail);
            AddQuad(x + 14.6, y + 21.8, z - 1.35, x + 18.4, y + 21.8, z - 1.35, x + 18.4, y + 25.0, z - 1.35, x + 14.6, y + 25.0, z - 1.35, null, terminalScreen, FaceLayer.SmallDetail);

            AddBlock(x + 3, y - 0.2, z + 3, x + 7, y + 0.8, z + 7, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(x + 49, y - 0.2, z + 3, x + 53, y + 0.8, z + 7, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(x + 3, y - 0.2, z + 27, x + 7, y + 0.8, z + 31, null, bodyDark, FaceLayer.SmallDetail);
            AddBlock(x + 49, y - 0.2, z + 27, x + 53, y + 0.8, z + 31, null, bodyDark, FaceLayer.SmallDetail);
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

            // Металлическая верхняя площадка мойки.
            AddBlock(x, y + 0.4, z, x + 34, y + 2.0, z + 42, null, steel, FaceLayer.Furniture);

            // Передний бортик мойки.
            AddBlock(x + 2, y + 2.0, z + 2, x + 32, y + 4.8, z + 7, null, steelDark, FaceLayer.SmallDetail);

            // Задний бортик мойки.
            AddBlock(x + 2, y + 2.0, z + 35, x + 32, y + 4.8, z + 40, null, steelDark, FaceLayer.SmallDetail);

            // Левый бортик мойки.
            AddBlock(x + 2, y + 2.0, z + 7, x + 7, y + 4.8, z + 35, null, steelDark, FaceLayer.SmallDetail);

            // Правый бортик мойки.
            AddBlock(x + 27, y + 2.0, z + 7, x + 32, y + 4.8, z + 35, null, steelDark, FaceLayer.SmallDetail);

            // Тёмная внутренняя часть чаши.
            AddQuad(x + 7, y + 2.1, z + 7, x + 27, y + 2.1, z + 7, x + 27, y + 2.1, z + 35, x + 7, y + 2.1, z + 35, null, basinDark, FaceLayer.SmallDetail);

            // Тёмно-красное пятно на дне чаши.
            AddQuad(x + 12, y + 2.2, z + 14, x + 22, y + 2.2, z + 14, x + 22, y + 2.2, z + 28, x + 12, y + 2.2, z + 28, null, HorrorColor.OldBlood, FaceLayer.SmallDetail);

            // Вертикальная стойка крана.
            AddBlock(x + 14, y + 2.0, z + 6, x + 17, y + 18.0, z + 9, null, faucet, FaceLayer.Furniture);

            // Горизонтальная дуга/носик крана.
            AddBlock(x + 10, y + 16.0, z + 6, x + 21, y + 18.0, z + 21, null, faucet, FaceLayer.Furniture);

            // Кончик крана, откуда условно льётся вода.
            AddBlock(x + 18, y + 12.0, z + 18, x + 21, y + 16.0, z + 21, null, faucet, FaceLayer.SmallDetail);
        }

        // =========================================================
        // СТОЛЫ И СТУЛЬЯ
        // =========================================================

        private void AddCafeTable(double x1, double x2, double z1, double z2)
        {
            // Столешница.
            // SmallDetail можно оставить, потому что это плоская верхняя доска.
            AddBlock(
                x1, -45, z1,
                x2, -40, z2,
                null,
                HorrorColor.DeepWood,
                FaceLayer.SmallDetail
            );

            // Передняя левая ножка стола.
            AddBlock(
                x1 + 5, -100, z2 - 10,
                x1 + 10, -46, z2 - 5,
                null,
                HorrorColor.DeadWood,
                FaceLayer.Furniture
            );

            // Передняя правая ножка стола.
            AddBlock(
                x2 - 10, -100, z2 - 10,
                x2 - 5, -46, z2 - 5,
                null,
                HorrorColor.DeadWood,
                FaceLayer.Furniture
            );

            // Задняя левая ножка стола.
            AddBlock(
                x1 + 5, -100, z1 + 5,
                x1 + 10, -46, z1 + 10,
                null,
                HorrorColor.DeadWood,
                FaceLayer.Furniture
            );

            // Задняя правая ножка стола.
            AddBlock(
                x2 - 10, -100, z1 + 5,
                x2 - 5, -46, z1 + 10,
                null,
                HorrorColor.DeadWood,
                FaceLayer.Furniture
            );
        }

        private void AddChairFrontLeft(double x, double z)
        {
            // Сиденье стула.
            AddBlock(x, -60, z, x + 45, -55, z + 40, null, HorrorColor.DeepWood, FaceLayer.SmallDetail);

            // Спинка стула слева.
            AddBlock(x, -25, z + 10, x + 5, -5, z + 30, null, HorrorColor.DeepWood, FaceLayer.Furniture);

            // Задняя левая высокая ножка, она же стойка спинки.
            AddBlock(x, -100, z + 30, x + 5, 0, z + 40, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Передняя левая высокая ножка.
            AddBlock(x, -100, z, x + 5, 0, z + 10, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Передняя правая короткая ножка.
            AddBlock(x + 40, -100, z, x + 45, -61, z + 10, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Задняя правая короткая ножка.
            AddBlock(x + 40, -100, z + 30, x + 45, -61, z + 40, null, HorrorColor.DeadWood, FaceLayer.Furniture);
        }

        private void AddChairBackLeft(double x, double z)
        {
            // Сиденье стула.
            AddBlock(x, -60, z, x + 40, -55, z + 45, null, HorrorColor.DeepWood, FaceLayer.SmallDetail);

            // Спинка стула сзади.
            AddBlock(x + 10, -25, z + 40, x + 30, -5, z + 45, null, HorrorColor.DeepWood, FaceLayer.Furniture);

            // Задняя левая высокая ножка.
            AddBlock(x, -100, z + 40, x + 10, 0, z + 45, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Задняя правая высокая ножка.
            AddBlock(x + 40, -100, z + 40, x + 30, 0, z + 45, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Передняя левая короткая ножка.
            AddBlock(x, -100, z, x + 10, -61, z + 5, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Передняя правая короткая ножка.
            AddBlock(x + 40, -100, z, x + 30, -61, z + 5, null, HorrorColor.DeadWood, FaceLayer.Furniture);
        }

        private void AddChairRight(double x, double z)
        {
            // Сиденье стула.
            AddBlock(x, -60, z, x + 45, -55, z + 40, null, HorrorColor.DeepWood, FaceLayer.SmallDetail);

            // Правая боковая спинка стула.
            AddBlock(x + 45, -25, z + 10, x + 50, -5, z + 30, null, HorrorColor.DeepWood, FaceLayer.Furniture);

            // Задняя правая высокая ножка, она же стойка спинки.
            AddBlock(x + 45, -100, z + 30, x + 50, 0, z + 40, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Передняя правая высокая ножка.
            AddBlock(x + 45, -100, z, x + 50, 0, z + 10, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Передняя левая короткая ножка.
            AddBlock(x, -100, z, x + 5, -61, z + 10, null, HorrorColor.DeadWood, FaceLayer.Furniture);

            // Задняя левая короткая ножка.
            AddBlock(x, -100, z + 30, x + 5, -61, z + 40, null, HorrorColor.DeadWood, FaceLayer.Furniture);
        }

        // =========================================================
        // ШКАФ И СИРОПЫ
        // =========================================================

        private void AddOpenWallCabinet(double xBack, double yBottom, double zMin, double xFront, double yTop, double zMax)
        {
            Color wood = HorrorColor.DeepWood;
            double backThickness = 3.0;
            double sideThickness = 4.0;
            double shelfThickness = 4.0;

            AddBlock(xBack, yBottom, zMin, xBack + backThickness, yTop, zMax, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yBottom, zMin, xFront, yTop, zMin + sideThickness, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yBottom, zMax - sideThickness, xFront, yTop, zMax, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yTop - shelfThickness, zMin + sideThickness, xFront, yTop, zMax - sideThickness, null, wood, FaceLayer.Furniture);
            AddBlock(xBack + backThickness, yBottom, zMin + sideThickness, xFront, yBottom + shelfThickness, zMax - sideThickness, null, wood, FaceLayer.Furniture);

            double midY = (yBottom + yTop) / 2.0 - shelfThickness / 2.0;
            AddBlock(xBack + backThickness, midY, zMin + sideThickness, xFront, midY + shelfThickness, zMax - sideThickness, null, wood, FaceLayer.Furniture);
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

            AddSyrupBottle(bottleX1, bottleX2, middleShelfTop + 0.4, zCenters[0], Color.FromArgb(68, 38, 34), Color.FromArgb(154, 58, 88), Color.FromArgb(112, 36, 64), SyrupIconType.Raspberry, "RASP");
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
            AddBlock(xMid - 1.5, baseY + 28.4, zCenter - 2.2, x2 + 1.2, baseY + 29.8, zCenter + 2.2, null, capColor, FaceLayer.SmallDetail);
            AddBlock(x2 + 1.0, baseY + 27.2, zCenter - 0.55, x2 + 2.8, baseY + 28.0, zCenter + 0.55, null, capColor, FaceLayer.SmallDetail);
            AddBlock(x2 + 2.1, baseY + 26.1, zCenter - 0.50, x2 + 2.8, baseY + 27.2, zCenter + 0.50, null, capColor, FaceLayer.SmallDetail);

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
                    AddLabelDiamond(x, centerY, centerZ, 3.45, 1.65, green);
                    AddLabelOval(x, centerY, centerZ, 2.55, 1.25, green);
                    AddLabelOval(x, centerY + 0.15, centerZ - 0.92, 1.45, 0.48, green);
                    AddLabelOval(x, centerY + 0.15, centerZ + 0.92, 1.45, 0.48, green);
                    AddLabelRect(x, centerY - 2.55, centerY + 2.55, centerZ - 0.10, centerZ + 0.10, Color.FromArgb(126, 174, 118));
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
            AddMiniStroke(x, y + 0.90, y + 1.2, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y - 1.2, y - 0.90, z - 0.55, z + 0.55, color);
            AddMiniStroke(x, y + 0.45, y + 0.75, z + 0.18, z + 0.52, color);
            AddMiniStroke(x, y + 0.05, y + 0.35, z - 0.05, z + 0.28, color);
            AddMiniStroke(x, y - 0.35, y - 0.05, z - 0.28, z + 0.05, color);
            AddMiniStroke(x, y - 0.75, y - 0.45, z - 0.52, z - 0.18, color);
        }

        // =========================================================
        // ПОЛКА С ЧАШКАМИ
        // =========================================================

        private void AddTeaShelf()
        {
            Color wood = HorrorColor.DeepWood;

            AddBlock(-7, -85, 592.5, 7, 45, 598, null, wood, FaceLayer.Furniture);
            AddTeaShelfBoard(-57);
            AddTeaShelfBoard(-22);
            AddTeaShelfBoard(13);

            double teaZ = 596.1;
            AddTeaRow(-54.4, teaZ, Color.FromArgb(168, 162, 150), Color.FromArgb(144, 136, 124), Color.FromArgb(170, 166, 156), Color.FromArgb(130, 124, 114));
            AddLatteCupRow(-19.4, teaZ);
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
            double[] xs = { -52, -26, 26, 52 };

            for (int i = 0; i < xs.Length && i < colors.Length; i++)
                AddTeaPair(xs[i], y, z, colors[i]);
        }

        private void AddTeaPair(double x, double y, double z, Color cupColor)
        {
            AddSaucer(x, y + 0.6, z, Color.FromArgb(144, 140, 130));
            AddCup(x, y + 1.15, z, cupColor);
        }

        private void AddLatteCupRow(double y, double z)
        {
            double[] xs = { -52, -26, 26, 52 };
            Color[] cupColors =
            {
                Color.FromArgb(150, 144, 132),
                Color.FromArgb(130, 122, 112),
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
            double[] xs = { -52, -26, 26, 52 };
            Color[] bodyColors =
            {
                Color.FromArgb(158, 150, 136),
                Color.FromArgb(138, 126, 112),
                Color.FromArgb(170, 162, 150),
                Color.FromArgb(144, 132, 118)
            };

            for (int i = 0; i < xs.Length; i++)
                AddPaperCup(xs[i], y + 0.8, z, bodyColors[i]);
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
            double left = -188;
            double right = -124;
            double bottom = -100;
            double top = 72;

            AddBlock(left - 4, bottom, 596.7, left, top + 4, 600, null, frame, FaceLayer.Wall);
            AddBlock(right, bottom, 596.7, right + 4, top + 4, 600, null, frame, FaceLayer.Wall);
            AddBlock(left - 4, top, 596.7, right + 4, top + 4, 600, null, frame, FaceLayer.Wall);

            AddQuad(left, bottom, 599.0, left, top, 599.0, right, top, 599.0, right, bottom, 599.0, null, door, FaceLayer.Wall);
            AddQuad(left + 8, -78, 598.8, left + 8, -20, 598.8, right - 8, -20, 598.8, right - 8, -78, 598.8, null, panel, FaceLayer.Wall);
            AddQuad(left + 10, 0, 598.85, left + 10, 60, 598.85, right - 10, 60, 598.85, right - 10, 0, 598.85, null, HorrorColor.DirtyGlass, FaceLayer.Wall);

            AddBlock(left + 8, -2, 598.4, right - 8, 2, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(left + 8, 58, 598.4, right - 8, 62, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(left + 8, 0, 598.4, left + 12, 60, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(right - 12, 0, 598.4, right - 8, 60, 599.3, null, frame, FaceLayer.Wall);
            AddBlock(left + 30, 0, 598.5, left + 34, 60, 599.25, null, frame, FaceLayer.Wall);

            AddDoorHandleOnZPlane(right - 5.0, -5.0, 598.95);
        }

        private void AddRightWallServiceDoor()
        {
            Color frame = HorrorColor.DeepWood;
            Color door = Color.FromArgb(64, 40, 30);
            Color panel = Color.FromArgb(84, 52, 36);
            double xDoor = 299.0;
            double xFrame1 = 296.5;
            double zLeft = 265;
            double zRight = 335;
            double bottom = -100;
            double top = 72;

            AddBlock(xFrame1, bottom, zLeft - 4, 300, top + 4, zLeft, null, frame, FaceLayer.Wall);
            AddBlock(xFrame1, bottom, zRight, 300, top + 4, zRight + 4, null, frame, FaceLayer.Wall);
            AddBlock(xFrame1, top, zLeft - 4, 300, top + 4, zRight + 4, null, frame, FaceLayer.Wall);

            AddQuad(xDoor, bottom, zLeft, xDoor, top, zLeft, xDoor, top, zRight, xDoor, bottom, zRight, null, door, FaceLayer.Wall);
            AddQuad(xDoor - 0.15, -78, zLeft + 10, xDoor - 0.15, 20, zLeft + 10, xDoor - 0.15, 20, zRight - 10, xDoor - 0.15, -78, zRight - 10, null, panel, FaceLayer.WallDetail);

            AddDoorHandleOnXPlane(xDoor, -6.0, zRight - 11.0);
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

        // =========================================================
        // ХОРРОР-ТЕЛЕВИЗОР
        // =========================================================

        private void AddHorrorTvStandAndTv()
        {
            Color tvBody = Color.FromArgb(30, 30, 34);
            Color tvBodyLight = Color.FromArgb(52, 52, 58);
            Color tvBack = Color.FromArgb(20, 20, 24);
            Color metal = Color.FromArgb(92, 96, 102);
            Color metalDark = Color.FromArgb(56, 60, 66);
            Color screenBlack = HorrorColor.ScreenBlack;
            Color screenDark = Color.FromArgb(12, 24, 26);
            Color screenGlow = Color.FromArgb(42, 88, 78);
            Color ghostGlow = HorrorColor.GhostGreen;
            Color redGlow = HorrorColor.RedGlow;
            Color crackColor = Color.FromArgb(78, 86, 92);

            AddBlock(-44, -100, 8, 44, -54, 48, null, HorrorColor.DeepWood, FaceLayer.Furniture);
            AddBlock(-47, -54, 6, 47, -49, 50, null, HorrorColor.DeadWoodLight, FaceLayer.Furniture);
            AddBlock(-42, -100, 10, 42, -96, 46, null, HorrorColor.Shadow, FaceLayer.SmallDetail);

            AddQuad(-40, -92, 48.35, -40, -60, 48.35, -5, -60, 48.35, -5, -92, 48.35, null, HorrorColor.DeadWood, FaceLayer.SmallDetail);
            AddQuad(5, -92, 48.35, 5, -60, 48.35, 40, -60, 48.35, 40, -92, 48.35, null, HorrorColor.DeadWood, FaceLayer.SmallDetail);
            AddBlock(-1.2, -94, 47.4, 1.2, -58, 49.4, null, HorrorColor.Shadow, FaceLayer.SmallDetail);
            AddBlock(-16, -78, 47.7, -13, -73, 49.5, null, HorrorColor.Brass, FaceLayer.SmallDetail);
            AddBlock(13, -78, 47.7, 16, -73, 49.5, null, HorrorColor.Brass, FaceLayer.SmallDetail);
            AddQuad(-40, -95, 48.45, -40, -89, 48.45, 40, -89, 48.45, 40, -95, 48.45, null, HorrorColor.OldBlood, FaceLayer.SmallDetail);

            AddBlock(-40, -106, 12, -34, -100, 18, null, HorrorColor.Shadow, FaceLayer.SmallDetail);
            AddBlock(34, -106, 12, 40, -100, 18, null, HorrorColor.Shadow, FaceLayer.SmallDetail);
            AddBlock(-40, -106, 38, -34, -100, 44, null, HorrorColor.Shadow, FaceLayer.SmallDetail);
            AddBlock(34, -106, 38, 40, -100, 44, null, HorrorColor.Shadow, FaceLayer.SmallDetail);

            AddBlock(-5, -49, 24, 5, -22, 32, null, metal, FaceLayer.Furniture);
            AddBlock(-18, -52, 18, 18, -48, 38, null, metalDark, FaceLayer.Furniture);
            AddBlock(-12, -48, 22, 12, -46.2, 34, null, metal, FaceLayer.SmallDetail);

            AddBlock(-38, -22, 15, 38, 24, 44, null, tvBody, FaceLayer.Furniture);
            AddBlock(-28, -12, 8, 28, 16, 16, null, tvBack, FaceLayer.Furniture);
            AddBlock(-34, 24, 18, 34, 28, 40, null, tvBodyLight, FaceLayer.Furniture);

            AddQuad(-39, -23, 44.55, -39, 25, 44.55, 39, 25, 44.55, 39, -23, 44.55, null, tvBody, FaceLayer.SmallDetail);
            AddQuad(-36.5, -20.5, 44.72, -36.5, 22.5, 44.72, 36.5, 22.5, 44.72, 36.5, -20.5, 44.72, null, tvBodyLight, FaceLayer.SmallDetail);
            AddQuad(-33.5, -17.5, 45.0, -33.5, 19.5, 45.0, 26.5, 19.5, 45.0, 26.5, -17.5, 45.0, null, screenBlack, FaceLayer.SmallDetail);
            AddQuad(-32.0, -16.0, 45.12, -32.0, 18.0, 45.12, 25.0, 18.0, 45.12, 25.0, -16.0, 45.12, null, screenDark, FaceLayer.SmallDetail);
            AddQuad(-29.5, 13.8, 45.24, -29.5, 16.5, 45.24, 22.5, 16.5, 45.24, 22.5, 13.8, 45.24, null, screenGlow, FaceLayer.SmallDetail);

            AddQuad(-31, 8.8, 45.20, -31, 9.9, 45.20, 24, 9.9, 45.20, 24, 8.8, 45.20, null, Color.FromArgb(36, 76, 78), FaceLayer.SmallDetail);
            AddQuad(-31, 2.8, 45.22, -31, 3.7, 45.22, 24, 3.7, 45.22, 24, 2.8, 45.22, null, Color.FromArgb(28, 64, 62), FaceLayer.SmallDetail);
            AddQuad(-31, -2.5, 45.21, -31, -1.7, 45.21, 24, -1.7, 45.21, 24, -2.5, 45.21, null, Color.FromArgb(36, 74, 70), FaceLayer.SmallDetail);
            AddQuad(-31, -8.0, 45.23, -31, -7.1, 45.23, 24, -7.1, 45.23, 24, -8.0, 45.23, null, Color.FromArgb(30, 58, 56), FaceLayer.SmallDetail);

            AddQuad(-8, -10, 45.28, -8, 8, 45.28, 10, 8, 45.28, 10, -10, 45.28, null, Color.FromArgb(22, 72, 68), FaceLayer.SmallDetail);
            AddQuad(-5.5, 2.0, 45.34, -5.5, 4.2, 45.34, -1.8, 4.2, 45.34, -1.8, 2.0, 45.34, null, redGlow, FaceLayer.SmallDetail);
            AddQuad(3.2, 2.0, 45.34, 3.2, 4.2, 45.34, 6.9, 4.2, 45.34, 6.9, 2.0, 45.34, null, redGlow, FaceLayer.SmallDetail);
            AddQuad(-4.5, -5.5, 45.30, -4.5, -3.5, 45.30, 6.0, -3.5, 45.30, 6.0, -5.5, 45.30, null, ghostGlow, FaceLayer.SmallDetail);

            AddQuad(-27, 15, 45.36, -24, 13, 45.36, -7, -1, 45.36, -10, 1, 45.36, null, crackColor, FaceLayer.SmallDetail);
            AddQuad(-8, -1, 45.37, -6, -3, 45.37, 3, -11, 45.37, 1, -9, 45.37, null, crackColor, FaceLayer.SmallDetail);
            AddQuad(8, 10, 45.36, 10, 8, 45.36, 20, -4, 45.36, 18, -2, 45.36, null, crackColor, FaceLayer.SmallDetail);

            AddBlock(28.5, -16, 44.2, 36.0, 18, 45.5, null, Color.FromArgb(20, 20, 24), FaceLayer.SmallDetail);
            AddTvSpeakerSlots();
            AddBlock(30.3, 3.0, 45.0, 32.0, 4.7, 46.2, null, Color.FromArgb(92, 96, 100), FaceLayer.SmallDetail);
            AddBlock(33.0, 3.0, 45.0, 34.7, 4.7, 46.2, null, Color.FromArgb(92, 96, 100), FaceLayer.SmallDetail);
            AddBlock(30.1, 8.0, 45.0, 32.4, 10.3, 46.2, null, metal, FaceLayer.SmallDetail);
            AddBlock(33.0, 8.0, 45.0, 35.3, 10.3, 46.2, null, metal, FaceLayer.SmallDetail);
            AddBlock(31.3, 14.0, 45.2, 33.6, 15.6, 46.2, null, redGlow, FaceLayer.SmallDetail);
            AddQuad(-25, -23.2, 45.0, -25, -22.4, 45.0, 25, -22.4, 45.0, 25, -23.2, 45.0, null, Color.FromArgb(16, 16, 20), FaceLayer.SmallDetail);
        }

        private void AddTvSpeakerSlots()
        {
            for (int i = 0; i < 5; i++)
            {
                double y = -13 + i * 2.5;
                AddBlock(30.0, y, 45.1, 34.5, y + 0.9, 46.0, null, Color.FromArgb(60, 64, 68), FaceLayer.SmallDetail);
            }
        }

        // =========================================================
        // БАЗОВАЯ ГЕОМЕТРИЯ
        // =========================================================

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

        // ВАЖНО:
        // Порядок граней в AddBlock не менять.
        // SceneView отсекает задние грани у мебели.
        // Если поменять порядок точек, предметы начнут "просвечивать".
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

            _model.AddPoint(minX, minY, minZ); // 0
            _model.AddPoint(maxX, minY, minZ); // 1
            _model.AddPoint(maxX, minY, maxZ); // 2
            _model.AddPoint(minX, minY, maxZ); // 3

            _model.AddPoint(minX, maxY, minZ); // 4
            _model.AddPoint(maxX, maxY, minZ); // 5
            _model.AddPoint(maxX, maxY, maxZ); // 6
            _model.AddPoint(minX, maxY, maxZ); // 7

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

            // ВАЖНО: порядок точек менять нельзя.
            // Эти грани направлены наружу, и SceneView правильно отсекает только задние стороны.
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
            _model.AddFace(new List<int> { p1, p2, p3, p4 }, textureKey, color, layer);
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
            _model.AddCollider(-135, 375, 299, 495, "bar_main");
            _model.AddCollider(255, 456, 299, 590, "coffee_machine_wall_counter");
        }

        // =========================================================
        // КАМЕРА И КОЛЛИЗИИ
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
            if (x < RoomMinX + PlayerRadius || x > RoomMaxX - PlayerRadius)
                return true;

            if (z < RoomMinZ + PlayerRadius || z > RoomMaxZ - PlayerRadius)
                return true;

            for (int i = 0; i < _model.Colliders.Count; i++)
            {
                BoxCollider collider = _model.Colliders[i];

                if (collider.Enabled && CircleIntersectsBox(x, z, PlayerRadius, collider))
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
