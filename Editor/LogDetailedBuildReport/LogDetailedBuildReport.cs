using System;
using SuperUnityBuild.BuildTool;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SuperUnityBuild.BuildActions
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Networking;

    public sealed class LogDetailedBuildReport : BuildAction, IPostBuildPerPlatformAction {

        public string DiscordWebhookUrl;
        public bool   IncludeBuildStepTimings;

        [System.Serializable]
        private class DiscordWebhookPayload
        {
            public string content;
        }

        public override void PerBuildExecute(
            BuildReleaseType releaseType,
            BuildPlatform platform,
            BuildArchitecture architecture,
            BuildScriptingBackend scriptingBackend,
            BuildDistribution distribution,
            DateTime buildTime, ref BuildOptions options, string configKey, string buildPath) {

            if (string.IsNullOrEmpty(DiscordWebhookUrl)) {
                Debug.LogWarning("Webhook URL not set.");
                return;
            }

            var report = BuildReport.GetLatestReport();
            if (report == null) {
                return;
            }

            SendToDiscord(GetSummaryString(report));
        }

        async void SendToDiscord(string message) {
            var payload = new DiscordWebhookPayload { content = message };
            var json = JsonUtility.ToJson(payload);
            UnityWebRequest request = new(DiscordWebhookUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            var async = request.SendWebRequest();
            async.completed += x =>
            {
                if(request.result != UnityWebRequest.Result.Success)
                    Debug.LogError("[DiscordBuildReport] Failed to send: " + request.error);
                else
                    Debug.Log("[DiscordBuildReport] Sent to Discord successfully.");

                request.Dispose();
            };
            await async;
        }

        protected override void DrawProperties(SerializedObject obj) {
            base.DrawProperties(obj);
            if(GUILayout.Button("Test", GUILayout.ExpandWidth(true))) {
                SendToDiscord("Test");
            }
        }

        string GetSummaryString(BuildReport report)
        {
            if (report == null)
                return "No BuildReport provided.";

            var sb = new StringBuilder();
            var summary = report.summary;

            // Include SuperUnityBuild BuildConstants if available
            Type buildConstantsType = FindBuildConstantsType();
            if (buildConstantsType != null)
            {
                sb.AppendLine("Build Constants:");
                foreach (var field in buildConstantsType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    try
                    {
                        object value = field.GetValue(null);
                        if (value == null)
                            continue;

                        string strValue = value.ToString();
                        if (string.IsNullOrWhiteSpace(strValue))
                            continue;

                        sb.AppendLine($"- {field.Name}: {strValue}");
                    }
                    catch
                    {
                        // Ignore any inaccessible or problematic fields
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine("Build Summary:");
            sb.AppendLine($"- Machine: {Environment.MachineName}");
            sb.AppendLine($"- Unity Version: {Application.unityVersion}");
            sb.AppendLine($"- Platform: {summary.platform}");
            sb.AppendLine($"- Output Path: {summary.outputPath}");
            sb.AppendLine($"- Start Time: {summary.buildStartedAt:G}");
            sb.AppendLine($"- End Time: {summary.buildEndedAt:G}");
            sb.AppendLine($"- Duration: {summary.totalTime:g}");
            sb.AppendLine($"- Result: {summary.result}");

            if (summary.totalSize > 0)
                sb.AppendLine($"- Total Size: {FormatSize(summary.totalSize)}");

            sb.AppendLine($"- Warnings: {summary.totalWarnings}");
            sb.AppendLine($"- Errors: {summary.totalErrors}");

            // Compiler or build messages
            var errorMessages = report.steps
                .SelectMany(s => s.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.content)
                .Distinct()
                .ToList();

            if (errorMessages.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Compiler / Build Errors:");
                foreach (var msg in errorMessages.Take(10))
                    sb.AppendLine($"- {msg}");

                if (errorMessages.Count > 10)
                    sb.AppendLine($"(and {errorMessages.Count - 10} more...)");
            }

            // Optional build step timings
            if(IncludeBuildStepTimings) {
                var buildSteps = report.steps?.Select(s => $"{s.name} ({s.duration.TotalSeconds:F1}s)").ToList();
                if (buildSteps != null && buildSteps.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Build Steps:");
                    foreach (var step in buildSteps)
                        sb.AppendLine($"- {step}");
                }
            }

            return sb.ToString();
        }

        private static Type FindBuildConstantsType()
        {
            // Try to find the MonoScript via AssetDatabase
            string[] guids = AssetDatabase.FindAssets("BuildConstants t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null)
                    continue;

                Type type = script.GetClass();
                if (type != null && type.Name == "BuildConstants")
                    return type;
            }

            // Fallback: attempt to find by scanning loaded assemblies
            var fallbackType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "BuildConstants");
            return fallbackType;
        }

        private static string FormatSize(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
