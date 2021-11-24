$license=$args[0]
Write-Output $license 
$hostip=$args[1]
Write-Output $hostip 
$fqdnRavenDB = @{ singlenode = "$($hostip)"; }
$tcpClient = New-Object Net.Sockets.TcpClient
Write-Output "Verifying connection the single node"
do
{
    try
    {
        Write-Output "Trying to connect to the single node"
        $tcpClient.Connect($fqdnRavenDB['singlenode'], 8080)
        Write-Output "Connection to the single node successful"
    } catch 
    {
        Start-Sleep -Seconds 2
    }
} While($tcpClient.Connected -ne "True")
$tcpClient.Close()
Write-Output "Connection to the single node verified"

Write-Output "Activating license on leader"
Invoke-WebRequest "http://$($fqdnRavenDB['singlenode']):8080/admin/license/activate" -Method POST -Headers @{ 'Content-Type' = 'application/json'; 'charset' = 'UTF-8' } -Body "$($license)"