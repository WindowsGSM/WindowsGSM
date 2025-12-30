# Copilot AI Directives

## Build Instructions
To build the project successfully, use the legacy MSBuild from the .NET Framework directory. Do not use `dotnet build` or newer MSBuild versions as they may fail with this project structure.

**Command:**
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" WindowsGSM.sln /p:Configuration=Release /p:Platform="Any CPU"
```

## Quality Assurance Instructions
Before considering a task complete, ensure the solution builds cleanly.

1.  **Zero Warnings Policy:** You must resolve all build warnings and errors.
2.  **Verification:** Run the build command and verify the output shows "0 Warning(s)" and "0 Error(s)".

## Versioning Instructions
**CRITICAL:** You MUST increment the version number for EVERY successful build or code change. This is mandatory to ensure the update logic detects the new version.

1.  **File to Edit:** `WindowsGSM/Properties/AssemblyInfo.cs`
2.  **Attributes to Update:**
    *   `[assembly: AssemblyVersion("X.XX.X.X")]`
    *   `[assembly: AssemblyFileVersion("X.XX.X.X")]`
3.  **Rule:** Increment the build number (3rd digit) or revision (4th digit) as appropriate (e.g., `1.23.1.0` -> `1.23.2.0`).
4.  **Timing:** Perform this update *before* running the final verification build.

## Documentation Instructions
Upon completing a significant task or feature implementation, update the patch notes.

1.  **File to Edit:** `PATCHNOTES.md` (in the root directory).
2.  **Action:** Add a brief summary of the changes, fixes, or improvements made under the current date's section. If a section for the current date doesn't exist, create one.

## Knowledge Sharing Instructions
If you discover critical project behaviors, establish new architectural patterns, or make significant changes to workflows that would benefit future AI agents:

1.  **File to Edit:** `.github/copilot-instructions.md` (this file).
2.  **Action:** Add a new section or update existing instructions to capture this knowledge. Ensure it is clear, concise, and actionable for future sessions.
