<#
.SYNOPSIS
    Build + test toàn bộ LMHull.sln (dùng cục bộ và trong CI).

.PARAMETER Configuration
    Debug hoặc Release (mặc định Release).

.PARAMETER NoTest
    Chỉ build, bỏ qua bước test.

.EXAMPLE
    ./build.ps1
    ./build.ps1 -Configuration Debug -NoTest
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoTest
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$solution = Join-Path $root 'LMHull.sln'
$artifacts = Join-Path $root 'artifacts'
$testResults = Join-Path $artifacts 'test-results'

function Invoke-Step([string]$name, [scriptblock]$action) {
    Write-Host "==> $name" -ForegroundColor Cyan
    $sw = [Diagnostics.Stopwatch]::StartNew()
    & $action
    if ($LASTEXITCODE -ne 0) { throw "$name thất bại (exit code $LASTEXITCODE)." }
    Write-Host ("    xong sau {0:N1} s" -f $sw.Elapsed.TotalSeconds) -ForegroundColor DarkGray
}

Invoke-Step 'Restore' { dotnet restore $solution --nologo }

# Rhino khóa LMHull.Rhino.rhp khi plugin đang nạp: lần build thường lỗi copy (MSB3027) thì build lại ra artifacts/
Write-Host '==> Build' -ForegroundColor Cyan
$buildLog = dotnet build $solution -c $Configuration --no-restore --nologo 2>&1
$buildLog | Write-Host
if ($LASTEXITCODE -ne 0) {
    if ($buildLog -match 'MSB3027|MSB3021') {
        Write-Host '    File plugin đang bị khóa (Rhino đang mở) — build lại ra artifacts/bin' -ForegroundColor Yellow
        Invoke-Step 'Build (artifacts)' { dotnet build $solution -c $Configuration --no-restore --nologo "-p:OutDir=$artifacts\bin\" }
    }
    else {
        throw 'Build thất bại.'
    }
}

if (-not $NoTest) {
    Invoke-Step 'Test' {
        dotnet test (Join-Path $root 'tests\LMHull.Tests\LMHull.Tests.csproj') -c $Configuration --no-build --nologo `
            --logger "trx;LogFileName=LMHull.Tests.trx" --results-directory $testResults `
            --collect "XPlat Code Coverage"
    }
}

Write-Host ''
Write-Host "LMHull: build $Configuration thành công$(if (-not $NoTest) { ', toàn bộ test pass' })." -ForegroundColor Green
if (-not $NoTest) { Write-Host "Kết quả test: $testResults" -ForegroundColor DarkGray }
