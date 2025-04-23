using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using static LazyRedpaw.StaticHashes.Constants;
using static LazyRedpaw.StaticHashes.StaticHashesEditorHelper;
using Object = UnityEngine.Object;

namespace LazyRedpaw.StaticHashes
{
    public class StaticHashesWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset _treeAsset;
        [SerializeField] private VisualTreeAsset _hashCategoryAsset;
        [SerializeField] private VisualTreeAsset _hashItemListAsset;

        private static StaticHashesWindow _window;
        private CategoryElement _categoriesElement;
        private List<CategoryElement> _categories;
        private List<CategoryElement> _deletedCategories;
        private Button _createCategoryButton;
        private TextField _newCategoryNameField;
        private ScrollView _categoriesScrollView;
        private Button _expandButton;
        private VisualElement _nameExistingError;
        private Label _categoriesCountLabel;
        private VisualElement _unsavedChangesPanel;
        private Button _saveAllButton;
        private Button _revertAllButton;
        private Button _tmpSaveButton;
        private bool _isExpanded;
        private int _changesCount;
        private HashSet<int> _addedCategoryIds;
        private HashSet<int> _deletedCategoryIds;
        private bool _isNewCategoryAddedManually;
        
        private int ChangesCount
        {
            get => _changesCount;
            set
            {
                if(_changesCount == value) return;
                _changesCount = value;
                // CheckChanges();
            }
        }

        [MenuItem("Window/Static Hashes")]
        private static void OpenWindow()
        {
            _window = GetWindow<StaticHashesWindow>(StaticHashesWindowTitle);
        }

        private void CreateGUI()
        {
            _deletedCategories = new List<CategoryElement>();
            _categories = new List<CategoryElement>();
            _addedCategoryIds = new HashSet<int>();
            _deletedCategoryIds = new HashSet<int>();
            InitUiElements();
            CreateCategoriesList();
            SubscribeOnUiEvents();
        }

        private void SubscribeOnUiEvents()
        {
            _createCategoryButton.clicked += OnCreateCategoryClicked;
            _newCategoryNameField.RegisterValueChangedCallback(evt =>
                CheckNewNameField(_categories, evt, _newCategoryNameField, _nameExistingError, NameRegex, _createCategoryButton));
            _expandButton.clicked += OnExpandButtonClicked;
            _saveAllButton.clicked += OnSaveAllButtonClicked;
            _revertAllButton.clicked += OnRevertAllButtonClicked;
            _tmpSaveButton.clicked += OnSaveAllButtonClicked;
        }
        
