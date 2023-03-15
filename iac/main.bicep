targetScope = 'resourceGroup'

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
    roleDefinitionIds: [ '8e3af657-a8ff-443c-a75c-2fe8c4bcb635', '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec', '6d8ee4ec-f05a-4a1d-8b00-a9b17e38b437' ]
    roleAssignmentDescription: 'Owner, SQL Server Contributor, SQL DB Contributor'
    // roleDefinitionIds: [ '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec', '6d8ee4ec-f05a-4a1d-8b00-a9b17e38b437' ]
    // roleAssignmentDescription: 'SQL Server Contributor, SQL DB Contributor'
  }
}
