﻿/*********************************
 * (c) Christopher Wang / Steamfist Innovations
 * 10/6/2016
 * Please don't steal this code or use without permission
*********************************/

using UnityEngine;
using UnityEditor;

namespace StoryBookEditor
{
    /// <summary>
    /// Editor Object for a story book UI
    /// </summary>
    [CustomEditor(typeof(StoryBook))]
    [CanEditMultipleObjects]
    public class StoryBookEditor : Editor
    {
        public static GUIContent LocationLabel = new GUIContent("Location");
        public static GUIContent SFXLabel = new GUIContent("Sound Effect");
        public static GUIContent SizeLabel = new GUIContent("Size");
        public static GUIContent SpriteLabel = new GUIContent("Image");
        public static GUIContent NextPageLabel = new GUIContent("Next Page");
        public static GUIContent NewPageLabel = new GUIContent("New Page");
        public static GUIContent BranchesLabel = new GUIContent("Branches");
        public static GUIContent SettingsLabel = new GUIContent("Settings");
        public static GUIContent TransitionLengthLabel = new GUIContent("Length of Transition");
        public static GUIContent CurrentImageLabel = new GUIContent("Slide Start Image");
        public static GUIContent NextImageLabel = new GUIContent("Slide End Image");
        public static GUILayoutOption ButtonWidth = GUILayout.Width(66);

        protected SerializedProperty PageName;
        protected SerializedProperty PagesBG;
        protected SerializedProperty PagesCanBack;
        protected SerializedProperty Branches;
        protected SerializedProperty BackgroundClip;

        protected Sprite _nextImg;
        protected Sprite _transCurrentImg;
        protected Sprite _transNextImg;
        protected int _transLength = 1000;
        protected TransitionTypes _transType = TransitionTypes.None;
        protected AudioClip _nextSfx;
        protected Vector2 _location = Vector2.zero;
        protected Vector2 _size = new Vector2(1, 1);
        protected string _nextName = string.Empty;
        protected bool _showNewPageContent = false;
        protected bool _showBranches = false;
        protected bool _showSettings = false;

        /// <summary>
        /// Invoked on enabled
        /// </summary>
        void OnEnable()
        {
            PagesBG = serializedObject.FindProperty(StoryBook.PAGE_IMAGE_PROPERTY);
            PageName = serializedObject.FindProperty(StoryBook.PAGE_NAME_PROPERTY);
            Branches = serializedObject.FindProperty(StoryBook.BRANCHES_PROPERTY);
            PagesCanBack = serializedObject.FindProperty(StoryBook.PAGE_CAN_BACK_PROPERTY);
            BackgroundClip = serializedObject.FindProperty(StoryBook.BACKGROUND_CLIP_PROPERTY);
        }
        /// <summary>
        /// Casting property for serialized object as Book object
        /// </summary>
        public StoryBook TargetBook
        {
            get
            {
                return serializedObject.targetObject as StoryBook;
            }
        }
        /// <summary>
        /// Generates UI for creating book
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            #region Back Button
            EditorGUILayout.PropertyField(PageName);
            EditorGUILayout.PropertyField(PagesBG);
            EditorGUILayout.PropertyField(BackgroundClip);
            if (PagesCanBack.boolValue)
            {
                if (GUILayout.Button("Back"))
                {
                    TargetBook.LoadBack();
                }
            }
            #endregion

