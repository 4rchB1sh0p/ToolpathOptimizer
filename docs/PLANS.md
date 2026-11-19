# Planning system

Status: Active | Last reviewed: 2026-11-19

## Purpose and scope
- How execution plans are written, sequenced and closed. No dates or delivery guarantees.

## Rules
- Plans live in [exec-plans/active/](exec-plans/active/) and move to [completed/](exec-plans/completed/README.md) after acceptance evidence is recorded.
- Each plan states: goal, scope, assumptions, tasks, acceptance/test criteria, risks, rollback, related links.
- Debt goes in [Decisions.md §7](Decisions.md); decisions in [Decisions.md §8](Decisions.md).
- One vertical slice at a time; documentation updated in the same change.

## Sequence
| Plan | Title | Depends on |
|---|---|---|
| [0001](exec-plans/active/0001-documentation-to-scaffold.md) | Documentation and discovery to scaffold | — |
| [0002](exec-plans/active/0002-core-optimisation-slice.md) | Core optimisation slice | 0001 |
| [0003](exec-plans/active/0003-application-and-cli-slice.md) | Application and CLI slice | 0002 |
| [0004](exec-plans/active/0004-shipanysaas-verification-spike.md) | ShipAnySaaS verification spike | 0001 (parallel with 0002/0003) |
| [0005](exec-plans/active/0005-web-api-and-tenancy.md) | Web API and tenancy | 0003, 0004 |
| [0006](exec-plans/active/0006-containers-and-pilot-hardening.md) | Containers and pilot hardening | 0005 |

## Acceptance criteria
- Each plan closes only with linked evidence against [QUALITY_SCORE.md](QUALITY_SCORE.md).

## Risks
- Parallel plans drifting from contracts → contract changes require ADR/D-entry first.

## Related
[Decisions.md](Decisions.md) · [tech-debt-tracker.md](exec-plans/tech-debt-tracker.md)
