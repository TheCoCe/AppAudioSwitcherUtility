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

In server mode the application starts a websocket server to listen for requests. Requests to the application are sent as UTF8 encoded json.
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
  	"title": "Request",
	"type": "object",
	"required": ["Type", "Payload"],
	"properties":
	{
		"Type":
		{
			"type": "string"
		},
		"Payload":
		{
			"type": "object"
		}
	}
}
```

To start the application in server mode you need to start the application with the `--server` option. 

`AppAudioSwitcherUtility.exe --server [(--port|-p) <port>]`

If `--port` is not specified, the server will start on `http://localhost:32122/ws/` and wait for a connection.

In server mode the app will listen for requests from connected clients and handle them as they are incoming. The app will also notify clients about changes in devices, sessions or the focused application.

### Requests
#### DevicesMessageRequest
Request the current AudioDevice info for a device type.
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
	"title": "DevicesMessageRequest",
	"type": "object",
	"required": ["DataFlow"],
	"properties":
	{
		"DataFlow":
    	{
      		"type": "string"
			"description": "0 = eRender, 1 = eCapture, 2 = eAll"
    	}
	}
}
```
Will be responded with a list of devices that match the requested type:
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
	"title": "DevicesMessageResponse",
	"type": "object",
	"required": ["Devices"],
	"properties": {
		"Devices": {
			"type": "array",
			"items": {
				"type": "object",
                "required": ["DeviceId", "DeviceName", "State", "DataFlow"],
				"properties": {
					"DeviceId": {
						"type": "string"
					},
					"DeviceName": {
						"type": "string"
					},
					"State": {
						"type": "number"
						"description": "0 = ACTIVE, 1 = DISABLED, 2 = NOTPRESENT, 3 = UNPLUGGED"
					},
					"DataFlow": {
						"type": "number"
						"description": "0 = eRender, 1 = eCapture, 2 = eAll"
					}
				}
			}
		}
	}
}
```
#### FocusedMessageRequest
Request information about a process or the currently focused process.
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
	"title": "FocusedMessageRequest",
	"type": "object",
	"required": ["Icon"],
	"properties": {
		"Icon": {
			"type": "boolean",
			"description": "Indicates whether the response should include a base64 encoded icon for the focused application"
		},
		"ProcessId": {
			"type": "integer",
			"description": "Optional process ID to retrieve process data for. If not provided, the process ID of the focused application will be used."
		}
	}
}

```
Will cause a response to be sent that contains the following info.
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
	"title": "FocusedMessageResponse",
	"type": "object",
	"required": ["ProcessId", "ProcessName", "DeviceId", "HasSession"],
	"properties": {
		"ProcessId": {
			"type": "integer"
		},
		"ProcessName": {
			"type": "string"
		},
		"DeviceId": {
			"type": "string"
		},
		"HasSession": {
			"type": "boolean"
		},
		"ProcessIconBase64": {
			"type": "string"
		}
	}
}
```
#### SetAppDeviceMessageRequest
Can be used to set the used audio device for a specific process.
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
	"title": "SetAppDeviceMessageRequest",
	"type": "object",
	"required": ["ProcessId", "DeviceId"],
	"properties": {
		"ProcessId": {
			"type": "integer"
		},
		"DeviceId": {
			"type": "string"
		}
	}
}
```
Will cause a acknowledge response:
```JSON
{
	"$schema": "http://json-schema.org/draft/2019-09/schema",
	"title": "SetAppDeviceMessageResponse",
	"type": "object",
	"required": ["Success"],
	"properties": {
		"Success": {
			"type": "boolean"
		}
	}
}
```

### Example
The AppAudioSwitcher StreamDeck plugin communicates via websocket by spawning the app as a child process and then connecting similar to this:

```TypeScript
export interface Message {
    Type: string
    Payload: any
}

const socket = new NodeWebSocket(`ws://localhost:32122/ws/`);

socket.onmessage = (ev) => {
    const msg = JSON.parse(ev.data.toString()) as Message;
    switch(msg.Type) {
        "DevicesMessageResponse": {
            HandleDevicesResponse(msg.Payload);
            break;
        },
        "FocusedMessageResponse": {
            HandleFocusedResponse(msg.Payload);
            break;
        }
    }
}
```
