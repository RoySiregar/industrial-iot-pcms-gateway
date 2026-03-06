# ?? Industrial IoT & PCMS Gateway (Microservices Architecture)

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![MQTT](https://img.shields.io/badge/MQTT-660066?style=for-the-badge&logo=mqtt&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![Vue.js](https://img.shields.io/badge/Vue.js-35495E?style=for-the-badge&logo=vue.js&logoColor=4FC08D)
![Security](https://img.shields.io/badge/Security-JWT_Auth-black?style=for-the-badge&logo=jsonwebtokens&logoColor=white)

## ?? Web Dashboard Preview
<img width="986" height="688" alt="dashboard-preview" src="https://github.com/user-attachments/assets/98618748-daf3-453f-aa7a-481730c88fe4" />
<img width="975" height="684" alt="edgegateway_alarm-preview" src="https://github.com/user-attachments/assets/b278e5da-658b-4c30-be38-ff2b9cff8352" />



## ?? Overview
This project is an **End-to-End Industrial IoT (IIoT) Data Pipeline** designed to bridge the gap between shop floor operations (Heavy Machinery, Welding Machines, CNCs) and enterprise systems like **Project Control Management Systems (PCMS)**. 

Targeting high-tech manufacturing, Marine, Offshore, and EPCI sectors, this solution implements a decoupled, event-driven microservices architecture to ensure high fault tolerance, enterprise-grade security, and real-time data visibility.

## ??? System Architecture

The system is fully decoupled into 4 separate modules. If the database goes down, the edge gateway continues to publish data. If the edge device loses connection, the dashboard remains active showing historical data.

`[Heavy Machinery / Modbus]` ? `[Edge Gateway]` ? `[MQTT Cloud Broker]` ? `[Data Logger]` ? `[MySQL]` ? `[Secure REST API]` ? `[Vue.js SPA Dashboard]`

### ?? Repository Structure
This monorepo contains the following isolated services:

1. **`ModbusCollector/` (Edge Gateway)**
   - A C# background service acting as an Industrial Protocol Translator.
   - Polls legacy Modbus TCP registers (simulating a Welding Machine) at 1Hz.
   - Wraps raw OT data into structured JSON and publishes it to a cloud MQTT broker.
2. **`PcmsDataLogger/` (Backend Consumer)**
   - A C# service that subscribes to the MQTT broker.
   - Safely parses incoming telemetry and persists it into a MySQL relational database for historical auditing and OEE calculations.
3. **`PcmsApi/` (Enterprise REST API)**
   - A modern .NET Minimal API utilizing `Dapper` (Micro-ORM) for blazing-fast database queries.
   - **Secured with JWT (JSON Web Tokens)** to ensure only authorized frontend applications can access manufacturing data.
   - Serves real-time machinery status and historical trends to the frontend.
4. **`PcmsDashboard/` (Frontend UI)**
   - A responsive, dark-mode SCADA-like Single Page Application (SPA) dashboard built with Vue.js 3, Tailwind CSS, and Chart.js.
   - Features real-time live charts, equipment status indicators, dynamic sidebar navigation, and automated JWT Bearer Token injection.

## ?? Key Features
- **IT/OT Convergence:** Seamlessly translates industrial protocols (Modbus) into IT standards (JSON/REST).
- **Enterprise Security (JWT):** API endpoints are locked down and require valid authentication tokens.
- **Fault-Tolerant Microservices:** Edge devices and databases are decoupled via an MQTT message broker.
- **Data Fluctuation Simulation:** Includes a realistic noise/fluctuation generator when the machine is in the "Running/Welding" state.
- **Real-Time Visualization:** Zero-refresh dashboard with automated HTTP short polling.

## ??? Tech Stack
- **Backend & Edge Services:** C# .NET 9.0, NModbus4, MQTTnet
- **Database & ORM:** MySQL (Laragon), Dapper
- **Frontend:** HTML5, Vue.js 3, Tailwind CSS, Chart.js
- **Protocols:** Modbus TCP, MQTT, HTTP/REST

## ?? Getting Started (Local Development)

### Prerequisites
- .NET 9.0 SDK
- MySQL Server (e.g., Laragon or XAMPP)
- Modbus Simulator (e.g., ModbusHD)

### 1. Database Setup
Execute the following SQL query to set up the telemetry table:
```sql
CREATE DATABASE pcms_iot;
USE pcms_iot;
CREATE TABLE Machine_Telemetry_Log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    timestamp DATETIME NOT NULL,
    machine_id VARCHAR(50) NOT NULL,
    status_code INT,
    status_text VARCHAR(20),
    voltage INT,
    ampere INT,
    operating_hours INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### 2. Run the service
```
# Terminal 1: Start Edge Gateway
cd ModbusCollector && dotnet run

# Terminal 2: Start Data Logger
cd PcmsDataLogger && dotnet run

# Terminal 3: Start Secure REST API
cd PcmsApi && dotnet run

```

### 3. Open Dashboard
Navigate to the PcmsDashboard folder and open dashboard.html in any modern web browser.
(Note: The dashboard is pre-configured to automatically authenticate with the API using default demo credentials).

- **Username:** admin
- **Password:** pcms2026
