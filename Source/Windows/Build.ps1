param([string]$SdkDirectory='C:\Jago Studio Dev\Suite\shared\WebView2')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
& node (Join-Path $root 'Source\build.cjs')
if($LASTEXITCODE -ne 0){throw 'HTML build failed'}
$sdk=[IO.Path]::GetFullPath($SdkDirectory)
if(-not(Test-Path -LiteralPath (Join-Path $sdk 'lib\net462\Microsoft.Web.WebView2.Core.dll'))){throw 'Shared WebView2 SDK is missing. Specify -SdkDirectory.'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$previewWorker=Join-Path $root 'Jago-Image-Preview.exe'
& $compiler /nologo /target:exe /platform:x64 /optimize+ ('/out:'+$previewWorker) /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll /reference:System.Xaml.dll ('/reference:'+(Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll')) ('/reference:'+(Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll')) (Join-Path $PSScriptRoot 'ImagePreviewWorker.cs')
if($LASTEXITCODE -ne 0){throw 'Image preview helper build failed'}
$argsList=@('/nologo','/target:winexe','/platform:x64','/optimize+','/utf8output',('/out:'+(Join-Path $root 'Jago-Library-Production-Studio.exe')),('/win32icon:'+(Join-Path $PSScriptRoot 'studio.ico')),('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')),'/reference:System.dll','/reference:System.Core.dll','/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll','/reference:System.Web.Extensions.dll','/reference:System.Security.dll','/reference:System.Xml.Linq.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll',('/reference:'+(Join-Path $sdk 'lib\net462\Microsoft.Web.WebView2.Core.dll')),('/reference:'+(Join-Path $sdk 'lib\net462\Microsoft.Web.WebView2.WinForms.dll')))
$resources=@{'index.html'=(Join-Path $root 'index.html');'Microsoft.Web.WebView2.Core.dll'=(Join-Path $sdk 'lib\net462\Microsoft.Web.WebView2.Core.dll');'Microsoft.Web.WebView2.WinForms.dll'=(Join-Path $sdk 'lib\net462\Microsoft.Web.WebView2.WinForms.dll');'WebView2Loader.dll'=(Join-Path $sdk 'runtimes\win-x64\native\WebView2Loader.dll');'WebView2-LICENSE.txt'=(Join-Path $sdk 'LICENSE.txt');'WebView2-NOTICE.txt'=(Join-Path $sdk 'NOTICE.txt')}
$resources['Jago-Image-Preview.exe']=$previewWorker
$argsList+=(Join-Path $PSScriptRoot 'ImagePreviews.cs')
foreach($name in $resources.Keys){$argsList+='/resource:'+$resources[$name]+',World.'+$name}
$argsList+=(Join-Path $PSScriptRoot 'App.cs')
$argsList+=(Join-Path $PSScriptRoot 'JagoSuiteHost.cs')
$argsList+=(Join-Path $PSScriptRoot 'Metadata.cs')
$argsList+=(Join-Path $PSScriptRoot 'Managed.cs')
$argsList+=('/resource:'+(Join-Path $PSScriptRoot 'ImageTextEngine.ps1')+',ImageTextEngine.ps1')
$argsList+=(Join-Path $PSScriptRoot 'OfflineImageText.cs')
$argsList+=(Join-Path $PSScriptRoot 'ImageTextHost.cs')
$argsList+=(Join-Path $PSScriptRoot 'SystemTools.cs')

$argsList+=(Join-Path $PSScriptRoot 'LibraryAudit.cs')
$argsList+=(Join-Path $PSScriptRoot 'PDFConversion.cs')
$argsList+=(Join-Path $PSScriptRoot 'SourceScanner.cs')
$argsList+=(Join-Path $PSScriptRoot 'LinkedSources.cs')
$argsList+=(Join-Path $PSScriptRoot 'CaptureInbox.cs')
$argsList+=(Join-Path $PSScriptRoot 'BoardMode.cs')
$argsList+=(Join-Path $PSScriptRoot 'Documents.cs')
$argsList+=(Join-Path $PSScriptRoot 'OfficeRenderer.cs')
$argsList+=(Join-Path $PSScriptRoot 'ProjectPackages.cs')
$argsList+=(Join-Path $PSScriptRoot 'ProjectReview.cs')
$argsList+=(Join-Path $PSScriptRoot 'UserTemplates.cs')
$argsList+=(Join-Path $PSScriptRoot 'LibraryHome.cs')
& $compiler @argsList
if($LASTEXITCODE -ne 0){throw 'Windows build failed'}
Write-Output 'Library + Production Windows edition built.'

# Short public filename; the development alias keeps older local scripts working.
try { Copy-Item -LiteralPath (Join-Path $root 'Jago-Library-Production-Studio.exe') -Destination (Join-Path $root 'Jago-Library.exe') -Force } catch [System.IO.IOException] { Write-Warning ('The short-name executable is open. The rebuilt development executable is ready; close the app before replacing its running copy. ' + $_.Exception.Message) }

Copy-Item -LiteralPath (Join-Path $root 'Jago-Library-Production-Studio.html') -Destination (Join-Path $root 'Jago-Library.html') -Force

& $compiler /nologo /target:winexe /platform:x64 /optimize+ ('/out:'+(Join-Path $root 'Jago-Board.exe')) ('/win32icon:'+(Join-Path $PSScriptRoot 'board.ico')) /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Xaml.dll ('/reference:'+(Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll')) ('/reference:'+(Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll')) ('/reference:'+(Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll')) (Join-Path $PSScriptRoot 'BoardLauncher.cs') (Join-Path $PSScriptRoot 'BoardShellTasks.cs') /reference:System.Web.Extensions.dll
if($LASTEXITCODE -ne 0){throw 'Board launcher build failed'}
