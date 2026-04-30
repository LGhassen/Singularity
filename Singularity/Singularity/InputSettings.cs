using KSP.IO;
using UnityEngine;

namespace Singularity
{
	// This class registers custom key bindings that appear in KSP's Settings > Key Bindings menu
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class InputSettings : MonoBehaviour
	{
		public static KeyBinding toggleSingularityUI;
		
		void Awake()
		{
			DontDestroyOnLoad(this);
			
			// Create a key binding for toggling Singularity UI (default: Modifier + S)
			toggleSingularityUI = new KeyBinding(KeyCode.S);
		}
	}
}