        private void OnSaveAllButtonClicked()
        {
            bool isAnyHashExisting = false;
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].IsAnyHashExisting())
                {
                    isAnyHashExisting = true;
                    break;
                }
            }
            if(!isAnyHashExisting && _categories.Count == 0 && _deletedCategories.Count == 0) return;

            for (int i = 0; i < _categories.Count; i++)
            {
                if(_categories[i].ID == _categoriesElement.ID) continue;
                if (_categories[i].IsNameChanged)
                {
                    _categoriesElement.ChangeHash(_categories[i].SavedID, _categories[i].CategoryName);
                }
            }
            
            for (int i = 0; i < _categories.Count; i++)
            {
                CategoryElement category = _categories[i];
                if(category.IsNameChanged) UpdateFieldsValues(category.SavedID, category.ID);
                for (int j = 0; j < category.Hashes.Count; j++)
                {
                    HashElement hash = category.Hashes[j];
                    if(hash.IsNameChanged) UpdateFieldsValues(hash.SavedValue, hash.Value);
                }
            }
            UpdateStaticCalls();
            UpdateScriptFiles();
            AssetDatabase.Refresh();
        }
        
        private void UpdateStaticCalls()
        {
            string path = Application.dataPath;
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            

            for (int i = 0; i < files.Length; i++)
            {
                string fileContent = File.ReadAllText(files[i]);
                for (int j = 0; j < _categories.Count; j++)
                {
                    CategoryElement category = _categories[j];
                    Regex regex = new Regex(@$"(?<![a-zA-Z0-9]){category.SavedCategoryName}(?![a-zA-Z0-9])");
                    fileContent = regex.Replace(fileContent, category.CategoryName);
                    for (int k = 0; k < category.Hashes.Count; k++)
                    {
                        HashElement hash = category.Hashes[k];
                        regex = new Regex(@$"(?<![a-zA-Z0-9]){hash.SavedHashName}(?![a-zA-Z0-9])");
                        fileContent = regex.Replace(fileContent, hash.HashName);
                    }
                }
                File.WriteAllText(files[i], fileContent);
            }
        }

        private void UpdateScriptFiles()
        {
            List<string> storageScriptLines = new List<string>()
            {
                "//This file was generated automatically. Do not change it manually!",
                "namespace LazyRedpaw.StaticHashes",
                "{",
            };
            for (int i = 0; i < _categories.Count; i++)
            {
                storageScriptLines.AddRange(_categories[i].GetCategoryAsScriptLines());
            }
            storageScriptLines.Add("}");
            if (!File.Exists(StaticHashesStorageFilePath))
            {
                string directory = Path.GetDirectoryName(StaticHashesStorageFilePath);
                if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.Create(StaticHashesStorageFilePath).Close();
            }
            File.WriteAllLines(StaticHashesStorageFilePath, storageScriptLines);
        }

        private void OnRevertAllButtonClicked()
        {
            foreach (int categoryId in _addedCategoryIds)
            {
                DeleteCategoryCompletely(categoryId);
            }
            _addedCategoryIds.Clear();
            RestoreDeletedCategories();
            for (int i = 0; i < _categories.Count; i++)
            {
                _categories[i].RevertCompletely();
            }
            SortCategories();
        }

        private void RestoreDeletedCategories()
        {
            for (int i = 0; i < _deletedCategories.Count; i++)
            {
                _categories.Add(_deletedCategories[i]);
            }
            _deletedCategoryIds.Clear();
            _deletedCategories.Clear();
        }

        private void DeleteCategoryCompletely(int categoryId)
        {
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].ID == categoryId)
                {
                    _categories.RemoveAt(i);
                    break;
                }
            }
        }

        private void InitUiElements()
        {
            _treeAsset.CloneTree(rootVisualElement);
            _createCategoryButton = rootVisualElement.Q<Button>(CreateCategoryButton);
            _newCategoryNameField = rootVisualElement.Q<TextField>(NewCategoryNameField);
            _categoriesScrollView = rootVisualElement.Q<ScrollView>(CategoriesList);
            _expandButton = rootVisualElement.Q<Button>(ExpandButton);
            _nameExistingError = rootVisualElement.Q<VisualElement>(NameExistingErrorText);
            _categoriesCountLabel = rootVisualElement.Q<Label>(CategoriesCount);
            _unsavedChangesPanel = rootVisualElement.Q<VisualElement>(UnsavedChangesPanel);
            _saveAllButton = rootVisualElement.Q<Button>(SaveAllButton);
            _revertAllButton = rootVisualElement.Q<Button>(RevertAllButton);
            _tmpSaveButton = rootVisualElement.Q<Button>(TmpSaveButton);
            _nameExistingError.style.display = DisplayStyle.None;
            _categoriesScrollView.style.display = DisplayStyle.None;
            _unsavedChangesPanel.style.display = DisplayStyle.None;
            _createCategoryButton.SetEnabled(false);
        }
        
        private void CheckChanges()
        {
            if (ChangesCount > 0)
            {
                Debug.Log("TRUE");
                _unsavedChangesPanel.style.display = DisplayStyle.Flex;
                // _unsavedChangesPanel.schedule.Execute(() => _unsavedChangesPanel.style.display = DisplayStyle.Flex);
            }
            else
            {
                Debug.Log("FALSE");
                _unsavedChangesPanel.style.display = DisplayStyle.None;
                // _unsavedChangesPanel.schedule.Execute(() => _unsavedChangesPanel.style.display = DisplayStyle.None);
            }
        }
        
        private void OnExpandButtonClicked()
        {
            if (_isExpanded)
            {
                _isExpanded = false;
                _categoriesScrollView.style.display = DisplayStyle.None;
                _expandButton.RemoveFromClassList(ExpandButtonExpanded);
            }
            else
            {
                _isExpanded = true;
                _categoriesScrollView.style.display = DisplayStyle.Flex;
                _expandButton.AddToClassList(ExpandButtonExpanded);
            }
        }

        private void OnCreateCategoryClicked()
        {
            _isNewCategoryAddedManually = true;
            // List<string> scriptLines = File.ReadAllLines(AllStaticHashesFilePath).ToList();
            // List<int> addedHashes = new List<int>(names.Length);

            string categoryName = _newCategoryNameField.value;
            _newCategoryNameField.value = string.Empty;
            int id = GetNameHash(categoryName);
            var categoryElement = _deletedCategories.FirstOrDefault(h => h.CategoryName == categoryName);
            if (categoryElement != null)
            {
                _deletedCategories.Remove(categoryElement);
                AddExistingCategory(categoryElement);
            }
            else
            {
                AddNewCategory(categoryName, id);
            }
            SortCategories();
            // SortCategories(ref scriptLines);
            // File.WriteAllLines(AllStaticHashesFilePath, scriptLines);
            // AssetDatabase.Refresh();
            
            // CheckChanges();
        }

        public void AddNewCategory(string categoryName, int categoryID)
        {
            _hashCategoryAsset.CloneTree(_categoriesScrollView.contentContainer);
            VisualElement categoryRoot = _categoriesScrollView.contentContainer.Q<VisualElement>(CategoryRoot);
            categoryRoot.name = categoryName;
            CategoryElement newCategory = new CategoryElement(categoryID, categoryRoot, categoryName, _hashItemListAsset);
            newCategory.DeletionRequested += OnCategoryItemDeletionRequested;
            newCategory.NewNameFieldChanged += OnNewNameFieldChanged;
            newCategory.ChangeNameFieldChanged += OnChangeNameFieldChanged;
            newCategory.ChangesCountChanged += OnCategoryChangesCountChanged;
            _categories.Add(newCategory);
            UpdateCountLabel();
            _addedCategoryIds.Add(categoryID);
            if(_categoriesElement != null) _categoriesElement.AddHash(categoryName, categoryID);
            if (_isNewCategoryAddedManually)
            {
                _isNewCategoryAddedManually = false;
                _addedCategoryIds.Add(categoryID);
                ChangesCount++;
            }
        }

        private void UpdateCountLabel()
        {
            _categoriesCountLabel.text = "Items " + (_categories.Count - 1);
        }

        private void OnCategoryChangesCountChanged(int diff)
        {
            ChangesCount += diff;
        }

        public void AddExistingCategory(CategoryElement element)
        {
            _categoriesScrollView.contentContainer.Add(element.Root);
            element.DeletionRequested += OnCategoryItemDeletionRequested;
            element.NewNameFieldChanged += OnNewNameFieldChanged;
            element.ChangeNameFieldChanged += OnChangeNameFieldChanged;
            element.ChangesCountChanged += OnCategoryChangesCountChanged;
            ChangesCount += element.ChangesCount;
            _categories.Add(element);
            UpdateCountLabel();
            _categoriesElement.AddHash(element.CategoryName, element.ID);
            if (_deletedCategoryIds.Contains(element.ID))
            {
                _deletedCategoryIds.Remove(element.ID);
                ChangesCount--;
            }
            else
            {
                _addedCategoryIds.Add(element.ID);
                ChangesCount++;
            }
        }
        
        private void SortCategories()
        {
            _categories.Sort((a, b) => String.Compare(a.CategoryName.ToLower(),
                b.CategoryName.ToLower(), StringComparison.Ordinal));
            for (int i = 0; i < _categories.Count; i++)
            {
                _categoriesScrollView.Remove(_categories[i].Root);
                _categoriesScrollView.Insert(i, _categories[i].Root);
            }
        }

        private void OnNewNameFieldChanged(ChangeEvent<string> evt, TextField nameField, VisualElement errorElement,
            Regex nameRegex, Button createButton)
        {
            CheckNewNameField(_categories, evt, nameField, _nameExistingError, nameRegex, createButton);
        }
        
        private void OnChangeNameFieldChanged(ChangeEvent<string> evt, TextField nameField, int savedHash,
            VisualElement errorElement, Regex nameRegex, int currentHash)
        {
            CheckChangeNameField(_categories, evt, nameField, savedHash, errorElement, nameRegex, currentHash);
        }
        
        private void CreateCategoriesList()
        {
            if (File.Exists(StaticHashesStorageFilePath))
            {
                List<string> scriptLines = File.ReadAllLines(StaticHashesStorageFilePath).ToList();
                for (int i = 0; i < scriptLines.Count; i++)
                {
                    if (scriptLines[i].Contains("class"))
                    {
                        GetNameAndHashFromScriptLine(scriptLines[i], out string categoryName, out int categoryID);
                        AddNewCategory(categoryName, categoryID);
                    }
                    else if(scriptLines[i].Contains("int"))
                    {
                        GetNameAndHashFromScriptLine(scriptLines[i], out string hashName, out int hash);
                        _categories[^1].AddHash(hashName, hash);
                    }
                }
            }
            int categoriesID = GetNameHash(CategoriesName);
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].ID == categoriesID)
                {
                    _categoriesElement = _categories[i];
                    break;
                }
            }
            if (_categoriesElement == null)
            {
                AddNewCategory(CategoriesName, categoriesID);
                _categoriesElement = _categories[^1];
            }
            _categoriesElement.SetDisplay(false);
            // File.WriteAllLines(AllStaticHashesFilePath, scriptLines);
            // AssetDatabase.Refresh();
        }

        private void GetNameAndHashFromScriptLine(string scriptLine, out string hashName, out int hash)
        {
            string[] splitValues = scriptLine.Split(' ');
            hashName = splitValues[4];
            hash = int.Parse(splitValues[^1].Replace(";", string.Empty));
        }
        
        private string GetNameFromScriptLine(string scriptLine)
        {
            string[] splitValues = scriptLine.Split(' ');
            return splitValues[3];
        }

        private void OnCategoryItemDeletionRequested(CategoryElement item)
        {
            item.DeletionRequested -= OnCategoryItemDeletionRequested;
            item.NewNameFieldChanged -= OnNewNameFieldChanged;
            item.ChangeNameFieldChanged -= OnChangeNameFieldChanged;
            item.ChangesCountChanged -= OnCategoryChangesCountChanged;
            ChangesCount -= item.ChangesCount;
            _categories.Remove(item);
            _deletedCategories.Add(item);
            _categoriesElement.DeleteHash(item.ID);
            _categoriesCountLabel.text = "Items " + _categories.Count;
            if (_addedCategoryIds.Contains(item.ID))
            {
                _addedCategoryIds.Remove(item.ID);
                ChangesCount--;
            }
            else
            {
                _deletedCategoryIds.Add(item.ID);
                ChangesCount++;
            }
            // CheckChanges();
        }

        private void UpdateFieldsValues(int oldValue, int newValue)
        {
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (assetPaths[i].StartsWith("Assets") &&
                    (assetPaths[i].EndsWith(".prefab") || assetPaths[i].EndsWith(".asset") || assetPaths[i].EndsWith(".unity")))
                {
                    UpdateFieldValue(assetPaths[i], oldValue, newValue);
                }
            }
            Debug.unityLogger.logEnabled = false;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.unityLogger.logEnabled = true;
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }

        private void UpdateFieldValue(string path, int oldValue, int newValue)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if(asset == null) return;
            bool assetChanged = false;

            if (asset is GameObject)
            {
                GameObject gameObject = (GameObject)asset;
                assetChanged = UpdateFieldInGameObject(gameObject, oldValue, newValue);
            }
            else if (path.EndsWith(".unity"))
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                List<GameObject> sceneObjects = GetAllSceneObjects(scene);
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    assetChanged |= UpdateFieldInGameObject(sceneObjects[i], oldValue, newValue);
                }
                if (assetChanged)
                {
                    Debug.unityLogger.logEnabled = false;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.unityLogger.logEnabled = true;
                }
            }
            else
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                assetChanged = UpdateFieldInSerializedObject(serializedObject, oldValue, newValue);
            }

            if (assetChanged) EditorUtility.SetDirty(asset);
        }

        private bool UpdateFieldInGameObject(GameObject gameObject, int oldValue, int newValue)
        {
            bool objectChanged = false;
            Component[] components = gameObject.GetComponentsInChildren<Component>(true);

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue; // In case of missing scripts
                SerializedObject serializedObject = new SerializedObject(components[i]);
                objectChanged |= UpdateFieldInSerializedObject(serializedObject, oldValue, newValue);
            }
            return objectChanged;
        }

        private bool UpdateFieldInSerializedObject(SerializedObject serializedObject, int oldValue, int newValue)
        {
            bool objectChanged = false;
            SerializedProperty property = serializedObject.GetIterator();
            while (property.NextVisible(true))
            {
                if (IsFieldValid(property))
                {
                    if (property.propertyType == SerializedPropertyType.Integer &&
                        property.intValue == oldValue)
                    {
                        property.intValue = newValue;
                        objectChanged = true;
                    }
                    else if (property.propertyType == SerializedPropertyType.Generic)
                    {
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            SerializedProperty prop = property.GetArrayElementAtIndex(i);
                            if (prop.intValue == oldValue)
                            {
                                prop.intValue = newValue;
                                objectChanged = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (objectChanged) serializedObject.ApplyModifiedProperties();
            return objectChanged;
        }

        private bool IsFieldValid(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetType().GetMembers(FieldsBindingFlags).Any(member =>
                member.Name == property.name && Attribute.IsDefined(member, typeof(StaticHashDropdownAttribute)));
        }

        private List<GameObject> GetAllSceneObjects(Scene scene)
        {
            List<GameObject> sceneObjects = new List<GameObject>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                sceneObjects.AddRange(obj.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject));
            }
            return sceneObjects;
        }
    }
}