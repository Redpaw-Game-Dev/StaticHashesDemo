using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace LazyRedpaw.StaticHashes
{
    public static class SerializedPropertyExtensions
    {
        public static MemberInfo GetMemberInfo(this SerializedProperty property)
        {
            object target = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data[", "[");
            Type type = target.GetType();

            MemberInfo memberInfo = null;

            foreach (string part in path.Split('.'))
            {
                if (type == null) return null;

                string fieldName = part;
                int index = -1;

                // Handle collection index: "myList[0]"
                if (fieldName.Contains("["))
                {
                    int iStart = fieldName.IndexOf('[');
                    int iEnd = fieldName.IndexOf(']');
                    string indexStr = fieldName.Substring(iStart + 1, iEnd - iStart - 1);
                    int.TryParse(indexStr, out index);
                    fieldName = fieldName.Substring(0, iStart);
                }

                memberInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                             ?? (MemberInfo)type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (memberInfo == null)
                    return null;

                type = GetMemberType(memberInfo);

                // If list/array — unwrap element type
                if (index >= 0)
                {
                    if (type.IsArray)
                        type = type.GetElementType();
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                        type = type.GetGenericArguments()[0];
                    else
                        return null;
                }
            }

            return memberInfo;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => null
            };
        }
    }
}