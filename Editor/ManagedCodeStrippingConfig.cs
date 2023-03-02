using System.Collections.Generic;

namespace ManagedCodeStripping.Editor
{
    public static class ManagedCodeStrippingConfig
    {
        public static string[] RESOURSE_ROOT_PATH => new string[]
        {
            UnityEngine.Application.dataPath,
            @"P:\iHuman\Repos\hongen_sci\client\trunk",
        };

        public static HashSet<string> ScriptReferencedScriptExtensions = new HashSet<string>()
        {
            ".lua",
            //".cs",
        };

        public static HashSet<string> ScriptReferencedAssetExtensions = new HashSet<string>()
        {
            ".unity",                       // MonoBehaviors
            ".prefab",                      // MonoBehaviors
            ".controller",                  // StateMachineBehaviours
            ".playable",
            ".asset",
        };
    }
}
