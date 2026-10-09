FROM node:22-bookworm-slim AS build
WORKDIR /app

ENV NODE_TLS_REJECT_UNAUTHORIZED=0

COPY apps/web/package*.json ./
RUN npm config set strict-ssl false && npm ci

COPY apps/web/ ./
RUN npm run build

FROM caddy:2-alpine AS runtime
COPY --from=build /app/dist /usr/share/caddy

RUN printf ":80 {\n    root * /usr/share/caddy\n    file_server\n    try_files {path} /index.html\n}\n" > /etc/caddy/Caddyfile

EXPOSE 80
CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile"]
