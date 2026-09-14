param([Parameter(Mandatory)][int]$AppPid)

$ErrorActionPreference = 'Stop'
$passed = 0
$failed = 0

function Test-UI {
    param([string]$Name, [scriptblock]$Action)
    try {
        & $Action
        if ($LASTEXITCODE -ne 0) { throw "winapp ui exited with $LASTEXITCODE" }
        $script:passed++
        Write-Host "PASS: $Name"
    }
    catch {
        $script:failed++
        Write-Host "FAIL: $Name - $_" -ForegroundColor Red
    }
}

Test-UI 'App settings button exists' {
    winapp ui wait-for 'AppSettingsButton' -a $AppPid -t 3000
}
Test-UI 'Open app settings dialog' {
    winapp ui invoke 'AppSettingsButton' -a $AppPid
    winapp ui wait-for 'AppSettingsDialog' -a $AppPid -t 3000
}
Test-UI 'Dependency status is visible' {
    winapp ui wait-for 'AppDependencyStatus' -a $AppPid -t 3000
}
Test-UI 'Reset action is available' {
    winapp ui wait-for 'ResetSettingsButton' -a $AppPid -t 3000
}

New-Item -ItemType Directory -Force -Path '.\test-artifacts' | Out-Null
winapp ui screenshot -a $AppPid -o '.\test-artifacts\app-settings-dialog.png' 2>$null
Test-UI 'Reset requires confirmation' {
    winapp ui invoke 'ResetSettingsButton' -a $AppPid
    winapp ui wait-for 'ConfirmResetSettingsDialog' -a $AppPid -t 3000
}
Test-UI 'Reset confirmation can be cancelled' {
    winapp ui send-keys 'escape' -a $AppPid --via send-input
    winapp ui wait-for 'ConfirmResetSettingsDialog' -a $AppPid --gone -t 3000
}

Write-Host "Passed: $passed | Failed: $failed"
if ($failed -gt 0) { exit 1 }
