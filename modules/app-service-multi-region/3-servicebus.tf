# ---------------------------------------------------------------------------
# Service Bus - once an order is stored, the API sends an "order-placed" message
# to the "orders" queue for anything downstream (fulfilment, emails) to pick up.
# ---------------------------------------------------------------------------
resource "azurerm_servicebus_namespace" "main" {
  for_each = local.regions

  name                = "${var.name_prefix}-${each.value}"
  resource_group_name = azurerm_resource_group.main[each.key].name
  location            = azurerm_resource_group.main[each.key].location
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_servicebus_queue" "orders" {
  for_each = local.regions

  name         = "orders"
  namespace_id = azurerm_servicebus_namespace.main[each.key].id

  # A message that fails processing this many times is moved to the
  # dead-letter sub-queue rather than being retried forever.
  max_delivery_count = 5
}
