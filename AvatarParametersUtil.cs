using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System;
using nadena.dev.ndmf;

#if AVATAR_PARAMETERS_UTIL_HAS_MA
using nadena.dev.modular_avatar.core;
#endif

namespace Narazaka.VRChat.AvatarParametersUtil
{
    public static class AvatarParametersUtil
    {
        class ParameterNameComparer : IEqualityComparer<VRCExpressionParameters.Parameter>
        {
            public bool Equals(VRCExpressionParameters.Parameter x, VRCExpressionParameters.Parameter y)
            {
                return x.name == y.name;
            }

            public int GetHashCode(VRCExpressionParameters.Parameter obj)
            {
                return obj.name.GetHashCode();
            }
        }

        [Obsolete("Use nmdf ParameterInfo")]
        public static IEnumerable<VRCExpressionParameters.Parameter> GetParameters(VRCAvatarDescriptor avatar, bool includeAnimators = false)
        {
            var parameters = Enumerable.Empty<VRCExpressionParameters.Parameter>();
            if (avatar == null)
            {
                return parameters;
            }

            if (avatar.expressionParameters != null) parameters = parameters.Concat(avatar.expressionParameters.parameters);

#if AVATAR_PARAMETERS_UTIL_HAS_MA
            parameters = parameters.Concat(GetModularAvatarParameters(avatar));
#endif
            parameters = parameters.Concat(GetVRCPhysBoneParameters(avatar));
            parameters = parameters.Concat(GetVRCContactReceiverParameters(avatar));

            if (includeAnimators)
            {
#if UNITY_EDITOR
                parameters = parameters.Concat(GetAvatarAnimatorControllerParameters(avatar));
#if AVATAR_PARAMETERS_UTIL_HAS_MA
                parameters = parameters.Concat(GetModularAvatarAnimatorControllerParameters(avatar));
#endif
#endif
            }

            return parameters.Where(p => !string.IsNullOrEmpty(p.name)).Distinct(new ParameterNameComparer());
        }

#if AVATAR_PARAMETERS_UTIL_HAS_MA
        [Obsolete("Use nmdf ParameterInfo")]
        public static IEnumerable<VRCExpressionParameters.Parameter> GetModularAvatarParameters(VRCAvatarDescriptor avatar)
        {
            var maParameters = avatar.GetAllComponentsInChildren<ModularAvatarParameters>();
            return maParameters
                .Where(map => map.parameters != null)
                .SelectMany(maParameter => maParameter
                    .parameters
                    .Where(p => p.syncType != ParameterSyncType.NotSynced)
                    .Select(p => new VRCExpressionParameters.Parameter
                    {
                        name = string.IsNullOrWhiteSpace(p.remapTo) ? p.nameOrPrefix : p.remapTo,
                        valueType = p.syncType == ParameterSyncType.Bool ? VRCExpressionParameters.ValueType.Bool : p.syncType == ParameterSyncType.Int ? VRCExpressionParameters.ValueType.Int : VRCExpressionParameters.ValueType.Float,
                        saved = p.saved,
                        defaultValue = p.defaultValue,
                        networkSynced = !p.localOnly,
                    })
                    );
        }
#endif

        public static IEnumerable<VRCExpressionParameters.Parameter> GetVRCPhysBoneParameters(VRCAvatarDescriptor avatar)
        {
            return avatar.GetAllComponentsInChildren<VRCPhysBone>().SelectMany(ToVRCExpressionParametersParameters);
        }

        public static IEnumerable<VRCExpressionParameters.Parameter> ToVRCExpressionParametersParameters(this VRCPhysBone physBone)
        {
            if (string.IsNullOrEmpty(physBone.parameter)) return Enumerable.Empty<VRCExpressionParameters.Parameter>();
            return new VRCExpressionParameters.Parameter[] {
                new VRCExpressionParameters.Parameter {
                    name = $"{physBone.parameter}_IsGrabbed",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = false,
                    defaultValue = 0f,
                    networkSynced = false,
                },
                new VRCExpressionParameters.Parameter {
                    name = $"{physBone.parameter}_IsPosed",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = false,
                    defaultValue = 0f,
                    networkSynced = false,
                },
                new VRCExpressionParameters.Parameter {
                    name = $"{physBone.parameter}_Angle",
                    valueType = VRCExpressionParameters.ValueType.Float,
                    saved = false,
                    defaultValue = 0f,
                    networkSynced = false,
                },
                new VRCExpressionParameters.Parameter {
                    name = $"{physBone.parameter}_Stretch",
                    valueType = VRCExpressionParameters.ValueType.Float,
                    saved = false,
                    defaultValue = 0f,
                    networkSynced = false,
                },
                new VRCExpressionParameters.Parameter {
                    name = $"{physBone.parameter}_Squish",
                    valueType = VRCExpressionParameters.ValueType.Float,
                    saved = false,
                    defaultValue = 0f,
                    networkSynced = false,
                },
            };
        }

        public static IEnumerable<VRCExpressionParameters.Parameter> GetVRCContactReceiverParameters(VRCAvatarDescriptor avatar)
        {
            var receivers = avatar.GetAllComponentsInChildren<VRCContactReceiver>();
            foreach (var receiver in receivers)
            {
                yield return receiver.ToVRCExpressionParametersParameter();
            }
        }

