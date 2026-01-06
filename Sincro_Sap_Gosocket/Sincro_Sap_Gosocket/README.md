📦 Servicio Windows – Sincro SAP → GoSocket
1️⃣ Visión general del proyecto

Este proyecto es un Servicio de Windows (.NET Worker Service) encargado de:

Detectar documentos electrónicos generados en SAP:

Facturas electrónicas

Notas de crédito

Notas de débito

Obtener la información desde SQL Server mediante procedimientos almacenados.

Traducir los datos del formato Factura Electrónica CR v4.4 al XML requerido por GoSocket (xDoc Global).

Enviar los documentos a GoSocket.

Registrar el estado y la respuesta del envío.

Mantener un historial y trazabilidad mediante logs.

El servicio está diseñado bajo principios de:

Clean Code

SOLID

Separación de responsabilidades

Mantenibilidad y escalabilidad

2️⃣ Estructura general de carpetas
Sincro_Sap_Gosocket
│
├── Aplicacion
│   ├── Interfaces
│   └── Servicios
│
├── Comunes
│
├── Configuracion
│
├── Dominio
│   ├── Entidades
│   └── Enumeraciones
│
├── Infraestructura
│   ├── Gosocket
│   ├── Logs
│   ├── Sap
│   └── Sql
│
├── Mapeo
│
├── appsettings.json
├── Program.cs
└── Worker.cs

3️⃣ Descripción detallada por carpeta
📁 Aplicacion

Contiene la lógica de orquestación del negocio, sin dependencias técnicas directas.

📂 Interfaces

Define contratos que permiten desacoplar la lógica del negocio de la infraestructura.

Ejemplos:

IClienteGosocket → contrato para enviar documentos

IRepositorioColaDocumentos → acceso a cola de documentos pendientes

ITraductorXml → contrato para traducir FE → XML GoSocket

📌 Regla: aquí solo hay interfaces, nunca implementación.

📂 Servicios

Contiene los casos de uso del sistema.

Ejemplo:

ServicioProcesamientoDocumentos

Decide qué documentos procesar

Llama a SAP / SQL

Invoca el traductor

Envía a GoSocket

Actualiza estados

📌 No accede directamente a SQL ni HTTP, solo a interfaces.

📁 Comunes

Código reutilizable y transversal al proyecto.

Ejemplos:

Validaciones → reglas comunes

PoliticaReintentos → reintentos controlados

Extensiones → métodos helper

📌 No depende de ninguna capa específica.

📁 Configuracion

Clases que representan la configuración del sistema.

Ejemplos:

OpcionesGosocket

OpcionesSap

OpcionesSql

OpcionesServicio

Se cargan desde:

appsettings.json


📌 Permite cambiar ambientes sin tocar código.

📁 Dominio

Representa el modelo del negocio, completamente independiente de la tecnología.

📂 Entidades

Objetos del negocio:

DocumentoPendiente

ResultadoEnvio

RespuestaGosocket

📌 No contienen lógica técnica.

📂 Enumeraciones

Valores controlados del dominio:

TipoDocumento (FE, NC, ND)

EstadoDocumento (Pendiente, Enviado, Error)

📁 Infraestructura

Implementaciones técnicas concretas.

📂 Gosocket

Comunicación con GoSocket:

Cliente HTTP

Modelos de request/response

Manejo de errores

📌 Implementa IClienteGosocket.

📂 Logs

Configuración y enriquecimiento de logs.

Ejemplo:

EnriquecedorLogs → agrega correlación, documento, ambiente

📌 Los logs son fundamentales en servicios Windows.

📂 Sap

Acceso a SAP / Base de datos SAP.

Ejemplo:

LectorSapSql → consulta documentos creados

📂 Sql

Acceso a SQL Server.

Ejemplos:

EjecutorProcedimientosSql

RepositorioColaDocumentosSql

RepositorioEstadosSql

📌 Implementa los repositorios definidos en Aplicacion.

📁 Mapeo

Responsable de traducir datos entre estructuras.

Ejemplos:

Mapeo de resultados de SP → Entidades

Mapeo de Entidades → XML GoSocket

📌 Aquí vive la lógica de transformación, no en servicios.

4️⃣ Flujo general del servicio

Worker se ejecuta periódicamente.

Consulta si hay documentos pendientes (cola o control de consecutivos).

Por cada documento:

Obtiene datos desde SQL (SP).

Mapea datos a entidades de dominio.

Traduce a XML GoSocket.

Envía a GoSocket.

Registra respuesta y estado.

Guarda logs y resultados.

Espera siguiente ciclo.

5️⃣ Puntos clave de diseño

El Worker no tiene lógica de negocio, solo coordina.

La lógica vive en Aplicacion.

La infraestructura se puede cambiar sin romper el negocio.

El proyecto es testeable, mantenible y extensible.