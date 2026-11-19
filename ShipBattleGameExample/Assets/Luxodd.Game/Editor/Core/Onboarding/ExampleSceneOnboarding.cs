#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luxodd.Game.Editor
{
    [InitializeOnLoad]
    internal static class ExampleSceneOnboarding
    {
        private const string PreferenceKeyPrefix = "Luxodd.UnityPlugin.Onboarding.ExampleSceneShown.";
        private const string ExampleScenePath = "Assets/Luxodd.Game/Example/Scenes/ExampleScene.unity";

        private static bool _isWaitingForEditorReady;

        static ExampleSceneOnboarding()
        {
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += TryShowOnboarding;
        }

        [MenuItem("Luxodd/Open Example Scene")]
        private static void OpenExampleSceneFromMenu()
        {
            OpenExampleScene();
        }

        private static void TryShowOnboarding()
        {
            EditorApplication.delayCall -= TryShowOnboarding;

            if (HasShownOnboarding())
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                RegisterEditorReadyCallback();
                return;
            }

            ShowOnboardingDialog();
        }

        private static void RegisterEditorReadyCallback()
        {
            if (_isWaitingForEditorReady)
                return;

            _isWaitingForEditorReady = true;
            EditorApplication.update += WaitForEditorReady;
        }

        private static void WaitForEditorReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= WaitForEditorReady;
            _isWaitingForEditorReady = false;

            if (!HasShownOnboarding())
                ShowOnboardingDialog();
        }

        private static void ShowOnboardingDialog()
        {
            if (HasShownOnboarding())
                return;

            EditorPrefs.SetBool(GetPreferenceKey(), true);

            bool shouldOpenExampleScene = EditorUtility.DisplayDialog(
                "Luxodd Unity Plugin",
                "The Luxodd Unity Plugin is ready to use.\n\n" +
                "Would you like to open the Example Scene to verify the setup and explore the available features?",
                "Open Example Scene",
                "Later");

            if (shouldOpenExampleScene)
                OpenExampleScene();
        }

        private static bool HasShownOnboarding()
        {
            return EditorPrefs.GetBool(GetPreferenceKey(), false);
        }

        private static string GetPreferenceKey()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string normalizedProjectRoot = NormalizeProjectRoot(projectRoot);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] projectPathBytes = Encoding.UTF8.GetBytes(normalizedProjectRoot);
                byte[] projectHashBytes = sha256.ComputeHash(projectPathBytes);
                string projectHash = BitConverter.ToString(projectHashBytes)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();

                return PreferenceKeyPrefix + projectHash;
            }
        }

        private static string NormalizeProjectRoot(string projectRoot)
        {
            string normalizedPath = projectRoot.Replace('\\', '/');
            string pathRoot = Path.GetPathRoot(projectRoot).Replace('\\', '/');

            while (normalizedPath.Length > pathRoot.Length && normalizedPath.EndsWith("/", StringComparison.Ordinal))
                normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - 1);

            if (Path.DirectorySeparatorChar == '\\')
                normalizedPath = normalizedPath.ToUpperInvariant();

            return normalizedPath;
        }

        private static void OpenExampleScene()
        {
            SceneAsset exampleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ExampleScenePath);
            if (exampleScene == null)
            {
                ShowSceneError(
                    "The Luxodd Example Scene could not be found at:\n\n" + ExampleScenePath +
                    "\n\nRestore the scene file before trying again.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                Scene openedScene = EditorSceneManager.OpenScene(ExampleScenePath, OpenSceneMode.Single);
                if (!openedScene.IsValid() || !openedScene.isLoaded)
                {
                    ShowSceneError("The Luxodd Example Scene could not be loaded:\n\n" + ExampleScenePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowSceneError(
                    "The Luxodd Example Scene could not be loaded:\n\n" + exception.Message);
            }
        }

        private static void ShowSceneError(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Luxodd Unity Plugin", message, "OK");
        }
    }
}
#endif
