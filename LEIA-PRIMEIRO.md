# Pacote de bootstrap do Átrio — Plano 01

Este pacote acompanha o prompt de execução do Antigravity para implementar **apenas o Plano 01**.

## Estrutura

- `ANTIGRAVITY-START.md` — prompt completo a copiar e colar no agente.
- `docs/superpowers/specs/2026-10-02-atrio-architecture-design.md` — especificação arquitetural.
- `docs/superpowers/plans/2026-10-02-atrio-v0.1-implementation-roadmap.md` — roadmap e dependências.
- `docs/superpowers/plans/2026-10-02-atrio-01-foundation-platform.md` — único plano autorizado nesta rodada.

## Como usar

1. Crie uma pasta de trabalho **nova** chamada `atrio`, fora do workspace do `biblioflux`.
2. Extraia o conteúdo deste ZIP nessa pasta, preservando `docs/superpowers/...`.
3. Abra **somente essa pasta** como workspace no Antigravity Desktop.
4. Abra `ANTIGRAVITY-START.md`, copie o prompt integral e envie ao agente.
5. Confira os resultados e a PR após o agente finalizar. Não autorize o Plano 02 antes do review.

O pacote contém documentos e prompt, **não contém a implementação**. O agente fará o scaffolding, Git, código, testes e commits conforme o plano.
