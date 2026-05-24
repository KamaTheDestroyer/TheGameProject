using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfApp2.Entities;
using static WpfApp2.Entities.Enums;

namespace WpfApp2
{
    public partial class MainWindow
    {
        void InitShip()
        {
            ship = new Polygon
            {
                Points = new PointCollection
                    {
                        new Point(0, -15),
                        new Point(10, 10),
                        new Point(-10, 10)
                    },
                Fill = Brushes.LightGray,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            shipRotate = new RotateTransform(0);
            ship.RenderTransform = shipRotate;
            GameCanvas.Children.Add(ship);
            PlaceShipAtCenter();
        }

        void PlaceShipAtCenter()
        {
            // защита от ранних вызовов
            if (ship == null || GameCanvas == null) return;
            if (GameCanvas.ActualWidth <= 0 || GameCanvas.ActualHeight <= 0) return;

            shipCenter = new Point(GameCanvas.ActualWidth / 2.0, GameCanvas.ActualHeight / 2.0);

            // Центрирование полигона: используем Canvas.SetLeft/Top так, чтобы точки вращения совпадали с центром
            Canvas.SetLeft(ship, shipCenter.X - shipRadiusApprox);
            Canvas.SetTop(ship, shipCenter.Y - shipRadiusApprox);

            // update shield position/size if active
            UpdateShieldVisual();
        }

        void UpdateShieldVisual()
        {
            if (ShieldEllipse == null) return;
            if (shieldActive)
            {
                double size = shipRadiusApprox * 6;
                ShieldEllipse.Width = size;
                ShieldEllipse.Height = size;
                Canvas.SetLeft(ShieldEllipse, shipCenter.X - size / 2.0);
                Canvas.SetTop(ShieldEllipse, shipCenter.Y - size / 2.0);
                ShieldEllipse.Visibility = Visibility.Visible;
            }
            else
            {
                ShieldEllipse.Visibility = Visibility.Collapsed;
            }
        }

        // Меню: кнопки
        private void BtnArcade_Click(object sender, RoutedEventArgs e)
        {
            currentMode = Mode.Arcade;
            MainMenuOverlay.Visibility = Visibility.Collapsed;
            LevelsOverlay.Visibility = Visibility.Collapsed;
            ResetGameForArcade();
        }

        private void BtnCampaign_Click(object sender, RoutedEventArgs e)
        {
            // оставляем для совместимости, но теперь уровни выбираются явно
            currentMode = Mode.Playing;
            MainMenuOverlay.Visibility = Visibility.Collapsed;
            LevelsOverlay.Visibility = Visibility.Collapsed;
            StartLevel(1);
        }

        private void BtnLevelsMenu_Click(object sender, RoutedEventArgs e)
        {
            // уровни всегда доступны
            LevelsOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseMenu_Click(object sender, RoutedEventArgs e)
        {
            MainMenuOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnLevel1_Click(object sender, RoutedEventArgs e) => StartLevel(1);
        private void BtnLevel2_Click(object sender, RoutedEventArgs e) => StartLevel(2);
        private void BtnLevel3_Click(object sender, RoutedEventArgs e) => StartLevel(3);
        private void BtnLevel4_Click(object sender, RoutedEventArgs e) => StartLevel(4);
        private void BtnLevel5_Click(object sender, RoutedEventArgs e) => StartLevel(5);
        private void BtnLevelsBack_Click(object sender, RoutedEventArgs e) => LevelsOverlay.Visibility = Visibility.Collapsed;

        void ResetGameForArcade()
        {
            // очистка
            foreach (var en in enemies) if (en.Shape != null) GameCanvas.Children.Remove(en.Shape);
            foreach (var l in lasers) if (l.Shape != null) GameCanvas.Children.Remove(l.Shape);
            enemies.Clear();
            lasers.Clear();
            isGameOver = false;
            score = 0;
            ScoreText.Text = "Score: 0";
            GameOverText.Visibility = Visibility.Collapsed;
            InfoText.Text = "A/D — поворот, Пробел — выстрел";
            ship.Fill = Brushes.LightGray;
            shipAngleDeg = 0;
            shipRotate.Angle = 0;
            rotateSpeedMultiplier = 1.0;
            PlaceShipAtCenter();
            spawnTimer.Interval = TimeSpan.FromSeconds(1.0);
            spawnTimer.Start();
            CompositionTarget.Rendering += GameLoop;
        }

        void StartLevel(int level)
        {
            // полный сброс состояния уровня/прогресса
            foreach (var en in enemies) if (en.Shape != null) GameCanvas.Children.Remove(en.Shape);
            foreach (var l in lasers) if (l.Shape != null) GameCanvas.Children.Remove(l.Shape);
            enemies.Clear();
            lasers.Clear();
            isGameOver = false;
            score = 0;
            ScoreText.Text = "Score: 0";
            GameOverText.Visibility = Visibility.Collapsed;
            ReturnToMenuButton.Visibility = Visibility.Collapsed;
            ship.Fill = Brushes.LightGray;
            shipAngleDeg = 0;
            shipRotate.Angle = 0;
            rotateSpeedMultiplier = 1.0;
            shieldActive = false;
            UpdateShieldVisual();

            // сброс таймеров уровня
            levelTimer = 0;

            currentLevel = level;
            currentMode = Mode.Playing;
            LevelText.Text = $"Level {level}";
            TimerText.Text = "";
            InfoText.Text = "A/D — поворот, Пробел — выстрел\n\nНабери 500 очков!";

            // конфигурация уровней — можно настроить произвольные параметры
            switch (level)
            {
                case 1:

                    levelSurviveMode = false; levelTargetScore = 500; enemiesTargetShip = false; boostEnabled = false; hostileEnabled = false;
                    spawnTimer.Interval = TimeSpan.FromSeconds(1.0);
                    spawnTimer.Start();
                    break;
                case 2:
                    InfoText.Text = "A/D — поворот, Пробел — выстрел\n\nЭти нечисти летят на тебя! Продержись 30сек, защищаясь!";
                    levelSurviveMode = true; levelDuration = 30; levelTargetScore = 0; enemiesTargetShip = true; boostEnabled = false; hostileEnabled = false;
                    spawnTimer.Interval = TimeSpan.FromSeconds(0.9);
                    spawnTimer.Start();
                    break;
                case 3:
                    InfoText.Text = "A/D — поворот, Пробел — выстрел\n\nПодбирай бусты " +
                        "(они ускоряют скорость повотора)\nОни тебе пригодятся!Стало больше нечисти! Продержись 90сек, защищаясь!";
                    levelSurviveMode = true; levelDuration = 90; levelTargetScore = 0;
                    enemiesTargetShip = false; boostEnabled = true; hostileEnabled = false;
                    spawnTimer.Interval = TimeSpan.FromSeconds(0.6); // больше шариков
                    spawnTimer.Start();
                    break;
                case 4:
                    InfoText.Text = "A/D — поворот, Пробел — выстрел\n\nВ этот раз их не просто больше, они тепрь летят в твоем направлении\n" +
                        "Не дай им уничтожить тебя и набери 702 очка!";
                    levelSurviveMode = false; levelTargetScore = 702; enemiesTargetShip = true; boostEnabled = true; hostileEnabled = false;
                    spawnTimer.Interval = TimeSpan.FromSeconds(0.7);
                    spawnTimer.Start();
                    break;
                case 5:
                    InfoText.Text = "A/D — поворот, Пробел — выстрел |  ----> Q - щит\n\nПоявляются вражеские корабли, их цель тебя уничтожить\n" +
                        "Используй щит, дабы не позволить им это сделать!\nИ уничтожай нечисть, она по-прежнему тебе угрожает!";
                    levelSurviveMode = false; levelTargetScore = 0; enemiesTargetShip = false; boostEnabled = false; hostileEnabled = true;
                    spawnTimer.Interval = TimeSpan.FromSeconds(1.1);
                    spawnTimer.Start();
                    break;
            }

            // обеспечить игровой цикл
            lastTime = stopwatch.Elapsed.TotalSeconds;
            CompositionTarget.Rendering += GameLoop;

            // Скрываем меню если оно было открыто
            MainMenuOverlay.Visibility = Visibility.Collapsed;
            LevelsOverlay.Visibility = Visibility.Collapsed;
        }

        void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (isGameOver) return;

            if (e.Key == System.Windows.Input.Key.A) leftPressed = true;
            if (e.Key == System.Windows.Input.Key.D) rightPressed = true;
            if (e.Key == System.Windows.Input.Key.Space && canShoot)
            {
                Shoot();
                canShoot = false;
                shootCooldownTimer = 0;
            }

            // Щит: показываем, пока клавиша Q удерживается; доступен только на уровне 5
            if (e.Key == System.Windows.Input.Key.Q && currentLevel == 5)
            {
                shieldActive = true;
                UpdateShieldVisual();
            }

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // вернуться в меню
                ShowMainMenu(true);
                if (BtnLevelsMenu != null) BtnLevelsMenu.Visibility = Visibility.Visible; // уровни всегда доступны
            }
        }

        void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.A) leftPressed = false;
            if (e.Key == System.Windows.Input.Key.D) rightPressed = false;

