﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using nadena.dev.ndmf;

namespace Narazaka.VRChat.AvatarParametersUtil.Editor
{
    public class AvatarParametersUtilEditor
    {
        static Dictionary<SerializedObject, AvatarParametersUtilEditor> Cache = new Dictionary<SerializedObject, AvatarParametersUtilEditor>();

        public static AvatarParametersUtilEditor Get(SerializedObject serializedObject, bool forceUpdate = false)
        {
            if (Cache.TryGetValue(serializedObject, out var parameterUtil) && parameterUtil != null)
            {
                if (forceUpdate)
                {
                    parameterUtil.UpdateParametersCache();
                }
            }
            else
            {
                parameterUtil = new AvatarParametersUtilEditor(serializedObject);
                Cache.Add(serializedObject, parameterUtil);
            }
            return parameterUtil;
        }

        public SerializedObject SerializedObject;
        ProvidedParameter[] ParametersCache;
        Dictionary<string, int> ParameterNameToIndexCache = new Dictionary<string, int>();

        public AvatarParametersUtilEditor(SerializedObject serializedObject)
        {
            SerializedObject = serializedObject;
            UpdateParametersCache();
        }

        public void ShowParameterNameField(Rect rect, SerializedProperty property, GUIContent label = null) => ShowParameterNameField(rect, property, null, label);

        public void ShowParameterNameField(Rect rect, SerializedProperty property, System.Func<ProvidedParameter, bool> filterParameter, GUIContent label = null)
        {
            rect.width -= EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, property, label);
            rect.x += rect.width;
            rect.width = EditorGUIUtility.singleLineHeight;
            GUIStyle style = "IN DropDown";
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard, style))
            {
                PopupWindow.Show(rect, new ParametersPopupWindow(GetParentAvatar(), filterParameter)
                {
                    UpdateProperty = (name) =>
                    {
                        property.stringValue = name;
                        SerializedObject.ApplyModifiedProperties();
                        UpdateParametersCache();
                    }
                });
            }
            rect.x -= 30;
            rect.width = 30;
            ShowParameterTypeField(rect, property.stringValue);
        }

        public void ShowParameterValueField(Rect rect, string parameterName, SerializedProperty property, GUIContent label = null)
        {
            var parameter = GetParameter(parameterName);
            if (parameter?.ParameterType == AnimatorControllerParameterType.Bool)
            {
                if (label == null) label = new GUIContent(property.displayName);
                var result = EditorGUI.Toggle(rect, label, property.floatValue >= 0.5f);
                property.floatValue = result ? 1f : 0f;
            }
            else if (parameter?.ParameterType == AnimatorControllerParameterType.Int)
            {
                if (label == null) label = new GUIContent(property.displayName);
                var result = EditorGUI.IntField(rect, label, Mathf.RoundToInt(property.floatValue));
                property.floatValue = result;
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label);
            }
        }

        void ShowParameterTypeField(Rect rect, string parameterName)
        {
            var parameter = GetParameter(parameterName);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.IndentedRect(rect);
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, parameter == null ? "?" : parameter.ParameterType.ToString(), EditorStyles.centeredGreyMiniLabel);
            EditorGUI.indentLevel = indentLevel;
        }

        public ProvidedParameter GetParameter(string name)
        {
            if (ParameterNameToIndexCache.TryGetValue(name, out var index))
            {
                return ParametersCache[index];
            }
            return null;
        }

        void UpdateParametersCache()
        {
            var avatar = GetParentAvatar();
            ParametersCache = avatar == null ? new ProvidedParameter[0] : ParameterInfo.ForUI.GetParametersForObject(avatar).ToDistinctSubParameters().NotEmpty().OnlyVisible().ToArray();
            ParameterNameToIndexCache = ParametersCache.Select((p, index) => new { p.EffectiveName, index }).ToDictionary(p => p.EffectiveName, p => p.index);
        }

        GameObject GetParentAvatar()
        {
            return (SerializedObject.targetObject as Component)?.GetComponentInParent<VRCAvatarDescriptor>()?.gameObject;
        }
    }
}
