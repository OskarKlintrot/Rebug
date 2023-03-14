@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Web site name')
param webSiteName string

@description('Web site id')
param webSiteId string

param logAnalyticsWorkspace string = '${uniqueString(resourceGroup().id)}la'

// Monitor
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspace
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'AppInsights${webSiteName}'
  location: location
  tags: {
    'hidden-link:${webSiteId}': 'Resource'
    displayName: 'AppInsightsComponent'
  }
  kind: 'web'
  properties: {
    WorkspaceResourceId: logAnalytics.id
    Application_Type: 'web'
  }
}

output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