            // Убираем щит, когда отпускается Q
            if (e.Key == System.Windows.Input.Key.Q)
            {
                shieldActive = false;
                UpdateShieldVisual();
            }
        }

        void Shoot()
        {
            // направление корабля
            double angleRad = shipAngleDeg * Math.PI / 180.0;
            Vector dir = new Vector(Math.Sin(angleRad), -Math.Cos(angleRad)); // вверх is -Y

            var laser = new Laser
            {
                Shape = new Rectangle
                {
                    Width = 4,
                    Height = 12,
                    Fill = Brushes.Cyan,
                    RadiusX = 2,
                    RadiusY = 2
                },
                Velocity = dir * 600.0,
                LifeTime = 2.5,
                FromEnemy = false
            };

            GameCanvas.Children.Add(laser.Shape);
            // позиция у носа корабля
            Point tip = new Point(shipCenter.X + dir.X * (shipRadiusApprox + 6), shipCenter.Y + dir.Y * (shipRadiusApprox + 6));
            Canvas.SetLeft(laser.Shape, tip.X - laser.Shape.Width / 2.0);
            Canvas.SetTop(laser.Shape, tip.Y - laser.Shape.Height / 2.0);

            // угол наклона лазера
            laser.Shape.RenderTransform = new RotateTransform(shipAngleDeg, laser.Shape.Width / 2.0, laser.Shape.Height / 2.0);

            lasers.Add(laser);
        }

