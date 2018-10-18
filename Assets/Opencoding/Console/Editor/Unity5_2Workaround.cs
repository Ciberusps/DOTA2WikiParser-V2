#if UNITY_EDITOR_OSX
using UnityEngine;
using System.Collections;
using Opencoding.Console;
using UnityEditor;

// Unity 5.2 breaks keyboard input on Mac. This is a work around that will allow you to open the console with a menu item (and Command+G).

public static class Unity5_2Workaround  {
	[MenuItem("Window/Toggle TouchConsole Pro _%g", true)]
	static bool ToggleConsoleMenuItem_Enabled () {
		return DebugConsole.Instance != null;
	}

	[MenuItem("Window/Toggle TouchConsole Pro _%g")]
	static void ToggleConsoleMenuItem () {
		DebugConsole.IsVisible = !DebugConsole.IsVisible;
	}
}
#endif
