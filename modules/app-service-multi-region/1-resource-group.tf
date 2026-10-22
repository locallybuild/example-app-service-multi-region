# ---------------------------------------------------------------------------
# Resource Groups - one per region, each holding that region's resources.
# ---------------------------------------------------------------------------
resource "azurerm_resource_group" "main" {
  for_each = local.regions

  name     = "${var.name_prefix}-${each.value}"
  location = each.value
  tags     = var.tags
}
