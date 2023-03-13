# Rebug
https://github.com/dotnet/SqlClient/issues/1933

# Deploy infrastructure
```powershell
> az login
> az account set --subscription sandbox
> az group create --name rg-rebug --location northeurope
> az deployment group create -g rg-rebug --template-file .\main.bicep --confirm-with-what-if --parameters appName=Rebug sqlAdministratorLogin=<username> sqlAdministratorPassword=<password>
```