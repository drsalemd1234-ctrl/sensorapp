# Technical Assessment — Candidate Brief
## Senior Software Developer — R8897 (Data Modernization)

---

## Overview

> **Important:** This is a **sample application created specifically for this assessment**. It is not a real product or production system. It has been purpose-built to demonstrate a range of common patterns found in legacy codebases. Your task is to treat it as representative of real-world technical debt and show us how you would approach modernizing it.

This assessment reflects the actual nature of the work you'll be doing in this role — modernizing a mature codebase where data architecture decisions have a direct impact on scalability, maintainability, and customer experience.

---

## The Application

You are given a small but realistic **legacy IoT Sensor API** built in .NET Core 3.1.

The application:
- Ingests readings from IoT sensors (temperature, humidity, pressure)
- Manages sensor device registrations
- Triggers threshold-based alerts
- Maintains an audit log of events

The codebase has grown organically over time. It works — but it was never designed for scale, maintainability, or modern data practices.

**Repository:** https://github.com/sheshisheriaspen/R8897-DataModernization

---

## Getting Started

You will receive this application either as a **ZIP file (`SensorApp-Assessment.zip`) from the recruiting team** or via a **GitHub repository link**.

> **We strongly encourage using the Docker option.** It most closely reflects a real-world environment and gives you the opportunity to showcase a broader range of modernization choices — including database migrations, containerization strategy, and infrastructure decisions. If you have Docker installed, please use it.

---

### 🐳 Option B — SQL Server with Docker *(Recommended)*

**If you received a ZIP:** Extract `SensorApp-Assessment.zip` — open the **`Docker-SQLServer`** folder and run:
```bash
docker-compose up --build
```

**If you received a GitHub link:**
```bash
git clone https://github.com/sheshisheriaspen/R8897-DataModernization.git
cd R8897-DataModernization
docker-compose up --build
```

The API will be available at: **http://localhost:5000/swagger**

**Requirements:** [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

### ✅ Option A — SQLite (no Docker required)

If you do not have Docker installed, use this option instead.

**If you received a ZIP:** Extract `SensorApp-Assessment.zip` — open the **`SQLite`** folder and run:
```bash
dotnet run --project SensorApp
```

**If you received a GitHub link:**
```bash
git clone -b sqlite https://github.com/sheshisheriaspen/R8897-DataModernization.git
cd R8897-DataModernization
dotnet run --project SensorApp
```

The API will be available at: **http://localhost:5000/swagger**

No database setup needed — SQLite database file is created automatically on first run.

**Requirements:** [.NET SDK 8.0+](https://dotnet.microsoft.com/en-us/download)

---

## Your Task

**Analyze the existing application and produce a modernized version.**

We are not looking for a perfect, production-ready system. We are looking for how you think, how you approach an unfamiliar codebase, and how you reason about data architecture decisions.

### Part 1 — Analysis (written)
Prepare a short written summary (can be a markdown file or README) covering:
- What problems did you identify in the current application?
- What data is being stored incorrectly or inefficiently?
- What would you change and why?

### Part 2 — Implementation
Modernize the application. The choice of tools, databases, and architecture is **entirely yours** — we want to see your reasoning, not a prescribed solution.

Things we care about:
- Are you using the right database for the right type of data?
- Is the application layer clean, testable, and maintainable?
- Have you addressed the most critical issues?
- Can you explain the trade-offs of your decisions?

You do **not** need to modernize everything — prioritize what you think matters most and be prepared to explain why.

---

## What to Submit

Please share a link to a GitHub repository containing:
1. Your modernized application code
2. A `README.md` or `ANALYSIS.md` documenting your findings and decisions
3. Instructions to run your version

---

## Time Expectation

We expect this to take **2–4 hours** for an experienced developer. You are welcome to use any tools you normally use — including AI coding assistants. If you do use AI tools, be prepared to discuss what they helped with and where you had to course-correct them.

---

## Follow-up Discussion

In your next interview session, you will walk us through:
- Your analysis of the legacy application
- The architectural decisions you made
- Trade-offs you considered
- One or two things you would do differently with more time

> **Please note:** During the follow-up session, we may ask you to **make a live change or enhancement** to your solution based on the discussion. This is not a trick — we want to see how you reason and adapt in the moment, not just how you prepared in advance. Come ready to open your code.

---

## Questions?

If you have any questions about the assessment or setup issues, please reach out to your recruiting contact directly.

We look forward to seeing your approach.

---

*This assessment is confidential. Please do not share the repository or this brief publicly.*
