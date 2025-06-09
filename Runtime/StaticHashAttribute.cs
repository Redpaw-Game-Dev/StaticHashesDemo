using System;
using System.Diagnostics;
using UnityEngine;

namespace LazyRedpaw.StaticHashes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class StaticHashAttribute : PropertyAttribute
    {
        
    }
}