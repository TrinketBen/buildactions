using SuperUnityBuild.BuildTool;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SuperUnityBuild.BuildActions {
    public class ScriptRunner : BuildAction, IPreBuildAction, IPreBuildPerPlatformAction, IPostBuildAction, IPostBuildPerPlatformAction, IPreBuildPerPlatformActionCanConfigureEditor {
        [BuildTool.FilePath(false, true, "Select program/script to run.")]
        public string scriptPath = "";

        public string scriptArguments = "";

        string[] _keychains;
        int _selectedKeychainIndex;
        GUIContent[] _guiContent;
        int[] _options;


        public override void Execute() {
            RunScript(scriptPath, scriptArguments);
        }

        public override void PerBuildExecute(BuildReleaseType releaseType, BuildPlatform platform, BuildArchitecture architecture, BuildScriptingBackend scriptingBackend, BuildDistribution distribution, DateTime buildTime, ref BuildOptions options, string configKey, string buildPath) {
            var resolvedScriptPath = BuildProject.ResolvePath(scriptPath, releaseType, platform, architecture, scriptingBackend, distribution, buildTime);
            var outputPath = scriptArguments.Replace("$BUILDPATH", buildPath);
            var resolvedScriptArgs = BuildProject.ResolvePath(outputPath, releaseType, platform, architecture, scriptingBackend, distribution, buildTime);

            RunScript(resolvedScriptPath, resolvedScriptArgs);
        }



        protected override void DrawProperties(SerializedObject obj) {
            base.DrawProperties(obj);

            if(_keychains == null) {
                _keychains = BuildSettings.projectConfigurations.configSet.Where(kvp => kvp.Value.childKeys?.Length == 0).Select(kvp => kvp.Key).ToArray();
                _guiContent = _keychains.Select(bc => new GUIContent(bc)).ToArray();
                _options = Enumerable.Range(0, _keychains.Length).ToArray();
            }

            EditorGUILayout.BeginHorizontal();

            _selectedKeychainIndex = EditorGUILayout.IntPopup(_selectedKeychainIndex, _guiContent, _options);

            GUI.enabled = _selectedKeychainIndex >= 0;
            if(GUILayout.Button("Test", GUILayout.ExpandWidth(true))) {
                BuildReleaseType releaseType;
                BuildPlatform platform;
                BuildArchitecture architecture;
                BuildDistribution distribution;
                BuildScriptingBackend scriptingBackend;
                var configKey = _keychains[_selectedKeychainIndex];

                BuildSettings.projectConfigurations.ParseKeychain(configKey, out releaseType, out platform, out architecture, out scriptingBackend, out distribution);

                var resolvedScriptPath = BuildProject.ResolvePath(scriptPath, releaseType, platform, architecture, scriptingBackend, distribution, DateTime.Now);
                var resolvedScriptArgs = BuildProject.ResolvePath(scriptArguments, releaseType, platform, architecture, scriptingBackend, distribution, DateTime.Now);
                RunScript(resolvedScriptPath, resolvedScriptArgs);

            }

            EditorGUILayout.EndHorizontal();
        }

        void RunScript(string inScriptPath, string inArguments) {
            if(string.IsNullOrEmpty(inScriptPath)) {
                UnityEngine.Debug.LogError("Empty script path!");
                return;
            }
            
            inScriptPath = Path.GetFullPath(inScriptPath).Replace('\\', '/');;
            UnityEngine.Debug.Log($"About to start process {inScriptPath} with arguments: {inArguments}");

            ProcessStartInfo startInfo;
            if(Environment.OSVersion.Platform == PlatformID.Win32NT) {
                if(inScriptPath.EndsWith(".bat")) {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {inScriptPath} {inArguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    };
                }
                else {
                    startInfo = new ProcessStartInfo {
                        FileName = "wsl",
                        Arguments = $"/bin/bash {inScriptPath} {inArguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    };
                }
            }
            else {
                startInfo = new ProcessStartInfo {
                    FileName = "/bin/bash",
                    Arguments = $"{inScriptPath} {inArguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
            }

            try {
                using var process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                
                var output = process.StandardOutput.ReadToEnd();
                var error  = process.StandardError.ReadToEnd();
                if(!string.IsNullOrEmpty(output))
                    UnityEngine.Debug.Log(output);
                if(!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.Log(error);
                    
                process.WaitForExit();
                
                
            } catch(Exception exception) {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