            #region Current Adjacent Pages
            if (Branches.arraySize > 0)
            {
                EditorGUILayout.Separator();
                _showBranches = EditorGUILayout.Foldout(_showBranches, BranchesLabel);
                if (_showBranches)
                {
                    EditorGUI.indentLevel += 1;

                    for (int i = 0; i < Branches.arraySize; i++)
                    {
                        var property = Branches.GetArrayElementAtIndex(i);
                        var nameProp = property.FindPropertyRelative("NextPageName");
                        EditorGUILayout.LabelField("Branch:" + nameProp.stringValue);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("ItemLocation"), LocationLabel);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("ItemSize"), SizeLabel);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("ImageSprite"), SpriteLabel);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("NextPageName"), NextPageLabel);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("SFXClip"), SFXLabel);
                        property.FindPropertyRelative("TransitionType").intValue = (int)(TransitionTypes)EditorGUILayout.EnumPopup("Transition Type", (TransitionTypes)property.FindPropertyRelative("TransitionType").intValue);
                        
                        if((TransitionTypes)property.FindPropertyRelative("TransitionType").intValue == TransitionTypes.Slide)
                        {
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("TransitionLength"), TransitionLengthLabel);
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("CurrentImageSprite"), CurrentImageLabel);
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("NextImageSprite"), NextImageLabel);
                        }
                        else if((TransitionTypes)property.FindPropertyRelative("TransitionType").intValue == TransitionTypes.Fade)
                        {
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("TransitionLength"), TransitionLengthLabel);
                        }
                        else
                        {
                            property.FindPropertyRelative("TransitionLength").intValue = 1000;
                            property.FindPropertyRelative("CurrentImageSprite").objectReferenceValue = null;
                            property.FindPropertyRelative("NextImageSprite").objectReferenceValue = null;
                        }
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Go To", EditorStyles.miniButtonLeft, ButtonWidth))
                        {
                            TargetBook.LoadPage(TargetBook.Branches[i].NextPageId, null);
                        }
                        if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, ButtonWidth))
                        {
                            TargetBook.DeleteBranch(TargetBook.Branches[i].Id);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            #endregion

            #region New Page Area
            EditorGUILayout.Separator();
            _showNewPageContent = EditorGUILayout.Foldout(_showNewPageContent, NewPageLabel);
            if (_showNewPageContent)
            {
                _nextName = EditorGUILayout.TextField("New Page Name", _nextName);
                _location = EditorGUILayout.Vector2Field("Item Location", _location);
                _size = EditorGUILayout.Vector2Field("Item Size", _size);
                _nextSfx = EditorGUILayout.ObjectField(new GUIContent("SFX"), _nextSfx, typeof(AudioClip), true) as AudioClip;
                _nextImg = EditorGUILayout.ObjectField(new GUIContent("Item Image"), _nextImg, typeof(Sprite), true) as Sprite;
                _nextImg = EditorGUILayout.ObjectField(new GUIContent("Item Image"), _nextImg, typeof(Sprite), true) as Sprite;

                _transType = (TransitionTypes)EditorGUILayout.EnumPopup(new GUIContent("Transition Type"), _transType);
                _transLength = EditorGUILayout.IntField(new GUIContent("Transition Length"), _transLength);
                if (_transType == TransitionTypes.Slide)
                {
                    _transCurrentImg = EditorGUILayout.ObjectField(new GUIContent("Slide Start Image"), _nextImg, typeof(Sprite), true) as Sprite;
                    _transNextImg = EditorGUILayout.ObjectField(new GUIContent("Slide End Image"), _nextImg, typeof(Sprite), true) as Sprite;
                }

                if (GUILayout.Button("Create"))
                {
                    TargetBook.AddBranchToPage(_location, _size, _nextImg, _nextSfx, _transType, _transLength, _transCurrentImg, _transNextImg, _nextName);
                    _location = Vector2.zero;
                    _size = new Vector2(1, 1);
                    _nextImg = null;
                    _nextSfx = null;
                    _nextName = string.Empty;
                    _transType = TransitionTypes.None;
                    _transLength = 1000;
                    _transCurrentImg = null;
                    _transNextImg = null;
                }
            }
            EditorGUILayout.Separator();
            #endregion

            if (serializedObject.ApplyModifiedProperties() ||
                 (Event.current.type == EventType.ExecuteCommand &&
                 Event.current.commandName == "UndoRedoPerformed"))
            {
                TargetBook.BookUpdated();
            }
        }
    }
}
