# ADR-0004 — Deterministic computation; optional, non-authoritative AI narrative

Status: Accepted (AGENTS.md rule 7) | Date: 2026-11-19 | Log: D-004

## Context
AI summaries could help explain reports but are non-deterministic and may hallucinate numbers.

## Decision
- All metrics, constraint checks and routes are computed deterministically in Core.
- AI is not part of MVP. If added later: opt-in per tenant, off by default, excluded from Core, cites computed values only, labelled "AI-generated, non-authoritative".
- Point metadata and free text are never sent to an AI service without explicit consent and a data-processing disclosure.

## Consequences
- Reports are reproducible and auditable without AI.

## Alternatives rejected
- AI-suggested sequences: unverifiable, non-reproducible.

## Acceptance criteria
- No AI dependency in Core, Application calculation paths or CLI.
- Any future AI text is visually and structurally separate from computed results.

## Risks
- Prompt injection via metadata → exclude free-text fields from prompts; treat output as untrusted display text.

## Related
[core-beliefs.md](../core-beliefs.md) · [threat-model.md](../threat-model.md) · [SECURITY.md](../../SECURITY.md)
