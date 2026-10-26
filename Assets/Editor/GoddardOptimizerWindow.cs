using System;
using System.IO;
using System.Linq;
using System.Text;
using Goddard.Core;
using UnityEditor;
using UnityEngine;

namespace Goddard.Stand.EditorTools
{
    /// <summary>
    /// Офлайн-оптимизация профилей тяги — НЕ в FixedUpdate.
    /// Порядок кнопок повторяет драматургию занятия 3:
    ///   Акт 1. Наивная оптимизация без структуры — околооптимум ценой ШИМ-зигзага;
    ///   Акт 2. Формула дуги известна, стык решается на лету — заикание у выходного стыка;
    ///   Акт 3. ПМП: сначала широкая карта ландшафта, затем сужаем область и ищем (s1, s2).
    /// Профили экспортируются в JSON для ThrustProfilePlayer.
    /// </summary>
    public class GoddardOptimizerWindow : EditorWindow
    {
        private int steps = 1001;
        private double tEnd = 0.35;

        // Область поиска (s1, s2): дефолты — выведенные границы от t_b = 0.0571;
        // после обзора полной карты сужаются вручную под интересующую область.
        private double s1Lo = 0.003, s1Hi = 0.054;   // 0.05..0.95 · t_b
        private double s2Lo = 0.011, s2Hi = 0.114;   // 0.2..2 · t_b
        private int nS1 = 16, nS2 = 16;

        private string chatteredPath = "Assets/optimal_profiles/goddard_profile_chattering.json";
        private string greedyPath = "Assets/optimal_profiles/goddard_profile_junction.json";
        private string optimalPath = "Assets/optimal_profiles/goddard_profile.json";
        private string status = "—";

        private double bestS1 = 0.02, bestS2 = 0.07, bestH;

        [MenuItem("Goddard/Optimal Profiles")]
        public static void Open() => GetWindow<GoddardOptimizerWindow>("Goddard Optimizer");

        private Vector2 scroll;
        private readonly HeatmapView heatmap = new HeatmapView();

        void OnEnable() => wantsMouseMove = true;   // тултип карты высот
        void OnDisable() => heatmap.Dispose();

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            steps = EditorGUILayout.IntField("Узлов сетки", steps);
            tEnd = EditorGUILayout.DoubleField("tEnd (модельное)", tEnd);
            chatteredPath = EditorGUILayout.TextField("JSON наивного", chatteredPath);
            greedyPath = EditorGUILayout.TextField("JSON жадного", greedyPath);
            optimalPath = EditorGUILayout.TextField("JSON оптимума", optimalPath);

