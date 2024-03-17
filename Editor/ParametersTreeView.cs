using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Narazaka.VRChat.AvatarParametersUtil.Editor
{
    class ParametersTreeView : TreeView
    {
        class Item : TreeViewItem
        {
            public ProvidedParameter source;
        }

        public Action<ProvidedParameter> OnSelect;
        public Action<ProvidedParameter> OnCommit;

        ProvidedParameter[] Parameters;

        public ParametersTreeView(TreeViewState state, ProvidedParameter[] parameters) : base(state)
        {
            Parameters = parameters;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
            SetupParentsAndChildrenFromDepths(root, Parameters.Select((p, index) => new Item() { id = index, depth = 0, displayName = p.EffectiveName, source = p } as TreeViewItem).ToList());
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is Item source)
            {
                var rect = args.rowRect;
                rect.xMin += GetContentIndent(args.item) + extraSpaceBeforeIconAndLabel;
                EditorGUI.LabelField(rect, source.source.EffectiveName);
            }
            else
            {
                base.RowGUI(args);
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var item = Parameters[selectedIds[0]];
            if (item != null && OnSelect != null) OnSelect(item);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = Parameters[id];
            if (item != null && OnCommit != null) OnCommit(item);
        }
    }
}
