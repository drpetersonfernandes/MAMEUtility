# MAME Utility

## Overview

MAME Utility is a tool designed to help manage and organize MAME (Multiple Arcade Machine Emulator) data. It provides functionalities to create lists based on various criteria, merge lists, and copy ROMs and images.

![Screenshot](screenshot1.png)

## Features

-   **Create MAME Lists**: Generate XML lists based on:
    -   Full driver information
    -   Manufacturer
    -   Year
    -   Source file
    -   Software list
-   **Merge Lists**: Combine multiple XML lists into a single XML and DAT file.  The DAT file is in MessagePack format, compatible with SimpleLauncher's MameConfig.
-   **Copy ROMs**: Copy ROM files (ZIP) from a source directory to a destination directory based on an XML list.
-   **Copy Images**: Copy image files (PNG, JPG, JPEG) from a source directory to a destination directory based on an XML list.
-   **Logging**: Provides a log window to display the progress and any errors encountered during operations.
-   **Progress Tracking**: A progress bar provides visual feedback on long-running operations.

## Usage

1.  **Create Lists**:
    -   Select the desired list type from the main window (e.g., "Create MAME Manufacturer List").
    -   Choose the MAME full driver information XML file as input. You can download this from the MAME website.
    -   Specify the output folder or file path for the generated list.
2.  **Merge Lists**:
    -   Click the "Merge Lists" button.
    -   Select multiple XML files to merge. The application supports both "Machines" and "Softwares" XML formats.
    -   Choose the output file path for the merged XML and DAT files.
3.  **Copy ROMs/Images**:
    -   Click the "Copy Roms" or "Copy Images" button.
    -   Select the source directory containing the ROMs or images.
    -   Select the destination directory to copy the files to.
    -   Choose the XML file(s) containing ROM or image information.

### Prerequisites

- Windows 7 or later.
- .NET 9.0 Runtime.
- MAME ROMs and images you wish to manage.
- The MAME full driver information in XML format (available from the official [MAME](https://www.mamedev.org/release.html) website).

## License

This project is licensed under the terms of the [GNU General Public License v3.0](LICENSE.txt). See `LICENSE.txt` for more information.

## Support

If you like the software, please give us a star.  Consider [donating](https://www.purelogiccode.com/donate) to support the project or simply to express your gratitude!

## Developer

- **Peterson Fernandes** – [Github Profile](https://github.com/drpetersonfernandes)