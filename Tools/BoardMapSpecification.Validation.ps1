Set-StrictMode -Version Latest

function Assert-MapCondition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-ReferenceShape {
    param(
        [object[]]$Actual,
        [string[]]$Expected,
        [string]$Label
    )

    Assert-MapCondition (($Actual -join '|') -eq ($Expected -join '|')) "$Label must compose $($Expected -join ' and ')."
}

function Get-CoordinateKey {
    param([int]$Q, [int]$R)
    return "$Q,$R"
}

function Test-BoardMapSpecificationInvariant {
    param(
        [Parameter(Mandatory = $true)]$Map,
        [string]$Label = 'map specification'
    )

    Assert-MapCondition ($Map.'$schema' -eq 'guards-digital/map-specification/v1') "$Label has an unexpected map schema."
    Assert-MapCondition ($Map.schemaVersion -eq 1) "$Label has an unexpected schema version."
    Assert-MapCondition (-not [string]::IsNullOrWhiteSpace($Map.mapId)) "$Label must have a stable mapId."
    # ConvertFrom-Json yields [int] under Windows PowerShell 5.1 but [long] under PowerShell 7.
    Assert-MapCondition (($Map.revision -is [int] -or $Map.revision -is [long]) -and $Map.revision -ge 1) "$Label must have a positive integer revision."

    Assert-MapCondition ($Map.identity.spaceIdScope -eq 'map-local') "$Label SpaceId values must be map-local."
    Assert-MapCondition ($Map.identity.zoneIdScope -eq 'map-local') "$Label ZoneId values must be map-local."
    Assert-MapCondition ($Map.identity.spawnPointIdScope -eq 'map-local') "$Label SpawnPointId values must be map-local."
    Assert-ReferenceShape @($Map.identity.globalSpaceReference) @('mapId', 'spaceId') "$Label global space references"
    Assert-ReferenceShape @($Map.identity.globalZoneReference) @('mapId', 'zoneId') "$Label global zone references"
    Assert-ReferenceShape @($Map.identity.globalSpawnPointReference) @('mapId', 'spawnPointId') "$Label global spawn-point references"
    Assert-ReferenceShape @($Map.identity.selectedMapReference) @('mapId', 'revision') "$Label selected-map references"
    Assert-MapCondition ($Map.identity.selectionMutability -eq 'immutable-per-match') "$Label selection must be immutable per match."
    Assert-MapCondition ($Map.identity.coordinateQueryScope -eq 'selected-map-local') "$Label coordinate queries must be local to the selected match map."

    Assert-MapCondition ($Map.coordinateSystem.type -eq 'axial') "$Label must use axial coordinates."
    Assert-MapCondition ($Map.coordinateSystem.orientation -eq 'pointy-top') "$Label must use pointy-top orientation."
    Assert-MapCondition ($Map.connectionPolicy.kind -eq 'standard-axial-touching-inventory-v1') "$Label has an unexpected connection policy."

    $expectedDirectionOffsets = @('1|0', '1|-1', '0|-1', '-1|0', '-1|1', '0|1')
    $actualDirectionOffsets = @($Map.coordinateSystem.directions | ForEach-Object { "$($_.dq)|$($_.dr)" })
    Assert-MapCondition ($actualDirectionOffsets.Count -eq 6) "$Label must declare all six axial directions."
    Assert-MapCondition (@($actualDirectionOffsets | Select-Object -Unique).Count -eq 6) "$Label axial directions must be unique."
    foreach ($directionOffset in $expectedDirectionOffsets) {
        Assert-MapCondition ($actualDirectionOffsets -contains $directionOffset) "$Label is missing axial direction '$directionOffset'."
    }

    $zones = @($Map.zones)
    Assert-MapCondition ($zones.Count -gt 0) "$Label must contain at least one zone."
    $zoneIds = @($zones | ForEach-Object { $_.zoneId })
    Assert-MapCondition (@($zoneIds | Select-Object -Unique).Count -eq $zoneIds.Count) "$Label ZoneId values must be unique within the map."
    foreach ($zone in $zones) {
        Assert-MapCondition (-not [string]::IsNullOrWhiteSpace($zone.zoneId)) "$Label contains an empty ZoneId."
        Assert-MapCondition (-not [string]::IsNullOrWhiteSpace($zone.kind)) "$Label zone '$($zone.zoneId)' must declare a kind."
        Assert-MapCondition ($zone.isThrone -is [bool]) "$Label zone '$($zone.zoneId)' must declare isThrone."
        Assert-MapCondition ($zone.isStartingBattleZone -is [bool]) "$Label zone '$($zone.zoneId)' must declare isStartingBattleZone."
        Assert-MapCondition ($zone.isThrone -eq ($zone.kind -eq 'throne')) "$Label zone '$($zone.zoneId)' has inconsistent Throne metadata."
    }
    $declaredOrderedLanes = @(if ($Map.PSObject.Properties.Name -contains 'orderedLanes') {
        $Map.orderedLanes
    }
    elseif ($Map.PSObject.Properties.Name -contains 'orderedLaneZoneIds') {
        [pscustomobject]@{ laneId = 'main'; zoneIds = @($Map.orderedLaneZoneIds) }
    }
    else {
    })
    $expectedStartingBattleZones = [Math]::Max(1, $declaredOrderedLanes.Count)
    Assert-MapCondition (@($zones | Where-Object { $_.isStartingBattleZone }).Count -eq $expectedStartingBattleZones) "$Label must have one starting Battle Zone per ordered Lane."

    $spaces = @($Map.spaces)
    Assert-MapCondition ($spaces.Count -gt 0) "$Label must contain at least one space."
    Assert-MapCondition (@($spaces.spaceId | Select-Object -Unique).Count -eq $spaces.Count) "$Label SpaceId values must be unique within the map."

    $spacesByCoordinate = @{}
    $spacesById = @{}
    foreach ($space in $spaces) {
        $expectedSpaceId = "q$($space.q)_r$($space.r)"
        Assert-MapCondition ($space.spaceId -eq $expectedSpaceId) "$Label space '$($space.spaceId)' does not match canonical map-local ID '$expectedSpaceId'."

        $coordinateKey = Get-CoordinateKey -Q $space.q -R $space.r
        Assert-MapCondition (-not $spacesByCoordinate.ContainsKey($coordinateKey)) "$Label contains duplicate coordinate '$coordinateKey'."
        $spacesByCoordinate[$coordinateKey] = $space
        $spacesById[$space.spaceId] = $space

        if ($space.kind -eq 'terrain') {
            Assert-MapCondition ($null -eq $space.zoneId) "$Label terrain cell '$($space.spaceId)' must not belong to a zone."
        }
        elseif ($space.kind -eq 'playable') {
            Assert-MapCondition ($zoneIds -contains $space.zoneId) "$Label playable space '$($space.spaceId)' has an unknown ZoneId."
        }
        else {
            throw "$Label space '$($space.spaceId)' has unknown kind '$($space.kind)'."
        }
    }

    $positiveDirections = @(@(1, 0), @(1, -1), @(0, -1))
    $physicalConnectionCount = 0
    $playableConnectionCount = 0
    $zoneAdjacencyKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($space in $spaces) {
        foreach ($offset in $positiveDirections) {
            $neighborKey = Get-CoordinateKey -Q ($space.q + $offset[0]) -R ($space.r + $offset[1])
            if ($spacesByCoordinate.ContainsKey($neighborKey)) {
                $physicalConnectionCount++
                if ($space.kind -eq 'playable' -and $spacesByCoordinate[$neighborKey].kind -eq 'playable') {
                    $playableConnectionCount++
                    $neighbor = $spacesByCoordinate[$neighborKey]
                    if ($space.zoneId -ne $neighbor.zoneId) {
                        $orderedZoneIds = @($space.zoneId, $neighbor.zoneId) | Sort-Object
                        [void]$zoneAdjacencyKeys.Add("$($orderedZoneIds[0])|$($orderedZoneIds[1])")
                    }
                }
            }
        }
    }
    Assert-MapCondition ($Map.connectionPolicy.undirectedPhysicalConnectionCount -eq $physicalConnectionCount) "$Label recorded physical connection total is stale."
    Assert-MapCondition ($Map.connectionPolicy.undirectedPlayableConnectionCount -eq $playableConnectionCount) "$Label recorded playable connection total is stale."

    $zonesById = @{}
    foreach ($zone in $zones) { $zonesById[$zone.zoneId] = $zone }
    $orderedLanes = @()
    foreach ($declaredLane in $declaredOrderedLanes) {
        $orderedLaneZoneIds = @($declaredLane.zoneIds)
        $laneLabel = if ([string]::IsNullOrWhiteSpace($declaredLane.laneId)) { 'ordered Lane' } else { "ordered Lane '$($declaredLane.laneId)'" }
        Assert-MapCondition (@($orderedLaneZoneIds | Select-Object -Unique).Count -eq $orderedLaneZoneIds.Count) "$Label $laneLabel ZoneId values must be unique."
        foreach ($laneZoneId in $orderedLaneZoneIds) {
            Assert-MapCondition ($zoneIds -contains $laneZoneId) "$Label $laneLabel references unknown ZoneId '$laneZoneId'."
        }
        if ($orderedLaneZoneIds.Count -eq 0) { continue }
        Assert-MapCondition ($orderedLaneZoneIds.Count -ge 2) "$Label ordered Lane must contain at least two zones."
        Assert-MapCondition $zonesById[$orderedLaneZoneIds[0]].isThrone "$Label ordered Lane must begin at a Throne."
        Assert-MapCondition $zonesById[$orderedLaneZoneIds[-1]].isThrone "$Label ordered Lane must end at a Throne."
        Assert-MapCondition (@($orderedLaneZoneIds | Where-Object { $zonesById[$_].isStartingBattleZone }).Count -eq 1) "$Label ordered Lane must contain the starting Battle Zone."
        for ($index = 0; $index -lt $orderedLaneZoneIds.Count - 1; $index++) {
            $orderedPair = @($orderedLaneZoneIds[$index], $orderedLaneZoneIds[$index + 1]) | Sort-Object
            $adjacencyKey = "$($orderedPair[0])|$($orderedPair[1])"
            Assert-MapCondition ($zoneAdjacencyKeys.Contains($adjacencyKey)) "$Label consecutive ordered Lane zones '$($orderedLaneZoneIds[$index])' and '$($orderedLaneZoneIds[$index + 1])' do not share a playable border."
        }
        $orderedLanes += [pscustomobject]@{ LaneId = $declaredLane.laneId; ZoneIds = $orderedLaneZoneIds }
    }
    if ($orderedLanes.Count -gt 0) {
        $orderedNonThroneZoneIds = @($orderedLanes | ForEach-Object { $_.ZoneIds } | Where-Object { -not $zonesById[$_].isThrone })
        $expectedNonThroneLaneZoneIds = @($zones | Where-Object { $_.kind -eq 'lane' } | ForEach-Object { $_.zoneId })
        Assert-MapCondition (($orderedNonThroneZoneIds.Count -eq $expectedNonThroneLaneZoneIds.Count) -and
            (@($orderedNonThroneZoneIds | Select-Object -Unique).Count -eq $orderedNonThroneZoneIds.Count) -and
            (@($orderedNonThroneZoneIds | Where-Object { $expectedNonThroneLaneZoneIds -notcontains $_ }).Count -eq 0)) "$Label ordered Lanes must contain every non-Throne lane zone exactly once."
    }

    $spawnPoints = @($Map.spawnPoints)
    Assert-MapCondition (@($spawnPoints.spawnPointId | Select-Object -Unique).Count -eq $spawnPoints.Count) "$Label SpawnPointId values must be unique within the map."
    foreach ($spawnPoint in $spawnPoints) {
        Assert-MapCondition (-not [string]::IsNullOrWhiteSpace($spawnPoint.spawnPointId)) "$Label contains an empty SpawnPointId."
        Assert-MapCondition ($spacesById.ContainsKey($spawnPoint.spaceId)) "$Label spawn point '$($spawnPoint.spawnPointId)' references an unknown SpaceId."
        Assert-MapCondition ($spacesById[$spawnPoint.spaceId].kind -eq 'playable') "$Label spawn point '$($spawnPoint.spawnPointId)' must be on a playable space."
        Assert-MapCondition (-not [string]::IsNullOrWhiteSpace($spawnPoint.teamId)) "$Label spawn point '$($spawnPoint.spawnPointId)' must declare a teamId."
        Assert-MapCondition (-not [string]::IsNullOrWhiteSpace($spawnPoint.unitType)) "$Label spawn point '$($spawnPoint.spawnPointId)' must declare a unitType."
    }

    return [pscustomobject]@{
        Zones = $zones
        ZoneIds = $zoneIds
        Spaces = $spaces
        SpacesById = $spacesById
        SpawnPoints = $spawnPoints
        PhysicalConnectionCount = $physicalConnectionCount
        PlayableConnectionCount = $playableConnectionCount
        ZoneAdjacencyKeys = @($zoneAdjacencyKeys | Sort-Object)
        OrderedLanes = $orderedLanes
        OrderedLaneZoneIds = if ($orderedLanes.Count -eq 1) { @($orderedLanes[0].ZoneIds) } else { @() }
    }
}
