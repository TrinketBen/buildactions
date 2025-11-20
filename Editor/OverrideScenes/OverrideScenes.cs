using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SuperUnityBuild.BuildTool; // Required for BuildAction

namespace SuperUnityBuild.BuildActions
{
    public class OverrideScenes : BuildAction, IPreBuildPerPlatformAction
    {
        [Tooltip("The scenes to use for this specific build configuration.")]
        public List<SceneAsset> _scenesOverride = new();

        public override void PerBuildExecute(
            BuildReleaseType releaseType,
            BuildPlatform platform,
            BuildArchitecture architecture,
            BuildScriptingBackend scriptingBackend,
            BuildDistribution distribution,
            DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
        {
            if (_scenesOverride == null || _scenesOverride.Count == 0)
            {
                Debug.LogWarning("[OverrideScenesAction] No scenes assigned. Skipping scene override.");
                return;
            }

            List<string> scenePaths = new();
            foreach(var sceneAsset in _scenesOverride) {
                if (sceneAsset == null) {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(sceneAsset);
                if(!string.IsNullOrEmpty(path)) {
                    scenePaths.Add(path);
                }
            }

            var newSettings = scenePaths
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            EditorBuildSettings.scenes = newSettings;

            Debug.Log($"[OverrideScenesAction] Successfully swapped build list to {newSettings.Length} scenes.");
        }
    }
}
