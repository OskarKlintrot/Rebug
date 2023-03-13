@description('Application name - used as prefix for resource names')
param appName string

@description('APPINSIGHTS_INSTRUMENTATIONKEY')
param appInsightsId string

@description('Database server to use')
param databaseServer string

@description('Database name')
param databaseName string

// Web App config
resource webSiteConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  name: '${appName}/web'
  properties: {
    appSettings: [
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appInsightsId
      }
    ]
  }
}

resource connectionString 'Microsoft.Web/sites/config@2022-03-01' = {
  name: '${appName}/connectionstrings'
  properties: {
    DbConnection: {
      value: 'Server=${databaseServer};Authentication=Active Directory Managed Identity;Database=${databaseName};MultipleActiveResultSets=True;App=EntityFramework'
      type: 'SQLAzure'
    }
  }
}
