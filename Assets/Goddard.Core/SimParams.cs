
using System;

namespace Goddard.Core
{
    /// <summary>
    /// Параметры постановки COPS, безразмерная нормировка.
    /// </summary>
    public record SimParams
    {
        public double H0 = 1.0;
        public double G0 = 1.0;
        public double Hc = 500.0;
        public double Vc = 620.0;
        public double M0 = 1.0;
        public double Mc = 0.6;
        public double Mf => Mc * M0;              // 0.6
        public double Tc = 3.5;
        public double UMax => Tc * M0 * G0;       // T_max = 3.5 — управление = ТЯГА
        
        public bool EnableDrag = true;
        public bool EnableGravity = true;
        
        /// <summary>
        /// Эффективная скорость истечения c.
        /// </summary>
        public double C => 0.5 * Math.Sqrt(G0 * H0);  // 0.5
        
        /// <summary>
        /// Коэффициент сопротивления Dc.
        /// </summary>
        public double Dc => 0.5 * Vc * M0 / G0;       // 310
    }
}
