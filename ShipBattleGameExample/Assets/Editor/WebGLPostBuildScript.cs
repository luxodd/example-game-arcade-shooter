using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if SOME_BUILD
namespace Editor
{
    public class WebGLPostBuildScript
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
        {
            if (target == BuildTarget.WebGL)
            {
                string buildFolder = Path.Combine(pathToBuildProject, "Build");
                string indexFile = Path.Combine(pathToBuildProject, "index.html");

                if (File.Exists(indexFile))
                {
                    ModifyIndexHtml(indexFile, buildFolder);
                }
            }
        }

        private static void ModifyIndexHtml(string indexFile, string buildFolder)
        {
            // Generate version of the builds
            string versionParam = "?v=" + Application.version; //DateTime.Now.Ticks;

            // Rea index.html
            string htmlContent = File.ReadAllText(indexFile);

            
            // Modify links to the files in the assembly
            htmlContent = htmlContent.Replace("Build/Build.js", $"Build/Build.js{versionParam}");
            htmlContent = htmlContent.Replace("Build/Build.wasm", $"Build/Build.wasm{versionParam}");
            htmlContent = htmlContent.Replace("Build/Build.data", $"Build/Build.data{versionParam}");

            // Write updated file
            File.WriteAllText(indexFile, htmlContent, Encoding.UTF8);
            LoggerHelper.Log("index.html was successfully modified.");
        }
    }
}
#endif