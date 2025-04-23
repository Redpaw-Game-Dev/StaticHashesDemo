using System;
using UnityEngine.UIElements;
using static LazyRedpaw.StaticHashes.Constants;

namespace LazyRedpaw.StaticHashes
{
    public class HashItem
    {
        private readonly int _hash;
        private readonly int _categoryHash;
        private readonly string _hashName;
        private readonly VisualElement _root;
        private readonly TextField _hashNameField;
        private readonly Button _applyButton;
        private readonly Button _revertButton;
        private readonly Button _deleteButton;
        private readonly VisualElement _existingNameError;

        public int Hash => _hash;
        public int CategoryHash => _categoryHash;
        public string HashName => _hashName;
        
        public event Action<HashItem> DeletionRequested;
        public event Action<ChangeEvent<string>, TextField, VisualElement, Button, int> HashNameFieldChanged;
        public event Action<int, int, string> HashChangeRequested;
        
        public HashItem(int hash, int categoryHash, VisualElement root, string hashName)
        {
            _hash = hash;
            _categoryHash = categoryHash;
            _root = root;
            _hashName = hashName;
            _root.name = _hashName;
            _hashNameField = _root.Q<TextField>(HashNameField);
            _hashNameField.value = _hashName;
            _applyButton = _root.Q<Button>(ApplyButton);
            _revertButton = _root.Q<Button>(RevertButton);
            _deleteButton = _root.Q<Button>(DeleteButton);
            _existingNameError = _root.Q<VisualElement>(NameExistingErrorText);
            _existingNameError.style.display = DisplayStyle.None;
            _revertButton.SetEnabled(false);
            _applyButton.SetEnabled(false);

            _hashNameField.RegisterValueChangedCallback(OnHashNameFieldChanged);
            _deleteButton.clicked += OnDeleteButtonClicked;
            _applyButton.clicked += OnApplyButtonClicked;
            _revertButton.clicked += OnRevertButtonClicked;
        }

        private void OnRevertButtonClicked()
        {
            _hashNameField.value = _hashName;
            _revertButton.SetEnabled(false);
        }
        
        private void OnApplyButtonClicked()
        {
            HashChangeRequested?.Invoke(_hash, _categoryHash, _hashNameField.value);
        }

        private void OnHashNameFieldChanged(ChangeEvent<string> evt)
        {
            HashNameFieldChanged?.Invoke(evt, _hashNameField, _existingNameError, _applyButton, _hash);
            
            if (evt.newValue != _hashName)
            {
                if (_applyButton.enabledSelf)
                {
                    _hashNameField.RemoveFromClassList(ErrorBorder);
                    _hashNameField.AddToClassList(ChangedBorder);
                }
                else
                {
                    _hashNameField.RemoveFromClassList(ChangedBorder);
                    _hashNameField.AddToClassList(ErrorBorder);
                }
                _revertButton.SetEnabled(true);
            }
            else
            {
                _hashNameField.RemoveFromClassList(ErrorBorder);
                _hashNameField.RemoveFromClassList(ChangedBorder);
                _revertButton.SetEnabled(false);
                _applyButton.SetEnabled(false);
            }
        }

        private void OnDeleteButtonClicked()
        {
            DeletionRequested?.Invoke(this);
        }
    }
}