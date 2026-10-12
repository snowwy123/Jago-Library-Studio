# Building Jago Library

Use Windows x64 and Microsoft Edge WebView2 Runtime. Install Node.js for the browser build and the .NET Framework C# compiler for the Windows host.

From this repository, run **Build.ps1** in PowerShell. The wrapper uses the included WebView2 SDK. Edit the files under **Source**, then rebuild; generated HTML is build output.

Keep licence notices with redistributed code and binaries.

The PDF helper has separate build instructions in **Source/PDF-Engine/BUILD.md**. Keep its complete Tools/PDF runtime beside the released app. The helper and bundled components retain their individual licences. Upstream source archives use Git LFS; use a Git client with LFS to publish this repository.
