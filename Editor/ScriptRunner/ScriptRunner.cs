using SuperUnityBuild.BuildTool;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SuperUnityBuild.BuildActions {
    using buildactions.Editor;

    public class ScriptRunner : BuildAction, IPreBuildAction, IPreBuildPerPlatformAction, IPostBuildAction, IPostBuildPerPlatformAction, IPreBuildPerPlatformActionCanConfigureEditor {
        [BuildTool.FilePath(false, true, "Select program/script to run.")]
        public string scriptPath = "";

        public string scriptArguments = "";

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
            BuildActionStaticUtilities.DrawTestButton(x => {
                var resolvedScriptPath = BuildProject.ResolvePath(scriptPath,
                    x.ReleaseType,
                    x.Platform,
                    x.Architecture,
                    x.ScriptingBackend,
                    x.Distribution,
                    DateTime.Now
                );
                var resolvedScriptArgs = BuildProject.ResolvePath(scriptArguments,
                    x.ReleaseType,
                    x.Platform,
                    x.Architecture,
                    x.ScriptingBackend,
                    x.Distribution,
                    DateTime.Now
                );
                RunScript(resolvedScriptPath, resolvedScriptArgs);
            });
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
