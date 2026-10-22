terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.0"
    }
  }
}

provider "azurerm" {
  features {}
}

module "app-service-multi-region" {
  source = "../../modules/app-service-multi-region"

  name_prefix        = "locally-example-orders"
  primary_location   = "london"
  secondary_location = "paris"
  tags = {
    ProvisionedVia = "Terraform"
  }
}
