# LOG SUMMARY Application

A C#/WPF Windows desktop application for sorting and analyzing log files containing account data.

## Features

- **Professional Dark Theme UI** - Modern dark-themed interface matching the original Python application
- **Multi-Folder Support** - Select and process multiple folders at once
- **Comprehensive Analysis** - Categorizes accounts by rare items, VBucks ranges, and skin counts
- **Active/Inactive Tracking** - Tracks and displays active vs inactive accounts
- **Icon Support** - Displays icons for categories (with placeholder fallback)
- **Background Processing** - Non-blocking UI with background thread processing
- **JSON Export** - Outputs summary data in JSON format
- **Sorted Output** - Generates sorted text files for each category

## Requirements

- .NET 7.0 or higher
- Windows OS (WPF application)

## Dependencies

- **Ookii.Dialogs.Wpf** (v5.0.1) - Modern folder browser dialog

## Project Structure

```
LogSummaryApp/
├── App.xaml                    # Application resources and colors
├── App.xaml.cs                 # Application entry point
├── MainWindow.xaml             # Main UI layout
├── MainWindow.xaml.cs          # Application logic
├── Models/
│   ├── AccountData.cs          # Account data model
│   ├── CategoryCount.cs        # Category count model
│   ├── LogSummary.cs           # Summary data model
│   └── TableRowData.cs         # Table row data model
├── skin_icons/                 # Icon directory (optional)
│   ├── vbucks_icon.png         # VBucks range icon
│   ├── skin_range_icon.png     # Skin range icon
│   └── [category icons]        # Individual category icons
└── README.md                   # This file
```

## Building the Application

1. Open the project in Visual Studio 2022 or later
2. Restore NuGet packages (automatic)
3. Build the solution (F6)

Alternatively, using .NET CLI:

```bash
cd LogSummaryApp
dotnet restore
dotnet build
```

## Running the Application

```bash
cd LogSummaryApp
dotnet run
```

Or run the built executable from:
```
LogSummaryApp/bin/Debug/net7.0-windows/LogSummaryApp.exe
```

## Usage

1. **Launch the application** - Window opens maximized
2. **Click "Select Folder(s) and Start Sorting"**
3. **Select a folder** containing log files (skins0to10.txt, skins10to50.txt, or skins50plus.txt)
4. **Choose to add more folders** or start processing
5. **Wait for processing** - Status updates shown
6. **View results** - Table populates with categorized data
7. **Check output** - Files saved to `skins_output/` folder in first selected directory

## Input Files

The application processes these specific log files:
- `skins0to10.txt`
- `skins10to50.txt`
- `skins50plus.txt`

Each file should contain account data with fields:
- Email:Password (login credentials)
- Character: (comma-separated list)
- Pickaxe: (comma-separated list)
- Glider: (comma-separated list)
- Inactive: (true/false)
- VBucks: (numeric value)
- Skins: (numeric count)

## Output

Results are saved to `skins_output/` directory in the first selected folder:

### Text Files
- One `.txt` file per category with matching account logins (sorted alphabetically)
- VBucks ranges: `vbucks_1-50.txt`, `vbucks_51-100.txt`, etc.
- Skin ranges: `skins_6-20.txt`, `skins_20-50.txt`, etc.
- Rare items: `[item_name].txt` (sanitized filename)

### JSON Summary
- `summary.json` - Complete summary with all counts and totals

## Icons

Place icon files in the `skin_icons/` directory:

**Required special icons:**
- `vbucks_icon.png` - Icon for VBucks categories
- `skin_range_icon.png` - Icon for skin range categories

**Category icons:**
Icon filenames should match category names (sanitized):
- Lowercase
- Spaces replaced with underscores
- Special characters removed
- `.png` extension

Example: "Skull Trooper" → `skull_trooper.png`

If an icon is missing, a white star placeholder is displayed.

## Color Scheme

The application uses a professional dark theme:

- **Background**: #1c1c1c (dark gray)
- **Frame**: #2a2a2a (medium gray)
- **Header**: #283593 (dark blue)
- **Row Separator**: #3a3a3a (light gray)
- **Text (default)**: #dcdcdc (very light gray)
- **Text (active)**: #2ecc71 (green)
- **Text (inactive)**: #e74c3c (red)
- **Text (count)**: #f1c40f (yellow/gold)

## Categories

### Rare Skins
Skull Trooper, Wildcat, Black Knight, Galaxy, IKONIK, Sparkle Specialist, World Warrior, Travis Scott, Glow, Stealth Reflex, Double Helix, Blue Squire, The Reaper, Royale Bomber, Psycho Bandit, Elite Agent, Trailblazer, Dark Vertex, Special Forces, Neo Versa, Rogue Spider Knight, Dark Skully, Dark Voyager, Kratos, Omega, Huntmaster Saber, Rose Team Leader, Rogue Agent, Polo Prodigy, Renegade Raider, Ghoul Trooper

### Rare Pickaxes
Merry Mint Axe, Leviathan Axe

### Rare Gliders
Mako

### VBucks Ranges
1-50, 51-100, 100-200, 200-500, 500-2000, 2000-5000, 5000+

### Skin Count Ranges
6-20, 20-50, 50-100, 100-150, 150-200, 200+

## Conversion Notes

This C#/WPF application is a complete rewrite of the original Python/CustomTkinter application. It maintains:
- Identical functionality and logic
- Same visual appearance and color scheme
- Same categorization and processing rules
- Same output format and structure

The title has been changed from the original to "LOG SUMMARY" as requested.

## License

This project is provided as-is for account log analysis purposes.
