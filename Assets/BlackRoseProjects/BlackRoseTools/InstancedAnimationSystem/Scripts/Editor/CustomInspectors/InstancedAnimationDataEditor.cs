#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.Callbacks;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomEditor(typeof(InstancedAnimationData))]
    internal class InstancedAnimationDataEditor : Editor
    {
        private const string ContextPath = "CONTEXT/InstancedAnimationData/";

        private static int selectedAnimation;

        private static bool animations;
        private static bool animationsBones;

        [SerializeField] private static bool[] lod_foldgroup = new bool[4];
        [SerializeField] private static int currentTab;

        private static bool customValuesEdit;

        private static Dictionary<Material, MaterialEditor> materials = new Dictionary<Material, MaterialEditor>();
        private static List<Material> currentScopeMaterials = new List<Material>();

        private GUIStyle CustomShaderValue_groupName;
        private GUIStyle labelHeader;

        private GUIStyle groupButton;
        private GUIStyle groupButtonSelected;

        private GUIStyle lodFoldout;
        private GUIStyle lodFoldoutDescription;
        private GUIStyle lodBackground;
        private GUIStyle lodCustomCategories;
        private GUIStyle removeButton;

        private bool requestWarmup = true;
        private bool setDirty = false;

        private Texture2D selectedTexture;
        private Texture2D selectedHovedTexture;
        private Texture2D normalTexture;
        private Texture2D normalHoverTexture;
        private Vector2 bonesScroll;

        private static class GuiContents
        {
            public static GUIContent CustomShaderValue_groupName = new GUIContent("Group name", "Name of group this custom value will be member of");
            public static GUIContent CustomShaderValue_propertyName = new GUIContent("Property name", "In shader name of property in instancing property block");
            public static GUIContent CustomShaderValue_defaultValue = new GUIContent("Default value", "Value that will be assigned at renderer initialization");

            public static GUIContent animations_animationName = new GUIContent("Animation", "Animation name (Read only)");
            public static GUIContent animations_duration = new GUIContent("Duration (s)", "Animation duration in seconds (Read only)");
            public static GUIContent animations_frames = new GUIContent("Frames", "Number of frames for this animation (Read only)");
            public static GUIContent animations_fps = new GUIContent("FPS", "Number of frames per second for this animation (Read only)");
            public static GUIContent animations_rootMotion = new GUIContent("Root Motion", "Is this clip supports root motion (Read only)");
            public static GUIContent animations_wrapMode = new GUIContent("Wrap Mode", "Wrap animation mode for this animation (Read only)");
            public static GUIContent animations_hasEvents = new GUIContent("Has events", "Is this animation clip has any events (Read only)");
            public static GUIContent animations_bonePerVertex = new GUIContent("Bone Per Vertex", "Number of bones that can affects every vertex (Read only)");
            public static GUIContent animations_animTextureWidth = new GUIContent("Animation Texture Width", "Width of animation texture (Read only)");
            public static GUIContent animations_animTextureHeight = new GUIContent("Animation Texture Height", "Height of animation texture (Read only)");

            public static GUIContent group_model = new GUIContent("Model", "Model informations");
            public static GUIContent group_animation = new GUIContent("Animation", "Animation informations");
            public static GUIContent group_culling = new GUIContent("Culling", "Culling options");
            public static GUIContent group_variants = new GUIContent("Variants", "Options for creating and managing variants");

            public static GUIContent group_bones = new GUIContent("Bones", "Names of all bones that has been baked (Read only)");
            public static GUIContent group_animations = new GUIContent("Animations Clips", "Data about all baked animations clips (Read only)");

            public static GUIContent lod_mesh = new GUIContent("Mesh", "Mesh used to drawing (Read only)");
            public static GUIContent lod_castShadows = new GUIContent("Cast Shadows", "Specifies whether a geometry creates shadows or not when a shadow-casting Light shines on it.");
            public static GUIContent lod_receiveShadows = new GUIContent("Receive Shadows", "When enabled, any shadows cast from other objects are drawn on the geometry.");
            public static GUIContent lod_layer = new GUIContent("Layer", "Layer on which this mesh will be rendered");
            public static GUIContent lod_distance = new GUIContent("Culling distance", "Distance to which this LOD will be used. This distance is scaled with object size and LOD Bias from Project Settings");
            public static GUIContent lod_customValues = new GUIContent("Show Custom Shader values", "Enable edit for custom shader block values. Required enabled Custom Shader Values in Project Settings -> InstancedAnimation System");
            public static GUIContent lod_customValues_remove = new GUIContent("-", "Remove this Custom Value");
            public static GUIContent lod_boundingSize = new GUIContent("Sphere radius", "Radius of bounding sphere used for renderer culling");
            public static GUIContent lod_boundingPivot = new GUIContent("Sphere pivot", "Pivot for bounding sphere");
            public static GUIContent lod_materials = new GUIContent("Materials", "Materials for this mesh");

            public static GUIContent materials_update = new GUIContent("Setup keywords", "Update all used materials keywords to match Animation Data");
            public static GUIContent materials_upgrade = new GUIContent("Match to pipeline", "Change materials Shaders to match their BlackRose equivalent for current Render Pipeline");
            public static GUIContent materials_validity = new GUIContent("Materials validity", "Check if all used materials are supported by Instanced Animation Rendering");

            public static GUIContent export_createVariant = new GUIContent("Create Variant", "Create variant of this Instanced Animation Data");
            public static GUIContent export_updateMeshSettingInVariants = new GUIContent("Sync Mesh data", "Update and override all mesh, materials and LOD settings in all variants to match this data. This will also reset Custom Shader Values");
            public static GUIContent export_updateMeshSetting = new GUIContent("Sync mesh data", "Update all mesh and LOD settings in this variant to match parent. This will also reset Custom Shader Values");
            public static GUIContent export_updateAnimationSettingInVariants = new GUIContent("Sync Animations data", "Update all animation data settings in all variants to match this data");
            public static GUIContent export_updateAnimationSetting = new GUIContent("Sync Animations data", "Update Animation data settings in this variant to match parent");
            public static GUIContent export_updateAllInVariants = new GUIContent("Sync all data in variants", "Update all baked data in all variants. This will clear any modifications in variants!");
            public static GUIContent export_updateAll = new GUIContent("Sync all data", "Update all baked data in this variant to match parent. This will override any modifications in this variant!");
            public static GUIContent export_updateCullingSettingInVariants = new GUIContent("Sync Culling data", "Update culling settings in all variants to match this data");
            public static GUIContent export_updateCullingSetting = new GUIContent("Sync Culling data", "Update culling settings in this variant to match parent");
        }

        private Texture2D CreateTexture(int size, Color color)
        {
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            Texture2D backgroundTexture = new Texture2D(size, size);
            backgroundTexture.SetPixels(pixels);
            backgroundTexture.Apply();
            return backgroundTexture;
        }

        private void Warmup()
        {
            if (!requestWarmup)
                return;
            requestWarmup = false;
            CustomShaderValue_groupName = new GUIStyle(GUI.skin.label);
            CustomShaderValue_groupName.alignment = TextAnchor.MiddleLeft;
            CustomShaderValue_groupName.clipping = TextClipping.Clip;
            CustomShaderValue_groupName.stretchWidth = false;
            CustomShaderValue_groupName.fixedWidth = 65;

            selectedTexture = CreateTexture(1, new Color(70 / 255f, 96 / 255f, 124 / 255f));
            selectedHovedTexture = CreateTexture(1, new Color(79 / 255f, 101 / 255f, 127 / 255f));
            normalTexture = CreateTexture(1, new Color(88 / 255f, 88 / 255f, 88 / 255f));
            normalHoverTexture = CreateTexture(1, new Color(103 / 255f, 103 / 255f, 103 / 255f));

            labelHeader = new GUIStyle(GUI.skin.label);
            labelHeader.fontStyle = FontStyle.Bold;

            groupButton = new GUIStyle(EditorStyles.toolbarButton);
            groupButton.margin = new RectOffset(2, 2, 1, 1);
            groupButton.padding = new RectOffset(5, 5, 2, 2);
            groupButton.fixedHeight = 20;
            groupButton.normal.textColor = Color.white;
            groupButton.normal.background = normalTexture;
            groupButton.hover.background = normalHoverTexture;
            groupButton.hover.textColor = Color.white;

            groupButtonSelected = new GUIStyle(EditorStyles.toolbarButton);
            groupButtonSelected.margin = new RectOffset(2, 2, 1, 1);
            groupButtonSelected.padding = new RectOffset(5, 5, 2, 2);
            groupButtonSelected.fixedHeight = 20;
            groupButtonSelected.normal.background = selectedTexture;
            groupButtonSelected.normal.textColor = Color.white;
            groupButtonSelected.hover.background = selectedHovedTexture;
            groupButtonSelected.hover.textColor = Color.white;

            lodFoldout = new GUIStyle(EditorStyles.foldoutHeader);
            lodFoldout.fontStyle = FontStyle.Normal;
            lodFoldout.richText = true;
            lodFoldout.fontSize = 13;
            lodFoldout.margin = new RectOffset(15, 5, 1, 1);
            lodFoldout.fixedHeight = 25;

            lodBackground = new GUIStyle(EditorStyles.helpBox);
            lodBackground.stretchWidth = true;
            lodBackground.border = new RectOffset(5, 5, 5, 5);

            removeButton = new GUIStyle(GUI.skin.button);
            removeButton.normal.textColor = Color.red;

            lodFoldoutDescription = new GUIStyle(GUI.skin.label);
            lodFoldoutDescription.alignment = TextAnchor.MiddleRight;
            lodFoldoutDescription.richText = true;

            lodCustomCategories = new GUIStyle(GUI.skin.label);
            lodCustomCategories.fontStyle = FontStyle.Bold;
        }

        private void OnEnable()
        {
            ClearMaterialEditors();
            requestWarmup = true;
        }

        private void OnDisable()
        {
            ClearMaterialEditors();
            DestroyImmediate(selectedTexture);
            DestroyImmediate(normalTexture);
            DestroyImmediate(normalHoverTexture);
            DestroyImmediate(selectedHovedTexture);
        }

        public override void OnInspectorGUI()
        {
            Warmup();

            InstancedAnimationData obj = (InstancedAnimationData)target;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(GuiContents.group_model, currentTab == 0 ? groupButtonSelected : groupButton))
                currentTab = 0;
            if (GUILayout.Button(GuiContents.group_animation, currentTab == 1 ? groupButtonSelected : groupButton))
                currentTab = 1;
            if (GUILayout.Button(GuiContents.group_culling, currentTab == 2 ? groupButtonSelected : groupButton))
                currentTab = 2;
            if (GUILayout.Button(GuiContents.group_variants, currentTab == 3 ? groupButtonSelected : groupButton))
                currentTab = 3;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            switch (currentTab)
            {
                case 0:
                    PrintMeshInfo(obj);
                    break;
                case 1:
                    PrintAnimations(obj);
                    break;
                case 2:
                    PrintBoundingSphere(obj);
                    break;
                case 3:
                    PrintVariants(obj);
                    break;
            }
            if (setDirty)
            {
                setDirty = false;
                EditorUtility.SetDirty(obj);
            }
        }

        private void ClearMaterialEditors()
        {
            foreach (KeyValuePair<Material, MaterialEditor> mat in materials)
            {
                MaterialEditor _materialEditor = mat.Value;
                if (_materialEditor != null)
                    DestroyImmediate(_materialEditor);
            }
            materials.Clear();
        }

        private void PrintVariants(InstancedAnimationData data)
        {
            EditorGUILayout.BeginVertical(lodBackground);

            bool isVariant = data.parent != null;
            if (!isVariant)
            {
                if (GUILayout.Button(GuiContents.export_createVariant))
                {
                    InstancedAnimationData variant = CreateInstance<InstancedAnimationData>();
                    EditorUtility.SetDirty(data);
                    EditorUtility.SetDirty(variant);
                    data.CopyTo(variant, true);
                    string path = AssetDatabase.GetAssetPath(data);
                    path = path.Substring(0, path.LastIndexOf(".")) + " (Variant).asset";
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(variant, path);
                    variant = AssetDatabase.LoadAssetAtPath<InstancedAnimationData>(path);
                    EditorGUIUtility.PingObject(variant);
                    AssetDatabase.SaveAssetIfDirty(data);
                    AssetDatabase.SaveAssetIfDirty(variant);
                }

                if (data.variants.Count > 0)
                {
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("All variants manage settings", lodCustomCategories);
                    GUILayout.BeginHorizontal();
                    GUI.color = new Color(1f, 0.95f, 0.95f);


                    if (GUILayout.Button(GuiContents.export_updateMeshSettingInVariants, GUILayout.MinWidth(20)))
                    {
                        for (int i = 0; i < data.variants.Count; ++i)
                        {
                            InstancedAnimationData iad = data.variants[i];
                            if (iad == null)
                                continue;
                            Undo.RecordObject(iad, "Update export");
                            iad.LOD = new InstancingLODData[data.LOD.Length];
                            for (int j = 0; j < data.LOD.Length; ++j)
                                iad.LOD[j] = data.LOD[j].Copy();
                            iad.customFloats = new List<CustomValueFloatHolder>(data.customFloats);
                            iad.customVectors = new List<CustomValueVectorHolder>(data.customVectors);
                        }
                    }
                    if (GUILayout.Button(GuiContents.export_updateAnimationSettingInVariants, GUILayout.MinWidth(20)))
                    {
                        for (int i = 0; i < data.variants.Count; ++i)
                        {
                            InstancedAnimationData iad = data.variants[i];
                            if (iad == null)
                                continue;
                            Undo.RecordObject(iad, "Update export");
                            iad.textureWidth = data.textureWidth;
                            iad.textureHeight = data.textureHeight;
                            iad.blockWidth = data.blockWidth;
                            iad.blockHeight = data.blockHeight;
                            iad.bonePerVertex = data.bonePerVertex;

                            iad.animations = new AnimationInfo[data.animations.Length];
                            for (int j = 0; j < data.animations.Length; ++j)
                                iad.animations[j] = data.animations[j].Copy();
                        }
                    }
                    if (GUILayout.Button(GuiContents.export_updateCullingSettingInVariants, GUILayout.MinWidth(20)))
                    {
                        for (int i = 0; i < data.variants.Count; ++i)
                        {
                            InstancedAnimationData iad = data.variants[i];
                            if (iad == null)
                                continue;
                            Undo.RecordObject(iad, "Update export");

                            iad.boundingSphereOffset = data.boundingSphereOffset;
                            iad.boundingSphereRadius = data.boundingSphereRadius;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUI.color = new Color(1f, 0.90f, 0.90f);
                    if (GUILayout.Button(GuiContents.export_updateAllInVariants))
                    {
                        for (int i = 0; i < data.variants.Count; ++i)
                        {
                            InstancedAnimationData iad = data.variants[i];
                            if (iad == null)
                                continue;
                            Undo.RecordObject(iad, "Update export");
                            data.CopyTo(iad, false);
                        }
                    }
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField("Variants", lodCustomCategories);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical(lodBackground);
                    GUI.enabled = false;
                    for (int i = 0; i < data.variants.Count; ++i)
                    {
                        InstancedAnimationData iad = data.variants[i];
                        if (iad == null)
                        {
                            EditorUtility.SetDirty(data);
                            data.variants.RemoveAt(i);
                            --i;
                            continue;
                        }
                        EditorGUILayout.ObjectField(iad, typeof(InstancedAnimationData), false);
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Parent", data.parent, typeof(InstancedAnimationData), false);
                GUI.enabled = true;
                GUILayout.BeginHorizontal();
                GUI.color = new Color(1f, 0.95f, 0.95f);
                if (GUILayout.Button(GuiContents.export_updateMeshSetting, GUILayout.MinWidth(20)))
                {
                    Undo.RecordObject(data, "Update export");
                    data.LOD = new InstancingLODData[data.parent.LOD.Length];
                    for (int j = 0; j < data.parent.LOD.Length; ++j)
                        data.LOD[j] = data.parent.LOD[j].Copy();
                    data.customFloats = new List<CustomValueFloatHolder>(data.parent.customFloats);
                    data.customVectors = new List<CustomValueVectorHolder>(data.parent.customVectors);
                }
                if (GUILayout.Button(GuiContents.export_updateAnimationSetting, GUILayout.MinWidth(20)))
                {
                    Undo.RecordObject(data, "Update export");
                    data.textureWidth = data.parent.textureWidth;
                    data.textureHeight = data.parent.textureHeight;
                    data.blockWidth = data.parent.blockWidth;
                    data.blockHeight = data.parent.blockHeight;
                    data.bonePerVertex = data.parent.bonePerVertex;

                    data.animations = new AnimationInfo[data.parent.animations.Length];
                    for (int j = 0; j < data.parent.animations.Length; ++j)
                        data.animations[j] = data.parent.animations[j].Copy();
                }
                if (GUILayout.Button(GuiContents.export_updateCullingSetting, GUILayout.MinWidth(20)))
                {
                    Undo.RecordObject(data, "Update export");
                    data.boundingSphereOffset = data.parent.boundingSphereOffset;
                    data.boundingSphereRadius = data.parent.boundingSphereRadius;
                }
                EditorGUILayout.EndVertical();
                GUI.color = new Color(1f, 0.90f, 0.90f);
                if (GUILayout.Button(GuiContents.export_updateAll))
                {
                    Undo.RecordObject(data, "Update export");
                    data.parent.CopyTo(data, false);
                }
                GUI.color = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void PrintMeshInfo(InstancedAnimationData data)
        {
            currentScopeMaterials.Clear();

            bool showCustomValues = true;
#if !BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            showCustomValues = false;
            customValuesEdit = false;
#endif
            customValuesEdit = InstancedAnimationHelper.ToggleField(GuiContents.lod_customValues, customValuesEdit, 200, showCustomValues);
            int customInUse = data.customFloats.Count + data.customVectors.Count;
            if (customInUse > 0 && showCustomValues)
                EditorGUILayout.HelpBox("Custom values in use: " + customInUse, MessageType.None);
            if (!showCustomValues)
                EditorGUILayout.HelpBox("Custom shader values are disabled. To use this feature, enable it in Project Settings->Instancing Animation System", MessageType.Info);

            Rect rect = GUILayoutUtility.GetRect(5000, 30);
            InstancedAnimationEditorManager.DrawLODBar(rect, data, Vector3.zero, 1f, null, false, false);
            //lod groups
            for (int i = 0; i < data.LOD.Length; ++i)
                PrintSingleLOD(data, i);

            if (CheckLodOverlap(data))
                EditorGUILayout.HelpBox("2 or more LOD's groups are overlaping! Consider changing LOD groups distances", MessageType.Warning);


            //material fixers
            GUILayout.Space(15);

            EditorGUILayout.HelpBox("All Shaders used by InstancedRenderers must include base from AnimationInstancing. Use one of BlackRoseProjects shader, or create custom. To lern more check documentation", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(GuiContents.materials_update))
            {
                UpdateMaterials(data);
            }
            if (GUILayout.Button(GuiContents.materials_upgrade))
            {
                UpgradeMaterials(data);
            }
            if (GUILayout.Button(GuiContents.materials_validity))
            {
                MaterialValidity(data);
            }
            EditorGUILayout.EndHorizontal();

            //matrial inspectors
            int savedLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            foreach (KeyValuePair<Material, MaterialEditor> mat in materials)
            {
                MaterialEditor _materialEditor = mat.Value;
                if (_materialEditor != null && currentScopeMaterials.Contains(mat.Key))
                {
                    _materialEditor.DrawHeader();
                    _materialEditor.OnInspectorGUI();
                }
            }
            EditorGUI.indentLevel = savedLevel;
        }

        private static void UpdateMaterials(InstancedAnimationData data)
        {
            int bonePerVertex = data.bonePerVertex;
            foreach (Material material in GetUniqueFixedMaterials(data))
            {
                Undo.RecordObject(material, "Updateing material");
                InstancedAnimationHelper.FixMaterialData(material, bonePerVertex);
            }
            EditorUtility.DisplayDialog("Animation data", "Updated all used materials keywords", "Close");
        }

        private static void UpgradeMaterials(InstancedAnimationData data)
        {
            int anyChanged = 0;
            int bonePerVertex = data.bonePerVertex;
            foreach (Material material in GetUniqueFixedMaterials(data))
            {
                Undo.RecordObject(material, "Upgrading material");
                Shader s = InstancedAnimationHelper.GetInstancedShaderForShader(material.shader);
                if (s != material.shader)
                {
                    anyChanged++;
                    material.shader = s;
                }
                InstancedAnimationHelper.FixMaterialData(material, bonePerVertex);
            }
            if (anyChanged > 0 && !Application.isPlaying)
                InstancedAnimationEditorManager.RefreshMaterials();
            if (anyChanged > 0)
                EditorUtility.DisplayDialog("Animation data", "Converted " + anyChanged + " materials", "Close");
            else
                EditorUtility.DisplayDialog("Animation data", "No materials need converting", "Close");
        }

        private static List<Material> GetUniqueFixedMaterials(InstancedAnimationData data)
        {
            List<Material> list = new List<Material>();
            for (int i = 0; i < data.LOD.Length; ++i)
            {
                for (int j = 0; j < data.LOD[i].instancingMeshData.Length; ++j)
                {
                    for (int k = 0; k < data.LOD[i].instancingMeshData[j].fixedMaterials.Length; ++k)
                    {
                        Material mat = data.LOD[i].instancingMeshData[j].fixedMaterials[k];
                        if (mat != null && !list.Contains(mat))
                        {
                            list.Add(mat);
                        }
                    }
                }
            }
            return list;
        }

        private static void MaterialValidity(InstancedAnimationData data)
        {
            int anyFailed = 0;
            foreach (Material mat in GetUniqueFixedMaterials(data))
            {
                if (!InstancedAnimationHelper.CheckIfIncludeInstanced(mat))
                {
                    ++anyFailed;
                    Debug.LogError("Material " + mat + " is most likely not using InstancedAnimation shader!", mat);
                }
            }
            if (anyFailed > 0)
                EditorUtility.DisplayDialog("Animation Data", "Found " + anyFailed + " potentialy not working shaders. For more informations check console logs", "close");
            else
                EditorUtility.DisplayDialog("Animation Data", "All materials are valid", "close");

        }

        private void PrintBoundingSphere(InstancedAnimationData data)
        {
            EditorGUILayout.BeginVertical(lodBackground);
            EditorGUILayout.HelpBox("Bounding sphere is used for culling Instanced Animation Renderer when out of screen. This value is converted during baking, but due to applying animations, baked values might not cover whole mesh. Best adjustment can be done using Configurator Helper window from Black Rose Projects -> Instanced Animation System -> Configurator Helper", MessageType.Info);

            EditorGUILayout.LabelField("Bounding sphere", labelHeader);
            EditorGUI.indentLevel++;
            Vector3 v = data.boundingSphereOffset;
            Vector3 pivot = InstancedAnimationHelper.Vector3Field(GuiContents.lod_boundingPivot, v, 150, true);
            float range = InstancedAnimationHelper.FloatField(GuiContents.lod_boundingSize, data.boundingSphereRadius, 150, true);
            if (pivot != v || range != data.boundingSphereRadius)
            {
                Undo.RecordObject(data, "change bounding");
                setDirty = true;
                data.boundingSphereOffset = pivot;
                data.boundingSphereRadius = range;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private bool CheckLodOverlap(InstancedAnimationData data)
        {
            float min = -1;
            for (int i = 0; i < data.LOD.Length; ++i)
            {
                float h = data.LOD[i].height;
                if (h == min && h != -1)
                {
                    return true;
                }
                else
                {
                    min = h;
                }
            }
            return false;
        }

        private void PrintSingleLOD(InstancedAnimationData data, int lod)
        {
            if (data.LOD.Length <= lod)
                return;
            InstancingLODData ilod = data.LOD[lod];

            EditorGUILayout.BeginVertical(lodBackground);
            string text;
            if (ilod.height == -1)
                text = $"<b>LOD {lod}</b> [infinity]";
            else
                text = $"<b>LOD {lod}</b> [{ilod.height:0.00}m]";
            lod_foldgroup[lod] = EditorGUILayout.BeginFoldoutHeaderGroup(lod_foldgroup[lod], text, lodFoldout);
            Rect lastRect = GUILayoutUtility.GetLastRect();

            int meshes = ilod.instancingMeshData.Length;
            int vertex = 0;
            for (int i = 0; i < meshes; ++i)
                vertex += ilod.instancingMeshData[i].mesh.vertexCount;
            string name;
            if (lastRect.width > 300)
                name = $"Meshes: {meshes}, vertex: {vertex}";
            else if (lastRect.width > 200)
                name = $"Vertex: {vertex}";
            else
                name = "";
            EditorGUI.LabelField(lastRect, name, lodFoldoutDescription);

            if (lod_foldgroup[lod])
            {
                float height = InstancedAnimationHelper.FloatField(GuiContents.lod_distance, ilod.height, 150, true);

                if (height != ilod.height)
                {
                    if (height <= 0)
                    {
                        height = -1;
                        if (lod < data.LOD.Length - 1) height = ilod.height;
                    }
                    else
                    {
                        if (lod < data.LOD.Length - 1 && height >= data.LOD[lod + 1].height && data.LOD[lod + 1].height != -1)
                            height = data.LOD[lod + 1].height;
                        if (lod > 0 && height <= data.LOD[lod - 1].height)
                            height = data.LOD[lod - 1].height;
                    }
                    if (height != ilod.height)
                    {
                        Undo.RecordObject(data, "Change lod settings");
                        ilod.height = height;
                        setDirty = true;
                        switch (lod)
                        {
                            case 0:
                                data.lodFloat.x = height;
                                break;
                            case 1:
                                data.lodFloat.y = height;
                                break;
                            case 2:
                                data.lodFloat.z = height;
                                break;
                            case 3:
                                data.lodFloat.w = height;
                                break;
                        }
                    }
                }

                EditorGUI.indentLevel++;
                for (int meshID = 0; meshID < meshes; ++meshID)
                {
                    InstancingMeshData imd = ilod.instancingMeshData[meshID];


                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(GuiContents.lod_mesh, GUILayout.Width(150));
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(imd.mesh, typeof(Mesh), false);
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();

                    int layer = InstancedAnimationHelper.LayerField(GuiContents.lod_layer, ilod.layer[meshID], 150, true);
                    ShadowCastingMode castShadows = (ShadowCastingMode)InstancedAnimationHelper.EnumField(GuiContents.lod_castShadows, ilod.shadowsMode[meshID], 150, true);
                    bool receiveShadows = InstancedAnimationHelper.ToggleField(GuiContents.lod_receiveShadows, ilod.receiveShadows[meshID], 150, true);

                    if (castShadows != ilod.shadowsMode[meshID] || receiveShadows != ilod.receiveShadows[meshID] || layer != ilod.layer[meshID])
                    {
                        Undo.RecordObject(data, "Modify mesh settings");
                        setDirty = true;
                        ilod.shadowsMode[meshID] = castShadows;
                        ilod.receiveShadows[meshID] = receiveShadows;
                        ilod.layer[meshID] = layer;
                    }
                    EditorGUILayout.LabelField(GuiContents.lod_materials, labelHeader);
                    EditorGUI.indentLevel++;
                    for (int materialID = 0; materialID < imd.originalMaterials.Length; ++materialID)
                    {
                        EditorGUILayout.BeginHorizontal(GUI.skin.box);
                        //  GUI.enabled = false;
                        //  EditorGUILayout.ObjectField(imd.originalMaterials[materialID], typeof(Material), false);
                        // GUI.enabled = true;
                        //  EditorGUILayout.LabelField("→", GUILayout.Width(45));
                        Material m = imd.fixedMaterials[materialID];
                        Material mat = (Material)EditorGUILayout.ObjectField("Element " + materialID, m, typeof(Material), false);
                        EditorGUILayout.EndHorizontal();
                        if (mat != imd.fixedMaterials[materialID])
                        {
                            Undo.RecordObject(data, "Modify material settings");
                            imd.fixedMaterials[materialID] = mat;
                            setDirty = true;

                            if (mat != null)
                            {
                                if (!InstancedAnimationHelper.CheckIfIncludeInstanced(mat))
                                    Debug.LogError("Shader for material " + mat.name + " is most likely not support InstancingRendering. Please use one of BlackRose default shader, or write custom shader, that supports InstancedRendering of animations", mat);
                                else
                                {
                                    Undo.RecordObject(mat, "Modify material settings");//keep same name so it will record in single undo operation
                                    InstancedAnimationHelper.FixMaterialData(mat, data.bonePerVertex);
                                    Debug.Log("Material " + mat.name + " has been upgraded", mat);
                                }
                            }
                        }
                        if (mat == null || !mat.enableInstancing)
                            EditorGUILayout.HelpBox("Material must have enabled GPU instancing and include InstancedRenderer base in shader", MessageType.Error);
                        if (mat != null)
                        {
                            currentScopeMaterials.Add(mat);
                            if (!materials.ContainsKey(mat))
                                materials.Add(mat, (MaterialEditor)CreateEditor(mat));
                        }
                        //custom shader values
                        if (customValuesEdit)
                        {
                            EditorGUI.indentLevel++;

                            Rect labels = GUILayoutUtility.GetRect(60, 2000, 20, 20);
                            float width = labels.width - 50;
                            EditorGUI.LabelField(new Rect(labels.x, labels.y, width * 0.25f, labels.height), GuiContents.CustomShaderValue_groupName, lodCustomCategories);
                            EditorGUI.LabelField(new Rect(labels.x + width * 0.25f, labels.y, width * 0.25f, labels.height), GuiContents.CustomShaderValue_propertyName, lodCustomCategories);
                            EditorGUI.LabelField(new Rect(labels.x + width * 0.5f, labels.y, width * 0.5f, labels.height), GuiContents.CustomShaderValue_defaultValue, lodCustomCategories);

                            GUI.color = new Color(1f, 0.9f, 0.9f);
                            for (int cf = 0; cf < imd.floatCustomData.Length; ++cf)
                            {
                                InstancingCustomFloatData icfd = imd.floatCustomData[cf];
                                if (icfd.submeshID == materialID)
                                {
                                    Rect v = GUILayoutUtility.GetRect(10, 2000, 20, 20);
                                    string groupName = EditorGUI.TextField(new Rect(v.x, v.y, width * 0.25f, v.height), icfd.groupName);
                                    string propertyName = EditorGUI.TextField(new Rect(v.x + width * 0.25f, v.y, width * 0.25f, v.height), icfd.propertyName);
                                    float defaultValue = EditorGUI.FloatField(new Rect(v.x + width * 0.5f, v.y, width * 0.5f, v.height), icfd.defaultValue);

                                    bool remove = false;
                                    if (GUI.Button(new Rect(width + 30, v.y, 35, v.height), GuiContents.lod_customValues_remove, removeButton))
                                        remove = true;

                                    if (icfd.groupName != groupName || icfd.propertyName != propertyName || icfd.defaultValue != defaultValue || remove)
                                    {
                                        Undo.RecordObject(data, "Changing custom values");
                                        setDirty = true;
                                        icfd.groupName = groupName;
                                        icfd.propertyName = propertyName;
                                        icfd.defaultValue = defaultValue;
                                        if (remove)
                                        {
                                            ArrayUtility.RemoveAt(ref imd.floatCustomData, cf);
                                            --cf;
                                        }
                                        data.GenerateCustomFloat();
                                    }
                                }
                            }
                            for (int cv = 0; cv < imd.vectorCustomData.Length; ++cv)
                            {
                                InstancingCustomVector4Data icvd = imd.vectorCustomData[cv];
                                if (icvd.submeshID == materialID)
                                {
                                    Rect v = GUILayoutUtility.GetRect(10, 2000, 20, 20);
                                    string groupName = EditorGUI.TextField(new Rect(v.x, v.y, width * 0.25f, v.height), icvd.groupName);
                                    string propertyName = EditorGUI.TextField(new Rect(v.x + width * 0.25f, v.y, width * 0.25f, v.height), icvd.propertyName);
                                    Vector4 defaultValue = EditorGUI.Vector4Field(new Rect(v.x + width * 0.5f, v.y, width * 0.5f, v.height), "", icvd.defaultValue);

                                    bool remove = false;
                                    if (GUI.Button(new Rect(width + 30, v.y, 35, v.height), GuiContents.lod_customValues_remove, removeButton))
                                        remove = true;

                                    if (icvd.groupName != groupName || icvd.propertyName != propertyName || icvd.defaultValue != defaultValue || remove)
                                    {
                                        Undo.RecordObject(data, "Changing custom values");
                                        setDirty = true;
                                        icvd.groupName = groupName;
                                        icvd.propertyName = propertyName;
                                        icvd.defaultValue = defaultValue;
                                        if (remove)
                                        {
                                            ArrayUtility.RemoveAt(ref imd.vectorCustomData, cv);
                                            --cv;
                                        }
                                        data.GenerateCustomVector();
                                    }
                                }
                            }
                            GUILayout.BeginHorizontal();
                            GUILayoutUtility.GetRect(45, 20);
                            if (GUI.Button(GUILayoutUtility.GetRect(35, 40, 20, 20), "Add float"))
                            {
                                Undo.RecordObject(data, "Changing custom values");
                                setDirty = true;
                                InstancingCustomFloatData idfd = new InstancingCustomFloatData();
                                idfd.defaultValue = 0;
                                idfd.submeshID = materialID;

                                ArrayUtility.Add(ref imd.floatCustomData, idfd);
                                data.GenerateCustomFloat();
                            }
                            if (GUI.Button(GUILayoutUtility.GetRect(35, 40, 20, 20), "Add Vector"))
                            {
                                Undo.RecordObject(data, "Changing custom values");
                                setDirty = true;
                                InstancingCustomVector4Data idfd = new InstancingCustomVector4Data();
                                idfd.defaultValue = Vector4.zero;
                                idfd.submeshID = materialID;
                                ArrayUtility.Add(ref imd.vectorCustomData, idfd);
                                data.GenerateCustomVector();
                            }
                            if (GUI.Button(GUILayoutUtility.GetRect(35, 40, 20, 20), "Clear"))
                            {
                                Undo.RecordObject(data, "Changing custom values");
                                setDirty = true;
                                ArrayUtility.Clear(ref imd.vectorCustomData);
                                ArrayUtility.Clear(ref imd.floatCustomData);
                                data.GenerateCustomFloat();
                                data.GenerateCustomVector();
                            }
                            GUILayout.EndHorizontal();
                            EditorGUI.indentLevel--;
                            GUI.color = Color.white;
                        }

                        EditorGUILayout.Space(0.2f);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }

        private void PrintAnimations(InstancedAnimationData obj)
        {
            int clips = obj.animations.Length;
            string[] names = new string[obj.animations.Length];
            int frames = 0;
            for (int i = 0; i < clips; ++i)
            {
                frames += obj.animations[i].totalFrame;
                names[i] = obj.animations[i].animationName;
            }

            EditorGUILayout.BeginVertical(lodBackground);
            EditorGUILayout.HelpBox($"Animation Clips: {clips}\nTotal frames: {frames}", MessageType.Info);
            EditorGUILayout.LabelField("Animation texture info", labelHeader);

            InstancedAnimationHelper.StringField(GuiContents.animations_bonePerVertex, obj.bonePerVertex.ToString(), 200);
            InstancedAnimationHelper.StringField(GuiContents.animations_animTextureWidth, obj.textureWidth.ToString(), 200);
            InstancedAnimationHelper.StringField(GuiContents.animations_animTextureHeight, obj.textureHeight.ToString(), 200);
            EditorGUILayout.EndVertical();
            animations = EditorGUILayout.BeginFoldoutHeaderGroup(animations, GuiContents.group_animations);
            if (obj.animations != null && animations)
            {
                EditorGUILayout.BeginVertical(lodBackground);
                if (selectedAnimation > names.Length)
                    selectedAnimation = 0;
                selectedAnimation = EditorGUILayout.Popup(selectedAnimation, names);
                EditorGUI.indentLevel++;
                AnimationInfo selected = obj.animations[selectedAnimation];
                InstancedAnimationHelper.StringField(GuiContents.animations_duration, string.Format("{0:0.00}", (1f / selected.fps) * selected.totalFrame), 185);
                InstancedAnimationHelper.StringField(GuiContents.animations_frames, selected.totalFrame.ToString(), 185);
                InstancedAnimationHelper.StringField(GuiContents.animations_fps, selected.fps.ToString(), 185);
                InstancedAnimationHelper.ToggleField(GuiContents.animations_rootMotion, selected.rootMotion, 185);
                InstancedAnimationHelper.StringField(GuiContents.animations_wrapMode, selected.wrapMode.ToString(), 185);
                InstancedAnimationHelper.ToggleField(GuiContents.animations_hasEvents, selected.eventList != null && selected.eventList.Length > 0, 185);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            animationsBones = EditorGUILayout.BeginFoldoutHeaderGroup(animationsBones, GuiContents.group_bones);
            if (animationsBones)
            {
                EditorGUILayout.BeginVertical(lodBackground);
                EditorGUILayout.BeginVertical(GUILayout.Height(200));
                bonesScroll = EditorGUILayout.BeginScrollView(bonesScroll, false, true);
                for (int i = 0; i < obj.bonesNames.Length; ++i)
                    EditorGUILayout.TextField("", obj.bonesNames[i]);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        [MenuItem(ContextPath + "Open in Configurator", false, 150)]
        internal static void Context_OpenConfigurator(MenuCommand command)
        {
            InstancedAnimationData iad = (InstancedAnimationData)command.context;
            InstancedRendererConfigurator.OpenForConfig(iad, InstancedRendererConfigurator.OpenOption.none);
        }

        [MenuItem(ContextPath + "Setup Keywords", false, 300)]
        internal static void Context_UpdateMaterials(MenuCommand command)
        {
            InstancedAnimationData iad = (InstancedAnimationData)command.context;
            UpdateMaterials(iad);
        }

        [MenuItem(ContextPath + "Match to pipeline", false, 301)]
        internal static void Context_UpgradeMaterials(MenuCommand command)
        {
            InstancedAnimationData iad = (InstancedAnimationData)command.context;
            UpgradeMaterials(iad);
        }

        [MenuItem(ContextPath + "Materials validity", false, 302)]
        internal static void Context_CheckMaterials(MenuCommand command)
        {
            InstancedAnimationData iad = (InstancedAnimationData)command.context;
            MaterialValidity(iad);
        }

        [OnOpenAsset]
        internal static bool OnOpenAsset(int instanceID, int line)
        {
            Object target = EditorUtility.InstanceIDToObject(instanceID);

            if (target is InstancedAnimationData)
            {
                InstancedRendererConfigurator.OpenForConfig((InstancedAnimationData)target);
                return true;
            }
            return false;
        }
    }
}
#endif