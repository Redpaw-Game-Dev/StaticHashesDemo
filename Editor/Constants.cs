using System.Reflection;
using System.Text.RegularExpressions;

namespace LazyRedpaw.StaticHashes
{
    public static class Constants
    {
        public static readonly Regex NameRegex = new(@"^[A-Za-z][A-Za-z0-9]*$");
        public static readonly Regex StaticCallRegex = new(@"\b\w+\.\w+\b");
        
        public const string StaticHashesWindowTitle = "Static Hashes";
        public const string NewCategoryNameField = "NewCategoryNameField";
        public const string CreateCategoryButton = "CreateCategoryButton";
        public const string NewHashNameField = "NewHashNameField";
        public const string CreateHashButton = "CreateHashButton";
        public const string CategoriesFoldout = "CategoriesFoldout";
        public const string CategoriesList = "CategoriesList";
        public const string CategoriesCount = "CategoriesCount";
        public const string HashCategory = "HashCategory";
        public const string HashList = "HashList";
        public const string DeleteCategoryButton = "DeleteCategoryButton";
        public const string ChangeCategoryNameField = "ChangeCategoryNameField";
        public const string ChangeCategoryNameButton = "ChangeCategoryNameButton";
        public const string NameExistingErrorText = "NameExistingErrorText";
        public const string CategoryNameExistingErrorTextUxml = "CategoryNameExistingErrorTextUXML";
        public const string NewHashNameExistingErrorTextUxml = "NewHashNameExistingErrorTextUXML";
        public const string HashListItem = "HashListItem";
        public const string HashNameField = "HashNameField";
        public const string ApplyButton = "ApplyButton";
        public const string RevertButton = "RevertButton";
        public const string DeleteButton = "DeleteButton";
        public const string ExpandButton = "ExpandButton";
        public const string ExpandHashesButton = "ExpandHashesButton";
        public const string ExpandCategoryButton = "ExpandCategoryButton";
        public const string HashListItemsCount = "HashListItemsCount";
        public const string CategoryContent = "CategoryContent";
        public const string CategoryRoot = "CategoryRoot";
        public const string UnsavedChangesPanel = "UnsavedChangesPanel";
        public const string SaveAllButton = "SaveAllButton";
        public const string RevertAllButton = "RevertAllButton";
        public const string TmpSaveButton = "TmpSaveButton";
        public const string CategoriesName = "Categories";
        
        public const BindingFlags FieldsBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        public const string SaveFolderPrefKey = "LP_StaticHashesSaveFolderPath";
        public const string HashesFileFullName = "StaticHashesStorage.cs";
        public const string CategoriesFileFullName = "StaticHashesCategory.cs";
        
        public const string ErrorBorder = "error-border";
        public const string ChangedBorder = "changed-border";
        public const string ExpandButtonExpanded = "expand-button-expanded";
        
        public const int InsertCategoryIndex = 3;

        public static readonly string[] CategoryTemplate = new string[]
        {
            $"\tpublic abstract partial class {NamePlaceholder} : StaticHashCategory //",
            "\t{",
            "\t}"
        };
        public static readonly string[] StorageTemplate = new string[]
        {
            "//This file was generated automatically. Do not change it manually!",
            "namespace LazyRedpaw.StaticHashes",
            "{",
            "}"
        };
        public const string StaticHashesStorageFilePath = "Assets/Script/LazyRedpaw/StaticHashes/StaticHashesStorage.cs";
        public const string NamePlaceholder = "NAME_PLACEHOLDER";
    }
}