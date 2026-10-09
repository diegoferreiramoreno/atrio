# Átrio

Fonte oficial de comunicação e distribuição de conteúdo entre o Centro Cultural São José Sanchez Del Rio, professores e famílias.

## Arquitetura e Plataforma

- **Backend:** .NET 10 / ASP.NET Core / Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`)
- **Frontend:** React 19 / TypeScript / Vite / TanStack Query / React Router
- **Banco de Dados:** PostgreSQL 17 (Npgsql, EF Core)
- **Infraestrutura:** Docker Compose, Caddy (reverse proxy same-origin)
- **Testes:** xUnit, FluentAssertions, Testcontainers (PostgreSQL real), NetArchTest (regras de dependência), Vitest, React Testing Library.

## Documentação

- Especificação Arquitetural: `docs/superpowers/specs/2026-10-02-atrio-architecture-design.md`
- Roadmap de Implementação: `docs/superpowers/plans/2026-10-02-atrio-v0.1-implementation-roadmap.md`
- Plano 01 (Foundation): `docs/superpowers/plans/2026-10-02-atrio-01-foundation-platform.md`
- Guia de Desenvolvimento: `docs/development.md`
- Guia de Arquitetura: `docs/architecture.md`

## Como Executar Localmente

### Pré-requisitos

- .NET SDK 10.0+
- Node.js 22+ / npm
- Docker Engine & Docker Compose

### Executando com Docker Compose

1. Copie o arquivo de exemplo de ambiente:
   ```bash
   cp .env.example .env
   ```
2. Inicie a composição de desenvolvimento:
   ```bash
   docker compose up -d --build
   ```
3. Verifique os status dos serviços:
   ```bash
   docker compose ps
   ```
4. Execute o smoke test:
   - Linux/macOS:
     ```bash
     ./scripts/smoke.sh
     ```
   - Windows (PowerShell):
     ```powershell
     .\scripts\smoke.ps1
     ```
5. Para encerrar os serviços:
   ```bash
   docker compose down
   ```

### Executando Testes Localmente

- Backend (Unitários, Arquitetura e Integração com Testcontainers):
  ```bash
  dotnet test apps/api/Atrio.slnx
  ```
- Frontend (Testes unitários e typecheck):
  ```bash
  npm --prefix apps/web test -- --run
  npm --prefix apps/web run typecheck
  npm --prefix apps/web run build
  ```
