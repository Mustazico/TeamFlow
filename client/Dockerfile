# Multi-stage build for TeamFlow client (Vite + React)
FROM node:22-alpine AS build
WORKDIR /app
COPY client/package.json client/package-lock.json* ./
RUN npm ci
COPY client/. .
RUN npm run build

FROM nginx:alpine AS runtime
# Use nginx's built-in template rendering so API_UPSTREAM can be set at runtime.
# Files under /etc/nginx/templates/*.template are envsubst'd into /etc/nginx/conf.d/ at start.
COPY client/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
