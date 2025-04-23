using System;
using System.Diagnostics;
using UnityEngine;

namespace LazyRedpaw.StaticHashes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class StaticHashDropdownAttribute : PropertyAttribute
    {
        private Type _type;

        public StaticHashDropdownAttribute(Type type = null)
        {
            _type = type;
        }
    }
}