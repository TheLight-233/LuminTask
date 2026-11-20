using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BenchBuild
{
    public static void Run()
    {
        var arguments = Environment.GetCommandLineArgs();
        string output = null;
        var target = Array.IndexOf(arguments, "-bench32") >= 0
            ? BuildTarget.StandaloneWindows : BuildTarget.StandaloneWindows64;
        if (Array.IndexOf(arguments, "-benchWebGL") >= 0) target = BuildTarget.WebGL;
        var namedTarget = target == BuildTarget.WebGL ? NamedBuildTarget.WebGL : NamedBuildTarget.Standalone;
        for (int i = 0; i + 1 < arguments.Length; i++)
            if (arguments[i] == "-benchBuildOut") output = arguments[i + 1];
        if (output == null) throw new ArgumentException("-benchBuildOut is required");

        PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(namedTarget, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(namedTarget, Il2CppCompilerConfiguration.Release);
        PlayerSettings.allowUnsafeCode = false;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Benchmark").AddComponent<BenchEntry>();
        const string scenePath = "Assets/BenchScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = output,
            target = target,
            options = BuildOptions.CleanBuildCache,
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("IL2CPP build failed: " + report.summary.result);
        File.WriteAllText(output + ".build.txt", "seconds=" + report.summary.totalTime.TotalSeconds +
            "\nbytes=" + report.summary.totalSize + "\nresult=" + report.summary.result);
    }
}
