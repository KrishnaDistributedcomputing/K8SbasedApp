# Module H – Observability & Monitoring

## Objective

Module H defines the observability strategy — metrics collection, distributed tracing, structured logging, alerting, and dashboards. Every service is observable by default — health checks, structured logs, and telemetry are not optional add-ons but architectural requirements. The goal is end-to-end visibility from API request through service processing to database query.

---

## Observability Pillars

### 1. Metrics (Prometheus + Azure Monitor)

**Collection**: OpenTelemetry SDK exports metrics to Prometheus. Key metrics per service:

| Metric | Type | Labels | Purpose |
|---|---|---|---|
| `http_request_duration_seconds` | Histogram | method, route, status_code | API latency P50/P95/P99 |
| `http_requests_total` | Counter | method, route, status_code | Request throughput |
| `db_query_duration_seconds` | Histogram | query_type, table | Database performance |
| `event_published_total` | Counter | event_type, topic | Event publishing rate |
| `event_consumed_total` | Counter | event_type, consumer_group | Event processing rate |
| `event_processing_errors_total` | Counter | event_type, error_type | Consumer failures |
| `active_connections` | Gauge | service, target | DB/Redis connection pool |
| `cache_hit_ratio` | Gauge | cache_key_pattern | Redis cache effectiveness |

**Dashboard Targets**:
- API Performance: Request rate, error rate, latency percentiles per endpoint
- Database Health: Query duration, connection pool utilization, active transactions
- Event Streaming: Publish/consume rates, consumer lag, DLQ depth
- Infrastructure: Node CPU/memory, pod restarts, HPA scaling events

### 2. Distributed Tracing (OpenTelemetry + Tempo/Jaeger)

**Correlation**: Every incoming HTTP request receives a `X-Correlation-ID`. This ID propagates through:

1. NGINX Ingress → API Gateway (HTTP header)
2. API Gateway → Backend Service (HTTP header via DelegatingHandler)
3. Backend Service → Database (span context)
4. Backend Service → Event Hub (message property)
5. Consumer Worker → Processing (extracted from message)

**Trace Structure**:
```
[Root Span] POST /api/v2/patients
  └── [Span] PatientService.CreatePatient
      ├── [Span] EhrDbContext.SaveChanges (PostgreSQL)
      ├── [Span] RedisCache.Invalidate
      └── [Span] KafkaProducer.Publish (ehr.patient.events)
          └── [Span] AuditWorker.Consume
              └── [Span] AuditDbContext.SaveChanges
```

### 3. Structured Logging (Serilog + Loki/Application Insights)

**Log Format**: JSON structured logs with consistent fields:

```json
{
  "timestamp": "2026-03-22T14:30:00.123Z",
  "level": "Information",
  "messageTemplate": "Patient {PatientId} created by {UserId}",
  "properties": {
    "PatientId": 15,
    "UserId": "clinician@hospital.ca",
    "CorrelationId": "corr-x1y2z3",
    "ServiceName": "patient-service",
    "Version": "v2",
    "Duration": 45
  }
}
```

**Log Levels by Environment**:

| Environment | Default Level | Debug Services | Retention |
|---|---|---|---|
| Production | Warning | Error | 30 days |
| Staging | Information | Debug | 14 days |
| Development | Debug | Verbose | 7 days |
| POC | Information | Debug | 7 days |

---

## Current Health Check Implementation

Every service exposes a health endpoint:

```csharp
app.MapGet("/health", () => Results.Ok(new {
    status = "healthy",
    service = "ehr-api",
    timestamp = DateTime.UtcNow
}));
```

### Target: Comprehensive Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connStr, name: "postgresql")
    .AddRedis(redisConnStr, name: "redis")
    .AddAzureServiceBusSubscription(sbConnStr, topicName, subscriptionName, name: "servicebus")
    .AddCheck("self", () => HealthCheckResult.Healthy());

app.MapHealthChecks("/health/live", new() {
    Predicate = check => check.Name == "self"  // Liveness
});
app.MapHealthChecks("/health/ready", new() {
    Predicate = _ => true  // Readiness (all dependencies)
});
```

---

## Target Monitoring Stack

### Prometheus + Grafana (In-Cluster)

| Component | Purpose | Namespace |
|---|---|---|
| Prometheus | Metrics collection and storage | monitoring |
| Grafana | Dashboard visualization | monitoring |
| AlertManager | Alert routing and notification | monitoring |
| Loki | Log aggregation | monitoring |
| Tempo | Distributed trace storage | monitoring |

### Azure Monitor (Cloud-Native)

| Component | Purpose |
|---|---|
| Application Insights | APM, distributed tracing, live metrics |
| Azure Monitor Metrics | AKS cluster and node metrics |
| Log Analytics Workspace | Centralized log storage and KQL queries |
| Azure Alerts | Threshold-based alerting with action groups |

---

## Alert Rules

| Alert | Condition | Severity | Action |
|---|---|---|---|
| High Error Rate | HTTP 5xx > 5% for 5 min | Critical | PagerDuty + Teams |
| High Latency | P95 > 2s for 5 min | Warning | Teams notification |
| Pod Restart Loop | Restart count > 3 in 10 min | Critical | PagerDuty |
| Database Connection Failures | Connection errors > 0 for 2 min | Critical | PagerDuty + Teams |
| DLQ Depth > 0 | Dead-letter messages accumulating | Warning | Teams notification |
| Consumer Lag > 1000 | Kafka consumer falling behind | Warning | Teams notification |
| Node CPU > 85% | Sustained for 10 min | Warning | Auto-scale trigger |
| Certificate Expiry < 14d | cert-manager certificate TTL | Warning | Teams notification |

---

## Dependencies

- **Upstream**: Module B (AKS for monitoring stack deployment), Module F (services emit telemetry)
- **Downstream**: Module K (resiliency testing validates observability)
