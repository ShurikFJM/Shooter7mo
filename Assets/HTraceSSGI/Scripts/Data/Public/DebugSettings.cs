//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Globals;
using UnityEngine;

namespace HTraceSSGI.Scripts.Data.Public
{
	[Serializable]
	public class DebugSettings
	{
		public bool ShowBowels       = false;
		public bool ShowFullDebugLog = false;
		public LayerMask            HTraceLayer          = ~0;

		public bool TestCheckBox1;
		public bool TestCheckBox2;
		public bool TestCheckBox3;
	}
}
