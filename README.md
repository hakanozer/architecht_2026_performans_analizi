# 📊 Observability & Monitoring Stack (Docker)

Bu proje, modern bir **observability (izlenebilirlik)** ve **code quality (kod kalitesi)** altyapısını lokal ortamda çalıştırmak için hazırlanmıştır.

---

## 🧱 Stack İçeriği

Bu Docker stack aşağıdaki bileşenleri içerir:

- 📈 **Metrics:** Prometheus + Grafana  
- 📜 **Logging:** Elasticsearch + Logstash + Kibana (ELK Stack)  
- 🔍 **Tracing:** Jaeger + OpenTelemetry  
- 🧪 **Code Quality:** SonarQube  

---

## 🚀 Servis Özeti

| Servis | Açıklama | URL | Default Kullanıcı | Default Şifre |
|--------|---------|-----|------------------|---------------|
| **Prometheus** | Metric toplama ve time-series veri tabanı | http://localhost:9090 | - | - |
| **Grafana** | Dashboard ve görselleştirme aracı | http://localhost:3000 | admin | admin |
| **Elasticsearch** | Log ve veri arama motoru | http://localhost:9200 | - | - |
| **Logstash** | Log işleme ve pipeline aracı | http://localhost:5001 | - | - |
| **Kibana** | Elasticsearch UI (log görüntüleme) | http://localhost:5601 | - | - |
| **Jaeger** | Distributed tracing aracı | http://localhost:16686 | - | - |
| **OpenTelemetry Collector** | Telemetry verilerini toplar ve yönlendirir | http://localhost:14317 / 14318 | - | - |
| **SonarQube** | Kod kalitesi ve statik analiz aracı | http://localhost:9000 | admin | admin |
| **PostgreSQL (Sonar DB)** | SonarQube veritabanı | - | sonar | sonar |

---

## 🧠 Ne İşe Yarar?

### 📈 Monitoring
- **Prometheus**: Uygulamalardan metric toplar (CPU, memory, request vb.)
- **Grafana**: Bu metricleri dashboard olarak görselleştirir

### 📜 Logging (ELK Stack)
- **Logstash**: Logları toplar ve işler  
- **Elasticsearch**: Logları saklar ve indexler  
- **Kibana**: Logları UI üzerinden görüntüler  

### 🔍 Tracing
- **OpenTelemetry**: Trace verisini toplar ve yönlendirir  
- **Jaeger**: Microservice request akışını izler  

### 🧪 Code Quality
- **SonarQube**: Kod kalitesi, security açıkları ve code smell analizi yapar  

---

## ⚙️ Kurulum & Çalıştırma

### Container'ları başlat

```bash
docker-compose down
docker-compose up -d
```

### 🔑 Önemli Notlar
- Grafana ilk girişte şifre değiştirmeni ister
- SonarQube ilk login sonrası şifre değiştirmeni ister
- Elasticsearch security kapalıdır (authentication yok)
- Tüm servisler localhost üzerinden erişilebilir

### 🧩 Mimari Genel Bakış
```[Application]
   │
   ├── Metrics  → Prometheus → Grafana
   ├── Logs     → Logstash → Elasticsearch → Kibana
   └── Traces   → OpenTelemetry → Jaeger
```
