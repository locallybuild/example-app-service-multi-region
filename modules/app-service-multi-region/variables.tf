variable "name_prefix" {
  description = "The prefix used for all resources in this example."
  type        = string
}

variable "primary_location" {
  description = "The region the API runs in, alongside its primary Storage Account and Service Bus namespace."
  type        = string
}

variable "secondary_location" {
  description = "The second region, which gets its own Storage Account and Service Bus namespace."
  type        = string
}

variable "tags" {
  description = "A mapping of tags which should be applied to each of the resources."
  type        = map(string)
  default     = {}
}

variable "app_service_sku" {
  description = "App Service plan SKU."
  type        = string
  default     = "B1"
}
