# ---------------------------------------------------------------------------
# Storage Accounts - the API writes each order as a JSON blob in the "orders"
# container, and reads it back from there.
# ---------------------------------------------------------------------------
resource "azurerm_storage_account" "main" {
  for_each = local.regions

  name                            = "${local.storage_prefix}${substr(each.value, 0, 3)}"
  resource_group_name             = azurerm_resource_group.main[each.key].name
  location                        = azurerm_resource_group.main[each.key].location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  tags                            = var.tags
}

resource "azurerm_storage_container" "orders" {
  for_each = local.regions

  name               = "orders"
  storage_account_id = azurerm_storage_account.main[each.key].id
}
