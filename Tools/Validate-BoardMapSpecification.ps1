param(
    [string]$SpecificationPath = (Join-Path $PSScriptRoot '..\Docs\AI\Maps\official-single-lane.map.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-MapCondition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-CoordinateKey {
    param([int]$Q, [int]$R)
    return "$Q,$R"
}

$resolvedPath = (Resolve-Path -LiteralPath $SpecificationPath).Path
$map = Get-Content -Raw -LiteralPath $resolvedPath | ConvertFrom-Json

Assert-MapCondition ($map.'$schema' -eq 'guards-digital/map-specification/v1') 'Unexpected map schema.'
Assert-MapCondition ($map.schemaVersion -eq 1) 'Unexpected schema version.'
Assert-MapCondition ($map.mapId -eq 'official-single-lane') 'Unexpected map ID.'
Assert-MapCondition ($map.revision -eq 1) 'Unexpected map revision.'
Assert-MapCondition ($map.identity.spaceIdScope -eq 'map-local') 'Space IDs must remain map-local.'
Assert-MapCondition ($map.identity.globalSpaceReference.Count -eq 2) 'Global space references must compose mapId and spaceId.'
Assert-MapCondition ($map.identity.globalSpaceReference[0] -eq 'mapId') 'The first global space reference component must be mapId.'
Assert-MapCondition ($map.identity.globalSpaceReference[1] -eq 'spaceId') 'The second global space reference component must be spaceId.'
Assert-MapCondition ($map.coordinateSystem.type -eq 'axial') 'The coordinate system must remain axial.'
Assert-MapCondition ($map.coordinateSystem.orientation -eq 'pointy-top') 'The map orientation must remain pointy-top.'
Assert-MapCondition ($map.connectionPolicy.kind -eq 'standard-axial-touching-inventory-v1') 'Unexpected connection policy.'

$expectedDirections = @(
    'east|1|0',
    'north-east|1|-1',
    'north-west|0|-1',
    'west|-1|0',
    'south-west|-1|1',
    'south-east|0|1'
)
$actualDirections = @($map.coordinateSystem.directions | ForEach-Object { "$($_.name)|$($_.dq)|$($_.dr)" })
Assert-MapCondition (($actualDirections -join ';') -eq ($expectedDirections -join ';')) 'Axial directions do not match the project contract.'

$expectedZones = @{
    'red-throne' = 19
    'red-approach' = 41
    'starting-battle-zone' = 60
    'blue-approach' = 41
    'blue-throne' = 19
    'top-jungle' = 15
    'bottom-jungle' = 15
}
$zoneIds = @($map.zones | ForEach-Object { $_.zoneId })
Assert-MapCondition ($zoneIds.Count -eq $expectedZones.Count) 'Unexpected zone count.'
Assert-MapCondition (@($zoneIds | Select-Object -Unique).Count -eq $zoneIds.Count) 'Zone IDs must be unique.'
foreach ($zoneId in $expectedZones.Keys) {
    Assert-MapCondition ($zoneIds -contains $zoneId) "Missing zone '$zoneId'."
}
Assert-MapCondition (@($map.zones | Where-Object { $_.kind -eq 'throne' }).Count -eq 2) 'The map must contain two Throne zones.'
Assert-MapCondition (@($map.zones | Where-Object { $_.isStartingBattleZone }).Count -eq 1) 'Exactly one zone must be the starting Battle Zone.'
Assert-MapCondition (($map.zones | Where-Object { $_.isStartingBattleZone }).zoneId -eq 'starting-battle-zone') 'The wrong zone is marked as the starting Battle Zone.'

$spaces = @($map.spaces)
Assert-MapCondition ($spaces.Count -eq 292) 'Expected exactly 292 inventoried cells.'
Assert-MapCondition (@($spaces | Where-Object { $_.kind -eq 'playable' }).Count -eq 210) 'Expected exactly 210 playable spaces.'
Assert-MapCondition (@($spaces | Where-Object { $_.kind -eq 'terrain' }).Count -eq 82) 'Expected exactly 82 terrain cells.'
Assert-MapCondition (@($spaces.spaceId | Select-Object -Unique).Count -eq $spaces.Count) 'Space IDs must be unique within the map.'

$spacesByCoordinate = @{}
$spacesById = @{}
foreach ($space in $spaces) {
    $expectedSpaceId = "q$($space.q)_r$($space.r)"
    Assert-MapCondition ($space.spaceId -eq $expectedSpaceId) "Space '$($space.spaceId)' does not match its canonical map-local coordinate ID '$expectedSpaceId'."

    $coordinateKey = Get-CoordinateKey -Q $space.q -R $space.r
    Assert-MapCondition (-not $spacesByCoordinate.ContainsKey($coordinateKey)) "Duplicate coordinate '$coordinateKey'."
    $spacesByCoordinate[$coordinateKey] = $space
    $spacesById[$space.spaceId] = $space

    if ($space.kind -eq 'terrain') {
        Assert-MapCondition ($null -eq $space.zoneId) "Terrain cell '$($space.spaceId)' must not belong to a zone."
    }
    elseif ($space.kind -eq 'playable') {
        Assert-MapCondition ($zoneIds -contains $space.zoneId) "Playable space '$($space.spaceId)' has an unknown zone."
    }
    else {
        throw "Space '$($space.spaceId)' has unknown kind '$($space.kind)'."
    }
}

foreach ($zoneId in $expectedZones.Keys) {
    $actualCount = @($spaces | Where-Object { $_.zoneId -eq $zoneId }).Count
    Assert-MapCondition ($actualCount -eq $expectedZones[$zoneId]) "Zone '$zoneId' has $actualCount spaces; expected $($expectedZones[$zoneId])."
}

$positiveDirections = @(
    @(1, 0),
    @(1, -1),
    @(0, -1)
)
$physicalConnectionCount = 0
$playableConnectionCount = 0
foreach ($space in $spaces) {
    foreach ($offset in $positiveDirections) {
        $neighborKey = Get-CoordinateKey -Q ($space.q + $offset[0]) -R ($space.r + $offset[1])
        if ($spacesByCoordinate.ContainsKey($neighborKey)) {
            $physicalConnectionCount++
            if ($space.kind -eq 'playable' -and $spacesByCoordinate[$neighborKey].kind -eq 'playable') {
                $playableConnectionCount++
            }
        }
    }
}
Assert-MapCondition ($physicalConnectionCount -eq 809) "Expected 809 undirected physical connections; found $physicalConnectionCount."
Assert-MapCondition ($playableConnectionCount -eq 530) "Expected 530 undirected playable connections; found $playableConnectionCount."
Assert-MapCondition ($map.connectionPolicy.undirectedPhysicalConnectionCount -eq $physicalConnectionCount) 'Recorded physical connection total is stale.'
Assert-MapCondition ($map.connectionPolicy.undirectedPlayableConnectionCount -eq $playableConnectionCount) 'Recorded playable connection total is stale.'

$spawnPoints = @($map.spawnPoints)
Assert-MapCondition ($spawnPoints.Count -eq 40) 'Expected exactly 40 spawn points.'
Assert-MapCondition (@($spawnPoints.spawnPointId | Select-Object -Unique).Count -eq $spawnPoints.Count) 'Spawn point IDs must be unique.'
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
foreach ($spawnPoint in $spawnPoints) {
    Assert-MapCondition ($spacesById.ContainsKey($spawnPoint.spaceId)) "Spawn point '$($spawnPoint.spawnPointId)' references an unknown space."
    Assert-MapCondition ($spacesById[$spawnPoint.spaceId].kind -eq 'playable') "Spawn point '$($spawnPoint.spawnPointId)' must be on a playable space."
    $spawnKey = "$($spawnPoint.teamId)|$($spawnPoint.unitType)"
    Assert-MapCondition ($expectedSpawnCounts.ContainsKey($spawnKey)) "Spawn point '$($spawnPoint.spawnPointId)' has an unknown team/type combination."
    if (-not $actualSpawnCounts.ContainsKey($spawnKey)) {
        $actualSpawnCounts[$spawnKey] = 0
    }
    $actualSpawnCounts[$spawnKey]++
}
foreach ($spawnKey in $expectedSpawnCounts.Keys) {
    Assert-MapCondition ($actualSpawnCounts[$spawnKey] -eq $expectedSpawnCounts[$spawnKey]) "Spawn count '$spawnKey' is $($actualSpawnCounts[$spawnKey]); expected $($expectedSpawnCounts[$spawnKey])."
}
Assert-MapCondition (@($spawnPoints | Where-Object { $_.teamId -eq 'red' }).Count -eq 20) 'Red must have exactly 20 spawn points.'
Assert-MapCondition (@($spawnPoints | Where-Object { $_.teamId -eq 'blue' }).Count -eq 20) 'Blue must have exactly 20 spawn points.'

$canonicalLines = @()
foreach ($space in $spaces) {
    $canonicalLines += "$($space.spaceId)|$($space.kind)|$($space.zoneId)"
}
foreach ($spawnPoint in $spawnPoints) {
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
Assert-MapCondition ($reviewDigest -eq 'addb4cdea8f2e0733233bd7fc047bc6cdd3b934102b48990699a524cfb7158bb') 'The reviewed space/spawn inventory digest changed; repeat source review before accepting it.'

Assert-MapCondition ($map.inventoryTotals.cells -eq $spaces.Count) 'Recorded cell total is stale.'
Assert-MapCondition ($map.inventoryTotals.playableSpaces -eq 210) 'Recorded playable-space total is stale.'
Assert-MapCondition ($map.inventoryTotals.terrainCells -eq 82) 'Recorded terrain-cell total is stale.'
Assert-MapCondition ($map.inventoryTotals.spawnPoints -eq $spawnPoints.Count) 'Recorded spawn-point total is stale.'

Write-Output "PASS: $($map.mapId) revision $($map.revision)"
Write-Output "Cells: 292 total, 210 playable, 82 terrain"
Write-Output "Zones: 7; connections: 809 physical, 530 playable"
Write-Output "Spawn points: 40 total, 20 Red, 20 Blue"
Write-Output "Reviewed inventory digest: $reviewDigest"
