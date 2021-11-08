using System;
using System.Collections;
using System.Reflection;
using UnityEditor.iOS.Xcode;

static class PBXProjectExtensions {

    readonly static Type _guidList;
    readonly static Type _pbxBuildFileData;
    readonly static Type _pbxElementArray;
    readonly static Type _pbxElementString;
    readonly static Type _pbxVariantGroupData;
    readonly static FieldInfo _dataFileGroups;
    readonly static FieldInfo _dataFileRefsField;
    readonly static FieldInfo _knownRegionsDict;
    readonly static FieldInfo _groupChildren;
    readonly static FieldInfo _groupName;
    readonly static FieldInfo _groupPath;
    readonly static FieldInfo _pbxObjectGUID;
    readonly static FieldInfo _pbxElementArrayValues;
    readonly static FieldInfo _projectData;
    readonly static FieldInfo _resourceFiles;
    readonly static FieldInfo _variantGroupName;
    readonly static PropertyInfo _fileRefsPath;
    readonly static PropertyInfo _projectResoruces;
    readonly static PropertyInfo _projectSection;
    readonly static PropertyInfo _projectSectionObjectData;
    readonly static PropertyInfo _projectVariantGroups;
    readonly static MethodInfo _dataFileRefsFieldObjects;
    readonly static MethodInfo _fileRefDataCreateFromFile;
    readonly static MethodInfo _getPropertiesRaw;
    readonly static MethodInfo _groupsObjects;
    readonly static MethodInfo _guidListAdd;
    readonly static MethodInfo _guidListContains;
    readonly static MethodInfo _pbxBuildFileDataCreateFromFile;
    readonly static MethodInfo _projectBuildFilesAdd;
    readonly static MethodInfo _projectFileRefsAdd;
    readonly static MethodInfo _projectBuildFilesGetForSourceFile;
    readonly static MethodInfo _rawPropertiesValuesAddValue;
    readonly static MethodInfo _rawPropertiesValuesGetValue;
    readonly static MethodInfo _resorucesObjects;
    readonly static MethodInfo _variantGroupsAddEntry;
    readonly static MethodInfo _variantGroupsObjects;
    readonly static MethodInfo _variantGroupsSetPropertyString;

    static PBXProjectExtensions() {

        /// <summary>
        /// Namespace for assembly
        /// </summary>
        const string PBX_NAMESPACE = "UnityEditor.iOS.Xcode.PBX";

        /// <summary>
        /// Assembly
        /// </summary>
        Assembly assembly = typeof(PBXProject).Assembly;

        /// <summary>
        /// Flags list for reflection
        /// </summary>
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        _guidList = assembly.GetType($"{PBX_NAMESPACE}.GUIDList");
        _pbxBuildFileData = assembly.GetType($"{PBX_NAMESPACE}.PBXBuildFileData");
        _pbxElementArray = assembly.GetType($"{PBX_NAMESPACE}.PBXElementArray");
        _pbxElementString = assembly.GetType($"{PBX_NAMESPACE}.PBXElementString");
        _pbxVariantGroupData = assembly.GetType($"{PBX_NAMESPACE}.PBXVariantGroupData");
        Type fileRefData = assembly.GetType($"{PBX_NAMESPACE}.PBXFileReferenceData");
        Type group = assembly.GetType($"{PBX_NAMESPACE}.PBXGroupData");
        Type pbxObject = assembly.GetType($"{PBX_NAMESPACE}.PBXObjectData");
        Type fileGUIDListBase = assembly.GetType($"{PBX_NAMESPACE}.FileGUIDListBase");
        Type pbxElementDict = assembly.GetType($"{PBX_NAMESPACE}.PBXElementDict");
        Type pbxProjectSection = assembly.GetType($"{PBX_NAMESPACE}.PBXProjectSection");
        _projectData = typeof(PBXProject).GetField("m_Data", flags);
        _dataFileRefsField = _projectData.FieldType.GetField("fileRefs", flags);
        _dataFileGroups = _projectData.FieldType.GetField("groups", flags);
        _resourceFiles = fileGUIDListBase.GetField("files");
        _pbxObjectGUID = pbxObject.GetField("guid");
        _pbxElementArrayValues = _pbxElementArray.GetField("values");
        _groupChildren = group.GetField("children");
        _groupName = group.GetField("name");
        _groupPath = group.GetField("path");
        _variantGroupName = _pbxVariantGroupData.GetField("name");
        _groupsObjects = _dataFileGroups.FieldType.GetMethod("GetObjects");
        _dataFileRefsFieldObjects = _dataFileRefsField.FieldType.GetMethod("GetObjects");
        _getPropertiesRaw = pbxObject.GetMethod("GetPropertiesRaw", flags);
        _guidListAdd = _guidList.GetMethod("AddGUID");
        _guidListContains = _guidList.GetMethod("Contains");
        _fileRefDataCreateFromFile = fileRefData.GetMethod("CreateFromFile", BindingFlags.Static | BindingFlags.Public);
        _pbxBuildFileDataCreateFromFile = _pbxBuildFileData.GetMethod("CreateFromFile", BindingFlags.Static | BindingFlags.Public);
        _projectBuildFilesAdd = typeof(PBXProject).GetMethod("BuildFilesAdd", flags);
        _projectFileRefsAdd = typeof(PBXProject).GetMethod("FileRefsAdd", flags);
        _projectBuildFilesGetForSourceFile = typeof(PBXProject).GetMethod("BuildFilesGetForSourceFile", flags);
        _fileRefsPath = fileRefData.GetProperty("path");
        _knownRegionsDict = pbxElementDict.GetField("m_PrivateValue", flags);
        _projectSection = typeof(PBXProject).GetProperty("project", flags);
        _projectSectionObjectData = _projectSection.PropertyType.GetProperty("project");
        _projectResoruces = typeof(PBXProject).GetProperty("resources", flags);
        _projectVariantGroups = typeof(PBXProject).GetProperty("variantGroups", flags);
        _rawPropertiesValuesGetValue = _knownRegionsDict.FieldType.GetMethod("TryGetValue");
        _rawPropertiesValuesAddValue = _knownRegionsDict.FieldType.GetMethod("Add");
        _resorucesObjects = _projectResoruces.PropertyType.GetMethod("GetObjects");
        _variantGroupsAddEntry = _projectVariantGroups.PropertyType.GetMethod("AddEntry");
        _variantGroupsObjects = _projectVariantGroups.PropertyType.GetMethod("GetObjects");
        _variantGroupsSetPropertyString = group.GetMethod("SetPropertyString", flags);
    }

