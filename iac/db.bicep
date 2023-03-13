@description('Specifies region for all resources')
param location string = resourceGroup().location

@description('Name of database - default is rebugdb')
param databaseName string = 'rebugdb'

@description('Specifies sql admin login')
param sqlAdministratorLogin string

@description('Specifies sql admin SID (object ID)')
param sqlAdministratorSid string

// Data resources
resource sqlserver 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'sqlserver${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    version: '12.0'
    administrators: {
      azureADOnlyAuthentication: true
      administratorType: 'ActiveDirectory'
      login: sqlAdministratorLogin
      sid: sqlAdministratorSid
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

output databaseName string = sqlserver::database.name
output fullyQualifiedDomainName string = sqlserver.properties.fullyQualifiedDomainName
