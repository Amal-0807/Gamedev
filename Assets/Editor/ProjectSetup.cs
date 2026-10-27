using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace BlockBlaster.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/BlockBlaster.unity";
        const string XRPath = "Assets/XR/GeneralSettings.asset";

        [InitializeOnLoadMethod]
        static void FirstOpen()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(XRPath)) Configure();
            };
        }

        [MenuItem("Block Blaster/Configure project")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/XR");
            AssetDatabase.Refresh();
            PlayerSettings.companyName = "Amal & Basil";
            PlayerSettings.productName = "Block Blaster AR";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.amalbasil.blockblaster");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.runInBackground = false;
            // Keep the native legacy touch API used by the one-finger controls.
            var playerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerAssets.Length > 0)
            {
                var serialized = new SerializedObject(playerAssets[0]);
                var input = serialized.FindProperty("activeInputHandler");
                if (input != null) { input.intValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            }

            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget targets))
            {
                targets = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XRPath);
                if (targets == null)
                {
                    targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(targets, XRPath);
                }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, targets, true);
            }
            if (!targets.HasSettingsForBuildTarget(BuildTargetGroup.Android)) targets.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!targets.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) targets.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var settings = targets.SettingsForBuildTarget(BuildTargetGroup.Android);
            settings.InitManagerOnStart = false;
            settings.AssignedSettings.automaticLoading = false;
            settings.AssignedSettings.automaticRunning = false;
            bool hasARCore = false;
            foreach (var loader in settings.AssignedSettings.activeLoaders)
                if (loader != null && loader.GetType().FullName == "UnityEngine.XR.ARCore.ARCoreLoader") hasARCore = true;
            if (!hasARCore && !XRPackageMetadataStore.AssignLoader(settings.AssignedSettings, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android))
                throw new InvalidOperationException("Could not assign ARCore loader. Check Package Manager errors.");
            EditorUtility.SetDirty(settings.AssignedSettings);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(targets);

            var arCore = ARCoreSettings.GetOrCreateSettings();
            arCore.requirement = ARCoreSettings.Requirement.Optional;
            arCore.depth = ARCoreSettings.Requirement.Optional;
            if (!AssetDatabase.Contains(arCore)) AssetDatabase.CreateAsset(arCore, "Assets/XR/ARCoreSettings.asset");
            ARCoreSettings.currentSettings = arCore;
            EditorUtility.SetDirty(arCore);
            EnsureScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Block Blaster configured: portrait Android ARM64, AR Optional, Practice mode, and entry scene.");
        }

        static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject("Block Blaster", typeof(GameManager));
            SceneManager.MoveGameObjectToScene(root, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        [MenuItem("Block Blaster/Open game scene")]
        public static void OpenGame()
        {
            Configure();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Block Blaster/Build Android APK")]
        public static void BuildAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch to Android in File > Build Profiles first, or run scripts/build-android.sh.");
            Configure();
            GameSelfCheck.RunRules();
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = BuildTarget.Android,
                locationPathName = "Builds/BlockBlaster.apk",
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            Debug.Log("APK built at Builds/BlockBlaster.apk (" + report.summary.totalSize + " bytes).");
        }
    }
}
