targetScope = 'resourceGroup'

@description('Do not try to create db users on first run, we need to assign Directory Readers to SQL Server Identity first')
param firstDeploy bool = false

@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Application name - used as prefix for resource names')
param appName string

var databaseName = 'rebugdb'

// Data resources
module db 'db.bicep' = {
  name: '${appName}-db-${uniqueString(resourceGroup().name)}'
  dependsOn: [ webapp, identity ]
  params: {
    location: location
    databaseName: databaseName
    user: appName
    managedIdentityResoureName: identity.outputs.managedIdentityResoureName
    firstDeploy: firstDeploy
  }
}

// Web App resources
module webapp 'webapp.bicep' = {
  name: '${appName}-webapp-${uniqueString(resourceGroup().name)}'
  params: {
    location: location
    appName: appName
  }
}

module conf 'webapp.config.bicep' = {
  name: '${appName}-webapp-conf-${uniqueString(resourceGroup().name)}'
  dependsOn: [ webapp, appInsights ]
  params: {
    appName: appName
    appInsights: {
      instrumentationKey: appInsights.outputs.instrumentationKey
      connectionString: appInsights.outputs.connectionString
    }
    databaseName: db.outputs.databaseName
    databaseServer: db.outputs.fullyQualifiedDomainName
  }
}

// Monitor
module appInsights 'ai.bicep' = {
  name: '${appName}-ai-${uniqueString(resourceGroup().name)}'
  params: {
    location: location
    webSiteId: webapp.outputs.id
    webSiteName: webapp.outputs.name
  }
}

// Managed identity
module identity 'managed-identity.bicep' = {
  name: 'ra-sqlserver${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    managedIdentityName: 'mi-sqlserver${uniqueString(resourceGroup().id)}'
    roleDefinitionIds: [
      // '88d8e3e3-8f55-4a1e-953a-9b9898b8876b' // Directory Readers
      // '8e3af657-a8ff-443c-a75c-2fe8c4bcb635' // Owner
      // '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec' // SQL Server Contributor
      // '6d8ee4ec-f05a-4a1d-8b00-a9b17e38b437' // SQL DB Contributor
    ]
    // roleAssignmentDescription: 'Directory Readers'
    // roleAssignmentDescription: 'Owner, SQL Server Contributor, SQL DB Contributor'
  }
}

output managedIdentityName string = identity.outputs.managedIdentityResoureName
