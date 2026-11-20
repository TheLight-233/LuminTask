using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SmokeBuild
{
    public static void Run()
    {
        var arguments = Environment.GetCommandLineArgs();
        int outputIndex = Array.IndexOf(arguments, "-luminPlayer");
        if (outputIndex < 0 || outputIndex == arguments.Length - 1)
            throw new ArgumentException("Pass -luminPlayer followed by the executable path.");

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Standalone, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        new GameObject("LuminTask smoke").AddComponent<SmokeEntry>();
        const string scenePath = "Assets/LuminSmoke.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        string output = arguments[outputIndex + 1];
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Unity player build failed: " + report.summary.result);
    }
}
