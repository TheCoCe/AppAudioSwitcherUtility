# About

This is a utility application based on .NET Framework 4.6.2 used to retrieve info about available audio endpoints aswell as setting per application enpoints on windows.
It was built for the [AppAudioSwitcher](https://github.com/TheCoCe/AppAudioSwitcher-StreamDeck) StreamDeck plugin.
All the audio endpoint code is heavily based on [EarTrumpet](https://github.com/File-New-Project/EarTrumpet/). If you are looking to implement similar functionality, I highly recommend checking out their project!

# Requirements

For this application to work you will need to have [.NET Framework 4.6.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net462) installed. 
This will only work if you have at least Windows 10 1803 (April 2018 Update) installed.

# Usage

You can either invoke this app once for each information you want to gather via arguments, or start it in a server mode where it will open a TCP connection on localhost to communicate.

`AppAudioSwitcherUtility.exe [mode] <mode-type> [options]` 

The application will always respond with a JSON string in the form:

```JSON
{
    "id": "",
    "payload": {}
}
```

- `id` is the indetifier for the type of payload.
- `payload` contains the actual data for the request and differes between the different requests.

## Arguments

### Get mode

`AppAudioSwitcherUtility.exe --get <mode-type> [options]`

The get mode allows you to retrieve data. When invoked directly it will respond with a JSON string in the stdout.
You can define what info to retrieve by setting the corresponding `<mode-type>` argument.

#### Mode Types:

##### Devices

`--get devices [--type <type>] [--state <state>]`: Retrieves a list of available audio devices (endpoints)
  - `[--type|-t <type>]` to define the type of devices you are interested in
    - `capture|c`: for capture devices (e.g. microphones)
    - `render|r`: for rendering devices (e.g. speakers)
    - `all|a`: for all devices
  - `[--state|-s <state>]` to define a state filter for the devices
    - `active`: active devices
    - `disabled`: disabled devices
    - `notpresent`: notpresent devices
    - `unplugged`: unplugged devices

Example: `AppAudioSwitcherUtility.exe --get devices --type render -s active`

Result:
```JSON
{
	"id": "devices",
	"payload": {
		"devices": [
			{
				"Name": "Sound Device 1",
				"Id": "{0.0.0.00000000}.{00000000-0000-0000-0000-000000000000}"
			},
			{
				"Name": "Sound Device 2",
				"Id": "{0.0.0.00000000}.{00000000-0000-0000-0000-000000000000}"
			}
		]
	}
}
```
---

##### Focused

`--get focused [--icon]`: Retrives information about the currently focused process on the machine.

- `[--icon]`: If this flag is set, the resulting JSON will contain a base64 encoded image of the process

Example: `--get focused --icon`

Result:
```JSON
{
	"id": "icon",
	"payload": {
		"processId": "123456",
		"processName": "My Process",
		"deviceId": "",
		"processIconBase64": "iVBORw0KG..."
	}
}
```

### Set mode

`AppAudioSwitcherUtility.exe --set <mode-type> [options]`

#### Mode Types:

##### AppDevice

`--set appDevice (--process|-p) <pid> (--device|-d) <did>`: Retrieves a list of available audio devices (endpoints)
  - `(--process|-p) <pid>` process id of the process to set the device for
  - `(--device|-d) <did>` id of the device to set for this process, must be the same format as the ids retrieved

**Example:** `AppAudioSwitcherUtility.exe --set appDevice -p 123456 -d "{0.0.0.00000000}.{00000000-0000-0000-0000-000000000000}"`

**Result:** Sets the device for this application.

## Server Mode

In server mode the application starts a TCP server to listen for commands. Commands to the application are sent as UTF8 encoded strings and follow the same format as the arguments to the application. To start the application in server mode you need to start the application with the `--server` option.

`AppAudioSwitcherUtility.exe --server [(--port|-p) <port>]`

If `--port` is not specified, the server will start on `localhost:32122` and wait for a connection.

### Example
The AppAudioSwitcher StreamDeck plugin communicates via TCP by spawning the app as a child process and then connecting similar to this:

```TypeScript
const client = new Socket();

client.on("data", (data) => {
    const JsonObj = JSON.parse(data.toString());
    switch (JsonObj.id) {
        case "devices": {
            // Handle device message here
            ...
            break;
        }
        case "focused": {
            // Handle focused process message here
            ...
            break;
        }
    }
})
 
client.connect(32122, "127.0.0.1");

client.write("--get devices --type render -s active");
...
```
