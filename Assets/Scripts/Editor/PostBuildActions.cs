using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

/// <summary>
/// Proccess data after build
/// Folder for libs in root, Frameworks
/// </summary>
public class PostBuildActions {

    /// <summary>
    /// Languages app list, sync it with your app config
    /// </summary>
    private static string[] LANGUAGES = new string[] { "ru", "en" };

    /// <summary>
    /// App name
    /// </summary>
    private const string APP_NAME = "game";

    /// <summary>
    /// Support files directory
    /// </summary>
    private const string SUPPORT_FILES_FOLDER = "SupportFiles";

    /// <summary>
    /// Directory for iOS locales
    /// </summary>
    private const string LOCALES_FOLDER = "Locales";

    /// <summary>
    /// Run after project build
    /// Save version.txt file for build shell script
    /// </summary>
    /// <param name="buildTarget">Platform</param>
    /// <param name="path">Path to folder</param>
    [PostProcessBuild]
    public static void PostProcess(BuildTarget buildTarget, string pathToBuiltProject) {
        if (buildTarget == BuildTarget.iOS) {
#if UNITY_IOS
            FixPlist(pathToBuiltProject);
            AddFrameworks(pathToBuiltProject);
            AddSupportFiles(pathToBuiltProject);
            AddLanguages(pathToBuiltProject);
            AddLocalization(pathToBuiltProject);
#endif
            try {
                int build = int.Parse(PlayerSettings.iOS.buildNumber);
                build++;
                PlayerSettings.iOS.buildNumber = build.ToString();
                Debug.LogFormat("New build number: {0}", build);
            } catch (Exception e) {
                Debug.LogErrorFormat("Error on setup new build: {0}. Error: {1}", PlayerSettings.iOS.buildNumber, e.Message);
            }
        } else if (buildTarget == BuildTarget.Android) {
            PlayerSettings.Android.bundleVersionCode++;
            Debug.LogFormat("New bundle version code: {0}", PlayerSettings.Android.bundleVersionCode);
        }
    }

#if UNITY_IOS
    /// <summary>
    /// Add params to Info.plist
    /// </summary>
    /// <param name="path">Path to folder</param>
    private static void FixPlist(string path) {
        string plistPath = Path.Combine(path, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("NSPhotoLibraryUsageDescription", "$(PRODUCT_NAME) need to access the library in order to select photo for user's avatar.");
        rootDict.SetString("NSPhotoLibraryAddUsageDescription", "$(PRODUCT_NAME) need to access the library in order to select photo for user's avatar.");
        rootDict.SetString("NSCameraUsageDescription", "$(PRODUCT_NAME) need to access the camera in order to capture photo for user's avatar.");
        rootDict.SetString("NSUserTrackingUsageDescription", "$(PRODUCT_NAME) need to access the IDFA in order to improve personalized advertising of game, log player's behaviour and catch critical bugs.");
        plist.WriteToFile(plistPath);
        Debug.Log("Plist fixed");
    }

    /// <summary>
    /// Add external files to project
    /// </summary>
    /// <param name="path">Path to folder</param>
    private static void AddSupportFiles(string path) {
        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        string file = File.ReadAllText(projectPath);
        project.ReadFromString(file);
        string target = project.GetUnityMainTargetGuid();
        //
        // Example how to add some external files to Xcode project
        //
        // AddFileToRoot(project, path, target, "splash-iphone.png");
        // AddFileToRoot(project, path, target, "splash-ipad.png");
    }

    /// <summary>
    /// Add file to Xcode project
    /// </summary>
    /// <param name="project">Project</param>
    /// <param name="path">Project path</param>
    /// <param name="target">Target GUID</param>
    /// <param name="fileName">File to add</param>
    static void AddFileToRoot(PBXProject project, string path, string target, string fileName) {
        CopyAndReplaceFile(SUPPORT_FILES_FOLDER, path, fileName);
        string name = project.AddFile(fileName, fileName, PBXSourceTree.Source);
        project.AddFileToBuild(target, name);
        Debug.LogFormat("File \"{0}\" added to project", fileName);
    }

    /// <summary>
    /// Copy file
    /// </summary>
    /// <param name="source">Dir copy from</param>
    /// <param name="distination">Dir copy to</param>
    /// <param name="fileName">File to copy</param>
    static void CopyAndReplaceFile(string source, string distination, string fileName) {
        if (File.Exists(Path.Combine(distination, fileName))) {
            File.Delete(Path.Combine(distination, fileName));
        }
        File.Copy(Path.Combine(source, fileName), Path.Combine(distination, fileName));
    }

    /// <summary>
    /// Add languages to project
    /// </summary>
    /// <param name="path">Path to folder</param>
    static void AddLanguages(string path) {
        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);
        project.ClearKnownRegions();
        PlistDocument plist = new PlistDocument();
        string plistPath = Path.Combine(path, "Info.plist");
        plist.ReadFromFile(plistPath);
        PlistElementArray languages = plist.root.CreateArray("CFBundleLocalizations");
        foreach (string code in LANGUAGES) {
            project.AddKnownRegion(code);
            languages.AddString(code);
            Debug.LogFormat("Language \"{0}\" added to project", code);
        }
        plist.WriteToFile(plistPath);
        project.WriteToFile(projectPath);
    }

