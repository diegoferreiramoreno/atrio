# Átrio — Arquitetura de Software e Regras de Dependência

Este documento resume a topologia arquitetural da plataforma Átrio e as regras estritas de dependência entre as camadas do sistema.

A especificação normativa integral e aprovada está disponível em:
[`docs/superpowers/specs/2026-10-02-atrio-architecture-design.md`](superpowers/specs/2026-10-02-atrio-architecture-design.md)

---

## 1. Princípios Arquiteturais Centrais

1. **Monólito Modular:** Os módulos funcionais compartilham o mesmo processo de execução e o mesmo banco de dados PostgreSQL, mas mantêm limites bem definidos e desacoplados.
2. **Clean Architecture & DDD Pragmático:** Separação estrita de responsabilidades entre regras de negócio, casos de uso, persistência externa e exposição HTTP.
3. **Segurança Server-Side:** Autorização rigorosa por recurso e contexto; a camada de apresentação nunca toma decisões finais de permissão.
4. **Infraestrutura Mínima e Portável:** Sem mensageria externa pesada (RabbitMQ/Kafka) ou caches distribuídos (Redis) no MVP v0.1. Apenas PostgreSQL 17, Caddy como reverse proxy e containers Docker.

---

## 2. Direção de Dependência entre Camadas (Clean Architecture)

A direção permitida de dependência entre os projetos da solução .NET é:

```text
Domain ← Application ← Infrastructure
                    ↖ Api consumes Application + Infrastructure
```

### Regras Estritas:

| Camada | Projeto | Pode Referenciar | Proibido Referenciar |
|---|---|---|---|
| **Domain** | `Atrio.Domain` | *Nenhum projeto Átrio* | `Atrio.Application`, `Atrio.Infrastructure`, `Atrio.Api` |
| **Application** | `Atrio.Application` | `Atrio.Domain` | `Atrio.Infrastructure`, `Atrio.Api` |
| **Infrastructure** | `Atrio.Infrastructure` | `Atrio.Application`, `Atrio.Domain` | `Atrio.Api` |
| **Api** | `Atrio.Api` | `Atrio.Application`, `Atrio.Infrastructure`, `Atrio.Domain` | — |

Essas restrições são executadas e verificadas automaticamente em tempo de compilação e através da suíte de testes de arquitetura automatizados com NetArchTest em:
`apps/api/tests/Atrio.Architecture.Tests/LayerDependencyTests.cs`

---

## 3. Contratos de Plataforma e Diagnóstico

- **API Base Path:** `/api/v1`
- **Erros Uniformes:** RFC ProblemDetails com extensões obrigatórias:
  - `code`: identificador estável de erro (ex: `internal_error`, `validation_failed`).
  - `traceId`: identificador único de correlação por requisição.
  - *Stack traces e detalhes sensíveis de infraestrutura nunca são serializados no response de produção.*
- **Health Checks:**
  - `GET /health/live`: Liveness do processo ASP.NET (independe de PostgreSQL).
  - `GET /health/ready`: Readiness da plataforma (inclui verificação de conectividade com o PostgreSQL).
- **Version Endpoint:**
  - `GET /api/v1/system/version`: Retorna `version` e `commit` informados por configuração/ambiente.

---

## 4. Frontend e Same-Origin

- A aplicação SPA em React 19 / TypeScript consome a API através de um cliente HTTP centralizado com `credentials: "include"`.
- O roteamento no ambiente de produção ocorre sob a mesma origem pública (`same-origin`) através do Caddy reverse proxy.
- No ambiente de desenvolvimento, o Vite faz proxy de `/api` para o backend preservando a semântica de mesma origem.
