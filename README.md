# 🕹️ MAME Utility

**A modern, high-performance Windows application for managing MAME ROM collections**

[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%20x64%20%7C%20ARM64-0078d7.svg?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows)
[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512bd4.svg?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg?style=for-the-badge)](LICENSE.txt)
[![GitHub release](https://img.shields.io/github/v/release/drpetersonfernandes/MAMEUtility?style=for-the-badge&logo=github)](https://github.com/drpetersonfernandes/MAMEUtility/releases)

[📥 Download Latest](https://github.com/drpetersonfernandes/MAMEUtility/releases) • [🐛 Report Bug](https://github.com/drpetersonfernandes/MAMEUtility/issues) • [⭐ Star This Repo](https://github.com/drpetersonfernandes/MAMEUtility)

---

## 🎯 Overview

**MAME Utility** is a powerful Windows desktop application designed to help retro gaming enthusiasts and MAME collectors efficiently manage, organize, and filter their MAME (Multiple Arcade Machine Emulator) collections. Built with modern .NET 10 and WPF, it offers a sleek, responsive interface for processing massive XML databases with ease.

Whether you're curating a custom arcade cabinet setup, organizing ROMs by manufacturer or year, or merging multiple game lists, MAME Utility provides the tools you need with exceptional performance.

---

## ✨ Features

### 📊 List Generation & Organization

| Feature | Description |
|---------|-------------|
| **Full Driver List** | Generate comprehensive name/description mappings from MAME XML |
| **By Manufacturer** | Automatically separate ROMs by company (e.g., `Capcom.xml`, `Nintendo.xml`, `Sega.xml`) |
| **By Year** | Organize games chronologically with 4-digit year validation (1970-2099) |
| **By Source File** | Group machines by their MAME driver source code file |
| **Software Lists** | Process and consolidate MAME software list XML directories |

### 🔧 Data Processing

| Feature | Description |
|---------|-------------|
| **Parallel Processing** | Multi-core CPU utilization for lightning-fast XML parsing |
| **Smart Merging** | Combine multiple XML lists (Machine & Software formats) with duplicate removal |
| **SimpleLauncher Integration** | Auto-generates `.dat` files in MessagePack format for [SimpleLauncher](https://github.com/drpetersonfernandes/SimpleLauncher) compatibility |
| **Streaming I/O** | Handles multi-gigabyte XML files without memory exhaustion |

### 📁 Collection Management

| Feature | Description |
|---------|-------------|
| **Batch ROM Copying** | Copy ZIP files based on filtered XML game lists |
| **Image Synchronization** | Sync PNG/JPG/JPEG artwork based on XML entries |
| **Progress Tracking** | Real-time progress bars with percentage and elapsed time |
| **Cancellation Support** | Safely interrupt long-running operations at any time |

### 🖥️ User Experience

| Feature | Description |
|---------|-------------|
| **Modern WPF Interface** | Clean, intuitive Windows desktop application |
| **Integrated Logging** | Built-in log window for debugging and operation history |
| **Automatic Updates** | Version checker notifies of new releases |
| **Bug Reporting** | Optional automated crash reporting for stability improvements |

---

## 📸 Screenshots

[Screenshot 1](screenshot1.png)
[Screenshot 2](screenshot2.png)
[Screenshot 3](screenshot3.png)

---

## 🚀 Getting Started

### Prerequisites

- **Operating System**: Windows 10 or later (Windows 7/8.1 supported with .NET 10 runtime)
- **Runtime**: [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64 or ARM64)
- **Input Data**: MAME `listxml` output file (generate with: `mame -listxml > mame.xml`)

### Installation

1. Go to the [Releases](https://github.com/drpetersonfernandes/MAMEUtility/releases) page
2. Download the latest `MAMEUtility.zip` for your architecture
3. Extract to your preferred location
4. Run `MAMEUtility.exe`

## 📖 Usage Guide

### 1. Generating MAME Lists

1. Launch MAME Utility
2. Select your desired list type:
   - **Create MAME Full List** - Complete simplified mapping
   - **Create MAME Manufacturer List** - Separate files per manufacturer
   - **Create MAME Year List** - Organized by release year
   - **Create MAME Source File List** - Grouped by driver source
   - **Create MAME Software List** - Process software list XMLs
3. Select your MAME `listxml` file
4. Choose the output directory
5. Click **Process** and wait for completion

### 2. Merging Lists

1. Click **Merge Lists**
2. Select multiple XML files to merge
3. Choose output location
4. The utility generates:
   - A merged `.xml` file (deduplicated)
   - A `.dat` file in MessagePack format (for SimpleLauncher)

### 3. Copying ROMs

1. Click **Copy ROMs**
2. Select your source ROM directory (containing `.zip` files)
3. Select destination folder
4. Choose the XML list(s) defining which games to copy
5. The utility copies only matching ROM files

### 4. Copying Images

1. Click **Copy Images**
2. Select your source images directory
3. Select destination folder
4. Choose the XML list(s) for filtering
5. Supports: `.png`, `.jpg`, `.jpeg`

---

## 🏗️ Architecture

### Technology Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 10 (WPF) |
| **Language** | C# 14 |
| **UI Framework** | Windows Presentation Foundation (WPF) |
| **Serialization** | [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) |
| **Architecture Pattern** | Service-based with Service Locator |

### Project Structure

```
MAMEUtility/
├── Services/              # Business logic services
│   ├── MameProcessingService.cs
│   ├── LogService.cs
│   ├── DialogService.cs
│   ├── GitHubVersionService.cs
│   └── BugReportService.cs
├── Interfaces/            # Service contracts
├── Models/                # Data models
├── Converters/            # WPF value converters
├── CopyRoms.cs           # ROM batch operations
├── CopyImages.cs         # Image batch operations
├── MergeList.cs          # XML merging logic
├── MAMEFull.cs           # Full list generation
├── MAMEManufacturer.cs   # Manufacturer filtering
├── MAMEYear.cs           # Year-based filtering
├── MAMESourcefile.cs     # Source file filtering
└── MAMESoftwareList.cs   # Software list processing
```

---

## 📄 License

This project is licensed under the **GNU General Public License v3.0**.

See [LICENSE.txt](LICENSE.txt) for the full license text.

---

## 💖 Support

If you find MAME Utility helpful, please consider:

- ⭐ **Star this repository** on GitHub
- 💰 **[Donate](https://www.purelogiccode.com/donate)** to support ongoing development

---

Made with ❤️ by [PureLogicCode](https://www.purelogiccode.com)