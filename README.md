# Rebug
https://github.com/dotnet/SqlClient/issues/1933

# Deploy infrastructure
```powershell
> az login
> az account set --subscription sandbox
> az group create --name rg-rebug --location northeurope
> az deployment group create -g rg-rebug --template-file .\main.bicep --confirm-with-what-if --parameters appName=Rebug sqlAdministratorLogin=<username> sqlAdministratorSid=<sid>
```

# Azure SQL
Create user for App Service:

```SQL
CREATE USER [Rebug] FROM EXTERNAL PROVIDER
EXEC sp_addrolemember N'db_datawriter', [Rebug]
EXEC sp_addrolemember N'db_datareader', [Rebug]
```

# Deploy web app
Right-click on the project in Visual Studio and click "Publish...". Follow the instructions to deploy to the newly created app service.