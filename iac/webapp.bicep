@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Application name - used as prefix for resource names')
param appName string

@description('Specifies app plan SKU')
param skuName string = 'B1'

@description('Specifies app plan capacity')
param skuCapacity int = 2

// Web App resources
resource hostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'hostingplan${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: skuName
    capacity: skuCapacity
  }
}

resource webSite 'Microsoft.Web/sites@2022-03-01' = {
  name: appName
  location: location
  tags: {
    'hidden-related:${hostingPlan.id}': 'empty'
    displayName: 'Website'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: 'v7.0'
      windowsFxVersion: 'DOTNET|7.0'
      use32BitWorkerProcess: false
      // https://github.com/Azure/bicep-types-az/issues/1393
      metadata :[
        {
          name:'CURRENT_STACK'
          value:'dotnet'
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output name string = webSite.name
output id string = webSite.id
output managedIdentity string = webSite.identity.principalId
