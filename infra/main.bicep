// ============================================================================
// ScholarPath — Azure infrastructure (Bicep)
//
// Declares every resource the platform needs:
//   - App Service Plan (Linux)
//   - App Service for the .NET 10 API
//   - Static Web App for the React client
//   - Azure SQL (logical server + database)
//   - Azure Cache for Redis
//   - Key Vault (connection strings; the RS256 JWT signing key is uploaded
//     manually post-provision — see docs/deployment.md §5)
//   - Application Insights (+ Log Analytics workspace)
//
// The API App Service is given a system-assigned managed identity, granted
// RBAC read access to the Key Vault, and configured with Key Vault
// references so secrets never appear in app settings.
//
// Deploy:
//   az group create --name <rg> --location <location>
//   az deployment group create \
//     --resource-group <rg> \
//     --template-file infra/main.bicep \
//     --parameters infra/main.parameters.json \
//     --parameters sqlAdminPassword=<value>
// ============================================================================

targetScope = 'resourceGroup'

// ─── Parameters ─────────────────────────────────────────────────────────────

@description('Base name for all resources. Lowercase letters and digits; keep short — used to derive globally-unique names.')
@minLength(3)
@maxLength(17)
param baseName string = 'scholarpath'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Deployment environment discriminator (suffixed onto resource names).')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environmentName string = 'prod'

@description('SKU for the App Service Plan. B1 is fine for a graduation demo; P1v3 for production load.')
@allowed([
  'B1'
  'B2'
  'S1'
  'P0v3'
  'P1v3'
])
param appServicePlanSku string = 'B1'

@description('Azure SQL administrator login name.')
param sqlAdminLogin string = 'scholarpathadmin'

@description('Azure SQL administrator password. Pass at deploy time — never commit a value.')
@secure()
@minLength(12)
param sqlAdminPassword string

@description('SKU name for the Azure SQL database.')
param sqlDatabaseSku string = 'Basic'

@description('SKU for Azure Cache for Redis. family C / capacity 0 == Basic 250 MB.')
@allowed([
  'Basic'
  'Standard'
])
param redisSkuName string = 'Basic'

@description('Redis capacity (0-6 for Basic/Standard C-family).')
@minValue(0)
@maxValue(6)
param redisCapacity int = 0

@description('Name of the Key Vault secret holding the RS256 JWT signing key (an RSA private key in PEM format). The operator uploads this secret manually after provisioning — see docs/deployment.md §5.')
param jwtKeyName string = 'scholarpath-jwt-signing'

@description('Object ID of an Entra ID user/group to grant Key Vault Secrets Officer (for managing secrets). Optional.')
param keyVaultAdminObjectId string = ''

@description('JWT issuer URL.')
param jwtIssuer string = 'https://${baseName}-${environmentName}.example.com'

@description('JWT audience URL.')
param jwtAudience string = 'https://${baseName}-${environmentName}.example.com'

@description('Allowed CORS origin for the client (the Static Web App URL). Set after first deploy if not known yet.')
param clientCorsOrigin string = ''

@description('Tags applied to every resource.')
param tags object = {
  project: 'ScholarPath'
  environment: environmentName
  managedBy: 'bicep'
}

// ─── Derived names ──────────────────────────────────────────────────────────

var nameSuffix = '${baseName}-${environmentName}'
// uniqueString keeps globally-scoped names (SQL server, Key Vault, App Service) collision-free per RG.
var uniqueSuffix = take(uniqueString(resourceGroup().id, environmentName), 6)

var appServicePlanName = 'plan-${nameSuffix}'
var apiAppName = 'app-${nameSuffix}-api'
var staticWebAppName = 'swa-${nameSuffix}-client'
var sqlServerName = 'sql-${nameSuffix}-${uniqueSuffix}'
var sqlDatabaseName = 'sqldb-${baseName}'
var redisName = 'redis-${nameSuffix}'
// Key Vault names: 3-24 chars, alphanumeric + hyphens.
var keyVaultName = take('kv-${nameSuffix}-${uniqueSuffix}', 24)
var logAnalyticsName = 'log-${nameSuffix}'
var appInsightsName = 'appi-${nameSuffix}'

// Built-in Azure RBAC role definition IDs.
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User (read)
var keyVaultSecretsOfficerRoleId = 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7' // Key Vault Secrets Officer (read/write)

// ─── Log Analytics + Application Insights ───────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

// ─── Key Vault ──────────────────────────────────────────────────────────────

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // RBAC authorization model — access is governed by role assignments below.
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
  }
}

