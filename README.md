# Godot 4 DeployToSteamOS
A Godot 4 addon to deploy your game to your SteamOS devkit devices with one click

## Setup
**Due to external dependencies, this addon is only compatible with Godot 4 C# for the time being**

- Install the following NuGet packages
  - Newtonsoft.Json (Version 13.0.3)
  - SSH.NET (Version 2023.0.0)
  - Zeroconf (Version 3.6.11)
- Add the `deploy_to_steamos` folder to your project
- Build your project
- Enable the addon and define a build path in the `Deploy to SteamOS` project settings panel
- Create a new `Linux/X11` export preset and name it `Steamdeck`
  - **Optional:** You can exclude this addon in your export by selecting `Export all resources in the project except resources checked below` under `Resources` -> `Export Mode` and checking the `deploy_to_steamos` folder
- **Optional:** You can add the created `.deploy_to_steamos` folder to your `.gitignore` if you do not want to commit changes to the settings to your git repo

## Usage
After the initial setup, you can open the dropdown on the top right and click on `Add devkit device` to pair with a new SteamOS devkit device.

**Please keep in mind that you need to connect to the device at least once before you can pair it**

## FAQ
### Why do I need to connect through the official SteamOS devkit client once?
Only the official SteamOS devkit client can establish the initial connection, install the devkit tools on your devkit device and create the necessary connection keys for the time being.

### Why is this addon only compatible with the C# build of Godot?
This project uses external libraries for establishing SSH connections and scanning for devices on your network that aren't accessible through GDScript only.

## Donations
If you would like to support my open source projects, feel free to [drop me a coffee (or rather, an energy drink)](https://ko-fi.com/laurasofiaheimann) or check out my [release games](https://indiegesindel.itch.io) on itch.

## License
This project is licensed under the (MIT License)[LICENSE].