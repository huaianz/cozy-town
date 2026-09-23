using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildHelper
{
    private static string ProjectRoot
    {
        get { return Directory.GetParent(Application.dataPath).FullName; }
    }

    private static string[] GameScenes
    {
        get
        {
            return new[]
            {
                "Assets/Scenes/StartScene.unity",
                "Assets/Scenes/GameScene.unity"
            };
        }
    }

    [MenuItem("����/WebGL ���")]
    public static void BuildWebGL()
    {
        BuildWebGLInternal(false);
    }

    [MenuItem("����/WebGL ���������")]
    public static void BuildWebGLAndRun()
    {
        BuildWebGLInternal(true);
    }

    [MenuItem("����/Android APK")]
    public static void BuildAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog("ȱ�ٹ���ģ��", "û�а�װ Android ����ģ�顣���� Unity Hub ��� 2021.3.45f2c1 ���� Android Build Support���� SDK/NDK/JDK����", "֪����");
            return;
        }

        string outputDir = Path.Combine(ProjectRoot, "Builds/Android");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        EditorUserBuildSettings.buildAppBundle = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GameScenes,
            locationPathName = Path.Combine(outputDir, "CozyTown.apk"),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);

        Debug.Log("Android ������ɣ�" + outputDir);
    }

    private static void BuildWebGLInternal(bool autoRun)
    {
        ConfigureWebGL();

        string outputDir = Path.Combine(ProjectRoot, "Builds/WebGL");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GameScenes,
            locationPathName = outputDir,
            target = BuildTarget.WebGL,
            options = autoRun ? BuildOptions.AutoRunPlayer : BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);

        Debug.Log("WebGL ������ɣ�" + outputDir);
    }

    private static void ConfigureWebGL()
    {
        PlayerSettings.WebGL.template = "PROJECT:Mobile";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.memorySize = 256;
        PlayerSettings.WebGL.dataCaching = true;

        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;
    }
}