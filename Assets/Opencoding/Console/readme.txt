Thank you for purchasing TouchConsole Pro!

I really hope you enjoy using TouchConsole Pro. Please do send me any questions, suggestions or feedback at support@opencoding.net!

The documentation for TouchConsole Pro is available here: http://opencoding.net/TouchConsolePro/getting_started.php

If you just want to quickly have a play around with the console, open the ConsoleDemoScene scene in the Demo folder and press Play!

=== Changelog ===
== Version 2.0.0 ==
New features
- Massively improved log emails:
  - Quickly jump between errors/warnings etc.
  - Filter the log
  - See hardware data about the device that sent the log
  - See game data such as the camera position, direction etc (if you provide it - see example in DemoController.cs)
  - See a screenshot of the game (optionally, this can be disabled in the settings)
  - See the save file (if you provide it - see example in DemoController.cs)
  - See the ‘real’ device time that each log message occurred at
- Added buttons for toggling Exceptions and Asserts, rather than grouping them with Errors as Unity does. This can be disabled in the settings if you prefer the old behavior.
- Added icons for Exceptions and Asserts. Exceptions are magenta squares, asserts are blue circles (obviously!)
- You can now press and hold one of the log message type buttons (errors, warrnings etc.) at the top of the screen to only show that type. Press and hold again to show all log message types.
- Added option in the settings to automatically pause the game (setting Time.timeScale) when the console is opened. This defaults to being disabled.
- Game version is now shown at the bottom of the in-console settings popup (the cog in the top right corner)

Changes
- Settings are now in their own ScriptableObject asset. This will mean you need to re-set your settings if you’ve made any changes to the defaults, but it should mean that this won’t need to happen again in the future.

Bug fixes
- Workaround for Unity 5.2.x bug (http://tinyurl.com/qje3nd7) where the console couldn’t be opened on the OS X editor or OS X standalone builds. This means that on these platforms and versions you can only open the console with the tilde/back quote key. This should be fixed in a Unity patch release very soon.
- Fixed parameter suggestions not being shown for properties with the CommandHandler attribute added.
- Fixed console command lines in the log being counted as errors
- Fixed console opening animation being affected by Time.timeScale, causing it to fail to open if the game was paused.
- Fixed parameter suggestion buttons being left showing once one of them had been used to execute a command.
- Fixed the bottom of 'y' and 'g' characters on auto-complete buttons being cut off in the editor.
- Minor fixes to the layout of the search/filter bar.

== Version 1.5.0 ==
New features
- By default the console is now automatically added to any scene in the project when you press play. This doesn’t affect builds, and can be turned off via the Unity Preferences window.
- Added support for setting the default email address that logs should be emailed to.
- Added support for changing the scale of the console, with independent settings for the editor, mobile and standalone.
- Added ability to change the log history size limit (default 3000 items) using LogHistory.Instance.ItemLimit.
- Made it easier to modify the install location: you’ll need to edit the _opencodingDirectoryLocation field in DebugConsoleEditorSettings.cs.

Bug fixes
- OS X Editor/Standalone: Temporary work around for broken keyboard input on OS X (introduced in Unity 5.2). There’s now a menu item that allows you to show/hide the console in the editor (or Command+G).
- Fixed an error related to an undocumented property (TextEditor.pos) that was renamed in Unity 5.2.
- iOS: Fixed console appearing too large when the “Target Resolution” player setting is set to something other than Native.
- Fixed exception that occurred when Unity 'hot reloaded' the project due to the console not being able to completely serialize its state. A better fix for this will come in the future.
- Fixes exporting the console log on Windows where invalid characters in the filename caused it to fail.
- Fixed exporting the console log failing on standalone builds.
- Fixed exception occurring when using Debug.Assert.

== Version 1.4.0 ==
New features
- Automatically disables Unity GUI input when the console is opened. This can be disabled in the Settings if you have your own way of doing this. This works by setting the EventSystem.current property to null while the console is open and restoring it once the console is closed.
- Added two new callbacks - DebugConsole.Instance.ConsoleAboutToOpen and DebugConsole.Instance.ConsoleAboutToClose. These allow you to be notified when the console is going to open or close and prevent it, if you wish.

Bug fixes
- DebugConsole game objects will now destroy themselves if there is already an instance of DebugConsole loaded.
- Fixed the Instance field for the DebugConsole class not being set to null when it was Destroyed.

== Version 1.3.0 ==
New features
- Added Run button to the end of the input line on mobile devices. This is makes the console at least partly useable in landscape on Nexus and Xperia devices (where a Unity bug causes the touch screen keyboard to be non-interactive).
- Suggestion buttons now automatically execute the command if it takes no parameters.
- Made the keys that open/close the console configurable. By default this is one of ~, \, `, |, § or ±.

Bug fixes
- Fixed Android log emails not including the attachment sometimes.
- Fixed a very infrequent issue where an exception would sometimes be thrown when log messages were emitted from a non-main thread.
- Fixed error that sometimes occurred when running the Demo scene.
- Fixed an issue on Windows where the console would flash up for a single frame when opened.
- Fixed compilation errors with Unity 5 on iOS.

== Version 1.2.0 ==

IMPORTANT: If upgrading to this version, make sure you rebuild your Xcode project from scratch or you will get compile errors.

New Features
- Added CommandHandlers.BeforeCommandExecutedHook that allows you to prevent a command from being executed.
- Added a demo scene and code that shows how you can use this to ask the user for a password before certain commands are executed - useful for public betas.

Changes
- Switched to a different method for modifying the Xcode project. This should be more compatible with other Unity plugins, most notably the Facebook SDK. On upgrade, the old code for this will be automatically deleted to avoid the unnecessary code hanging around - you may notice this in your version control system.
- The filter bar is now automatically closed when the console is.
- On Mobile: Opening the filter bar with the console maximized now temporarily minimizes the console so the keyboard doesn’t overlay the console.

Bug Fixes
- Worked around a bug in Unity 4.6.1 that caused a crash on iOS and Android (thanks to the multiple users who noticed this!)
- Fixed copying text not working in the web player (thanks jerotas!)
- Fixed an exception that occurred when the filter bar was closed using the Done/Return button on the Touch Screen Keyboard.

== Version 1.1.1 ==

New Features
- Added a new method for opening the console - holding down three fingers for about half a second. This can be enabled in the settings.
- Added hook to allow you to customise the email that is sent - extra attachments can be added and the message modified or replaced. This is useful for adding your save file or screenshots etc.
- Added method for triggering the log email to be sent, if you want to provide another method for sending it.

Bug Fixes
- Fixed the log being blank on Unity 5.
- Fixed WebGL builds on Unity 5.
- Fixed an error when the console was used in a game with stripping enabled (added a link.xml file).
- Fix for builds failing when the console was used in a game with the Facebook SDK included.