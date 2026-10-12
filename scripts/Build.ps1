$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework 4.x C# compiler is required.' }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$exe = Join-Path $dist 'SoundToggle.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll "/out:$exe" (Join-Path $root 'src\SoundToggle.cs')
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
$package = Join-Path $dist 'portable'
New-Item -ItemType Directory -Path $package -Force | Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $package 'SoundToggle.exe') -Force
Copy-Item -LiteralPath (Join-Path $root 'packaging\Install.cmd') -Destination (Join-Path $package 'Install.cmd') -Force
Copy-Item -LiteralPath (Join-Path $root 'packaging\README.txt') -Destination (Join-Path $package 'README.txt') -Force
$zip = Join-Path $dist 'SoundToggle-Portable.zip'
Compress-Archive -LiteralPath @((Join-Path $package 'SoundToggle.exe'), (Join-Path $package 'Install.cmd'), (Join-Path $package 'README.txt')) -DestinationPath $zip -Force
$checksums = @($exe, $zip) | ForEach-Object { $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256; '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), (Split-Path -Leaf $_) }
[IO.File]::WriteAllLines((Join-Path $dist 'SHA256SUMS.txt'), [string[]]$checksums, [Text.UTF8Encoding]::new($false))
Write-Output 'PASS: built GUI executable, three-file portable ZIP and SHA256SUMS.txt'
