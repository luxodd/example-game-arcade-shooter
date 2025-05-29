using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Utils.Editor
{
    public static class DeleteSaveHelper
    {
        //private static bool IsMacOS => SystemInfo.operatingSystem.Contains("Mac OS");
        //private static bool IsWinOS => SystemInfo.operatingSystem.Contains("Windows");

        [MenuItem("Tools/Delete Save All", false, 3)]
        public static void DeleteSaveAll()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
#endif