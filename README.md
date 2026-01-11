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
    id: "",
    payload: {}
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


## Server Mode