    /// <summary>
    /// Get file reference by path
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <param name="path">Path to directory</param>
    /// <returns>File reference</returns>
    static object GetFileRefDataByPath(this PBXProject project, string path) {
        object data = _projectData.GetValue(project);
        object fileRefs = _dataFileRefsField.GetValue(data);
        ICollection values = _dataFileRefsFieldObjects.Invoke(fileRefs, null) as ICollection;
        foreach (object file in values) {
            string fileRefPath = _fileRefsPath.GetValue(file) as string;
            if (fileRefPath.Equals(path)) {
                return file;
            }
        }
        return null;
    }

    /// <summary>
    /// Get group by custom name
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <param name="name">Name</param>
    /// <returns>Group</returns>
    static object GetGroupByName(this PBXProject project, string name) {
        object data = _projectData.GetValue(project);
        object groups = _dataFileGroups.GetValue(data);
        ICollection groupsValues = _groupsObjects.Invoke(groups, null) as ICollection;
        foreach (var group in groupsValues) {
            string groupName = _groupName.GetValue(group) as string;
            if (groupName.Equals(name)) {
                return group;
            }
        }
        return null;
    }

    /// <summary>
    /// Get all languages exists in project
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <returns>List of exists regions</returns>
    static IList GetKnownRegions(this PBXProject project) {
        const string target = "knownRegions";
        object section = _projectSection.GetValue(project);
        object data = _projectSectionObjectData.GetValue(section);
        object properties = _getPropertiesRaw.Invoke(data, null);
        object[] args = new[] { target, null };
        object dictionary = _knownRegionsDict.GetValue(properties);
        bool result = (bool)_rawPropertiesValuesGetValue.Invoke(dictionary, args);
        if (!result) {
            args[1] = Activator.CreateInstance(_pbxElementArray);
            _rawPropertiesValuesAddValue.Invoke(dictionary, new object[] { target, args[1] });
        }
        return _pbxElementArrayValues.GetValue(args[1]) as IList;
    }

