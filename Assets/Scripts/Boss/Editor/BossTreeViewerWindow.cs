using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BossTreeViewerWindow : EditorWindow
{
    private BossController targetBoss;
    private int selectedPhaseIndex = 0;
    private bool followActivePhase = true;
    private bool showStats = false;
    private bool isPaused = false;

    private ObjectField bossField;
    private PopupField<string> phasePopup;
    private ToolbarToggle followPhaseToggle;
    private ToolbarToggle statsToggle;
    private ToolbarButton frameButton;
    private ToolbarButton copySummaryButton;

    private BossTreeCanvas canvas;
    private VisualElement sidebar;
    private ListView historyListView;
    private ScrollView detailScrollView;

    private int lastSeenTraceVersion = -1;
    private readonly List<BossDecisionRecord> displayHistory = new List<BossDecisionRecord>();
    private BossDecisionRecord selectedHistoryRecord;
    private int selectedNodeIndex = -1;
    private BossDecisionNode selectedNode;

    [MenuItem("Vallegris/Boss/Boss Tree Viewer")]
    public static void OpenWindow()
    {
        BossTreeViewerWindow wnd = GetWindow<BossTreeViewerWindow>();
        wnd.titleContent = new GUIContent("Boss Tree Viewer");
        wnd.minSize = new Vector2(800, 500);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            lastSeenTraceVersion = -1;
            selectedHistoryRecord = null;
            RefreshTargetBoss();
            RebuildCanvas();
        }
    }

    private void OnAfterAssemblyReload()
    {
        lastSeenTraceVersion = -1;
        RefreshTargetBoss();
        RebuildCanvas();
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.AddToClassList("boss-tree-viewer");

        // Cargar estilo USS mediante AssetDatabase.FindAssets
        string[] guids = AssetDatabase.FindAssets("BossTreeViewer t:StyleSheet");
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet != null) root.styleSheets.Add(sheet);
        }

        // 1. Barra de herramientas superior
        Toolbar toolbar = CreateToolbar();
        root.Add(toolbar);

        // 2. Contenedor de dos paneles
        TwoPaneSplitView splitView = new TwoPaneSplitView(0, 500, TwoPaneSplitViewOrientation.Horizontal);
        splitView.style.flexGrow = 1;

        // Izquierda: Lienzo
        canvas = new BossTreeCanvas();
        canvas.SetCallbacks(OnNodeSelected, OnBackgroundClicked);
        splitView.Add(canvas);

        // Derecha: Panel lateral (historial + detalles)
        sidebar = CreateSidebar();
        splitView.Add(sidebar);

        root.Add(splitView);

        // Auto-detectar jefe si está vacío
        RefreshTargetBoss();
        RebuildCanvas();

        // Polling a 10 Hz (100 ms)
        root.schedule.Execute(OnPollTick).Every(100);

        // Atajo de teclado: F para encuadrar
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.F)
            {
                canvas?.FrameAll();
                evt.StopPropagation();
            }
        });
    }

    private Toolbar CreateToolbar()
    {
        Toolbar toolbar = new Toolbar();
        toolbar.AddToClassList("boss-toolbar");

        // Selector de Boss
        bossField = new ObjectField("Jefe") { objectType = typeof(BossController) };
        bossField.style.width = 200;
        bossField.RegisterValueChangedCallback(evt =>
        {
            targetBoss = evt.newValue as BossController;
            UpdatePhaseOptions();
            RebuildCanvas();
        });
        toolbar.Add(bossField);

        // Selector de fase
        List<string> dummyChoices = new List<string> { "Fase 0" };
        phasePopup = new PopupField<string>("Fase", dummyChoices, 0);
        phasePopup.RegisterValueChangedCallback(evt =>
        {
            int idx = phasePopup.index;
            if (idx >= 0 && idx != selectedPhaseIndex)
            {
                selectedPhaseIndex = idx;
                RebuildCanvas();
            }
        });
        toolbar.Add(phasePopup);

        // Checkbox "Seguir fase activa"
        followPhaseToggle = new ToolbarToggle { text = "Seguir fase activa", value = followActivePhase };
        followPhaseToggle.RegisterValueChangedCallback(evt => followActivePhase = evt.newValue);
        toolbar.Add(followPhaseToggle);

        // Botón Ordenar/Encuadrar
        frameButton = new ToolbarButton(() => canvas?.FrameAll()) { text = "Encuadrar (F)" };
        toolbar.Add(frameButton);

        // Toggle Estadísticas
        statsToggle = new ToolbarToggle { text = "Estadísticas", value = showStats };
        statsToggle.RegisterValueChangedCallback(evt =>
        {
            showStats = evt.newValue;
            UpdateStatsView();
        });
        toolbar.Add(statsToggle);

        // Copiar Resumen
        copySummaryButton = new ToolbarButton(CopySummaryToClipboard) { text = "Copiar resumen" };
        toolbar.Add(copySummaryButton);

        return toolbar;
    }

    private VisualElement CreateSidebar()
    {
        VisualElement panel = new VisualElement();
        panel.AddToClassList("sidebar-panel");
        panel.style.width = 300;

        // Historial Header
        VisualElement histHeader = new VisualElement();
        histHeader.style.flexDirection = FlexDirection.Row;
        histHeader.style.justifyContent = Justify.SpaceBetween;
        histHeader.style.alignItems = Align.Center;

        Label histTitle = new Label("Historial de decisiones");
        histTitle.AddToClassList("sidebar-section-title");
        histHeader.Add(histTitle);

        Button clearBtn = new Button(() =>
        {
            displayHistory.Clear();
            historyListView?.Rebuild();
            selectedHistoryRecord = null;
            canvas?.ResetHighlights();
        }) { text = "Limpiar" };
        histHeader.Add(clearBtn);

        panel.Add(histHeader);

        // ListView de historial
        historyListView = new ListView(displayHistory, 24, MakeHistoryItem, BindHistoryItem);
        historyListView.selectionType = SelectionType.Single;
        historyListView.style.height = 180;
        historyListView.style.borderTopWidth = 1;
        historyListView.style.borderBottomWidth = 1;
        historyListView.style.borderLeftWidth = 1;
        historyListView.style.borderRightWidth = 1;
        Color borderColor = new Color(0.2f, 0.2f, 0.2f);
        historyListView.style.borderTopColor = borderColor;
        historyListView.style.borderBottomColor = borderColor;
        historyListView.style.borderLeftColor = borderColor;
        historyListView.style.borderRightColor = borderColor;
        historyListView.selectionChanged += OnHistorySelectionChanged;
        panel.Add(historyListView);

        // Detalle del nodo seleccionado
        Label detailTitle = new Label("Detalle del nodo");
        detailTitle.AddToClassList("sidebar-section-title");
        panel.Add(detailTitle);

        detailScrollView = new ScrollView();
        detailScrollView.style.flexGrow = 1;
        detailScrollView.style.borderTopWidth = 1;
        detailScrollView.style.borderBottomWidth = 1;
        detailScrollView.style.borderLeftWidth = 1;
        detailScrollView.style.borderRightWidth = 1;
        detailScrollView.style.borderTopColor = borderColor;
        detailScrollView.style.borderBottomColor = borderColor;
        detailScrollView.style.borderLeftColor = borderColor;
        detailScrollView.style.borderRightColor = borderColor;
        detailScrollView.style.paddingLeft = 4;
        detailScrollView.style.paddingRight = 4;
        panel.Add(detailScrollView);

        return panel;
    }

    private VisualElement MakeHistoryItem()
    {
        Label lbl = new Label();
        lbl.AddToClassList("history-item");
        return lbl;
    }

    private void BindHistoryItem(VisualElement element, int index)
    {
        if (index < 0 || index >= displayHistory.Count) return;
        BossDecisionRecord rec = displayHistory[index];
        Label lbl = (Label)element;
        string fbMark = rec.UsedFallback ? "[FB] " : "";
        lbl.text = $"{rec.Time:0.0}s | F{rec.PhaseIndex} {rec.DistanceBand} → {fbMark}{rec.ChosenAction}";
    }

    private void OnHistorySelectionChanged(IEnumerable<object> selection)
    {
        foreach (object obj in selection)
        {
            if (obj is BossDecisionRecord rec)
            {
                selectedHistoryRecord = rec;
                if (rec.PhaseIndex != selectedPhaseIndex)
                {
                    selectedPhaseIndex = rec.PhaseIndex;
                    UpdatePhaseOptions();
                    RebuildCanvas();
                }
                canvas?.HighlightDecision(rec, targetBoss);
                break;
            }
        }
    }

    private void RefreshTargetBoss()
    {
        if (targetBoss == null)
            targetBoss = FindAnyObjectByType<BossController>();

        if (bossField != null)
            bossField.value = targetBoss;

        UpdatePhaseOptions();
    }

    private void UpdatePhaseOptions()
    {
        if (targetBoss == null || targetBoss.phases == null || targetBoss.phases.Count == 0)
        {
            phasePopup?.SetEnabled(false);
            return;
        }

        phasePopup?.SetEnabled(true);
        List<string> choices = new List<string>();
        for (int i = 0; i < targetBoss.phases.Count; i++)
        {
            string pName = targetBoss.phases[i] != null ? targetBoss.phases[i].name : $"Fase {i}";
            choices.Add($"{i} · {pName}");
        }

        if (selectedPhaseIndex >= choices.Count)
            selectedPhaseIndex = 0;

        phasePopup.choices = choices;
        phasePopup.index = selectedPhaseIndex;
    }

    private void RebuildCanvas()
    {
        if (canvas == null) return;

        if (targetBoss == null || targetBoss.phases == null || selectedPhaseIndex < 0 || selectedPhaseIndex >= targetBoss.phases.Count)
        {
            canvas.BuildTree(null);
            UpdateDetailPanel();
            return;
        }

        BossPhase currentPhase = targetBoss.phases[selectedPhaseIndex];
        if (currentPhase == null)
        {
            canvas.BuildTree(null);
            UpdateDetailPanel();
            return;
        }

        if (currentPhase.decisionTree != null)
        {
            canvas.BuildTree(currentPhase.decisionTree);
        }
        else
        {
            canvas.ShowPatternChain(currentPhase.pattern);
        }

        if (selectedHistoryRecord != null && selectedHistoryRecord.PhaseIndex == selectedPhaseIndex)
            canvas.HighlightDecision(selectedHistoryRecord, targetBoss);

        UpdateDetailPanel();
        UpdateStatsView();
    }

    private void OnPollTick()
    {
        if (targetBoss == null)
        {
            if (Application.isPlaying)
                RefreshTargetBoss();
            return;
        }

        // Seguir fase activa si está en play
        if (Application.isPlaying && followActivePhase && targetBoss.CurrentPhaseIndex >= 0)
        {
            if (targetBoss.CurrentPhaseIndex != selectedPhaseIndex)
            {
                selectedPhaseIndex = targetBoss.CurrentPhaseIndex;
                UpdatePhaseOptions();
                RebuildCanvas();
            }
        }

        // Cooldowns en vivo
        if (Application.isPlaying)
        {
            canvas?.UpdateLiveCooldowns(targetBoss, selectedPhaseIndex);
        }

        // Consultar Trace
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (targetBoss.Trace != null && targetBoss.Trace.Version != lastSeenTraceVersion)
        {
            lastSeenTraceVersion = targetBoss.Trace.Version;

            displayHistory.Clear();
            for (int i = targetBoss.Trace.History.Count - 1; i >= 0; i--)
            {
                displayHistory.Add(targetBoss.Trace.History[i]);
            }
            historyListView?.Rebuild();

            if (!isPaused && targetBoss.Trace.Last != null)
            {
                BossDecisionRecord last = targetBoss.Trace.Last;
                if (last.PhaseIndex == selectedPhaseIndex)
                {
                    canvas?.HighlightDecision(last, targetBoss);
                }
            }

            if (showStats)
                UpdateStatsView();
        }
#endif
    }

    private void OnNodeSelected(int index, BossDecisionNode node)
    {
        selectedNodeIndex = index;
        selectedNode = node;
        UpdateDetailPanel();
    }

    private void OnBackgroundClicked()
    {
        selectedNodeIndex = -1;
        selectedNode = null;
        UpdateDetailPanel();
    }

    private void UpdateDetailPanel()
    {
        detailScrollView.Clear();

        if (selectedNode == null)
        {
            detailScrollView.Add(new Label("(Ningún nodo seleccionado)") { style = { color = new Color(0.6f, 0.6f, 0.6f) } });
            return;
        }

        if (selectedNode is BossDecisionBranch branch)
        {
            detailScrollView.Add(new Label($"Rama #{selectedNodeIndex}") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 } });
            detailScrollView.Add(new Label($"Condición: {branch.condition}"));
            switch (branch.condition)
            {
                case BossDecisionCondition.DistanceBand:
                    detailScrollView.Add(new Label($"Banda: {branch.distanceBand}"));
                    break;
                case BossDecisionCondition.HealthBelow:
                    detailScrollView.Add(new Label($"Umbral: {branch.healthThreshold}%"));
                    break;
                case BossDecisionCondition.ActionReady:
                    detailScrollView.Add(new Label($"Acción: {(branch.action != null ? branch.action.GetType().Name : "Nula")}"));
                    break;
                case BossDecisionCondition.PlayerIdleFor:
                    detailScrollView.Add(new Label($"Segundos: {branch.idleSeconds}s"));
                    break;
            }
        }
        else if (selectedNode is BossDecisionLeaf leaf)
        {
            detailScrollView.Add(new Label($"Hoja #{selectedNodeIndex}") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 } });
            detailScrollView.Add(new Label($"Entradas: {(leaf.entries != null ? leaf.entries.Count : 0)}"));
            if (leaf.fallbackAction != null)
                detailScrollView.Add(new Label($"Respaldo acción: {leaf.fallbackAction.GetType().Name}"));
            if (leaf.fallbackPattern != null)
                detailScrollView.Add(new Label($"Respaldo patrón: {leaf.fallbackPattern.name}"));

            if (leaf.entries != null)
            {
                for (int i = 0; i < leaf.entries.Count; i++)
                {
                    BossWeightedEntry e = leaf.entries[i];
                    string title = e.action != null ? e.action.GetType().Name : (e.pattern != null ? e.pattern.name : "Combo");
                    detailScrollView.Add(new Label($"[{i}] {title} | Peso: {e.weight} | Cooldown: {e.cooldown}s | MaxCons: {e.maxConsecutives}"));
                }
            }
        }
    }

    private void UpdateStatsView()
    {
        if (!showStats)
        {
            canvas?.ApplyStats(null, null);
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (targetBoss == null || targetBoss.Trace == null) return;

        Dictionary<int, int> leafCounts = new Dictionary<int, int>();
        Dictionary<int, Dictionary<int, int>> entryCounts = new Dictionary<int, Dictionary<int, int>>();

        for (int i = 0; i < targetBoss.Trace.History.Count; i++)
        {
            BossDecisionRecord r = targetBoss.Trace.History[i];
            if (r.PhaseIndex != selectedPhaseIndex) continue;

            if (!leafCounts.ContainsKey(r.LeafNodeIndex))
                leafCounts[r.LeafNodeIndex] = 0;
            leafCounts[r.LeafNodeIndex]++;

            if (!entryCounts.ContainsKey(r.LeafNodeIndex))
                entryCounts[r.LeafNodeIndex] = new Dictionary<int, int>();

            if (r.ChosenEntryIndex >= 0)
            {
                if (!entryCounts[r.LeafNodeIndex].ContainsKey(r.ChosenEntryIndex))
                    entryCounts[r.LeafNodeIndex][r.ChosenEntryIndex] = 0;
                entryCounts[r.LeafNodeIndex][r.ChosenEntryIndex]++;
            }
        }

        canvas?.ApplyStats(leafCounts, entryCounts);
#endif
    }

    private void CopySummaryToClipboard()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (targetBoss == null || targetBoss.Trace == null)
        {
            GUIUtility.systemCopyBuffer = "No hay datos de trace disponibles.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[BOSSDBG_SUMMARY] Total Decisiones: {targetBoss.Trace.History.Count}");

        Dictionary<string, int> chosenCounts = new Dictionary<string, int>();
        Dictionary<int, int> phaseDecisions = new Dictionary<int, int>();

        for (int i = 0; i < targetBoss.Trace.History.Count; i++)
        {
            BossDecisionRecord r = targetBoss.Trace.History[i];
            if (!chosenCounts.ContainsKey(r.ChosenAction)) chosenCounts[r.ChosenAction] = 0;
            chosenCounts[r.ChosenAction]++;

            if (!phaseDecisions.ContainsKey(r.PhaseIndex)) phaseDecisions[r.PhaseIndex] = 0;
            phaseDecisions[r.PhaseIndex]++;
        }

        foreach (var kvp in phaseDecisions)
            sb.AppendLine($"  Fase {kvp.Key}: {kvp.Value} decisiones");

        foreach (var kvp in chosenCounts)
            sb.AppendLine($"  Acción '{kvp.Key}': elegida {kvp.Value} veces");

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[BossTreeViewer] Resumen copiado al portapapeles:\n" + GUIUtility.systemCopyBuffer);
#endif
    }
}
