# Automatic Build Selection Implementation Plan

> **For agentic workers:** Execute task by task with superpowers:executing-plans.

**Goal:** Rule-based frame and weapon proposals with deliberate confirmation.
**Architecture:** Pure deterministic planner, bounded owner-bound proposal store,
MVC preview and revalidated save, using existing compatibility/calculator.
**Tech Stack:** ASP.NETCore10,PostgreSQL,Razor,Playwright.
**Spec:** ../specs/2026-11-18-auto-build.md

## Global constraints

No LLM requirement; no writes before confirmation; no existing-build mutations.
Validate capacities, ranks, family duplicates, polarities, owner and CSRF.

## Review focus

Parallel saves, cancelled/unknown tokens, catalogue drift, cache capacity,
neutral polarity assumption, sparse catalogue and goal-specific UI/mobile layout.

## Tasks

- [x] Read current source; confirm rule-based approach with user.
- [x] Implement pure planner and autonomous tests for goal/category/budget invariants.
- [x] Add owner-bound proposal lifecycle, refetch/revalidation and idempotent confirmation.
- [x] Add form/preview/results links and explicit confirm/cancel.
- [x] Review: reproduce and repair bounded-cache admission; add Bo stance regression.
- [x] Run build, calculation/planner/HTTP/browser suites and manual screenshot inspection.
- [x] Update docs, publish authorised GitHub main and verify uploaded hashes.
