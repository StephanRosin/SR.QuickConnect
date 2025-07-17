# QuickConnect for Valheim

**QuickConnect** is a Valheim mod that adds a **"Join my Server"** button to the game's main menu.  
It allows you to quickly connect to a pre-defined dedicated server without going through the server browser.

---

## ✨ Features

- Adds a new **"Join my Server"** button directly above the "Start Game" button.
- Automatically connects to your configured server.
- DNS resolution support (you can use domain names, not just IPs).
- Simple plain-text config file for server info.

---

## 🛠 Installation

1. Make sure you have [BepInEx 5.x](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) installed.
2. Download the compiled `QuickConnect.dll`.
3. Place the DLL into your `Valheim/BepInEx/plugins/` folder.
4. Launch the game once to generate the config file (see below).

---

## ⚙️ Configuration

After first launch, a config file will be created at:
BepInEx/config/quick_connect_servers.cfg

Edit this file to define one or more servers:
Format: name:ip:port:password

Example:
MyServer:play.example.com:2456:mypassword

- `name`: label for menu entry
- `ip`: IP address or domain name
- `port`: The game's server port (usually `2456`)
- `password`: Optional server password

> ⚠️ Only the **first** entry in the list is currently used!

---

## ✅ How to Use

1. Launch Valheim.
2. In the main menu, click the new **"Join my Server"** button.
3. The mod will try to connect to the first server from your config.
4. If DNS is used, resolution is handled in the background.

---

## 🐛 Troubleshooting

- Make sure the server info in `quick_connect_servers.cfg` is correct.
- If the "Join my Server" button doesn't appear, make sure:
  - The mod DLL is loaded (check your BepInEx log).
  - The scene name is still `"start"` (mod is scene-aware).
- Check `BepInEx/LogOutput.log` for error messages.

---

## 💡 Planned Features

- Support for selecting between multiple servers
- In-game UI for editing servers
- Persistent server passwords via encrypted config

