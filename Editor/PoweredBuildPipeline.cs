using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Audio;

namespace ManagedCodeStripping.Editor
{
    public static class PoweredBuildPipeline
    {
        public class LinkXmlGenerator
        {
            private Dictionary<Type, Type> m_TypeConversion = new Dictionary<Type, Type>();

            private HashSet<Type> m_Types = new HashSet<Type>();

            private HashSet<Assembly> m_Assemblies = new HashSet<Assembly>();

            protected Dictionary<string, HashSet<string>> serializedClassesPerAssembly = new Dictionary<string, HashSet<string>>();

            public static LinkXmlGenerator CreateDefault()
            {
                LinkXmlGenerator linkXmlGenerator = new LinkXmlGenerator();
                KeyValuePair<Type, Type>[] editorTypeConversions = GetEditorTypeConversions();
                KeyValuePair<Type, Type>[] array = editorTypeConversions;
                for (int i = 0; i < array.Length; i++)
                {
                    KeyValuePair<Type, Type> keyValuePair = array[i];
                    linkXmlGenerator.SetTypeConversion(keyValuePair.Key, keyValuePair.Value);
                }

                return linkXmlGenerator;
            }

            public static KeyValuePair<Type, Type>[] GetEditorTypeConversions()
            {
                Assembly assembly = Assembly.GetAssembly(typeof(BuildPipeline));
                return new KeyValuePair<Type, Type>[3]
                {
                new KeyValuePair<Type, Type>(typeof(AnimatorController), typeof(RuntimeAnimatorController)),
                new KeyValuePair<Type, Type>(assembly.GetType("UnityEditor.Audio.AudioMixerController"), typeof(AudioMixer)),
                new KeyValuePair<Type, Type>(assembly.GetType("UnityEditor.Audio.AudioMixerGroupController"), typeof(AudioMixerGroup))
                };
            }

            public void AddAssemblies(params Assembly[] assemblies)
            {
                if (assemblies != null)
                {
                    foreach (Assembly a in assemblies)
                    {
                        AddAssemblyInternal(a);
                    }
                }
            }

            public void AddAssemblies(IEnumerable<Assembly> assemblies)
            {
                if (assemblies == null)
                {
                    return;
                }

                foreach (Assembly assembly in assemblies)
                {
                    AddAssemblyInternal(assembly);
                }
            }

            public void AddTypes(params Type[] types)
            {
                if (types != null)
                {
                    foreach (Type t in types)
                    {
                        AddTypeInternal(t);
                    }
                }
            }

            public void AddTypes(IEnumerable<Type> types)
            {
                if (types == null)
                {
                    return;
                }

                foreach (Type type in types)
                {
                    AddTypeInternal(type);
                }
            }

            public void AddSerializedClass(IEnumerable<string> serializedRefTypes)
            {
                if (serializedRefTypes == null)
                {
                    return;
                }

                foreach (string serializedRefType in serializedRefTypes)
                {
                    int num = serializedRefType.IndexOf(':');
                    if (num != -1)
                    {
                        AddSerializedClassInternal(serializedRefType.Substring(0, num), serializedRefType.Substring(num + 1, serializedRefType.Length - (num + 1)));
                    }
                }
            }

            private void AddTypeInternal(Type t)
            {
                if (!(t == null))
                {
                    if (m_TypeConversion.TryGetValue(t, out var value))
                    {
                        m_Types.Add(value);
                    }
                    else
                    {
                        m_Types.Add(t);
                    }
                }
            }

            private void AddSerializedClassInternal(string assemblyName, string classWithNameSpace)
            {
                if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(classWithNameSpace))
                {
                    if (!serializedClassesPerAssembly.TryGetValue(assemblyName, out var value))
                    {
                        value = (serializedClassesPerAssembly[assemblyName] = new HashSet<string>());
                    }

                    value.Add(classWithNameSpace);
                }
            }

            private void AddAssemblyInternal(Assembly a)
            {
                if (!(a == null))
                {
                    m_Assemblies.Add(a);
                }
            }

            public void SetTypeConversion(Type a, Type b)
            {
                m_TypeConversion[a] = b;
            }

