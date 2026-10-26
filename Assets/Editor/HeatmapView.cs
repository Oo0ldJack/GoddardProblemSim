using UnityEditor;
using UnityEngine;

public sealed class HeatmapView : System.IDisposable
{
    public float Aspect = 1f;          // высота / ширина области графика
    const float MarginLeft = 60f, MarginRight = 70f, MarginBottom = 30f;

    public float MinPlotWidth = 160f;
    public float MaxPlotWidth = 320f;
    public float DefaultPlotWidth = 240f;

    float _availWidth;   // инициализируем в конструкторе


    public string XLabel = "s1", YLabel = "s2", ValueFormat = "0.000000";

    double[] _x, _y;
    double[,] _v;
    Texture2D _tex;
    double _min, _max;

    public HeatmapView()
    {
        _availWidth = DefaultPlotWidth + MarginLeft + MarginRight;   // 370
    }

    public bool HasData => _tex != null;
    
    public double LogBaseline = 1.0;      // вычитается из значения перед log10
    public string LogLabel = "h_max − 1";
    const double LogEps = 1e-12;          // защита от log(0) и отрицательных значений

    bool _logScale = true;
    double _fMin, _fMax;                  // границы в преобразованной шкале

    public bool LogScale
    {
        get => _logScale;
        set
        {
            if (_logScale == value) return;
            _logScale = value;
            if (_v != null) BuildTexture();
        }
    }

    double Transform(double v) =>
        _logScale ? System.Math.Log10(System.Math.Max(v - LogBaseline, LogEps)) : v;

    double Inverse(double f) =>
        _logScale ? LogBaseline + System.Math.Pow(10, f) : f;

    static readonly Color[] Viridis =
    {
        new Color32(68, 1, 84, 255), new Color32(59, 82, 139, 255),
        new Color32(33, 145, 140, 255), new Color32(94, 201, 98, 255),
        new Color32(253, 231, 37, 255),
    };

    static Color Map(float t)
    {
        t = Mathf.Clamp01(t) * (Viridis.Length - 1);
        int i = Mathf.Min((int)t, Viridis.Length - 2);
        return Color.Lerp(Viridis[i], Viridis[i + 1], t - i);
    }

    // values[i, j]: i — индекс по X, j — индекс по Y
    public void SetData(double[] x, double[] y, double[,] values)
    {
        _x = x; _y = y; _v = values;
        _min = double.MaxValue; _max = double.MinValue;
        foreach (var v in values) { if (v < _min) _min = v; if (v > _max) _max = v; }
        BuildTexture();
    }
    
    void BuildTexture()
    {
        int w = _x.Length, h = _y.Length;

        _fMin = double.MaxValue; _fMax = double.MinValue;
        foreach (var v in _v)
        {
            double f = Transform(v);
            if (f < _fMin) _fMin = f;
            if (f > _fMax) _fMax = f;
        }
        double range = System.Math.Max(_fMax - _fMin, 1e-12);

        if (_tex != null) Object.DestroyImmediate(_tex);
        _tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var px = new Color[w * h];
        for (int i = 0; i < w; i++)
        for (int j = 0; j < h; j++)
            px[j * w + i] = Map((float)((Transform(_v[i, j]) - _fMin) / range));
        _tex.SetPixels(px);
        _tex.Apply();
    }
    
    // Возвращает true, если окну нужно вызвать Repaint()
    public bool Draw()
    {
        if (!HasData) return false;

        GUILayout.Space(8);

        // Проба нулевой высоты: узнаём доступную ширину (без навязывания минимума)
        Rect probe = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.Height(0));
        if (Event.current.type == EventType.Repaint && !Mathf.Approximately(probe.width, _availWidth))
        {
            _availWidth = probe.width;
            _needRepaint = true;
        }

        // Высота для layout считается по кэшированной ширине
        float layoutPlotW = Mathf.Clamp(_availWidth - MarginLeft - MarginRight, MinPlotWidth, MaxPlotWidth);
        float layoutH = layoutPlotW * Aspect + MarginBottom;

        Rect outer = GUILayoutUtility.GetRect(0, layoutH,
            GUILayout.ExpandWidth(true), GUILayout.Height(layoutH));

