// Colócalo en cualquier carpeta llamada "Editor" (ej. Assets/Water/Editor/StylizedWaterGUI.cs)
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StylizedWaterGUI : ShaderGUI
{
    // ---------------------------------------------------------------- keywords
    const string K_WAVES_OFF   = "_WAVES_OFF";
    const string K_NORMALMAP   = "_NORMALMAP";
    const string K_DEPTH_OFF   = "_DEPTH_OFF";
    const string K_PATTERN_OFF = "_PATTERN_OFF";
    const string K_PATTERN_TEX = "_PATTERN_TEXTURE";
    const string K_FOAM_OFF    = "_FOAM_OFF";
    const string K_SPEC_OFF    = "_SPECULAR_OFF";
    const string K_REFLECT     = "_REFLECTIONS";
    const string K_REFRACT     = "_REFRACTION";
    const string K_ARM         = "_SURFMAP_ARM";
    const string K_SEPARATE    = "_SURFMAP_SEPARATE";

    static readonly string[] AllKeywords =
    {
        K_WAVES_OFF, K_NORMALMAP, K_DEPTH_OFF, K_PATTERN_OFF, K_PATTERN_TEX,
        K_FOAM_OFF, K_SPEC_OFF, K_REFLECT, K_REFRACT, K_ARM, K_SEPARATE
    };

    const string Prefs = "StylizedWaterGUI.";
    static readonly string[] PresetNames = { "Tropical (default)", "Deep Ocean", "Lagoon Pool", "Mobile Lite" };

    MaterialEditor _ed;
    Material _mat;
    readonly Dictionary<string, MaterialProperty> _props = new Dictionary<string, MaterialProperty>();

    // ---------------------------------------------------------------- helpers
    MaterialProperty P(string n) => _props[n];
    bool Kw(string k) => _mat.IsKeywordEnabled(k);
    bool On(string k, bool inverted) => inverted ? !Kw(k) : Kw(k);

    void SetKw(string k, bool on)
    {
        foreach (UnityEngine.Object t in _ed.targets)
        {
            var m = (Material)t;
            if (on) m.EnableKeyword(k); else m.DisableKeyword(k);
            EditorUtility.SetDirty(m);
        }
    }

    void Prop(string name, string label, string tip = null)
        => _ed.ShaderProperty(P(name), new GUIContent(label, tip));

    static Color HeaderBg => EditorGUIUtility.isProSkin ? new Color(0.20f, 0.23f, 0.25f) : new Color(0.78f, 0.86f, 0.89f);
    static readonly Color Accent = new Color(0.18f, 0.72f, 0.78f);

    // ---------------------------------------------------------------- OnGUI
    public override void OnGUI(MaterialEditor ed, MaterialProperty[] props)
    {
        _ed = ed;
        _mat = (Material)ed.target;
        _props.Clear();
        foreach (var p in props) _props[p.name] = p;

        // Mantén _NORMALMAP sincronizado con el slot de textura (menos clics, menos errores)
        bool hasNormal = P("_NormalMap").textureValue != null;
        if (hasNormal != Kw(K_NORMALMAP)) SetKw(K_NORMALMAP, hasNormal);

        DrawBanner();
        DrawPresets();
        DrawCostMeter();
        DrawPipelineWarnings();

        Section("Water Color", "color", DrawColor);
        Section("Depth Effects", "depth", DrawDepth, K_DEPTH_OFF, true,
                "Colorea según la profundidad y suaviza la orilla. Requiere Depth Texture.");
        Section("Pattern (cells / caustics)", "pattern", DrawPattern, K_PATTERN_OFF, true,
                "Celdas animadas. Procedural = 0 texturas.");
        Section("Shore Foam", "foam", DrawFoam, K_FOAM_OFF, true,
                "Espuma donde el agua toca geometría. Requiere Depth Effects.");
        Section("Waves (vertex)", "waves", DrawWaves, K_WAVES_OFF, true,
                "Ondulación en vértices. Usa un plano subdividido.");
        Section("Surface Detail", "surface", DrawSurface);
        Section("Toon Lighting & Fresnel", "light", DrawLighting);
        Section("Toon Specular", "spec", DrawSpecular, K_SPEC_OFF, true);
        Section("Environment Reflections", "refl", DrawReflections, K_REFLECT, false,
                "Muestrea el skybox / reflection probe. Cuesta 1 sample.");
        Section("Refraction", "refr", DrawRefraction, K_REFRACT, false,
                "Distorsiona lo que hay bajo el agua. Requiere Opaque Texture. Es la opción más cara.");
        Section("Advanced", "adv", DrawAdvanced, null, false, null, false);
    }

    // ---------------------------------------------------------------- UI chrome
    void DrawBanner()
    {
        Rect r = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(0.08f, 0.35f, 0.42f));
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 3, r.width, 3), Accent);
        var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, normal = { textColor = Color.white } };
        var sub = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f, 0.95f, 1f) } };
        GUI.Label(new Rect(r.x + 10, r.y + 4, r.width - 20, 20), "Stylized Water  •  URP", title);
        GUI.Label(new Rect(r.x + 10, r.y + 22, r.width - 20, 16), "Optimized, keyword-driven, ARM or separate maps", sub);
        EditorGUILayout.Space(4);
    }

    void DrawPresets()
    {
        EditorGUI.BeginChangeCheck();
        int idx = EditorGUILayout.Popup(new GUIContent("Quick Preset", "Aplica una configuración completa (colores + funciones)."), -1, PresetNames);
        if (EditorGUI.EndChangeCheck() && idx >= 0) ApplyPreset(idx);
        EditorGUILayout.Space(2);
    }

    float Cost(out string label, out string advice)
    {
        float c = 1f;
        if (On(K_DEPTH_OFF, true)) c += 1f;
        if (On(K_PATTERN_OFF, true)) c += Kw(K_PATTERN_TEX) ? 1.5f : 3f;
        if (Kw(K_NORMALMAP)) c += 2f;
        if (Kw(K_ARM)) c += 1f;
        else if (Kw(K_SEPARATE)) c += 3f;
        if (On(K_FOAM_OFF, true) && On(K_DEPTH_OFF, true)) c += 1f;
        if (On(K_WAVES_OFF, true)) c += 0.5f;
        if (On(K_SPEC_OFF, true)) c += 0.5f;
        if (Kw(K_REFLECT)) c += 1.5f;
        if (Kw(K_REFRACT)) c += 3.5f;

        if (c <= 5f)        { label = "Ultra-light";  advice = "Ideal para móviles de gama baja."; }
        else if (c <= 9f)   { label = "Balanced";     advice = "Móviles medios/altos, PC y consolas."; }
        else if (c <= 13f)  { label = "Heavy";        advice = "Recomendado para PC / consola."; }
        else                { label = "Very heavy";   advice = "Desactiva refracción o mapas separados si buscas rendimiento."; }
        return c;
    }

    void DrawCostMeter()
    {
        float c = Cost(out string label, out string advice);
        Rect r = EditorGUILayout.GetControlRect(false, 20);
        Color prev = GUI.color;
        GUI.color = Color.Lerp(new Color(0.4f, 0.9f, 0.5f), new Color(1f, 0.4f, 0.35f), Mathf.Clamp01((c - 1f) / 14f));
        EditorGUI.ProgressBar(r, Mathf.Clamp01(c / 16f), "Shader cost: " + label);
        GUI.color = prev;
        EditorGUILayout.LabelField(advice, EditorStyles.miniLabel);
        EditorGUILayout.Space(2);
    }

    void DrawPipelineWarnings()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            EditorGUILayout.HelpBox("URP no está activo. Asigna un Universal Render Pipeline Asset en Project Settings > Graphics.", MessageType.Error);
            return;
        }
        if (On(K_DEPTH_OFF, true) && !urp.supportsCameraDepthTexture)
        {
            EditorGUILayout.HelpBox("Depth Effects necesita 'Depth Texture' en el URP Asset.", MessageType.Warning);
            if (GUILayout.Button("Enable Depth Texture"))
            {
                urp.supportsCameraDepthTexture = true;
                EditorUtility.SetDirty(urp);
            }
        }
        if (Kw(K_REFRACT) && !urp.supportsCameraOpaqueTexture)
        {
            EditorGUILayout.HelpBox("Refraction necesita 'Opaque Texture' en el URP Asset.", MessageType.Warning);
            if (GUILayout.Button("Enable Opaque Texture"))
            {
                urp.supportsCameraOpaqueTexture = true;
                EditorUtility.SetDirty(urp);
            }
        }
    }

    // ---------------------------------------------------------------- sección plegable con toggle
    void Section(string title, string key, Action body, string kw = null, bool inverted = false,
                 string tip = null, bool defaultOpen = true)
    {
        string pref = Prefs + key;
        bool open = SessionState.GetBool(pref, defaultOpen);

        Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, HeaderBg);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), Accent);

        Rect tog = new Rect(r.xMax - 26, r.y + 4, 18, 18);
        bool on = true;
        if (kw != null)
        {
            on = On(kw, inverted);
            EditorGUI.BeginChangeCheck();
            bool nv = EditorGUI.Toggle(tog, on);
            if (EditorGUI.EndChangeCheck())
            {
                _ed.RegisterPropertyChangeUndo(title);
                SetKw(kw, inverted ? !nv : nv);
                on = nv;
            }
        }

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)
            && (kw == null || !tog.Contains(e.mousePosition)))
        {
            open = !open;
            SessionState.SetBool(pref, open);
            e.Use();
        }

        if (e.type == EventType.Repaint)
            EditorStyles.foldout.Draw(new Rect(r.x + 8, r.y + 4, 16, 16), false, false, open, false);
        GUI.Label(new Rect(r.x + 26, r.y + 3, r.width - 64, 18), new GUIContent(title, tip), EditorStyles.boldLabel);

        if (open)
        {
            EditorGUILayout.Space(3);
            using (new EditorGUI.DisabledScope(!on))
            {
                EditorGUI.indentLevel++;
                body();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(4);
        }
        EditorGUILayout.Space(2);
    }

    // ---------------------------------------------------------------- contenido de secciones
    void DrawColor()
    {
        Prop("_ShallowColor", "Shallow Color", "Color cerca de la orilla. El alfa es la opacidad.");
        Prop("_DeepColor", "Deep Color", "Color en aguas profundas. El alfa es la opacidad.");
        if (!On(K_DEPTH_OFF, true))
            Prop("_FlatDepth", "Flat Blend", "Mezcla fija Shallow↔Deep cuando Depth Effects está apagado.");
        Prop("_UseMeshUV", "Use Mesh UV", "Apagado = UVs en mundo (XZ), la escala no depende del tamaño de la malla.");
    }

    void DrawDepth()
    {
        Prop("_DepthDistance", "Depth Distance", "Distancia (en metros de vista) hasta llegar al color profundo.");
        Prop("_DepthPower", "Depth Curve", "<1 = transición rápida, >1 = transición lenta.");
        Prop("_EdgeSoftness", "Edge Softness", "Desvanece el agua donde toca la orilla.");
    }

    void DrawPattern()
    {
        bool tex = Kw(K_PATTERN_TEX);
        Rect rr = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 20));
        EditorGUI.BeginChangeCheck();
        int mode = GUI.Toolbar(rr, tex ? 1 : 0, new[] { "Procedural (Voronoi)", "Texture" });
        if (EditorGUI.EndChangeCheck())
        {
            _ed.RegisterPropertyChangeUndo("Pattern Source");
            SetKw(K_PATTERN_TEX, mode == 1);
            tex = mode == 1;
        }

        if (tex)
        {
            _ed.TexturePropertySingleLine(new GUIContent("Pattern Texture", "Escala de grises (R). Se muestrea 2 veces."), P("_PatternTex"));
            LinearWarning(P("_PatternTex").textureValue);
        }
        else
        {
            EditorGUILayout.LabelField("Sin texturas: 9 celdas por píxel, sin sin()/cos().", EditorStyles.miniLabel);
        }
        Prop("_PatternColor", "Pattern Color");
        Prop("_PatternIntensity", "Intensity");
        Prop("_PatternScale", "Scale", "Celdas por metro.");
        Prop("_PatternSpeed", "Speed");
        Prop("_PatternLineWidth", "Line Width");
        Prop("_PatternSoftness", "Line Softness", "0 = bordes duros (toon puro).");
        Prop("_PatternDeepFade", "Fade In Deep Water", "Atenúa el patrón en zonas profundas.");
    }

    void DrawFoam()
    {
        if (!On(K_DEPTH_OFF, true))
            EditorGUILayout.HelpBox("La espuma necesita Depth Effects activado.", MessageType.Info);
        Prop("_FoamColor", "Foam Color");
        Prop("_FoamWidth", "Width", "Ancho de la franja de espuma.");
        Prop("_FoamCutoff", "Cutoff", "Umbral toon: más alto = menos espuma.");
        Prop("_FoamSoftness", "Softness");
        Prop("_FoamNoise", "Noise", "Cuánto rompe el patrón el borde de la espuma.");
        Prop("_FoamSpeed", "Pulse Speed", "Velocidad del vaivén de la espuma.");
    }

    void DrawWaves()
    {
        Prop("_WaveAmplitude", "Height");
        Prop("_WaveFrequency", "Frequency");
        Prop("_WaveSpeed", "Speed");
        MaterialProperty dir = P("_WaveDirection");
        Vector4 v = dir.vectorValue;
        EditorGUI.BeginChangeCheck();
        Vector2 a = EditorGUILayout.Vector2Field("Direction A", new Vector2(v.x, v.y));
        Vector2 b = EditorGUILayout.Vector2Field("Direction B", new Vector2(v.z, v.w));
        if (EditorGUI.EndChangeCheck()) dir.vectorValue = new Vector4(a.x, a.y, b.x, b.y);
    }

    void DrawSurface()
    {
        // ---- Normal map ----
        EditorGUI.BeginChangeCheck();
        _ed.TexturePropertySingleLine(new GUIContent("Normal Map", "Se mezclan 2 capas con scroll opuesto."), P("_NormalMap"));
        if (EditorGUI.EndChangeCheck()) SetKw(K_NORMALMAP, P("_NormalMap").textureValue != null);
        NormalWarning(P("_NormalMap").textureValue);
        if (Kw(K_NORMALMAP))
        {
            Prop("_NormalStrength", "Strength");
            Prop("_NormalTiling", "Tiling");
            MaterialProperty sp = P("_NormalSpeed");
            Vector4 v = sp.vectorValue;
            EditorGUI.BeginChangeCheck();
            Vector2 a = EditorGUILayout.Vector2Field("Layer A Speed", new Vector2(v.x, v.y));
            Vector2 b = EditorGUILayout.Vector2Field("Layer B Speed", new Vector2(v.z, v.w));
            if (EditorGUI.EndChangeCheck()) sp.vectorValue = new Vector4(a.x, a.y, b.x, b.y);
        }

        EditorGUILayout.Space(6);

        // ---- Modo de mapas de superficie ----
        int cur = Kw(K_ARM) ? 1 : Kw(K_SEPARATE) ? 2 : 0;
        Rect rr = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 20));
        EditorGUI.BeginChangeCheck();
        int next = GUI.Toolbar(rr, cur, new[] { "No maps", "ARM packed", "Separate" });
        if (EditorGUI.EndChangeCheck())
        {
            _ed.RegisterPropertyChangeUndo("Surface Maps Mode");
            SetKw(K_ARM, next == 1);
            SetKw(K_SEPARATE, next == 2);
            cur = next;
        }

        if (cur == 0)
        {
            Prop("_Roughness", "Roughness", "Más bajo = brillo más pequeño y reflejos más nítidos.");
            Prop("_Metallic", "Metallic", "Tiñe el reflejo con el color del agua.");
            return;
        }

        if (cur == 1)
        {
            _ed.TexturePropertySingleLine(new GUIContent("ARM Map", "R = Ambient Occlusion, G = Roughness, B = Metallic"), P("_ARMMap"));
            LinearWarning(P("_ARMMap").textureValue);
            EditorGUILayout.LabelField("R = AO   G = Roughness   B = Metallic", EditorStyles.miniLabel);
        }
        else
        {
            _ed.TexturePropertySingleLine(new GUIContent("Occlusion"), P("_OcclusionMap"));
            LinearWarning(P("_OcclusionMap").textureValue);
            _ed.TexturePropertySingleLine(new GUIContent("Roughness"), P("_RoughnessMap"));
            LinearWarning(P("_RoughnessMap").textureValue);
            _ed.TexturePropertySingleLine(new GUIContent("Metallic"), P("_MetallicMap"));
            LinearWarning(P("_MetallicMap").textureValue);
        }

        Prop("_OcclusionStrength", "AO Strength");
        Prop("_SurfaceTiling", "Maps Tiling");

        MaterialProperty remap = P("_SurfRemap");
        Vector4 rv = remap.vectorValue;
        float rMin = rv.x, rMax = rv.y, mMin = rv.z, mMax = rv.w;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.MinMaxSlider(new GUIContent("Roughness Remap", "Valor mínimo y máximo al que se remapea el mapa."), ref rMin, ref rMax, 0f, 1f);
        EditorGUILayout.MinMaxSlider(new GUIContent("Metallic Remap"), ref mMin, ref mMax, 0f, 1f);
        if (EditorGUI.EndChangeCheck()) remap.vectorValue = new Vector4(rMin, rMax, mMin, mMax);
        EditorGUILayout.LabelField($"Roughness {rMin:0.00}–{rMax:0.00}    Metallic {mMin:0.00}–{mMax:0.00}", EditorStyles.miniLabel);
    }

    void DrawLighting()
    {
        Prop("_ShadowColor", "Shade Color", "Color de la zona en sombra del toon.");
        Prop("_ShadeThreshold", "Shade Threshold");
        Prop("_ShadeSoftness", "Shade Softness", "Bajo = bandas duras estilo cel-shading.");
        Prop("_LightInfluence", "Light Color Influence", "Cuánto afecta el color de la luz principal.");
        EditorGUILayout.Space(2);
        Prop("_FresnelColor", "Fresnel Color");
        Prop("_FresnelPower", "Fresnel Power");
        Prop("_FresnelStrength", "Fresnel Strength");
    }

    void DrawSpecular()
    {
        Prop("_SpecularColor", "Specular Color", "El alfa controla la intensidad.");
        Prop("_SpecThreshold", "Threshold", "Más alto = brillo más pequeño.");
        Prop("_SpecSoftness", "Softness");
    }

    void DrawReflections()
    {
        Prop("_ReflectionStrength", "Strength");
        Prop("_ReflectionPower", "Fresnel", "Más alto = reflejos solo en ángulos rasantes.");
        EditorGUILayout.LabelField("Roughness desenfoca el reflejo, Metallic lo tiñe.", EditorStyles.miniLabel);
    }

    void DrawRefraction()
    {
        Prop("_RefractionStrength", "Strength");
    }

    void DrawAdvanced()
    {
        _ed.RenderQueueField();
        Prop("_Cull", "Cull Mode");
    }

    // ---------------------------------------------------------------- avisos de importación
    void LinearWarning(Texture tex)
    {
        if (tex == null) return;
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (imp != null && imp.sRGBTexture)
        {
            EditorGUILayout.HelpBox("Esta textura contiene datos (no color): desactiva sRGB.", MessageType.Warning);
            if (GUILayout.Button("Fix: disable sRGB"))
            {
                imp.sRGBTexture = false;
                imp.SaveAndReimport();
            }
        }
    }

    void NormalWarning(Texture tex)
    {
        if (tex == null) return;
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            EditorGUILayout.HelpBox("La textura no está importada como Normal Map.", MessageType.Warning);
            if (GUILayout.Button("Fix: set as Normal Map"))
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.SaveAndReimport();
            }
        }
    }

    // ---------------------------------------------------------------- presets
    static void C(Material m, string n, float r, float g, float b, float a) => m.SetColor(n, new Color(r, g, b, a));
    static void F(Material m, string n, float v) => m.SetFloat(n, v);

    void ApplyPreset(int i)
    {
        _ed.RegisterPropertyChangeUndo("Water Preset");
        foreach (UnityEngine.Object t in _ed.targets)
        {
            var m = (Material)t;
            foreach (string k in AllKeywords) m.DisableKeyword(k);   // estado base: todo activo excepto normal/reflejos/refracción/mapas

            switch (i)
            {
                case 0: // Tropical (default) - coincide con la vista del juego
                    C(m, "_ShallowColor", 0.33f, 0.80f, 0.82f, 0.60f);
                    C(m, "_DeepColor", 0.07f, 0.52f, 0.63f, 0.95f);
                    C(m, "_PatternColor", 0.70f, 0.95f, 0.97f, 1f);
                    C(m, "_ShadowColor", 0.55f, 0.78f, 0.88f, 1f);
                    F(m, "_DepthDistance", 2f); F(m, "_PatternScale", 0.6f); F(m, "_PatternIntensity", 0.55f);
                    F(m, "_WaveAmplitude", 0.04f); F(m, "_FoamWidth", 0.5f);
                    break;
                case 1: // Deep Ocean
                    C(m, "_ShallowColor", 0.10f, 0.55f, 0.70f, 0.75f);
                    C(m, "_DeepColor", 0.01f, 0.12f, 0.30f, 1f);
                    C(m, "_PatternColor", 0.45f, 0.80f, 0.95f, 1f);
                    C(m, "_ShadowColor", 0.35f, 0.55f, 0.75f, 1f);
                    F(m, "_DepthDistance", 6f); F(m, "_PatternScale", 0.35f); F(m, "_PatternIntensity", 0.35f);
                    F(m, "_WaveAmplitude", 0.10f); F(m, "_WaveFrequency", 0.8f); F(m, "_FoamWidth", 0.8f);
                    m.EnableKeyword(K_REFLECT);
                    break;
                case 2: // Lagoon Pool
                    C(m, "_ShallowColor", 0.45f, 0.95f, 0.90f, 0.45f);
                    C(m, "_DeepColor", 0.10f, 0.65f, 0.75f, 0.80f);
                    C(m, "_PatternColor", 0.90f, 1f, 1f, 1f);
                    C(m, "_ShadowColor", 0.70f, 0.90f, 0.95f, 1f);
                    F(m, "_DepthDistance", 1.2f); F(m, "_PatternScale", 0.9f); F(m, "_PatternIntensity", 0.7f);
                    F(m, "_WaveAmplitude", 0.015f);
                    m.EnableKeyword(K_REFRACT);
                    break;
                case 3: // Mobile Lite
                    C(m, "_ShallowColor", 0.33f, 0.80f, 0.82f, 0.65f);
                    C(m, "_DeepColor", 0.07f, 0.52f, 0.63f, 0.95f);
                    F(m, "_WaveAmplitude", 0.03f);
                    m.EnableKeyword(K_SPEC_OFF);
                    break;
            }
            EditorUtility.SetDirty(m);
        }
    }
}
#endif
