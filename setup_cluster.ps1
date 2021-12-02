$license=$args[0]
Write-Output $license
$hostip=$args[1]
Write-Output $hostip 
$fqdnRavenDB = @{ leader = "$($hostip):8081"; follower1 = "$($hostip):8082"; follower2 = "$($hostip):8083" }
@($fqdnRavenDB.keys) | ForEach-Object -Parallel {
    $hashTable = $using:fqdnRavenDB;
    $tcpClient = New-Object Net.Sockets.TcpClient
    Write-Output "Verifying connection $_"
    do
    {
        try
        {
            Write-Output "Trying to connect to $_"
            $ipAndPort = $hashTable[$_].Split(":")
            $tcpClient.Connect($ipAndPort[0], $ipAndPort[1])
            Write-Output "Connection to $_ successful"
        } catch 
        {
            Start-Sleep -Seconds 2
        }
    } While($tcpClient.Connected -ne "True")
    $tcpClient.Close()
    Write-Output "Connection to $_ verified"
}
# Once you set the license on a node, it assumes the node to be a cluster, so only set the license on the leader
Write-Output "Activating license on leader"
Invoke-WebRequest "http://$($fqdnRavenDB['leader'])/admin/license/activate" -Method POST -Headers @{ 'Content-Type' = 'application/json'; 'charset' = 'UTF-8' } -Body "$($license)"
Invoke-WebRequest "http://$($fqdnRavenDB['leader'])/admin/license/set-limit?nodeTag=A&newAssignedCores=1" -Method POST -Headers @{ 'Content-Type' = 'application/json'; 'Context-Length' = '0'; 'charset' = 'UTF-8' }
$encodedURL = [System.Web.HttpUtility]::UrlEncode("http://$($fqdnRavenDB['follower1'])") 
Invoke-WebRequest "http://$($fqdnRavenDB['leader'])/admin/cluster/node?url=$($encodedURL)&tag=B&watcher=true&assignedCores=1" -Method PUT -Headers @{ 'Content-Type' = 'application/json'; 'Context-Length' = '0'; 'charset' = 'UTF-8' }
$encodedURL = [System.Web.HttpUtility]::UrlEncode("http://$($fqdnRavenDB['follower2'])")
Invoke-WebRequest "http://$($fqdnRavenDB['leader'])/admin/cluster/node?url=$($encodedURL)&tag=C&watcher=true&assignedCores=1" -Method PUT -Headers @{ 'Content-Type' = 'application/json'; 'Context-Length' = '0'; 'charset' = 'UTF-8' }