# Universal File System API (Backend)

## 🚀 Overview

The **Universal File System API** provides a modular, secure, and
extensible backend service that supports multiple storage providers such
as **Local**, **S3**, **FTP**, **WebDAV**, and **Google Drive**.\
This repository contains the **backend (.NET 9)** implementation focused
on the API, plugin architecture, authentication, logging, caching, and
CI/CD pipelines.

------------------------------------------------------------------------

## 🎯 Scope & Objectives

-   Modular plugin architecture for extensible storage backends.
-   Robust error handling and structured logging.
-   Secure authentication with OAuth2, AWS IAM, and Basic Auth.
-   Sandboxed access control with PostgreSQL-based ACLs.
-   Redis + MemoryCache caching layer.
-   Full CI/CD pipeline with containerized deployment (Docker +
    Kubernetes).

------------------------------------------------------------------------

## 🧩 MVP Feature Set

### 1. Core API & Plugin System

-   `IUniversalFileSystem` interface: `Open`, `Read`, `Write`, `List`,
    `Delete`, `Stat`, `Mkdir`
-   **PluginManager** for dynamic provider registration
-   **Built-in Providers:**
    -   Local Disk (`System.IO`)
    -   In-memory (testing)
-   **External Providers:**
    -   AWS S3 (`AWS SDK for .NET`)
    -   FTP/S (`FluentFTP`)
    -   WebDAV (`WebDAVClient`)
    -   Google Drive (`Google.Apis.Drive.v3`)

### 2. Error Handling & Logging

-   Centralized `FileSystemErrorHandler`
-   Structured JSON logging via **Serilog**
-   Log outputs: Console, File, Elasticsearch, Loki
-   Log metadata: URI, Duration, Status, Provider

### 3. Authentication & Security

-   **AuthenticationManager**:
    -   OAuth2 (Google Drive)
    -   AWS IAM (S3)
    -   Basic Auth (FTP/WebDAV)
-   **AccessControlManager** with PostgreSQL ACLs
-   Sandboxed path access enforcement

### 4. Caching & Performance

-   **CacheManager:**
    -   In-memory LRU cache (`MemoryCache`)
    -   Redis distributed cache
    -   Configurable TTLs & invalidation policies

### 5. CI/CD & Deployment

-   **GitHub Actions pipeline** includes:
    -   Build & test (.NET 9 backend)
    -   Integration tests (PostgreSQL + Redis via Testcontainers)
    -   Security audits & static analysis
    -   Docker image build & push
    -   Kubernetes deployment via Helm
-   **Deployment Environments:**
    -   Docker Compose (Dev)
    -   Kubernetes (Prod)

------------------------------------------------------------------------

## ⚙️ Technology Stack

### Backend

-   **Runtime:** .NET 9 (C# 13)
-   **Framework:** ASP.NET Core Minimal APIs or FastEndpoints
-   **ORM:** Entity Framework Core 9 + PostgreSQL
-   **Authentication:** .NET Identity + OAuth2/OpenID Connect + AWS IAM
-   **Logging:** Serilog → Elasticsearch / Loki
-   **Caching:** MemoryCache + Redis
-   **Validation:** FluentValidation
-   **Monitoring:** Prometheus-net + Grafana
-   **Testing:** xUnit / NUnit + Testcontainers

------------------------------------------------------------------------

## 🧪 Testing Strategy

  -----------------------------------------------------------------------
  Type          Framework                   Description
  ------------- --------------------------- -----------------------------
  Unit Tests    xUnit / NUnit               Core API logic and providers

  Integration   xUnit + Testcontainers      Redis, PostgreSQL, plugin
  Tests                                     lifecycle

  End-to-End    Playwright / Cypress        Full-stack validation with
                                            frontend
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 🐳 DevOps & Infrastructure

### Containerization

-   Multi-stage Docker builds for backend
-   Docker Compose for local dev

### Orchestration

-   Kubernetes manifests for API, Redis, PostgreSQL
-   Helm charts for production deployment

### CI/CD

-   GitHub Actions workflows for build, test, deploy

### Monitoring & Logging

-   Prometheus & Grafana dashboards
-   Loki / Elasticsearch + Kibana for logs

### Secrets Management

-   Kubernetes Secrets or HashiCorp Vault

------------------------------------------------------------------------

## 🧰 Developer Setup

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/)
-   [Docker & Docker Compose](https://www.docker.com/)
-   [Redis](https://redis.io/)
-   [PostgreSQL](https://www.postgresql.org/)

------------------------------------------------------------------------

## 📘 Documentation

-   API Docs: Swagger/OpenAPI (auto-generated)
-   Developer Docs: Markdown + Confluence
-   Logs & Metrics: Grafana, Prometheus dashboards

------------------------------------------------------------------------

## 🧑‍💻 Contributing

1.  Fork the repository
2.  Create a feature branch (`git checkout -b feature/my-feature`)
3.  Commit your changes (`git commit -m "Add feature"`)
4.  Push to the branch (`git push origin feature/my-feature`)
5.  Open a Pull Request

------------------------------------------------------------------------

## 🪪 License

Licensed under the MIT License. See [LICENSE](LICENSE) for details.

------------------------------------------------------------------------