        public static VRCExpressionParameters.Parameter ToVRCExpressionParametersParameter(this VRCContactReceiver receiver)
        {
            return new VRCExpressionParameters.Parameter
            {
                name = receiver.parameter,
                valueType = receiver.receiverType.ToVRCExpressionParametersValueType(),
                saved = false,
                defaultValue = 0f,
                networkSynced = false,
            };
        }


        public static VRCExpressionParameters.ValueType ToVRCExpressionParametersValueType(this VRC.Dynamics.ContactReceiver.ReceiverType receiverType)
        {
            switch (receiverType)
            {
                case VRC.Dynamics.ContactReceiver.ReceiverType.Proximity:
                    return VRCExpressionParameters.ValueType.Float;
                default:
                    return VRCExpressionParameters.ValueType.Bool;
            }
        }

#if UNITY_EDITOR
        public static IEnumerable<VRCExpressionParameters.Parameter> GetAvatarAnimatorControllerParameters(VRCAvatarDescriptor avatar)
        {
            if (!avatar.customizeAnimationLayers) return Enumerable.Empty<VRCExpressionParameters.Parameter>();

            var baseLayers = avatar.baseAnimationLayers.Where(l => !l.isDefault).Select(l => l.animatorController).Where(ac => ac != null);
            var specialLayers = avatar.specialAnimationLayers.Where(l => !l.isDefault).Select(l => l.animatorController).Where(ac => ac != null);
            return baseLayers.Concat(specialLayers).SelectMany(GetAnimatorControllerParameters);
        }

#if AVATAR_PARAMETERS_UTIL_HAS_MA
        public static IEnumerable<VRCExpressionParameters.Parameter> GetModularAvatarAnimatorControllerParameters(VRCAvatarDescriptor avatar)
        {
            var maMergeAnimators = avatar.GetAllComponentsInChildren<ModularAvatarMergeAnimator>();
            return maMergeAnimators.Select(ma => ma.animator).Where(ac => ac != null).SelectMany(GetAnimatorControllerParameters);
        }
#endif

        public static IEnumerable<VRCExpressionParameters.Parameter> GetAnimatorControllerParameters(RuntimeAnimatorController animatorController)
        {
            var controller = animatorController as UnityEditor.Animations.AnimatorController;
            if (controller == null) return Enumerable.Empty<VRCExpressionParameters.Parameter>();
            return controller.parameters.Select(p => p.ToVRCExpressionParametersParameter());
        }
#endif

#if UNITY_EDITOR
        public static AnimatorControllerParameterType ToAnimatorControllerParameterType(this VRCExpressionParameters.ValueType valueType)
        {
            switch (valueType)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    return AnimatorControllerParameterType.Bool;
                case VRCExpressionParameters.ValueType.Int:
                    return AnimatorControllerParameterType.Int;
                case VRCExpressionParameters.ValueType.Float:
                    return AnimatorControllerParameterType.Float;
                default:
                    throw new System.InvalidCastException();
            }
        }

        public static VRCExpressionParameters.ValueType ToVRCExpressionParametersValueType(this AnimatorControllerParameterType valueType)
        {
            switch (valueType)
            {
                case AnimatorControllerParameterType.Bool:
                    return VRCExpressionParameters.ValueType.Bool;
                case AnimatorControllerParameterType.Int:
                    return VRCExpressionParameters.ValueType.Int;
                case AnimatorControllerParameterType.Float:
                    return VRCExpressionParameters.ValueType.Float;
                case AnimatorControllerParameterType.Trigger:
                    return VRCExpressionParameters.ValueType.Bool;
                default:
                    throw new System.InvalidCastException();
            }
        }

        public static AnimatorControllerParameter ToAnimatorControllerParameter(this VRCExpressionParameters.Parameter parameter)
        {
            return new AnimatorControllerParameter
            {
                name = parameter.name,
                type = parameter.valueType.ToAnimatorControllerParameterType(),
                defaultBool = parameter.valueType == VRCExpressionParameters.ValueType.Bool ? parameter.defaultValue > 0.5f : false,
                defaultInt = parameter.valueType == VRCExpressionParameters.ValueType.Int ? (int)parameter.defaultValue : 0,
                defaultFloat = parameter.valueType == VRCExpressionParameters.ValueType.Float ? parameter.defaultValue : 0f,
            };
        }

        public static AnimatorControllerParameter ToAnimatorControllerParameter(this ProvidedParameter parameter)
        {
            return new AnimatorControllerParameter
            {
                name = parameter.EffectiveName,
                type = parameter.ParameterType == null ? AnimatorControllerParameterType.Float : (AnimatorControllerParameterType)parameter.ParameterType,
            };
        }

        public static VRCExpressionParameters.Parameter ToVRCExpressionParametersParameter(this AnimatorControllerParameter parameter)
        {
            return new VRCExpressionParameters.Parameter
            {
                name = parameter.name,
                valueType = parameter.type.ToVRCExpressionParametersValueType(),
                saved = false,
                defaultValue = parameter.type == AnimatorControllerParameterType.Bool ? (parameter.defaultBool ? 1f : 0f) : parameter.type == AnimatorControllerParameterType.Int ? parameter.defaultInt : parameter.defaultFloat,
                networkSynced = false,
            };
        }
#endif
        static T[] GetAllComponentsInChildren<T>(this Component component)
        {
            return component.GetComponentsInChildren<T>(true);
        }
    }
}
