terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 1.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

provider "azapi" {}

# Generate a random suffix for unique resource names
resource "random_id" "suffix" {
  byte_length = 4
}

locals {
  resource_suffix = random_id.suffix.hex
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${var.resource_group_name}-${local.resource_suffix}"
  location = var.location

  tags = {
    Environment  = "Development"
    Project      = var.project_name
    azd-env-name = var.project_name
  }
}

# AI Foundry Hub
resource "azapi_resource" "ai_foundry_hub" {
  type      = "Microsoft.MachineLearningServices/workspaces@2024-04-01"
  name      = "${var.project_name}-hub-${local.resource_suffix}"
  location  = azurerm_resource_group.main.location
  parent_id = azurerm_resource_group.main.id

  body = jsonencode({
    properties = {
      description  = "AI Foundry Hub for ${var.project_name}"
      friendlyName = "${var.project_name} AI Hub"
      workspaceHubConfig = {
        defaultWorkspaceResourceGroup = azurerm_resource_group.main.id
      }
    }
    kind = "Hub"
  })

  tags = {
    Environment = "Development"
    Project     = var.project_name
  }
}

# AI Foundry Project
resource "azapi_resource" "ai_foundry_project" {
  type      = "Microsoft.MachineLearningServices/workspaces@2024-04-01"
  name      = "${var.project_name}-project-${local.resource_suffix}"
  location  = azurerm_resource_group.main.location
  parent_id = azurerm_resource_group.main.id

  body = jsonencode({
    properties = {
      description   = "AI Foundry Project for ${var.project_name}"
      friendlyName  = "${var.project_name} AI Project"
      hubResourceId = azapi_resource.ai_foundry_hub.id
    }
    kind = "Project"
  })

  tags = {
    Environment = "Development"
    Project     = var.project_name
  }

  depends_on = [azapi_resource.ai_foundry_hub]
}
