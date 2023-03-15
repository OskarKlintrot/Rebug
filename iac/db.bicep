@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Name of database - default is rebugdb')
param databaseName string = 'rebugdb'

@description('External user to add access to the database. Usually the app name.')
param user string

param managedIdentityResoureName string

// Data resources
resource sqlserver 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'sqlserver${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    version: '12.0'
    administrators: {
      azureADOnlyAuthentication: true
      administratorType: 'ActiveDirectory'
      login: managedIdentity.name
      sid: managedIdentity.properties.clientId
      tenantId: subscription().tenantId
    }
  }

  resource database 'databases@2021-11-01' = {
    name: databaseName
    location: location
    sku: {
      name: 'Standard'
      tier: 'Standard'
      capacity: 10
    }
    properties: {
      collation: 'Finnish_Swedish_CI_AS'
      maxSizeBytes: 5368709120
      zoneRedundant: false
    }
  }

  resource firewallRule 'firewallRules@2021-11-01' = {
    name: 'AllowAllWindowsAzureIps'
    properties: {
      endIpAddress: '0.0.0.0'
      startIpAddress: '0.0.0.0'
    }
  }
}

// Rights
resource runPowerShellInline 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'runPowerShellInline'
  location: location
  kind: 'AzurePowerShell'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    azPowerShellVersion: '8.3'
    scriptContent: '''
      param([string] $sqlServer, [string] $database, [string] $user)
      
      Set-PSRepository PSGallery `
                       -SourceLocation https://www.powershellgallery.com/api/v2 `
                       -PackageManagementProvider NuGet `
                       -InstallationPolicy Trusted

      Install-Module -Name SqlServer -RequiredVersion 21.1.18256

#disable-next-line no-hardcoded-env-urls
      $credentials = Get-AzAccessToken -ResourceUrl https://database.windows.net
      $token = $credentials.Token

      $queries = @(
          "CREATE USER [${user}] FROM EXTERNAL PROVIDER"
          "EXEC sp_addrolemember N'db_datawriter', [${user}]"
          "EXEC sp_addrolemember N'db_datareader', [${user}]"
      )

      foreach ( $query in $queries )
      {
          Invoke-SqlCmd -ServerInstance $sqlServer `
                        -Database $database `
                        -AccessToken $token `
                        -Query $query
      }
    '''
    arguments: '-sqlServer ${sqlserver.properties.fullyQualifiedDomainName} -database ${sqlserver::database.name} -user ${user}'
    timeout: 'PT10M'
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'PT1H'
  }
}

// Managed Identity resources
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: managedIdentityResoureName
}

output databaseName string = sqlserver::database.name
output fullyQualifiedDomainName string = sqlserver.properties.fullyQualifiedDomainName