            // Все константы выводятся из двух масштабов: шага интегратора и времени
            // выгорания на полной тяге t_b = (M0−Mf)·c/UMax = 0.0571 (тест Циолковского).
            var p = Params.P;
            double dtInt = tEnd / (steps - 1);
            double tBurnFull = (p.M0 - p.Mf) * p.C / p.UMax;
            double segDt = 10 * dtInt;                                  // узел управления = 10 шагов интегратора
            double tBurn = 2 * tBurnFull;                               // горизонт: любой разумный профиль выгорает раньше
            int nSeg = (int)Math.Ceiling(tBurn / segDt);                // 33
            int rolloutSteps = (int)Math.Ceiling(tBurn / (2 * dtInt)) + 1;  // прогноз жадного грубее полёта вдвое

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Акт 1. Наивная оптимизация (без структуры)", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Наивный поиск из full thrust"))
            {
                var law = PiecewiseConstantLaw.FromFullThrust(nSeg, tBurn, p);
                double hStart = ChatteringDemo.Evaluate(law, steps, tEnd, p);
                double hNaive = ChatteringDemo.CoordinateDescent(law, maxSweeps: 40, tol: 1e-7, steps, tEnd, p);
                int zig = ChatteringDemo.CountZigzags(law.U);
                ExportProfile(
                    law.U.Select((u, k) => ((float)(k * law.SegDt), (float)u)).ToArray(),
                    chatteredPath, "naive-pwm");
                status = $"наивный: h={hNaive:F6} (старт {hStart:F6}), зигзагов {zig} → {chatteredPath}";
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Акт 2. Дуга известна, стык — на лету", EditorStyles.boldLabel);

            if (GUILayout.Button("2. Жадный переключатель (прогноз апогея)"))
            {
                var greedy = GreedySwitcher.Build(
                    segDt, stepsPerSeg: 11, rolloutSteps: rolloutSteps, tHorizon: tBurn, p: p);
                double hGreedy = ChatteringDemo.Evaluate(greedy, steps, tEnd, p);
                int zig = ChatteringDemo.CountZigzags(greedy.U);
                ExportProfile(
                    greedy.U.Select((u, k) => ((float)(k * greedy.SegDt), (float)u)).ToArray(),
                    greedyPath, "greedy-junction");
                status = $"жадный: h={hGreedy:F6}, переключений у выходного стыка {zig} → {greedyPath}";
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Акт 3. ПМП: структура + стыки (s1, s2)", EditorStyles.boldLabel);

            DrawGridSettings(tBurnFull, dtInt);

            if (GUILayout.Button("3. Ландшафт области → CSV + карта"))
            {
                if (ValidateRegion())
                {
                    string dir = Path.Combine(Application.dataPath, "optimal_profiles");
                    Directory.CreateDirectory(dir);

                    var scan = ParameterOptimizer.ScanGrid(s1Lo, s1Hi, nS1, s2Lo, s2Hi, nS2, steps, tEnd);
                    var sb = new StringBuilder("s1,s2,h_max\n");
                    for (int i = 0; i < scan.S1Grid.Length; i++)
                    for (int j = 0; j < scan.S2Grid.Length; j++)
                        sb.AppendLine(System.FormattableString.Invariant(
                            $"{scan.S1Grid[i]},{scan.S2Grid[j]},{scan.Landscape[i, j]}"));
                    File.WriteAllText("Assets/optimal_profiles/goddard_landscape.csv", sb.ToString());
                    AssetDatabase.Refresh();
                    heatmap.SetData(scan.S1Grid, scan.S2Grid, scan.Landscape);
                    status = $"ландшафт [{s1Lo:F3}..{s1Hi:F3}]x[{s2Lo:F3}..{s2Hi:F3}] → Assets/optimal_profiles/goddard_landscape.csv; " +
                             $"best ({scan.S1:F4}, {scan.S2:F4}), h={scan.HMax:F6}";
                }
            }

            if (heatmap.Draw()) Repaint();

            if (GUILayout.Button("4. Поиск (s1, s2) в заданной области"))
            {
                if (ValidateRegion())
                {
                    var r = ParameterOptimizer.Solve(s1Lo, s1Hi, nS1, s2Lo, s2Hi, nS2, steps, tEnd);
                    bestS1 = r.S1; bestS2 = r.S2; bestH = r.HMax;
                    status = $"структура: s1={r.S1:F4}, s2={r.S2:F4}, h_max={r.HMax:F6} ({r.ExitReason})";
                }
            }

            if (GUILayout.Button("5. Экспортировать чистый профиль"))
            {
                ExportProfile(
                    ParameterOptimizer.SampleProfile(bestS1, bestS2, steps, tEnd)
                        .Select(q => ((float)q.t, (float)q.u)).ToArray(),
                    optimalPath, "pmp-clean");
                status = $"чистый профиль → {optimalPath} (s1={bestS1:F4}, s2={bestS2:F4}, h={bestH:F6})";
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.HelpBox(
                "Ориентиры: full thrust 1.0125, эталон COPS 1.01284. " +
                "Наивный ≈ 1.0129 (ШИМ-зигзаг), жадный ≈ 1.01284 (заикание у выходного стыка), " +
                "структурный ≈ 1.0128. Контраст формы, не значения.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Редактируемая область поиска: широкая для обзора, узкая — для уточнения.</summary>
        private void DrawGridSettings(double tBurnFull, double dtInt)
        {
            EditorGUILayout.LabelField("Область поиска", EditorStyles.miniBoldLabel);
            
            s1Lo = EditorGUILayout.DoubleField("s1Lo", s1Lo);
            s1Hi = EditorGUILayout.DoubleField("s1Hi", s1Hi);
            nS1 = Mathf.Max(2, EditorGUILayout.IntField("nS1", nS1));
            
            s2Lo = EditorGUILayout.DoubleField("s2Lo", s2Lo);
            s2Hi = EditorGUILayout.DoubleField("s2Hi", s2Hi);
            nS2 = Mathf.Max(2, EditorGUILayout.IntField("nS2", nS2));
                
            double ds1 = (s1Hi - s1Lo) / (nS1 - 1), ds2 = (s2Hi - s2Lo) / (nS2 - 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"шаг: Δs1={ds1:F4} ({ds1 / dtInt:F0}·dt), Δs2={ds2:F4} ({ds2 / dtInt:F0}·dt)",
                    EditorStyles.miniLabel);
                if (GUILayout.Button("Сбросить к выведенным", EditorStyles.miniButton, GUILayout.Width(160)))
                {
                    s1Lo = 0.05 * tBurnFull; s1Hi = 0.95 * tBurnFull;
                    s2Lo = 0.2 * tBurnFull;  s2Hi = 2.0 * tBurnFull;
                    nS1 = 16; nS2 = 16;
                }
            }
        }

        private bool ValidateRegion()
        {
            if (s1Lo >= s1Hi || s2Lo >= s2Hi)
            {
                status = "пустая область: проверьте границы s1/s2";
                return false;
            }
            return true;
        }

        private static void ExportProfile((float t, float u)[] pts, string path, string label)
        {
            var profile = new ThrustProfile
            {
                label = label,
                points = pts.Select(q => new ThrustProfilePoint { t = q.t, u = q.u }).ToArray()
            };
            string dir = Path.Combine(Application.dataPath, "optimal_profiles");
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(profile, prettyPrint: true));
            AssetDatabase.Refresh();
        }
    }
}