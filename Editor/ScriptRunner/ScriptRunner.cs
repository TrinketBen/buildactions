using SuperUnityBuild.BuildTool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SuperUnityBuild.BuildActions
{
    public class ScriptRunner : BuildAction, IPreBuildAction, IPreBuildPerPlatformAction, IPostBuildAction, IPostBuildPerPlatformAction, IPreBuildPerPlatformActionCanConfigureEditor
    {
        [BuildTool.FilePath(false, true, "Select program/script to run.")]
        public string scriptPath = "";

        public string scriptArguments = "";

        string[] _keychains;
        int _selectedKeychainIndex;
        GUIContent[] _guiContent;
        int[] _options;


        public override void Execute()
        {
            RunScript(scriptPath, scriptArguments);
        }

        public override void PerBuildExecute(BuildReleaseType releaseType, BuildPlatform platform, BuildArchitecture architecture, BuildScriptingBackend scriptingBackend, BuildDistribution distribution, DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
        {
            var resolvedScriptPath = BuildProject.ResolvePath(scriptPath, releaseType, platform, architecture, scriptingBackend, distribution, buildTime);
            var outputPath         = scriptArguments.Replace("$BUILDPATH", buildPath);
            var resolvedScriptArgs = BuildProject.ResolvePath(outputPath, releaseType, platform, architecture, scriptingBackend, distribution, buildTime);

            RunScript(resolvedScriptPath, resolvedScriptArgs);
        }

        private void RunScript(string scriptPath, string arguments)
        {
            if (string.IsNullOrEmpty(scriptPath))
            {
                UnityEngine.Debug.LogError("Empty script path!");
                return;
            }

            var startInfo = new ProcessStartInfo {
                FileName = Path.GetFullPath(scriptPath)
            };

            if (!string.IsNullOrEmpty(arguments))
                startInfo.Arguments = arguments;

            UnityEngine.Debug.Log($"About to start process {scriptPath} with arguments: {arguments}");
            var process = Process.Start(startInfo);
            if(process == null)
                return;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (_, args) => UnityEngine.Debug.Log($"{scriptPath}: {args.Data}");
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        protected override void DrawProperties(SerializedObject obj)
        {
            base.DrawProperties(obj);

            if (_keychains == null)
            {
                _keychains = BuildSettings.projectConfigurations.BuildAllKeychains();
                _guiContent = _keychains.Select(bc => new GUIContent(bc)).ToArray();
                _options = Enumerable.Range(0, _keychains.Length).ToArray();
            }

            EditorGUILayout.BeginHorizontal();
            
            _selectedKeychainIndex = EditorGUILayout.IntPopup(_selectedKeychainIndex, _guiContent, _options);

            GUI.enabled = _selectedKeychainIndex >= 0;
            if (GUILayout.Button("Test", GUILayout.ExpandWidth(true)))
            {
                BuildReleaseType releaseType;
                BuildPlatform platform;
                BuildArchitecture architecture;
                BuildDistribution distribution;
                BuildScriptingBackend scriptingBackend;
                string configKey = _keychains[_selectedKeychainIndex];

                BuildSettings.projectConfigurations.ParseKeychain(configKey, out releaseType, out platform, out architecture, out scriptingBackend, out distribution);

                string resolvedScriptPath = BuildProject.ResolvePath(scriptPath, releaseType, platform, architecture, scriptingBackend, distribution, DateTime.Now);
                string resolvedScriptArgs = BuildProject.ResolvePath(scriptArguments, releaseType, platform, architecture, scriptingBackend, distribution, DateTime.Now);
                RunScript(resolvedScriptPath, resolvedScriptArgs);

            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
