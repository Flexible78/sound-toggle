$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$exe = Join-Path $dist 'SoundToggle.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Run scripts\Build.ps1 first.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$tests = Join-Path $dist 'VerificationTests.exe'
& $compiler /nologo /target:exe /platform:anycpu /main:VerificationTests /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll "/out:$tests" (Join-Path $root 'src\SoundToggle.cs') (Join-Path $root 'tests\VerificationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Regression test compilation failed.' }
& $tests
if ($LASTEXITCODE -ne 0) { throw 'Regression fixtures failed.' }
$assembly = [Reflection.Assembly]::LoadFrom($tests)
$audio = $assembly.GetType('SoundToggleWin11.Audio')
$flowType = $assembly.GetType('SoundToggleWin11.EDataFlow')
$roleType = $assembly.GetType('SoundToggleWin11.ERole')
function Get-RoleSnapshot {
    $values = foreach ($flow in 0,1) { foreach ($role in 0,1,2) {
        try {
            $id = $audio.GetMethod('DefaultIdForRole').Invoke($null, @([Enum]::ToObject($flowType,$flow),[Enum]::ToObject($roleType,$role)))
        } catch {
            $cause = $_.Exception
            while ($cause.InnerException) { $cause = $cause.InnerException }
            if ($cause -is [Runtime.InteropServices.COMException] -and $cause.HResult -eq -2147023728) {
                # E_NOTFOUND is expected on headless runners with no default audio endpoint.
                # Preserve it in the snapshot; other COM failures must still fail the test.
                $id = '<no-default-endpoint>'
            } else { throw }
        }
        '{0}:{1}:{2}' -f $flow,$role,$id
    } }
    return ($values -join "`n")
}
$before = Get-RoleSnapshot
$temp = Join-Path ([IO.Path]::GetTempPath()) ('SoundToggle-Checks-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
foreach ($case in @(@{Name='list';Args='-List -NoUI';Code=0;Text=''}, @{Name='invalid';Args='-DefinitelyInvalid -NoUI';Code=1;Text='ERROR: Unknown argument:'})) {
    $config = Join-Path $temp $case.Name
    $arguments = $case.Args + ' "--config-dir=' + $config + '"'
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Smoke check timed out.' }
    $process.Refresh()
    if ($process.ExitCode -ne $case.Code) { throw ('Unexpected exit code for ' + $case.Name + ': ' + $process.ExitCode) }
    $log = Join-Path $config 'last-run.log'
    if (!(Test-Path -LiteralPath $log)) { throw 'Missing isolated operation log.' }
    if ($case.Text -and !(Select-String -LiteralPath $log -SimpleMatch $case.Text -Quiet)) { throw 'Expected error message missing.' }
    if (Test-Path -LiteralPath (Join-Path $config 'devices.json')) { throw 'Non-switching checks unexpectedly saved device settings.' }
    Write-Output ('PASS: isolated ' + $case.Name + ' process / exit code / log / no device-pair write')
}
if ((Get-RoleSnapshot) -cne $before) { throw 'Playback or capture defaults changed during non-switching checks.' }
Write-Output 'PASS: all six playback/capture default roles unchanged'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $dist 'SoundToggle-Portable.zip'))
try {
    $names = @($archive.Entries | ForEach-Object FullName | Sort-Object)
    if (($names -join '|') -cne 'Install.cmd|README.txt|SoundToggle.exe') { throw 'Unexpected release ZIP contents.' }
    $entry = $archive.GetEntry('SoundToggle.exe')
    $stream = $entry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $zipHash = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','') } finally { $stream.Dispose(); $sha.Dispose() }
    if ($zipHash -cne (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash) { throw 'ZIP executable hash mismatch.' }
} finally { $archive.Dispose() }
Write-Output 'PASS: ZIP allowlist and executable SHA256 match'
Write-Output ('Isolated logs retained locally at: ' + $temp)
