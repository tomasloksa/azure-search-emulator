param([String]$fname="solr_662_ssl", [String]$dname="localhost", [String]$pw="123SecureSolr!", [Int32]$expireMonths=36)

Write-Host "Generating certificate using friendly name $fname & DNS name $dname"

$cert = New-SelfSignedCertificate -FriendlyName $fname -DnsName $dname -CertStoreLocation 'cert:\LocalMachine' -NotAfter (Get-Date).AddMonths($expireMonths)
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root", "LocalMachine"
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()
New-Item -Path (Join-Path $PSScriptRoot "\certs") -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
$cert | Export-PfxCertificate -FilePath (Join-Path $PSScriptRoot "\certs\solr-ssl.keystore.pfx") -Password (ConvertTo-SecureString -String $pw -Force -AsPlainText) -Force | Out-Null
$cert | Remove-Item