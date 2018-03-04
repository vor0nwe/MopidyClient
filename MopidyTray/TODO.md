TO DO
=====

  * Write a MopidyClient wrapper
    * MopidyClient(Uri uri)
	* Methods (preferably async)
	  * Connect()
	  * Disconnect()
	  * void Command(string command, params object[] parameters); // send generic command; don’t wait for result
	  * MopidyResult Command(uint timeout, string command, params object[] parameters); // send generic command — and wait for result
	  * ...more specific commands matching the JSON-RPC API’s [core methods](https://docs.mopidy.com/en/latest/api/core/)
	* Events:
	  * OnConnect
	  * OnDisconnect
	  * OnError
	  * OnMessage (generic)
	  * OnEvent (when the message contains an "event" property)
	  * OnResponse (when the message contains a "result" property; it’s a response to a command)
	  * ...more specific events matching the JSON-RPC API’s [core events](https://docs.mopidy.com/en/latest/api/core/#mopidy.core.CoreListener)
  * React to user actions on tray icon:
    * Show/hide main form on single-click on tray icon
  * Add easy controls for play/pause, previous/next
  * Add easy controls for volume up/down (and easy volume shortcuts 1 through 9)
  * 
  * Also support global hotkeys
    * media keys + a custom hotkey to register/unregister the media keys (so they can also be used in other apps)
	* custom keys for play/pause, previous/next, volume up/down