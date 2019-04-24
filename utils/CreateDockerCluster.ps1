param(
    $MasterBindPort = 8084,
    $MasterBindTcpPort = 38888,
    $Node2BindPort = 8085,
    $Node2BindTcpPort = 38889,
    $Node3BindPort = 8086,
    $Node3BindTcpPort = 38890,
    $ipAddress='10.0.75.2'
    )

	
docker run -d -e RAVEN_Security_UnsecuredAccessAllowed=PublicNetwork -e RAVEN_PublicServerUrl=http://${ipAddress}:${MasterBindPort} -e RAVEN_PublicServerUrl_Tcp=tcp://${ipAddress}:${MasterBindTcpPort} -e RAVEN_ARGS='--print-id --log-to-console --Setup.Mode=None --License.Eula.Accepted=true' -p ${MasterBindPort}:8080 -p ${MasterBindTcpPort}:38888 ravendb/ravendb:ubuntu-latest

# A manual step need to happen here. A license need to be provided for the master node. 
Write-Host "`r`nhttp://${ipAddress}:${MasterBindPort}";
Write-Host -NoNewLine 'Log into the Raven and provide a license. If you use development license reduce the amount of used cores to 1.';
Write-Host "`r`nAfter doing that press a key to continue...";
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');

# Bootstraping a cluster
Invoke-WebRequest -Method Post -URI http://${ipAddress}:${MasterBindPort}/admin/cluster/bootstrap

# Starting two additional nodes
docker run -d -e RAVEN_Security_UnsecuredAccessAllowed=PublicNetwork -e RAVEN_PublicServerUrl=http://${ipAddress}:${Node2BindPort} -e RAVEN_PublicServerUrl_Tcp=tcp://${ipAddress}:${Node2BindTcpPort} -e RAVEN_ARGS='--print-id --log-to-console --Setup.Mode=None --License.Eula.Accepted=true' -p ${Node2BindPort}:8080 -p ${Node2BindTcpPort}:38888 ravendb/ravendb:ubuntu-latest
docker run -d -e RAVEN_Security_UnsecuredAccessAllowed=PublicNetwork -e RAVEN_PublicServerUrl=http://${ipAddress}:${Node3BindPort} -e RAVEN_PublicServerUrl_Tcp=tcp://${ipAddress}:${Node3BindTcpPort} -e RAVEN_ARGS='--print-id --log-to-console --Setup.Mode=None --License.Eula.Accepted=true' -p ${Node3BindPort}:8080 -p ${Node3BindTcpPort}:38888 ravendb/ravendb:ubuntu-latest

# Waiting couple of seconds as otherwise one can't add them to the cluster
Write-Host -NoNewLine 'Waiting 2 seconds for the containers to start and adding them to cluster';
Start-Sleep -s 2

# Adding two nodes into the cluster. 
Invoke-WebRequest -Method Put -URI "http://${ipAddress}:${MasterBindPort}/admin/cluster/node?url=http://${ipAddress}:${Node2BindPort}&tag=B&assignedCores=1"
Invoke-WebRequest -Method Put -URI "http://${ipAddress}:${MasterBindPort}/admin/cluster/node?url=http://${ipAddress}:${Node3BindPort}&tag=C&assignedCores=1"