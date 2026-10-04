using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace AppDiscovery.Editor
{
    /// <summary>
    /// Configures the generated Xcode project for the AppDiscovery iOS SDK:
    /// Swift settings for the bridge and the -ObjC linker flag.
    /// </summary>
    public static class AppDiscoveryPostProcessBuild
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (target != BuildTarget.iOS)
                return;

            string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            string targetGuid = proj.GetUnityMainTargetGuid();
            string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

            // The bridge is Swift: set the language version and embed the Swift runtime.
            proj.SetBuildProperty(targetGuid, "SWIFT_VERSION", "5.0");
            proj.SetBuildProperty(frameworkTargetGuid, "SWIFT_VERSION", "5.0");
            proj.SetBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            proj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
            proj.AddBuildProperty(frameworkTargetGuid, "OTHER_LDFLAGS", "-ObjC");

            proj.WriteToFile(projPath);
            Debug.Log("[AppDiscovery Build] Xcode project configured (Swift 5.0, -ObjC).");
#endif
        }
    }
}