        void SpawnBoost()
        {
            if (GameCanvas.ActualWidth <= 0 || GameCanvas.ActualHeight <= 0) return;

            double radius = 16;
            double w = GameCanvas.ActualWidth;
            double h = GameCanvas.ActualHeight;
            double x = rand.NextDouble() * (w - 2 * radius) + radius;
            double y = rand.NextDouble() * (h - 2 * radius) + radius;

            var enemy = new Enemy
            {
                Radius = radius,
                Velocity = new Vector(rand.NextDouble() * 120 - 60, rand.NextDouble() * 120 - 60),
                ScoreValue = 0,
                IsBoost = true,
                IsHostile = false,
                Health = 1,
                ShootCooldown = 0
            };

            // Создаём контейнер: круг + чёрная буква "B" внутри
            var container = new Grid
            {
                Width = radius * 2,
                Height = radius * 2,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = true
            };

            var ellipse = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.Gold,
                Stroke = Brushes.Orange,
                StrokeThickness = 2
            };

            var tb = new TextBlock
            {
                Text = "B",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = radius, // масштабируем размер буквы
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            container.Children.Add(ellipse);
            container.Children.Add(tb);

            enemy.Shape = container;

            GameCanvas.Children.Add(enemy.Shape);
            Canvas.SetLeft(enemy.Shape, x - radius);
            Canvas.SetTop(enemy.Shape, y - radius);

            enemies.Add(enemy);
        }

