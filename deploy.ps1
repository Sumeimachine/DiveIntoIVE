Write-Host "========== STARTING DEPLOY =========="

$server = "root@5.223.68.15"
$remotePath = "/var/www/api"
$tempPath = "/var/www/api_new"

Write-Host "Building project..."
dotnet publish -c Release

Write-Host "Uploading build to temporary folder..."
ssh $server "rm -rf $tempPath && mkdir $tempPath"

scp -r bin/Release/net8.0/publish/* ${server}:${tempPath}

Write-Host "Swapping deployment folders..."

ssh $server "if [ -d $remotePath ]; then mv $remotePath ${remotePath}_backup; fi"
ssh $server "mv $tempPath $remotePath"

Write-Host "Restarting API..."
ssh $server "systemctl restart iveph-api"

Write-Host "Cleaning old backup..."
ssh $server "rm -rf ${remotePath}_backup"

Write-Host "========== DEPLOY COMPLETE =========="