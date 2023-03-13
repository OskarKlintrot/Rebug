@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Application name - used as prefix for resource names')
param appName string

@description('Specifies sql admin login')
param sqlAdministratorLogin string

@description('Specifies sql admin password')
@secure()
param sqlAdministratorPassword string

var databaseName = 'rebugdb'

// Data resources
module db 'db.bicep' = {
  name: '${appName}-db-${uniqueString(resourceGroup().name)}'
  params: {
    location: location
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorPassword: sqlAdministratorPassword
    databaseName: databaseName
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
    appInsightsId: appInsights.outputs.appId
    databaseName: db.outputs.databaseName
    databaseServer: db.outputs.fullyQualifiedDomainName
  }
}

// // Managed Identity resources
// resource msi 'Microsoft.ManagedIdentity/identities@2023-01-31' existing = {
//   name: 'default'
// }

// resource roleassignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
//   name: guid(msi.id, resourceGroup().id, 'b24988ac-6180-42a0-ab88-20f7382dd24c')
//   properties: {
//     principalType: 'ServicePrincipal'
//     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
//     principalId: msi.properties.principalId
//   }
// }

// Monitor
module appInsights 'ai.bicep' = {
  name: '${appName}-ai-${uniqueString(resourceGroup().name)}'
  params: {
    location: location
    webSiteId: webapp.outputs.id
    webSiteName: webapp.outputs.name
  }
}
