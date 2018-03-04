TO DO
=====

  * Write a MopidyClient wrapper
    * `MopidyClient(Uri uri)`
	* Methods (preferably async)
	  * `Connect()` #1
	  * `Disconnect()` #1
	  * `int Execute(string command, params object[] parameters);` // send generic command; don’t wait for result but return ID of command sent #1
	  * `MopidyResult Execute(uint timeout, string command, params object[] parameters);` // send generic command — and wait for result #3
	  * `Play(uint timeout = 0);` #5
	  * `Pause(uint timeout = 0);` #5
	  * `Toggle(uint timeout = 0);` #5
	  * `Next(uint timeout = 0);` #5
	  * `Previous(uint timeout = 0);` #5
	  * ...more specific commands matching the JSON-RPC API’s [core methods](https://docs.mopidy.com/en/latest/api/core/) #6
	* Properties // keep track by picking up relevant events; if not seen yet then specifically request the info #4
	  * `bool Muted;`
	  * `int Volume;`
	  * `string State;`
	  * `Track CurrentTrack;`
	* Events:
	  * `OnConnect` #1
	  * `OnDisconnect` #1
	  * `OnError` #1
	  * `OnMessage` (generic) #1
	  * `OnEvent` (when the message contains an "event" property) #2
	  * `OnResponse` (when the message contains a "result" property; it’s a response to a command) #2
	  * ...more specific events matching the JSON-RPC API’s [core events](https://docs.mopidy.com/en/latest/api/core/#mopidy.core.CoreListener)
  * UI: Add easy controls for (un)mute, volume up/down (and easy volume shortcuts 1 through 9)
  * UI: React to user actions on tray icon:
    * Show/hide main form on single-click on tray icon (`WM_CLICK`, `NIN_SELECT`, `NIN_KEYSELECT`)
	* Show a context menu on right-click on tray icon (`WM_CONTEXTMENU`), with options for play/pause, previous, next, (un)mute
	* Also show a volume submenu with 0%, 10%, etc.
  * UI: Also support global hotkeys
    * media keys + a custom hotkey to register/unregister the media keys (so they can also be used in other apps)
	* custom keys for play/pause, previous/next, volume up/down
