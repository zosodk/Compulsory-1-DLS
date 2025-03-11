
FROM node:18 AS build

WORKDIR /app

COPY web-ui/package.json web-ui/package-lock.json ./
RUN npm install

COPY web-ui/ .

RUN npm run build -- --configuration=production && ls -lah www

FROM nginx:alpine

COPY --from=build /app/www /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
