#!/bin/bash

# API Gateway Kubernetes Deployment Script
# Usage: ./deploy.sh [environment] [action]
# Example: ./deploy.sh production apply

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${1:-development}
ACTION=${2:-apply}
NAMESPACE=${ENVIRONMENT}

echo -e "${GREEN}API Gateway Kubernetes Deployment${NC}"
echo "Environment: ${ENVIRONMENT}"
echo "Action: ${ACTION}"
echo "Namespace: ${NAMESPACE}"
echo ""

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(development|staging|production)$ ]]; then
    echo -e "${RED}Error: Invalid environment. Must be development, staging, or production${NC}"
    exit 1
fi

# Validate action
if [[ ! "$ACTION" =~ ^(apply|delete|diff|dry-run)$ ]]; then
    echo -e "${RED}Error: Invalid action. Must be apply, delete, diff, or dry-run${NC}"
    exit 1
fi

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}Error: kubectl is not installed${NC}"
    exit 1
fi

# Check if kustomize is installed
if ! command -v kustomize &> /dev/null; then
    echo -e "${YELLOW}Warning: kustomize is not installed. Using kubectl kustomize instead${NC}"
    KUSTOMIZE_CMD="kubectl kustomize"
else
    KUSTOMIZE_CMD="kustomize build"
fi

# Check if namespace exists
if ! kubectl get namespace ${NAMESPACE} &> /dev/null; then
    echo -e "${YELLOW}Namespace ${NAMESPACE} does not exist. Creating...${NC}"
    kubectl create namespace ${NAMESPACE}
fi

# Check if secrets exist
if [[ "$ACTION" == "apply" ]] && ! kubectl get secret api-gateway-secrets -n ${NAMESPACE} &> /dev/null; then
    echo -e "${YELLOW}Warning: Secret 'api-gateway-secrets' does not exist in namespace ${NAMESPACE}${NC}"
    echo "Please create secrets before deploying:"
    echo "  kubectl create secret generic api-gateway-secrets -n ${NAMESPACE} \\"
    echo "    --from-literal=redis-connection-string='your-redis-connection'"
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Determine overlay path
if [[ -d "overlays/${ENVIRONMENT}" ]]; then
    OVERLAY_PATH="overlays/${ENVIRONMENT}"
    echo "Using overlay: ${OVERLAY_PATH}"
else
    OVERLAY_PATH="."
    echo "Using base configuration (no overlay found)"
fi

# Execute action
case $ACTION in
    apply)
        echo -e "${GREEN}Applying Kubernetes manifests...${NC}"
        ${KUSTOMIZE_CMD} ${OVERLAY_PATH} | kubectl apply -f -
        
        echo ""
        echo -e "${GREEN}Deployment successful!${NC}"
        echo ""
        echo "Checking deployment status..."
        kubectl rollout status deployment/api-gateway -n ${NAMESPACE} --timeout=5m
        
        echo ""
        echo "Pods:"
        kubectl get pods -l app=api-gateway -n ${NAMESPACE}
        
        echo ""
        echo "Service:"
        kubectl get svc api-gateway -n ${NAMESPACE}
        
        echo ""
        echo "HPA:"
        kubectl get hpa -l app=api-gateway -n ${NAMESPACE}
        ;;
        
    delete)
        echo -e "${RED}Deleting Kubernetes resources...${NC}"
        read -p "Are you sure you want to delete all resources? (y/N) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            ${KUSTOMIZE_CMD} ${OVERLAY_PATH} | kubectl delete -f -
            echo -e "${GREEN}Resources deleted${NC}"
        else
            echo "Deletion cancelled"
        fi
        ;;
        
    diff)
        echo -e "${YELLOW}Showing diff...${NC}"
        ${KUSTOMIZE_CMD} ${OVERLAY_PATH} | kubectl diff -f - || true
        ;;
        
    dry-run)
        echo -e "${YELLOW}Dry run (no changes will be applied)...${NC}"
        ${KUSTOMIZE_CMD} ${OVERLAY_PATH} | kubectl apply -f - --dry-run=client
        echo -e "${GREEN}Dry run completed${NC}"
        ;;
esac

echo ""
echo -e "${GREEN}Done!${NC}"
