@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Web site name')
param webSiteName string

@description('Web site id')
param webSiteId string

// Monitor
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'AppInsights${webSiteName}'
  location: location
  tags: {
    'hidden-link:${webSiteId}': 'Resource'
    displayName: 'AppInsightsComponent'
  }
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
