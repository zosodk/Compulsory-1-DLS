FROM node:18 AS build

WORKDIR /app

COPY web-ui/package.json web-ui/package-lock.json ./
RUN npm install

COPY web-ui/ .

RUN npm run build -- --configuration=production && ls -lah www

FROM nginx:alpine
RUN rm /etc/nginx/conf.d/default.conf 

#COPY ./nginx/default.conf /etc/nginx/conf.d/default.conf
#COPY --from=build /app/dist /usr/share/nginx/html/
COPY --from=build /app/dist/web-ui/ /usr/share/nginx/html
COPY web-ui/nginx.conf /etc/nginx/nginx.conf


EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
