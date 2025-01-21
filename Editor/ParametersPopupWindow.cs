using nadena.dev.ndmf;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Narazaka.VRChat.AvatarParametersUtil.Editor
{
    public class ParametersPopupWindow : PopupWindowContent
    {
        public Action<string> UpdateProperty;
        GameObject BaseObject;
        Func<ProvidedParameter, bool> FilterParameter;
        ProvidedParameter[] Parameters;
        SearchField SearchField;
        string SearchQuery;
        bool IncludeAnimators;
        ParametersTreeView TreeView;

        public ParametersPopupWindow(GameObject baseObject)
        {
            BaseObject = baseObject;
        }

        public ParametersPopupWindow(GameObject baseObject, Func<ProvidedParameter, bool> filterParameter)
        {
            BaseObject = baseObject;
            FilterParameter = filterParameter;
        }

        public override void OnGUI(Rect rect)
        {

            if (SearchField == null) SearchField = new SearchField();
            SearchQuery = SearchField.OnGUI(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), SearchQuery);

            rect.y += EditorGUIUtility.singleLineHeight;
            rect.height -= EditorGUIUtility.singleLineHeight;
            var newIncludeAnimators = EditorGUI.Toggle(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Include Animators", IncludeAnimators);
            if (newIncludeAnimators != IncludeAnimators || Parameters == null)
            {
                IncludeAnimators = newIncludeAnimators;
                Parameters = BaseObject == null ? new ProvidedParameter[0] : ParameterInfo.ForUI.GetParametersForObject(BaseObject).ToDistinctSubParameters().NotEmpty().OnlyVisible().Where(p => p.ParameterType != null).Where(FilterParameter == null ? (p) => true : FilterParameter).ToArray();
                TreeView = null;
            }
            rect.y += EditorGUIUtility.singleLineHeight;
            rect.height -= EditorGUIUtility.singleLineHeight;
            if (TreeView == null)
            {
                TreeView = new ParametersTreeView(new TreeViewState(), Parameters)
                {
                    OnSelect = (parameter) =>
                    {
                        if (UpdateProperty != null) UpdateProperty(parameter.EffectiveName);
                    },
                    OnCommit = (parameter) =>
                    {
                        if (UpdateProperty != null) UpdateProperty(parameter.EffectiveName);
                        editorWindow.Close();
                    }
                };
                TreeView.Reload();
            }
            TreeView.searchString = SearchQuery;
            TreeView.OnGUI(rect);
        }
    }
}
