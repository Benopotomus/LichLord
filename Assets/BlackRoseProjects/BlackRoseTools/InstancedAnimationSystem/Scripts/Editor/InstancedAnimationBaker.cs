#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using Unity.Mathematics;
using BlackRoseProjects.Utility;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedAnimationBaker : EditorWindow
    {
        #region Helper classes

        internal class SkinnedRendererHandler
        {
            public string name;
            public Mesh mesh;
            public SkinnedMeshRenderer skinnedMesh;
            public Material[] materials;
            public string[] bones;

            public SkinnedRendererHandler(SkinnedMeshRenderer renderer)
            {
                name = renderer.name;
                mesh = renderer.sharedMesh;
                skinnedMesh = renderer;
                materials = renderer.sharedMaterials;
                Transform[] root = renderer.bones;
                bones = new string[root.Length];
                for (int i = 0; i < root.Length; ++i)
                    bones[i] = root[i].name;
            }
        }

        internal class LodInfo
        {
            public float height;
            public SkinnedRendererHandler[] skinnedRenderers;
            public InstancedAnimationManager.VertexCache[] vertexCacheList;
            public InstancedAnimationManager.MaterialBlock[] materialBlockList;

            public void RegenerateCaches()
            {
                vertexCacheList = new InstancedAnimationManager.VertexCache[skinnedRenderers.Length /*+ staticRenderers.Length*/];
                materialBlockList = new InstancedAnimationManager.MaterialBlock[vertexCacheList.Length];
            }
        }

        private class AnimationBakeInfo
        {
            public SkinnedMeshRenderer[] skinnedMeshRenderers;
            public Animator animator;
            public int workingFrame;
            public float length;
            public int layer;
            public AnimationInfo info;
        }

        private class AnimationSetting
        {
            public bool useAnimation;
            public int fps;
            public int index;

            public AnimationSetting(int fps)
            {
                this.fps = fps;
                useAnimation = true;
            }
        }

        private class BakedFrameInfo
        {
            public Matrix4x4 worldMatrix;
            public int nameCode;
            public float animationTime;
            public int stateName;
            public int frameIndex;
            public int boneListIndex = -1;
            public Matrix4x4[] boneMatrix;

            public void CopyMatrixData(BakedFrameInfo src)
            {
                animationTime = src.animationTime;
                boneListIndex = src.boneListIndex;
                frameIndex = src.frameIndex;
                nameCode = src.nameCode;
                stateName = src.stateName;
                worldMatrix = src.worldMatrix;
                boneMatrix = src.boneMatrix;
            }
        }
        private static class GUIContents
        {
            public static readonly GUIContent prefab = new GUIContent("Prefab", "Prefab with Unity Animator and Skinned Meshes hierarchy, or Scene object with that hierarchy.");
            public static readonly GUIContent targetName = new GUIContent("Target Asset name", "Name that will be used for generated Asset.");
            public static readonly GUIContent bonePerVertex = new GUIContent("Bone per Vertex", "Determinate maximal number of bones that can affects every vertex. Lower value might results vertex glitches if rig is destined for higher bones, but lower bones per vertex reduce GPU performance during runtime.");
            public static readonly GUIContent fps = new GUIContent("Baking FPS", "Number of FPS in baked animations for every animation clip. For fast and dynamics animations it's recomended to increase this value. Usually 30fps is fully sufficient smooth animation. In runtime between-frames are interpolated.");
            public static readonly GUIContent customFps = new GUIContent("Use custom FPS", "Allow to set custom FPS value for individuals animations.");
            public static readonly GUIContent customFpsSingle = new GUIContent("FPS", "FPS value for this specific animation");
            public static readonly GUIContent animations = new GUIContent("Animations", "Select animations that will be baked");
            public static readonly GUIContent bake = new GUIContent("Generate Baked Animation Data");
            public static readonly GUIContent generateBakedAnimator = new GUIContent("Bake Animator", "This option allow to automatically bake Unity Animator into baked version supported by Instanced Animation System. This component is not required to play baked animations, but allow to reproduce working of Unity Animator behaviour");

            public static readonly GUIContent selectInAssets = new GUIContent("Select in Assets", "Ping generated Asset in Assets window");
            public static readonly GUIContent generateSimpleAtScene = new GUIContent("Create at Scene", "Create Instanced Renderer Behaviour at scene");
            public static readonly GUIContent openConfigurator = new GUIContent("Open Configurator", "Open Configurator window");
            public static readonly GUIContent helpButton = new GUIContent(EditorGUIUtility.IconContent("_Help@2x"));

            static GUIContents()
            {
                helpButton.tooltip = "Open online documentation";
            }
        }

        #endregion


        private readonly int[] stardardTextureSize = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };

        private Vector2 scrollPos;

        private string animationPackname;
        private string directoryPath;
        private GameObject generatedObject;
        [SerializeField] private GameObject generatedPrefab;
        [SerializeField] private List<AnimationClip> customClips = new List<AnimationClip>();
        private Dictionary<string, AnimationSetting> generateAnims = new Dictionary<string, AnimationSetting>();
        private Dictionary<int, InstancedAnimationManager.VertexCache> generateVertexCachePool;
        private Dictionary<int, List<BakedFrameInfo>> generateMatrixDataPool;
        private Dictionary<AnimatorState, AnimatorStateTransition[]> cacheTransition;
        private Dictionary<AnimationClip, UnityEngine.AnimationEvent[]> cacheAnimationEvent;
        private RuntimeAnimatorController runtimeInstance;
        private List<AnimatorStateBackup> backup;
        private List<AnimationClip> animationClips;
        private List<AnimationInfo> aniInfo = new List<AnimationInfo>();
        private int aniFps = 30;
        private bool allowCustomFPS = false;
        private bool bakeAnimator = true;

        private InstancedAnimationData generatedData;
        private InstancedAnimatorData bakedAnimator;

        private BakedFrameInfo[] generateObjectData;
        private List<AnimationBakeInfo> generateInfo;
        private int currentDataIndex;
        private AnimationBakeInfo workingInfo;
        private Transform[] boneTransform;
        private int boneCount = 20;
        const int BakeFrameCount = 50000;
        const int textureBlockWidth = 4;
        int textureBlockHeight = 10;

        private Texture2D bakedBoneTexture = null;
        private int pixelx = 0, pixely = 0;
        private int bonePerVertex = 4;

        private void OnEnable()
        {
            generateInfo = new List<AnimationBakeInfo>();
            cacheTransition = new Dictionary<AnimatorState, AnimatorStateTransition[]>();
            cacheAnimationEvent = new Dictionary<AnimationClip, UnityEngine.AnimationEvent[]>();
            backup = new List<AnimatorStateBackup>();
            animationClips = new List<AnimationClip>(16);
            generateVertexCachePool = new Dictionary<int, InstancedAnimationManager.VertexCache>();
            generateMatrixDataPool = new Dictionary<int, List<BakedFrameInfo>>();
            generateObjectData = new BakedFrameInfo[BakeFrameCount];
            for (int i = 0; i != generateObjectData.Length; ++i)
                generateObjectData[i] = new BakedFrameInfo();
        }
        private void Reset()
        {
            pixelx = 0;
            pixely = 0;
            if (generateVertexCachePool != null)
                generateVertexCachePool.Clear();
            if (generateMatrixDataPool != null)
                generateMatrixDataPool.Clear();
            currentDataIndex = 0;
        }

        private bool GenerateInternal()
        {
            if (generateInfo.Count > 0 && workingInfo == null)
            {
                workingInfo = generateInfo[0];
                generateInfo.RemoveAt(0);

                workingInfo.animator.gameObject.SetActive(true);
                workingInfo.animator.Update(0);
                workingInfo.animator.Play(workingInfo.info.animationNameHash);
                workingInfo.animator.Update(0);
                workingInfo.workingFrame = 0;
                return true;
            }
            if (workingInfo != null)
            {
                for (int j = 0; j != workingInfo.skinnedMeshRenderers.Length; ++j)
                {
                    GenerateBoneMatrix(workingInfo.skinnedMeshRenderers[j].name.GetHashCode(),
                                            workingInfo.info.animationNameHash,
                                            workingInfo.workingFrame);
                }
                if (workingInfo.info.velocity != null)
                {
                    workingInfo.info.velocity[workingInfo.workingFrame] = workingInfo.animator.velocity;
                    workingInfo.info.angularVelocity[workingInfo.workingFrame] = workingInfo.animator.angularVelocity * Mathf.Rad2Deg;
                }
                if (++workingInfo.workingFrame >= workingInfo.info.totalFrame)
                {
                    aniInfo.Add(workingInfo.info);
                    if (generateInfo.Count == 0)
                    {
                        foreach (var obj in cacheTransition)
                        {
                            obj.Key.transitions = obj.Value;
                        }
                        cacheTransition.Clear();
                        foreach (var obj in cacheAnimationEvent)
                        {
                            AnimationUtility.SetAnimationEvents(obj.Key, obj.Value);
                        }
                        cacheAnimationEvent.Clear();
                        PrepareBoneTexture(aniInfo);
                        bool status = SetupAnimationTexture(aniInfo, bakedBoneTexture);
                        if (!status)
                            Debug.LogError("Error while trying to generate animation texture!");

                        SaveAnimationInfo(animationPackname);

                        SkinnedMeshRenderer[] meshRender = generatedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                        for (int i = 0; i < meshRender.Length; ++i)
                        {
                            DestroyImmediate(meshRender[i].sharedMesh);
                        }

                        DestroyImmediate(workingInfo.animator.gameObject);
                        foreach (AnimatorStateBackup b in backup)
                            b.Revert();
                        backup.Clear();
                        DestroyImmediate(runtimeInstance);
                        EditorUtility.ClearProgressBar();
                    }

                    if (workingInfo.animator != null)
                    {
                        workingInfo.animator.gameObject.transform.position = Vector3.zero;
                        workingInfo.animator.gameObject.transform.rotation = Quaternion.identity;
                    }
                    workingInfo = null;
                    return true;
                }

                float deltaTime = workingInfo.length / (workingInfo.info.totalFrame - 1);
                workingInfo.animator.Update(deltaTime);
                EditorUtility.DisplayProgressBar("Generating Animations", string.Format("Animation '{0}' is Generating.", workingInfo.info.animationName),
                    (float)workingInfo.workingFrame / workingInfo.info.totalFrame);
                return true;
            }
            return false;
        }

        public static string OpenSaveFileWindow(string folder, string name)
        {
            string newPath = "";
            string path = EditorUtility.SaveFilePanel("Save as...", folder, name, "asset");
            if (path.Length > 0)
            {
                if (path.Contains(Application.dataPath))
                {
                    string s = path;
                    string d = Application.dataPath + "/";
                    string p = "Assets/" + s.Remove(0, d.Length);
                    newPath = p;
                }
                else
                {
                    Debug.LogError("Path is outside project: " + path);
                }
            }
            return newPath;
        }

        [MenuItem("Tools/Black Rose Projects/Instanced Animation System/Animation Baker", false, 0)]
        static void MakeWindow()
        {
            InstancedAnimationBaker window = GetWindow<InstancedAnimationBaker>("Instanced Animation Baker");
            window.minSize = new Vector2(350, 250);
        }

        private void OnGUI()
        {
            GUI.skin.label.richText = true;

            GUILayout.BeginHorizontal();
            GameObject prefab = EditorGUILayout.ObjectField(GUIContents.prefab, generatedPrefab, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button(GUIContents.helpButton, EditorStyles.toolbarButton, GUILayout.MaxWidth(30)))
                Application.OpenURL("http://docs.blackrosetools.com/InstancedAnimations/html/window_animation.html");
            GUILayout.EndHorizontal();
            if (prefab != generatedPrefab)
            {
                generateAnims.Clear();
                customClips.Clear();
                generatedPrefab = prefab;
                generatedData = null;
                bakedAnimator = null;
                if (prefab != null)
                {
                    SkinnedMeshRenderer[] meshRender = generatedPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    List<Matrix4x4> bindPose = new List<Matrix4x4>(150);
                    boneTransform = InstancedAnimationHelper.MergeBone(meshRender, bindPose);
                }
                animationPackname = "";
                directoryPath = "";
            }
            if (generatedPrefab == null)
                return;
            Animator animator = generatedPrefab.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                EditorGUILayout.HelpBox("Not found Animator on selected object.", MessageType.Error);
                return;
            }
            if (animator.runtimeAnimatorController == null)
            {
                EditorGUILayout.HelpBox("Not found Animator Controller in selected object's Animator", MessageType.Error);
                return;
            }
            if (generatedPrefab.GetComponentInChildren<SkinnedMeshRenderer>() == null)
            {
                EditorGUILayout.HelpBox("Not found any SkinnedMeshRenderer on selectd object", MessageType.Error);
                return;
            }

            bonePerVertex = EditorGUILayout.IntSlider(GUIContents.bonePerVertex, bonePerVertex, 1, 4);
            bakeAnimator = EditorGUILayout.Toggle(GUIContents.generateBakedAnimator, bakeAnimator);
            GUI.enabled = !allowCustomFPS;
            aniFps = EditorGUILayout.IntSlider(GUIContents.fps, aniFps, 1, 144);
            GUI.enabled = true;
            allowCustomFPS = EditorGUILayout.Toggle(GUIContents.customFps, allowCustomFPS);

            List<AnimationClip> clips = GetClips(animator);
            string[] clipNames = generateAnims.Keys.ToArray();
            int totalFrames = 0;
            List<int> frames = new List<int>();
            int order = 0;
            foreach (var clipName in clipNames)
            {
                AnimationSetting settings = generateAnims[clipName];
                if (!settings.useAnimation)
                    continue;
                AnimationClip clip = null;
                for (int i = 0; i < clips.Count; ++i)
                    if (clips[i].name == clipName)
                    {
                        clip = clips[i];
                        break;
                    }
                int framesToBake = (int)(clip.length * (allowCustomFPS ? settings.fps : aniFps) / 1.0f + 0.5f) + 1;
                framesToBake = Mathf.Clamp(framesToBake, 1, framesToBake);
                settings.index = order++;
                totalFrames += framesToBake;
                frames.Add(framesToBake);
            }
            if (totalFrames >= BakeFrameCount)
            {
                EditorGUILayout.HelpBox("Number of animation frames reached max allowed value. Use lower FPS setting, deselect some animations, optimalize Unity Animations or reduce mesh rig bones", MessageType.Error);
                return;
            }

            CalculateTextureSize(out int textureCount, out int textureWidth, out int textureHeight, frames.ToArray(), boneTransform);
            bool error = textureCount != 1;
            if (textureCount == 0)
                EditorGUILayout.HelpBox("There is certain animation's frames which is larger than a whole texture.", MessageType.Error);
            else if (textureCount == 1)
                EditorGUILayout.HelpBox(string.Format("Animation Texture will be approximately {0} X {1}", textureWidth, textureHeight), MessageType.None);
            else if (textureCount == -1)
                EditorGUILayout.HelpBox(string.Format("Not selected any animation to bake"), MessageType.None);
            else
                EditorGUILayout.HelpBox(string.Format("Too much key frames! Please reduce animations key frames before baking animation or use lower FPS setting"), MessageType.Error);

            EditorGUILayout.LabelField(GUIContents.animations);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);


            foreach (string clipName in clipNames)
            {
                AnimationClip clip = null;
                for (int i = 0; i < clips.Count; ++i)
                    if (clips[i].name == clipName)
                    {
                        clip = clips[i];
                        break;
                    }
                AnimationSetting settings = generateAnims[clipName];
                int framesToBake = clip ? (int)(clip.length * (allowCustomFPS ? settings.fps : aniFps) / 1.0f) : 1;
                framesToBake = Mathf.Clamp(framesToBake, 1, framesToBake);
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUIUtility.labelWidth = 200;
                    GUIContent content = new GUIContent($"({framesToBake}) {clipName}", $"{clipName}, frames: " + framesToBake);
                    settings.useAnimation = EditorGUILayout.Toggle(content, settings.useAnimation, GUILayout.ExpandWidth(!allowCustomFPS));
                    //EditorGUILayout.
                    GUI.enabled = settings.useAnimation;
                    if (allowCustomFPS)
                    {
                        EditorGUIUtility.labelWidth = 50;
                        settings.fps = EditorGUILayout.IntSlider(GUIContents.customFpsSingle, settings.fps, 1, 144, GUILayout.ExpandWidth(true));
                    }

                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                if (framesToBake > 5000)
                {
                    GUI.skin.label.richText = true;
                    EditorGUILayout.LabelField("<color=yellow>Long animations degrade performance, consider using a higher frame skip value.</color>", GUI.skin.label);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            GUILayout.FlexibleSpace();
            if (generatedData != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(GUIContents.selectInAssets))
                {
                    EditorGUIUtility.PingObject(generatedData);
                }
                if (GUILayout.Button(GUIContents.generateSimpleAtScene))
                {
                    GenerateSimpleAtScene();
                }
                if (GUILayout.Button(GUIContents.openConfigurator))
                {
                    InstancedRendererConfigurator.OpenForConfig(generatedData, InstancedRendererConfigurator.OpenOption.bounding);
                }
                GUILayout.EndHorizontal();
            }

            if (!error)
            {
                if (GUILayout.Button(GUIContents.bake))
                {
                    if (!CheckValidityOfModel(generatedPrefab))
                    {
                        EditorUtility.DisplayDialog("Animation Baker", "One of Skinned Mesh Renderer have missing mesh or material", "Cancel");
                        return;
                    }

                    if (string.IsNullOrEmpty(animationPackname))
                    {
                        animationPackname = generatedPrefab.name;
                        directoryPath = AssetDatabase.GetAssetPath(generatedPrefab);
                        if (!string.IsNullOrEmpty(directoryPath))
                            directoryPath = directoryPath.Substring(0, directoryPath.LastIndexOf("/"));
                    }
                    string output = OpenSaveFileWindow(directoryPath, animationPackname);
                    if (string.IsNullOrEmpty(output))
                        return;

                    directoryPath = output.Substring(0, output.LastIndexOf("/") + 1);
                    animationPackname = output.Substring(output.LastIndexOf("/") + 1);
                    animationPackname = animationPackname.Substring(0, animationPackname.LastIndexOf("."));

                    generatedData = null;
                    bakedAnimator = null;
                    BakeWithAnimator();
                    while (GenerateInternal()) ;
                }
            }
        }

        private static bool CheckValidityOfModel(GameObject root)
        {
            SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skinned.Length; ++i)
            {
                if (skinned[i].sharedMesh == null)
                    return false;
                foreach (Material m in skinned[i].sharedMaterials)
                {
                    if (m == null)
                        return false;
                }
            }
            return true;
        }

        private void GenerateSimpleAtScene()
        {
            GameObject gm = new GameObject(animationPackname);
            Undo.RegisterCreatedObjectUndo(gm, "Create Instanced Renderer");
            Vector3 pos = generatedPrefab.transform.position;
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                Camera cam = sv.camera;
                pos = cam.transform.position;
                pos += cam.transform.forward * 8;
            }
            gm.transform.position = pos;
            gm.SetActive(false);
            InstancedAnimationRenderer irb = gm.AddComponent<InstancedAnimationRenderer>();
            irb.animationData = generatedData;
            irb.animator = bakedAnimator;
            gm.SetActive(true);
            Selection.activeObject = gm;
            EditorGUIUtility.PingObject(gm);
        }

        internal static string[] MergeBone(SkinnedRendererHandler[] meshRender, List<Matrix4x4> bindPose, List<Transform> additionalBones = null)
        {
            List<string> listTransform = new List<string>(150);
            for (int i = 0; i != meshRender.Length; ++i)
            {
                string[] bones = meshRender[i].bones;
                Matrix4x4[] checkBindPose = meshRender[i].mesh.bindposes;
                for (int j = 0; j != bones.Length; ++j)
                {
                    string bone = bones[j];
                    int index = listTransform.FindIndex(q => q == bone);
                    if (index < 0)
                    {
                        listTransform.Add(bone);
                        if (bindPose != null)
                            bindPose.Add(checkBindPose[j]);
                    }
                    else
                        bindPose[index] = checkBindPose[j];
                }
            }
            return listTransform.ToArray();
        }

        private bool DoValidityCheck()
        {
            SkinnedMeshRenderer[] meshRender = generatedPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (meshRender.Length == 0)
            {
                EditorUtility.DisplayDialog("Animation Baker", "Selected prefab must have at least one skinned mesh renderer", "Close");
                return false;
            }

            return true;
        }

        private void ClearSpeedValues()
        {
            AnimatorController controller = runtimeInstance as AnimatorController;
            backup.Clear();
            foreach (var stat in controller.layers[0].stateMachine.states)
                backup.Add(new AnimatorStateBackup(stat.state));
        }

        private void BakeWithAnimator()
        {
            if (!DoValidityCheck())
                return;

            if (generatedPrefab != null)
            {
                generatedObject = Instantiate(generatedPrefab);
                //Selection.activeGameObject = generatedObject;
                generatedObject.transform.position = Vector3.zero;
                generatedObject.transform.rotation = Quaternion.identity;
                Animator animator = generatedObject.GetComponentInChildren<Animator>();

                runtimeInstance = Instantiate(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = runtimeInstance;
                ClearSpeedValues();

                SkinnedMeshRenderer[] meshRender = generatedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < meshRender.Length; ++i)
                {
                    meshRender[i].sharedMesh = Instantiate(meshRender[i].sharedMesh);
                }
                List<Matrix4x4> bindPose = new List<Matrix4x4>(150);
                Transform[] boneTransform = InstancedAnimationHelper.MergeBone(meshRender, bindPose, true);

                Reset();
                AddMeshVertex2Generate(meshRender, boneTransform, bindPose.ToArray());

                for (int j = 0; j != meshRender.Length; ++j)
                {
                    meshRender[j].enabled = true;
                }
                animator.applyRootMotion = true;

                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                cacheTransition.Clear();
                cacheAnimationEvent.Clear();
                AnimatorControllerLayer layer = controller.layers[0];

                AnalyzeStateMachine(layer.stateMachine, animator, meshRender, 0, 0);
            }
        }

        private void AnalyzeStateMachine(AnimatorStateMachine stateMachine, Animator animator, SkinnedMeshRenderer[] renderers, int layer, int animationIndex)
        {
            for (int i = 0; i != stateMachine.states.Length; ++i)
            {
                ChildAnimatorState state = stateMachine.states[i];
                AnimationClip clip = state.state.motion as AnimationClip;
                if (clip == null)
                    continue;
                AnimationSetting animationSettings;
                bool baked = false;
                if (!generateAnims.TryGetValue(clip.name, out animationSettings))
                    continue;
                if (!animationSettings.useAnimation)
                    continue;
                foreach (var obj in generateInfo)
                {
                    if (obj.info.animationName == clip.name)
                    {
                        baked = true;
                        break;
                    }
                }

                if (baked)
                    continue;

                AnimationBakeInfo bake = new AnimationBakeInfo();
                int bakeFPS = allowCustomFPS ? animationSettings.fps : aniFps;
                bake.length = clip.averageDuration;
                bake.animator = animator;
                bake.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                bake.skinnedMeshRenderers = renderers;
                bake.layer = layer;
                bake.info = new AnimationInfo();
                bake.info.animationName = clip.name;
                bake.info.animationNameHash = state.state.nameHash;
                bake.info.animationIndex = animationIndex;
                bake.info.totalFrame = (int)(bake.length * bakeFPS + 0.5f) + 1;
                bake.info.totalFrame = Mathf.Clamp(bake.info.totalFrame, 1, bake.info.totalFrame);
                bake.info.fps = bakeFPS;
                bake.info.rootMotion = clip.hasRootCurves;
                bake.info.wrapMode = clip.isLooping ? WrapMode.Loop : clip.wrapMode;
                if (bake.info.rootMotion)
                {
                    bake.info.velocity = new Vector3[bake.info.totalFrame];
                    bake.info.angularVelocity = new Vector3[bake.info.totalFrame];
                }
                generateInfo.Add(bake);
                animationIndex += bake.info.totalFrame;

                List<AnimationEvent> events = new List<AnimationEvent>();
                foreach (var evt in clip.events)
                {
                    AnimationEvent aniEvent = new AnimationEvent();
                    aniEvent.function = evt.functionName;
                    aniEvent.floatParameter = evt.floatParameter;
                    aniEvent.intParameter = evt.intParameter;
                    aniEvent.stringParameter = evt.stringParameter;
                    aniEvent.time = evt.time;
                    if (evt.objectReferenceParameter != null)
                        aniEvent.objectParameter = evt.objectReferenceParameter.name;
                    else
                        aniEvent.objectParameter = "";
                    events.Add(aniEvent);
                }
                bake.info.eventList = events.ToArray();
                cacheTransition.Add(state.state, state.state.transitions);
                state.state.transitions = null;
                cacheAnimationEvent.Add(clip, clip.events);
                UnityEngine.AnimationEvent[] tempEvent = new UnityEngine.AnimationEvent[0];
                AnimationUtility.SetAnimationEvents(clip, tempEvent);
            }
            for (int i = 0; i != stateMachine.stateMachines.Length; ++i)
            {
                AnalyzeStateMachine(stateMachine.stateMachines[i].stateMachine, animator, renderers, layer, animationIndex);
            }
        }


        private void SaveAnimationInfo(string name)
        {
            string path = directoryPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string scriptablePath = path + name + ".asset";

            InstancedAnimationData animationDataScriptable = AssetDatabase.LoadAssetAtPath<InstancedAnimationData>(scriptablePath);
            if (animationDataScriptable == null)
            {
                animationDataScriptable = CreateInstance<InstancedAnimationData>();
            }
            else
            {
                animationDataScriptable.animations = null;
                animationDataScriptable.animationTexture = null;
                animationDataScriptable.LOD = null;
            }
            animationDataScriptable = BRPAssetsHelper.CreateAsset(animationDataScriptable, scriptablePath);

            List<AnimationInfo> animations = new List<AnimationInfo>();
            object[] aiArray = aniInfo.ToArray();

            for (int i = 0; i < aiArray.Length; ++i)
            {
                AnimationInfo ai = (AnimationInfo)aiArray[i];
                ai.animationNameHash = ai.animationName.GetHashCode();

                if (ai.velocity != null)
                {
                    bool hasAnyKey = false;
                    for (int j = 0; j < ai.velocity.Length; ++j)
                    {
                        if (ai.velocity[j] != Vector3.zero || ai.angularVelocity[j] != Vector3.zero)
                        {
                            hasAnyKey = true;
                            break;
                        }
                    }
                    if (!hasAnyKey)
                    {
                        ai.velocity = null;
                        ai.angularVelocity = null;
                        ai.rootMotion = false;
                    }
                }

                animations.Add(ai);
            }
            AnimationInfo.Sort(animations);
            animationDataScriptable.animations = animations.ToArray();

            Texture2D texture = bakedBoneTexture;

            try
            {
                animationDataScriptable.bonesHashes = PrepareBindPoses(generatedObject.transform, out animationDataScriptable.bindPoses, out animationDataScriptable.bonesNames);
                Bounds bounds = GenerateMeshData(animationDataScriptable, generatedObject, bonePerVertex);
                Dictionary<Material, Material> materialMapper = new Dictionary<Material, Material>();
                for (int i = 0; i < animationDataScriptable.LOD.Length; ++i)
                {
                    InstancingLODData ild = animationDataScriptable.LOD[i];
                    for (int j = 0; j < ild.instancingMeshData.Length; ++j)
                    {
                        InstancingMeshData imd = ild.instancingMeshData[j];
                        imd.mesh.name = imd.mesh.name;
                        imd.mesh.bounds = bounds;
                        StripMesh(imd.mesh);

                        // imd.mesh = BRPAssetsHelper.CreateAsset(imd.mesh, targetPath + imd.mesh.name + ".mesh");
                        imd.mesh = BRPAssetsHelper.AddAssetToAsset(imd.mesh, animationDataScriptable);
                        imd.fixedMaterials = new Material[imd.originalMaterials.Length];
                        for (int k = 0; k < imd.originalMaterials.Length; ++k)
                        {
                            Material origi = imd.originalMaterials[k];
                            if (!materialMapper.TryGetValue(origi, out Material mat))
                            {
                                mat = TryAutoConvertMaterial(origi, bonePerVertex);
                                mat.name = mat.name;
                                // mat = BRPAssetsHelper.CreateAsset(mat, targetPath + mat.name + ".mat");
                                mat = BRPAssetsHelper.AddAssetToAsset(mat, animationDataScriptable);
                                materialMapper[origi] = mat;
                            }
                            imd.fixedMaterials[k] = mat;
                        }
                    }
                }
                texture.name = "BakedAnimationTexture";
                // texture = BRPAssetsHelper.CreateAsset(texture, targetPath + texture.name + ".asset");
                texture = BRPAssetsHelper.AddAssetToAsset(texture, animationDataScriptable);

                //inverse bindposes
                for (int i = 0; i < animationDataScriptable.bindPoses.Length; ++i)
                    animationDataScriptable.bindPoses[i] = animationDataScriptable.bindPoses[i].inverse;

                animationDataScriptable.animationTexture = texture;
                animationDataScriptable.parent = null;
                animationDataScriptable.textureWidth = texture.width;
                animationDataScriptable.textureHeight = texture.height;
                animationDataScriptable.bonePerVertex = bonePerVertex;
                animationDataScriptable.blockHeight = textureBlockHeight;
                animationDataScriptable.blockWidth = textureBlockWidth;
                if (animationDataScriptable.lodFloat.x == 0f && animationDataScriptable.lodFloat.y == 0f && animationDataScriptable.lodFloat.z == 0f && animationDataScriptable.lodFloat.w == 0f)
                    animationDataScriptable.lodFloat = CalculateLodFloat(animationDataScriptable.LOD);//dont override values!
                for (int i = 0; i < animationDataScriptable.LOD.Length; ++i)
                {
                    switch (i)
                    {
                        case 0:
                            animationDataScriptable.LOD[i].height = animationDataScriptable.lodFloat.x;
                            break;
                        case 1:
                            animationDataScriptable.LOD[i].height = animationDataScriptable.lodFloat.y;
                            break;
                        case 2:
                            animationDataScriptable.LOD[i].height = animationDataScriptable.lodFloat.z;
                            break;
                        case 3:
                            animationDataScriptable.LOD[i].height = animationDataScriptable.lodFloat.w;
                            break;
                    }
                }
                CalcBoundingSphere(generatedObject.transform, out float boundSize, out float3 boundOffset);
                if (animationDataScriptable.boundingSphereRadius == 0f && animationDataScriptable.boundingSphereOffset.x == 0 && animationDataScriptable.boundingSphereOffset.y == 0 && animationDataScriptable.boundingSphereOffset.z == 0)
                {//dont override values
                    animationDataScriptable.boundingSphereRadius = boundSize;
                    animationDataScriptable.boundingSphereOffset = boundOffset;
                }

                Debug.Log(string.Format("Saved animation texture with size {0}x{1}", texture.width, texture.height));

                foreach (AnimatorStateBackup b in backup)
                    b.Revert();
                backup.Clear();

                Animator animator = generatedPrefab.GetComponentInChildren<Animator>();
                if (animator != null && bakeAnimator)
                {
                    AnimatorConverter converter = new AnimatorConverter();
                    InstancedAnimatorData a = converter.Convert(animator.runtimeAnimatorController as AnimatorController);
                    a.name = name + "_Animator";
                    bakedAnimator = AnimatorConverter.SaveAsset(a, path + name + "_Animator.asset");
                }

                UpdateReferences(animationDataScriptable);
                animationDataScriptable.UpdateVariantsOnBaking();
                animationDataScriptable = BRPAssetsHelper.CreateAsset(animationDataScriptable, scriptablePath);
                generatedData = animationDataScriptable;

            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            aniInfo.Clear();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(animationDataScriptable);
        }

        private void UpdateReferences(InstancedAnimationData animationDataScriptable)
        {
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(animationDataScriptable));
            animationDataScriptable.animationTexture = GetNewReference(animationDataScriptable.animationTexture, objects);
            for (int i = 0; i < animationDataScriptable.LOD.Length; ++i)
            {
                InstancingLODData ild = animationDataScriptable.LOD[i];
                for (int j = 0; j < ild.instancingMeshData.Length; ++j)
                {
                    InstancingMeshData imd = ild.instancingMeshData[j];
                    imd.mesh = GetNewReference(imd.mesh, objects);
                    for (int k = 0; k < imd.fixedMaterials.Length; ++k)
                        imd.fixedMaterials[k] = GetNewReference(imd.fixedMaterials[k], objects);
                }
            }
        }

        private T GetNewReference<T>(T original, Object[] list) where T : Object
        {
            for (int i = 0; i < list.Length; ++i)
            {
                Object o = list[i];
                if (o.name == original.name && o.GetType() == o.GetType())
                    return (T)o;
            }
            return original;
        }

        internal static Bounds GenerateMeshData(InstancedAnimationData animationData, GameObject prefabRoot, int bonePerVertex)
        {
            List<Bounds> allBounds = new List<Bounds>();

            LODGroup lodgroup = prefabRoot.GetComponent<LODGroup>();
            List<SkinnedMeshRenderer> skinned_ = new List<SkinnedMeshRenderer>();
            LodInfo[] lodInfo;
            if (lodgroup != null)
            {
                lodInfo = new LodInfo[lodgroup.lodCount];
                LOD[] lods = lodgroup.GetLODs();
                for (int i = 0, i_size = lods.Length; i < i_size; ++i)
                {
                    if (lods[i].renderers == null)
                        continue;
                    LodInfo info = new LodInfo();
                    info.height = (lodgroup.size * 0.5f) / lods[i].screenRelativeTransitionHeight;
                    info.vertexCacheList = new InstancedAnimationManager.VertexCache[lods[i].renderers.Length];
                    info.materialBlockList = new InstancedAnimationManager.MaterialBlock[info.vertexCacheList.Length];
                    List<SkinnedMeshRenderer> listSkinnedMeshRenderer = new List<SkinnedMeshRenderer>();
                    for (int j = 0, j_size = lods[i].renderers.Length; j < j_size; ++j)
                    {
                        Renderer render = lods[i].renderers[j];
                        if (render is SkinnedMeshRenderer)
                            listSkinnedMeshRenderer.Add((SkinnedMeshRenderer)render);
                    }
                    info.skinnedRenderers = new SkinnedRendererHandler[listSkinnedMeshRenderer.Count];
                    for (int j = 0; j < info.skinnedRenderers.Length; ++j)
                        info.skinnedRenderers[j] = new SkinnedRendererHandler(listSkinnedMeshRenderer[j]);

                    lodInfo[i] = info;
                    skinned_.AddRange(listSkinnedMeshRenderer);
                }
            }
            else
            {
                lodInfo = new LodInfo[1];
                LodInfo info = new LodInfo();
                info.height = -1f;
                SkinnedMeshRenderer[] skinned = prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                info.skinnedRenderers = new SkinnedRendererHandler[skinned.Length];
                for (int j = 0; j < info.skinnedRenderers.Length; ++j)
                    info.skinnedRenderers[j] = new SkinnedRendererHandler(skinned[j]);

                skinned_.AddRange(skinned);
                info.vertexCacheList = new InstancedAnimationManager.VertexCache[info.skinnedRenderers.Length];
                info.materialBlockList = new InstancedAnimationManager.MaterialBlock[info.vertexCacheList.Length];
                lodInfo[0] = info;
            }

            for (int lodID = 0, lodSize = lodInfo.Length; lodID < lodSize; ++lodID)
            {
                LodInfo lod = lodInfo[lodID];
                for (int i = 0, i_size = lod.skinnedRenderers.Length; i < i_size; ++i)
                {
                    SkinnedRendererHandler renderer = lod.skinnedRenderers[i];
                    Mesh m = renderer.mesh;
                    if (m is null)
                        continue;

                    renderer.mesh = Instantiate(m);
                    allBounds.Add(renderer.mesh.bounds);
                    renderer.mesh.name = m.name;
                    renderer.mesh.MarkDynamic();

                    int nameCode = renderer.name.GetHashCode();
                    InstancedAnimationManager.VertexCache vertexCache = CreateVertexCache(nameCode, renderer.mesh);
                    vertexCache.bindPose = animationData.bindPoses;
                    SetupVertex(vertexCache, renderer, animationData.bonesHashes, bonePerVertex);
                    lod.vertexCacheList[i] = vertexCache;
                }
            }

            animationData.LOD = new InstancingLODData[lodInfo.Length];
            for (int i = 0; i < lodInfo.Length; ++i)
            {
                InstancingLODData ild = new InstancingLODData();
                LodInfo li = lodInfo[i];

                ild.height = li.height;
                int total = li.skinnedRenderers.Length;
                ild.instancingMeshData = new InstancingMeshData[total];
                ild.shadowsMode = new UnityEngine.Rendering.ShadowCastingMode[total];
                ild.layer = new int[total];
                ild.receiveShadows = new bool[total];
                for (int j = 0; j < ild.instancingMeshData.Length; ++j)
                {
                    SkinnedRendererHandler srh = li.skinnedRenderers[j];
                    InstancingMeshData imd = new InstancingMeshData();
                    imd.mesh = srh.mesh;
                    imd.originalMaterials = srh.materials;
                    ild.instancingMeshData[j] = imd;
                    ild.shadowsMode[j] = srh.skinnedMesh.shadowCastingMode;
                    ild.layer[j] = srh.skinnedMesh.gameObject.layer;
                    ild.receiveShadows[j] = srh.skinnedMesh.receiveShadows;
                }
                animationData.LOD[i] = ild;
            }

            Bounds b = new Bounds(allBounds[0].center, allBounds[0].size);

            for (int i = 1; i < allBounds.Count; ++i)
            {
                b.Encapsulate(allBounds[i]);
            }
            b = new Bounds(b.center, b.size * 1.35f);
            return b;
        }

        internal static InstancedAnimationManager.VertexCache CreateVertexCache(int renderName, Mesh mesh)
        {
            InstancedAnimationManager.VertexCache vertexCache = new InstancedAnimationManager.VertexCache();
            int cacheName = renderName;
            vertexCache.nameCode = cacheName;
            vertexCache.mesh = mesh;
            vertexCache.boneTextureIndex = 0;
            vertexCache.weight = new Vector4[mesh.vertexCount];
            vertexCache.boneIndex = new Vector4[mesh.vertexCount];
            vertexCache.instanceBlockList = new Dictionary<int, InstancedAnimationManager.MaterialBlock>();
            return vertexCache;
        }

        internal static void SetupVertex(InstancedAnimationManager.VertexCache vertexCache, SkinnedRendererHandler render, int[] boneTransform, int bonePerVertex)
        {
            int[] boneIndex = null;
            if (render.bones.Length != boneTransform.Length)
                Debug.LogWarning("Different bones count for Skinned mesh " + render.name);
            if (render.bones.Length < boneTransform.Length)
            {
                if (render.bones.Length == 0)
                {
                    Debug.LogWarning("Mismatch bones");
                    boneIndex = null;
                }
                else
                {
                    int bones = render.bones.Length;
                    boneIndex = new int[bones];
                    for (int j = 0; j < bones; ++j)
                    {
                        boneIndex[j] = -1;
                        int hashTransformName = render.bones[j].GetHashCode();
                        for (int k = 0; k < boneTransform.Length; ++k)
                        {
                            if (hashTransformName == boneTransform[k])
                            {
                                boneIndex[j] = k;
                                break;
                            }
                        }
                    }

                    if (boneIndex.Length == 0)
                    {
                        boneIndex = null;
                    }
                }
            }
            Mesh m = render.mesh;
            BoneWeight[] boneWeights = m.boneWeights;
            bool skipBoneMerge = boneWeights.Length == 0;
            for (int j = 0, j_size = m.vertexCount; j < j_size; ++j)
            {
                if (!skipBoneMerge)
                {
                    if (boneIndex == null)
                    {
                        vertexCache.boneIndex[j].x = boneWeights[j].boneIndex0;
                        vertexCache.boneIndex[j].y = boneWeights[j].boneIndex1;
                        vertexCache.boneIndex[j].z = boneWeights[j].boneIndex2;
                        vertexCache.boneIndex[j].w = boneWeights[j].boneIndex3;
                    }
                    else
                    {
                        vertexCache.boneIndex[j].x = boneIndex[boneWeights[j].boneIndex0];
                        vertexCache.boneIndex[j].y = boneIndex[boneWeights[j].boneIndex1];
                        vertexCache.boneIndex[j].z = boneIndex[boneWeights[j].boneIndex2];
                        vertexCache.boneIndex[j].w = boneIndex[boneWeights[j].boneIndex3];
                    }

                    if (bonePerVertex > 1)
                    {
                        vertexCache.weight[j].x = boneWeights[j].weight0;
                        vertexCache.weight[j].y = boneWeights[j].weight1;
                        vertexCache.weight[j].z = boneWeights[j].weight2;
                        vertexCache.weight[j].w = boneWeights[j].weight3;
                    }
                }

                if (bonePerVertex == 3)
                {
                    float rate = 1.0f / (vertexCache.weight[j].x + vertexCache.weight[j].y + vertexCache.weight[j].z);
                    vertexCache.weight[j].x = vertexCache.weight[j].x * rate;
                    vertexCache.weight[j].y = vertexCache.weight[j].y * rate;
                    vertexCache.weight[j].z = vertexCache.weight[j].z * rate;
                    vertexCache.weight[j].w = -0.1f;
                }
                else if (bonePerVertex == 2)
                {
                    float rate = 1.0f / (vertexCache.weight[j].x + vertexCache.weight[j].y);
                    vertexCache.weight[j].x = vertexCache.weight[j].x * rate;
                    vertexCache.weight[j].y = vertexCache.weight[j].y * rate;
                    vertexCache.weight[j].z = -0.1f;
                    vertexCache.weight[j].w = -0.1f;
                }
                else if (bonePerVertex == 1)
                {
                    vertexCache.weight[j].x = 1.0f;
                    vertexCache.weight[j].y = -0.1f;
                    vertexCache.weight[j].z = -0.1f;
                    vertexCache.weight[j].w = -0.1f;
                }
            }

            if (vertexCache.materials == null)
                vertexCache.materials = render.materials;
            SetupAdditionalData(vertexCache);
        }

        internal static void SetupAdditionalData(InstancedAnimationManager.VertexCache vertexCache)
        {
            int size = vertexCache.weight.Length;
            Color[] colors = new Color[size];
            for (int i = 0; i < size; ++i)
            {
                colors[i].r = vertexCache.weight[i].x;
                colors[i].g = vertexCache.weight[i].y;
                colors[i].b = vertexCache.weight[i].z;
                colors[i].a = vertexCache.weight[i].w;
            }
            vertexCache.mesh.colors = colors;
            vertexCache.mesh.SetUVs(2, vertexCache.boneIndex);
            vertexCache.mesh.UploadMeshData(false);
        }

        internal float4 CalculateLodFloat(InstancingLODData[] lod)
        {
            float4 lodFloat = new float4();
            int lenght = lod.Length;
            lodFloat.x = lod[0].height;
            lodFloat.y = lod[1 >= lenght ? lenght - 1 : 1].height;
            lodFloat.z = lod[2 >= lenght ? lenght - 1 : 2].height;
            lodFloat.w = lod[3 >= lenght ? lenght - 1 : 3].height;
            return lodFloat;
        }

        private static int[] PrepareBindPoses(Transform root, out Matrix4x4[] bindPoses, out string[] boneNames)
        {
            SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            List<Matrix4x4> bindPose = new List<Matrix4x4>(150);
            Transform[] bones = InstancedAnimationHelper.MergeBone(skinnedRenderers, bindPose);
            bindPose.Capacity = bindPose.Count;
            int[] boneID = new int[bones.Length];
            boneNames = new string[bones.Length];
            for (int i = 0; i < boneID.Length; ++i)
            {
                boneNames[i] = bones[i].name;
                boneID[i] = boneNames[i].GetHashCode();
            }
            bindPoses = bindPose.ToArray();
            return boneID;
        }

        private static void CalcBoundingSphere(Transform root, out float size, out float3 offset)
        {
            SkinnedMeshRenderer[] skinnedMeshRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            root.parent = null;
            root.position = Vector3.zero;
            size = 0;
            offset = new float3(0, 0, 0);

            if (skinnedMeshRenderer.Length > 0)
            {
                for (int i = 0; i < skinnedMeshRenderer.Length; ++i)
                {
                    if (skinnedMeshRenderer[i].rootBone == null)
                        continue;
                    Bounds bound = skinnedMeshRenderer[i].localBounds;

                    size = math.max(size, math.max(math.max(bound.extents.x, bound.extents.y), bound.extents.z));
                    offset = math.max(bound.center + skinnedMeshRenderer[i].rootBone.position, offset);
                }
            }
        }

        private void StripMesh(Mesh mesh)
        {
            mesh.boneWeights = null;
            mesh.bindposes = null;
            mesh.ClearBlendShapes();
            mesh.UploadMeshData(true);
        }

        private Material TryAutoConvertMaterial(Material material, int bonePerVertex)
        {
            Material newOne = Instantiate(material);
            newOne.name = material.name + "_Instancing";
            newOne.shader = InstancedAnimationHelper.GetInstancedShaderForShader(material.shader);
            InstancedAnimationHelper.FixMaterialData(newOne, bonePerVertex);
            return newOne;
        }

        private List<AnimationClip> GetClips(Animator animator)
        {
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            animationClips.Clear();
            return GetClipsFromStatemachine(controller.layers[0].stateMachine);
        }

        private List<AnimationClip> GetClipsFromStatemachine(AnimatorStateMachine stateMachine)
        {
            for (int i = 0; i != stateMachine.states.Length; ++i)
            {
                ChildAnimatorState state = stateMachine.states[i];
                if (state.state.motion is BlendTree)
                {
                    continue;
                    //BlendTree blendTree = state.state.motion as BlendTree;
                    //ChildMotion[] childMotion = blendTree.children;
                    //for (int j = 0; j != childMotion.Length; ++j)
                    //{
                    //    animationClips.Add(childMotion[j].motion as AnimationClip);
                    //}
                }
                else if (state.state.motion != null)
                    animationClips.Add(state.state.motion as AnimationClip);
            }
            for (int i = 0; i != stateMachine.stateMachines.Length; ++i)
            {
                animationClips.AddRange(GetClipsFromStatemachine(stateMachine.stateMachines[i].stateMachine));
            }

            var distinctClips = animationClips.Select(q => (AnimationClip)q).Distinct().ToList();
            for (int i = 0; i < distinctClips.Count; i++)
            {
                if (distinctClips[i] && generateAnims.ContainsKey(distinctClips[i].name) == false)
                    generateAnims.Add(distinctClips[i].name, new AnimationSetting(aniFps));
            }
            return animationClips;
        }

        private void GenerateBoneMatrix(int nameCode, int stateName, float stateTime)
        {
            InstancedAnimationManager.VertexCache vertexCache = null;
            if (!generateVertexCachePool.TryGetValue(nameCode, out vertexCache))
                return;

            BakedFrameInfo matrixData = generateObjectData[currentDataIndex++];
            matrixData.nameCode = nameCode;
            matrixData.stateName = stateName;
            matrixData.animationTime = stateTime;
            matrixData.worldMatrix = Matrix4x4.identity;
            matrixData.frameIndex = -1;
            matrixData.boneListIndex = -1;

            if (generateMatrixDataPool.ContainsKey(stateName))
            {
                List<BakedFrameInfo> list = generateMatrixDataPool[stateName];
                matrixData.boneMatrix = CalculateSkinMatrix(
                        vertexCache.bonePose,
                        vertexCache.bindPose);

                BakedFrameInfo data = new BakedFrameInfo();
                data.CopyMatrixData(matrixData);
                list.Add(data);
            }
            else
            {
                matrixData.boneMatrix = CalculateSkinMatrix(
                    vertexCache.bonePose,
                    vertexCache.bindPose);

                List<BakedFrameInfo> list = new List<BakedFrameInfo>();
                BakedFrameInfo data = new BakedFrameInfo();
                data.CopyMatrixData(matrixData);
                list.Add(data);
                generateMatrixDataPool[stateName] = list;
            }
        }

        private void AddMeshVertex2Generate(SkinnedMeshRenderer[] meshRender, Transform[] boneTransform, Matrix4x4[] bindPose)
        {
            boneCount = boneTransform.Length;
            textureBlockHeight = boneCount;
            for (int i = 0; i != meshRender.Length; ++i)
            {
                Mesh m = meshRender[i].sharedMesh;
                if (m == null)
                    continue;

                int nameCode = meshRender[i].name.GetHashCode();
                if (generateVertexCachePool.ContainsKey(nameCode))
                    continue;

                InstancedAnimationManager.VertexCache vertexCache = new InstancedAnimationManager.VertexCache();
                generateVertexCachePool[nameCode] = vertexCache;
                vertexCache.nameCode = nameCode;
                vertexCache.bonePose = boneTransform;
                vertexCache.bonePose = boneTransform;
                vertexCache.bindPose = bindPose;
                break;
            }
        }

        private void PrepareBoneTexture(List<AnimationInfo> infoList)
        {
            int[] frames = new int[infoList.Count];
            for (int i = 0; i != infoList.Count; ++i)
            {
                AnimationInfo info = infoList[i];
                frames[i] = info.totalFrame;
            }
            CalculateTextureSize(out int count, out int textureWidth, out int textureHeight, frames);
            bakedBoneTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false);
            bakedBoneTexture.filterMode = FilterMode.Point;
        }

        private void CalculateTextureSize(out int textureCount, out int xRes, out int yRes, int[] frames, Transform[] bone = null)
        {
            if (frames.Length == 0)
            {
                textureCount = -1;
                xRes = 0;
                yRes = 0;
                return;
            }

            int blockWidth;//is const 4
            int blockHeight;//bones count
            if (bone != null)
            {
                boneCount = bone.Length;
                blockWidth = textureBlockWidth;
                blockHeight = boneCount;
            }
            else
            {
                blockWidth = textureBlockWidth;
                blockHeight = textureBlockHeight;
            }

            int destX = 0;
            int destY = 0;
            int count = 0;
            for (int oddMode = 0; oddMode < 2; ++oddMode)
            {
                for (int i = 0; i < stardardTextureSize.Length; ++i)
                {
                    int size = stardardTextureSize[i];
                    int blockCountEachLine = size / blockWidth;//is always int
                    int usedX = 0, usedY = 0;
                    for (int frameIndex = 0; frameIndex < frames.Length; ++frameIndex)
                    {
                        int frame = frames[frameIndex];
                        int currentLineEmptyBlockCount = (size - usedX) / blockWidth % blockCountEachLine;
                        if (oddMode == 0)
                            usedX = (usedX + frame % (blockCountEachLine * blockWidth)) % size;
                        else
                            usedX = (usedX + frame % blockCountEachLine * blockWidth) % size;

                        if (frame > currentLineEmptyBlockCount)
                        {
                            usedY += (frame - currentLineEmptyBlockCount) * blockHeight / blockCountEachLine;
                            usedY += currentLineEmptyBlockCount > 0 ? blockHeight : 0;
                        }
                    }
                    if (usedY <= size * 2)
                    {//success
                        count = 1;
                        if (oddMode == 1)
                        {
                            if (size > destX)
                                destX = size;
                            int tmpY = usedY <= size ? size : size * 2;
                            if (tmpY > destY)
                                destY = tmpY;
                        }
                        else
                        {
                            destX = size;
                            destY = usedY <= size ? size : size * 2;
                        }
                        break;
                    }
                }
            }

            textureCount = count;
            xRes = destX;
            yRes = destY;
        }
        private bool SetupAnimationTexture(List<AnimationInfo> infoList, Texture2D targetTexture)
        {
            pixelx = 0;
            pixely = 0;
            int preNameCode = generateObjectData[0].stateName;
            int width = targetTexture.width;
            int height = targetTexture.height;
            for (int i = 0; i != currentDataIndex; ++i)
            {
                BakedFrameInfo matrixData = generateObjectData[i];
                if (matrixData.boneMatrix == null)
                    continue;
                if (preNameCode != matrixData.stateName)
                {
                    preNameCode = matrixData.stateName;
                    int totalFrames = currentDataIndex - i;
                    for (int j = i; j < currentDataIndex; ++j)
                    {
                        if (preNameCode != generateObjectData[j].stateName)
                        {
                            totalFrames = j - i;
                            break;
                        }
                    }
                    int y = pixely;
                    int currentLineBlockCount = (width - pixelx) / textureBlockWidth % (width / textureBlockWidth);
                    totalFrames -= currentLineBlockCount;
                    if (totalFrames > 0)
                    {
                        int framesEachLine = width / textureBlockWidth;
                        y += (totalFrames / framesEachLine) * textureBlockHeight;
                        y += currentLineBlockCount > 0 ? textureBlockHeight : 0;
                        if (height < y + textureBlockHeight)
                        {
                            Debug.LogError("Animation Texture size generation error. Try different frame rate for animation baking");
                            return false;
                        }
                    }

                    foreach (var obj in infoList)
                    {
                        AnimationInfo info = obj;
                        if (info.animationNameHash == matrixData.stateName)
                        {
                            info.animationIndex = pixelx / textureBlockWidth + pixely / textureBlockHeight * targetTexture.width / textureBlockWidth;
                        }
                    }
                }
                if (matrixData.boneMatrix != null)
                {
                    Debug.Assert(pixely + textureBlockHeight <= targetTexture.height);
                    Color[] color = Convert2Color(matrixData.boneMatrix);
                    targetTexture.SetPixels(pixelx, pixely, textureBlockWidth, textureBlockHeight, color);
                    matrixData.frameIndex = pixelx / textureBlockWidth + pixely / textureBlockHeight * targetTexture.width / textureBlockWidth;
                    pixelx += textureBlockWidth;
                    if (pixelx + textureBlockWidth > targetTexture.width)
                    {
                        pixelx = 0;
                        pixely += textureBlockHeight;
                    }
                    if (pixely + textureBlockHeight > targetTexture.height)
                    {
                        Debug.Assert(generateObjectData[i + 1].stateName != matrixData.stateName);
                        pixelx = 0;
                        pixely = 0;
                        Debug.LogError("Animation Texture size generation error. Try different frame rate for animation baking");
                        return false;
                    }
                }
                else
                {
                    List<BakedFrameInfo> list = generateMatrixDataPool[matrixData.stateName];
                    BakedFrameInfo originalData = list[matrixData.boneListIndex];
                    matrixData.frameIndex = originalData.frameIndex;
                    Debug.LogError("Internal baking error. Not found BoneMatrix data!");
                    return false;

                }
            }
            currentDataIndex = 0;
            return true;
        }

        private static Matrix4x4[] CalculateSkinMatrix(Transform[] bonePose, Matrix4x4[] bindPose)
        {
            if (bonePose.Length == 0)
                return null;

            Transform root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            Matrix4x4 rootMat = root.worldToLocalMatrix;

            Matrix4x4[] matrix = new Matrix4x4[bonePose.Length];
            for (int i = 0; i != bonePose.Length; ++i)
            {
                matrix[i] = rootMat * bonePose[i].localToWorldMatrix * bindPose[i];
            }
            return matrix;
        }

        private static Color[] Convert2Color(Matrix4x4[] boneMatrix)
        {
            Color[] color = new Color[boneMatrix.Length * 4];
            int index = 0;
            foreach (var obj in boneMatrix)
            {
                color[index++] = obj.GetRow(0);
                color[index++] = obj.GetRow(1);
                color[index++] = obj.GetRow(2);
                color[index++] = obj.GetRow(3);
            }
            return color;
        }
    }
}
#endif