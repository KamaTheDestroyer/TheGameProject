using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp2.Entities
{
    public class Enemy
    {
        public FrameworkElement Shape; // изменено с Shape -> FrameworkElement, чтобы поддерживать комбинированные визуалы (круг + буква)
        public Vector Velocity;
        public double Radius;
        public int ScoreValue;
        public bool IsBoost;
        public bool IsHostile;
        public int Health;
        public double ShootCooldown; // для вражеских кораблей

        // Для "треугольных" вражеских кораблей (уровень 5)
        public bool IsTriangleEnemy = false;
        public bool IsOrbiting = false;
        public double OrbitRadius = 0;
        public double OrbitAngle = 0;
        public double OrbitSpeed = 0; // радианы в секунду
        public double ApproachSpeed = 0;
        public int OrbitDirection = 1; // 1 или -1
    }
}
