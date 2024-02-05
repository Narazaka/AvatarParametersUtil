using System.Collections.Generic;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Narazaka.VRChat.AvatarParametersUtil
{
    public interface IParameterNameAndTypesProvider
    {
        IEnumerable<VRCExpressionParameters.Parameter> GetParameterNameAndTypes();
    }
}
