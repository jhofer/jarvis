@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param my_acr_outputs_name string

param principalType string

param principalId string

resource my_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' existing = {
  name: my_acr_outputs_name
}