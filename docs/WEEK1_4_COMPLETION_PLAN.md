# Completion Plan Week 1-4

> Updated: 2026-04-07
>
> This file no longer describes a large unfinished Week 1-4 rescue plan.
> It now records the current completion position and the Week 5 handoff checklist.

---

## Current Position

Week 1-4 should now be treated as:

- feature implementation baseline completed
- core architecture stabilized
- ready for Week 5 verification work

Verified on 2026-04-07:
- Backend `dotnet build --no-restore`: pass, `0 warnings`, `0 errors`
- Frontend `npm run build`: pass

---

## Definition Of Done For Entering Week 5

Week 1-4 is considered complete enough to hand off into Week 5 when all of the following are true:

1. Backend builds cleanly.
2. Frontend builds successfully.
3. DB bootstrap logic exists in code.
4. Core FE/BE contracts are aligned enough for runtime testing.
5. Week 4 payment flow has cash, VietQR, and deferred payment.
6. Admin runtime management exists in the merged `staff = user` model.
7. Role/data-scope enforcement exists in both FE and BE for the main modules.
8. The week-specific upgrade docs reflect the current architecture.

Current verdict: `met`

---

## What Is Already Completed

### Week 1

- [x] SQL/Mongo foundation exists
- [x] appointment SP path exists
- [x] genealogy SP path exists
- [x] DB bootstrapper now applies required SQL artifacts at startup

### Week 2

- [x] Mongo medical-history backend exists
- [x] genealogy backend exists
- [x] genealogy frontend exists
- [x] patient-facing medical-history timeline wiring exists

### Week 3

- [x] audit log infrastructure exists
- [x] analytics backend uses the real Mongo schema
- [x] analytics FE adapter/UI wiring exists
- [x] reports module builds against the current contract

### Week 4

- [x] backend RBAC foundation exists
- [x] nurse subtype enforcement exists
- [x] admin runtime is aligned to `staff = user`
- [x] `Staff` page is the real admin management path
- [x] admin controls also exist in `Notifications` and `Departments`
- [x] payment confirm flow exists
- [x] VietQR API exists
- [x] `PaymentWizard` and `VietQRDisplay` exist
- [x] deferred payment (`Thu sau`) exists
- [x] seed data now supports the practical runtime/testing roles

---

## Week 5 Entry Checklist

The next phase should focus on proof, not large new implementation.

### 1. Environment Validation

- [ ] Start from a clean DB snapshot or known reset state
- [ ] Verify migrations + bootstrap scripts run correctly
- [ ] Verify seeded accounts can log in as expected

### 2. Role-By-Role Regression

- [ ] Admin
- [ ] Y ta hanh chinh
- [ ] Bac si
- [ ] Y ta lam sang
- [ ] Y ta can lam sang
- [ ] Ky thuat vien

For each role, verify:
- [ ] menu visibility
- [ ] route access
- [ ] page-level access
- [ ] action-level access
- [ ] backend data scope

### 3. Core Workflow Regression

- [ ] appointment booking
- [ ] check-in
- [ ] patient registration/update
- [ ] LS exam flow
- [ ] CLS order and result flow
- [ ] prescription creation and pharmacy flow
- [ ] payment confirm
- [ ] VietQR generation and payment path
- [ ] deferred payment (`Thu sau`)
- [ ] notification flow
- [ ] report visibility by role

### 4. Data Scope Verification

- [ ] dashboard scope
- [ ] queue scope
- [ ] patients scope
- [ ] history scope
- [ ] clinical scope
- [ ] CLS scope
- [ ] prescription/pharmacy scope

### 5. Seed And Runtime Confidence

- [ ] verify admin account
- [ ] verify YTHC account
- [ ] verify Bac si account
- [ ] verify Y ta LS account
- [ ] verify Y ta CLS account
- [ ] verify KTV account
- [ ] verify schedules/rooms actually drive queue visibility correctly

### 6. Documentation Lock

- [ ] sync `rbac_matrix.md` with the real runtime behavior
- [ ] sync Week 5 role/UAT docs with current code
- [ ] mark historical docs that are no longer the source of truth
- [ ] remove or label legacy pages/components that are no longer runtime-primary

### 7. Demo/Release Preparation

- [ ] prepare smoke-test script
- [ ] prepare demo accounts
- [ ] prepare demo data reset steps
- [ ] prepare known-issues list if any non-blocking warnings remain

---

## Remaining Non-Blocking Risks

These should be tracked in Week 5, but they no longer mean Week 1-4 is unfinished:

- frontend chunk-size warning from Vite
- SignalR pure-comment warning in build output
- possible legacy files that are still present but no longer runtime-primary
- runtime behavior still needs real-account verification after restart and reseed

---

## Exit Criteria For Week 5

Week 5 can claim final closure when all of these are true:

1. Every target role passes manual regression on the intended pages.
2. Data scope matches the intended RBAC matrix in runtime, not only in source.
3. Payment and VietQR are verified end-to-end.
4. Seeded accounts and schedules support the expected demos/tests.
5. Docs, UAT checklists, and role matrix all match the actual code behavior.

---

## Final Handoff Statement

Week 1-4 should now be treated as completed implementation work.  
Week 5 should be treated as the proof and hardening phase:

- regression
- UAT
- role verification
- demo preparation
- final documentation lock
