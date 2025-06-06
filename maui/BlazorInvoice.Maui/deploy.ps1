dotnet publish .\BlazorInvoice.Maui.csproj -f net9.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64

$releasePath = "./bin/Release/net9.0-windows10.0.19041.0/win10-x64/AppPackages"

$latestDir = (Get-ChildItem -Directory $releasePath | Sort-Object CreationTime)[-1].FullName

$filesToDeploy = Get-ChildItem -Path $latestDir | Select-Object Name | Where-Object { $_ -match 'BlazorInvoice.Maui*' }

$regex = [regex] "\d+\.\d+\.\d+\.\d+"

$version = $regex.Match($filesToDeploy[0].Name).Value

$file2 = Join-Path -Path $latestDir -ChildPath $filesToDeploy[1].Name

# Rename MSIX file
$newName = "BlazorInvoice.Maui_" + $version + "_x64.msix"
$newFilePath = Join-Path -Path $latestDir -ChildPath $newName
Rename-Item -Path $file2 -NewName $newName

$file3 = Join-Path -Path $latestDir -ChildPath "latest.yml"

if (!(Test-Path $file3))
{
    New-Item -Path $file3
    "Version: " + $version + "`r`n" | Out-File -FilePath $file3
}

$ghVersion = "v" + $version

gh release create --generate-notes --draft $ghVersion $newFilePath $file3