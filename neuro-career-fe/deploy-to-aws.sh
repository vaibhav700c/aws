#!/bin/bash

# AWS Frontend Deployment Script for S3 + CloudFront
# Make sure you have AWS CLI configured

set -e

# Configuration - UPDATE THESE VALUES
BUCKET_NAME="neuro-career-frontend"
CLOUDFRONT_DISTRIBUTION_ID=""  # Add after creating CloudFront distribution
AWS_REGION="us-east-1"
BACKEND_URL=""  # Your App Runner or ALB URL

echo "🚀 Starting AWS Frontend Deployment..."

# Step 1: Check if bucket exists, create if not
echo "📦 Setting up S3 bucket..."
if ! aws s3 ls "s3://$BUCKET_NAME" 2>/dev/null; then
    aws s3 mb s3://$BUCKET_NAME --region $AWS_REGION
    
    # Configure bucket for static website hosting
    aws s3 website s3://$BUCKET_NAME --index-document index.html --error-document 404.html
    
    # Set public read policy
    cat > bucket-policy.json << EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::$BUCKET_NAME/*"
        }
    ]
}
EOF
    
    aws s3api put-bucket-policy --bucket $BUCKET_NAME --policy file://bucket-policy.json
    rm bucket-policy.json
fi

# Step 2: Update environment variables
if [ -n "$BACKEND_URL" ]; then
    echo "🔧 Updating production environment..."
    sed -i.bak "s|https://your-aws-backend-url-here|$BACKEND_URL|g" .env.production
fi

# Step 3: Build the application
echo "🔨 Building Next.js application..."
npm run build

# Step 4: Deploy to S3
echo "📤 Deploying to S3..."
aws s3 sync out/ s3://$BUCKET_NAME --delete

# Step 5: Invalidate CloudFront (if distribution exists)
if [ -n "$CLOUDFRONT_DISTRIBUTION_ID" ]; then
    echo "🔄 Invalidating CloudFront cache..."
    aws cloudfront create-invalidation --distribution-id $CLOUDFRONT_DISTRIBUTION_ID --paths "/*"
fi

echo "✅ Frontend deployment complete!"
echo ""
echo "📋 Next steps:"
echo "1. If this is your first deployment, create a CloudFront distribution"
echo "2. Point CloudFront origin to: $BUCKET_NAME.s3-website-$AWS_REGION.amazonaws.com"
echo "3. Update CLOUDFRONT_DISTRIBUTION_ID in this script"
echo "4. Update BACKEND_URL in this script with your actual backend URL"
echo ""
echo "🌐 S3 Website URL: http://$BUCKET_NAME.s3-website-$AWS_REGION.amazonaws.com"
echo "📝 Don't forget to update CORS in your backend with the CloudFront domain!"