    /// <summary>
    /// Add localization to project *.lproj
    /// </summary>
    /// <param name="path">Path to folder</param>
    /// <param name="infoFile">File to localize</param>
    static void AddLocalization(string path, string infoFile = "InfoPlist.strings") {
        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);
        foreach (string code in LANGUAGES) {
            string langDir = string.Format("{0}.lproj", code);
            string directory = Path.Combine(path, langDir);
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, infoFile);
            string relativePath = Path.Combine(langDir, infoFile);
            string sourcePath = Path.Combine(SUPPORT_FILES_FOLDER, LOCALES_FOLDER, langDir, infoFile);
            string data = File.ReadAllText(sourcePath);
            File.WriteAllText(filePath, data);
            project.AddLocaleVariantFile(infoFile, code, relativePath);
            Debug.LogFormat("Localization \"{0}\" added to project", langDir);
        }
        project.WriteToFile(projectPath);
    }

    /// <summary>
    /// Connect external libs
    /// </summary>
    /// <param name="path">Path to folder</param>
    private static void AddFrameworks(string path) {
        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        string file = File.ReadAllText(projectPath);
        project.ReadFromString(file);
        string target = project.GetUnityMainTargetGuid();
        string managerFile = string.Format("Unity-iPhone/{0}.entitlements", APP_NAME);
        ProjectCapabilityManager manager = new ProjectCapabilityManager(projectPath, managerFile, "Unity-iPhone", target);
        //
        // Example how to add some capabilities to Xcode project
        //
        project.AddFrameworkToProject(target, "StoreKit.framework", false);
        manager.AddInAppPurchase();
        manager.AddSignInWithApple();
        manager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
#if DEBUG
        manager.AddPushNotifications(true);
#else
		manager.AddPushNotifications (false);
#endif
        manager.WriteToFile();
        project.AddFile(managerFile, managerFile);
        project.AddBuildProperty(target, "CODE_SIGN_ENTITLEMENTS", managerFile);
        string targetFramework = project.GetUnityFrameworkTargetGuid();
        project.AddFrameworkToProject(targetFramework, "UserNotifications.framework", false);
        project.AddFrameworkToProject(targetFramework, "AuthenticationServices.framework", false);
        project.AddFrameworkToProject(targetFramework, "StoreKit.framework", false);
        project.AddFrameworkToProject(targetFramework, "MessageUI.framework", false);
        project.AddFrameworkToProject(targetFramework, "Webkit.framework", false);
        project.WriteToFile(projectPath);
        Debug.Log("Frameworks added");
    }
#endif

}