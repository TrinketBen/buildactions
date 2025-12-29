using SuperUnityBuild.BuildTool;
using System;
using UnityEditor;
using UnityEditor.Build;

namespace SuperUnityBuild.BuildActions
{
    public sealed class OverrideBundleIdentifier : BuildAction, IPreBuildPerPlatformAction
    {
        public string           bundleIdentifier = string.Empty;
        public BuildTargetGroup buildTarget = BuildTargetGroup.Unknown;

        public override void PerBuildExecute(
            BuildReleaseType releaseType,
            BuildPlatform platform,
            BuildArchitecture architecture,
            BuildScriptingBackend scriptingBackend,
            BuildDistribution distribution,
            DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
        {
            if(platform.targetGroup == buildTarget) {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTarget), bundleIdentifier);
            }
        }

        protected override void DrawProperties(SerializedObject obj)
        {
            if (obj != null)
            {
                EditorGUILayout.PropertyField(obj.FindProperty("bundleIdentifier"));
                EditorGUILayout.PropertyField(obj.FindProperty("buildTarget"));
            }
        }
    }
}
