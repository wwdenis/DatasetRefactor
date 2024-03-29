﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace DatasetRefactor.Extensions
{
    internal static class ReflectionExtensions
    {
        private const BindingFlags DeclaredMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        private const BindingFlags AllMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static IEnumerable<Type> FindTypes(this Assembly assembly, IEnumerable<string> baseNames, string excludeName = null)
        {
            return assembly.ExportedTypes.FindTypes(baseNames, excludeName);
        }

        public static IEnumerable<Type> FindTypes(this IEnumerable<Type> types, IEnumerable<string> baseNames, string excludeName = null)
        {
            return from i in types
                   let genericBase = i.BaseType.IsGenericType ? i.BaseType.GetGenericTypeDefinition() : null
                   let baseType = genericBase ?? i.BaseType
                   where baseNames.Contains(baseType.FullName)
                   && (string.IsNullOrEmpty(excludeName) || !i.Name.Equals(excludeName, StringComparison.Ordinal))
                   orderby i.Name
                   select i;
        }

        public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type type)
        {
            var methods = type.GetMethods(DeclaredMembers);
            return from i in methods
                   where !i.IsSpecialName
                   select i;
        }

        public static string GetCsName(this Type type)
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
                name = valueType.Name;
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
            var method = type.GetMethod(methodName, AllMembers);
            instance.InvokeDefault(method);
        }

        public static T GetPropertyValue<T>(this object instance, string name)
        {
            var type = instance.GetType();
            var prop = type.GetProperty(name, AllMembers);
            if (prop is null)
            {
                return default;
            }
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

        public static bool IsSimple(this Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            
            if (nullableType is not null)
            {
                return nullableType.IsSimple();
            }

            var types = new[]
            {
                typeof(object),
                typeof(string),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid),
                typeof(Uri),
            };

            return type.IsPrimitive
              || type.IsEnum
              || types.Contains(type);
        }
    }
}
