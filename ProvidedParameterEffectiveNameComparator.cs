#if UNITY_EDITOR
using nadena.dev.ndmf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narazaka.VRChat.AvatarParametersUtil
{
    public class ProvidedParameterEffectiveNameComparator : IEqualityComparer<ProvidedParameter>
    {
        public bool Equals(ProvidedParameter x, ProvidedParameter y) => x.EffectiveName == y.EffectiveName;

        public int GetHashCode(ProvidedParameter obj) => obj.EffectiveName == null ? 0 : obj.EffectiveName.GetHashCode();
    }
}
#endif
