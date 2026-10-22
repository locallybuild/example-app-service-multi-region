output "api_url" {
  description = "The base URL of the orders API."
  value       = module.app-service-multi-region.api_url
}

output "resource_groups" {
  description = "The resource group in each region."
  value       = module.app-service-multi-region.resource_groups
}

output "storage_accounts" {
  description = "The Storage Account in each region."
  value       = module.app-service-multi-region.storage_accounts
}

output "servicebus_namespaces" {
  description = "The Service Bus namespace in each region."
  value       = module.app-service-multi-region.servicebus_namespaces
}