            public void Save(string path)
            {
                Dictionary<Assembly, List<Type>> dictionary = new Dictionary<Assembly, List<Type>>();
                foreach (Assembly assembly2 in m_Assemblies)
                {
                    if (!dictionary.TryGetValue(assembly2, out var _))
                    {
                        dictionary.Add(assembly2, new List<Type>());
                    }
                }

                foreach (Type type in m_Types)
                {
                    Assembly assembly = type.Assembly;
                    if (!dictionary.TryGetValue(assembly, out var value2))
                    {
                        dictionary.Add(assembly, value2 = new List<Type>());
                    }

                    value2.Add(type);
                }

                XmlDocument xmlDocument = new XmlDocument();
                XmlNode xmlNode = xmlDocument.AppendChild(xmlDocument.CreateElement("linker"));
                foreach (KeyValuePair<Assembly, List<Type>> item in dictionary)
                {
                    XmlNode xmlNode2 = xmlNode.AppendChild(xmlDocument.CreateElement("assembly"));
                    XmlAttribute xmlAttribute = xmlDocument.CreateAttribute("fullname");
                    xmlAttribute.Value = item.Key.FullName;
                    if (xmlNode2.Attributes == null)
                    {
                        continue;
                    }

                    xmlNode2.Attributes.Append(xmlAttribute);
                    if (m_Assemblies.Contains(item.Key))
                    {
                        XmlAttribute xmlAttribute2 = xmlDocument.CreateAttribute("preserve");
                        xmlAttribute2.Value = "all";
                        xmlNode2.Attributes.Append(xmlAttribute2);
                    }

                    foreach (Type item2 in item.Value)
                    {
                        XmlNode xmlNode3 = xmlNode2.AppendChild(xmlDocument.CreateElement("type"));
                        XmlAttribute xmlAttribute3 = xmlDocument.CreateAttribute("fullname");
                        xmlAttribute3.Value = item2.FullName;
                        if (xmlNode3.Attributes != null)
                        {
                            xmlNode3.Attributes.Append(xmlAttribute3);
                            XmlAttribute xmlAttribute4 = xmlDocument.CreateAttribute("preserve");
                            xmlAttribute4.Value = "all";
                            xmlNode3.Attributes.Append(xmlAttribute4);
                        }
                    }

                    string name = item.Key.GetName().Name;
                    if (!serializedClassesPerAssembly.ContainsKey(name))
                    {
                        continue;
                    }

                    foreach (string item3 in serializedClassesPerAssembly[name])
                    {
                        XmlNode xmlNode4 = xmlNode2.AppendChild(xmlDocument.CreateElement("type"));
                        XmlAttribute xmlAttribute5 = xmlDocument.CreateAttribute("fullname");
                        xmlAttribute5.Value = item3;
                        if (xmlNode4.Attributes != null)
                        {
                            xmlNode4.Attributes.Append(xmlAttribute5);
                            XmlAttribute xmlAttribute6 = xmlDocument.CreateAttribute("preserve");
                            xmlAttribute6.Value = "nothing";
                            xmlNode4.Attributes.Append(xmlAttribute6);
                            XmlAttribute xmlAttribute7 = xmlDocument.CreateAttribute("serialized");
                            xmlAttribute7.Value = "true";
                            xmlNode4.Attributes.Append(xmlAttribute7);
                        }
                    }

                    serializedClassesPerAssembly.Remove(name);
                }

                foreach (KeyValuePair<string, HashSet<string>> item4 in serializedClassesPerAssembly)
                {
                    XmlNode xmlNode5 = xmlNode.AppendChild(xmlDocument.CreateElement("assembly"));
                    XmlAttribute xmlAttribute8 = xmlDocument.CreateAttribute("fullname");
                    xmlAttribute8.Value = item4.Key;
                    foreach (string item5 in item4.Value)
                    {
                        XmlNode xmlNode6 = xmlNode5.AppendChild(xmlDocument.CreateElement("type"));
                        XmlAttribute xmlAttribute9 = xmlDocument.CreateAttribute("fullname");
                        xmlAttribute9.Value = item5;
                        if (xmlNode6.Attributes != null)
                        {
                            xmlNode6.Attributes.Append(xmlAttribute9);
                            XmlAttribute xmlAttribute10 = xmlDocument.CreateAttribute("preserve");
                            xmlAttribute10.Value = "nothing";
                            xmlNode6.Attributes.Append(xmlAttribute10);
                            XmlAttribute xmlAttribute11 = xmlDocument.CreateAttribute("serialized");
                            xmlAttribute11.Value = "true";
                            xmlNode6.Attributes.Append(xmlAttribute11);
                        }
                    }
                }

                xmlDocument.Save(path);
            }
        }
    }
}