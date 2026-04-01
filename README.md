# 🚀 Order Processing API - .NET | Kafka | Docker | Polly

Backend application built with .NET focused on scalable and resilient processing using event-driven architecture.

## 📌 Overview

This project demonstrates the implementation of a backend system using:

- **.NET 8 / C#**
- **Apache Kafka** for asynchronous messaging
- **Docker** 
- **Kubernetes (Minukube)** 
- **Saga Pattern** 
- **Workers com BackgroudService** 
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
- **Infrastructure Layer** → Kafka, persistence, external services, workers

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
- Kubernetes (Minikube)
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
### 🔹 Build with Kubernetes and Load Minicube

```bash
docker build -t ordersystem-api:vX -f .\Order.Api\Dockerfile
minikube image load ordersysetm-api:vX
```

---
### 🔹 Deploy Minicube

```bash
kubectl apply -f .\k8s\postgres.yaml
kubectl apply -f .\k8s\kafka.yaml
kubectl apply -f .\k8s\migration-job.yaml
kubectl apply -f .\k8s\orders-api-deployment.yaml
kubectl apply -f .\k8s\orders-workers-deployment.yaml
```

---
### 🔹 Basic Observability 

```bash
kubectl get pods -n order-system
kubectl logs <pod-name> -n order-system 
```

---


## 🔁 Resilience with Polly

The application uses Polly to handle failures:

Retry policies
Circuit breaker
Timeout strategies

Ensuring the system remains stable under transient failures.

---

## 📈 Features
Event-driven architecture
Asynchronous processing
Fault tolerance
Scalable design
Clean and maintainable code

---
## 🧠 Learnings

This project explores:

Designing distributed systems
Handling eventual consistency
Implementing resilience patterns
Working with messaging systems

---

## 📂 Project Structure
```bash
src/
 ├── API
 ├── Application
 ├── Domain
 ├── Infrastructure
```
---
## 🚀 Future Improvements
Add observability (OpenTelemetry / Prometheus)
Add authentication & authorization
Improve monitoring and logging

---
## 👩‍💻 Author

Senior Backend .NET Developer
Focused on scalable, resilient, and high-performance systems

