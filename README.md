# DV Multiplayer Mod

DV Multiplayer Mod is a modification to allow multiplayer in Derail Valley.

## Requirements

- [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)

## Installation

Download the mod from [Nexus Mods](https://www.nexusmods.com/derailvalley/mods/272)

Install the mod with Unity Mod Manager.

## Usage

When in game press the HOME key to open the menu (F7 in VR). In the menu you have the option to host your game or to connect to a game someone is hosting.

### Hosting
When hosting make sure you have port forwarded the port: 4296 UDP and 4296 TCP (unless you have changed this.)

*This mod does not have UPnP support.*

### Connecting
When connecting make sure you have the IP of the person you want to connect to. The port should be 4296 unless the host changed it.

## Supported mods
* Passenger Jobs

For everything else:
As of currently this mod has no real mod syncing this will maybe be added later but for now this is not the case. So if you use mods with the current save file please back the save file up so there are no game breaking issues that can corrupt your save game. You can test out if mods are compatible but remember to back your normal save up. I'm not reliable for your corrupted saves! [Skip tutorial? Use this save game](https://www.nexusmods.com/derailvalley/mods/88).

## Features
Features have been split into sections to show different parts of the mod. The state indicates if a feature is currently being worked on/improved and the version introduced column shows which version of the mod that feature was first implemented.

* :x: - This is currently not being worked on, but is planned.
* :hourglass: - This feature is currently being worked on.
* :heavy_check_mark: - this feature has been completed and is implemented.

### Server/Client Networking

| **Feature**                  |      **State**     | **Version Introduced** |
|------------------------------|:------------------:|------------------------|
| Connect/Disconnect to Server | :heavy_check_mark: | 1.0.0                  |
| Server Save Downloader       | :heavy_check_mark: | 1.1.0                  |
| Mod Mismatch Checking        | :heavy_check_mark: | *1.3.0* ([#18](https://github.com/Patrick-van-Halm/dv-multiplayer-mod/issues/18)) |
| Reload Single Player Save    | :hourglass:        | 1.1.0                  |

### Syncronization

| **Feature**                  |      **State**     | **Version Introduced** |
|------------------------------|:------------------:|------------------------|
| DE2 (Shunter)                | :heavy_check_mark: | 1.0.0                  |
| DE6 (Diesel)                 | :heavy_check_mark: | 1.3.5                  |
| SH282 (Steamer)              | :heavy_check_mark: | 1.3.5                  |
| Coupling                     | :hourglass:        | 1.0.0                  |
| Decoupling                   | :hourglass:        | 1.0.0                  |
| Multiple Unit Working        | :hourglass:        | 1.4.0                  |
| Player                       | :hourglass:        | 1.0.0                  |
| Turntable                    | :hourglass:        | *1.3.0* ([#16](https://github.com/Patrick-van-Halm/dv-multiplayer-mod/issues/16)) |
| Junctions                    | :hourglass:        | 1.0.0                  |
| World                        | :hourglass:        | 1.0.0                  |

### Multiplayer Features

| **Feature**                  |      **State**     | **Version Introduced** |
|------------------------------|:------------------:|------------------------|
| Name Tags                    | :heavy_check_mark: | 1.2.1                  |
| Custom UI                    | :heavy_check_mark: | *1.3.0* ([#26](https://github.com/Patrick-van-Halm/dv-multiplayer-mod/pull/26)) |
| Show Players on Map          | :heavy_check_mark: | 1.4.0                  |
| SteamAPI Integration         | :x:                |                        |

### Reporting Issues
If you have found a bug, please open an issue and describe the bug in as much detail as possible and include both the server log which can be found in:
* Derail Valley Directory -> Mods -> DVMultiplayer -> Logs -> (Date) -> (Time).log

And the player log, which can be found in:
* AppData -> LocalLow -> Altfuture -> Derail Valley -> Player.log

## Contributing
Pull requests/changes are welcome, please open an issue first to discuss what you would like to change.

Please make sure to test and format code as appropriate. (use `uncrustify` or your editors built-in tool)

## License
[Apache 2.0](https://opensource.org/licenses/Apache-2.0)
