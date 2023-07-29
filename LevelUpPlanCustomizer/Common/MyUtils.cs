using Kingmaker.Blueprints;
using Kingmaker.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LevelUpPlanCustomizer.Common
{
    public static class MyUtils
    {
        private static readonly Regex OwlcatPattern = new("^!bp_[0-9abcdef]{32}");
        private static readonly Regex VekPattern = new("^Blueprint:[0-9abcdef]{32}:.?");
        private static readonly string VekNULL = "^Blueprint::NULL";
        private static readonly Regex BubbleprintsPattern = new("^link: [0-9abcdef]{32} .?");
        private static readonly string BubbleprintsNull = "null";

        public static Guid ParseRef(string str)
        {
            if (str == null || str == "" || str == VekNULL || str == BubbleprintsNull)
            {
                return Guid.Empty;
            }
            else if (OwlcatPattern.Match(str).Success)
            {
                return Guid.Parse(str.Substring(4, 32));
            }
            else if (VekPattern.Match(str).Success)
            {
                return Guid.Parse(str.Substring(10, 32));
            }
            else if (BubbleprintsPattern.Match(str).Success)
            {
                return Guid.Parse(str.Substring(6, 32));
            }
            else
            {
                Guid.TryParse(str, out Guid res);
                return res;
            }
        }

        public static BlueprintGuid ParseToBPGuid(string str)
        {
            var r = ParseRef(str);
            if (r == null || r == Guid.Empty)
            {

            }
            return new BlueprintGuid(r);
        }

        public class InvalidReferenceException : ApplicationException
        {
            public InvalidReferenceException(string id) : base($"Invalid reference: {id}")
            {
            }
        }


        public static T GetBlueprint<T>(string id) where T : SimpleBlueprint
        {
            if (ResourcesLibrary.TryGetBlueprint(ParseToBPGuid(id)) is not T obj)
            {
                throw new InvalidReferenceException(id);
            }
            return obj;
        }

        public static T GetBlueprintReference<T>(string id) where T : BlueprintReferenceBase
        {
            if (id == null || id == "")
            {
                return null;
            }
            var guid = ParseRef(id);
            if (guid == null || guid == Guid.Empty)
            {
                return null;
            }
            T val = Activator.CreateInstance<T>();
            val.deserializedGuid = new BlueprintGuid(guid);
            return val;
        }

        public static void SetComponents(this BlueprintScriptableObject obj, params BlueprintComponent[] components)
        {
            HashSet<string> hashSet = new();
            foreach (BlueprintComponent blueprintComponent in components)
            {
                if (string.IsNullOrEmpty(blueprintComponent.name))
                {
                    blueprintComponent.name = "$" + blueprintComponent.GetType().Name;
                }

                if (!hashSet.Add(blueprintComponent.name))
                {
                    int num = 0;
                    string name;
                    while (!hashSet.Add(name = $"{blueprintComponent.name}${num}"))
                    {
                        num++;
                    }

                    blueprintComponent.name = name;
                }
            }

            obj.ComponentsArray = components;
            obj.OnEnable();
        }

        public static void SetComponents(this BlueprintScriptableObject obj, IEnumerable<BlueprintComponent> components)
        {
            obj.SetComponents(components.ToArray());
        }

        public static void RemoveComponents<T>(this BlueprintScriptableObject obj) where T : BlueprintComponent
        {
            T[] array = obj.GetComponents<T>().ToArray();
            foreach (T value in array)
            {
                obj.SetComponents(obj.ComponentsArray.RemoveFromArray(value));
            }
        }

        internal static T[] RemoveFromArray<T>(this T[] array, T value)
        {
            List<T> list = array.ToList();
            if (!list.Remove(value))
            {
                return array;
            }

            return list.ToArray();
        }

        public static void AddComponent(this BlueprintScriptableObject obj, BlueprintComponent component)
        {
            obj.SetComponents(obj.ComponentsArray.AppendToArray(component));
        }

        internal static T[] AppendToArray<T>(this T[] array, T value)
        {
            int num = array != null ? array.Length : 0;
            T[] array2 = new T[num + 1];
            if (num > 0)
            {
                Array.Copy(array, array2, num);
            }

            array2[num] = value;
            return array2;
        }
        internal static LocalizedString CreateLocalizedString(string key, string value)
        {
            var localizedString = new LocalizedString() { m_Key = key };
            LocalizationManager.CurrentPack.PutString(key, value);
            return localizedString;
        }
    }
}
