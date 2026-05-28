using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using nadena.dev.ndmf;
using System;

namespace Narazaka.VRChat.AvatarParametersUtil.Editor
{
    public class AvatarParametersUtilEditor
    {
        static Dictionary<SerializedObject, AvatarParametersUtilEditor> Cache = new Dictionary<SerializedObject, AvatarParametersUtilEditor>();
        static Dictionary<SerializedObject, AvatarParametersUtilEditor> HierarchalCache = new Dictionary<SerializedObject, AvatarParametersUtilEditor>();

        public static AvatarParametersUtilEditor GetForHierarchy(SerializedObject serializedObject, bool forceUpdate = false)
        {
            if (HierarchalCache.TryGetValue(serializedObject, out var parameterUtil) && parameterUtil != null)
            {
                if (forceUpdate)
                {
                    parameterUtil.UpdateParametersCache();
                }
            }
            else
            {
                parameterUtil = new AvatarParametersUtilEditor(serializedObject, true);
                HierarchalCache.Add(serializedObject, parameterUtil);
            }
            return parameterUtil;
        }

        [Obsolete("Use GetForAvatarRoot (or update to GetForHierarchy) instead.")]
        public static AvatarParametersUtilEditor Get(SerializedObject serializedObject, bool forceUpdate = false) => GetForAvatarRoot(serializedObject, forceUpdate);

        public static AvatarParametersUtilEditor GetForAvatarRoot(SerializedObject serializedObject, bool forceUpdate = false)
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
                parameterUtil = new AvatarParametersUtilEditor(serializedObject, false);
                Cache.Add(serializedObject, parameterUtil);
            }
            return parameterUtil;
        }

        public bool ForHierarchy;
        public SerializedObject SerializedObject;
        ProvidedParameter[] ParametersCache;
        Dictionary<string, int> ParameterNameToIndexCache = new Dictionary<string, int>();

        [Obsolete("Use AvatarParametersUtilEditor(SerializedObject, bool) instead.")]
        public AvatarParametersUtilEditor(SerializedObject serializedObject) : this(serializedObject, false) { }

        /// <param name="serializedObject">target</param>
        /// <param name="forHierarchy">if true, the editor will operate on the hierarchy level instead of the avatar root.</param>
        public AvatarParametersUtilEditor(SerializedObject serializedObject, bool forHierarchy)
        {
            ForHierarchy = forHierarchy;
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
                var avatarRoot = GetParentAvatar();
                var hierarchyObject = ForHierarchy ? (SerializedObject.targetObject as Component)?.gameObject : null;
                PopupWindow.Show(rect, new ParametersPopupWindow(avatarRoot, hierarchyObject, filterParameter)
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
            if (avatar == null)
            {
                ParametersCache = new ProvidedParameter[0];
            }
            else if (ForHierarchy)
            {
                var hierarchyObject = (SerializedObject.targetObject as Component)?.gameObject;
                ParametersCache = hierarchyObject == null
                    ? new ProvidedParameter[0]
                    : AvatarParametersHierarchy.GetParametersAtHierarchy(ParameterInfo.ForUI, avatar, hierarchyObject).ToDistinctSubParameters().NotEmpty().OnlyVisible().ToArray();
            }
            else
            {
                ParametersCache = ParameterInfo.ForUI.GetParametersForObject(avatar).ToDistinctSubParameters().NotEmpty().OnlyVisible().ToArray();
            }
            ParameterNameToIndexCache = ParametersCache.Select((p, index) => new { p.EffectiveName, index }).ToDictionary(p => p.EffectiveName, p => p.index);
        }

        GameObject GetParentAvatar()
        {
            return (SerializedObject.targetObject as Component)?.GetComponentInParent<VRCAvatarDescriptor>()?.gameObject;
        }
    }
}
