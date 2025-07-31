output "resource_group_name" {
  description = "Name of the created resource group"
  value       = azurerm_resource_group.main.name
}

output "ai_foundry_hub_name" {
  description = "Name of the AI Foundry Hub"
  value       = azapi_resource.ai_foundry_hub.name
}

output "ai_foundry_project_name" {
  description = "Name of the AI Foundry Project"
  value       = azapi_resource.ai_foundry_project.name
}

output "ai_foundry_discovery_url" {
  description = "AI Foundry discovery URL"
  value       = "https://ai.azure.com"
}

output "next_steps" {
  description = "Next steps to complete the setup"
  value       = <<-EOT
    
    🎯 NEXT STEPS TO COMPLETE SETUP:
    
    1. 📋 Note your resource details:
       - Resource Group: ${azurerm_resource_group.main.name}
       - AI Hub: ${azapi_resource.ai_foundry_hub.name}
       - AI Project: ${azapi_resource.ai_foundry_project.name}
    
    2. 🚀 Deploy GPT-4o model:
       - Visit: https://ai.azure.com
       - Navigate to your project: ${azapi_resource.ai_foundry_project.name}
       - Go to "Deployments" → "Deploy model"
       - Select "gpt-4o" from Azure OpenAI models
       - Use deployment name: "gpt-4o"
    
    3. 🔧 Get your project endpoint:
       - In AI Studio, go to project overview
       - Copy the project endpoint URL
       - Update appsettings.json with this endpoint
    
    4. 🎤 Create Speech Services:
       - Go to Azure Portal
       - Create "Speech Services" resource
       - Note subscription key and region
       - Update appsettings.json with speech settings
    
    5. ▶️ Run the application:
       - cd src/VoiceChatBot
       - dotnet run
       - Visit https://localhost:5001
    
    EOT
}
