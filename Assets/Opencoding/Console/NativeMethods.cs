using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	static class NativeMethods
	{
#if UNITY_IOS
		[DllImport("__Internal")]
		private static extern void _opencodingConsoleBeginEmail(string toAddress, string subject, string body, bool isHTML);

		[DllImport("__Internal")]
		private static extern void _opencodingConsoleAddAttachment(byte[] bytes, int length, string mimeType, string filename);

		[DllImport("__Internal")]
		private static extern void _opencodingConsoleFinishEmail();

		[DllImport("__Internal")]
		private static extern void _opencodingConsoleCopyToClipboard(string text);

		[DllImport("__Internal")]
		private static extern int _opencodingConsoleGetNativeScreenWidth();
#endif

		public static void SendEmail(Email email)
		{
#if UNITY_IOS && !UNITY_EDITOR
			_opencodingConsoleBeginEmail(email.ToAddress, email.Subject, email.Message, email.IsHTML);
			foreach (var attachment in email.Attachments)
			{
				_opencodingConsoleAddAttachment(attachment.Data, attachment.Data.Length, attachment.MimeType, attachment.Filename);
			}
			_opencodingConsoleFinishEmail();
#elif UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass androidEmailClass = new AndroidJavaClass("net.opencoding.console.Email");
			
			androidEmailClass.CallStatic("beginEmail", email.ToAddress, email.Subject, email.Message, email.IsHTML);
			var emailAttachmentsDirectory = Path.Combine(Application.temporaryCachePath, "EmailAttachments");
			Directory.CreateDirectory(emailAttachmentsDirectory);
			foreach (var attachment in email.Attachments)
			{
				var attachmentPath = Path.Combine(emailAttachmentsDirectory, attachment.Filename);
				File.WriteAllBytes(attachmentPath, attachment.Data);
				androidEmailClass.CallStatic("addAttachment", attachmentPath);
			}

			androidEmailClass.CallStatic("finishEmail");
#else
			throw new InvalidOperationException("Emailing is not supported on this platform. Please contact support@opencoding.net and I'll do my best to support it!");
#endif
		}

		public static void CopyTextToClipboard(string text)
		{
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
			var textEditor = new TextEditor {content = new GUIContent(text)};
			textEditor.SelectAll();
			textEditor.Copy();
#elif UNITY_IOS 
			_opencodingConsoleCopyToClipboard(text);
#elif UNITY_ANDROID
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("net.opencoding.console.NativeMethods");
			androidJavaClass.CallStatic("copyToClipboard", text);
#else
			Debug.LogError("Copy and paste isn't supported on this platform currently. Please contact support@opencoding.net and I'll do my best to support it!");
#endif
		}

		public static float GetNativeScreenScaleFactor()
		{
#if UNITY_EDITOR || !UNITY_IOS
			return 1.0f;
#else
			return Screen.width / (float)_opencodingConsoleGetNativeScreenWidth();
#endif
		}
	}
}