# ---------------------------------------------------------------------------
# App Service Plan - houses the API.
# ---------------------------------------------------------------------------
resource "azurerm_service_plan" "main" {
  name                = "${var.name_prefix}-plan"
  resource_group_name = azurerm_resource_group.main["primary"].name
  location            = azurerm_resource_group.main["primary"].location
  os_type             = "Linux"
  sku_name            = var.app_service_sku
}

# ---------------------------------------------------------------------------
# Orders API - takes orders over HTTP, stores them in Blob Storage and
# announces each one on the Service Bus queue.
# ---------------------------------------------------------------------------
resource "azurerm_linux_web_app" "api" {
  name                = "${var.name_prefix}-api"
  resource_group_name = azurerm_resource_group.main["primary"].name
  location            = azurerm_service_plan.main.location
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true
  tags                = var.tags

  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false

  site_config {
    always_on                         = true
    health_check_path                 = "/healthz"
    health_check_eviction_time_in_min = 2

    application_stack {
      docker_image_name        = "${local.image_name}:${local.image_tag}"
      docker_registry_url      = "https://${azurerm_container_registry.main.login_server}"
      docker_registry_username = azurerm_container_registry.main.admin_username
      docker_registry_password = azurerm_container_registry.main.admin_password
    }
  }

  app_settings = {
    # App Service routes public traffic to the container on this port, and the
    # app binds to it (12-factor port binding).
    WEBSITES_PORT = tostring(local.app_port)
    PORT          = tostring(local.app_port)

    # The Storage Account and Service Bus namespace in each region. ASP.NET Core
    # reads these as ConnectionStrings:StoragePrimary and so on.
    ConnectionStrings__StoragePrimary      = azurerm_storage_account.main["primary"].primary_blob_connection_string
    ConnectionStrings__StorageSecondary    = azurerm_storage_account.main["secondary"].primary_blob_connection_string
    ConnectionStrings__ServiceBusPrimary   = azurerm_servicebus_namespace.main["primary"].default_primary_connection_string
    ConnectionStrings__ServiceBusSecondary = azurerm_servicebus_namespace.main["secondary"].default_primary_connection_string

    # Both regions use the same container and queue names.
    Orders__Container = azurerm_storage_container.orders["primary"].name
    Orders__Queue     = azurerm_servicebus_queue.orders["primary"].name
  }

  # The image must exist in the registry before the app starts.
  depends_on = [
    terraform_data.build_and_push,
  ]
}
