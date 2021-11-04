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
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Public | BindingFlags.DeclaredOnly);
            return from i in methods
                   where !i.IsSpecialName
                   select i;
        }

        public static string GetFriendlyName(this Type type)
        {
            var valueType = Nullable.GetUnderlyingType(type);
            var isNullable = valueType != null;
            valueType ??= type;

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

        public static void InvokeDefault(this object instance, MethodInfo method)
        {
            var parameters = method
                .GetParameters()
                .Select(i => i.ParameterType.CreateInstance())
                .ToArray();

            try
            {
                method.Invoke(instance, parameters);
            }
            catch
            {
            }
        }

        public static void InvokeDefault(this object instance, string methodName)
        {
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            instance.InvokeDefault(method);
        }

        public static T GetPropertyValue<T>(this object instance, string name)
        {
            var type = instance.GetType();
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (T)prop.GetValue(instance);
        }

        public static object CreateInstance(this Type type)
        {
            var hasConstructor = type.GetConstructor(Type.EmptyTypes) != null;
            var nullableType = Nullable.GetUnderlyingType(type);

            if (nullableType is not null)
            {
                return nullableType.CreateInstance();
            }

            if (!type.IsClass || hasConstructor)
            {
                return Activator.CreateInstance(type);
            }
            else if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), 0);
            }
            else if (type == typeof(string))
            {
                return string.Empty;
            }

            return null;
        }
    }
}
