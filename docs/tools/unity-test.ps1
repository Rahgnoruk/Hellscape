param()
if (-not $env:UNITY_PATH) { 
    throw "Set UNITY_PATH to your Unity.exe" 
}
$ProjectPath = (Resolve-Path "$PSScriptRoot\..").Path
New-Item -ItemType Directory -Force -Path "$ProjectPath\TestResults" | Out-Null
$Results = "$ProjectPath\TestResults\EditMode-$(Get-Date -UFormat %s).xml"
& $env:UNITY_PATH `
  -batchmode -nographics -quit `
  -projectPath "$ProjectPath" `
  -runTests -testPlatform EditMode `
  -testResults "$Results" `
  -logFile -

Write-Host "Unity Edit Mode test results: $Results"