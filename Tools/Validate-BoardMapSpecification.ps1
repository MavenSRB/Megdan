param(
    [string]$SpecificationPath = ([System.IO.Path]::Combine(
        $PSScriptRoot, '..', 'Docs', 'AI', 'Maps', 'official-single-lane.map.json'))
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
        orderedLaneZoneIds = @()
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

$expectedZoneAdjacency = @(
    'blue-approach|blue-throne',
    'blue-approach|starting-battle-zone',
    'blue-throne|bottom-jungle',
    'bottom-jungle|starting-battle-zone',
    'red-approach|red-throne',
    'red-approach|starting-battle-zone',
    'red-throne|top-jungle',
    'starting-battle-zone|top-jungle'
)
Assert-MapCondition (($validation.ZoneAdjacencyKeys -join ';') -eq ($expectedZoneAdjacency -join ';')) 'Official zone adjacency changed.'
$expectedOrderedLane = @('red-throne', 'red-approach', 'starting-battle-zone', 'blue-approach', 'blue-throne')
Assert-MapCondition (($validation.OrderedLaneZoneIds -join ';') -eq ($expectedOrderedLane -join ';')) 'Official ordered Lane changed.'

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
$canonicalLines += "ordered-lane|$($validation.OrderedLaneZoneIds -join '|')"
foreach ($adjacencyKey in $validation.ZoneAdjacencyKeys) {
    $canonicalLines += "zone-adjacency|$adjacencyKey"
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
$expectedReviewDigest = '185b600d381d05953f3896999866cff4b48ab6afe212994606bb2b7a3f1b9378'
Assert-MapCondition ($reviewDigest -eq $expectedReviewDigest) "The reviewed zone/space/spawn/Lane inventory digest changed to '$reviewDigest'; repeat source review before accepting it."

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

# The runtime resource is generated from this specification, never edited alongside it. Unity cannot
# load the Docs path, so a byte-identical copy lives under Assets/Resources; before milestone 6.1d
# nothing compared them and a divergence would have been silent - this validator would keep passing
# against the specification while the game loaded something else.
$resourcePath = [System.IO.Path]::Combine(
    $PSScriptRoot, '..', 'Assets', 'Resources', 'Maps', 'official-single-lane.json')
Assert-MapCondition (Test-Path -LiteralPath $resourcePath -PathType Leaf) `
    "The generated runtime board-map resource is missing. Run Tools/Sync-BoardMapResource.ps1."

$specificationBytes = [System.IO.File]::ReadAllBytes($SpecificationPath)
$resourceBytes = [System.IO.File]::ReadAllBytes($resourcePath)
$resourceMatchesSpecification = $resourceBytes.Length -eq $specificationBytes.Length -and
    [System.Linq.Enumerable]::SequenceEqual([byte[]] $resourceBytes, [byte[]] $specificationBytes)
Assert-MapCondition $resourceMatchesSpecification (
    "The runtime board-map resource has drifted from this specification " +
    "($($resourceBytes.Length) bytes against $($specificationBytes.Length)). " +
    'Run Tools/Sync-BoardMapResource.ps1 to regenerate it from the reviewed source.')

Write-Output "PASS: $($map.mapId) revision $($map.revision)"
Write-Output "Runtime resource byte-identical to specification: PASS ($($specificationBytes.Length) bytes)"
Write-Output 'Reusable invariants: PASS; synthetic map-local identity isolation: PASS'
Write-Output 'Cells: 292 total, 210 playable, 82 terrain'
Write-Output 'Zones: 7; connections: 809 physical, 530 playable'
Write-Output 'Spawn points: 40 total, 20 Red, 20 Blue'
Write-Output "Reviewed zone/space/spawn/Lane inventory digest: $reviewDigest"

# The alternate map has its own reviewed counts; official-map evidence above stays unchanged.
$alternatePath = Join-Path $PSScriptRoot '../Docs/AI/Maps/alternate-small-single-lane.map.json'
$alternate = Get-Content -Raw -LiteralPath $alternatePath | ConvertFrom-Json
$alternateValidation = Test-BoardMapSpecificationInvariant -Map $alternate -Label 'alternate small map'
if ($alternate.spaces.Count -ne 258 -or @($alternate.spaces | Where-Object kind -eq 'playable').Count -ne 156 -or $alternate.spawnPoints.Count -ne 40) {
    throw 'Alternate map inventory differs from the reviewed 258/156/40 cells/playable/spawns.'
}
$coordinates = @{}
foreach ($space in $alternate.spaces) { $coordinates["$($space.q),$($space.r)"] = $true }
foreach ($space in @($alternate.spaces | Where-Object kind -eq 'playable')) {
    foreach ($offset in @(@(1,0),@(1,-1),@(0,-1),@(-1,0),@(-1,1),@(0,1))) {
        $key = "$($space.q + $offset[0]),$($space.r + $offset[1])"
        if (-not $coordinates.ContainsKey($key)) { throw "Missing alternate map border cell $key" }
    }
}
& (Join-Path $PSScriptRoot 'Sync-BoardMapResource.ps1') -Check
Write-Output 'PASS: alternate small map; 258 cells, 156 playable, 40 spawns, complete terrain border and resource parity.'
