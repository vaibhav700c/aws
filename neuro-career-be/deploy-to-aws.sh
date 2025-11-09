#!/bin/bash

# AWS Deployment Script for Neuro-Career Backend
# Make sure you have AWS CLI configured with appropriate permissions

set -e

# Configuration
AWS_REGION="us-east-1"
ECR_REPO_NAME="neuro-career-backend"
SERVICE_NAME="neuro-career-backend"

# Get AWS Account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)

echo "🚀 Starting AWS Backend Deployment..."
echo "AWS Account ID: $AWS_ACCOUNT_ID"
echo "Region: $AWS_REGION"

# Step 1: Create ECR repository (if it doesn't exist)
echo "📦 Creating ECR repository..."
aws ecr describe-repositories --repository-names $ECR_REPO_NAME --region $AWS_REGION 2>/dev/null || \
aws ecr create-repository --repository-name $ECR_REPO_NAME --region $AWS_REGION

# Step 2: Build Docker image
echo "🔨 Building Docker image..."
docker build -t $ECR_REPO_NAME .

# Step 3: Login to ECR
echo "🔐 Logging into ECR..."
aws ecr get-login-password --region $AWS_REGION | \
docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Step 4: Tag and push image
echo "📤 Pushing image to ECR..."
docker tag $ECR_REPO_NAME:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO_NAME:latest
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO_NAME:latest

# Step 5: Store API keys in Parameter Store (if not already stored)
echo "🔑 Setting up API keys in Parameter Store..."

# Check if parameters exist and create them if they don't
if ! aws ssm get-parameter --name "/neuro-career/assemblyai-key" --region $AWS_REGION 2>/dev/null; then
    echo "Please enter your AssemblyAI API key:"
    read -s ASSEMBLYAI_KEY
    aws ssm put-parameter --name "/neuro-career/assemblyai-key" --value "$ASSEMBLYAI_KEY" --type "SecureString" --region $AWS_REGION
fi

if ! aws ssm get-parameter --name "/neuro-career/gemini-key" --region $AWS_REGION 2>/dev/null; then
    echo "Please enter your Gemini API key:"
    read -s GEMINI_KEY
    aws ssm put-parameter --name "/neuro-career/gemini-key" --value "$GEMINI_KEY" --type "SecureString" --region $AWS_REGION
fi

if ! aws ssm get-parameter --name "/neuro-career/elevenlabs-key" --region $AWS_REGION 2>/dev/null; then
    echo "Please enter your ElevenLabs API key:"
    read -s ELEVENLABS_KEY
    aws ssm put-parameter --name "/neuro-career/elevenlabs-key" --value "$ELEVENLABS_KEY" --type "SecureString" --region $AWS_REGION
fi

# Step 6: Update apprunner-config.json with actual account ID
sed -i.bak "s/YOUR_ACCOUNT_ID/$AWS_ACCOUNT_ID/g" apprunner-config.json
sed -i.bak "s/YOUR_ACCOUNT_ID/$AWS_ACCOUNT_ID/g" apprunner.yaml

echo "✅ Backend deployment preparation complete!"
echo ""
echo "📋 Next steps:"
echo "1. Create App Runner service using AWS Console or CLI"
echo "2. Use the apprunner-config.json file for configuration"
echo "3. Your backend image: $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO_NAME:latest"
echo ""
echo "🔗 To create App Runner service via CLI:"
echo "aws apprunner create-service --cli-input-json file://apprunner-config.json --region $AWS_REGION"