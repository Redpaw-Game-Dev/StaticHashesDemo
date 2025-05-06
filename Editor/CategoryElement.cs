using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using static LazyRedpaw.StaticHashes.Constants;
using static LazyRedpaw.StaticHashes.StaticHashesEditorHelper;

namespace LazyRedpaw.StaticHashes
{
    public class CategoryElement
    {
        private readonly int _savedID;
        private readonly VisualElement _categoryRoot;
        private readonly VisualElement _categoryContainer;
        private readonly TextField _newHashNameField;
        private readonly TextField _changeCategoryNameField;
        private readonly Button _createHashButton;
        private readonly Button _deleteCategoryButton;
        private readonly Button _expandHashesButton;
        private readonly Button _expandCategoryButton;
        private readonly Button _revertButton;
        private readonly ScrollView _hashScrollView;
        private readonly VisualElement _hashNameExistingError;
        private readonly VisualElement _categoryNameExistingError;
        private readonly string _savedCategoryName;
        private readonly Label _hashesCountLabel;
        private readonly List<HashElement> _hashes;
        private readonly List<HashElement> _deletedHashes;
        private readonly VisualTreeAsset _hashTreeAsset;
        private readonly HashSet<int> _addedHashValues;
        private readonly HashSet<int> _deletedHashValues;

        private bool _isHashesExpanded;
        private bool _isCategoryExpanded;
        private int _hashesCount;
        private int _id;
        private int _changesCount;
        private bool _isNewHashAddedManually;

        public string CategoryName => _changeCategoryNameField.value;
        public string SavedCategoryName => _savedCategoryName;
        public int ID => _id;
        public int SavedID => _savedID;
        public List<HashElement> Hashes => _hashes;
        public VisualElement Root => _categoryRoot;
        public bool IsChanged => _changesCount > 0;
        public bool IsNameChanged => _id != _savedID;

        public int ChangesCount
        {
            get => _changesCount;
            private set
            {
                if(_changesCount == value) return;
                int diff = value - _changesCount;
                _changesCount = value;
                ChangesCountChanged?.Invoke(diff);
            }
        }

        public event Action<CategoryElement> DeletionRequested;
        public event Action<ChangeEvent<string>, TextField, VisualElement, Regex, Button> NewNameFieldChanged;
        public event Action<ChangeEvent<string>, TextField, int, VisualElement, Regex, int> ChangeNameFieldChanged;
        public event Action<int> ChangesCountChanged;//int arg - difference between new value and previous value

        public CategoryElement(int id, VisualElement categoryRoot, string categoryName, VisualTreeAsset hashTreeAsset)
        {
            _id = id;
            _savedID = id;
            _categoryRoot = categoryRoot;
            _savedCategoryName = categoryName;
            _newHashNameField = _categoryRoot.Q<TextField>(NewHashNameField);
            _changeCategoryNameField = _categoryRoot.Q<TextField>(ChangeCategoryNameField);
            _changeCategoryNameField.value = categoryName;
            _createHashButton = _categoryRoot.Q<Button>(CreateHashButton);
            _deleteCategoryButton = _categoryRoot.Q<Button>(DeleteButton);
            _expandHashesButton = _categoryRoot.Q<Button>(ExpandHashesButton);
            _expandCategoryButton = _categoryRoot.Q<Button>(ExpandCategoryButton);
            _revertButton = _categoryRoot.Q<Button>(RevertButton);
            _hashScrollView = _categoryRoot.Q<ScrollView>(HashList);
            _hashNameExistingError = _categoryRoot.Q<VisualElement>(NewHashNameExistingErrorTextUxml);
            _categoryNameExistingError = _categoryRoot.Q<VisualElement>(CategoryNameExistingErrorTextUxml);
            _categoryContainer = _categoryRoot.Q<VisualElement>(CategoryContent);
            _hashesCountLabel = _categoryRoot.Q<Label>(HashListItemsCount);
            _hashNameExistingError.style.display = DisplayStyle.None;
            _categoryNameExistingError.style.display = DisplayStyle.None;
            _hashScrollView.style.display = DisplayStyle.None;
            _categoryContainer.style.display = DisplayStyle.None;
            _hashes = new List<HashElement>();
            _deletedHashes = new List<HashElement>();
            _addedHashValues = new HashSet<int>();
            _deletedHashValues = new HashSet<int>();
            _hashTreeAsset = hashTreeAsset;
            _revertButton.SetEnabled(false);
            _createHashButton.SetEnabled(false);

            _newHashNameField.RegisterValueChangedCallback(OnNewHashNameFieldChanged);
            _changeCategoryNameField.RegisterValueChangedCallback(OnCategoryNameFieldChanged);
            _deleteCategoryButton.clicked += OnDeleteCategoryButtonClicked;
            _createHashButton.clicked += OnCreateHashButtonClicked;
            _expandHashesButton.clicked += OnExpandHashesButtonClicked;
            _expandCategoryButton.clicked += OnExpandCategoryButtonClicked;
            _revertButton.clicked += OnRevertButtonClicked;
        }

