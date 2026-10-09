FROM node:22-bookworm-slim AS build
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*

# Configura certificados adicionais confiáveis da infraestrutura/ambiente (ex.: CA de inspeção TLS corporativa em redes de desenvolvimento)
# permitindo autenticar endpoints HTTPS (como npm registry) mantendo validação estrita de certificados TLS sem bypasses
COPY infra/certificates/ /usr/local/share/ca-certificates/
RUN update-ca-certificates
ENV NODE_EXTRA_CA_CERTS=/etc/ssl/certs/ca-certificates.crt

COPY apps/web/package*.json ./
RUN npm ci

COPY apps/web/ ./
RUN npm run build

FROM caddy:2-alpine AS runtime
COPY --from=build /app/dist /usr/share/caddy

RUN printf ":80 {\n    root * /usr/share/caddy\n    file_server\n    try_files {path} /index.html\n}\n" > /etc/caddy/Caddyfile

EXPOSE 80
CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile"]
