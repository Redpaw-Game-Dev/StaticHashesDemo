using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static LazyRedpaw.StaticHashes.StaticHashesEditorHelper;

namespace LazyRedpaw.StaticHashes
{
    [CustomPropertyDrawer(typeof(StaticHashDropdownAttribute))]
    public class StaticHashDropdownAttributeDrawer : PropertyDrawer
    {
        private readonly string constrTypeError =
            "StaticHashDropdownAttribute attribute must be of type inherited from StaticHashCategory.";

        private readonly string fieldTypeError = "StaticHashDropdownAttribute attribute must be of type int.";
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, fieldTypeError);
                Debug.LogError(fieldTypeError);
                return;
            }
            
            StaticHashDropdownAttribute dropdownAttribute = (StaticHashDropdownAttribute)attribute;
            
            FieldInfo typeFieldInfo = dropdownAttribute.GetType().GetField("_type", BindingFlags.NonPublic | BindingFlags.Instance);
            Type type = (Type)typeFieldInfo.GetValue(dropdownAttribute);

            if (type != null && !type.IsSubclassOf(typeof(StaticHashesCategory)))
            {
                EditorGUI.LabelField(position, constrTypeError);
                Debug.LogError(constrTypeError);
                return;
            }
            
            int[] hashes = null;
            GUIContent[] labels = null;
            if(type != null) GetAllHashesFromCategory(type, out hashes, out labels);
            else GetAllStaticHashes(out hashes, out labels);
            
            int selectedIndex = GetSelectedHashIndex(property.intValue, hashes);
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
                property.intValue = hashes[selectedIndex];
            }
            
            int newIndex = EditorGUI.Popup(position, label, selectedIndex, labels);
            property.intValue = hashes[newIndex];
        }
        
        private int GetSelectedHashIndex(int selectedHash, int[] hashes)
        {
            for (int i = 0; i < hashes.Length; i++)
            {
                if (selectedHash == hashes[i]) return i;
            }
            return -1;
        }
    }
}