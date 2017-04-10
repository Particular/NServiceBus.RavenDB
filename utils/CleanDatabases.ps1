$DatabaseUrl = "http://localhost:8084"
$DatabasePath = "C:\RavenDBv35\Databases"
$WaitMinutes = 0
$ignore = @( "system", "databases" )

function TC-Write([string]$name) {
	Write-Output "##teamcity[message text='$name']"
}

TC-Write "Running RavenDB Database Cleaner"
TC-Write "RavenDB URL: $DatabaseUrl"
TC-Write "RavenDB Data Path: $DatabasePath"

$databases = Get-ChildItem $DatabasePath -Directory `
| Where-Object { $ignore -notcontains $_.Name } `
| Where-Object { $_.CreationTimeUtc -lt (([DateTime]::UtcNow).AddMinutes(-$WaitMinutes)) }


foreach($database in $databases) {
    TC-Write "Deleting $database"
    $deleteUrl = "$($DatabaseUrl)/admin/databases/$([Uri]::EscapeDataString($database.Name))?hard-delete=true"
    TC-Write "DELETE $deleteUrl"
    Invoke-RestMethod $deleteUrl -Method Delete
}

Write-Host Completed