        void SpawnEnemy()
        {
            if (isGameOver) return;
            if (GameCanvas.ActualWidth <= 0 || GameCanvas.ActualHeight <= 0) return;

            // На уровне 5 — пригождаем треугольных вражеских кораблей (max 2 параллельно)
            if (currentLevel == 5 && hostileEnabled)
            {
                int existingTriangles = 0;
                foreach (var e in enemies) if (e.IsHostile && e.IsTriangleEnemy) existingTriangles++;
                if (existingTriangles < 2)
                {
                    // шанс спавна треугольного вражеского корабля
                    if (rand.NextDouble() < 0.6) // 60% из спавнов — треугольные враги
                    {
                        SpawnTriangleEnemy();
                        return;
                    }
                }
            }

            // иначе обычный спавн (бусты/шарики)
            // шанс спавна буста на уровнях с boostEnabled
            if (boostEnabled && rand.NextDouble() < 0.08)
            {
                SpawnBoost();
                return;
            }

            // размеры: большие/средние/малые (фиксированные радиусы и очки)
            int sizeCase = rand.Next(3); // 0 big,1 medium,2 small
            double radius = 36;
            int points = 10;
            switch (sizeCase)
            {
                case 0: radius = 36; points = 10; break; // big
                case 1: radius = 24; points = 15; break; // medium
                default: radius = 12; points = 30; break; // small
            }

            double w = GameCanvas.ActualWidth;
            double h = GameCanvas.ActualHeight;
            double x = 0, y = 0;

            // Spawn outside bounds: выбираем сторону
            int side = rand.Next(4); // 0=left,1=top,2=right,3=bottom
            double margin = 20 + radius;
            switch (side)
            {
                case 0: x = -margin; y = rand.NextDouble() * h; break;
                case 1: x = rand.NextDouble() * w; y = -margin; break;
                case 2: x = w + margin; y = rand.NextDouble() * h; break;
                default: x = rand.NextDouble() * w; y = h + margin; break;
            }

            Vector dir;
            if (enemiesTargetShip)
            {
                dir = (shipCenter - new Point(x, y));
                dir += new Vector(rand.NextDouble() * 40 - 20, rand.NextDouble() * 40 - 20);
            }
            else
            {
                if (rand.NextDouble() < 0.5)
                {
                    dir = (shipCenter - new Point(x, y));
                    dir += new Vector(rand.NextDouble() * 200 - 100, rand.NextDouble() * 200 - 100);
                }
                else
                {
                    dir = new Vector(rand.NextDouble() * 2 - 1, rand.NextDouble() * 2 - 1);
                }
            }

            if (dir.Length == 0) dir = new Vector(0, 1);
            dir.Normalize();
            double speed = rand.NextDouble() * 90 + 40;

            var enemy = new Enemy
            {
                Radius = radius,
                Velocity = dir * speed,
                ScoreValue = points,
                IsBoost = false,
                IsHostile = hostileEnabled && rand.NextDouble() < 0.35, // некот. врагов могут быть враждебными
                Health = (hostileEnabled && rand.NextDouble() < 0.35) ? 3 : 1,
                ShootCooldown = 2.0 + rand.NextDouble() * 3.0
            };

            // Shape: обычный эллипс
            var ell = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(Color.FromRgb((byte)rand.Next(80, 255), (byte)rand.Next(20, 180), (byte)rand.Next(20, 200))),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            enemy.Shape = ell;

            GameCanvas.Children.Add(enemy.Shape);
            Canvas.SetLeft(enemy.Shape, x - radius);
            Canvas.SetTop(enemy.Shape, y - radius);

            enemies.Add(enemy);
        }

