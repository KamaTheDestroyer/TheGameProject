using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfApp2.Entities;
using static WpfApp2.Entities.Enums;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Laser> lasers = new List<Laser>();

        readonly Random rand = new Random();

        // Корабль
        Polygon ship;
        RotateTransform shipRotate;
        Point shipCenter;
        double shipAngleDeg = 0;
        double baseRotateSpeed = 140.0;
        double rotateSpeedMultiplier = 1.0;
        const double shipRadiusApprox = 16.0;

        // Ввод
        bool leftPressed = false;
        bool rightPressed = false;
        bool canShoot = true;
        const double shootCooldown = 0.18; // сек
        double shootCooldownTimer = 0;

        // Таймеры / время
        readonly Stopwatch stopwatch = new Stopwatch();
        double lastTime = 0;
        DispatcherTimer spawnTimer;

        // Статус
        bool isGameOver = false;
        int score = 0;

        // Меню и режимы
        Mode currentMode = Mode.Menu;

        // Уровни
        int currentLevel = 0;
        double levelTimer = 0;
        double levelDuration = 0;
        bool levelSurviveMode = false;
        int levelTargetScore = 0;
        bool enemiesTargetShip = false;
        bool boostEnabled = false;
        bool hostileEnabled = false;
        bool shieldActive = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            KeyDown += MainWindow_KeyDown;
            KeyUp += MainWindow_KeyUp;

            // кнопка уровней всегда видна (если есть)
            if (BtnLevelsMenu != null) BtnLevelsMenu.Visibility = Visibility.Visible;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // InitShip and game loop are implemented in MainWindow.Game.cs
            InitShip();
            stopwatch.Start();
            lastTime = stopwatch.Elapsed.TotalSeconds;
            CompositionTarget.Rendering += GameLoop;

            spawnTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
            spawnTimer.Tick += (s, ev) => SpawnEnemy();
            spawnTimer.Stop();

            // show main menu on start
            ShowMainMenu(true);
        }

        void ShowMainMenu(bool show)
        {
            if (MainMenuOverlay == null) return;
            MainMenuOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                currentMode = Mode.Menu;
                // stop spawning while in menu
                spawnTimer?.Stop();
                CompositionTarget.Rendering -= GameLoop;
                // прячем кнопку возврата в меню на случай, если она была видна
                if (ReturnToMenuButton != null) ReturnToMenuButton.Visibility = Visibility.Collapsed;
                GameOverText.Visibility = Visibility.Collapsed;
            }
            else
            {
                CompositionTarget.Rendering += GameLoop;
            }
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PlaceShipAtCenter();
        }
    }
}
