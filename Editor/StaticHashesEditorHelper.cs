using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static LazyRedpaw.StaticHashes.Constants;

namespace LazyRedpaw.StaticHashes
{
    public static class StaticHashesEditorHelper
    {
        public static void GetAllStaticHashes(out int[] hashes, out GUIContent[] labels)
        {
            if (File.Exists(StaticHashesStorageFilePath))
            {
                string[] scriptLines = File.ReadAllLines(StaticHashesStorageFilePath);
                List<int> hashList =  new List<int>(scriptLines.Length);
                List<GUIContent> labelsList = new List<GUIContent>(scriptLines.Length);
                if (hashList.Count > 0)
                {
                    for (int i = InsertCategoryIndex; i < scriptLines.Length; i++)
                    {
                        if ((scriptLines[i].Contains("int") || scriptLines[i].Contains("class")) && !scriptLines[i].Contains(CategoriesName))
                        {
                            string[] splitLines = scriptLines[i].Split(' ');
                            hashList.Add(int.Parse(splitLines[^1].Replace(";", string.Empty)));
                            labelsList.Add(new GUIContent(splitLines[4]));
                        }
                    }
                }
                hashes = hashList.ToArray();
                labels = labelsList.ToArray();
            }
            hashes = new[] { 0 };
            labels = new[] { new GUIContent("No existing static hashes") };
        }
        
        public static void GetAllHashesFromCategory(int categoryHash, out int[] hashes, out GUIContent[] labels)
        {
            var scriptLines = File.ReadAllLines(StaticHashesStorageFilePath);
            List<int> hashList =  new List<int>(scriptLines.Length);
            List<GUIContent> labelsList = new List<GUIContent>(scriptLines.Length);
            bool isCategoryFound = false;
            for (int i = InsertCategoryIndex; i < scriptLines.Length; i++)
            {
                if (scriptLines[i].Contains('{') || scriptLines[i].Contains('}'))
                {
                    if(isCategoryFound) break;
                    else continue;
                }
                if (scriptLines[i].Contains("class"))
                {
                    int otherHash = int.Parse(scriptLines[i].Split(' ')[^1].Replace(";", string.Empty));
                    if (categoryHash == otherHash)
                    {
                        isCategoryFound = true;
                        i += 2;
                    }
                }
                if (isCategoryFound)
                {
                    string[] splitLines = scriptLines[i].Split(' ');
                    hashList.Add(int.Parse(splitLines[^1].Replace(";", string.Empty)));
                    labelsList.Add(new GUIContent(splitLines[^3]));
                }
            }
            hashes = hashList.ToArray();
            labels = labelsList.ToArray();
        }
        
        public static void GetAllHashesFromCategory(Type categoryType, out int[] hashes, out GUIContent[] labels)
        {
            var scriptLines = File.ReadAllLines(StaticHashesStorageFilePath);
            List<int> hashList =  new List<int>(scriptLines.Length);
            List<GUIContent> labelsList = new List<GUIContent>(scriptLines.Length);
            bool isCategoryFound = false;
            for (int i = InsertCategoryIndex; i < scriptLines.Length; i++)
            {
                if (scriptLines[i].Contains('{') || scriptLines[i].Contains('}'))
                {
                    if(isCategoryFound) break;
                    else continue;
                }
                if (scriptLines[i].Contains("class"))
                {
                    string otherName = scriptLines[i].Split(' ')[4];
                    if (categoryType.Name == otherName)
                    {
                        isCategoryFound = true;
                        i += 2;
                    }
                }
                if (isCategoryFound)
                {
                    string[] splitLines = scriptLines[i].Split(' ');
                    hashList.Add(int.Parse(splitLines[^1].Replace(";", string.Empty)));
                    labelsList.Add(new GUIContent(splitLines[4]));
                }
            }
            hashes = hashList.ToArray();
            labels = labelsList.ToArray();
        }
        
        public static int GetNameHash(string name)
        {
            string lowerCaseName = name.ToLower();
            return Animator.StringToHash(lowerCaseName);
        }
        
        public static void CheckNewNameField(List<CategoryElement> categories, ChangeEvent<string> evt, TextField nameField, VisualElement errorElement,
            Regex nameRegex, Button createButton)
        {
            if (evt.newValue != string.Empty && !nameRegex.IsMatch(evt.newValue))
            {
                nameField.value = evt.previousValue;
            }
            else
            {
                string[] names = evt.newValue.Split(',');
                for (int i = 0; i < names.Length; i++)
                {
                    CheckNameField(categories, names[i], nameField, errorElement, acceptButton: createButton);
                }
            }
        }
        
        public static void CheckChangeNameField(List<CategoryElement> categories, ChangeEvent<string> evt, TextField nameField, int savedHash,
            VisualElement errorElement, Regex nameRegex, int currentHash)
        {
            if (evt.newValue != string.Empty && !nameRegex.IsMatch(evt.newValue)) nameField.value = evt.previousValue;
            else CheckNameField(categories, evt.newValue, nameField, errorElement, currentHash, savedHash);
        }

        public static void CheckNameField(List<CategoryElement> categories, string nameStr, TextField nameField, VisualElement errorElement,
            int currentHash = 0, int savedHash = 0, Button acceptButton = null)
        {
            int hash = GetNameHash(nameStr);
            int[] ignoreHashes = currentHash != 0 ? new[] { currentHash } : null;
            if (IsHashExisting(categories, hash, ignoreHashes))
            {
                errorElement.style.display = DisplayStyle.Flex;
                if (acceptButton != null) acceptButton.SetEnabled(false);
                nameField.RemoveFromClassList(ChangedBorder);
                nameField.AddToClassList(ErrorBorder);
            }
            else
            {
                errorElement.style.display = DisplayStyle.None;
                if (acceptButton != null) acceptButton.SetEnabled(true);
                nameField.RemoveFromClassList(ErrorBorder);
                if(savedHash != 0 && savedHash != hash) nameField.AddToClassList(ChangedBorder);
                else nameField.RemoveFromClassList(ChangedBorder);
            }
            if(nameStr == string.Empty && acceptButton != null) acceptButton.SetEnabled(false);
        }
        
        public static bool IsHashExisting(List<CategoryElement> categories, int hash, params int[] ignoreHashes)
        {
            for (int j = 0; j < categories.Count; j++)
            {
                CategoryElement category = categories[j];
                if(ignoreHashes != null && ignoreHashes.Contains(category.ID)) continue;
                if (hash == category.ID) return true;
                if(category.IsHashExisting(hash, ignoreHashes)) return true;
            }
            return false;
        }
    }
}