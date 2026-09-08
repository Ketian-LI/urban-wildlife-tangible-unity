$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$pythonPath = Join-Path $projectRoot ".venv\Scripts\python.exe"
$env:PYTHONUTF8 = "1"

Set-Location -LiteralPath $projectRoot

if (-not (Test-Path -LiteralPath $pythonPath)) {
    py -3.12 -m venv .venv
}

& $pythonPath -m pip install --disable-pip-version-check -r requirements.txt
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $pythonPath vision\environment_check.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $pythonPath vision\generate_calibration_markers.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $pythonPath -m unittest discover -s tests -p "test_*.py" -v
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Vision setup complete. Connect a camera, then run:"
Write-Host ".\.venv\Scripts\python.exe vision\camera_probe.py --list"
