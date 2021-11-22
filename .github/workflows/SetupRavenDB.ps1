          $region = "switzerlandnorth"
          $license = ''

          function NewRavenDBNode {
            param (
                $region,
                $prefix,
                $instanceId,
                $runnerOs,
                $commit
            )

            $hostname = "$prefix-$instanceId"

            # echo will mess up the return value
            Write-Debug "Creating RavenDB container $hostname in $region (This can take a while.)"

            $details = az container create --image ravendb/ravendb:5.2-ubuntu-latest --name $hostname --location $region --dns-name-label $hostname --resource-group GitHubActions-RG --cpu 4 --memory 8 --ports 8080 38888 --ip-address public --environment-variables RAVEN_ServerUrl="http://0.0.0.0:8080" RAVEN_ServerUrl_Tcp="tcp://0.0.0.0:38888" RAVEN_PublicServerUrl="http://$($hostname).$($region).azurecontainer.io:8080" RAVEN_PublicServerUrl_Tcp="tcp://$($hostname).$($region).azurecontainer.io:38888" RAVEN_Setup_Mode="None" RAVEN_License_Eula_Accepted="true" RAVEN_Security_UnsecuredAccessAllowed="PublicNetwork" | ConvertFrom-Json
            
            # echo will mess up the return value
            Write-Debug "Tagging container image"
            $dateTag = "Created=$(Get-Date -Format "yyyy-MM-dd")"
            $ignore = az tag create --resource-id $details.id --tags Package=RavenGatewayPersistence RunnerOS=$runnerOs Commit=$commit $dateTag

            return $details.ipAddress.fqdn
        }

        $prefix = "psw-ravendb-gateway-$(Get-Random)"

        echo "::set-output name=prefix::$prefix"

        $fqdnRavenDB = @{ singlenode = ""; leader = ""; follower1 = ""; follower2 = "" } 

        $NewRavenDBNodeDef = $function:NewRavenDBNode.ToString()

        @($fqdnRavenDB.keys) | ForEach-Object -Parallel {
            $function:NewRavenDBNode = $using:NewRavenDBNodeDef
            $region = $using:region;
            $prefix = $using:prefix;
            $detail = NewRavenDBNode $region $prefix $_ Windows 56556
            $hashTable = $using:fqdnRavenDB;
            $hashTable[$_] = $detail
        }

        @($fqdnRavenDB.keys) | ForEach-Object -Parallel {
            $hashTable = $using:fqdnRavenDB;
            echo "::add-mask::$hashTable[$_]"
            $tcpClient = New-Object Net.Sockets.TcpClient
            echo "Verifying connection $_"
            echo $hashTable[$_]
            do
            {
                try
                {
                    echo "Trying to connect to $_"
                    $tcpClient.Connect($hashTable[$_], 8080)
                    echo "Connection to $_ successful"
                } catch 
                {
                    Start-Sleep -Seconds 2
                }
            } While($tcpClient.Connected -ne "True")
            $tcpClient.Close()
            echo "Connection to $_ verified"
        }

        # Once you set the license on a node, it assumes the node to be a cluster, so only set the license on the leader
        echo "Activating license on singlenode"
        curl "http://$($fqdnRavenDB['singlenode']):8080/admin/license/activate" -H 'Content-Type: application/json; charset=UTF-8' -d "$($license)"

        # Once you set the license on a node, it assumes the node to be a cluster, so only set the license on the leader
        echo "Activating license on leader"
        curl "http://$($fqdnRavenDB['leader']):8080/admin/license/activate" -H 'Content-Type: application/json; charset=UTF-8' -d "$($license)"

        curl "http://$($fqdnRavenDB['leader']):8080/admin/license/set-limit?nodeTag=A&newAssignedCores=1" -X POST -H 'Content-Type: application/json; charset=utf-8' -H 'Content-Length: 0'
        $encodedURL = [System.Web.HttpUtility]::UrlEncode("http://$($fqdnRavenDB['follower1']):8080") 
        curl "http://$($fqdnRavenDB['leader']):8080/admin/cluster/node?url=$($encodedURL)&tag=B&watcher=true&assignedCores=1" -X PUT -H 'Content-Type: application/json; charset=utf-8' -H 'Content-Length: 0'
        $encodedURL = [System.Web.HttpUtility]::UrlEncode("http://$($fqdnRavenDB['follower2']):8080")
        curl "http://$($fqdnRavenDB['leader']):8080/admin/cluster/node?url=$($encodedURL)&tag=C&watcher=true&assignedCores=1" -X PUT -H 'Content-Type: application/json; charset=utf-8' -H 'Content-Length: 0'

        echo "RavenSingleNodeUrl=http://$($fqdnRavenDB['singlenode']):8080" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf-8 -Append
        echo "CommaSeparatedRavenClusterUrls=http://$($fqdnRavenDB['leader']):8080,http://$($fqdnRavenDB['follower1']):8080,http://$($fqdnRavenDB['follower2']):8080" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf-8 -Append