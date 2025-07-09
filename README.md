<div align="center">

# 👻 GhosTTS

**Turn your keyboard into a voice.**  
GhosTTS is a lightweight, open-source text-to-speech deck that speaks
whatever you type – in-game, on Discord, in VRChat, anywhere a microphone
is accepted – while staying quietly hidden in your system tray.

[![Made with .NET](https://img.shields.io/badge/.NET-8.0-purple?logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![License GPL-3](https://img.shields.io/badge/license-GPLv3-blue.svg)](LICENSE)
[![Coqui-TTS](https://img.shields.io/badge/Powered_by-Coqui_TTS-47b881.svg)](https://github.com/coqui-ai/TTS)

</div>

---

## ✨ Features

|            |                                                                    |
|------------|--------------------------------------------------------------------|
| 🎙 **Pre-mapped voices**     | Pre-mapped VCTK speakers. If you use a different model you will need to remap the voices. |
| 😄 **Emotion parser**  | (Model dependent) Auto-detects *happy / sad / angry / confused*  |
| 🖼 **Drag-anywhere overlay** | Toggle with **Ctrl + Shift + T**, transparent & click-through. |
| 🚀 **Real-time chatting** | Voice your sentence while you type – debounce slider included. |
| 🔊 **Virtual-cable routing** | Bundled installer (VB-Cable). Pipes audio to Discord / OBS / games. |
| 🛠 **Open-source C# / WPF** | MIT-friendly code; easy to extend or embed. |

---

## 🖥 Quick start

1. **Download (ZIP):**  
   *Grab the latest `GhosTTS.zip` from the
   [releases](https://github.com/Pandaism/GhosTTS/releases) page.*  
   Unzip anywhere → double-click `GhosTTS.exe`.

2. **First run:**  
   When prompted, click **Yes** to install the bundled  
   **VB-Audio Virtual Cable** (silent, needs admin).

3. **Route audio:**

   | App | Setting |
   |-----|---------|
   | **GhosTTS Output Device** | `CABLE Input (VB-Audio Virtual Cable)` |
   | **Discord / OBS Mic** | `CABLE Output (VB-Audio Virtual Cable)` |

4. **Overlay:** press **Ctrl + Shift + T** → type → **Enter** to speak → **Esc** to hide.

5. **Close-to-tray:** click **✕** – the app hides; right-click the tray icon to exit.

---
1. **Download (Installer):**
   *Grab the latest `GhosTTS.msi` from the
   [releases](https://github.com/Pandaism/GhosTTS/releases) page.*  
---

## 🔧 Build from source

```bash
git clone https://github.com/Pandaism/GhosTTS.git
cd GhosTTS
dotnet build -c Release