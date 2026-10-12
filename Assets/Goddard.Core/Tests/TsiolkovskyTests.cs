using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Goddard.Core.Tests
{
    [TestFixture]
    public class TsiolkovskyTests
    {
        [Test]
        public void VacuumNoGravity_MatchesTsiolkovsky()
        {
            var p = new SimParams { EnableDrag = false, EnableGravity = false };

            // Выгорание при полной тяге: t_b = (M0-Mf)*C/UMax = 0.0571.
            // Сетка обязана разрешать его: dt = 1e-4 -> ~571 узел на форсаж,
            // квантованность узла горения <= u/m * dt ≈ 6e-4 (0.23% < 0.5%).
            double[] y0 = { p.H0, 0.0, p.M0 };
            var (T, Y) = Integrator.Solve(y0, tEnd: 1.0, steps: 10001,
                uOfT: _ => p.UMax, p);

            int burnout = 0;
            for (int i = 0; i < Y.Length; i++)
                if (Y[i][2] > p.Mf + 1e-9) burnout = i;

            double vNumeric  = Y[burnout][1];
            double vAnalytic = p.C * Math.Log(p.M0 / p.Mf);   // 0.255413...

            Assert.NotNull(vNumeric);
            Assert.That(vNumeric, Is.EqualTo(vAnalytic).Within(0.5).Percent);

        }

    }
}
