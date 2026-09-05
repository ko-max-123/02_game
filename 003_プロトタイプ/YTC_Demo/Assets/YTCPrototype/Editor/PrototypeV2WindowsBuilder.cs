using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YTCPrototype.Editor
{
    public static class PrototypeV2WindowsBuilder
    {
        private const string MenuPath = "YTC Prototype V2/Build Windows Standalone V2";
        private const string ExecutableName = "YTC_CombatDemo_V2.exe";

        [MenuItem(MenuPath)]
        public static void BuildFromMenu()
        {
            BuildWindowsStandalone();
            EditorUtility.DisplayDialog(
                "YTC Windows Build V2",
                "V2 build completed in 003_\u30d7\u30ed\u30c8\u30bf\u30a4\u30d7/YTC_StandalonePrototype_V2/.",
                "OK");
        }

        public static void BuildFromCommandLine()
        {
            BuildWindowsStandalone();
            Debug.Log("YTC V2 Windows standalone build command completed.");
        }

        private static void BuildWindowsStandalone()
        {
            PrototypeV2SceneBuilder.BuildOrRefreshScene();

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            string prototypeRoot = Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Prototype root could not be resolved.");
            string outputRoot = Path.Combine(prototypeRoot, "YTC_StandalonePrototype_V2");
            string executablePath = Path.Combine(outputRoot, ExecutableName);
            Directory.CreateDirectory(outputRoot);

            string originalCompanyName = PlayerSettings.companyName;
            string originalProductName = PlayerSettings.productName;
            int originalScreenWidth = PlayerSettings.defaultScreenWidth;
            int originalScreenHeight = PlayerSettings.defaultScreenHeight;
            FullScreenMode originalFullScreenMode = PlayerSettings.fullScreenMode;
            bool originalResizableWindow = PlayerSettings.resizableWindow;
            BuildReport report;
            try
            {
                PlayerSettings.companyName = "YTC Prototype Team";
                PlayerSettings.productName = "YTC Combat Demo V2";
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;

                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { PrototypeV2SceneBuilder.ScenePath },
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
            }
            finally
            {
                PlayerSettings.companyName = originalCompanyName;
                PlayerSettings.productName = originalProductName;
                PlayerSettings.defaultScreenWidth = originalScreenWidth;
                PlayerSettings.defaultScreenHeight = originalScreenHeight;
                PlayerSettings.fullScreenMode = originalFullScreenMode;
                PlayerSettings.resizableWindow = originalResizableWindow;
            }
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows V2 build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }

            foreach (string debugDirectory in Directory.EnumerateDirectories(
                outputRoot,
                "*_BurstDebugInformation_DoNotShip",
                SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(debugDirectory, true);
            }

            File.WriteAllText(
                Path.Combine(outputRoot, "BUILD_INFO.txt"),
                "YTC STANDALONE COMBAT PROTOTYPE V2\n"
                + $"Built: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n"
                + $"Unity: {Application.unityVersion}\n"
                + "Import: glTFast direct GLB / Mecanim / Generic rig / no root motion\n"
                + $"Executable: {ExecutableName}\n"
                + "Controls: A/D move, Space jump/hold jet, Left Click or J fire, R restart, Esc quit\n"
                + "Combat: finite projectiles with locked enemy telegraphs; shots can be dodged.\n");
            File.WriteAllText(
                Path.Combine(outputRoot, "README.txt"),
                "YTC STANDALONE COMBAT PROTOTYPE V2\n\n"
                + "Copy this entire folder to a Windows PC, then double-click YTC_CombatDemo_V2.exe.\n"
                + "Unity Editor and Unity Hub are not required. Keep the EXE, Data folder, DLL files, and runtime folders together.\n\n"
                + "A/D: Move, Space: Jump/hold Jet, Left Mouse/J: Fire, R: Restart, Esc: Quit\n"
                + "Enemy aim locks when the dashed warning appears. Move or jump before the projectile arrives.\n");

            Debug.Log(
                $"YTC V2 Windows build succeeded. Path={executablePath}, "
                + $"Size={report.summary.totalSize} bytes, Time={report.summary.totalTime}");
        }
    }
}
