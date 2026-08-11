param(
    [string]$SpecificationPath = (Join-Path $PSScriptRoot '..\Docs\AI\Maps\official-single-lane.map.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'BoardMapSpecification.Validation.ps1')

function New-SyntheticMapFixture {
    param([string]$MapId)

    return [pscustomobject]@{
        '$schema' = 'guards-digital/map-specification/v1'
        schemaVersion = 1
        mapId = $MapId
        revision = 1
        identity = [pscustomobject]@{
            spaceIdScope = 'map-local'
            zoneIdScope = 'map-local'
            spawnPointIdScope = 'map-local'
            globalSpaceReference = @('mapId', 'spaceId')
            globalZoneReference = @('mapId', 'zoneId')
            globalSpawnPointReference = @('mapId', 'spawnPointId')
            selectedMapReference = @('mapId', 'revision')
            selectionMutability = 'immutable-per-match'
            coordinateQueryScope = 'selected-map-local'
        }
        coordinateSystem = [pscustomobject]@{
            type = 'axial'
            orientation = 'pointy-top'
            directions = @(
                [pscustomobject]@{ dq = 1; dr = 0 },
                [pscustomobject]@{ dq = 1; dr = -1 },
                [pscustomobject]@{ dq = 0; dr = -1 },
                [pscustomobject]@{ dq = -1; dr = 0 },
                [pscustomobject]@{ dq = -1; dr = 1 },
                [pscustomobject]@{ dq = 0; dr = 1 }
            )
        }
        connectionPolicy = [pscustomobject]@{
            kind = 'standard-axial-touching-inventory-v1'
            undirectedPhysicalConnectionCount = 0
            undirectedPlayableConnectionCount = 0
        }
        zones = @([pscustomobject]@{
            zoneId = 'shared-zone'
            kind = 'lane'
            teamId = $null
            isThrone = $false
            isStartingBattleZone = $true
        })
        spaces = @([pscustomobject]@{
            spaceId = 'q0_r0'
            q = 0
            r = 0
            kind = 'playable'
            zoneId = 'shared-zone'
        })
        spawnPoints = @([pscustomobject]@{
            spawnPointId = 'shared-spawn'
            spaceId = 'q0_r0'
            teamId = 'red'
            unitType = 'hero'
        })
    }
}

$resolvedPath = (Resolve-Path -LiteralPath $SpecificationPath).Path
$map = Get-Content -Raw -LiteralPath $resolvedPath | ConvertFrom-Json
$validation = Test-BoardMapSpecificationInvariant -Map $map -Label 'official map specification'

# Source-reviewed official-single-lane evidence begins here. No assertion above this
# boundary may depend on an official map ID, reviewed count, location, or digest.
Assert-MapCondition ($map.mapId -eq 'official-single-lane') 'Unexpected official map ID.'
Assert-MapCondition ($map.revision -eq 1) 'Unexpected official map revision.'

$expectedDirections = @(
    'east|1|0',
    'north-east|1|-1',
    'north-west|0|-1',
    'west|-1|0',
    'south-west|-1|1',
    'south-east|0|1'
)
$actualDirections = @($map.coordinateSystem.directions | ForEach-Object { "$($_.name)|$($_.dq)|$($_.dr)" })
Assert-MapCondition (($actualDirections -join ';') -eq ($expectedDirections -join ';')) 'Official axial direction names/order changed.'

$expectedZones = @{
    'red-throne' = 'throne|red|True|False|19'
    'red-approach' = 'lane|red|False|False|41'
    'starting-battle-zone' = 'lane||False|True|60'
    'blue-approach' = 'lane|blue|False|False|41'
    'blue-throne' = 'throne|blue|True|False|19'
    'top-jungle' = 'jungle||False|False|15'
    'bottom-jungle' = 'jungle||False|False|15'
}
Assert-MapCondition ($validation.Zones.Count -eq $expectedZones.Count) 'Unexpected official zone count.'
foreach ($zone in $validation.Zones) {
    Assert-MapCondition ($expectedZones.ContainsKey($zone.zoneId)) "Unexpected official zone '$($zone.zoneId)'."
    $spaceCount = @($validation.Spaces | Where-Object { $_.zoneId -eq $zone.zoneId }).Count
    $actualMetadata = "$($zone.kind)|$($zone.teamId)|$($zone.isThrone)|$($zone.isStartingBattleZone)|$spaceCount"
    Assert-MapCondition ($actualMetadata -ceq $expectedZones[$zone.zoneId]) "Official zone '$($zone.zoneId)' metadata or membership count changed."
}

Assert-MapCondition ($validation.Spaces.Count -eq 292) 'Expected exactly 292 inventoried cells.'
Assert-MapCondition (@($validation.Spaces | Where-Object { $_.kind -eq 'playable' }).Count -eq 210) 'Expected exactly 210 playable spaces.'
Assert-MapCondition (@($validation.Spaces | Where-Object { $_.kind -eq 'terrain' }).Count -eq 82) 'Expected exactly 82 terrain cells.'
Assert-MapCondition ($validation.PhysicalConnectionCount -eq 809) "Expected 809 undirected physical connections; found $($validation.PhysicalConnectionCount)."
Assert-MapCondition ($validation.PlayableConnectionCount -eq 530) "Expected 530 undirected playable connections; found $($validation.PlayableConnectionCount)."

$expectedSpawnCounts = @{
    'red|hero' = 3
    'red|melee-minion' = 11
    'red|ranged-minion' = 3
    'red|heavy-minion' = 3
    'blue|hero' = 3
    'blue|melee-minion' = 11
    'blue|ranged-minion' = 3
    'blue|heavy-minion' = 3
}
$actualSpawnCounts = @{}
foreach ($spawnPoint in $validation.SpawnPoints) {
    $spawnKey = "$($spawnPoint.teamId)|$($spawnPoint.unitType)"
    Assert-MapCondition ($expectedSpawnCounts.ContainsKey($spawnKey)) "Official spawn point '$($spawnPoint.spawnPointId)' has an unknown team/type combination."
    if (-not $actualSpawnCounts.ContainsKey($spawnKey)) {
        $actualSpawnCounts[$spawnKey] = 0
    }
    $actualSpawnCounts[$spawnKey]++
}
Assert-MapCondition ($validation.SpawnPoints.Count -eq 40) 'Expected exactly 40 official spawn points.'
foreach ($spawnKey in $expectedSpawnCounts.Keys) {
    Assert-MapCondition ($actualSpawnCounts[$spawnKey] -eq $expectedSpawnCounts[$spawnKey]) "Official spawn count '$spawnKey' changed."
}
Assert-MapCondition (@($validation.SpawnPoints | Where-Object { $_.teamId -eq 'red' }).Count -eq 20) 'Red must have exactly 20 spawn points.'
Assert-MapCondition (@($validation.SpawnPoints | Where-Object { $_.teamId -eq 'blue' }).Count -eq 20) 'Blue must have exactly 20 spawn points.'

$canonicalLines = @()
foreach ($zone in $validation.Zones) {
    $canonicalLines += "$($zone.zoneId)|$($zone.kind)|$($zone.teamId)|$($zone.isThrone)|$($zone.isStartingBattleZone)"
}
foreach ($space in $validation.Spaces) {
    $canonicalLines += "$($space.spaceId)|$($space.kind)|$($space.zoneId)"
}
foreach ($spawnPoint in $validation.SpawnPoints) {
    $canonicalLines += "$($spawnPoint.spawnPointId)|$($spawnPoint.spaceId)|$($spawnPoint.teamId)|$($spawnPoint.unitType)"
}
$canonicalBytes = [Text.Encoding]::UTF8.GetBytes(($canonicalLines -join "`n"))
$sha256 = [Security.Cryptography.SHA256]::Create()
try {
    $reviewDigest = (($sha256.ComputeHash($canonicalBytes) | ForEach-Object { $_.ToString('x2') }) -join '')
}
finally {
    $sha256.Dispose()
}
$expectedReviewDigest = '5b4e3a2a36aeabaaa7db73083088174ca60978fd10d5e09c90e6a67b0a1733f0'
Assert-MapCondition ($reviewDigest -eq $expectedReviewDigest) "The reviewed zone/space/spawn inventory digest changed to '$reviewDigest'; repeat source review before accepting it."

Assert-MapCondition ($map.inventoryTotals.cells -eq $validation.Spaces.Count) 'Recorded cell total is stale.'
Assert-MapCondition ($map.inventoryTotals.playableSpaces -eq 210) 'Recorded playable-space total is stale.'
Assert-MapCondition ($map.inventoryTotals.terrainCells -eq 82) 'Recorded terrain-cell total is stale.'
Assert-MapCondition ($map.inventoryTotals.spawnPoints -eq $validation.SpawnPoints.Count) 'Recorded spawn-point total is stale.'

$syntheticAlpha = New-SyntheticMapFixture -MapId 'synthetic-scope-alpha'
$syntheticBeta = New-SyntheticMapFixture -MapId 'synthetic-scope-beta'
[void](Test-BoardMapSpecificationInvariant -Map $syntheticAlpha -Label 'synthetic scope fixture alpha')
[void](Test-BoardMapSpecificationInvariant -Map $syntheticBeta -Label 'synthetic scope fixture beta')
Assert-MapCondition ($syntheticAlpha.spaces[0].spaceId -eq $syntheticBeta.spaces[0].spaceId) 'Synthetic SpaceId reuse fixture is stale.'
Assert-MapCondition ($syntheticAlpha.zones[0].zoneId -eq $syntheticBeta.zones[0].zoneId) 'Synthetic ZoneId reuse fixture is stale.'
Assert-MapCondition ($syntheticAlpha.spawnPoints[0].spawnPointId -eq $syntheticBeta.spawnPoints[0].spawnPointId) 'Synthetic SpawnPointId reuse fixture is stale.'
Assert-MapCondition ("$($syntheticAlpha.mapId)|$($syntheticAlpha.spaces[0].spaceId)" -ne "$($syntheticBeta.mapId)|$($syntheticBeta.spaces[0].spaceId)") 'MapId-composed SpaceId references must remain distinct.'
Assert-MapCondition ("$($syntheticAlpha.mapId)|$($syntheticAlpha.zones[0].zoneId)" -ne "$($syntheticBeta.mapId)|$($syntheticBeta.zones[0].zoneId)") 'MapId-composed ZoneId references must remain distinct.'
Assert-MapCondition ("$($syntheticAlpha.mapId)|$($syntheticAlpha.spawnPoints[0].spawnPointId)" -ne "$($syntheticBeta.mapId)|$($syntheticBeta.spawnPoints[0].spawnPointId)") 'MapId-composed SpawnPointId references must remain distinct.'

Write-Output "PASS: $($map.mapId) revision $($map.revision)"
Write-Output 'Reusable invariants: PASS; synthetic map-local identity isolation: PASS'
Write-Output 'Cells: 292 total, 210 playable, 82 terrain'
Write-Output 'Zones: 7; connections: 809 physical, 530 playable'
Write-Output 'Spawn points: 40 total, 20 Red, 20 Blue'
Write-Output "Reviewed zone/space/spawn inventory digest: $reviewDigest"
