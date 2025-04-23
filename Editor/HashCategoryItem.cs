using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static LazyRedpaw.StaticHashes.Constants;

namespace LazyRedpaw.StaticHashes
{
    public class HashCategoryItem
    {
        private readonly int _hash;
        private readonly Foldout _categoryFoldout;
        private readonly TextField _newHashNameField;
        private readonly TextField _changeCategoryNameField;
        private readonly Button _createHashButton;
        private readonly Button _deleteCategoryButton;
        private readonly Button _changeCategoryNameButton;
        private readonly Button _expandButton;
        private readonly ScrollView _hashScrollView;
        private readonly VisualElement _hashNameExistingError;
        private readonly VisualElement _categoryNameExistingError;
        private readonly string _categoryName;
        private readonly Label _hashesCountLabel;
        private readonly List<HashItem> _hashItems;

        private bool _isExpanded;
        private int _hashesCount;
        
        public int Hash => _hash;
        public string CategoryName => _categoryName;
        
        public event Action<HashCategoryItem> DeletionRequested;
        public event Action<ChangeEvent<string>, TextField, VisualElement, Button, int> NameFieldChanged;
        public event Action<int, string> HashCreationRequested;
        
        public HashCategoryItem(int hash, Foldout categoryFoldout, string categoryName)
        {
            _hash = hash;
            _categoryFoldout = categoryFoldout;
            _categoryName = categoryName;
            _categoryFoldout.text = _categoryName;
            _newHashNameField = _categoryFoldout.Q<TextField>(NewHashNameField);
            _changeCategoryNameField = _categoryFoldout.Q<TextField>(ChangeCategoryNameField);
            _createHashButton = _categoryFoldout.Q<Button>(CreateHashButton);
            _deleteCategoryButton = _categoryFoldout.Q<Button>(DeleteCategoryButton);
            _changeCategoryNameButton = _categoryFoldout.Q<Button>(ChangeCategoryNameButton);
            _expandButton = _categoryFoldout.Q<Button>(ExpandButton);
            _hashScrollView = _categoryFoldout.Q<ScrollView>(HashList);
            _hashNameExistingError = _categoryFoldout.Q<VisualElement>(NewHashNameExistingErrorTextUxml);
            _categoryNameExistingError = _categoryFoldout.Q<VisualElement>(CategoryNameExistingErrorTextUxml);
            _hashesCountLabel = _categoryFoldout.Q<Label>(HashListItemsCount);
            _hashNameExistingError.style.display = DisplayStyle.None;
            _categoryNameExistingError.style.display = DisplayStyle.None;
            _hashScrollView.style.display = DisplayStyle.None;
            _hashItems = new List<HashItem>();

            _newHashNameField.RegisterValueChangedCallback(OnNewHashNameFieldChanged);
            _changeCategoryNameField.RegisterValueChangedCallback(OnCategoryNameFieldChanged);
            _deleteCategoryButton.clicked += OnDeleteCategoryButtonClicked;
            _createHashButton.clicked += OnCreateHashButtonClicked;
            _expandButton.clicked += OnExpandButtonClicked;
        }
        
        public VisualElement AddHashVisualElement(VisualTreeAsset itemTreeAsset)
        {
            itemTreeAsset.CloneTree(_hashScrollView.contentContainer);
            _hashesCountLabel.text = "Items " + ++_hashesCount;
            return _hashScrollView.contentContainer.Q<VisualElement>(HashListItem);
        }
        
        public void AddHashItem(HashItem item)
        {
            if(!_hashItems.Contains(item)) _hashItems.Add(item);
        }
        
        private void OnCreateHashButtonClicked()
        {
            HashCreationRequested?.Invoke(_hash, _newHashNameField.value);
        }
        
        private void OnCategoryNameFieldChanged(ChangeEvent<string> evt)
        {
            NameFieldChanged?.Invoke(evt, _changeCategoryNameField, _categoryNameExistingError, _changeCategoryNameButton, _hash);
        }
        
        private void OnNewHashNameFieldChanged(ChangeEvent<string> evt)
        {
            NameFieldChanged?.Invoke(evt, _newHashNameField, _hashNameExistingError, _createHashButton, _hash);
        }

        private void OnDeleteCategoryButtonClicked()
        {
            DeletionRequested?.Invoke(this);
        }
        
        private void OnExpandButtonClicked()
        {
            if (_isExpanded)
            {
                _isExpanded = false;
                _hashScrollView.style.display = DisplayStyle.None;
                _expandButton.RemoveFromClassList(ExpandButtonExpanded);
            }
            else
            {
                _isExpanded = true;
                _hashScrollView.style.display = DisplayStyle.Flex;
                _expandButton.AddToClassList(ExpandButtonExpanded);
            }
        }
    }
}