        void SpawnTriangleEnemy()
        {
            double w = GameCanvas.ActualWidth;
            double h = GameCanvas.ActualHeight;
            double radius = 18; // визуальный радиус
            double x = 0, y = 0;

            // Spawn далеко за границей (чтобы "подлетал из-за карты")
            int side = rand.Next(4);
            double margin = 200 + radius; // больше отступ, чтобы он приходил с "далека"
            switch (side)
            {
                case 0: x = -margin; y = rand.NextDouble() * h; break;
                case 1: x = rand.NextDouble() * w; y = -margin; break;
                case 2: x = w + margin; y = rand.NextDouble() * h; break;
                default: x = rand.NextDouble() * w; y = h + margin; break;
            }

            var enemy = new Enemy
            {
                Radius = radius,
                Velocity = new Vector(0, 0),
                ScoreValue = 250,
                IsBoost = false,
                IsHostile = true,
                Health = 3,
                ShootCooldown = 1.0 + rand.NextDouble() * 2.0,
                IsTriangleEnemy = true,
                IsOrbiting = false,
                ApproachSpeed = 120 + rand.NextDouble() * 80, // скорость подлёта
                OrbitRadius = 150 + rand.NextDouble() * 80,
                OrbitSpeed = 0.8 + rand.NextDouble() * 1.2,
                OrbitDirection = rand.Next(2) == 0 ? 1 : -1
            };

            // Полигон — треугольный корабль, похожий на игрока, но меньше
            var poly = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(0, -12),
                    new Point(8, 10),
                    new Point(-8, 10)
                },
                Fill = Brushes.DarkRed,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            poly.RenderTransform = new RotateTransform(0);

            enemy.Shape = poly;

            GameCanvas.Children.Add(enemy.Shape);
            Canvas.SetLeft(enemy.Shape, x - radius);
            Canvas.SetTop(enemy.Shape, y - radius);

            // начальный угол (для корректного перехода в орбиту)
            enemy.OrbitAngle = Math.Atan2((Canvas.GetTop(enemy.Shape) + enemy.Radius) - shipCenter.Y, (Canvas.GetLeft(enemy.Shape) + enemy.Radius) - shipCenter.X);

