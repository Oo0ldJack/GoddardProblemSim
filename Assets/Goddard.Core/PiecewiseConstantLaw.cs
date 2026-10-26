using System;

namespace Goddard.Core
{
    /// <summary>
    /// Управление без структуры: независимые значения тяги на N сегментах.
    /// Антитезис BangSingularBangLaw — здесь оптимизатор волен делать зигзаг.
    /// Используется в наивной ветке занятия 3: «оптимизируем u(t) без всякой теории».
    /// </summary>
    public sealed class PiecewiseConstantLaw
    {
        public readonly double[] U;
        public readonly double SegDt;
        private readonly SimParams p;

        public PiecewiseConstantLaw(double[] u, double segDt, SimParams p)
        {
            U = u;
            SegDt = segDt;
            this.p = p;
        }

        /// <summary>Управление на момент t: значение своего сегмента; за пределами — 0.</summary>
        public double GetU(double t, double[] y)
        {
            int k = (int)(t / SegDt);
            return (k < 0 || k >= U.Length) ? 0.0 : U[k];
        }

        /// <summary>
        /// Старт наивной оптимизации: полный форсаж на всех сегментах —
        /// ровно то, что известно после занятия 2. Никакой структуры и никаких s1/s2.
        /// </summary>
        public static PiecewiseConstantLaw FromFullThrust(int nSeg, double tBurn, SimParams p)
        {
            var u = new double[nSeg];
            for (int k = 0; k < nSeg; k++) u[k] = p.UMax;
            return new PiecewiseConstantLaw(u, tBurn / nSeg, p);
        }
    }
}