# Patch Notes - 2025-12-29

## Summary
A major refactoring and stabilization effort was conducted to improve the architecture, performance, security, and build stability of WindowsGSM. The primary focus was decoupling the monolithic `MainWindow` class, optimizing resource usage, securing sensitive data, and resolving numerous compilation and runtime issues to achieve a successful build and launch.

## Architectural Refactoring
### "God Class" Decomposition (MainWindow.xaml.cs)
- **ServerManager Service**: Created a new static `ServerManager` class in `Functions/ServerManager.cs` to centralize server state management and operations.
- **Logic Migration**: Moved core server operations from `MainWindow.xaml.cs` to `ServerManager`:
  - `StartServer`, `StopServer`, `RestartServer`
  - `UpdateServer`, `BackupServer`, `RestoreBackup`, `DeleteServer`
  - Helper logic: `BeginStartServer`, `EndAllRunningProcess`, `IsValidIPAddress`, `IsValidPort`, `OnGameServerExited`.
- **Event-Driven UI**: Implemented `OnServerStatusChanged` and `OnLog` events in `ServerManager`. `MainWindow` now subscribes to these events to update the UI, removing tight coupling.
- **Metadata Extraction**: Extracted `ServerMetadata` class and `ServerStatus` enum into `Functions/ServerMetadata.cs`.
- **DiscordBot Decoupling**:
  - Introduced `Functions/IServerManager.cs` interface.
  - Updated `DiscordBot/Bot.cs` and `Commands.cs` to depend on `IServerManager` rather than the concrete `MainWindow` class.
  - Updated `MainWindow` to implement `IServerManager`.

## Performance Improvements
- **Server Monitoring**: Optimized `Functions/ServerMonitor.cs`. Replaced expensive `Process.GetProcesses()` polling (which enumerates all system processes) with targeted `Process.GetProcessById(pid)` calls.
- **Configuration Caching**: Implemented caching in `DiscordBot/Configs.cs` for frequently accessed values (Prefix, Token, Admin IDs, Channels) to eliminate redundant file I/O on every access.

## Security Enhancements
- **Token Encryption**: Updated `DiscordBot/Configs.cs` to encrypt the Discord Bot Token using Windows DPAPI (`System.Security.Cryptography.ProtectedData`) before saving to disk. Tokens are no longer stored in plain text.
- **Firewall Dependency**: Removed the dependency on `NetFwTypeLib` (COM Object) and `AxImp.exe`. Refactored `WindowsFirewall.cs` to use dynamic type instantiation (`Type.GetTypeFromProgID("HNetCfg.FwPolicy2")`), reducing build requirements and potential security surface.

## Code Quality & Standardization
- **Unified Logging**: Created `Functions/Logger.cs` to provide a standardized logging mechanism.
- **Namespace Wrappers**: Fixed multiple files in `Functions/` that were missing namespace declarations or class wrappers (`ServerRestart.cs`, `ServerStart.cs`, `ServerStop.cs`, `ServerUpdate.cs`, `ServerBackup.cs`, `ServerLog.cs`, `ServerProcess.cs`).
- **Error Handling**: Added `try-catch` blocks with debug logging to `ServerProcess.cs` and `ServerLog.cs` to prevent swallowed exceptions.

## Build & Compilation Fixes
- **Async/Await Fixes**:
  - Fixed incorrect `Task<T>` usage and async signatures in `GlobalServerList.cs`, `ServerTable.cs`, and various GameServer classes.
  - Replaced `.NET Core` specific APIs (like `File.WriteAllTextAsync`, `Process.WaitForExitAsync`) with `.NET Framework 4.7.2` compatible alternatives.
- **GameServer Classes**:
  - Fixed constructor inheritance issues (missing `base(serverData)` calls) in `ARKSE`, `HEAT`, `ECO`, `ONSET`, `ROR2`, `ROK`, `MORDHAU`, `OLOW`.
  - Corrected variable naming mismatches (`serverData` vs `_serverData`) in `SW`, `VTS`, `UNT`, `TF`.
- **Missing Files**: Recreated missing `Images/Row.cs` and `Functions/ServerStatus.cs`.
- **Project File**: Cleaned up `WindowsGSM.csproj`, removed broken COM references, and aligned NuGet package versions.

## Runtime Stability
- **Startup Crash Fix**: Refactored the application entry point in `Program.cs`.
  - Changed `Main` from `async Task<int>` to `static void`.
  - Removed `Task.Run` wrapper around `App.Run()`.
  - **Result**: Fixed the issue where the application process would exit immediately upon launch because the main thread was not correctly blocking on the WPF message loop.

## Code Audit & Optimization
- **Thread Safety**: Implemented locking in `ServerConsole` to prevent race conditions and removed `Dispatcher.Invoke` from `AddOutput` to unblock the UI thread during high-volume logging.
- **UI Performance**: Increased `StartConsoleRefresh` polling interval from 10ms to 250ms to significantly reduce CPU usage.
- **Resource Management**: Added explicit `Process.Dispose()` in `ServerManager.OnGameServerExited` to prevent resource leaks.
- **Project Cleanup**: Removed unused `WindowsGSM.csproj.user` file and resolved multiple assembly binding redirect warnings in `App.config`.
