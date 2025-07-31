variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-voice-chat-bot"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
}

variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "voice-chat-bot"
}
