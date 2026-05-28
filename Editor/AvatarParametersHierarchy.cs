using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Narazaka.VRChat.AvatarParametersUtil.Editor
{
    public static class AvatarParametersHierarchy
    {
        /// <summary>
        /// アバター全体の ProvidedParameter を取得し、EffectiveName を hierarchyObject 位置で見える名前へ変換して返す。
        ///
        /// NDMF の GetParametersForObject(avatarRoot) はアバター全体（ルート視点の名前）を、
        /// GetParametersForObject(hierarchyObject) は hierarchyObject のサブツリーのみ（ルート視点の名前）を返すが、
        /// ここでは「アバター全パラメーター」を「階層位置から見える名前」で返す。
        /// 例：MA Parameters で foo→bar→baz とリネームされる場合、driverGo がリネームより内側にあるなら "foo"、外側なら "baz"。
        /// </summary>
        public static IEnumerable<ProvidedParameter> GetParametersAtHierarchy(
            ParameterInfo info,
            GameObject avatarRoot,
            GameObject hierarchyObject,
            ParameterInfo.ConflictHandler onConflict = null)
        {
            if (info == null) throw new System.ArgumentNullException(nameof(info));
            if (avatarRoot == null) throw new System.ArgumentNullException(nameof(avatarRoot));
            if (hierarchyObject == null) throw new System.ArgumentNullException(nameof(hierarchyObject));

            var rootParameters = info.GetParametersForObject(avatarRoot, onConflict);
            if (ReferenceEquals(avatarRoot, hierarchyObject)) return rootParameters;

            var path = BuildPath(avatarRoot, hierarchyObject);
            if (path == null) return rootParameters;

            var nameMaps = path.Select(info.GetParameterRemappingsAt).ToList();
            var renamesPerLevel = BuildRenamesPerLevel(nameMaps);

            return rootParameters.Select(p => RemapToHierarchy(p, renamesPerLevel));
        }

        static List<GameObject> BuildPath(GameObject root, GameObject leaf)
        {
            var path = new List<GameObject>();
            var current = leaf;
            while (current != null)
            {
                path.Add(current);
                if (ReferenceEquals(current, root)) break;
                var parent = current.transform.parent;
                current = parent != null ? parent.gameObject : null;
            }
            if (path.Count == 0 || !ReferenceEquals(path[path.Count - 1], root)) return null;
            path.Reverse();
            return path;
        }

        static List<List<(ParameterNamespace ns, string inner, string outer)>> BuildRenamesPerLevel(
            List<ImmutableDictionary<(ParameterNamespace, string), ParameterMapping>> nameMaps)
        {
            var result = new List<List<(ParameterNamespace, string, string)>>(nameMaps.Count);
            result.Add(new List<(ParameterNamespace, string, string)>());

            for (var i = 1; i < nameMaps.Count; i++)
            {
                var prev = nameMaps[i - 1];
                var cur = nameMaps[i];
                var levelRenames = new List<(ParameterNamespace, string, string)>();
                foreach (var kvp in cur)
                {
                    if (prev.TryGetValue(kvp.Key, out var prevMapping) &&
                        prevMapping.ParameterName == kvp.Value.ParameterName)
                    {
                        continue;
                    }
                    var ns = kvp.Key.Item1;
                    var inner = kvp.Key.Item2;
                    var rootOuter = kvp.Value.ParameterName;
                    var localOuter = rootOuter;
                    foreach (var prevKvp in prev)
                    {
                        if (prevKvp.Key.Item1 == ns && prevKvp.Value.ParameterName == rootOuter)
                        {
                            localOuter = prevKvp.Key.Item2;
                            break;
                        }
                    }
                    levelRenames.Add((ns, inner, localOuter));
                }
                result.Add(levelRenames);
            }
            return result;
        }

        static ProvidedParameter RemapToHierarchy(
            ProvidedParameter source,
            List<List<(ParameterNamespace ns, string inner, string outer)>> renamesPerLevel)
        {
            var currentName = source.EffectiveName;
            var ns = source.Namespace;
            var changed = false;
            for (var i = 1; i < renamesPerLevel.Count; i++)
            {
                foreach (var rename in renamesPerLevel[i])
                {
                    if (rename.ns == ns && currentName == rename.outer)
                    {
                        currentName = rename.inner;
                        changed = true;
                        break;
                    }
                }
            }
            if (!changed) return source;
            var clone = source.Clone();
            clone.EffectiveName = currentName;
            return clone;
        }
    }
}
