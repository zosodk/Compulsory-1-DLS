# Stage 1: Build Angular App
FROM node:18 AS build

WORKDIR /app

# Copy package.json separately for better caching
COPY web-ui/package.json web-ui/package-lock.json ./
RUN npm install

# Copy the entire Angular project
COPY web-ui/ ./

# Build the Angular app
RUN npm run build

# Stage 2: Serve Angular App with NGINX
FROM nginx:latest

RUN rm -rf /usr/share/nginx/html/*

# ✅ Copy only the built Angular files (dist/web-ui) to NGINX root
COPY --from=build /app/dist/web-ui/ /usr/share/nginx/html/


EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
