# Rebug
https://github.com/dotnet/SqlClient/issues/1933

# Deploy infrastructure
```powershell
> az login
> az account set --subscription sandbox
> az group create --name rg-rebug --location northeurope
> az deployment group create -g rg-rebug --template-file .\main.bicep --confirm-with-what-if --parameters appName=Rebug firstDeploy=true
```

## Create db users - alternative 1
Assign the system identity of the SQL Server the Azure AD role `Directory Readers`:
https://learn.microsoft.com/en-us/azure/active-directory/roles/manage-roles-portal#azure-portal

```powershell
> az deployment group create -g rg-rebug --template-file .\main.bicep --confirm-with-what-if --parameters appName=Rebug
```
## Create db users - alternative 2
Assign yourself as SQL Server Admin and execute these queries against the database:
```sql
CREATE USER [<appName>] FROM EXTERNAL PROVIDER
EXEC sp_addrolemember N'db_datawriter', [<appName>]
EXEC sp_addrolemember N'db_datareader', [<appName>]
```

# Deploy web app
Right-click on the project in Visual Studio and click "Publish...". Follow the instructions to deploy to the newly created app service.