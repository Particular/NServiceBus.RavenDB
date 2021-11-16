#$hostInfo = curl -H Metadata:true "169.254.169.254/metadata/instance?api-version=2017-08-01" | ConvertFrom-Json
$StartMs = (Get-Date).Millisecond
$region = "switzerlandnorth"

function NewRavenDBNode {
    param (
        $region,
        $prefix,
        $instanceId,
        $runnerOs
    )

    $hostname = "psw-ravendb-$prefix-$instanceId"

    # echo will mess up the return value
    Write-Debug "Creating RavenDB container $hostname in $region (This can take a while.)"

    $details = az container create --image ravendb/ravendb:5.2-ubuntu-latest --name $hostname --location $region --dns-name-label $hostname --resource-group GitHubActions-RG --cpu 4 --memory 8 --ports 8080 38888 --ip-address public -e RAVEN_ARGS="--ServerUrl=http://0.0.0.0:8080 --ServerUrl.Tcp=tcp://0.0.0.0:38888 --PublicServerUrl=http://$($hostname).$($region).azurecontainer.io:8080 --PublicServerUrl.Tcp=tcp://$($hostname).$($region).azurecontainer.io:38888 --Setup.Mode=None --License.Eula.Accepted=true --Security.UnsecuredAccessAllowed=PublicNetwork" | ConvertFrom-Json
    
    # echo will mess up the return value
    Write-Debug "Tagging container image"
    $dateTag = "Created=$(Get-Date -Format "yyyy-MM-dd")"
    $ignore = az tag create --resource-id $details.id --tags Package=RavenPersistence RunnerOS=$runnerOs $dateTag

    return $details.ipAddress.fqdn
}

$prefix = $(Get-Random)

$fqdnRavenDB = @(0, 1, 2, 3)

$NewRavenDBNodeDef = $function:NewRavenDBNode.ToString()

$fqdnRavenDB | ForEach-Object -Parallel {
    $function:NewRavenDBNode = $using:NewRavenDBNodeDef
    $region = $using:region;
    $prefix = $using:prefix;
    $detail = NewRavenDBNode $region $prefix $_ Windows
    $arr = $using:fqdnRavenDB;
    $arr[$_] = $detail
}

# For debugging
#$fqdnRavenDB = @(0)
#$fqdnRavenDB[0] = "psw-ravendb-709894776-0.switzerlandnorth.azurecontainer.io"
# $fqdnRavenDB[1] = "psw-ravendb-683065081-1.switzerlandnorth.azurecontainer.io"
# $fqdnRavenDB[2] = "psw-ravendb-683065081-2.switzerlandnorth.azurecontainer.io"
# $fqdnRavenDB[3] = "psw-ravendb-683065081-2.switzerlandnorth.azurecontainer.io"

$fqdnRavenDB | ForEach-Object -Parallel {
    $tcpClient = New-Object Net.Sockets.TcpClient
    echo "Verifying connection $_"
    do
    {
        try
        {
            echo "Trying to connect to $_"
            $tcpClient.Connect($_, 8080)
            echo "Connection to $_ successful"
        } catch 
        {
            Start-Sleep -Seconds 2
        }
    } While($tcpClient.Connected -ne "True")
    $tcpClient.Close()
    echo "Connection to $_ verified"
    echo "Activating license on $_"
    curl "http://$($_):8080/admin/license/activate" -H 'Content-Type: application/json; charset=UTF-8' -d 'LICENSE'
}

curl "http://$($fqdnRavenDB[1]):8080/admin/license/set-limit?nodeTag=A&newAssignedCores=1" -X POST -H 'Content-Type: application/json; charset=utf-8' -H 'Content-Length: 0'
$encodedURL = [System.Web.HttpUtility]::UrlEncode("http://$($fqdnRavenDB[2]):8080") 
curl "http://$($fqdnRavenDB[1]):8080/admin/cluster/node?url=$($encodedURL)&watcher=true&assignedCores=1" -X PUT -H 'Content-Type: application/json; charset=utf-8' -H 'Content-Length: 0'
$encodedURL = [System.Web.HttpUtility]::UrlEncode("http://$($fqdnRavenDB[3]):8080")
curl "http://$($fqdnRavenDB[1]):8080/admin/cluster/node?url=$($encodedURL)&watcher=true&assignedCores=1" -X PUT -H 'Content-Type: application/json; charset=utf-8' -H 'Content-Length: 0'

$EndMs = (Get-Date).Millisecond
Write-Host "The script took $($EndMs - $StartMs)"