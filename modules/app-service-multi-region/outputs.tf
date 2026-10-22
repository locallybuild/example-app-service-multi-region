output "api_url" {
  description = "The base URL of the orders API."
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "resource_groups" {
  description = "The resource group in each region."
  value       = { for k, rg in azurerm_resource_group.main : rg.location => rg.name }
}

output "storage_accounts" {
  description = "The Storage Account in each region."
  value       = { for k, sa in azurerm_storage_account.main : sa.location => sa.name }
}

output "servicebus_namespaces" {
  description = "The Service Bus namespace in each region."
  value       = { for k, ns in azurerm_servicebus_namespace.main : ns.location => ns.name }
}
