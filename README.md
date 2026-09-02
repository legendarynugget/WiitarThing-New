Here is the rewritten `README.md` reflecting all the new features, UI modernizations, pairing improvements, notification controls, and architectural fixes.

---

# WiitarThing New

A lightweight, modernized Windows application that seamlessly connects Wii Guitar Hero instruments, drums, turntables, and classic controllers to your PC wirelessly as virtual Xbox 360 controllers via Bluetooth.

---

## Table of Contents

- [What's New in This Version](#whats-new-in-this-version)
- [Setup](#setup)
  - [Prerequisites & Installation](#prerequisites--installation)
  - [Connecting Your Controller](#connecting-your-controller)
  - [Calibrating Guitars](#calibrating-guitars)
- [Features](#features)
  - [Supported Extensions](#supported-extensions)
  - [One-Click Smart Connect](#one-click-smart-connect)
  - [Live Battery Level & Indicator](#live-battery-level--indicator)
  - [Notification Settings & Throttling](#notification-settings--throttling)
  - [System Tray & Quick Access](#system-tray--quick-access)
  - [Guitar Touch Bar](#guitar-touch-bar)
- [Troubleshooting](#troubleshooting)
  - ["My Wiimote is stuck flashing LEDs or sleeping"](#my-wiimote-is-stuck-flashing-leds-or-sleeping)
  - ["My guitar is moving my desktop mouse cursor"](#my-guitar-is-moving-my-desktop-mouse-cursor)
  - ["How do I completely unpair all old Wiimotes?"](#how-do-i-completely-unpair-all-old-wiimotes)
- [Other Setup Scenarios](#other-setup-scenarios)
  - [Using a Mayflash DolphinBar](#using-a-mayflash-dolphinbar)
  - [Using with Native Guitar Hero PC Ports](#using-with-native-guitar-hero-pc-ports)
- [Credits](#credits)

---

## What's New in This Version

- **Modernized Dark Theme**: Clean, responsive layout with high-contrast elements, vector UI icons, and proper label wrapping.
- **Instant Extension Detection**: WiitarThing immediately queries the Wiimote extension port upon connection, instantly recognizing your Guitar, Drums, or Classic Controller without requiring manual button presses.
- **Smart One-Click Connect**: Pressing **CONNECT** automatically claims the first available Xbox 360 controller slot (Slots 1–4). Right-click or use the submenu if you wish to target a specific player slot manually.
- **Zero-Drop Bluetooth Pairing Engine**: Completely rewritten pairing pipeline that eliminates Windows 10/11 ghost registry device lockouts, avoids premature sync timeouts, and automatically closes the sync window upon successful confirmation.
- **Live Battery Reporting**: Real-time battery indicator with visual percentage display and dynamic color coding (Green, Amber, Red).
- **Customizable Notifications**: Notification controls directly in the Settings menu (toggle low-battery alerts, disconnect toasts, or disable notifications entirely).
- **Tray Double-Click Restoration**: Double-click the system tray icon anytime to bring the main window back into focus immediately.

---

## Setup

### Prerequisites & Installation

1. Download and install the [ViGEmBus Driver](https://github.com/ViGEm/ViGEmBus/releases).
2. Download the latest **WiitarThing** release and extract the folder anywhere on your PC.
3. Launch `WiitarThing.exe`.

### Connecting Your Controller

1. Click **⚡ SYNC NEW CONTROLLER** on the top toolbar.
2. Press the **Red SYNC Button** inside the Wiimote battery compartment (or hold **1 + 2**).
3. The sync window will perform the PIN handshake, bind the Windows HID service, and automatically close once your controller is ready.
4. Your controller will appear in the **AVAILABLE DEVICES** pane. Click **CONNECT** to instantly route it as Player 1 (or the next available Xbox 360 controller slot).
5. The player LED on your Wiimote will turn solid blue to match your assigned player index.

> **Note on Subsequent Reconnects:** Once paired, you do not need to open the Sync menu again. Simply tap any button on your controller, open WiitarThing, and click **CONNECT**.

### Calibrating Guitars

Calibration can be completed at any time without entering a separate configuration screen:

1. Lay the guitar flat with the frets facing up and the neck pointing left, then press the `1` button on the Wiimote.
2. Stand the guitar upright with the neck pointing directly toward the ceiling, then press the `2` button on the Wiimote.
3. Push your whammy bar down and release it a few times across its full range of motion.
4. Rotate the joystick in complete 360° circles.

---

## Features

### Supported Extensions

- **Guitar Hero Guitars** (Les Paul, World Tour, GH5, Kramer, etc.)
- **Guitar Hero Drum Kits**
- **DJ Hero Turntables**
- **Classic Controllers & Classic Controller Pros**
- **Nunchuks**
- **Wii U Pro Controllers** (native Bluetooth sync supported)
- **Standalone Wiimotes**

### One-Click Smart Connect

Clicking **CONNECT** automatically finds the lowest unoccupied Xbox 360 player slot (Players 1 through 4) without forcing you to pick from a menu. If you need a specific player slot for multi-player setups, you can still right-click the button or use the option menu.

### Live Battery Level & Indicator

Each device card features a live battery level with color-coded percentage status:
- **Green**: Good battery life ($>50\%$)
- **Amber**: Moderate battery level ($25\% - 50\%$)
- **Red**: Low battery level ($\le 20\%$)

### Notification Settings & Throttling

You can control Windows balloon and toast alerts under **⚙ Settings > Notifications**:
- **Enable All Notifications**: Master switch for Windows pop-up notifications.
- **Low Battery Warnings**: Alerts you when your controller battery drops below $20\%$ (includes built-in rate throttling so it never spams you during gameplay).
- **Controller Disconnected Alerts**: Alerts you if an active controller unexpectedly drops out.

### System Tray & Quick Access

- Minimizing the application hides it to the Windows Taskbar notification area (System Tray).
- **Double-click** the tray icon at any time to un-minimize and bring the window back into focus.

### Guitar Touch Bar

On World Tour and GH5 guitars, the touch bar can be toggled on/off by pressing the `+` and `-` buttons on the Wiimote. When enabled, the touch bar registers as standard fret inputs for slider notes.

---

## Troubleshooting

### "My Wiimote is stuck flashing LEDs or sleeping"
- Ensure **HID Wiimote** or legacy third-party virtual drivers are completely removed, as they block WiitarThing from accessing the HID interface.
- Make sure you have the official **ViGEmBus driver** installed.
- Click **❌ Unpair All Wiimotes** in the toolbar, wait for the purge to complete, and then pair via the **⚡ SYNC NEW CONTROLLER** button.

### "My guitar is moving my desktop mouse cursor"
- This is caused by Steam's desktop gamepad configuration.
- Open **Steam > Settings > Controller**, and under **Desktop Layout**, disable or remove the joystick-to-mouse mapping.

### "How do I completely unpair all old Wiimotes?"
- Click **❌ Unpair All Wiimotes** on the top toolbar.
- This purges stale Bluetooth registry device entries to clear phantom or dead devices.

---

## Other Setup Scenarios

### Using a Mayflash DolphinBar
1. Switch the DolphinBar to **Mode 4**.
2. Sync your Wiimote directly to the DolphinBar hardware.
3. Open WiitarThing, click the **ID** button on the detected entries until your guitar vibrates, and click **CONNECT**.

### Using with Native Guitar Hero PC Ports
WiitarThing creates a standard virtual **Xbox 360 Gamepad** (XInput) for maximum compatibility across games like *Clone Hero*, *YARG*, *RPCS3*, and *Dolphin*. 

To use your controller with native legacy PC ports (such as *Guitar Hero III PC* or *Aerosmith PC*) that specifically look for Xbox 360 guitar VID/PIDs, combine WiitarThing with [xinputemu](https://github.com/sanjay900/xinputemu).

---

## Credits

- **Justin Keys (KeyPuncher)**: Original creator of [WiinUSoft / WiinUPro](https://github.com/KeyPuncher/WiinUPro) architecture and Nintroller library.
- **Meowmaritus**: Guitar Hero extension support and initial WiitarThing implementation.
- **shockdude**: DJ Hero Turntable extension support.
- **MWisBest**: Original ViGEmBus integration.
- **Aida-Enna**: ViGEmBus updates and builds (2020–2023).
- **TheNathannator & Contributors**: Maintenance
