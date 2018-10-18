using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
#if !UNITY_5
using Opencoding.XCodeEditor;
#endif
using UnityEngine;

namespace Opencoding.Console.Editor
{
	static class BuildPostProcessor
	{
#if !UNITY_5
		[PostProcessBuild(200)]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
			if (target != BuildTarget.iOS)
				return;

			var project = new XCProject(pathToBuiltProject);
				project.ApplyMod(Application.dataPath, Path.Combine(DebugConsoleEditorSettings.OpencodingDirectoryLocation, "Console/Editor/fixup.projmods"));
				project.Save();
		}
#endif

		[PostProcessScene]
		public static void OnPostprocessScene()
		{
			if (EditorApplication.isPlaying)
				return;

			var debugConsoles = UnityEngine.Object.FindObjectsOfType<DebugConsole>();
			if(debugConsoles.Length > 1)
				throw new InvalidOperationException("More than one debug console in the scene " + EditorApplication.currentScene);

			if (debugConsoles.Length == 0)
				return;

			var debugConsole = debugConsoles[0];
			if(debugConsole.Settings.OnlyInDevBuilds && !EditorUserBuildSettings.development)
				UnityEngine.Object.DestroyImmediate(debugConsole.gameObject);

			if(!String.IsNullOrEmpty(debugConsole.Settings.DisableIfDefined) && EditorUserBuildSettings.activeScriptCompilationDefines.Contains(debugConsole.Settings.DisableIfDefined))
				UnityEngine.Object.DestroyImmediate(debugConsole.gameObject);

			if (debugConsole.Settings.AutoSetVersion)
			{
				debugConsole.Settings.GameVersion = PlayerSettings.bundleVersion;
			}
		}
	}
}