using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ManagedCodeStripping.Editor
{
    public static class CustomLinkXmlGen
    {
        public static string[] RESOURSE_ROOT_PATH => new string[]
        {
            Application.dataPath,
            @"P:\hongen_sci\client\trunk",
        };

        // 获取所有类型
        private static List<Type> AllTypes(bool exclude_generic_definition = true)
        {
            List<Type> allTypes = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
                    if (!(assemblies[i].ManifestModule is System.Reflection.Emit.ModuleBuilder))
                    {
#endif
                        allTypes.AddRange(assemblies[i].GetTypes()
                        .Where(type => exclude_generic_definition ? !type.IsGenericTypeDefinition : true)
                        );
#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
                    }
#endif
                }
                catch (Exception)
                {
                }
            }
            return allTypes;
        }

        // 获取可被引用的脚本类型
        private static Dictionary<string, Type> ReferencableTypes()
        {
            var referencableTypes = new Dictionary<string, Type>();
            var paths = AssetDatabase.GetAllAssetPaths().Where(s => s.EndsWith(".cs") && !s.Contains("/Editor/"));
            foreach (var path in paths)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script.GetClass();
                var guid = AssetDatabase.AssetPathToGUID(path);
                referencableTypes.Add(guid, type);
            }
            return referencableTypes;
        }

        // 遍历文件夹的文件
        private static void EnumFiles(string path, Action<string> action)
        {
            foreach (string file in Directory.EnumerateFiles(path))
            {
                action(file);
            }
            foreach (string subdir in Directory.EnumerateDirectories(path))
            {
                EnumFiles(subdir, action);
            }
        }

        public static void MixFileScanning(out HashSet<string> keywordsInScripts, out List<string> mapGuidUsed, out HashSet<string> identifiedClassNames)
        {
            var keywordsInScripts_Internal = new HashSet<string>();
            var mapGuidUsed_internal = new Dictionary<string, string>();
            var classIds = new Dictionary<string, string>();
            var ScriptReferencedScriptExtensions = new HashSet<string>()
            {
                ".lua",
                //".cs",
            };
            var ScriptReferencedAssetExtensions = new HashSet<string>()
            {
                ".unity",                       // MonoBehaviors
                ".prefab",                      // MonoBehaviors
                ".controller",                  // StateMachineBehaviours
                ".playable",
                ".asset",
            };
            void Fill(string path)
            {
                var ext = Path.GetExtension(path).ToLower();
                if (ScriptReferencedScriptExtensions.Contains(ext))
                {
                    // 获取文本中可能引用脚本的标识符
                    var content = File.ReadAllText(path);
                    var matches = Regex.Matches(content, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
                    foreach (Match match in matches)
                    {
                        keywordsInScripts_Internal.Add(match.Value);
                    }
                }
                else if (ScriptReferencedAssetExtensions.Contains(ext))
                {
                    // 获取美术资源引用的脚本
                    var content = File.ReadAllText(path);
                    var matches = Regex.Matches(content, @"m_Script:\s*{fileID:\s*\d+,\s*guid:\s*(?<guid>\w+),\s*type:\s*\d+}");
                    foreach (Match match in matches)
                    {
                        var guid = match.Groups["guid"].Value;
                        mapGuidUsed_internal[guid] = path;
                    }
                    var matchesU = Regex.Matches(content, @"!u!(?<classId>\d+)\s*&");
                    foreach (Match match in matchesU)
                    { 
                        var classId = match.Groups["classId"].Value;
                        classIds[classId] = path;
                    }
                }
            }
            foreach(var path in RESOURSE_ROOT_PATH)
            {
                EnumFiles(path, Fill);
            }
            keywordsInScripts = keywordsInScripts_Internal;
            mapGuidUsed = mapGuidUsed_internal.Keys.ToList();
            var identifiedClasses = UnityIdentifiedClasses.Map;
            identifiedClassNames = new HashSet<string>();
            foreach (var classId in classIds.Keys.OrderBy(k => k))
            {
                if (identifiedClasses.TryGetValue(classId, out var className))
                {
                    identifiedClassNames.Add(className);
                }
            }
            Debug.Log($"ClassIds.Count: {classIds.Count}");
        }

        public static void GetMemberReturnTypes(List<Type> types)
        {
            var making = new HashSet<Type>(types);
            var made = new HashSet<Type>();
            var i = 0;
            while (i < types.Count)
            {
                var type = types[i];
                if (!made.Contains(type))
                {
                    made.Add(type);
                    MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    foreach (MemberInfo member in members)
                    {
                        Type memberType = null;
                        switch (member.MemberType)
                        {
                            case MemberTypes.Field:
                                memberType = ((FieldInfo)member).FieldType;
                                break;
                            case MemberTypes.Method:
                                memberType = ((MethodInfo)member).ReturnType;
                                break;
                            case MemberTypes.Property:
                                memberType = ((PropertyInfo)member).PropertyType;
                                break;
                        }
                        if (memberType != null && !making.Contains(memberType))
                        {
                            making.Add(memberType);
                            var fn = memberType.FullName;
                            if (!String.IsNullOrEmpty(fn) && !fn.Contains("&") && !fn.Contains("["))
                                types.Add(memberType);
                        }
                    }
                }
                ++i;
            }
        }

        public static List<Type> FinalTypes()
        {
            var finalList = new List<Type>();
            var allTypes = AllTypes();
            Debug.Log($"AllTypes.Count: {allTypes.Count}");
            var referencableTypes = ReferencableTypes();
            Debug.Log($"ReferencableTypes.Count: {referencableTypes.Count}");

            MixFileScanning(out var keywordsInScripts, out var referencedTypeGuids, out var identifiedClassNames);
            Debug.Log($"ReferencedTypeGuids.Count: {referencedTypeGuids.Count}");
            Debug.Log($"KeywordsInScripts.Count: {keywordsInScripts.Count}");
            Debug.Log($"IdentifiedClassNames.Count: {identifiedClassNames.Count}");

            // filter asset referenced types
            foreach (var guid in referencedTypeGuids)
            {
                if (referencableTypes.TryGetValue(guid, out var type))
                {
                    finalList.Add(type);
                    //allTypes.Remove(type);
                }
            }
            Func<Type, bool> typeFilter = (Type t) =>
            {
                var ns = t.Namespace ?? "";
                var an = t.Assembly.GetName().Name ?? "";
                return ns.StartsWith("UnityEngine") && an != "UnityEngine.UIElementsModule" && an != "UnityEditor";
            };
            // filter script referenced types
            var typeNameMap = allTypes.Where(typeFilter).GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.ToArray());
            var typeNames = new HashSet<string>(typeNameMap.Keys);
            foreach (string keyword in keywordsInScripts)
            {
                if (typeNames.Contains(keyword))
                {
                    finalList.AddRange(typeNameMap[keyword]);
                }
            }
            foreach (string keyword in identifiedClassNames)
            {
                if (typeNames.Contains(keyword))
                {
                    finalList.AddRange(typeNameMap[keyword]);
                }
            }

            // special case
            //finalList.Add(Type.GetType("UnityEngine.AsyncOperation, UnityEngine.CoreModule"));
            GetMemberReturnTypes(finalList);

            // filter replicated
            var xlua = PoweredXlua.FinalTypes();
            finalList = finalList.Distinct().Except(xlua).ToList();
            return finalList;
        }

        [MenuItem("ManagedCodeStripping/CustomLinkXmlGen", false)]
        public static void LinkXmlGen()
        {
            var finalList = FinalTypes();
            // save to link.xml
            var linkXmlGenerator = new PoweredBuildPipeline.LinkXmlGenerator();
            linkXmlGenerator.AddTypes(finalList);
            linkXmlGenerator.Save("Assets/ManagedCodeStripping/link.xml");
            Debug.Log("Finish!");
        }
    }
}
