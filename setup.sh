#!/bin/bash
# setup.sh - Helper script to set up and run the app 

set -e

BOLD='\033[1m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' 

echo -e "${BOLD}==============================================${NC}"
echo -e "${BOLD}Negotiations API Docker Setup${NC}"
echo -e "${BOLD}==============================================${NC}"

if ! command -v docker &> /dev/null; then
    echo -e "${RED}ERROR: Docker is not installed.${NC}"
    echo "Please install Docker and try again."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}ERROR: Docker Compose is not installed.${NC}"
    echo "Please install Docker Compose and try again."
    exit 1
fi

docker info >/dev/null 2>&1
if [ $? -ne 0 ]; then
    echo -e "${RED}ERROR: Docker is not running.${NC}"
    echo "Please start Docker and try again."
    exit 1
fi

echo -e "${YELLOW}Building and starting containers...${NC}"
docker-compose down
docker-compose build
docker-compose up -d

echo -e "${YELLOW}Waiting for services to be ready...${NC}"
attempt=0
max_attempts=30
until $(curl --output /dev/null --silent --head --fail http://localhost:8080/swagger/index.html); do
    if [ ${attempt} -eq ${max_attempts} ]; then
        echo -e "${RED}Timed out waiting for API to be ready.${NC}"
        echo "Check logs with: docker-compose logs -f webapi"
        exit 1
    fi
    
    printf '.'
    attempt=$(($attempt+1))
    sleep 2
done

echo -e "\n${YELLOW}Verifying API health...${NC}"
health_status=$(curl -s http://localhost:8080/health | grep -o '"status":"Healthy"' || echo "")
if [[ -n "$health_status" ]]; then
    echo -e "${GREEN}API health check passed!${NC}"
else
    echo -e "${YELLOW}API health check returned non-healthy status. The application might still be initializing.${NC}"
    echo "Check API status with: curl http://localhost:8080/health"
fi

echo -e "\n${GREEN}Success! The Negotiations API is now running.${NC}"
echo -e "${BOLD}==============================================${NC}"
echo -e "API URL: ${GREEN}http://localhost:8080${NC}"
echo -e "Swagger UI: ${GREEN}http://localhost:8080/swagger${NC}"
echo -e "${BOLD}==============================================${NC}"
echo -e "Default admin credentials: username: ${GREEN}admin${NC}, password: ${GREEN}Admin123!${NC}"
echo -e "Default seller credentials: username: ${GREEN}seller${NC}, password: ${GREEN}Seller123!${NC}"
echo -e "${BOLD}==============================================${NC}"
echo -e "To view logs: ${YELLOW}docker-compose logs -f${NC}"
echo -e "To stop services: ${YELLOW}docker-compose down${NC}"
echo -e "${BOLD}==============================================${NC}"
