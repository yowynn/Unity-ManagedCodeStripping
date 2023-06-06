using System.Collections.Generic;

namespace ManagedCodeStripping.Editor
{
    public static partial class ManagedCodeStrippingConfig
    {

        static partial void CustomConfig();

        // 扫描资源的路径
        public static List<string> RESOURSE_ROOT_PATH = new List<string>
        {
            UnityEngine.Application.dataPath,
            //@"P:\iHuman\Repos\hongen_sci\client\trunk",
        };

        // 扫描脚本资源的后缀
        public static HashSet<string> ScriptReferencedScriptExtensions = new HashSet<string>()
        {
            ".lua",
            //".cs",
        };

        // 扫描美术资源的后缀
        public static HashSet<string> ScriptReferencedAssetExtensions = new HashSet<string>()
        {
            ".unity",                       // MonoBehaviors
            ".prefab",                      // MonoBehaviors
            ".controller",                  // StateMachineBehaviours
            ".playable",
            ".asset",
        };

        // 是否递归添加成员返回值类型的引用
        public static bool RECURSIVE_ADD_RETURN_TYPE = true;

        // 输出 link.xml 文件的目录
        public static string OUTPUT_PATH = @"Assets/stripping/";

        // 是否导出 unused monos
        public static bool EXPORT_UNUSED_MONOS = true;

        // 是否增量添加在原有的 link.xml 文件上
        public static bool INCREMENTAL_EXPORT = true;

        static ManagedCodeStrippingConfig()
        {
            CustomConfig();
        }
    }
}
