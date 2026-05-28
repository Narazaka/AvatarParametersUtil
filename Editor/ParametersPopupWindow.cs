using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
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
        GameObject HierarchyObject;
        Func<ProvidedParameter, bool> FilterParameter;
        ProvidedParameter[] Parameters;
        SearchField SearchField;
        string SearchQuery;
        bool IncludeAnimators;
        ParametersTreeView TreeView;

        [Obsolete]
        public ParametersPopupWindow(GameObject baseObject)
        {
            BaseObject = baseObject;
        }

        [Obsolete]
        public ParametersPopupWindow(GameObject baseObject, Func<ProvidedParameter, bool> filterParameter)
        {
            BaseObject = baseObject;
            FilterParameter = filterParameter;
        }

        /// <param name="avatarRoot">avatar root GameObject (VRCAvatarDescriptor host).</param>
        /// <param name="hierarchyObject">hierarchyObject: when non-null, parameter names are remapped to be visible at this object's hierarchy level (MA Parameters renames applied above this object are reversed). Null means use avatar-root-level names.</param>
        /// <param name="filterParameter">optional filter applied after parameter list construction.</param>
        public ParametersPopupWindow(GameObject avatarRoot, GameObject hierarchyObject, Func<ProvidedParameter, bool> filterParameter = null)
        {
            BaseObject = avatarRoot;
            HierarchyObject = hierarchyObject;
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
                Parameters = FetchParameters();
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

        ProvidedParameter[] FetchParameters()
        {
            if (BaseObject == null) return new ProvidedParameter[0];
            IEnumerable<ProvidedParameter> source;
            if (HierarchyObject != null && !ReferenceEquals(HierarchyObject, BaseObject))
            {
                source = AvatarParametersHierarchy.GetParametersAtHierarchy(ParameterInfo.ForUI, BaseObject, HierarchyObject);
            }
            else
            {
                source = ParameterInfo.ForUI.GetParametersForObject(BaseObject);
            }
            return source
                .ToDistinctSubParameters()
                .NotEmpty()
                .OnlyVisible()
                .Where(p => p.ParameterType != null)
                .Where(FilterParameter == null ? (p) => true : FilterParameter)
                .ToArray();
        }
    }
}
