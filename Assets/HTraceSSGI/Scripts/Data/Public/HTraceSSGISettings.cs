//pipelinedefine
#define H_URP

using HTraceSSGI.Scripts.Data.Private;
using UnityEngine;

namespace HTraceSSGI.Scripts.Data.Public
{
    /// <summary>
    /// Change global HTrace SSGI settings, only for UseVolumes is disabled in HTrace SSGI Renderer Feature
    /// </summary>
    public static class HTraceSSGISettings
    {
        private static HTraceSSGIProfile _cachedProfile;
    
        public static HTraceSSGIProfile ActiveProfile
        {
            get
            {
                return _cachedProfile;
            }
        }
    
        public static void SetProfile(HTraceSSGIProfile profile)
        {
            _cachedProfile = profile;
        }
    }
}
