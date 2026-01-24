# Crestron Master Tool

A modern Windows application for downloading and installing Crestron software and firmware from the official Crestron SFTP server. Built with C# and .NET 9.0.

## Features
- **SFTP Authentication**: Secure login to ftp.crestron.com with your credentials
- **Software & Firmware Browser**: Switch between software and firmware categories
- **Searchable Product List**: Quick filter to find products by name
- **Version Selection**: Click on any product to select from available versions
- **Batch Downloads**: Check multiple products and download them all at once
- **Progress Tracking**: Real-time download progress with speed and ETA
- **Auto-Install**: Silent installation for software packages (admin rights required)
- **Downloads Folder**: All files saved to your Windows Downloads folder

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 9.0 Runtime
- Crestron account credentials

### Running the Application
1. Clone the repository
2. Open `CrestronMasterTool.sln` in Visual Studio 2022 or later
3. Build and run the project (or use `dotnet run`)
4. Enter your Crestron username and password
5. Browse, select, and download products

### Publishing
To create a standalone executable:
```bash
dotnet publish -c Release
```
The executable will be in `bin/Release/net9.0-windows/win-x64/publish/`

## Usage
1. **Login**: Enter your Crestron credentials and click Login
2. **Select Type**: Choose between Software or Firmware
3. **Search**: Use the search box to filter products by name
4. **Select Versions**: Click the version column to choose which version to download
5. **Download**: Check the products you want and click "Download Selected"
6. **Install** (Software only): Click "Install Selected" to silently install downloaded software

## Technical Details
- Built with Windows Forms (.NET 9.0)
- Uses SSH.NET library for SFTP connectivity
- Downloads to `%USERPROFILE%\Downloads`
- Supports cancellation during downloads
- Silent install uses `/SILENT` flags

## Contributing
Contributions are welcome! Feel free to submit issues or pull requests.

## License
This is an open-source tool inspired by the original Crestron Master Installer.

## Links
- GitHub: https://github.com/alastairWH/CrestronMasterTool
- Crestron: https://www.crestron.com
