#!/bin/bash

# Kubernetes Manifest Validation Script
# Validates YAML syntax and Kubernetes resource definitions

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Validating Kubernetes Manifests${NC}"
echo ""

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}Error: kubectl is not installed${NC}"
    exit 1
fi

# Function to validate a file
validate_file() {
    local file=$1
    echo -n "Validating ${file}... "
    
    if kubectl apply --dry-run=client -f "${file}" &> /dev/null; then
        echo -e "${GREEN}✓${NC}"
        return 0
    else
        echo -e "${RED}✗${NC}"
        kubectl apply --dry-run=client -f "${file}"
        return 1
    fi
}

# Function to validate kustomization
validate_kustomization() {
    local dir=$1
    echo -n "Validating kustomization in ${dir}... "
    
    if kubectl kustomize "${dir}" &> /dev/null; then
        echo -e "${GREEN}✓${NC}"
        return 0
    else
        echo -e "${RED}✗${NC}"
        kubectl kustomize "${dir}"
        return 1
    fi
}

# Validate base manifests
echo "Base Manifests:"
validate_file "configmap.yaml"
validate_file "deployment.yaml"
validate_file "service.yaml"
validate_file "hpa.yaml"
validate_file "pdb.yaml"
validate_file "ingress.yaml"
validate_file "servicemonitor.yaml"
validate_file "networkpolicy.yaml"

echo ""
echo "Kustomizations:"
validate_kustomization "."
validate_kustomization "overlays/development"
validate_kustomization "overlays/production"

echo ""
echo -e "${GREEN}All validations passed!${NC}"
