# ChronoFlow

ChronoFlow is a modular event-driven backend platform built with .NET.

The goal of ChronoFlow is to provide a clean foundation for building
event-sourced systems, orchestration platforms, and scalable backend services.

This project follows strict architectural boundaries to ensure the core
remains reusable across multiple domains.

> Evolution isn’t deviation. It’s convergence.

---

# Architecture

ChronoFlow is built as a **Modular Monolith**.

A single deployable API with strict internal module boundaries.

ChronoFlow
├── ChronoFlow.Api
├── ChronoFlow.Core
├── ChronoFlow.Infrastructure
├── ChronoFlow.Modules.Identity
├── ChronoFlow.Modules.Events
└── ChronoFlow.Tests


Each module owns its domain logic while the core remains domain-agnostic.

A critical rule of the platform:

Domain logic must never leak into the core.

---

# Current Features

### Authentication

* User registration
* JWT authentication
* Protected endpoints

### Event System

* Event ingestion endpoint
* Event stream querying
* Chronological ordering
* Stream isolation

### Testing

* Unit tests for application logic
* Integration tests against the API
* Test-driven validation of event behavior

---

# Example API

### Register User


---

POST /auth/register

### Login
POST /auth/login

### Ingest Event
POST /events

### Query Event Stream
GET /streams/{streamId}/events
---

# Technology

ChronoFlow is built with:

* **.NET**
* **ASP.NET Core**
* **PostgreSQL**
* **Entity Framework Core**
* **xUnit testing**

---

# Documentation

* **[Wave 2 — Control operators & release](docs/WAVE2_CONTROL_OPERATORS_AND_RELEASE.md)** — control trigger intake, execution records, pending review actions, PostgreSQL migrations, and process-local dedupe assumptions.

---

# Development Principles

ChronoFlow follows strict engineering rules:

1. Write tests before shipping features.
2. Keep the core reusable and domain-agnostic.
3. Modules must not leak domain logic into the platform core.
4. Architecture must remain evolvable without rewrites.

---

# Project Status

ChronoFlow is currently in early development.

Completed:

* Identity module
* Event ingestion
* Event stream queries
* Integration testing

Planned:

* Event dispatch pipeline
* Projection system
* Workflow orchestration
* distributed processing

---

# License

Apache License 2.0
