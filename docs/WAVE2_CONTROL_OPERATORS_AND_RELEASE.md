# ChronoFlow Wave 2 — Control automation (operator & release notes)

This document describes the **operator-facing HTTP surface** and **release assumptions** for **Wave 2** (control trigger intake, execution records, orchestration policy, trace fields, and pending operator review). It reflects the implementation on branch `feature/phase-6-control-and-ail-integration` through Phase 6 hardening. It is not a product roadmap.

## Authentication

These endpoints use the shared intake guard (`ControlTriggerIntakeApiKey`):

- **Header:** `X-Api-Key` must match the configured value when enforcement is enabled.
- **Configuration:** `ControlTriggers:Intake:ExpectedApiKey` in application configuration (see `ControlTriggersIntakeOptions.SectionName`).
- **When `ExpectedApiKey` is empty or whitespace**, the check is skipped and requests are allowed without a key (use only in controlled environments).

**Endpoints covered:** `POST /control/triggers`, `GET /control/executions`, `GET /control/executions/{id}`, `POST /control/executions/{id}/approve`, `POST /control/executions/{id}/cancel`.

---

## Control trigger intake

**`POST /control/triggers`**

Accepts JSON aligned with `ReceiveControlTriggerRequest` (core alert/trigger fields plus Wave 2 optional fields).

### Wave 2 optional fields

| JSON field | Semantics |
|------------|-----------|
| `correlationId` | Optional trace correlation from upstream; stored in a bounded form on the execution record when present; omitted or empty → absent (`null`) in persistence and APIs. |
| `inboundDecision` | Optional object (`InboundDecisionIntakeRequest`): `summary`, `referenceId`, `confidence`, `reasonCode`, `linkedExternalExecutionId`. Values are bounded and snapshotted at acceptance; ChronoFlow does not recompute upstream decisions. |

### Default orchestration policy and `reasonCode`

When `inboundDecision.reasonCode` matches a **platform** constant, the default deterministic policy may suppress execution, require advisory-only persistence, or require operator review. Encoded values (see `OrchestrationPolicyInboundReasonCodes` in code):

- `CHRONOFLOW_ORCH_SUPPRESS`
- `CHRONOFLOW_ORCH_ADVISORY_ONLY`
- `CHRONOFLOW_ORCH_REQUIRE_REVIEW`

Other reason codes do not trigger these gates under the default policy.

### Successful response (`ControlTriggerAcceptedResponse`)

Includes execution summary fields, e.g. `wasExecuted`, `wasSuppressed`, `workflowKey`, `executedStepCount`, `executionRecordId`, `executionInstanceId`, `pendingOperatorReview`, `orchestrationPolicyOutcome`. Exact shape matches the API contract in `ChronoFlow.Api/Contracts/Control/`.

---

## Control execution records (list & detail)

**`GET /control/executions`** — query list (filters such as `alertId`, `lifecycleEventType`, `wasExecuted`, `wasSuppressed`, `workflowKey`, paging).

**`GET /control/executions/{id}`** — single record.

Responses use `ControlExecutionRecordResponse` (JSON camelCase). Wave 2–relevant fields include:

| Field | Semantics |
|-------|-----------|
| `correlationId` | Intake correlation snapshot; `null` when not supplied. |
| `occurredAtUtc` | Upstream `OccurredAtUtc` at acceptance; `null` on legacy rows (replay uses `receivedAtUtc` where applicable). |
| `advisoryWasUsed`, `advisoryStrategyKey`, `advisoryConfidence`, `advisoryReasonSummary` | Bounded advisory snapshot when advisory informed routing; honest absence when not used. |
| `linkedAilExecutionId` | Identifier returned by the advisory dependency when present; never invented locally; `null` when absent. |
| `inboundDecision*` | Bounded snapshot of optional inbound decision fields at start time. |
| `orchestrationPolicyOutcome` | Policy outcome vocabulary, e.g. `proceed`, `policy_suppressed`, `advisory_only`, `pending_review`, `review_cancelled` (see `OrchestrationPolicyOutcomes`). |
| `pendingOperatorReview` | `true` when execution is gated for operator review. |
| `operatorReviewAction`, `operatorReviewActionAtUtc`, `operatorReviewNote` | Set after a **cancel** or **approve** review action; `null` until then. Values `approved` / `cancelled` (see `OperatorReviewActions`). Note is bounded. |
| `executionInstanceId` | Orchestration execution instance identity when allocated for the path; distinct from record `id`. |