        // При отрисовке берём актуальную ширину, чтобы не было задержки в кадр
        float w = Event.current.type == EventType.Repaint ? outer.width : _availWidth;
        float plotW = Mathf.Clamp(w - MarginLeft - MarginRight, MinPlotWidth, MaxPlotWidth);
        float plotH = plotW * Aspect;
        float offsetX = Mathf.Max(0f, (w - (MarginLeft + plotW + MarginRight)) * 0.5f);

        Rect plot = new Rect(outer.x + offsetX + MarginLeft, outer.y, plotW, plotW * Aspect);

        GUI.DrawTexture(plot, _tex, ScaleMode.StretchToFill, false);
        Handles.DrawSolidRectangleWithOutline(plot, Color.clear, Color.gray);

        DrawAxes(plot);
        DrawColorbar(plot);

        bool repaint = DrawTooltip(plot) || _needRepaint;
        _needRepaint = false;
        return repaint;
    }

    bool _needRepaint;

    void DrawAxes(Rect plot)
    {
        var center = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
        var rightAl = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        float lx = plot.x - 56;   // левая колонка подписей

        GUI.Label(new Rect(plot.x, plot.yMax + 8, plot.width, 18), XLabel, center);
        GUI.Label(new Rect(lx, plot.y - 2, 50, 18), YLabel, rightAl);
        GUI.Label(new Rect(plot.x - 4, plot.yMax, 60, 16), _x[0].ToString("0.###"));
        GUI.Label(new Rect(plot.xMax - 40, plot.yMax, 60, 16), _x[_x.Length - 1].ToString("0.###"));
        GUI.Label(new Rect(lx, plot.yMax - 16, 50, 16), _y[0].ToString("0.###"), rightAl);
        GUI.Label(new Rect(lx, plot.y + 14, 50, 16), _y[_y.Length - 1].ToString("0.###"), rightAl);
    }

    void DrawColorbar(Rect plot)
    {
        Rect bar = new Rect(plot.xMax + 12, plot.y, 14, plot.height);
        for (int k = 0; k < (int)bar.height; k++)
            EditorGUI.DrawRect(new Rect(bar.x, bar.y + k, bar.width, 1), Map(1f - k / bar.height));

        double range = System.Math.Max(_fMax - _fMin, 1e-12);

        if (_logScale)
        {
            // Подписи на целых степенях десяти, попавших в диапазон
            for (int d = (int)System.Math.Ceiling(_fMin); d <= (int)System.Math.Floor(_fMax); d++)
            {
                float t = (float)((d - _fMin) / range);
                float y = bar.yMax - t * bar.height;
                EditorGUI.DrawRect(new Rect(bar.xMax, y, 4, 1), Color.gray);
                GUI.Label(new Rect(bar.xMax + 5, y - 8, 70, 16), $"1e{d}");
            }
            GUI.Label(new Rect(bar.x - 20, bar.y - 22, 140, 16), LogLabel);
        }
        else
        {
            GUI.Label(new Rect(bar.xMax + 2, bar.y - 8, 70, 16), _max.ToString(ValueFormat));
            GUI.Label(new Rect(bar.xMax + 2, bar.yMax - 8, 70, 16), _min.ToString(ValueFormat));
        }
    }
    
    bool DrawTooltip(Rect plot)
    {
        Vector2 m = Event.current.mousePosition;
        if (!plot.Contains(m)) return false;

        int i = Mathf.Clamp((int)((m.x - plot.x) / plot.width * _x.Length), 0, _x.Length - 1);
        int j = Mathf.Clamp((int)((plot.yMax - m.y) / plot.height * _y.Length), 0, _y.Length - 1);
        string tip = $"{XLabel}={_x[i]:0.0000}\n{YLabel}={_y[j]:0.0000}\nvalue={_v[i, j].ToString(ValueFormat)}";
        if (_logScale) tip += $"\nΔ={_v[i, j] - LogBaseline:0.00e+0}";
        var size = GUI.skin.box.CalcSize(new GUIContent(tip));
        GUI.Box(new Rect(m.x + 12, m.y + 12, size.x + 6, size.y + 4), tip);
        return Event.current.type == EventType.MouseMove;
    }

    public void Dispose()
    {
        if (_tex != null) Object.DestroyImmediate(_tex);
        _tex = null;
    }
}