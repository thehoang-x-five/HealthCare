# Reality Check Week 1-4

> Audit date: 2026-04-07
>
> Scope reviewed:
> - `docs/upgrade_tasks/week_1..4/*.md`
> - `docs/WEEK1_4_COMPLETION_PLAN.md`
> - Backend source in `HealthCare`
> - Frontend source in sibling repo `my-patients`
>
> Verification performed on 2026-04-07:
> - Backend `dotnet build --no-restore`: success, `0 warnings`, `0 errors`
> - Frontend `npm run build`: success
>
> Important:
> - This file is the current baseline for Week 1-4.
> - Build success proves compile-time integrity.
> - It does not replace Week 5 manual regression, UAT, or release sign-off.

---

## Executive Verdict

Week 1-4 is now in a state that is honest to describe as:

- implementation-complete enough to move into Week 5
- no longer blocked by the major code-level issues identified in the first audit
- not yet certified as fully release-ready without Week 5 runtime verification

Short version:

- `Implementation status`: ready to hand off into Week 5
- `Release confidence`: still depends on role-by-role manual regression

---

## Current Overall Status

| Week | Current status | Meaning |
|---|---|---|
| Week 1 | ✅ Implemented | Infrastructure, SQL/Mongo foundations, and DB bootstrap are now in code |
| Week 2 | ✅ Implemented | Genealogy and Mongo medical-history flow are wired into user-facing FE flow |
| Week 3 | ✅ Implemented | Audit and analytics code are aligned to the real Mongo schema and FE adapters |
| Week 4 | ✅ Implemented | RBAC, admin runtime model, payment inline, VietQR, and seed/runtime role setup are in place |

What remains is no longer "finish missing Week 1-4 code".  
What remains is Week 5 work: verification, regression, seed validation, role/UAT, and demo readiness.

---

## Verified Reality By Week

### Week 1: Infrastructure, SQL bootstrap, appointment state flows

Status: `implemented`

Verified as real in code:
- `AppointmentService` calls `sp_BookAppointment`
- genealogy services call the SQL procedures they depend on
- `DatabaseBootstrapper` now ensures required procedures, triggers, and check constraints exist
- `Program.cs` runs migrations and then runs `DatabaseBootstrapper`

Reality:
- the earlier "fresh environment bootstrap gap" has been closed in code
- Week 1 is no longer blocked by hidden manual SQL setup as the primary intended path

Residual note:
- a clean-machine runtime check is still worth doing in Week 5

### Week 2: Mongo medical history and genealogy

Status: `implemented`

Verified as real in code:
- Mongo medical history repository exists
- backend medical-history endpoint exists
- genealogy backend exists
- genealogy FE exists
- `PatientViewMode.jsx` now calls `useMedicalHistory()`
- `PatientTimeline.jsx` now consumes timeline-style events for the patient view

Reality:
- Mongo medical history is no longer just a backend-only feature
- the patient flow now has a real FE integration path for it

Residual note:
- Week 5 should still verify data quality and presentation with real seeded accounts

### Week 3: Analytics, audit logs, dashboard

Status: `implemented`

Verified as real in code:
- audit log repository and middleware exist
- analytics service now queries the real Mongo schema using `event_date` and `data.*`
- analytics FE adapter exists
- reports/analytics UI exists and builds successfully

Reality:
- the earlier hard mismatch between Mongo write schema and analytics read schema has been addressed
- the earlier FE/BE analytics contract drift has been reduced to a verification problem, not a known implementation blocker

Residual note:
- Week 5 should validate analytics with real seeded data and expected charts, not just build output

### Week 4: RBAC, admin, payment inline, VietQR

Status: `implemented`

Verified as real in code:
- `RequireRoleAttribute` and `RequireNurseTypeAttribute` exist and are actively used
- backend role/data-scope hardening has been added across the main modules
- admin runtime model is now clearly `staff = user`
- `AdminController` and `AdminService` provide real user/staff management APIs
- `Staff` is the real admin management UI path
- `Departments` and `Notifications` now include in-place admin management controls
- payment confirmation and VietQR endpoints exist
- frontend `PaymentWizard` exists
- frontend `VietQRDisplay` exists
- frontend `useGenerateVietQR()` exists and is wired into the wizard
- deferred payment (`Thu sau`) path exists

Reality:
- the major Week 4 implementation blockers from the earlier audit are no longer the baseline reality
- Week 4 is now best described as implemented, with Week 5 focused on end-to-end verification

Residual note:
- some role-specific runtime behavior still needs hands-on validation after restart/seed in Week 5

---

## What Changed Since The First Audit

The earlier audit correctly found major code/runtime gaps at that time.  
Those findings are no longer the right baseline in several important areas.

Now considered fixed in code:
- DB bootstrap for procedures/triggers/check constraints
- admin FE/BE contract normalization path
- pharmacy nurse-subtype enforcement path
- `PaymentWizard` VietQR integration
- `Thu sau` flow
- broader `ProtectedRoute` usage across routes
- patient timeline wiring for Mongo history
- multiple RBAC and data-scope gaps across admin, Y ta HC, Bac si, Y ta LS, Y ta CLS, and KTV paths

Now considered architectural decisions, not unfinished work:
- `staff = user`
- no separate `UserAccount` split
- no separate primary admin runtime page replacing `Staff`

---

## Remaining Risks For Week 5

These are not blockers to entering Week 5, but they are the right focus areas next:

1. Role-by-role manual regression is still required.
2. Data-scope behavior must be verified with real accounts, not only by source inspection.
3. Payment/VietQR must be tested end-to-end against real seeded invoices.
4. Seeded schedules, queue visibility, and department scoping need runtime validation.
5. Frontend build still has non-blocking warnings:
   - SignalR pure comment warning
   - large chunk warning from Vite

---

## Honest Conclusion

If the question is:

- "Can we move on to Week 5?" -> `Yes`
- "Can we already claim production-ready 100% with no more verification?" -> `No`

The correct handoff statement is:

> Week 1-4 implementation is sufficiently complete to freeze feature work and enter Week 5 for regression, UAT, demo preparation, and final release confidence.
