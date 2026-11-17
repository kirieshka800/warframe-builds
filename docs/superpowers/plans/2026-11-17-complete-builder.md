# Complete Warframe Builder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement task by task.

**Goal:** Finish the supplied site for warframe and weapon builds.
**Architecture:** Extend current EF Core/MVC app; typed views, central calculator and compatibility rules.
**Tech Stack:** ASP.NET Core10, PostgreSQL17, Razor, CSS/JS, Playwright.
**Spec:** ../specs/2026-11-17-complete-builder.md

## Global constraints

Preserve original stack and existing user data. Keep new accounts separate by verified/random ID.
Do not present approximate game formulas as complete simulation. Deliver source archive, not Rust repo changes.

## Review focus

Negative aura drains; stance subtypes; foreign-owner and CSRF writes; malformed imports;
capacity when aura removed; nullable empty slot migration; mobile editor and persisted ranks.

## Tasks

- [x] Inspect archive and establish frame+weapon scope with user.
- [x] Catalogue/model/calculator: seed data, compatibility, exact-rank effects and warnings.
- [x] MVC/identity: validated CRUD, owner checks, antiforgery, duplication, JSON, guides/comments.
- [x] UI: typed responsive pages, real forms/search, editor/stats, share links and error states.
- [x] Independent review: fix signed aura costs, stance metadata, TrueSteel annotation,
  enemy-debuff parsing and aura-removal capacity regression.
- [x] Verify build, calculation checks, real HTTP/browser flows, clean and legacy migration.
- [x] README, Docker/local startup, package source zip and inspect final artifact.
