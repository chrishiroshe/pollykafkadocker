# 🚀 Order Processing API - .NET | Kafka | Docker | Polly

Backend application built with .NET focused on scalable and resilient processing using event-driven architecture.

## 📌 Overview

This project demonstrates the implementation of a backend system using:

- **.NET 8 / C#**
- **Apache Kafka** for asynchronous messaging
- **Docker** for containerization
- **Polly** for resilience (retry, circuit breaker)
- **REST APIs**
- **Clean Architecture & SOLID principles**

The system simulates order processing using an event-driven approach, ensuring scalability, fault tolerance, and reliability.

---

## 🧱 Architecture

The solution follows a layered architecture:

- **API Layer** → Receives requests
- **Application Layer** → Business logic
- **Domain Layer** → Core entities and rules
- **Infrastructure Layer** → Kafka, persistence, external services

---

## 🔄 Flow

1. API receives a request to create an order
2. Order is published to Kafka topic
3. Consumer processes the message
4. Polly handles transient failures (retry/circuit breaker)
5. System ensures reliable processing

---

## ⚙️ Tech Stack

- .NET 8
- ASP.NET Core
- Apache Kafka
- Docker / Docker Compose
- Polly
- Entity Framework Core 
- PostgreSQL / SQL Server 

---

## 🛠️ Running the Project

### 🔹 Prerequisites

- Docker & Docker Compose
- .NET SDK 8

---

### 🔹 Run with Docker

```bash
docker-compose up --build
```
### 🔹 Run with Localy

```bash
dotnet build
dotnet run
```

---

####📡 Kafka Topics
order-created → Order creation events
order-processed → Processed orders

---
#####🔁 Resilience with Polly

The application uses Polly to handle failures:

Retry policies
Circuit breaker
Timeout strategies

Ensuring the system remains stable under transient failures.

---

##📈 Features
Event-driven architecture
Asynchronous processing
Fault tolerance
Scalable design
Clean and maintainable code

---
##🧠 Learnings

This project explores:

Designing distributed systems
Handling eventual consistency
Implementing resilience patterns
Working with messaging systems

---
##📂 Project Structure
src/
 ├── API
 ├── Application
 ├── Domain
 ├── Infrastructure

---
## 🚀 Future Improvements
Add observability (OpenTelemetry / Prometheus)
Implement Outbox Pattern
Add authentication & authorization
Improve monitoring and logging
---
##👩‍💻 Author

Senior Backend .NET Developer
Focused on scalable, resilient, and high-performance systems

