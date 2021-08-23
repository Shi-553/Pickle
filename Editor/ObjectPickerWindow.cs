﻿using Pickle.ObjectProviders;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pickle.Editor
{
    public class ObjectPickerWindow : EditorWindow
    {
        private const string SEARCH_FIELD_CONTROL_NAME = nameof(ObjectPickerWindow) + "_SearchField";

        private static readonly Vector2 DEFAULT_SIZE = new Vector2(700f, 560f);
        private IObjectProvider _lookupStrategy;
        private Action<UnityEngine.Object> _onPickCallback;
        private Predicate<ObjectTypePair> _filter;

        private int _selectedOptionIndex;
        private string _searchString;
        private Vector2 _scrollPosition;
        private List<ObjectTypePair> _options = new List<ObjectTypePair>();
        private List<int> _visibleOptionIndices = new List<int>();

        public static void OpenCustomPicker(string title, Action<UnityEngine.Object> onPick, IObjectProvider lookupStrategy, Predicate<ObjectTypePair> filter, UnityEngine.Object selectedObject = null)
        {
            var picker = GetWindow<ObjectPickerWindow>(true, title);

            picker._lookupStrategy = lookupStrategy;
            picker._onPickCallback = onPick;
            picker._filter = filter;
            picker.RefreshList();

            picker._selectedOptionIndex = picker._options.FindIndex((option) => option.Object == selectedObject);

        }

        private void OnEnable()
        {
            _searchString = null;
            _selectedOptionIndex = -1;
            _scrollPosition = new Vector2(0f, 0f);

            var pos = position;
            pos.size = DEFAULT_SIZE;
            position = pos;
        }

        private void RefreshList()
        {
            _options.Clear();

            var lookupEnumeration = _lookupStrategy.Lookup();
            while (lookupEnumeration.MoveNext())
            {
                var objectTypePair = lookupEnumeration.Current;
                var obj = objectTypePair.Object;

                if ((_filter?.Invoke(objectTypePair)).GetValueOrDefault(true) && (obj.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    _options.Add(objectTypePair);
                }
            }

            RefreshVisibleOptions();

            Repaint();
        }

        private void RefreshVisibleOptions()
        {
            _visibleOptionIndices.Clear();
            var hasSearchString = !string.IsNullOrEmpty(_searchString);

            _visibleOptionIndices.Add(-1);
            for (int i = 0; i < _options.Count; i++)
            {
                UnityEngine.Object obj = _options[i].Object;
                if (!hasSearchString || obj.name.ToLowerInvariant().Contains(_searchString.ToLowerInvariant()))
                {
                    _visibleOptionIndices.Add(i);
                }
            }
        }

        private void OnGUI()
        {
            // search bar
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(SEARCH_FIELD_CONTROL_NAME);
            _searchString = EditorGUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            GUI.FocusControl(SEARCH_FIELD_CONTROL_NAME);

            if (EditorGUI.EndChangeCheck())
            {
                RefreshVisibleOptions();
            }

            EditorGUILayout.Space();

            // display
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _visibleOptionIndices.Count; i++)
            {
                DrawOptionSelectionLabel(_visibleOptionIndices[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawOptionSelectionLabel(int optionIndex)
        {
            var obj = optionIndex >= 0 ? _options[optionIndex].Object : null;

            string name = optionIndex >= 0 ? _options[optionIndex].Object.name : "None";
            string tag = optionIndex >= 0 ? _options[optionIndex].Type.ToString() : "";

            var previewTexture = obj == null ? null : AssetPreview.GetMiniThumbnail(obj);

            if (DrawSelectableLabel(name, tag, _selectedOptionIndex == optionIndex, previewTexture))
            {
                if (_selectedOptionIndex == optionIndex)
                {
                    _onPickCallback?.Invoke(_selectedOptionIndex >= 0 ? _options[_selectedOptionIndex].Object : null);
                    Close();
                    return;
                }

                _selectedOptionIndex = optionIndex;
            }
        }

        private bool DrawSelectableLabel(string text, string tag, bool isSelected, Texture2D icon = null)
        {
            bool result = false;
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));

            if (GUI.Button(r, "", GUIStyle.none))
            {
                result = true;
            }

            if (isSelected)
            {
                EditorGUI.DrawRect(r, new Color32(44, 93, 135, 255));
            }

            float indentation = r.height;
            r.xMin += indentation;

            float iconWidth = r.height;
            if (icon)
            {
                GUI.DrawTexture(new Rect(r.min, new Vector2(r.height, r.height)), icon);
            }

            r.xMin += iconWidth;
            r.width -= iconWidth;

            float columnWidth = r.width / 2f;

            var columnRect = r; r.width = columnWidth;
            EditorGUI.LabelField(columnRect, text, EditorStyles.whiteLabel);

            columnRect.x += columnWidth;
            EditorGUI.LabelField(columnRect, tag, EditorStyles.whiteLabel);

            return result;
        }
    }
}
