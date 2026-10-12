namespace Goddard.Core
{
    /// <summary>
    /// Правая часть ОДУ задачи Годдарда: y = (h, v, m).
    /// </summary>
    public static class Dynamics
    {
        public static double[] F(double t, double[] y, double u, SimParams p)
        {
            double h = y[0], v = y[1], m = y[2];
            if (m <= p.Mf) u = 0.0;                    // топливо кончилось, сухая масса — ограничение
            
            double drag = p.EnableDrag ? Drag.D(h, v, p) : 0.0;
            double g = p.EnableGravity ? p.G0 * (p.H0 / h) * (p.H0 / h) : 0.0;  // (h0/h)^2

            
            return new[] {
                v,
                (u - drag) / m - g,
                -u / p.C                                                        // dm = -T/c
            };
        }
    }
}