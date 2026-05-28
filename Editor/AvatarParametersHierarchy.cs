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
            return RemapToHierarchyFromNameMaps(rootParameters, nameMaps);
        }

        /// <summary>
        /// テスト・差し替え用: あらかじめ用意した path 上の nameMap 列を直接受け取って remap を行う純粋関数。
        /// nameMaps[0] が avatarRoot、nameMaps[n-1] が hierarchyObject に対応する。
        /// </summary>
        internal static IEnumerable<ProvidedParameter> RemapToHierarchyFromNameMaps(
            IEnumerable<ProvidedParameter> rootParameters,
            IList<ImmutableDictionary<(ParameterNamespace, string), ParameterMapping>> nameMapsAlongPath)
        {
            if (nameMapsAlongPath == null || nameMapsAlongPath.Count <= 1)
            {
                return rootParameters;
            }
            var renamesPerLevel = BuildRenamesPerLevel(nameMapsAlongPath);
            return rootParameters.SelectMany(p => RemapToHierarchy(p, renamesPerLevel));
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

        internal static List<List<(ParameterNamespace ns, string inner, string outer)>> BuildRenamesPerLevel(
            IList<ImmutableDictionary<(ParameterNamespace, string), ParameterMapping>> nameMaps)
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

        // 1つの root-level パラメーターから、hierarchyObject 視点でアクセス可能な「全ての」名前を列挙する。
        // MA Parameters の rename は片方向 lookup (inner→outer の一回だけ) なので、リネーム chain 上の
        // 全ての名前（inner名、outer名、chain中間名）が等しく同じパラメーターを駆動できる。
        // shadowing (この hierarchy 名を書くと別パラメーターに化ける) を round-trip で除外。
        static IEnumerable<ProvidedParameter> RemapToHierarchy(
            ProvidedParameter source,
            List<List<(ParameterNamespace ns, string inner, string outer)>> renamesPerLevel)
        {
            var ns = source.Namespace;
            var rootName = source.EffectiveName;

            // 集合展開: ルートからdriverGoに向かって下りつつ、その時点までの集合に含まれる outer に対応する
            // inner を新規候補として追加していく。これがアクセス可能な hierarchy 名候補の上界。
            var candidates = new HashSet<string> { rootName };
            for (var i = 1; i < renamesPerLevel.Count; i++)
            {
                var snapshot = new List<string>(candidates);
                foreach (var rename in renamesPerLevel[i])
                {
                    if (rename.ns != ns) continue;
                    if (snapshot.Contains(rename.outer))
                    {
                        candidates.Add(rename.inner);
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                if (!RoundTripsToRoot(candidate, rootName, ns, renamesPerLevel)) continue;

                if (candidate == rootName)
                {
                    yield return source;
                }
                else
                {
                    var clone = source.Clone();
                    clone.EffectiveName = candidate;
                    yield return clone;
                }
            }
        }

        // 階層名を driverGo→root 方向に MA Parameters 適用相当のwalkで戻したとき、元のルート名に一致するか。
        // 一致 = この hierarchy 名で実際にそのパラメーターを駆動できる
        // 不一致 = shadowing で別パラメーターに化けるためアクセス不可
        static bool RoundTripsToRoot(
            string hierarchyName,
            string rootName,
            ParameterNamespace ns,
            List<List<(ParameterNamespace ns, string inner, string outer)>> renamesPerLevel)
        {
            var forwardName = hierarchyName;
            for (var k = renamesPerLevel.Count - 1; k >= 1; k--)
            {
                foreach (var rename in renamesPerLevel[k])
                {
                    if (rename.ns == ns && forwardName == rename.inner)
                    {
                        forwardName = rename.outer;
                        break;
                    }
                }
            }
            return forwardName == rootName;
        }
    }
}