            enemies.Add(enemy);
        }

        void GameLoop(object sender, EventArgs e)
        {
            double now = stopwatch.Elapsed.TotalSeconds;
            double dt = now - lastTime;
            lastTime = lastTime = now; // small safety set
            if (dt <= 0) return;

            if (currentMode == Mode.Menu) return;
            if (isGameOver) return;

            // Level timers (campaign-like)
            if (currentMode == Mode.Playing && currentLevel > 0)
            {
                if (levelSurviveMode)
                {
                    levelTimer += dt;
                    TimerText.Text = $"{(int)(levelDuration - levelTimer)}s";
                    if (levelTimer >= levelDuration)
                    {
                        // победа уровня: не переходим автоматически, показываем кнопку возврата в меню
                        ShowWinAndProceed();
                        return;
                    }
                }
                else if (levelTargetScore > 0)
                {
                    TimerText.Text = "";
                    if (score >= levelTargetScore)
                    {
                        ShowWinAndProceed();
                        return;
                    }
                }
            }

            // Ввод: поворот
            double rotateSpeed = baseRotateSpeed * rotateSpeedMultiplier;
            if (leftPressed) shipAngleDeg -= rotateSpeed * dt;
            if (rightPressed) shipAngleDeg += rotateSpeed * dt;
            shipRotate.Angle = shipAngleDeg;

            // cooldown для стрельбы
            if (!canShoot)
            {
                shootCooldownTimer += dt;
                if (shootCooldownTimer >= shootCooldown)
                {
                    canShoot = true;
                    shootCooldownTimer = 0;
                }
            }

            // Обновляем лазеры
            for (int i = lasers.Count - 1; i >= 0; i--)
            {
                var l = lasers[i];
                double lx = Canvas.GetLeft(l.Shape);
                double ly = Canvas.GetTop(l.Shape);
                lx += l.Velocity.X * dt;
                ly += l.Velocity.Y * dt;
                Canvas.SetLeft(l.Shape, lx);
                Canvas.SetTop(l.Shape, ly);

                l.LifeTime -= dt;
                if (l.LifeTime <= 0 ||
                    lx < -400 || ly < -400 || lx > GameCanvas.ActualWidth + 400 || ly > GameCanvas.ActualHeight + 400)
                {
                    GameCanvas.Children.Remove(l.Shape);
                    lasers.RemoveAt(i);
                    continue;
                }

                // проверка попадания по врагам
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    var en = enemies[j];
                    double ex = Canvas.GetLeft(en.Shape) + en.Radius;
                    double ey = Canvas.GetTop(en.Shape) + en.Radius;
                    double lxCenter = lx + l.Shape.Width / 2.0;
                    double lyCenter = ly + l.Shape.Height / 2.0;
                    double ddx = ex - lxCenter;
                    double ddy = ey - lyCenter;
                    double d2 = ddx * ddx + ddy * ddy;
                    if (d2 < (en.Radius + 2) * (en.Radius + 2))
                    {
                        // если это boost
                        if (en.IsBoost)
                        {
                            // применить буст: увеличить множитель поворота на время
                            rotateSpeedMultiplier = 1.8;
                            // установим таймер на 10 сек (локальный таймер)
                            var boostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                            boostTimer.Tick += (s2, ev2) =>
                            {
                                rotateSpeedMultiplier = 1.0;
                                ((DispatcherTimer)s2).Stop();
                            };
                            boostTimer.Start();
                            // удаляем boost
                            GameCanvas.Children.Remove(en.Shape);
                            enemies.RemoveAt(j);
                        }
                        else
                        {
                            // наносим урон врагу (учёт Hostile с Health)
                            en.Health -= 1;
                            if (en.Health <= 0)
                            {
                                // начисляем очки
                                score += en.ScoreValue;
                                ScoreText.Text = "Score: " + score;
                                GameCanvas.Children.Remove(en.Shape);
                                enemies.RemoveAt(j);
                            }
                        }

                        // удаляем лазер
                        GameCanvas.Children.Remove(l.Shape);
                        lasers.RemoveAt(i);
                        break;
                    }
                }
            }

            // Обновляем врагов
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var en = enemies[i];

                // Специальное поведение для треугольных вражеских кораблей (уровень 5)
                if (en.IsTriangleEnemy)
                {
                    double ex = Canvas.GetLeft(en.Shape) + en.Radius;
                    double ey = Canvas.GetTop(en.Shape) + en.Radius;

                    // направляем нос на игрока (во время подхода и в орбите)
                    Vector toShipVec = new Vector(shipCenter.X - ex, shipCenter.Y - ey);
                    if (toShipVec.Length > 0)
                    {
                        double angleFromX = Math.Atan2(toShipVec.Y, toShipVec.X) * 180.0 / Math.PI;
                        // корректировка: у полигона нос смотрит вверх при angle = 0, поэтому добавляем 90
                        double desiredAngle = angleFromX + 90.0;
                        if (en.Shape.RenderTransform is RotateTransform rt) rt.Angle = desiredAngle;
                        else en.Shape.RenderTransform = new RotateTransform(desiredAngle);
                    }

                    // Подлет к орбите
                    if (!en.IsOrbiting)
                    {
                        Vector toShip = new Vector(shipCenter.X - ex, shipCenter.Y - ey);
                        double toShipDist = toShip.Length;
                        if (toShipDist > en.OrbitRadius + 6.0)
                        {
                            toShip.Normalize();
                            ex += toShip.X * en.ApproachSpeed * dt;
                            ey += toShip.Y * en.ApproachSpeed * dt;
                            Canvas.SetLeft(en.Shape, ex - en.Radius);
                            Canvas.SetTop(en.Shape, ey - en.Radius);
                        }
                        else
                        {
                            en.IsOrbiting = true;
                            // установить угол так, чтобы позиция продолжила орбиту плавно
                            en.OrbitAngle = Math.Atan2(ey - shipCenter.Y, ex - shipCenter.X);
                        }
                    }
                    else
                    {
                        // Орбита: обновляем угол и позицию по окружности
                        en.OrbitAngle += en.OrbitSpeed * en.OrbitDirection * dt;
                        double nx = shipCenter.X + Math.Cos(en.OrbitAngle) * en.OrbitRadius;
                        double ny = shipCenter.Y + Math.Sin(en.OrbitAngle) * en.OrbitRadius;
                        Canvas.SetLeft(en.Shape, nx - en.Radius);
                        Canvas.SetTop(en.Shape, ny - en.Radius);
                        ex = nx; ey = ny;
                    }

                    // Стрельба по игроку (рандомно через ShootCooldown)
                    en.ShootCooldown -= dt;
                    if (en.ShootCooldown <= 0)
                    {
                        en.ShootCooldown = 1.0 + rand.NextDouble() * 3.0;
                        // выпустить снаряд в сторону корабля
                        Vector toShip = new Vector(shipCenter.X - ex, shipCenter.Y - ey);
                        if (toShip.Length == 0) toShip = new Vector(0, 1);
                        toShip.Normalize();
                        var enemyLaser = new Laser
                        {
                            Shape = new Rectangle { Width = 4, Height = 12, Fill = Brushes.OrangeRed, RadiusX = 2, RadiusY = 2 },
                            Velocity = toShip * 600.0,
                            LifeTime = 4.0,
                            FromEnemy = true
                        };
                        GameCanvas.Children.Add(enemyLaser.Shape);
                        // позиция у носа вражеского корабля
                        Point tip = new Point(ex + toShip.X * (en.Radius + 6), ey + toShip.Y * (en.Radius + 6));
                        Canvas.SetLeft(enemyLaser.Shape, tip.X - enemyLaser.Shape.Width / 2.0);
                        Canvas.SetTop(enemyLaser.Shape, tip.Y - enemyLaser.Shape.Height / 2.0);
                        enemyLaser.Shape.RenderTransform = new RotateTransform(0, enemyLaser.Shape.Width / 2.0, enemyLaser.Shape.Height / 2.0);
                        lasers.Add(enemyLaser);
                    }

                    // удаление ушедшего далеко треугольного врага (на случай)
                    if (Math.Abs(Canvas.GetLeft(en.Shape) + en.Radius) > GameCanvas.ActualWidth + 1000 ||
                        Math.Abs(Canvas.GetTop(en.Shape) + en.Radius) > GameCanvas.ActualHeight + 1000)
                    {
                        GameCanvas.Children.Remove(en.Shape);
                        enemies.RemoveAt(i);
                    }

                    // продолжаем цикл (обработка столкновений с кораблём ниже в общем коде)
                    continue;
                }

                // вражеские корабли могут стрелять (старый код)
                if (en.IsHostile)
                {
                    en.ShootCooldown -= dt;
                    if (en.ShootCooldown <= 0)
                    {
                        en.ShootCooldown = 1.0 + rand.NextDouble() * 3.0;
                        Vector toShip = (shipCenter - new Point(Canvas.GetLeft(en.Shape) + en.Radius, Canvas.GetTop(en.Shape) + en.Radius));
                        toShip.Normalize();
                        var enemyLaser = new Laser
                        {
                            Shape = new Rectangle { Width = 5, Height = 10, Fill = Brushes.OrangeRed, RadiusX = 2, RadiusY = 2 },
                            Velocity = toShip * 300,
                            LifeTime = 4.0,
                            FromEnemy = true
                        };
                        GameCanvas.Children.Add(enemyLaser.Shape);
                        Canvas.SetLeft(enemyLaser.Shape, Canvas.GetLeft(en.Shape) + en.Radius - enemyLaser.Shape.Width / 2.0);
                        Canvas.SetTop(enemyLaser.Shape, Canvas.GetTop(en.Shape) + en.Radius - enemyLaser.Shape.Height / 2.0);
                        lasers.Add(enemyLaser);
                    }
                }

                double ex2 = Canvas.GetLeft(en.Shape) + en.Radius;
                double ey2 = Canvas.GetTop(en.Shape) + en.Radius;

                ex2 += en.Velocity.X * dt;
                ey2 += en.Velocity.Y * dt;

                Canvas.SetLeft(en.Shape, ex2 - en.Radius);
                Canvas.SetTop(en.Shape, ey2 - en.Radius);

                // Столкновение с кораблём по центрам
                double dx = ex2 - shipCenter.X;
                double dy = ey2 - shipCenter.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < en.Radius + shipRadiusApprox)
                {
                    DestroyShip();
                    return;
                }

                // Удаляем ушедших далеко врагов
                if (!en.IsBoost && (ex2 < -400 || ey2 < -400 || ex2 > GameCanvas.ActualWidth + 400 || ey2 > GameCanvas.ActualHeight + 400))
                {
                    GameCanvas.Children.Remove(en.Shape);
                    enemies.RemoveAt(i);
                }
                else if (en.IsBoost && (ex2 < -400 || ey2 < -400 || ex2 > GameCanvas.ActualWidth + 400 || ey2 > GameCanvas.ActualHeight + 400))
                {
                    GameCanvas.Children.Remove(en.Shape);
                    enemies.RemoveAt(i);
                }
            }

            // Проверка попадания в корабль вражескими лазерами
            for (int i = lasers.Count - 1; i >= 0; i--)
            {
                var L = lasers[i];
                if (!L.FromEnemy) continue;
                double lx = Canvas.GetLeft(L.Shape) + L.Shape.Width / 2.0;
                double ly = Canvas.GetTop(L.Shape) + L.Shape.Height / 2.0;

                // сначала щит
                if (shieldActive)
                {
                    double sx = Canvas.GetLeft(ShieldEllipse) + ShieldEllipse.Width / 2.0;
                    double sy = Canvas.GetTop(ShieldEllipse) + ShieldEllipse.Height / 2.0;
                    double rs = ShieldEllipse.Width / 2.0;
                    double dx = lx - sx;
                    double dy = ly - sy;
                    if (dx * dx + dy * dy <= rs * rs)
                    {
                        // снаряд уничтожен щитом
                        GameCanvas.Children.Remove(L.Shape);
                        lasers.RemoveAt(i);
                        continue;
                    }
                }

                // попадание в корабль
                double dx2 = lx - shipCenter.X;
                double dy2 = ly - shipCenter.Y;
                double shipR = shipRadiusApprox;
                if (dx2 * dx2 + dy2 * dy2 <= shipR * shipR)
                {
                    // попадание в корабль -> поражение
                    DestroyShip();
                    return;
                }
            }
        }

        void ShowWinAndProceed()
        {
            // Показать WIN и остановить уровень, показать кнопку возврата в главное меню
            GameOverText.Text = "WIN";
            GameOverText.Foreground = Brushes.Lime;
            GameOverText.Visibility = Visibility.Visible;

            // остановим спавн и игровой цикл
            spawnTimer?.Stop();
            CompositionTarget.Rendering -= GameLoop;

            // покажем кнопку возврата в меню
            if (ReturnToMenuButton != null) ReturnToMenuButton.Visibility = Visibility.Visible;
        }

        private void ReturnToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Спрячем кнопку и вернёмся в главное меню
            ReturnToMenuButton.Visibility = Visibility.Collapsed;
            GameOverText.Visibility = Visibility.Collapsed;
            ShowMainMenu(true);
        }

        void DestroyShip()
        {
            isGameOver = true;
            GameOverText.Visibility = Visibility.Visible;
            GameOverText.Text = "GAME OVER";
            GameOverText.Foreground = Brushes.Red;
            // останавливаем таймеры и обновления
            spawnTimer?.Stop();
            CompositionTarget.Rendering -= GameLoop;
            ship.Fill = Brushes.DarkRed;
            InfoText.Text = "Нажмите R для перезапуска";
            KeyDown -= MainWindow_KeyDown;
            KeyUp -= MainWindow_KeyUp;
            KeyDown += RestartOnR;
        }

        void RestartOnR(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.R)
            {
                KeyDown -= RestartOnR;
                KeyDown += MainWindow_KeyDown;
                KeyUp += MainWindow_KeyUp;
                // Сброс прогресса и перезапуск уровня/аркады
                if (currentMode == Mode.Arcade)
                {
                    ResetGameForArcade();
                }
                else
                {
                    // перезапускаем текущий уровень с полного сброса
                    StartLevel(currentLevel > 0 ? currentLevel : 1);
                }
            }
        }
    }
}
