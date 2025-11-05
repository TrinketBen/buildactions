using SuperUnityBuild.BuildTool;
using System;
using UnityEditor;
using UnityEngine;

namespace SuperUnityBuild.BuildActions
{
    public sealed class SetAndroidKeystore : BuildAction, IPreBuildPerPlatformAction
    {
        string androidKeystorePath     = "ANDROID_KEYSTORE_PATH";
        string androidKeystorePassword = "ANDROID_KEYSTORE_PASSWORD";
        string androidKeyAlias         = "ANDROID_KEY_ALIAS";
        string androidKeyPassword      = "ANDROID_KEY_PASSWORD";

        public override void PerBuildExecute(
            BuildReleaseType releaseType,
            BuildPlatform platform,
            BuildArchitecture architecture,
            BuildScriptingBackend scriptingBackend,
            BuildDistribution distribution,
            DateTime buildTime, ref BuildOptions options, string configKey, string buildPath)
        {
            if(platform.targetGroup == BuildTargetGroup.Android) {
                SetKeystoreInfo();
            }
        }

        protected override void DrawProperties(SerializedObject obj)
        {
            if (obj != null)
            {
                EditorGUILayout.PropertyField(obj.FindProperty("androidKeystorePath"));
                EditorGUILayout.PropertyField(obj.FindProperty("androidKeystorePassword"));
                EditorGUILayout.PropertyField(obj.FindProperty("androidKeyAlias"));
                EditorGUILayout.PropertyField(obj.FindProperty("androidKeyPassword"));
            }
        }

        void SetKeystoreInfo()
        {
            string keystorePath = Environment.GetEnvironmentVariable(androidKeystorePath);
            string keystorePass = Environment.GetEnvironmentVariable(androidKeystorePassword);
            string keyAlias     = Environment.GetEnvironmentVariable(androidKeyAlias);
            string keyPass      = Environment.GetEnvironmentVariable(androidKeyPassword);
            string report       = $"keystorePath: {keystorePath}, keystorePass: {keystorePass}, keyAlias: {keyAlias}, keyPass: {keyPass}";

            Debug.Log("Android keystore info: " + report);

            if(   string.IsNullOrEmpty(keystorePath)
               || string.IsNullOrEmpty(keystorePass)
               || string.IsNullOrEmpty(keyAlias)
               || string.IsNullOrEmpty(keyPass)
            ) {
                Debug.LogError("Android keystore credentials not set in environment variables.");
                return;
            }

            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keyAlias;
            PlayerSettings.Android.keyaliasPass = keyPass;
        }
    }
}
