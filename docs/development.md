# Átrio — Guia de Desenvolvimento Local

Este guia documenta os procedimentos e comandos padrão para desenvolvimento, testes e execução local da plataforma Átrio.

---

## 1. Configuração Inicial do Ambiente

1. Certifique-se de que as ferramentas necessárias estão instaladas:
   - **.NET SDK 10.0+**
   - **Node.js 22+** e **npm**
   - **Docker Engine** e **Docker Compose**
   - **Git**

2. Crie seu arquivo de variáveis de ambiente a partir do modelo de exemplo:
   ```bash
   cp .env.example .env
   ```
   *(No Windows PowerShell: `Copy-Item .env.example .env`)*

   > **Importante:** O arquivo `.env` contém configurações locais de desenvolvimento e nunca deve conter segredos de produção ou ser commitado no repositório.

---

## 2. Backend (.NET 10)

A solução está localizada em `apps/api/Atrio.slnx`.

### Restaurar dependências
```bash
dotnet restore apps/api/Atrio.slnx
```

### Compilar a solução
```bash
dotnet build apps/api/Atrio.slnx
```

### Executar testes
O comando executa testes unitários, testes de arquitetura (NetArchTest) e testes de integração com PostgreSQL real (via Testcontainers):
```bash
dotnet test apps/api/Atrio.slnx
```

---

## 3. Frontend (React 19 / TypeScript / Vite)

O projeto da interface web está em `apps/web`.

### Instalar dependências
```bash
npm --prefix apps/web install
```

### Checagem de tipos (Typecheck)
```bash
npm --prefix apps/web run typecheck
```

### Executar testes (Vitest)
```bash
npm --prefix apps/web test -- --run
```

### Compilar build de produção
```bash
npm --prefix apps/web run build
```

### Servidor de desenvolvimento interativo
```bash
npm --prefix apps/web run dev
```
*(O Vite fará proxy de `/api` para o backend conforme configurado em `vite.config.ts`)*

---

## 4. Ambiente Docker Compose

A plataforma Átrio oferece uma composição portátil para desenvolvimento local e validação integrada.

### Iniciar stack de desenvolvimento
```bash
docker compose up -d --build
```

### Inspecionar status dos contêineres
```bash
docker compose ps
```

### Verificar logs
```bash
docker compose logs -f
```

### Executar Smoke Test Automatizado

- **Linux / macOS:**
  ```bash
  ./scripts/smoke.sh
  ```

- **Windows (PowerShell):**
  ```powershell
  .\scripts\smoke.ps1
  ```

Os scripts verificam:
- `GET /health/live` == 200 (Liveness da API)
- `GET /health/ready` == 200 (Readiness com PostgreSQL saudável)
- `GET /api/v1/system/version` == 200 (Contrato de versão e commit)
- `GET /` == 200 (Root da aplicação Web SPA)

### Encerrar e limpar a composição
```bash
docker compose down
```

Para remover também volumes persistentes de dados:
```bash
docker compose down -v
```
