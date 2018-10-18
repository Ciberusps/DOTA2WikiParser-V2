using System.IO;
using UnityEditor;

namespace Opencoding.Console.Editor
{
	[InitializeOnLoad] 
	public class DetectUpgrade
	{
		static DetectUpgrade()
		{
			EditorApplication.delayCall += ShowDeleteMessage;
		}

		private static void ShowDeleteMessage()
		{
			if (Directory.Exists("Assets/Opencoding/3rdParty/XcodeAPI"))
			{
				EditorUtility.DisplayDialog("Important Upgrade Information",
					"This version of TouchConsole Pro changes the way Xcode projects are built to reduce conflicts with other plugins.\n\n" +
					"This upgrade process will automatically delete the old system.\n\n" +
					"You MUST now do a clean build of your Xcode project or you will get errors.",
					"Ok");
				AssetDatabase.DeleteAsset("Assets/Opencoding/3rdParty/XcodeAPI");
				AssetDatabase.Refresh();
			}
		}
	}
}