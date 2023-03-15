# Rebug
https://github.com/dotnet/SqlClient/issues/1933

# Deploy infrastructure
```powershell
> az login
> az account set --subscription sandbox
> az group create --name rg-rebug --location northeurope
> az deployment group create -g rg-rebug --template-file .\main.bicep --confirm-with-what-if --parameters appName=Rebug
```

# Deploy web app
Right-click on the project in Visual Studio and click "Publish...". Follow the instructions to deploy to the newly created app service.