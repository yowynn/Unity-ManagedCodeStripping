using CSObjectWrapEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using XLua;
using static CSObjectWrapEditor.Generator;

namespace ManagedCodeStripping.Editor
{
    public static class PoweredXlua
    {
        private static LuaEnv luaenv = new LuaEnv();
        private static XLuaTemplates templateRef;

        // power by `Generator.Generator()`
        static PoweredXlua()
        {
#if !XLUA_GENERAL
            TemplateRef template_ref = ScriptableObject.CreateInstance<TemplateRef>();

            templateRef = new XLuaTemplates()
            {
#if GEN_CODE_MINIMIZE
                LuaClassWrap = { name = template_ref.LuaClassWrapGCM.name, text = template_ref.LuaClassWrapGCM.text },
#else
                LuaClassWrap = { name = template_ref.LuaClassWrap.name, text = template_ref.LuaClassWrap.text },
#endif
                LuaDelegateBridge = { name = template_ref.LuaDelegateBridge.name, text = template_ref.LuaDelegateBridge.text },
                LuaDelegateWrap = { name = template_ref.LuaDelegateWrap.name, text = template_ref.LuaDelegateWrap.text },
#if GEN_CODE_MINIMIZE
                LuaEnumWrap = { name = template_ref.LuaEnumWrapGCM.name, text = template_ref.LuaEnumWrapGCM.text },
#else
                LuaEnumWrap = { name = template_ref.LuaEnumWrap.name, text = template_ref.LuaEnumWrap.text },
#endif
                LuaInterfaceBridge = { name = template_ref.LuaInterfaceBridge.name, text = template_ref.LuaInterfaceBridge.text },
#if GEN_CODE_MINIMIZE
                LuaRegister = { name = template_ref.LuaRegisterGCM.name, text = template_ref.LuaRegisterGCM.text },
#else
                LuaRegister = { name = template_ref.LuaRegister.name, text = template_ref.LuaRegister.text },
#endif
                LuaWrapPusher = { name = template_ref.LuaWrapPusher.name, text = template_ref.LuaWrapPusher.text },
                PackUnpack = { name = template_ref.PackUnpack.name, text = template_ref.PackUnpack.text },
                TemplateCommon = { name = template_ref.TemplateCommon.name, text = template_ref.TemplateCommon.text },
            };
#endif
            luaenv.AddLoader((ref string filepath) =>
            {
                if (filepath == "TemplateCommon")
                {
                    return Encoding.UTF8.GetBytes(templateRef.TemplateCommon.text);
                }
                else
                {
                    return null;
                }
            });
        }

        // power by `Generator.CustomGen`
        private static void GenTask(CustomGenTask gen_task)
        {
            string template_src = ScriptableObject.CreateInstance<LinkXmlGen>().Template.text;
            LuaFunction template = XLua.TemplateEngine.LuaTemplate.Compile(luaenv, template_src);
            LuaTable meta = luaenv.NewTable();
            meta.Set("__index", luaenv.Global);
            gen_task.Data.SetMetaTable(meta);
            meta.Dispose();
            try
            {
                string genCode = XLua.TemplateEngine.LuaTemplate.Execute(template, gen_task.Data);
                gen_task.Output.Write(genCode);
                gen_task.Output.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError("gen file fail! template=" + template_src + ", err=" + e.Message + ", stack=" + e.StackTrace);
            }
            finally
            {
                gen_task.Output.Close();
            }
        }

        public static List<Type> FinalTypes()
        {
            Generator.GetGenConfig(XLua.Utils.GetAllTypes());
            var finalList = LuaCallCSharp.Concat(ReflectionUse).Distinct().ToList();
            return finalList;
        }

        [MenuItem("ManagedCodeStripping/XluaLinkXmlGen", false)]
        public static void XluaDefaultLinker()
        {
            GetTasks get_tasks = LinkXmlGen.GetTasks;
            Generator.GetGenConfig(XLua.Utils.GetAllTypes());
            foreach (var gen_task in get_tasks(luaenv, new UserConfig()
            {
                LuaCallCSharp = Generator.LuaCallCSharp,
                CSharpCallLua = Generator.CSharpCallLua,
                ReflectionUse = Generator.ReflectionUse
            }))
            {
                GenTask(gen_task);
            }
        }
    }
}