// NOTE: the RS256 JWT signing key (an RSA private key, PEM) is NOT created
// here. It is a multi-line secret the operator uploads manually after
// provisioning, under the name `jwtKeyName` — see docs/deployment.md §5.
// The API reads it at runtime via its managed identity (Key Vault Secrets
// User), so it never appears in App Service configuration.

// Database connection string secret.
resource sqlConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    contentType: 'text/plain'
  }
}

// Redis connection string secret.
resource redisConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Redis--ConnectionString'
  properties: {
    value: '${redis.properties.hostName}:${redis.properties.sslPort},password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
    contentType: 'text/plain'
  }
}

// Grant the API App Service's managed identity read access to secrets.
resource apiKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, apiApp.id, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Optionally grant a human admin secret-management rights.
resource adminKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(keyVaultAdminObjectId)) {
  scope: keyVault
  name: guid(keyVault.id, keyVaultAdminObjectId, keyVaultSecretsOfficerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsOfficerRoleId)
    principalId: keyVaultAdminObjectId
    principalType: 'User'
  }
}

// ─── Azure SQL ──────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: sqlDatabaseSku
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

// Allow other Azure services (the App Service) to reach the SQL server.
// The 0.0.0.0 "start/end" pair is the documented Azure-internal rule.
resource sqlAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ─── Azure Cache for Redis ──────────────────────────────────────────────────

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: redisName
  location: location
  tags: tags
  properties: {
    sku: {
      name: redisSkuName
      family: 'C'
      capacity: redisCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisVersion: '6'
  }
}

// ─── App Service Plan + API App Service ─────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: appServicePlanSku
  }
  properties: {
    reserved: true // required for Linux plans
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: appServicePlanSku != 'B1' // AlwaysOn is unsupported on Basic B1
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/health'
      cors: {
        allowedOrigins: empty(clientCorsOrigin) ? [] : [clientCorsOrigin]
        supportCredentials: true
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        // Connection string via Key Vault reference — the runtime resolves
        // this with the App Service's managed identity at startup.
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${sqlConnectionSecret.properties.secretUri})'
        }
        // RS256 JWT: the API reads the RSA signing key from Key Vault itself
        // at runtime (DefaultAzureCredential + SecretClient), so no Key Vault
        // *reference* here — just point it at the vault and the secret name.
        {
          name: 'Jwt__KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'Jwt__KeyName'
          value: jwtKeyName
        }
        {
          name: 'Jwt__Issuer'
          value: jwtIssuer
        }
        {
          name: 'Jwt__Audience'
          value: jwtAudience
        }
        {
          name: 'Redis__Enabled'
          value: 'true'
        }
        {
          name: 'Redis__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${redisConnectionSecret.properties.secretUri})'
        }
        // Hangfire recurring jobs run in-process; enable in the deployed app.
        {
          name: 'Hangfire__Enabled'
          value: 'true'
        }
        {
          name: 'Hangfire__DashboardEnabled'
          value: 'true'
        }
        {
          name: 'Cors__AllowedOrigins__0'
          value: clientCorsOrigin
        }
        {
          name: 'App__ClientUrl'
          value: clientCorsOrigin
        }
        // Application Insights.
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

// ─── Static Web App (React client) ──────────────────────────────────────────

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  // Static Web Apps are only available in a subset of regions; Free tier
  // is broadly available. Override if your RG region lacks SWA support.
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // CI/CD is driven by the deploy.yml workflow (deployment token), not by
    // the built-in GitHub integration — so no repo wiring here.
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
  }
}

// ─── Outputs ────────────────────────────────────────────────────────────────

@description('Default hostname of the API App Service.')
output apiAppDefaultHostname string = apiApp.properties.defaultHostName

@description('Resource name of the API App Service (use as AZURE_API_APP_NAME).')
output apiAppName string = apiApp.name

@description('Default hostname of the client Static Web App.')
output staticWebAppHostname string = staticWebApp.properties.defaultHostname

@description('Resource name of the Static Web App.')
output staticWebAppName string = staticWebApp.name

@description('Fully-qualified domain name of the Azure SQL server.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Name of the Azure SQL database.')
output sqlDatabaseName string = sqlDatabase.name

@description('Key Vault name.')
output keyVaultName string = keyVault.name

@description('Key Vault URI.')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Application Insights connection string.')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Principal (object) ID of the API App Service managed identity.')
output apiAppPrincipalId string = apiApp.identity.principalId
