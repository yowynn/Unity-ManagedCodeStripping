using CSObjectWrapEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ManagedCodeStripping.Editor
{
    public static class Test
    {
        [MenuItem("ManagedCodeStripping/Test/LogGuidToPath", false)]
        public static void LogGuidToPath()
        {
            var guid = "8b22792c3b570444eb18cb78c2af3a74";
            var path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log(path);
            Debug.Log(Application.dataPath);
        }

        [MenuItem("ManagedCodeStripping/Test/pipelinebuildlinkxml", false)]
        public static void pipelinebuildlinkxml()
        {
            var linkXmlGenerator = new PoweredBuildPipeline.LinkXmlGenerator();
            linkXmlGenerator.AddTypes(new Type[]{ typeof(String)});
            linkXmlGenerator.Save("Assets/ManagedCodeStripping/link.xml");
        }

        [MenuItem("ManagedCodeStripping/Test/typesdiff", false)]
        public static void typesdiff()
        {
            var CustomTypes = CustomLinkXmlGen.FinalTypes();
            var OriginTypes = PoweredXlua.FinalTypes().Concat(iHuman.Ams.Plugin.XLuaConfig.ReflectionUseGenerate).Concat(iHuman.Ams.Plugin.XLuaConfig.LuaCallCSharpGenerate).Distinct().ToList();
            var CustomExtra = CustomTypes.Except(OriginTypes).ToList();
            var OriginExtra = OriginTypes.Except(CustomTypes).ToList();
            var linkXmlGenerator = new PoweredBuildPipeline.LinkXmlGenerator();
            linkXmlGenerator.AddTypes(CustomExtra);
            linkXmlGenerator.Save("Assets/ManagedCodeStripping/CustomExtra.xml");
            linkXmlGenerator = new PoweredBuildPipeline.LinkXmlGenerator();
            linkXmlGenerator.AddTypes(OriginExtra);
            linkXmlGenerator.Save("Assets/ManagedCodeStripping/OriginExtra.xml");
            Debug.Log("OK");
        }

        [MenuItem("ManagedCodeStripping/Test/aaa", false)]
        public static void aaa()
        {
                //Generator.GetGenConfig(XLua.Utils.GetAllTypes());
                //foreach (var type in Generator.LuaCallCSharp)
                //{
                //    Debug.Log(type.Name);
                //}
                //Debug.Log(Generator.LuaCallCSharp.Count);
            Debug.Log(Type.GetType("UnityEngine.AsyncOperation, UnityEngine.CoreModule"));
        }
    }
}
