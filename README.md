# SkaldicSaga Server Installation & Configuration Guide

The SkaldicSagaLog mod automatically intercepts notable or embarrassing multiplayer server events—such as tragic player deaths, destroyed ships, or burnt meals—and broadcasts them to all connected players as Norse poetry directly within the game chat.

This guide details how to install and configure the **SkaldicSaga** mod on an online multiplayer server. Because this mod processes game logic entirely on the server thread, your players can join using completely unmodded vanilla Valheim clients (including Xbox players) and still experience the dynamic poetry in their global chat feed.

---

## 🛠️ Phase 1: Preparing the Server with BepInEx

If your multiplayer server is already running other BepInEx mods, you can skip directly to Phase 2. If your server is completely vanilla, you must install the modding framework first.

1. **Download the Framework:** Go to Thunderstore.io and download the latest stable release of **BepInExPack Valheim**.
2. **Access Your Server Files:** Connect to your server filesystem.
   * *For Rented Server Hosts:* Connect via an FTP client (like FileZilla) using the Host IP, Port, Username, and Password found on your hosting dashboard.
   * *For Self-Hosted Dedicated Servers:* Open the local folder on your machine where your server files reside.
3. **Upload Files:** Extract the contents of the BepInEx download package and upload them directly into your server's **Root Directory** (the main folder containing `valheim_server.exe` or `valheim_server.x86_64`).
4. **Initialize Framework:** Restart your server completely. This initial boot allows BepInEx to map out and automatically generate its internal subdirectories.

---

## 🚀 Phase 2: Installing the SkaldicSaga Mod

Once the BepInEx core framework is successfully active on the server, you can deploy your custom compiled plugin.

1. **Locate Your Assembly:** Navigate to your local project's build directory and grab your compiled **`SkaldicSaga.dll`** file (typically located under `YourProjectFolder/bin/Release/`).
2. **Navigate to Plugins:** Open your server's file management system and route into the newly initialized plugins directory:
   `[Server Root]/BepInEx/plugins/`
3. **Deploy the DLL:** Upload or drop your `SkaldicSaga.dll` file directly into this folder.
4. **Load the Plugin:** Restart your server once more. Watch your server's startup console log to verify initialization. You should see a line confirming execution:
   `[Info : BepInEx] Loading [SkaldicSagaLog 1.0.0]`

---

## 📁 Hosting Environment Navigation Comparison

| Action / File Target | Commercial Rented Host (G-Portal, Nitrado, etc.) | Self-Hosted Local Dedicated Server Box |
| :--- | :--- | :--- |
| **Connection Method** | Secure FTP Client (FileZilla, WinSCP) or Web File Manager | Local Windows File Explorer / Linux Terminal |
| **Root Path Location** | `/` or `/valheim/` (varies slightly by host) | `C:\ValheimServer\` or your custom install folder |
| **Mod Deployment Path** | `/BepInEx/plugins/SkaldicSaga.dll` | `C:\ValheimServer\BepInEx\plugins\SkaldicSaga.dll` |
| **Configuration Path** | `/BepInEx/config/com.user.skaldicsaga.cfg` | `C:\ValheimServer\BepInEx\config\com.user.skaldicsaga.cfg` |
| **Server Reboots** | Executed via the "Restart" button on the web dashboard | Executed by closing and restarting the server `.bat` script |

---

## ⚙️ Phase 3: Anti-Spam & Configuration Management

To ensure your multiplayer chat window remains readable and completely clear of excessive clutter during busy cooperative play sessions, the mod features an integrated dynamic cooldown gate.

1. **Locate Configuration:** After the server completes its first full boot sequence with the mod active, navigate to the config folder:
   `[Server Root]/BepInEx/config/`
2. **Open the Config File:** Open the newly generated file named **`com.user.skaldicsaga.cfg`** using any text editor (or your host's integrated online text editor window).
3. **Modify Cooldown Intervals:** Locate the configuration block governing time thresholds and tweak the value to match your server's needs:

```ini
[General]

## Minimum minutes between saga broadcasts to prevent chat spam.
# Setting type: Int32
# Default value: 15
CooldownMinutes = 15
```

* **Increase the value** (e.g., to `30`) for highly populated servers with many active players to heavily reduce chat messaging frequency.
* **Decrease the value** (e.g., to `5`) for smaller, casual groups playing closely together who want to see poems generate regularly for almost every blunder.
4. **Save and Apply:** Save the text file and restart your server to commit your new cooldown configurations.
