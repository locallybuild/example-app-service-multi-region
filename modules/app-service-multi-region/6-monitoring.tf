# ---------------------------------------------------------------------------
# Log Analytics - for storing the API's logs
# ---------------------------------------------------------------------------
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${var.name_prefix}-logs"
  resource_group_name = azurerm_resource_group.main["primary"].name
  location            = azurerm_resource_group.main["primary"].location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# ---------------------------------------------------------------------------
# Diagnostic settings - ship the API's Console Logs into the Log Analytics
# Workspace so they're KQL-queryable in the Monitoring -> Telemetry UI. Within
# the Locally Dashboard these can be queried using:
#   AppServiceConsoleLogs | order by TimeGenerated desc
# ---------------------------------------------------------------------------
resource "azurerm_monitor_diagnostic_setting" "api" {
  name                       = "console-logs"
  target_resource_id         = azurerm_linux_web_app.api.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  enabled_log {
    category = "AppServiceConsoleLogs"
  }
}
