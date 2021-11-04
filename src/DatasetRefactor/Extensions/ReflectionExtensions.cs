using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace DatasetRefactor.Extensions
{
    internal static class ReflectionExtensions
    {
        public static IEnumerable<Type> FindTypes(this Assembly assembly, string baseType, string excludeName = null)
        {
            return from i in assembly.ExportedTypes
                where baseType == i.BaseType.FullName
                && (string.IsNullOrEmpty(excludeName) | excludeName != i.Name)
                select i;
        }

        public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type type)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            return from i in methods
                   where !i.IsSpecialName
                   select i;
        }

        public static string GetFriendlyName(this Type type)
        {
            var valueType = Nullable.GetUnderlyingType(type);
            var isNullable = valueType != null;
            valueType = valueType ?? type;

            var name = string.Empty;
            var originalName = valueType.FullName.Replace("+", ".");
            var reference = new CodeTypeReference(valueType);

            using (var provider = new CSharpCodeProvider())
            {
                name = provider.GetTypeOutput(reference);
            }

            if (name.Equals(originalName))
            {
                return valueType.Name;
            }

            if (isNullable)
            {
                name += "?";
            }

            return name;
        }
    }
}