Semantics are **honest**: absent linkage remains `null`; list and detail use the same mapping (`ControlExecutionRecordResponseMapper`).

---

## Pending operator review actions

Only rows that are **actionably** pending review accept approve/cancel. In practice this means: pending review flag set, policy outcome `pending_review`, workflow not yet executed for that record, and no prior `operatorReviewAction`. Rows that already have a review action return a **finalized** conflict (see below).

### `POST /control/executions/{id}/approve`

- **Body (optional JSON):** `{ "note": "<bounded optional string>" }` (`OperatorReviewActionRequest`).
- **Behavior:** Performs **real** workflow execution via the same orchestration executor path used for normal proceed, then persists an executed record with policy outcome `proceed`, clears pending review, and records dedupe success for the trigger key when applicable.
- **Errors:** `404` if the record id is unknown; `409 Conflict` for invalid state, including “not pending”, “already finalized”, invalid durable row for execution, or workflow resolution mismatch vs stored `workflowKey`. Error payloads are JSON objects with an `error` string.

### `POST /control/executions/{id}/cancel`

- **Body (optional JSON):** same optional `note` as approve.
- **Behavior:** Does **not** run workflow steps. Persists outcome `review_cancelled`, sets operator review fields, clears pending review.
- **Errors:** same general pattern as approve (`404` / `409`).

---

## Operational limitation (deferred)

**Duplicate suppression and post-approve dedupe** use an **in-process** store (`IProcessedTriggerStore` / default in-memory implementation registered in the API).

- **Current honest assumption:** a **single API process** (or equivalent) so dedupe and approve/dedupe interactions are consistent with that instance’s memory.
- **Horizontal scaling:** multiple API instances do **not** share this store. Duplicate trigger suppression and dedupe semantics can diverge across instances until a **shared** backing implementation is introduced. **Wave 2 intentionally does not** implement distributed dedupe or database-backed dedupe.
- **Concurrent approve:** true multi-instance (and extreme same-instance concurrency) edge cases around double execution are **not** fully solved by Wave 2; Phase 6 added explicit finalized-state handling and re-reads before execute, not distributed locking.

Plan shared dedupe **before** relying on multi-instance behavior for control automation.

---

## Merge, migrations, and tagging (guidance)

1. **Wave 2** implementation is complete through Phase 6 on `feature/phase-6-control-and-ail-integration` (e.g. commit `ddb2537` and ancestors).
2. **PostgreSQL (non-test):** apply all **Events** EF Core migrations present in the repository up to and including Wave 2. **Phase 6 did not add a migration.** Representative control-execution migrations include (names may vary slightly): control execution records table and columns for advisory fields, execution instance id, inbound/advisory linkage, orchestration policy fields, correlation id, operator review / occurred-at (Phase 5). Use your standard `dotnet ef database update` (or CI/CD migration step) against the **Events** `DbContext`.
3. **Recommended closure steps:** merge the feature branch to your mainline, push, and tag the release (e.g. `wave2` or a semver tag) per your project conventions. **This repository run does not create tags.**
4. **Post-merge:** decide whether **shared dedupe** (or another strategy) is required before running multiple ChronoFlow API replicas for control intake.

---

## References in code

- Endpoints: `ChronoFlow.Api/Endpoints/ControlTriggersEndpoints.cs`, `ControlExecutionsEndpoints.cs`
- Contracts: `ChronoFlow.Api/Contracts/Control/`
- Security: `ChronoFlow.Api/Security/ControlTriggerIntakeApiKey.cs`
- Policy vocabulary: `OrchestrationPolicyOutcomes`, `OperatorReviewActions`, `OrchestrationPolicyInboundReasonCodes` in `ChronoFlow.Modules.ControlTriggers`
