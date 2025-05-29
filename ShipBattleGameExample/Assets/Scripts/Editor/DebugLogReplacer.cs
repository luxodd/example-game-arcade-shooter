using System.IO;
using System.Text.RegularExpressions;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
    public class DebugLogReplacer : EditorWindow
    {
        private string folderPath = "Assets";

        [MenuItem("Tools/Replace Debug.Log with LoggerHelper.Log")]
        public static void ShowWindow()
        {
            GetWindow(typeof(DebugLogReplacer), false, "DebugLog Replacer");
        }

        void OnGUI()
        {
            GUILayout.Label("Replace Debug.Log* with LoggerHelper.Log*", EditorStyles.boldLabel);
            folderPath = EditorGUILayout.TextField("Folder Path:", folderPath);

            if (GUILayout.Button("Start Replace"))
            {
                ReplaceLogsInFolder(folderPath);
                EditorUtility.DisplayDialog("Done", "Replacement Complete!", "OK");
            }
        }

        static void ReplaceLogsInFolder(string folder)
        {
            var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
            int count = 0;
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);

                // basic replacement
                string replaced = text;
                replaced = Regex.Replace(replaced, @"\bDebug\.LogWarning\s*\(", "LoggerHelper.LogWarning(", RegexOptions.Multiline);
                replaced = Regex.Replace(replaced, @"\bDebug\.LogError\s*\(", "LoggerHelper.LogError(", RegexOptions.Multiline);
                replaced = Regex.Replace(replaced, @"\bDebug\.Log\s*\(", "LoggerHelper.Log(", RegexOptions.Multiline);

                // If replacement wrote it to file
                if (replaced != text)
                {
                    File.WriteAllText(file, replaced);
                    count++;
                }
            }
            LoggerHelper.Log($"Was replaced files: {count}");
        }
    }
}