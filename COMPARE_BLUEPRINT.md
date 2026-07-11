# Multitenancy Compare Blueprint

## Decision Summary

For this codebase, the better default is to implement multitenancy in the same template and same product line.

Create a separate project only when the multitenant system is a different product, owned by a different team, or requires a radically different architecture.

## Option A: Same Project

Use the existing backend and frontend template and add multitenancy as a first-class capability.

### What this looks like

- One shared solution template.
- One auth and identity system.
- One tenant-aware database model.
- One deployment pipeline with per-environment config.
- One frontend shell that shows only the enabled modules for the current tenant.

### Pros

- Reuses the current layered architecture.
- Lower maintenance cost.
- Faster delivery.
- Easier to keep auth, menus, permissions, and module entitlements consistent.
- Better for a product suite like ERP, HRM, payroll, leave, and related modules.

### Cons

- More conditional logic in the template.
- More careful design needed for tenant scoping.
- Risk of complexity if the template becomes too generic.

## Option B: Separate Project

Build a second product or second template specifically for multitenant deployments.

### What this looks like

- A separate backend solution.
- A separate frontend app.
- Separate deployment pipeline.
- Separate tenant/module model.

### Pros

- Strong separation of concerns.
- Easier if the new system is a different product line.
- Avoids cluttering the existing template.

### Cons

- Duplicates auth, menu, deployment, and observability work.
- Higher long-term maintenance cost.
- Slower feature delivery.
- Harder to keep UX and API contracts aligned.

## Recommendation For This Repo

Use the same project and make multitenancy an optional feature at creation time.

Suggested project wizard option:

- `Multitenancy: yes/no`

If `yes`:

- generate tenant entities and tenant-aware data filters
- generate tenant entitlement tables
- add tenant-aware permissions and menu filtering
- add tenant switcher and tenant-scoped UI behavior in the frontend

If `no`:

- generate the standard single-tenant app
- skip tenant tables, filters, and tenant UI

## Practical Rule

Choose the same project if the answer to most of these is yes:

- Same auth system?
- Same deployment pipeline?
- Same menu and permission model?
- Same product family?
- Same frontend shell?

Choose a separate project if the answer to most of these is no.

## Suggested Next Step

Define the generator contract for both branches:

1. common app template
2. multitenant backend branch
3. multitenant frontend branch
4. single-tenant branch

That keeps the wizard simple and lets the template generate the right structure from one decision.