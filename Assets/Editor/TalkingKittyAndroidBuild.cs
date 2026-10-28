using System.IO;
using UnityEditor;
using UnityEngine;

namespace TalkingKitty.EditorTools
{
    public static class TalkingKittyAndroidBuild
    {
        const string PackageName = "com.huaihaoge.talkingkitty";
        const string ProductName = "TalkingKitty";
        const string CompanyName = "huaihaoge";

        [MenuItem("TalkingKitty/Configure Android Player Settings")]
        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Prefer not requesting internet for MVP; mic still required at runtime.
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            AssetDatabase.SaveAssets();
            Debug.Log($"[TalkingKittyAndroidBuild] Android player settings configured: {PackageName}, IL2CPP ARM64, min API 24, Portrait.");
        }

        static void ConfigureAndroidExternalTools()
        {
            var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var root = Path.Combine(home, "UnityAndroidToolchain");
            var jdk = Path.Combine(root, "OpenJDK");
            var sdk = Path.Combine(root, "sdk");
            var ndk = Path.Combine(root, "ndk");

            // Prefer first ndk-* folder if present.
            if (Directory.Exists(ndk))
            {
                var nested = Directory.GetDirectories(ndk);
                if (nested.Length == 1)
                    ndk = nested[0];
            }

            if (Directory.Exists(jdk))
            {
                UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = jdk;
                Debug.Log($"[TalkingKittyAndroidBuild] JDK={jdk}");
            }

            if (Directory.Exists(sdk))
            {
                UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdk;
                Debug.Log($"[TalkingKittyAndroidBuild] SDK={sdk}");
            }

            if (Directory.Exists(ndk) && File.Exists(Path.Combine(ndk, "ndk-build")))
            {
                UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = ndk;
                Debug.Log($"[TalkingKittyAndroidBuild] NDK={ndk}");
            }
        }

        [MenuItem("TalkingKitty/Build Android APK")]
        public static void BuildAndroidApk()
        {
            ConfigureAndroidPlayerSettings();
            ConfigureAndroidExternalTools();

            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(outDir);
            var apkPath = Path.Combine(outDir, "TalkingKitty.apk");

            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0 || string.IsNullOrEmpty(scenes[0].path))
            {
                Debug.LogError("[TalkingKittyAndroidBuild] No scenes in Build Settings. Run TalkingKitty/Build Main Scene first.");
                throw new System.InvalidOperationException("Missing build scenes");
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { scenes[0].path },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4
            };

            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log($"[TalkingKittyAndroidBuild] Result={summary.result} path={apkPath} errors={summary.totalErrors} size={summary.totalSize}");
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"Android build failed: {summary.result}");
            }
        }
    }
}