        public void SetDisplay(bool value)
        {
            if(value) _categoryRoot.style.display = DisplayStyle.Flex;
            else _categoryRoot.style.display = DisplayStyle.None;
        }

        public string GetCategoryAsHashScriptLine() => $"\t\tpublic static readonly int {_changeCategoryNameField.value} = {_id};";

        public string GetHashesArrayAsString()
        {
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < _hashes.Count; i++)
            {
                if(i > 0) strBuilder.Append(',');
                strBuilder.Append($" {_hashes[i].Value}");
            }
            return strBuilder.ToString();
        }
        
        public string GetCategoryHashesAsScriptLine()
        {
            StringBuilder strBuilder = new StringBuilder($"\t\tpublic static readonly int[] {CategoryName}{HashesArray} = ");
            strBuilder.Append('{');
            for (int i = 0; i < _hashes.Count; i++)
            {
                if(i > 0) strBuilder.Append(',');
                strBuilder.Append($" {_hashes[i].Value}");
            }
            strBuilder.Append(" };");
            return strBuilder.ToString();
        }
        
        public string GetHashNamesArrayAsString()
        {
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < _hashes.Count; i++)
            {
                if(i > 0) strBuilder.Append(',');
                strBuilder.Append($" \"{_hashes[i].HashName}\"");
            }
            return strBuilder.ToString();
        }
        
        public string GetCategoryHashNamesAsScriptLine()
        {
            StringBuilder strBuilder = new StringBuilder($"\t\tpublic static readonly string[] {CategoryName}{HashNamesArray} = ");
            strBuilder.Append('{');
            for (int i = 0; i < _hashes.Count; i++)
            {
                if(i > 0) strBuilder.Append(',');
                strBuilder.Append($" \"{_hashes[i].HashName}\"");
            }
            strBuilder.Append(" };");
            return strBuilder.ToString();
        }
        
        public List<string> GetCategoryAsScriptLines()
        {
            SortHashes();
            List<string> lines = new List<string>(_hashes.Count + 3);
            lines.Add($"\tpublic abstract partial class {_changeCategoryNameField.value} : StaticHashesCategory // {_id}");
            lines.Add("\t{");
            for (int i = 0; i < _hashes.Count; i++)
            {
                lines.Add(_hashes[i].GetHashAsScriptLine());
            }
            lines.Add("\t}");
            return lines;
        }
        
        public void Revert()
        {
            _changeCategoryNameField.value = _savedCategoryName;
        }

        public void RevertCompletely()
        {
            Revert();
            foreach (int hashValue in _addedHashValues)
            {
                DeleteHashCompletely(hashValue);
            }
            _addedHashValues.Clear();
            RestoreDeletedHashes();
            for (int i = 0; i < _hashes.Count; i++)
            {
                _hashes[i].Revert();
            }
            SortHashes();
        }

        public bool IsHashExisting(int hash, params int[] ignoreHashes)
        {
            for (int i = 0; i < _hashes.Count; i++)
            {
                if(ignoreHashes != null && ignoreHashes.Contains(_hashes[i].Value)) continue;
                if(_hashes[i].Value == hash) return true;
            }
            return false;
        } 

        public void AddHash(string hashName, int hashValue)
        {
            var hashElement = _deletedHashes.FirstOrDefault(h => h.Value == hashValue);
            if (hashElement != null)
            {
                _deletedHashes.Remove(hashElement);
                AddExistingHash(hashElement);
            }
            else if (_hashes.All(item => GetNameHash(item.HashName) != hashValue))
            {
                AddNewHash(hashName, hashValue);
            }
            SortHashes();
        }

        public void DeleteHash(int hashValue)
        {
            for (int i = 0; i < _hashes.Count; i++)
            {
                if (_hashes[i].Value == hashValue)
                {
                    HashElement element = _hashes[i];
                    element.HashNameFieldChanged -= OnHashNameFieldChanged;
                    element.DeletionRequested -= OnHashDeletionRequested;
                    element.ChangeDone -= OnHashChangeDone;
                    if (element.IsChanged) ChangesCount--;
                    _hashScrollView.contentContainer.Remove(element.Root);
                    _hashesCountLabel.text = "Items " + --_hashesCount;
                    _hashes.RemoveAt(i);
                    _deletedHashes.Add(element);
                    break;
                }
            }
        }

        public void ChangeHash(int oldValue, string newName)
        {
            for (int i = 0; i < _hashes.Count; i++)
            {
                if (_hashes[i].Value == oldValue)
                {
                    _hashes[i].Change(newName);
                }
            }
        }
        
        public void AddNewHash(string hashName, int hashValue)
        {
            _hashTreeAsset.CloneTree(_hashScrollView.contentContainer);
            _hashesCountLabel.text = "Items " + ++_hashesCount;
            VisualElement hashRoot = _hashScrollView.contentContainer.Q<VisualElement>(HashListItem);
            HashElement newHash = new HashElement(hashValue, hashRoot, hashName);
            newHash.HashNameFieldChanged += OnHashNameFieldChanged;
            newHash.DeletionRequested += OnHashDeletionRequested;
            newHash.ChangeDone += OnHashChangeDone;
            _hashes.Add(newHash);
            if (_isNewHashAddedManually)
            {
                _isNewHashAddedManually = false;
                _addedHashValues.Add(hashValue);
                ChangesCount++;
            }
        }

        public bool IsAnyHashExisting()
        {
            return _hashes.Count > 0 || _deletedHashes.Count > 0;
        }
        
        private void RestoreDeletedHashes()
        {
            for (int i = 0; i < _deletedHashes.Count; i++)
            {
                _hashes.Add(_deletedHashes[i]);
            }
            _deletedHashes.Clear();
            _deletedHashValues.Clear();
        }

        private void DeleteHashCompletely(int hashValue)
        {
            for (int i = 0; i < _hashes.Count; i++)
            {
                if (_hashes[i].Value == hashValue)
                {
                    _hashes.RemoveAt(i);
                    break;
                }
            }
        }
        
        private void OnHashChangeDone(bool value)
        {
            if (value) ChangesCount++;
            else ChangesCount--;
        }

        private void SortHashes()
        {
            _hashes.Sort((a, b) => String.Compare(a.HashName.ToLower(),
                b.HashName.ToLower(), StringComparison.Ordinal));
            for (int i = 0; i < _hashes.Count; i++)
            {
                _hashScrollView.Remove(_hashes[i].Root);
                _hashScrollView.Insert(i, _hashes[i].Root);
            }
        }
        
        private void OnRevertButtonClicked()
        {
            Revert();
        }

        private void AddExistingHash(HashElement element)
        {
            _hashScrollView.contentContainer.Add(element.Root);
            _hashesCountLabel.text = "Items " + ++_hashesCount;
            element.HashNameFieldChanged += OnHashNameFieldChanged;
            element.DeletionRequested += OnHashDeletionRequested;
            element.ChangeDone += OnHashChangeDone;
            if(element.IsChanged) ChangesCount++;
            _hashes.Add(element);
            if (_deletedHashValues.Contains(element.Value))
            {
                _deletedHashValues.Remove(element.Value);
                ChangesCount--;
            }
            else
            {
                _addedHashValues.Add(element.Value);
                ChangesCount++;
            }
        }

        private void OnHashDeletionRequested(HashElement element)
        {
            element.HashNameFieldChanged -= OnHashNameFieldChanged;
            element.DeletionRequested -= OnHashDeletionRequested;
            element.ChangeDone -= OnHashChangeDone;
            if (element.IsChanged) ChangesCount--;
            _hashScrollView.contentContainer.Remove(element.Root);
            _hashesCountLabel.text = "Items " + --_hashesCount;
            _hashes.Remove(element);
            _deletedHashes.Add(element);
            if (_addedHashValues.Contains(element.Value))
            {
                _addedHashValues.Remove(element.Value);
                ChangesCount--;
            }
            else
            {
                _deletedHashValues.Add(element.Value);
                ChangesCount++;
            }
        }

        private void OnHashNameFieldChanged(ChangeEvent<string> evt, TextField nameField, int savedHash,
            VisualElement errorElement, Regex nameRegex, int currentHash = 0)
        {
            ChangeNameFieldChanged?.Invoke(evt, nameField, savedHash, errorElement, nameRegex, currentHash);
        }

        private void OnCreateHashButtonClicked()
        {
            _isNewHashAddedManually = true;
            string name = _newHashNameField.value;
            int hash = GetNameHash(name);
            AddHash(name, hash);
            _newHashNameField.value = string.Empty;
        }

        private void OnCategoryNameFieldChanged(ChangeEvent<string> evt)
        {
            ChangeNameFieldChanged?.Invoke(evt, _changeCategoryNameField, _savedID, _categoryNameExistingError, NameRegex, _savedID);
            int newID = GetNameHash(evt.newValue);
            if (newID != _savedID)
            {
                _revertButton.SetEnabled(true);
                ChangesCount++;
            }
            else
            {
                _revertButton.SetEnabled(false);
                ChangesCount--;
            }

            _id = newID;
        }

        private void OnNewHashNameFieldChanged(ChangeEvent<string> evt)
        {
            NewNameFieldChanged?.Invoke(evt, _newHashNameField, _hashNameExistingError, NameRegex, _createHashButton);
        }

        private void OnDeleteCategoryButtonClicked()
        {
            _categoryRoot.RemoveFromHierarchy();
            DeletionRequested?.Invoke(this);
        }

        private void OnExpandHashesButtonClicked()
        {
            SwitchFoldoutState(ref _isHashesExpanded, _hashScrollView, _expandHashesButton);
        }
        
        private void OnExpandCategoryButtonClicked()
        {
            SwitchFoldoutState(ref _isCategoryExpanded, _categoryContainer, _expandCategoryButton);
        }

        private void SwitchFoldoutState(ref bool isExpanded, VisualElement visualElement, Button expandButton)
        {
            if (isExpanded)
            {
                isExpanded = false;
                visualElement.style.display = DisplayStyle.None;
                expandButton.RemoveFromClassList(ExpandButtonExpanded);
            }
            else
            {
                isExpanded = true;
                visualElement.style.display = DisplayStyle.Flex;
                expandButton.AddToClassList(ExpandButtonExpanded);
            }
        }
    }
}