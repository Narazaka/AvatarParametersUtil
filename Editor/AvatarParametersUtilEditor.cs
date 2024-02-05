using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

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
        VRCExpressionParameters.Parameter[] ParametersCache;
        Dictionary<string, int> ParameterNameToIndexCache = new Dictionary<string, int>();

        public AvatarParametersUtilEditor(SerializedObject serializedObject)
        {
            SerializedObject = serializedObject;
            UpdateParametersCache();
        }

        public void ShowParameterNameField(Rect rect, SerializedProperty property, GUIContent label = null)
        {
            rect.width -= EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, property, label);
            rect.x += rect.width;
            rect.width = EditorGUIUtility.singleLineHeight;
            GUIStyle style = "IN DropDown";
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard, style))
            {
                PopupWindow.Show(rect, new ParametersPopupWindow(GetParentAvatar())
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
            if (parameter?.valueType == VRCExpressionParameters.ValueType.Bool)
            {
                var result = EditorGUI.Toggle(rect, label, property.floatValue >= 0.5f);
                property.floatValue = result ? 1f : 0f;
            }
            else if (parameter?.valueType == VRCExpressionParameters.ValueType.Int)
            {
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
            EditorGUI.LabelField(rect, parameter == null ? "?" : parameter.valueType.ToString(), EditorStyles.centeredGreyMiniLabel);
        }

        public VRCExpressionParameters.Parameter GetParameter(string name)
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
            ParametersCache = AvatarParametersUtil.GetParameters(avatar, true).ToArray();
            ParameterNameToIndexCache = ParametersCache.Select((p, index) => new { p.name, index }).ToDictionary(p => p.name, p => p.index);
        }

        VRCAvatarDescriptor GetParentAvatar()
        {
            return (SerializedObject.targetObject as Component)?.GetComponentInParent<VRCAvatarDescriptor>();
        }
    }
}
