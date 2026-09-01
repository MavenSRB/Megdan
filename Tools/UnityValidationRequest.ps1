$ErrorActionPreference = 'Stop'

function Resolve-UnityValidationFilterExpression {
    param(
        [Parameter(Mandatory = $true)] [psobject] $Request
    )

    $propertyNames = @($Request.PSObject.Properties.Name)
    $hasFilter = $propertyNames -contains 'filter'
    $hasFilters = $propertyNames -contains 'filters'

    if ($hasFilter -and $hasFilters) {
        throw "Validation request cannot provide both 'filter' and 'filters'."
    }

    if (-not $hasFilters) {
        # Preserve the original scalar contract, including an empty or omitted filter for complete suites.
        return [string] $Request.filter
    }

    if ($Request.filters -isnot [System.Array]) {
        throw "Validation request 'filters' must be a JSON array."
    }

    $filters = @($Request.filters)
    if ($filters.Count -eq 0) {
        throw "Validation request 'filters' must contain at least one fixture or test name."
    }

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $validated = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $filters) {
        if ($entry -isnot [string]) {
            throw "Every validation request 'filters' entry must be a string."
        }

        $filter = [string] $entry
        if ([string]::IsNullOrWhiteSpace($filter)) {
            throw "Validation request 'filters' entries must be nonempty."
        }
        if ($filter -cne $filter.Trim()) {
            throw "Validation request 'filters' entries cannot have leading or trailing whitespace."
        }
        if ($filter.Contains(';')) {
            throw "Validation request 'filters' entries cannot contain semicolon separators."
        }
        if (-not $seen.Add($filter)) {
            throw "Validation request 'filters' entries must be distinct; duplicate '$filter'."
        }

        $validated.Add($filter)
    }

    return $validated -join ';'
}
