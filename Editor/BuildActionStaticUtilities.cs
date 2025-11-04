namespace SuperUnityBuild.BuildActions.buildactions.Editor
{
    using BuildTool;
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class BuildActionStaticUtilities
    {
        static int          _selectedKeychainIndex;
        static string[]     _keychains;
        static GUIContent[] _guiContent;
        static int[]        _options;

        public struct BuildKeychain
        {
            public BuildReleaseType      ReleaseType;
            public BuildPlatform         Platform;
            public BuildArchitecture     Architecture;
            public BuildDistribution     Distribution;
            public BuildScriptingBackend ScriptingBackend;
        }

        public static void DrawTestButton(Action<BuildKeychain> inAction)
        {
            void RefreshKeychains()
            {
                if (!(_keychains?.Any()).GetValueOrDefault())
                {
                    _keychains = BuildSettings.projectConfigurations.configSet
                        .Where(kvp => kvp.Value.childKeys == null || kvp.Value.childKeys.Length == 0)
                        .Select(kvp => kvp.Key).ToArray();
                    _guiContent = _keychains.Select(bc => new GUIContent(bc)).ToArray();
                    _options = Enumerable.Range(0, _keychains.Length).ToArray();
                }
            }

            RefreshKeychains();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            _selectedKeychainIndex = EditorGUILayout.IntPopup(_selectedKeychainIndex, _guiContent, _options);

            var wasEnabled = GUI.enabled;
            GUI.enabled = _selectedKeychainIndex >= 0;
            if(GUILayout.Button("Test", GUILayout.ExpandWidth(true))) {
                var configKey = _keychains[_selectedKeychainIndex];

                var keychain = new BuildKeychain();
                BuildSettings.projectConfigurations.ParseKeychain(configKey,
                    out keychain.ReleaseType,
                    out keychain.Platform,
                    out keychain.Architecture,
                    out keychain.ScriptingBackend,
                    out keychain.Distribution
                );

                inAction?.Invoke(keychain);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh keychains", GUILayout.ExpandWidth(true)))
            {
                _keychains = null;
                _guiContent = null;
                _options = null;
                RefreshKeychains();
            }

            EditorGUILayout.EndVertical();

            GUI.enabled = wasEnabled;
        }
    }
}
