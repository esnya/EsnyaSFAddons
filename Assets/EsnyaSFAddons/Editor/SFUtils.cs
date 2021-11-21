using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharp.Serialization;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace EsnyaAircraftAssets
{
    public class SFUtils
    {
        public static IEnumerable<FieldInfo> ListPublicVariables(Type type)
        {
            return type
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null);
        }

        public static MethodInfo[] ListCustomEvents(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static bool IsExtention(Type type)
        {
            return ListCustomEvents(type).Any(m => m.Name.StartsWith("SFEXT_"));
        }

        public static bool IsDFUNC(Type type)
        {
            return ListCustomEvents(type).Any(m => m.Name.StartsWith("DFUNC_"));
        }
        public static IEnumerable<UdonSharpBehaviour> FindExtentions(GameObject root)
        {
            return root.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).Where(udon => IsExtention(udon.GetType()) && !IsDFUNC(udon.GetType()));
        }

        public static IEnumerable<UdonSharpBehaviour> FindDFUNCs(GameObject root)
        {
            return root.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).Where(udon => IsDFUNC(udon.GetType()));
        }
    }
}
