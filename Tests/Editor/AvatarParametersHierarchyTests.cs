using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf;
using Narazaka.VRChat.AvatarParametersUtil.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Narazaka.VRChat.AvatarParametersUtil.Tests.Editor
{
    public class AvatarParametersHierarchyTests
    {
        // ===== ヘルパー =====

        // MAParametersIntrospection.RemapParameters と同じ chain 解決ロジックで nameMap を更新する。
        // 各 rename は宣言順に適用、target が既存 nameMap にある場合のみ chain 解決される。
        static ImmutableDictionary<(ParameterNamespace, string), ParameterMapping> ApplyRenames(
            ImmutableDictionary<(ParameterNamespace, string), ParameterMapping> prev,
            params (ParameterNamespace ns, string inner, string outer)[] renames)
        {
            var result = prev;
            foreach (var (ns, inner, outer) in renames)
            {
                var target = outer;
                if (result.TryGetValue((ns, target), out var existing))
                {
                    target = existing.ParameterName;
                }
                result = result.SetItem((ns, inner), new ParameterMapping(target));
            }
            return result;
        }

        static ImmutableDictionary<(ParameterNamespace, string), ParameterMapping> Empty =>
            ImmutableDictionary<(ParameterNamespace, string), ParameterMapping>.Empty;

        // テスト入力用 ProvidedParameter を作成。effectiveName 省略時は originalName と同じ。
        static ProvidedParameter Param(string originalName, string effectiveName = null,
            ParameterNamespace ns = ParameterNamespace.Animator,
            AnimatorControllerParameterType type = AnimatorControllerParameterType.Float)
        {
            var p = new ProvidedParameter(originalName, ns, null, null, type);
            if (effectiveName != null) p.EffectiveName = effectiveName;
            return p;
        }

        static string[] EffectiveNames(IEnumerable<ProvidedParameter> ps) =>
            ps.Select(p => p.EffectiveName).ToArray();

        // ===== End-to-end behavior tests =====

        [Test]
        public void Empty_path_returns_input_unchanged()
        {
            var rootParams = new[] { Param("foo"), Param("bar") };
            var nameMaps = new[] { Empty }; // path 長さ 1

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar" }, EffectiveNames(result));
        }

        [Test]
        public void No_renames_on_path_returns_input_unchanged()
        {
            var nameMaps = new[] { Empty, Empty };
            var rootParams = new[] { Param("qux") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "qux" }, EffectiveNames(result));
        }

        [Test]
        public void Single_rename_param_in_scope_both_inner_and_outer_accessible()
        {
            // path: [root, X, driverGo]、X で foo→bar
            // A は X のサブツリーで "foo" 宣言 → ルート "bar"、driverGo 視点で両方アクセス可能
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var L2 = L1;
            var nameMaps = new[] { L0, L1, L2 };
            var rootParams = new[] { Param("foo", effectiveName: "bar") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar" }, EffectiveNames(result));
            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.All(p => p.OriginalName == "foo"));
        }

        [Test]
        public void Parameter_unaffected_by_rename_only_one_name_accessible()
        {
            // X has foo→bar、C は無関係に "qux" 宣言 → driverGo 視点でも "qux" のみ
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var L2 = L1;
            var nameMaps = new[] { L0, L1, L2 };
            var rootParams = new[] { Param("qux") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "qux" }, EffectiveNames(result));
        }

        [Test]
        public void Shadowing_filters_out_inaccessible_parameter()
        {
            // X has foo→bar、B は X サブツリー外で "foo" 宣言 → ルート "foo"
            // driverGo で "foo" を書くと X で "bar" にリネームされる → B 到達不能で除外
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var L2 = L1;
            var nameMaps = new[] { L0, L1, L2 };
            var rootParams = new[] { Param("foo") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            Assert.AreEqual(0, result.Length, "shadowed parameter is filtered out");
        }

        [Test]
        public void Shadowing_and_non_shadowing_coexist()
        {
            // A: X 内 "foo" → ルート "bar" (両名アクセス可能)
            // B: X 外 "foo" → ルート "foo" (shadowed)
            // C: 無関係 "qux"
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var L2 = L1;
            var nameMaps = new[] { L0, L1, L2 };
            var rootParams = new[]
            {
                Param("foo", effectiveName: "bar"), // A
                Param("foo"),                       // B (shadowed)
                Param("qux"),                       // C
            };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar" },
                result.Where(p => p.OriginalName == "foo").Select(p => p.EffectiveName).ToArray());
            CollectionAssert.AreEquivalent(new[] { "qux" },
                result.Where(p => p.OriginalName == "qux").Select(p => p.EffectiveName).ToArray());
            Assert.AreEqual(3, result.Length);
        }

        [Test]
        public void Chain_renames_all_chain_names_accessible()
        {
            // path: [root, Y, X, driverGo]、Y: bar→baz、X: foo→bar (chain→ foo→baz)
            // A: 最深で "foo" 宣言 → ルート "baz"、driverGo で "foo"/"bar"/"baz" 全てアクセス可能
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "bar", "baz"));
            var L2 = ApplyRenames(L1, (ParameterNamespace.Animator, "foo", "bar"));
            var L3 = L2;
            var nameMaps = new[] { L0, L1, L2, L3 };
            var rootParams = new[] { Param("foo", effectiveName: "baz") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar", "baz" }, EffectiveNames(result));
        }

        [Test]
        public void Hierarchy_object_has_own_rename()
        {
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var nameMaps = new[] { L0, L1 };
            var rootParams = new[] { Param("foo", effectiveName: "bar") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar" }, EffectiveNames(result));
        }

        [Test]
        public void Multiple_renames_same_level_no_chain_when_target_not_preexisting()
        {
            // 宣言順: foo→bar, bar→baz
            //   foo→bar 適用後 {foo→bar}
            //   bar→baz 適用時 target "baz" は無く chain無し → {foo→bar, bar→baz}
            // A: "foo" → "bar"、B: "bar" → "baz"
            var L0 = Empty;
            var L1 = ApplyRenames(L0,
                (ParameterNamespace.Animator, "foo", "bar"),
                (ParameterNamespace.Animator, "bar", "baz"));
            var nameMaps = new[] { L0, L1 };
            var rootParams = new[]
            {
                Param("foo", effectiveName: "bar"), // A
                Param("bar", effectiveName: "baz"), // B
            };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            // A は "foo" だけ ("bar" は bar→baz で別物に化ける)
            CollectionAssert.AreEquivalent(new[] { "foo" },
                result.Where(p => p.OriginalName == "foo").Select(p => p.EffectiveName).ToArray());
            // B は "bar" と "baz" 両方
            CollectionAssert.AreEquivalent(new[] { "bar", "baz" },
                result.Where(p => p.OriginalName == "bar").Select(p => p.EffectiveName).ToArray());
        }

        [Test]
        public void Multiple_renames_same_level_chain_resolves_when_target_preexists()
        {
            // 宣言順: bar→baz, foo→bar
            //   bar→baz → {bar→baz}
            //   foo→bar 適用時 target "bar" あり → chain解決 → foo→baz
            //   → {bar→baz, foo→baz}
            // A: "foo" → "baz" (chain)
            var L0 = Empty;
            var L1 = ApplyRenames(L0,
                (ParameterNamespace.Animator, "bar", "baz"),
                (ParameterNamespace.Animator, "foo", "bar"));
            var nameMaps = new[] { L0, L1 };
            var rootParams = new[] { Param("foo", effectiveName: "baz") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar", "baz" }, EffectiveNames(result));
        }

        [Test]
        public void Namespace_isolation_physbones_unaffected_by_animator_renames()
        {
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var nameMaps = new[] { L0, L1 };
            var rootParams = new[] { Param("foo", ns: ParameterNamespace.PhysBonesPrefix) };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("foo", result[0].EffectiveName);
            Assert.AreEqual(ParameterNamespace.PhysBonesPrefix, result[0].Namespace);
        }

        [Test]
        public void Namespace_isolation_separate_renames_per_namespace()
        {
            // 同じ inner/outer 名でも namespace が違えば独立
            var L0 = Empty;
            var L1 = ApplyRenames(L0,
                (ParameterNamespace.Animator, "foo", "bar"),
                (ParameterNamespace.PhysBonesPrefix, "foo", "baz"));
            var nameMaps = new[] { L0, L1 };
            var rootParams = new[]
            {
                Param("foo", effectiveName: "bar", ns: ParameterNamespace.Animator),
                Param("foo", effectiveName: "baz", ns: ParameterNamespace.PhysBonesPrefix),
            };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar" },
                result.Where(p => p.Namespace == ParameterNamespace.Animator).Select(p => p.EffectiveName).ToArray());
            CollectionAssert.AreEquivalent(new[] { "foo", "baz" },
                result.Where(p => p.Namespace == ParameterNamespace.PhysBonesPrefix).Select(p => p.EffectiveName).ToArray());
        }

        [Test]
        public void Clone_preserves_metadata()
        {
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var nameMaps = new[] { L0, L1 };
            var rootParams = new[]
            {
                Param("foo", effectiveName: "bar",
                    ns: ParameterNamespace.Animator,
                    type: AnimatorControllerParameterType.Bool),
            };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            foreach (var p in result)
            {
                Assert.AreEqual("foo", p.OriginalName);
                Assert.AreEqual(ParameterNamespace.Animator, p.Namespace);
                Assert.AreEqual(AnimatorControllerParameterType.Bool, p.ParameterType);
            }
        }

        [Test]
        public void Rename_above_hierarchy_inner_outer_accessible()
        {
            // path: [root, mid, driverGo]、mid に foo→bar
            // driverGo は mid の下なので "foo" と "bar" 両方アクセス可能
            var L0 = Empty;
            var L1 = ApplyRenames(L0, (ParameterNamespace.Animator, "foo", "bar"));
            var L2 = L1;
            var nameMaps = new[] { L0, L1, L2 };
            var rootParams = new[] { Param("foo", effectiveName: "bar") };

            var result = AvatarParametersHierarchy
                .RemapToHierarchyFromNameMaps(rootParams, nameMaps)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "foo", "bar" }, EffectiveNames(result));
        }
    }
}