    /// <summary>
    /// Add file reference to build
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <param name="target">Target GUID</param>
    /// <param name="guid">Group GUID</param>
    /// <returns>Reference of file</returns>
    static string AddFileRefToBuild(this PBXProject project, string target, string guid) {
        object data = _pbxBuildFileDataCreateFromFile.Invoke(null, new object[] { guid, false, null });
        _projectBuildFilesAdd.Invoke(project, new object[] { target, data });
        return _pbxObjectGUID.GetValue(data) as string;
    }

    /// <summary>
    /// Add file to resources
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <param name="buildPhaseGuid">Build GUID</param>
    /// <param name="fileGuid">File GUID to add</param>
    static void AddFileToResourceBuildPhase(this PBXProject project, string buildPhaseGuid, string fileGuid) {
        object resources = _projectResoruces.GetValue(project);
        ICollection values = _resorucesObjects.Invoke(resources, null) as ICollection;
        foreach (object value in values) {
            string guid = _pbxObjectGUID.GetValue(value) as string;
            if (guid.Equals(buildPhaseGuid)) {
                object files = _resourceFiles.GetValue(value);
                _guidListAdd.Invoke(files, new object[] { fileGuid });
            }
        }
    }

    /// <summary>
    /// Clear old deprecated languages
    /// </summary>
    /// <param name="project">PBX Project</param>
    public static void ClearKnownRegions(this PBXProject project) {
        IList regions = project.GetKnownRegions();
        regions.Clear();
    }

    /// <summary>
    /// Add language to project
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <param name="code">Language code</param>
    public static void AddKnownRegion(this PBXProject project, string code) {
        IList regions = project.GetKnownRegions();
        object element = Activator.CreateInstance(_pbxElementString, code);
        regions.Add(element);
    }

    /// <summary>
    /// Add locale for file
    /// </summary>
    /// <param name="project">PBX project</param>
    /// <param name="groupName">Filename to add variant</param>
    /// <param name="code">Language code</param>
    /// <param name="path">Relative path to *.lproj</param>
    public static void AddLocaleVariantFile(this PBXProject project, string groupName, string code, string path) {
        path = path.Replace('\\', '/');
        object variantGroups = _projectVariantGroups.GetValue(project);
        ICollection variantGroupValues = _variantGroupsObjects.Invoke(variantGroups, null) as ICollection;
        object group = null;
        foreach (var value in variantGroupValues) {
            string name = _variantGroupName.GetValue(value) as string;
            if (name.Equals(groupName)) {
                group = value;
            }
        }
        if (group == null) {
            string guid = Guid.NewGuid().ToString("N").Substring(8).ToUpper();
            group = Activator.CreateInstance(_pbxVariantGroupData);
            _variantGroupName.SetValue(group, groupName);
            _groupPath.SetValue(group, groupName);
            _pbxObjectGUID.SetValue(group, guid);
            _groupChildren.SetValue(group, Activator.CreateInstance(_guidList));
            _variantGroupsSetPropertyString.Invoke(group, new object[] { "isa", "PBXVariantGroup" });
            _variantGroupsAddEntry.Invoke(variantGroups, new object[] { group });
        }
        string targetGuid = project.GetUnityMainTargetGuid();
        string groupGuid = _pbxObjectGUID.GetValue(group) as string;
        object buildFileData = _projectBuildFilesGetForSourceFile.Invoke(project, new object[] { targetGuid, groupGuid });
        if (buildFileData == null) {
            object customData = project.GetGroupByName("CustomTemplate");
            object children = _groupChildren.GetValue(customData);
            _guidListAdd.Invoke(children, new object[] { groupGuid });
            string buildFileGuid = project.AddFileRefToBuild(project.GetUnityMainTargetGuid(), groupGuid);
            string buildPhaseGuid = project.GetResourcesBuildPhaseByTarget(targetGuid);
            project.AddFileToResourceBuildPhase(buildPhaseGuid, buildFileGuid);
        }
        object fileRef = project.GetFileRefDataByPath(path);
        if (fileRef == null) {
            fileRef = _fileRefDataCreateFromFile.Invoke(null, new object[] { path, code, PBXSourceTree.Source });
            _projectFileRefsAdd.Invoke(project, new object[] { path, code, group, fileRef });
        }
        string fileRefsGuid = _pbxObjectGUID.GetValue(fileRef) as string;
        object groupChildren = _groupChildren.GetValue(group);
        bool result = (bool)_guidListContains.Invoke(groupChildren, new object[] { fileRefsGuid });
        if (!result) {
            _guidListAdd.Invoke(groupChildren, new[] { fileRefsGuid });
        }
    }
}
