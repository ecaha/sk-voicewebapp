# Setup Instructions

## Prerequisites
- .NET 8.0 SDK
- Azure subscription with AI Foundry project
- Azure Speech Services resource

## Configuration Setup

1. **Copy the template files:**
   ```bash
   cp src/VoiceChatBot/appsettings.json.template src/VoiceChatBot/appsettings.json
   cp src/VoiceChatBot/appsettings.Development.json.template src/VoiceChatBot/appsettings.Development.json
   ```

2. **Configure Azure AI Foundry:**
   - Update `AzureAI:Endpoint` with your AI Foundry project endpoint
   - Update `AzureAI:ApiKey` with your API key
   - Update `AzureAI:ModelDeploymentName` with your model deployment name (e.g., "gpt-4o")

3. **Configure Azure Speech Services:**
   - Update `AzureSpeech:SubscriptionKey` with your Speech Services key
   - Update `AzureSpeech:Region` with your Azure region (e.g., "swedencentral")

4. **Deploy Infrastructure (Optional):**
   If you want to deploy to Azure, navigate to the infrastructure folder and use Terraform:
   ```bash
   cd infrastructure
   terraform init
   terraform plan
   terraform apply
   ```

## Running the Application

1. Navigate to the application directory:
   ```bash
   cd src/VoiceChatBot
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser to `http://localhost:5225`

## Features
- **Voice Recording**: Click the microphone button to start/stop recording
- **Text Chat**: Type messages in the text input
- **AI Responses**: Get responses from Azure AI Foundry (GPT-4o)
- **Text-to-Speech**: Hear AI responses spoken aloud

## Troubleshooting
- Ensure your Azure services are properly configured and accessible
- Check that microphone permissions are granted in your browser
- Verify audio playback is enabled in your browser settings
