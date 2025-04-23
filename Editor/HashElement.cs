using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static LazyRedpaw.StaticHashes.Constants;
using static LazyRedpaw.StaticHashes.StaticHashesEditorHelper;

namespace LazyRedpaw.StaticHashes
{
    public class HashElement
    {
        private readonly VisualElement _root;
        private readonly TextField _hashNameField;
        private readonly Button _revertButton;
        private readonly Button _deleteButton;
        private readonly VisualElement _existingNameError;
        
        private string _savedHashName;
        private int _value;
        private int _savedValue;
        private bool _isChanged;

        public string HashName => _hashNameField.value;
        public string SavedHashName => _savedHashName;
        public int Value => _value;
        public int SavedValue => _savedValue;
        public VisualElement Root => _root;
        public bool IsChanged
        {
            get => _isChanged;
            private set
            {
                if(_isChanged == value) return;
                _isChanged = value;
                ChangeDone?.Invoke(_isChanged);
            }
        }
        public bool IsNameChanged => _value != _savedValue;

        public event Action<HashElement> DeletionRequested;
        public event Action<ChangeEvent<string>, TextField, int, VisualElement, Regex, int> HashNameFieldChanged;
        public event Action<bool> ChangeDone;
        
        public HashElement(int value, VisualElement root, string hashName)
        {
            _value = value;
            _savedValue = value;
            _root = root;
            _savedHashName = hashName;
            _root.name = hashName;
            _hashNameField = _root.Q<TextField>(HashNameField);
            _hashNameField.value = hashName;
            _revertButton = _root.Q<Button>(RevertButton);
            _deleteButton = _root.Q<Button>(DeleteButton);
            _existingNameError = _root.Q<VisualElement>(NameExistingErrorText);
            _existingNameError.style.display = DisplayStyle.None;
            _revertButton.SetEnabled(false);

            _hashNameField.RegisterValueChangedCallback(OnHashNameFieldChanged);
            _deleteButton.clicked += OnDeleteButtonClicked;
            _revertButton.clicked += OnRevertButtonClicked;
        }

        public string GetHashAsScriptLine() => $"\t\tpublic static readonly int {_hashNameField.value} = {_value};";

        public void Revert()
        {
            _hashNameField.value = _savedHashName;
        }

        public void Change(string newName)
        {
            int newHash = GetNameHash(newName);
            _hashNameField.value = newName;
            _value = newHash;
        }
        
        private void OnRevertButtonClicked()
        {
            Revert();
        }
        
        private void OnHashNameFieldChanged(ChangeEvent<string> evt)
        {
            HashNameFieldChanged?.Invoke(evt, _hashNameField, _savedValue, _existingNameError, NameRegex, _savedValue);
            int newHash = GetNameHash(evt.newValue);
            if (newHash != _savedValue)
            {
                _revertButton.SetEnabled(true);
                IsChanged = true;
            }
            else
            {
                _revertButton.SetEnabled(false);
                IsChanged = false;
            }
            _value = newHash;
        }

        private void OnDeleteButtonClicked()
        {
            DeletionRequested?.Invoke(this);
        }
    }
}