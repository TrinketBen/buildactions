using SuperUnityBuild.BuildTool;
using System;
using UnityEditor;
using UnityEditor.Build;

namespace SuperUnityBuild.BuildActions
{
    public sealed class OverrideBundleIdentifier : BuildAction, IPreBuildPerPlatformAction
    {
        public string           bundleIdentifier = string.Empty;

        public override void PerBuildExecute(
            BuildReleaseType releaseType,
            BuildPlatform platform,
            BuildArchitecture architecture,
            BuildScriptingBackend scriptingBackend,
            BuildDistribution distribution,
            DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(platform.targetGroup), bundleIdentifier);
        }

        protected override void DrawProperties(SerializedObject obj)
        {
            if (obj != null)
            {
                EditorGUILayout.PropertyField(obj.FindProperty("bundleIdentifier"));
            }
        }
    }
}
