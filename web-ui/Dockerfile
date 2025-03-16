
FROM node:18 AS build

WORKDIR /app

COPY web-ui/package.json web-ui/package-lock.json ./
RUN npm install

COPY web-ui/ .

RUN npm run build -- --configuration=production && ls -lah www

FROM nginx:alpine

COPY --from=build /app/dist/web-ui/ /usr/share/nginx/html
COPY web-ui/nginx.conf /etc/nginx/conf.d/default.conf


EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
