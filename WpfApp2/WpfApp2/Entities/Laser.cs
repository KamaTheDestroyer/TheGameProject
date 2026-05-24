using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace WpfApp2.Entities
{
    public class Laser
    {
        public Rectangle Shape;
        public Vector Velocity;
        public double LifeTime;
        public bool FromEnemy;
    }
}
