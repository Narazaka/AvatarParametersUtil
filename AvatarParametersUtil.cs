using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System;
#if UNITY_EDITOR
using nadena.dev.ndmf;
#endif

namespace Narazaka.VRChat.AvatarParametersUtil
{
    public static class AvatarParametersUtil
    {
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
    }
}
