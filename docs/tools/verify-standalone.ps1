param(
    [Parameter(Mandatory = $true)][string]$PlayerPath,
    [Parameter(Mandatory = $true)][string]$SavePath
)

$ErrorActionPreference = 'Stop'
$playerPath = (Resolve-Path -LiteralPath $PlayerPath).Path
$savePath = (Resolve-Path -LiteralPath $SavePath).Path
$logRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('../../Logs/StandaloneVerification/' + [Guid]::NewGuid().ToString('N'))))
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
$expected = Get-Content -Raw -LiteralPath $savePath | ConvertFrom-Json
if ($expected.version -ne 2) { throw '에너지 검증에는 버전 2의 격리 저장 파일이 필요합니다.' }
$previousPath = $env:MERGE_BOARD_VERIFY_SAVE
try {
    $env:MERGE_BOARD_VERIFY_SAVE = $savePath
    for ($run = 1; $run -le 2; $run++) {
        $logPath = Join-Path $logRoot "run-$run.log"
        $startedAt = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        $player = Start-Process -FilePath $playerPath -ArgumentList @('-batchmode', '-nographics', '-logFile', ('"' + $logPath + '"')) -PassThru -WindowStyle Hidden
        try {
            $deadline = [DateTime]::UtcNow.AddSeconds(40)
            $restoredLine = $null
            while ([DateTime]::UtcNow -lt $deadline -and -not $player.HasExited) {
                if (Test-Path -LiteralPath $logPath) {
                    $restoredLine = Get-Content -LiteralPath $logPath | Where-Object { $_.StartsWith('MVP_RESTORED ') } | Select-Object -First 1
                    if ($restoredLine) { break }
                }
                Start-Sleep -Milliseconds 250
                $player.Refresh()
            }
            if (-not $restoredLine) { throw "시작 상태 기록 누락: $run" }
            $actual = $restoredLine.Substring('MVP_RESTORED '.Length) | ConvertFrom-Json
            if ($actual.version -ne 2 -or $actual.coins -ne $expected.coins -or ($actual.cells -join ',') -ne ($expected.cells -join ',') -or ($actual.completedOrders -join ',') -ne ($expected.completedOrders -join ',') -or ($actual.orderIds -join ',') -ne ($expected.orderIds -join ',')) {
                throw "실행 파일 복원 상태 불일치: $run"
            }
            $observedAt = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
            $minimumEnergy = [Math]::Min(100, $expected.energy + [Math]::Floor([Math]::Max(0, $startedAt - $expected.energyRecoveryAnchorUtcSeconds) / 120))
            $maximumEnergy = [Math]::Min(100, $expected.energy + [Math]::Floor([Math]::Max(0, $observedAt - $expected.energyRecoveryAnchorUtcSeconds) / 120))
            $expectedAnchor = if ($actual.energy -eq 100) { 0 } else { $expected.energyRecoveryAnchorUtcSeconds + ($actual.energy - $expected.energy) * 120 }
            if ($actual.energy -lt $minimumEnergy -or $actual.energy -gt $maximumEnergy -or $actual.energyRecoveryAnchorUtcSeconds -ne $expectedAnchor) {
                throw "오프라인 에너지·남은 회복 시간 불일치: $run"
            }
            $errors = Select-String -LiteralPath $logPath -Pattern 'NullReferenceException|MissingReferenceException|ArgumentException|IndexOutOfRangeException'
            if ($errors) { throw "실행 파일 예외 발생: $run" }
            Write-Output "Windows 재실행 $run PASS: 보드 63칸, 코인 $($actual.coins), 주문, 에너지 $($actual.energy)/100, 회복 기준 복원"
        } finally {
            $player.Refresh()
            if (-not $player.HasExited) {
                if ([System.IO.Path]::GetFullPath($player.Path) -ne $playerPath) { throw '검증 대상 프로세스 경로 불일치' }
                Stop-Process -Id $player.Id
                $player.WaitForExit()
            }
        }
    }
} finally {
    $env:MERGE_BOARD_VERIFY_SAVE = $previousPath
}
