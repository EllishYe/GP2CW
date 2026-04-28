using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RuntimeCityGenerationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CityGenerationController controller;
    [SerializeField] private Canvas targetCanvas;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(360f, 760f);
    [SerializeField] private Vector2 panelOffset = new Vector2(16f, -16f);
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private TMP_FontAsset uiFont;
    [SerializeField] private bool buildOnStart = true;

    [Header("Runtime Presets")]
    [SerializeField] private RuntimeCityPresetLibrary presetLibrary;
    [SerializeField] private List<RuntimeCityPreset> presets = new List<RuntimeCityPreset>();

    private readonly List<RuntimeControl> controls = new List<RuntimeControl>();
    private readonly Dictionary<UrbanBlockType, BlockTypeProfileControls> blockTypeControls =
        new Dictionary<UrbanBlockType, BlockTypeProfileControls>();

    private TMP_Dropdown presetDropdown;
    private TMP_InputField globalSeedInput;
    private TMP_Dropdown assignmentModeDropdown;
    private GameObject randomWeightedPanel;
    private GameObject distanceToCenterPanel;
    private RectTransform panelRoot;

    private void Reset()
    {
        controller = FindFirstObjectByType<CityGenerationController>();
    }

    private void Start()
    {
        if (!buildOnStart)
        {
            return;
        }

        BuildUI();
    }

    public void BuildUI()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<CityGenerationController>();
        }

        if (controller == null)
        {
            Debug.LogWarning("RuntimeCityGenerationUI: CityGenerationController is not assigned.");
            return;
        }

        controller.FindStageGenerators();
        if (presetLibrary == null)
        {
            presetLibrary = controller.runtimePresetLibrary;
        }
        EnsureDefaultPresets();
        EnsureCanvas();
        EnsureEventSystem();
        ClearBuiltUI();

        panelRoot = CreatePanel(targetCanvas.transform);
        RectTransform content = CreateScrollContent(panelRoot);

        AddTitle(content, "City Generator");
        BuildPresetSection(content);
        BuildActionSection(content);
        BuildRoadSection(content);
        BuildWalkableSection(content);
        BuildBlockSection(content);
        BuildLotSection(content);

        LoadGeneratorValuesToControls();
        UpdateAssignmentModePanels();
    }

    private void BuildPresetSection(RectTransform parent)
    {
        RectTransform section = AddFoldout(parent, "Preset", true);

        presetDropdown = AddDropdown(section, "Preset", GetPresetNames(), 0);
        AddButton(section, "Apply Preset", ApplySelectedPreset);
        AddButton(section, "Randomize Seed", RandomizeSeed);
        globalSeedInput = AddInput(section, "Seed", "12345");
        globalSeedInput.onEndEdit.AddListener(_ => SyncGlobalSeedInput());
    }

    private void BuildActionSection(RectTransform parent)
    {
        RectTransform section = AddFoldout(parent, "Generate", true);

        AddButton(section, "Generate All", () => RunGeneration(controller.GenerateAll));
        AddButton(section, "Clear All", controller.ClearAll);
        AddButton(section, "Generate Roads", () => RunGeneration(controller.GenerateRoads));
        AddButton(section, "Generate Walkable", () => RunGeneration(controller.GenerateWalkable));
        AddButton(section, "Generate Blocks", () => RunGeneration(controller.GenerateBlocks));
        AddButton(section, "Generate Lots & Buildings", () => RunGeneration(controller.GenerateLots));
    }

    private void BuildRoadSection(RectTransform parent)
    {
        RectTransform section = AddFoldout(parent, "Road Network", false);

        AddInt(section, "Map Size", 50, 2500, () => controller.roadNetworkGenerator.mapSize,
            value => controller.roadNetworkGenerator.mapSize = value);
        AddInt(section, "Random Seed", 0, 999999, () => controller.roadNetworkGenerator.randomSeed,
            value => controller.roadNetworkGenerator.randomSeed = value);
        AddFloat(section, "Road Segment Length", 10f, 300f, () => controller.roadNetworkGenerator.roadSegmentLength,
            value => controller.roadNetworkGenerator.roadSegmentLength = value);
        AddInt(section, "Major Road Count", 0, 600, () => controller.roadNetworkGenerator.majorRoadCount,
            value => controller.roadNetworkGenerator.majorRoadCount = value);
        AddInt(section, "Minor Road Count", 0, 1200, () => controller.roadNetworkGenerator.minorRoadCount,
            value => controller.roadNetworkGenerator.minorRoadCount = value);
        AddFloat(section, "Branch Probability", 0f, 1f, () => controller.roadNetworkGenerator.branchProbability,
            value => controller.roadNetworkGenerator.branchProbability = value);
        AddFloat(section, "Deletion Probability", 0f, 1f, () => controller.roadNetworkGenerator.deletionProbability,
            value => controller.roadNetworkGenerator.deletionProbability = value);
        AddFloat(section, "Lane Width", 1f, 8f, () => controller.roadNetworkGenerator.laneWidth,
            value => controller.roadNetworkGenerator.laneWidth = value);
    }

    private void BuildWalkableSection(RectTransform parent)
    {
        RectTransform section = AddFoldout(parent, "Walkable / Crosswalk", false);

        AddBool(section, "Generate Walkable Mesh", () => controller.walkableAreaGenerator.generateWalkableMesh,
            value => controller.walkableAreaGenerator.generateWalkableMesh = value);
        AddBool(section, "Generate Crosswalk Mesh", () => controller.crosswalkGenerator.generateCrosswalkMesh,
            value => controller.crosswalkGenerator.generateCrosswalkMesh = value);
        AddFloat(section, "Vehicle Stop Distance", 0f, 40f, () => controller.crosswalkGenerator.vehicleStopDistance,
            value => controller.crosswalkGenerator.vehicleStopDistance = value);
    }

    private void BuildBlockSection(RectTransform parent)
    {
        RectTransform section = AddFoldout(parent, "Blocks / Land Use", false);

        AddFloat(section, "Park Probability", 0f, 1f, () => controller.blockAreaGenerator.parkProbability,
            value => controller.blockAreaGenerator.parkProbability = value);
        AddFloat(section, "Park Min Area", 0f, 50000f, () => controller.blockAreaGenerator.parkMinArea,
            value => controller.blockAreaGenerator.parkMinArea = value);
        AddFloat(section, "Park Irregularity", 0f, 1f, () => controller.blockAreaGenerator.parkIrregularityThreshold,
            value => controller.blockAreaGenerator.parkIrregularityThreshold = value);
        AddInt(section, "Random Seed", 0, 999999, () => controller.blockAreaGenerator.randomSeed,
            value => controller.blockAreaGenerator.randomSeed = value);
    }

    private void BuildLotSection(RectTransform parent)
    {
        RectTransform section = AddFoldout(parent, "Lots & Buildings", false);

        assignmentModeDropdown = AddEnum(section, "Assignment Mode", typeof(BlockTypeAssignmentMode),
            () => (int)controller.lotAreaGenerator.assignmentMode,
            value =>
            {
                controller.lotAreaGenerator.assignmentMode = (BlockTypeAssignmentMode)value;
                UpdateAssignmentModePanels();
            });

        AddEnum(section, "Default Building Type", typeof(UrbanBlockType),
            () => (int)controller.lotAreaGenerator.defaultBuildingType,
            value => controller.lotAreaGenerator.defaultBuildingType = (UrbanBlockType)value);

        randomWeightedPanel = AddSubPanel(section);
        AddFloat(randomWeightedPanel.transform as RectTransform, "Residential Weight", 0f, 1f,
            () => controller.lotAreaGenerator.residentialWeight,
            value => controller.lotAreaGenerator.residentialWeight = value);
        AddFloat(randomWeightedPanel.transform as RectTransform, "Commercial Weight", 0f, 1f,
            () => controller.lotAreaGenerator.commercialWeight,
            value => controller.lotAreaGenerator.commercialWeight = value);
        AddFloat(randomWeightedPanel.transform as RectTransform, "Industrial Weight", 0f, 1f,
            () => controller.lotAreaGenerator.industrialWeight,
            value => controller.lotAreaGenerator.industrialWeight = value);

        distanceToCenterPanel = AddSubPanel(section);
        AddFloat(distanceToCenterPanel.transform as RectTransform, "Commercial Inner Radius", 0f, 1f,
            () => controller.lotAreaGenerator.commercialInnerNormalizedRadius,
            value => controller.lotAreaGenerator.commercialInnerNormalizedRadius = value);
        AddFloat(distanceToCenterPanel.transform as RectTransform, "Residential Middle Radius", 0f, 1f,
            () => controller.lotAreaGenerator.residentialMiddleNormalizedRadius,
            value => controller.lotAreaGenerator.residentialMiddleNormalizedRadius = value);

        AddBlockTypeControls(section, UrbanBlockType.Residential, "Residential");
        AddBlockTypeControls(section, UrbanBlockType.Commercial, "Commercial");
        AddBlockTypeControls(section, UrbanBlockType.Industrial, "Industrial");
    }

    private void AddBlockTypeControls(RectTransform parent, UrbanBlockType type, string label)
    {
        AddLabel(parent, label);

        BlockTypeProfileControls profileControls = new BlockTypeProfileControls();
        blockTypeControls[type] = profileControls;

        profileControls.minHeight = AddFloat(parent, "Min Height", 0f, 120f,
            () => GetProfile(type).minHeight,
            value => GetProfile(type).minHeight = value);
        profileControls.maxHeight = AddFloat(parent, "Max Height", 0f, 160f,
            () => GetProfile(type).maxHeight,
            value => GetProfile(type).maxHeight = value);
        profileControls.buildingSetback = AddFloat(parent, "Building Setback", 0f, 20f,
            () => GetProfile(type).buildingSetback,
            value => GetProfile(type).buildingSetback = value);
    }

    private void RunGeneration(Action generateAction)
    {
        ApplyControlsToGenerators();
        generateAction?.Invoke();
    }

    private void ApplySelectedPreset()
    {
        if (presetDropdown == null || presetDropdown.value < 0 || presetDropdown.value >= presets.Count)
        {
            return;
        }

        ApplyPreset(presets[presetDropdown.value]);
        LoadGeneratorValuesToControls();
        UpdateAssignmentModePanels();
    }

    private void ApplyPreset(RuntimeCityPreset preset)
    {
        if (preset == null)
        {
            return;
        }

        EnsureGeneratorReferences();

        RoadNetworkGenerator roads = controller.roadNetworkGenerator;
        roads.mapSize = preset.mapSize;
        roads.randomSeed = preset.seed;
        roads.roadSegmentLength = preset.roadSegmentLength;
        roads.majorRoadCount = preset.majorRoadCount;
        roads.minorRoadCount = preset.minorRoadCount;
        roads.branchProbability = preset.branchProbability;
        roads.deletionProbability = preset.deletionProbability;
        roads.laneWidth = preset.laneWidth;
        roads.SanitizeSettings();

        BlockAreaGenerator blocks = controller.blockAreaGenerator;
        blocks.randomSeed = preset.seed;
        blocks.parkProbability = preset.parkProbability;
        blocks.parkMinArea = preset.parkMinArea;
        blocks.parkIrregularityThreshold = preset.parkIrregularityThreshold;

        LotAreaGenerator lots = controller.lotAreaGenerator;
        lots.randomSeed = preset.seed;
        lots.assignmentMode = preset.assignmentMode;
        lots.defaultBuildingType = preset.defaultBuildingType;
        lots.residentialWeight = preset.residentialWeight;
        lots.commercialWeight = preset.commercialWeight;
        lots.industrialWeight = preset.industrialWeight;
        lots.commercialInnerNormalizedRadius = preset.commercialInnerNormalizedRadius;
        lots.residentialMiddleNormalizedRadius = preset.residentialMiddleNormalizedRadius;

        ApplyProfilePreset(UrbanBlockType.Residential, preset.residential);
        ApplyProfilePreset(UrbanBlockType.Commercial, preset.commercial);
        ApplyProfilePreset(UrbanBlockType.Industrial, preset.industrial);
    }

    private void ApplyControlsToGenerators()
    {
        EnsureGeneratorReferences();

        for (int i = 0; i < controls.Count; i++)
        {
            controls[i].Apply();
        }

        controller.roadNetworkGenerator.SanitizeSettings();
        SanitizeLotProfiles();
    }

    private void LoadGeneratorValuesToControls()
    {
        EnsureGeneratorReferences();

        if (globalSeedInput != null)
        {
            globalSeedInput.text = controller.roadNetworkGenerator.randomSeed.ToString();
        }

        for (int i = 0; i < controls.Count; i++)
        {
            controls[i].Load();
        }
    }

    private void RandomizeSeed()
    {
        int seed = UnityEngine.Random.Range(0, 999999);
        if (globalSeedInput != null)
        {
            globalSeedInput.text = seed.ToString();
        }

        SyncSeed(seed);
        LoadGeneratorValuesToControls();
    }

    private void SyncGlobalSeedInput()
    {
        if (globalSeedInput == null || !int.TryParse(globalSeedInput.text, out int seed))
        {
            return;
        }

        SyncSeed(seed);
        LoadGeneratorValuesToControls();
    }

    private void SyncSeed(int seed)
    {
        if (controller.roadNetworkGenerator != null)
        {
            controller.roadNetworkGenerator.randomSeed = seed;
        }

        if (controller.blockAreaGenerator != null)
        {
            controller.blockAreaGenerator.randomSeed = seed;
        }

        if (controller.lotAreaGenerator != null)
        {
            controller.lotAreaGenerator.randomSeed = seed;
        }
    }

    private void UpdateAssignmentModePanels()
    {
        BlockTypeAssignmentMode mode = controller != null && controller.lotAreaGenerator != null
            ? controller.lotAreaGenerator.assignmentMode
            : BlockTypeAssignmentMode.Default;

        if (assignmentModeDropdown != null)
        {
            assignmentModeDropdown.value = (int)mode;
            assignmentModeDropdown.RefreshShownValue();
        }

        if (randomWeightedPanel != null)
        {
            randomWeightedPanel.SetActive(mode == BlockTypeAssignmentMode.RandomWeighted);
        }

        if (distanceToCenterPanel != null)
        {
            distanceToCenterPanel.SetActive(mode == BlockTypeAssignmentMode.DistanceToCenter);
        }
    }

    private void EnsureGeneratorReferences()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<CityGenerationController>();
        }

        if (controller == null)
        {
            return;
        }

        controller.FindStageGenerators();

        if (controller.walkableAreaGenerator == null)
        {
            controller.walkableAreaGenerator = controller.gameObject.AddComponent<WalkableAreaGenerator>();
        }

        if (controller.crosswalkGenerator == null)
        {
            controller.crosswalkGenerator = controller.gameObject.AddComponent<CrosswalkGenerator>();
        }

        if (controller.blockAreaGenerator == null)
        {
            controller.blockAreaGenerator = controller.gameObject.AddComponent<BlockAreaGenerator>();
        }

        if (controller.lotAreaGenerator == null)
        {
            controller.lotAreaGenerator = controller.gameObject.AddComponent<LotAreaGenerator>();
        }

        if (controller.lotBuildingGenerator == null)
        {
            controller.lotBuildingGenerator = controller.gameObject.AddComponent<LotBuildingGenerator>();
        }
    }

    private UrbanBlockTypeProfile GetProfile(UrbanBlockType type)
    {
        EnsureGeneratorReferences();
        return controller.lotAreaGenerator.GetProfile(type);
    }

    private void ApplyProfilePreset(UrbanBlockType type, RuntimeBlockTypePreset preset)
    {
        UrbanBlockTypeProfile profile = GetProfile(type);
        profile.minHeight = preset.minHeight;
        profile.maxHeight = preset.maxHeight;
        profile.buildingSetback = preset.buildingSetback;
        profile.Sanitize();
    }

    private void SanitizeLotProfiles()
    {
        GetProfile(UrbanBlockType.Residential).Sanitize();
        GetProfile(UrbanBlockType.Commercial).Sanitize();
        GetProfile(UrbanBlockType.Industrial).Sanitize();
    }

    private void EnsureDefaultPresets()
    {
        if (presetLibrary != null && presetLibrary.presets != null && presetLibrary.presets.Count > 0)
        {
            presets = new List<RuntimeCityPreset>(presetLibrary.presets);
            return;
        }

        if (presets.Count > 0)
        {
            return;
        }

        presets.Add(RuntimeCityPreset.CreateDefault());
        presets.Add(RuntimeCityPreset.CreateDenseCity());
        presets.Add(RuntimeCityPreset.CreateSparseGrid());
        presets.Add(RuntimeCityPreset.CreateOrganic());
        presets.Add(RuntimeCityPreset.CreateIndustrial());
    }

    private List<string> GetPresetNames()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < presets.Count; i++)
        {
            names.Add(presets[i] != null ? presets[i].presetName : $"Preset {i + 1}");
        }

        return names;
    }

    private void EnsureCanvas()
    {
        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("Runtime City Generation Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.sortingOrder = 1000;
        targetCanvas.pixelPerfect = false;

        CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void ClearBuiltUI()
    {
        if (panelRoot == null)
        {
            return;
        }

        Destroy(panelRoot.gameObject);
        panelRoot = null;
        controls.Clear();
        blockTypeControls.Clear();
    }

    private RectTransform CreatePanel(Transform parent)
    {
        GameObject panel = CreateUIObject("Runtime City Generation Panel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = panelOffset;
        rect.sizeDelta = panelSize;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.07f, 0.08f, 0.09f, 0.88f);

        return rect;
    }

    private RectTransform CreateScrollContent(RectTransform parent)
    {
        GameObject viewport = CreateUIObject("Viewport", parent);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = parent.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;

        return contentRect;
    }

    private RectTransform AddFoldout(RectTransform parent, string title, bool expanded)
    {
        GameObject root = CreateUIObject($"{title} Foldout", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = 4f;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Toggle toggle = AddToggle(rootRect, title, expanded);

        GameObject body = CreateUIObject($"{title} Body", root.transform);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(12, 0, 0, 0);
        bodyLayout.spacing = 4f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;
        body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        body.SetActive(expanded);

        toggle.onValueChanged.AddListener(body.SetActive);
        return bodyRect;
    }

    private GameObject AddSubPanel(RectTransform parent)
    {
        GameObject panel = CreateUIObject("Conditional Controls", parent);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.12f, 0.14f, 0.55f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return panel;
    }

    private void AddTitle(RectTransform parent, string text)
    {
        TMP_Text label = AddText(parent, text, 22, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
    }

    private void AddLabel(RectTransform parent, string text)
    {
        TMP_Text label = AddText(parent, text, 14, FontStyles.Bold);
        label.color = new Color(0.78f, 0.84f, 0.9f, 1f);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
    }

    private void AddButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUIObject(label, parent);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 30f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.26f, 0.34f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TMP_Text text = AddText(buttonObject.transform as RectTransform, label, 14, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform);
    }

    private Toggle AddBool(RectTransform parent, string label, Func<bool> getter, Action<bool> setter)
    {
        Toggle toggle = AddToggle(parent, label, getter());
        RuntimeControl control = new RuntimeControl(
            () => toggle.isOn = getter(),
            () => setter(toggle.isOn));
        controls.Add(control);
        toggle.onValueChanged.AddListener(setter.Invoke);
        return toggle;
    }

    private RuntimeControl AddFloat(RectTransform parent, string label, float min, float max, Func<float> getter, Action<float> setter)
    {
        GameObject row = CreateRow(parent, label);
        Slider slider = AddSlider(row.transform as RectTransform, min, max, false);
        TMP_InputField input = AddInputField(row.transform as RectTransform);

        RuntimeControl control = new RuntimeControl(
            () =>
            {
                float value = Mathf.Clamp(getter(), min, max);
                slider.SetValueWithoutNotify(value);
                input.SetTextWithoutNotify(FormatFloat(value));
            },
            () =>
            {
                if (float.TryParse(input.text, out float value))
                {
                    value = Mathf.Clamp(value, min, max);
                    setter(value);
                    slider.SetValueWithoutNotify(value);
                    input.SetTextWithoutNotify(FormatFloat(value));
                }
            });

        slider.onValueChanged.AddListener(value =>
        {
            input.SetTextWithoutNotify(FormatFloat(value));
            setter(value);
        });
        input.onEndEdit.AddListener(_ => control.Apply());
        controls.Add(control);
        return control;
    }

    private RuntimeControl AddInt(RectTransform parent, string label, int min, int max, Func<int> getter, Action<int> setter)
    {
        GameObject row = CreateRow(parent, label);
        Slider slider = AddSlider(row.transform as RectTransform, min, max, true);
        TMP_InputField input = AddInputField(row.transform as RectTransform);

        RuntimeControl control = new RuntimeControl(
            () =>
            {
                int value = Mathf.Clamp(getter(), min, max);
                slider.SetValueWithoutNotify(value);
                input.SetTextWithoutNotify(value.ToString());
            },
            () =>
            {
                if (int.TryParse(input.text, out int value))
                {
                    value = Mathf.Clamp(value, min, max);
                    setter(value);
                    slider.SetValueWithoutNotify(value);
                    input.SetTextWithoutNotify(value.ToString());
                }
            });

        slider.onValueChanged.AddListener(value =>
        {
            int intValue = Mathf.RoundToInt(value);
            input.SetTextWithoutNotify(intValue.ToString());
            setter(intValue);
        });
        input.onEndEdit.AddListener(_ => control.Apply());
        controls.Add(control);
        return control;
    }

    private TMP_Dropdown AddEnum(RectTransform parent, string label, Type enumType, Func<int> getter, Action<int> setter)
    {
        TMP_Dropdown dropdown = AddDropdown(parent, label, new List<string>(Enum.GetNames(enumType)), getter());
        RuntimeControl control = new RuntimeControl(
            () =>
            {
                dropdown.SetValueWithoutNotify(getter());
                dropdown.RefreshShownValue();
            },
            () => setter(dropdown.value));

        dropdown.onValueChanged.AddListener(value =>
        {
            setter(value);
            control.Load();
        });
        controls.Add(control);
        return dropdown;
    }

    private TMP_InputField AddInput(RectTransform parent, string label, string defaultValue)
    {
        GameObject row = CreateRow(parent, label);
        AddSpacer(row.transform as RectTransform, 132f);
        TMP_InputField input = AddInputField(row.transform as RectTransform);
        input.text = defaultValue;
        return input;
    }

    private TMP_Dropdown AddDropdown(RectTransform parent, string label, List<string> options, int selectedIndex)
    {
        GameObject row = CreateRow(parent, label);
        TMP_Dropdown dropdown = row.AddComponent<TMP_Dropdown>();
        Image image = row.GetComponent<Image>();
        if (image == null)
        {
            image = row.AddComponent<Image>();
        }
        image.color = new Color(0.12f, 0.14f, 0.16f, 1f);

        GameObject labelObject = CreateUIObject("Dropdown Label", row.transform);
        TMP_Text labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.font = ResolveFont();
        labelText.fontSize = 13;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = Color.white;
        Stretch(labelText.rectTransform);
        labelText.rectTransform.offsetMin = new Vector2(110f, 0f);
        labelText.rectTransform.offsetMax = new Vector2(-8f, 0f);

        dropdown.captionText = labelText;
        dropdown.template = CreateDropdownTemplate(row.transform, out TMP_Text itemText);
        dropdown.itemText = itemText;
        dropdown.options.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(options[i]));
        }

        dropdown.value = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1));
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent, out TMP_Text itemText)
    {
        GameObject template = CreateUIObject("Template", parent);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, -30f);
        templateRect.sizeDelta = new Vector2(0f, 160f);
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.08f, 0.09f, 0.1f, 0.98f);
        template.AddComponent<ScrollRect>().horizontal = false;

        GameObject viewport = CreateUIObject("Viewport", template.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject item = CreateUIObject("Item", content.transform);
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 26f;
        Toggle itemToggle = item.AddComponent<Toggle>();
        Image itemImage = item.AddComponent<Image>();
        itemImage.color = new Color(0.12f, 0.14f, 0.16f, 1f);
        itemToggle.targetGraphic = itemImage;

        itemText = AddText(item.transform as RectTransform, "Option", 13, FontStyles.Normal);
        itemText.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(itemText.rectTransform);
        itemText.rectTransform.offsetMin = new Vector2(8f, 0f);
        itemText.rectTransform.offsetMax = new Vector2(-8f, 0f);

        ScrollRect scrollRect = template.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        template.SetActive(false);
        return templateRect;
    }

    private Toggle AddToggle(RectTransform parent, string label, bool value)
    {
        GameObject row = CreateUIObject(label, parent);
        LayoutElement layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 24f;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        Toggle toggle = row.AddComponent<Toggle>();
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(0.11f, 0.13f, 0.15f, 0.85f);
        GameObject checkObject = CreateUIObject("Checkmark", row.transform);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.sizeDelta = new Vector2(18f, 18f);
        Image checkImage = checkObject.AddComponent<Image>();
        checkImage.color = value ? new Color(0.36f, 0.68f, 0.92f, 1f) : new Color(0.16f, 0.18f, 0.2f, 1f);
        toggle.targetGraphic = checkImage;
        toggle.graphic = checkImage;
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(isOn =>
            checkImage.color = isOn ? new Color(0.36f, 0.68f, 0.92f, 1f) : new Color(0.16f, 0.18f, 0.2f, 1f));

        TMP_Text text = AddText(row.transform as RectTransform, label, 14, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return toggle;
    }

    private GameObject CreateRow(RectTransform parent, string label)
    {
        GameObject row = CreateUIObject(label, parent);
        LayoutElement layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 28f;
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(0.1f, 0.12f, 0.14f, 0.55f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 2, 2);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        TMP_Text text = AddText(row.transform as RectTransform, label, 13, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 126f;
        return row;
    }

    private Slider AddSlider(RectTransform parent, float min, float max, bool wholeNumbers)
    {
        GameObject sliderObject = CreateUIObject("Slider", parent);
        LayoutElement layout = sliderObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 120f;
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;

        GameObject background = CreateUIObject("Background", sliderObject.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.22f, 1f);
        Stretch(background.GetComponent<RectTransform>());

        GameObject fill = CreateUIObject("Fill", sliderObject.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.36f, 0.68f, 0.92f, 1f);
        Stretch(fill.GetComponent<RectTransform>());

        slider.targetGraphic = fillImage;
        slider.fillRect = fill.GetComponent<RectTransform>();
        return slider;
    }

    private TMP_InputField AddInputField(RectTransform parent)
    {
        GameObject inputObject = CreateUIObject("Input", parent);
        LayoutElement layout = inputObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 76f;
        Image image = inputObject.AddComponent<Image>();
        image.color = new Color(0.1f, 0.11f, 0.12f, 1f);

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        TMP_Text text = AddText(inputObject.transform as RectTransform, "", 13, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(6f, 0f);
        text.rectTransform.offsetMax = new Vector2(-6f, 0f);
        input.textComponent = text;
        return input;
    }

    private void AddSpacer(RectTransform parent, float width)
    {
        GameObject spacer = CreateUIObject("Spacer", parent);
        spacer.AddComponent<LayoutElement>().preferredWidth = width;
    }

    private TMP_Text AddText(RectTransform parent, string value, int fontSize, FontStyles style)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = ResolveFont();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.AddComponent<RectTransform>();
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###");
    }

    private class RuntimeControl
    {
        private readonly Action load;
        private readonly Action apply;

        public RuntimeControl(Action load, Action apply)
        {
            this.load = load;
            this.apply = apply;
        }

        public void Load()
        {
            load?.Invoke();
        }

        public void Apply()
        {
            apply?.Invoke();
        }
    }

    private class BlockTypeProfileControls
    {
        public RuntimeControl minHeight;
        public RuntimeControl maxHeight;
        public RuntimeControl buildingSetback;
    }
}

[CreateAssetMenu(fileName = "RuntimeCityPresetLibrary", menuName = "Ellish/City Generation/Runtime City Preset Library")]
public class RuntimeCityPresetLibrary : ScriptableObject
{
    public List<RuntimeCityPreset> presets = new List<RuntimeCityPreset>();

    public void AddOrReplace(RuntimeCityPreset preset)
    {
        if (preset == null || string.IsNullOrWhiteSpace(preset.presetName))
        {
            return;
        }

        for (int i = 0; i < presets.Count; i++)
        {
            RuntimeCityPreset existing = presets[i];
            if (existing != null && existing.presetName == preset.presetName)
            {
                presets[i] = preset;
                return;
            }
        }

        presets.Add(preset);
    }
}

[Serializable]
public class RuntimeCityPreset
{
    public string presetName = "Default";
    public int seed = 12345;

    [Header("Road Network")]
    public int mapSize = 1000;
    public float roadSegmentLength = 100f;
    public int majorRoadCount = 200;
    public int minorRoadCount = 400;
    [Range(0f, 1f)] public float branchProbability = 0.1f;
    [Range(0f, 1f)] public float deletionProbability = 0.2f;
    public float laneWidth = 3.5f;

    [Header("Blocks")]
    [Range(0f, 1f)] public float parkProbability = 0.08f;
    public float parkMinArea = 12000f;
    [Range(0f, 1f)] public float parkIrregularityThreshold = 0.62f;

    [Header("Lots")]
    public BlockTypeAssignmentMode assignmentMode = BlockTypeAssignmentMode.Default;
    public UrbanBlockType defaultBuildingType = UrbanBlockType.Residential;
    [Range(0f, 1f)] public float residentialWeight = 0.45f;
    [Range(0f, 1f)] public float commercialWeight = 0.25f;
    [Range(0f, 1f)] public float industrialWeight = 0.3f;
    [Range(0f, 1f)] public float commercialInnerNormalizedRadius = 0.25f;
    [Range(0f, 1f)] public float residentialMiddleNormalizedRadius = 0.65f;

    [Header("Block Type Profiles")]
    public RuntimeBlockTypePreset residential = new RuntimeBlockTypePreset(6f, 18f, 3f);
    public RuntimeBlockTypePreset commercial = new RuntimeBlockTypePreset(20f, 80f, 1.5f);
    public RuntimeBlockTypePreset industrial = new RuntimeBlockTypePreset(5f, 14f, 2f);

    public static RuntimeCityPreset CreateDefault()
    {
        return new RuntimeCityPreset { presetName = "Default" };
    }

    public static RuntimeCityPreset CreateDenseCity()
    {
        return new RuntimeCityPreset
        {
            presetName = "Dense City",
            seed = 23191,
            roadSegmentLength = 72f,
            majorRoadCount = 320,
            minorRoadCount = 760,
            branchProbability = 0.18f,
            deletionProbability = 0.08f,
            parkProbability = 0.04f,
            assignmentMode = BlockTypeAssignmentMode.DistanceToCenter,
            commercialInnerNormalizedRadius = 0.32f,
            residentialMiddleNormalizedRadius = 0.72f,
            residential = new RuntimeBlockTypePreset(12f, 38f, 2f),
            commercial = new RuntimeBlockTypePreset(35f, 110f, 1f),
            industrial = new RuntimeBlockTypePreset(8f, 22f, 2f)
        };
    }

    public static RuntimeCityPreset CreateSparseGrid()
    {
        return new RuntimeCityPreset
        {
            presetName = "Sparse Grid",
            seed = 12011,
            roadSegmentLength = 140f,
            majorRoadCount = 120,
            minorRoadCount = 220,
            branchProbability = 0.06f,
            deletionProbability = 0.28f,
            parkProbability = 0.12f,
            assignmentMode = BlockTypeAssignmentMode.Default,
            defaultBuildingType = UrbanBlockType.Residential,
            residential = new RuntimeBlockTypePreset(5f, 16f, 4f),
            commercial = new RuntimeBlockTypePreset(12f, 36f, 2f),
            industrial = new RuntimeBlockTypePreset(5f, 12f, 3f)
        };
    }

    public static RuntimeCityPreset CreateOrganic()
    {
        return new RuntimeCityPreset
        {
            presetName = "Organic",
            seed = 8027,
            roadSegmentLength = 86f,
            majorRoadCount = 230,
            minorRoadCount = 520,
            branchProbability = 0.28f,
            deletionProbability = 0.18f,
            parkProbability = 0.16f,
            parkIrregularityThreshold = 0.5f,
            assignmentMode = BlockTypeAssignmentMode.RandomWeighted,
            residentialWeight = 0.58f,
            commercialWeight = 0.24f,
            industrialWeight = 0.18f
        };
    }

    public static RuntimeCityPreset CreateIndustrial()
    {
        return new RuntimeCityPreset
        {
            presetName = "Industrial",
            seed = 55102,
            roadSegmentLength = 125f,
            majorRoadCount = 160,
            minorRoadCount = 300,
            branchProbability = 0.09f,
            deletionProbability = 0.22f,
            laneWidth = 4f,
            parkProbability = 0.03f,
            assignmentMode = BlockTypeAssignmentMode.RandomWeighted,
            residentialWeight = 0.18f,
            commercialWeight = 0.22f,
            industrialWeight = 0.6f,
            residential = new RuntimeBlockTypePreset(5f, 14f, 3f),
            commercial = new RuntimeBlockTypePreset(10f, 34f, 2f),
            industrial = new RuntimeBlockTypePreset(6f, 20f, 4f)
        };
    }

    public static RuntimeCityPreset CreateFromController(CityGenerationController controller, string presetName)
    {
        RuntimeCityPreset preset = new RuntimeCityPreset
        {
            presetName = string.IsNullOrWhiteSpace(presetName) ? "Runtime Preset" : presetName
        };

        if (controller == null)
        {
            return preset;
        }

        controller.FindStageGenerators();

        RoadNetworkGenerator roads = controller.roadNetworkGenerator;
        if (roads != null)
        {
            preset.seed = roads.randomSeed;
            preset.mapSize = roads.mapSize;
            preset.roadSegmentLength = roads.roadSegmentLength;
            preset.majorRoadCount = roads.majorRoadCount;
            preset.minorRoadCount = roads.minorRoadCount;
            preset.branchProbability = roads.branchProbability;
            preset.deletionProbability = roads.deletionProbability;
            preset.laneWidth = roads.laneWidth;
        }

        BlockAreaGenerator blocks = controller.blockAreaGenerator;
        if (blocks != null)
        {
            preset.parkProbability = blocks.parkProbability;
            preset.parkMinArea = blocks.parkMinArea;
            preset.parkIrregularityThreshold = blocks.parkIrregularityThreshold;
        }

        LotAreaGenerator lots = controller.lotAreaGenerator;
        if (lots != null)
        {
            preset.assignmentMode = lots.assignmentMode;
            preset.defaultBuildingType = lots.defaultBuildingType;
            preset.residentialWeight = lots.residentialWeight;
            preset.commercialWeight = lots.commercialWeight;
            preset.industrialWeight = lots.industrialWeight;
            preset.commercialInnerNormalizedRadius = lots.commercialInnerNormalizedRadius;
            preset.residentialMiddleNormalizedRadius = lots.residentialMiddleNormalizedRadius;
            preset.residential = RuntimeBlockTypePreset.CreateFromProfile(lots.GetProfile(UrbanBlockType.Residential));
            preset.commercial = RuntimeBlockTypePreset.CreateFromProfile(lots.GetProfile(UrbanBlockType.Commercial));
            preset.industrial = RuntimeBlockTypePreset.CreateFromProfile(lots.GetProfile(UrbanBlockType.Industrial));
        }

        return preset;
    }
}

[Serializable]
public class RuntimeBlockTypePreset
{
    public float minHeight = 6f;
    public float maxHeight = 18f;
    public float buildingSetback = 3f;

    public RuntimeBlockTypePreset(float minHeight, float maxHeight, float buildingSetback)
    {
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.buildingSetback = buildingSetback;
    }

    public static RuntimeBlockTypePreset CreateFromProfile(UrbanBlockTypeProfile profile)
    {
        if (profile == null)
        {
            return new RuntimeBlockTypePreset(6f, 18f, 3f);
        }

        return new RuntimeBlockTypePreset(profile.minHeight, profile.maxHeight, profile.buildingSetback);
    }
}
