using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YTCPrototype.Editor
{
    public static class PrototypeWindowsBuilder
    {
        private const string MenuPath = "YTC Prototype/Build Windows Standalone";
        private const string ScenePath = "Assets/YTCPrototype/Scenes/YTC_Demo.unity";
        private const string ExecutableName = "YTC_CombatDemo.exe";

        [MenuItem(MenuPath)]
        public static void BuildFromMenu()
        {
            BuildWindowsStandalone();
            EditorUtility.DisplayDialog(
                "YTC Windows Build",
                "Standalone build completed in 003_プロトタイプ/YTC_StandalonePrototype/.",
                "OK");
        }

        public static void BuildFromCommandLine()
        {
            BuildWindowsStandalone();
            Debug.Log("YTC Windows standalone build command completed.");
        }

        private static void BuildWindowsStandalone()
        {
            PrototypeSceneBuilder.BuildOrRefreshScene();

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            string prototypeRoot = Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Prototype root could not be resolved.");
            string outputRoot = Path.Combine(prototypeRoot, "YTC_StandalonePrototype");
            string executablePath = Path.Combine(outputRoot, ExecutableName);
            Directory.CreateDirectory(outputRoot);

            PlayerSettings.companyName = "YTC Prototype Team";
            PlayerSettings.productName = "YTC Combat Demo";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }

            string buildInfo =
                "YTC Standalone Combat Prototype\n"
                + $"Built: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n"
                + $"Unity: {Application.unityVersion}\n"
                + $"Executable: {ExecutableName}\n"
                + "Controls: WASD, Space jump/hold jet, Left Click or J fire, R restart, Esc quit\n";
            File.WriteAllText(Path.Combine(outputRoot, "BUILD_INFO.txt"), buildInfo);

            string asciiReadme =
                "YTC STANDALONE COMBAT PROTOTYPE\n\n"
                + "HOW TO START\n"
                + "1. Copy this entire YTC_StandalonePrototype folder to a Windows PC.\n"
                + "2. Double-click YTC_CombatDemo.exe.\n"
                + "3. Unity Editor and Unity Hub are not required.\n\n"
                + "CONTROLS\n"
                + "A/D: Move, W/S: Depth lane, Space: Jump/hold Jet, "
                + "Left Mouse/J: Fire, R: Restart, Esc: Quit\n\n"
                + "Keep the EXE, Data folder, DLL files, and runtime directories together.\n";
            File.WriteAllText(Path.Combine(outputRoot, "README.txt"), asciiReadme);

            Debug.Log(
                $"YTC Windows build succeeded. Path={executablePath}, "
                + $"Size={report.summary.totalSize} bytes, Time={report.summary.totalTime}");
        }
    }
}
