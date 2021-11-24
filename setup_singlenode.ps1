$license=$args[0]
Write-Output $license 
$hostip=$args[1]
Write-Output $hostip 
$fqdnRavenDB = @{ singlenode = "$($hostip):8080"; }
$tcpClient = New-Object Net.Sockets.TcpClient
Write-Output "Verifying connection the single node"
do
{
    try
    {
        Write-Output "Trying to connect to the single node"
        $ipAndPort = $fqdnRavenDB['singlenode'].Split(":")
        $tcpClient.Connect($ipAndPort[0], $ipAndPort[1])
        Write-Output "Connection to the single node successful"
    } catch 
    {
        Start-Sleep -Seconds 2
    }
} While($tcpClient.Connected -ne "True")
$tcpClient.Close()
Write-Output "Connection to the single node verified"

Write-Output "Activating license on leader"
Invoke-WebRequest "http://$($fqdnRavenDB['singlenode'])/admin/license/activate" -Method POST -Headers @{ 'Content-Type' = 'application/json'; 'charset' = 'UTF-8' } -Body "$($license)"