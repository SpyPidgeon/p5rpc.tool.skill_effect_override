# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p5rpc.tool.skill_effect_override/*" -Force -Recurse
dotnet publish "./p5rpc.tool.skill_effect_override.csproj" -c Release -o "$env:RELOADEDIIMODS/p5rpc.tool.skill_effect_override" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location