//pipelinedefine
#define H_URP


using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Infrastructure.URP;
using UnityEngine;

namespace HTraceSSGI.Scripts
{
	[ExecuteAlways, ExecuteInEditMode, ImageEffectAllowedInSceneView, DefaultExecutionOrder(100)]
	[HelpURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK)]
	public class HTraceSSGI : MonoBehaviour
	{
		[Tooltip("Currently used HTrace SSGI profile with settings")]
		public HTraceSSGIProfile Profile;

		private void OnEnable() {
			CheckProfile();
		}

		private void OnDisable()
		{
			HTraceSSGISettings.SetProfile(null);
		}

		void OnValidate() {
			CheckProfile();
		}

		private void Reset() {
			CheckProfile();
		}

		void CheckProfile() 
		{
			if (Profile == null)
			{
				Profile = ScriptableObject.CreateInstance<HTraceSSGIProfile>();
				Profile.name = "New HTrace SSGI Profile";
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
			
			HTraceSSGISettings.SetProfile(this.Profile);
		}
	}
}
