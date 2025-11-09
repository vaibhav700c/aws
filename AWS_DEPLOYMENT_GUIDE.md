# AWS Deployment Guide for Neuro-Career

## Backend Deployment (FastAPI)

### Option 1: AWS App Runner (Recommended)

1. **Build and push to ECR:**
```bash
# Navigate to backend
cd neuro-career-be

# Build Docker image
docker build -t neuro-career-backend .

# Create ECR repository
aws ecr create-repository --repository-name neuro-career-backend --region us-east-1

# Get login token and login
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com

# Tag and push image
docker tag neuro-career-backend:latest YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/neuro-career-backend:latest
docker push YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/neuro-career-backend:latest
```

2. **Deploy to App Runner:**
```bash
# Create apprunner.yaml in backend directory
# Then use AWS CLI or Console to create App Runner service
aws apprunner create-service --cli-input-json file://apprunner-config.json
```

### Option 2: AWS ECS Fargate

1. **Create ECS cluster and task definition**
2. **Use the provided Dockerfile**
3. **Set up Application Load Balancer**

## Frontend Deployment

### Option 1: AWS S3 + CloudFront (Recommended)

```bash
# Navigate to frontend
cd neuro-career-fe

# Update .env.production with your backend URL
# Build the application
npm run build

# Deploy to S3
aws s3 sync out/ s3://your-bucket-name --delete

# Invalidate CloudFront cache
aws cloudfront create-invalidation --distribution-id YOUR_DISTRIBUTION_ID --paths "/*"
```

### Option 2: AWS Amplify

1. **Connect GitHub repository**
2. **Amplify will auto-detect Next.js**
3. **Set environment variables in Amplify console**

## Environment Variables Setup

### Backend (AWS Systems Manager Parameter Store or Secrets Manager):
- ASSEMBLYAI_API_KEY
- GEMINI_API_KEY  
- ELEVENLABS_API_KEY

### Frontend (Set in deployment platform):
- NEXT_PUBLIC_API_BASE_URL

## Security Considerations

1. **Use HTTPS everywhere**
2. **Store API keys securely (AWS Secrets Manager)**
3. **Configure proper CORS origins**
4. **Use IAM roles for service authentication**
5. **Enable AWS WAF for additional protection**

## Cost Optimization

1. **Use AWS App Runner for backend (pay-per-use)**
2. **Use S3 + CloudFront for frontend (very low cost)**
3. **Set up CloudWatch alarms for cost monitoring**
4. **Use AWS Cost Explorer to track expenses**