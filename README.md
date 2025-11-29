# ProcessManager

## Overview

**ProcessManager** is a .NET microservice responsible for tracking **active** and **completed** processes and their related tasks within a larger SaaS platform. It is designed to monitor **long-running operations** over time, capturing their execution progress and status changes so the main platform can provide accurate, real-time visibility.

ProcessManager acts as the system’s source of truth for process execution state (e.g., running, succeeded, failed, cancelled), enabling dashboards and other services to reliably query and display the latest process status.

## What it does

- Tracks **active** and **finished** processes
- Tracks process **steps / tasks** and their execution history over time
- Maintains the **current status** of long-running operations
- Updates and persists status changes coming from external orchestration tools
- Processes incoming events/messages asynchronously via a background worker

## Integration & Workflow

ProcessManager is typically updated by orchestration workflows such as **Azure Logic Apps**, which publish state changes (start/progress/finish/failure) to the service.  

A **Background Worker** receives these messages and processes them asynchronously to:
- validate the incoming update
- persist state transitions
- update timestamps, progression data, and execution records
- expose the latest status for querying by other services

This makes ProcessManager well-suited for workflows where operations may take minutes or hours and require durable status tracking across time.

## Technologies Used

- **.NET 6 (ASP.NET Core)**  
  Built on .NET 6, exposing functionality via a clean and scalable Web API.

- **Entity Framework Core**  
  Used for data access and persistence, supporting code-first patterns and migrations.

- **MediatR**  
  Implements the mediator pattern to decouple application logic and support a clean CQRS-style request pipeline (commands/queries/handlers).

- **Domain-Driven Design (DDD)**  
  Typically structured with separated layers (Domain / Infrastructure / API), supporting maintainable boundaries and a clear domain model.

## Part of a Microservice Platform

ProcessManager is one microservice within a broader microservice-based SaaS platform and is designed to integrate cleanly with other services and orchestration tooling (such as Azure Logic Apps) to provide reliable tracking of operational execution and outcomes.
