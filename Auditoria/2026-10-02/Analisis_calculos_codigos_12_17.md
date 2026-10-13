# Auditoría de cálculos y referencias 12–17

Fecha: 2 de octubre de 2026. Fase: análisis; ninguna implementación funcional.

## Conclusión y límites

No corresponde cambiar una fórmula global para incorporar estos códigos. Hay soporte para conservar la aritmética de NC/ND en 13/14 y aislar el tratamiento del período; 15/16 necesitan completar referencias. REP requiere una ruta de cálculo propia, porque no existe en el servicio. El caso 12 permanece **E — REQUIERE INFORMACIÓN** para su cálculo exacto: la exoneración ordinaria existente no demuestra que se pueda emitir correctamente un crédito financiero posterior.

Se revisaron C#, scripts SQL locales, definiciones desplegadas y consultas SELECT en Pruebas_SincroSapGoSocket y SBO_LARCE. No se ejecutó el servicio, no se ejecutaron los SP de negocio, no se enviaron documentos y no se escribieron datos en ninguna base. Las únicas escrituras son este informe y cuatro copias de definiciones SQL como evidencia. No se realizaron pruebas de aceptación ni de persistencia; abajo se diseñan para la fase posterior.

## Evidencia oficial y catálogos

Fuente normativa: [Anexos actualizados, bitácora 22/04/2026](https://www.hacienda.go.cr/docs/ANEXOS_Y_ESTRUCTURAS_V4.4.pdf), páginas 36, 55–58 y 73–74. La copia antigua del portal ATV tiene 98 páginas y no incorpora esos ajustes; no debe usarse como catálogo actualizado.

Síntesis normativa: Codigo pertenece a Nota 9; TipoDocIR, a Nota 10. En Nota 9: 12 es crédito financiero por exoneración posterior, con efecto en el período de la NC; 13/14 ajustan el período del original; 15 referencia su sustitución por error material; 16 sustituye un rechazado; 17 corresponde exclusivamente a REP. Nota 10 agrega 19 para exportación y 20 para REP. En REP, SubTotal registra el pago para calcular IVA; TotalComprobante suma TotalVenta. Las NC deben respetar el límite acumulado del original y sus ND. IVA devuelto en salud requiere proporcionalidad tarjeta/venta neta, límite al IVA de salud al 4% y conciliación de pagos.

[Comunicado oficial de implementación](https://www.hacienda.go.cr/docs/ActualizacionAnexosyEstructurasVersion4.4CEv2.pdf): los cambios están disponibles en pruebas y producción; implementación desde el 1 de noviembre, con uso anticipado permitido. La bitácora consultada corresponde a 2026.

Las fórmulas y diseños que siguen son hallazgos del sistema o propuestas técnicas; no se presentan como fórmulas fiscales adicionales dictadas por ese comunicado.

## Datos verificados por SELECT

- Pruebas_SincroSapGoSocket tiene dos tablas: Integration.DocumentosPendientes y dbo.ComprobantesElectronicos_Consecutivos.
- Cola: FE DONE 5, NC DONE 4, ND DONE 2, FE FAIL 4, FE PENDING 3, TE PENDING 1. Total 19. Ninguna fila tiene XmlFilePath no vacío. DONE no equivale por sí solo a aceptación fiscal.
- Consecutivos: FE, ND, NC, TE, FEC y FEE; no hay columna REP.
- La cola contiene TaxDate, Clave, DocEntry, ObjType, respuestas de Hacienda/GoSocket y estados. No tiene columnas de montos, referencia o período fiscal específico. No se encontraron restricciones CHECK en esa tabla desplegada: el script local y el despliegue no son idénticos.
- SAP OINV: 79.637 documentos con U_LDT_CodRef vacío; ORIN: 5.784 vacíos, 118 con 1, uno con 2, uno con 6 y uno con 9; OPCH: 96.536 vacíos, tres con 11 y dos con 7. No hay valores 12–17 en esas tres tablas. Estos conteos no incluyen otros posibles orígenes SAP.
- SAP ya tiene U_LDT_CodRef y U_LDT_TipoDocRef en OINV/ORIN/OPCH, además de DocDate/TaxDate, monedas, tasas y totales. No se necesitan UDF nuevos para representar un código.
- Existen ORCT y RCT2; RCT2 contiene DocNum, DocEntry, InvType, SumApplied, AppliedFC, AppliedSys y DocRate. Son candidatos para leer aplicaciones de pagos. Aún no se validó su unión efectiva, asignación por tarifa o cobertura de pagos elegibles para REP.
- appsettings.json apunta actualmente a SincroSapGoSocket. Las consultas de auditoría fijaron explícitamente Pruebas_SincroSapGoSocket. Una primera consulta de catálogos leyó la base configurada antes de corregir esa selección; también fue exclusivamente SELECT. No se debe iniciar el Worker para auditar, porque reclama documentos, actualiza estados y consecutivos y puede enviar a GoSocket.

## Trazabilidad del cálculo actual

Rutas relativas a Sincro_Sap_Gosocket/Sincro_Sap_Gosocket dentro del repositorio. Las copias *.desplegado.txt de esta carpeta permiten revisar las líneas reales del SP de pruebas; no son scripts de despliegue.

1. SAP: OINV/INV1 para ventas y ND; ORIN/RIN1 para NC; OPCH/PCH1 para FEC. Los SP leen también artículos, socios, impuestos y UDF existentes. NC obtiene documentos base desde RIN1 y referencias manuales mediante NumAtCard.
2. ServicioProcesamientoDocumentos.cs:489 selecciona SP para FE/TE, NC, ND y FEC. No hay caso REP. EjecutorProcedimientosSql devuelve DataTable/DataSet; no agrega fórmulas.
3. SQL: CTE DetalleServicioBase → DetalleServicio → ImpuestoBase → Exoneracion/Impuesto → TotalesBase → ResumenFactura → TotalComprobanteFinal.
4. C#: TraductorXml.cs:225 mapea cantidad, precio, bruto, subtotal, base imponible y total de línea. AplicaDescuento/AplicarImpuesto construyen nodos. ConstruirTotalesDesdeSp, línea 515, lee los montos SQL. No condiciona totales por Codigo.
5. DTO/XML: GosocketDte.cs representa DTE/Documento. TraductorXml.cs:604 asigna TpoDocRef, NumeroRef, FechaRef, CodRef y RazonRef. No genera directamente el XML final firmado de Hacienda: GoSocket transforma el DTE. La verificación completa requiere ambos XML.
6. Envío y persistencia: ServicioProcesamientoDocumentos serializa, normaliza, envía y actualiza cola/consecutivos. No hay una rutina encontrada que contabilice ajustes en otro período. TaxDate existente no prueba equivalencia con ese período.

Fórmulas localizadas, con ROUND a cinco decimales:

- En ramas ordinarias, monto bruto = cantidad × precio elegido por SQL; descuento se resuelve desde línea/documento; subtotal = bruto − descuento.
- NC/ND/FE: impuesto ordinario = subtotal × tarifa / 100. Existe una rama cuando subtotal = 0, descuento = bruto y tarifa positiva: impuesto = bruto × tarifa / 100. Autoconsumo tiene su propia condición. No debe reutilizarse la rama de bonificación como crédito financiero sin verificar su semántica.
- Exoneración: utiliza porcentaje autorizado y tarifa efectiva, con tope de proporción en 1. Impuesto neto y distribución gravado/exonerado intervienen en los totales.
- Venta neta = suma de ocho categorías de venta − descuentos; impuesto total suma impuesto neto o bruto según exoneración/fábrica; comprobante = venta neta + impuesto + otros cargos − IVA devuelto.
- Pruebas/SP NC: impuesto 771–780, exoneración hasta 886, venta neta 1161–1170, impuesto 1172–1181 y comprobante 1201–1208. ND: impuesto hasta 683, exoneración hasta 791, resumen 1066–1088 y comprobante 1106–1113. FE: resumen 967–991 y comprobante 1009–1016. FEC: impuesto definitivo 635–636, resumen 686–692 y comprobante 709–716.
- FEC calcula base definitiva incluyendo impuesto específico antes del IVA; su exoneración está fijada a cero. No es una fórmula idéntica a todas las otras familias.
- IVA devuelto está fijado a cero en los cuatro SP desplegados. C# genera un pago único con monto TotalComprobante (líneas 103–106); no representa hoy el reparto de pagos que requeriría el escenario mixto de salud.
- Fallback C# de VlrPagar: venta neta + impuesto si falta TotalComprobante. Omite otros cargos y devolución. Su uso para un escenario nuevo requiere evitar columnas faltantes; no cambiarlo globalmente sin medir documentos afectados.

## Matriz individual

A: informar campos; B: reutilizar cálculo; C: adaptar regla existente; D: cálculo/ruta nueva; E: falta evidencia para cerrar. Una misma fila puede distinguir importe de regla contable.

| Código | ¿Requiere cálculo? | Cálculo actual | Cambio requerido | Ubicación | Impacto en procesos existentes | Estrategia menos invasiva |
|---|---|---|---|---|---|---|
| 12 | E para crédito financiero exacto; tratamiento específico confirmado | NC ordinaria, exoneración proporcional y ramas TaxOnly/bonificación; no rama por 12 | Verificar representación de crédito posterior, original, autorización y ajustes anteriores; corregir razón | SP NC, construcción de líneas/totales y proceso fiscal externo por identificar | Sí si se aplica la fórmula de crédito a todas las NC; FE/ND/FEC comparten traductor | Conservar cálculo base; introducir únicamente diferencia demostrada para NC + 12; detener caso incompleto |
| 13 | B para importes equivalentes; C para período | NC/ND común; fecha propia y referencia del original, sin contabilización específica | Resolver período del original y trazabilidad de sustitución; no retrofechar emisión | InfoReferencia NC/ND; proceso contable pendiente de identificar | Sí si se cambia TaxDate o fecha para todas las notas | Condicionar NC/ND + 13; derivar período del original; preservar importes para entradas equivalentes |
| 14 | B para importes equivalentes; C para período | NC/ND calcula sus propias líneas; no calcula automáticamente diferencia entre versiones | Confirmar si SAP ya entrega el ajuste; resolver período sin imponer fórmula de diferencia | SP NC/ND; proceso contable externo | Sí si se interpreta todo importe SAP como diferencia pendiente | Reutilizar importes de la nota cuando sean el ajuste; lógica adicional solo con evidencia y código 14 |
| 15 | A para referencia; B para documento emitido | FE tiene referencias comentadas en SELECT; NC/ND sí las serializan | Habilitar referencia del nuevo documento y validar vínculo con anulación | SP de documento sustituto y ConstruirReferenciasDesdeSp | Sí si se emiten referencias indiscriminadamente | Completar InfoReferencia solo en sustitución 15; mantener aritmética del documento |
| 16 | A para referencia; B para documento emitido | FE sin referencia activa; cola conserva Clave/estado/respuesta | Identificar rechazado, clave y tipo de referencia; conservar rechazo distinto de error HTTP | SP emisor; validación contra cola/respuesta | Sí si se confunde rechazo fiscal con FAIL o si se reutiliza clave indebidamente | Condición explícita 16 y rechazo verificado; misma fórmula del nuevo comprobante |
| 17 | D para ruta REP ausente; E para asignación de pagos concreta | No SP, selector, consecutivo ni casos de datos REP | Definir fuente de pago, aplicación por documento/tarifa, moneda, repetición y XML GoSocket | Lectura ORCT/RCT2, servicio, SQL mínimo, DTO/mapeo REP | Sí si se usa la fórmula ordinaria o cambia selector por defecto | Incorporar rama REP reutilizando acceso/serialización; fórmula aislada; nunca copiar SP completo de ventas |

TipoDocIR 19/20: cambio de metadatos, sin evidencia de una nueva fórmula por sí mismo. Sin embargo, código local condiciona NC de exportación a Referencia_TipoDoc='09' (TraductorXml.cs:82 y SQL NC:670). Adoptar 19 sin adaptar esa condición puede perder comportamiento de exportación. Conservar compatibilidad solo para entradas históricas cuya semántica se haya comprobado; no tratar todo 09 como exportación, pues ese número pertenece a otro catálogo de referencia.

## Defectos y riesgos observados

- NC y ND normalizan el código a dos dígitos para salida, pero el CASE de razón compara el UDF sin normalizar. SAP contiene '1', '2', '6', '9': pueden salir '01' y razón 'Otros'. Es un defecto existente independiente del cálculo nuevo.
- CASE de razón mantiene 02 como corrección de texto y 03 como corrección de monto; debe revisarse la migración de catálogo antes de alterar datos históricos. 12 conserva descripción anterior y 13–17 terminan en Otros. No normalizar automáticamente datos SAP ni reinterpretar documentos emitidos.
- SQL local NC apunta a SincroSapGoSocket en referencias; desplegado de pruebas apunta a Pruebas_SincroSapGoSocket. Desplegar scripts locales sin revisar nombres podría mezclar claves y consecutivos de ambientes.
- Referencias C# se deduplican por tipo|número|código y se limitan a diez; se omiten si falta tipo o número. Esto necesita revisión para los escenarios admisibles sin número; no vincular esa excepción a Codigo=13 por confundir catálogos.
- La cola no guarda fórmula, montos ni referencias. Puede derivarse información desde SAP y clave original, o conservar XML en el mecanismo existente; aún no se justifica una tabla/columna nueva. No usar respuestas JSON como almacenamiento improvisado de reglas.

## Antes/después y pruebas propuestas

Los ejemplos siguientes son fixtures de diseño, no documentos enviados ni resultados medidos del servicio. El dato real NC 1000004644, línea 0, tiene cantidad 1 y PriceBefDi 274,43; no se convirtió su DocTotal a USD: SAP reporta DocCur USD pero DocTotal 370.698,85 y VatSum 42.646,77. Confundir esos campos con importes en moneda extranjera produciría una comparación inválida. La selección de precio y moneda debe respetar las CTE del SP.

| Caso | Actual / entrada de prueba | Nuevo / resultado esperado | Impacto y comparación |
|---|---|---|---|
| 12 | Una NC ordinaria disminuye venta neta según sus líneas | REQUIERE INFORMACIÓN: no calcular automáticamente un porcentaje de DocTotal; falta crédito autorizado, consumido y representación de línea | Si cambia monto neto de ventas sin corresponder, afecta todas las NC; exigir ejemplo aceptado y conciliación original/autorización |
| 13 | Fixture NC ordinaria: bruto 1000, descuento 0, IVA 13%, total 1130; original agosto, nota septiembre | Mismo fixture numérico 1130 si anulación integral y sin ajustes previos; variar referencia/período según regla identificada | Cálculo numérico sin diferencia; verificar por separado período del original y emisión real |
| 14 | Fixture de corrección ya expresada en SAP: subtotal de ajuste 100, IVA 13, total 113 | Mismos 113; si SAP entrega valor final en vez del ajuste, resultado pendiente de definir | No calcular otra diferencia sobre un importe que ya es diferencial |
| 15 | Fixture sustituto: venta neta 1000, IVA 130, total 1130 | 1130 con vínculo al documento sustituido; secuencia anulación/sustitución verificable | Ningún cambio de fórmula; impedir referencia ausente o duplicación del efecto contable |
| 16 | Fixture nuevo documento con los mismos importes ordinarios | 1130 y vínculo al rechazado verificado | No tomar FAIL como prueba de rechazo; controlar identidad nueva sin modificar documento anterior |
| 17 | Actualmente selector no admite REP | No utilizar 1130 automáticamente como pago; fixture de subtotal de pago P=100 tiene componente IVA 13 a tarifa 13%; TotalVenta/TotalComprobante y moneda requieren fijar entrada REP | Cálculo de componente, no total final inferido. Probar pagos parciales, tarifas mixtas, anulaciones y repetición |

Cada escenario deberá probar A–E, sin escribir SAP:

| Código | A: nuevo | B: existente | C: XML | D: persistencia posterior | E: integración posterior |
|---|---|---|---|---|---|
| 12 | Crédito autorizado, parcial/total y ajustes previos con resultado aprobado | NC 01/06/09 con exoneración y TaxOnly | Líneas, impuesto neto, referencia y razón; sin reducción indebida de venta | Conciliar clave original/nota y estado, sin doble crédito | DTE y XML Hacienda resultante; aceptación del caso aprobado |
| 13 | Anulación entre meses, período original y sustitución si existe | NC/ND 01 mismas entradas, mismo importe | Fecha de emisión real y referencia original; totales iguales | Derivar período y mantener identidad/clave | Flujo anulación y sustitución, sin duplicación |
| 14 | Corrección entre meses; ajustes positivos y negativos según NC/ND | NC/ND 02 con mismo ajuste | Montos del ajuste; referencia y período separados | Reconstrucción del origen y control de acumulado | Documento transformado y respuesta |
| 15 | Documento sustituto de un 13 con referencia completa | FE/TE/FEC sin código nuevo, XML/totales idénticos | Referencia emitida y campos tipo/clave correctos | Claves independientes y enlace verificable | Secuencia aceptada y efecto único |
| 16 | Rechazo fiscal real y nueva emisión | Documento sin código nuevo y caso FAIL técnico | Tipo de referencia, clave rechazada y totales | Conservar rechazo original y nueva fila/clave | Rechazo distinguido de error de transporte |
| 17 | Pago parcial/completo, varias tarifas y monedas; idempotencia | FE/NC/ND/FEC conservan DataTable→DTE original | Tipo REP, detalle, pagos, resumen y referencia 17 | Definir identidad de pago y consecutivo antes de escribir | Contrato GoSocket REP confirmado; sandbox y Hacienda |

Base de regresión disponible en Scripts_Sql/DocumentosProbados.txt: IVA FE 1000064384 / NC 1000004644 / ND 1000000034; exoneración 1000064383 / 1000004645 / 1000000035; tiquete 1000064410 / 1000004656 / 1000000037; combo FE 1000064404 / NC 1000004651; descuento 1000064411 / 1000004659 / 1000000038; exportación 1000064441 / 1000004668 / 1000000045; FEC 87835. Son identificadores listados por el proyecto, no certificaciones de aceptación ni fixtures inmutables. Capturar valores y XML de baseline antes de implementar y comparar decimales exactos; normalizar solo valores dinámicos justificados, no montos ni referencias.

Cambio adicional auditado: IVA devuelto de salud. Hoy cero, pago único; requiere regla aislada por servicio elegible, tarifa y medios de pago. Prueba nueva: venta neta 1000, impuesto elegible 40 y tarjeta 500 → devolución 20; total con impuesto 40 y sin cargos = 1020; otros pagos suman 520. Prueba existente: venta no elegible conserva devolución cero. XML y persistencia deben conciliar pago tarjeta 500 + otros 520. La aplicabilidad al negocio de Larce requiere información; no habilitar globalmente.

## Respuestas obligatorias antes de modificar

| Pregunta | 12 | 13 | 14 | 15 | 16 | 17 |
|---|---|---|---|---|---|---|
| 1. ¿Dónde calcula hoy? | SP NC | SP NC/ND | SP NC/ND | SP del sustituto | SP del emisor | No existe REP |
| 2. ¿Quién comparte? | Todas las NC; traductor común | Todas las NC/ND | Todas las NC/ND | FE/TE/FEC según tipo | Familia del emisor | Comparte transporte/DTO, no fórmula REP |
| 3. ¿Cambio directo? | Puede alterar venta/IVA de cualquier NC | Puede mover períodos ajenos | Puede recalcular dos veces | Puede agregar referencias ajenas | Puede sustituir sin rechazo | Puede inflar pago con IVA/cargos |
| 4. ¿Afecta existentes? | Sí si global | Sí si global | Sí si global | Sí si global | Sí si global | Sí si modifica fórmula común |
| 5. ¿Aislar por código? | Sí: NC+12 | Sí: NC/ND+13 | Sí: NC/ND+14 | Sí: sustituto+15 | Sí: emisor+16+rechazo | Sí: REP+17 |
| 6. ¿Reutilizar? | Base y autorización, sujeto a validación | Fórmula común | Fórmula común si SAP entrega ajuste | Fórmula completa del emisor | Fórmula completa del emisor | Acceso/serialización; no totales ordinarios |
| 7. ¿Modificar SQL? | Metadatos sí; fórmula pendiente | Razón/referencia sí; período según consumidor | Razón/referencia sí; diferencia pendiente | Sí en FE sin referencia | Sí donde falta referencia | Sí para lectura/proyección REP |
| 8. ¿Modificar C#? | Validación; cálculo pendiente | Validación/período si corresponde | Validación/período si corresponde | Solo si contrato actual insuficiente | Validación de rechazo | Sí: selector y mapeo específico |
| 9. ¿BD proyecto? | No demostrado | Derivar primero | Derivar primero | No demostrado | Clave/estado existentes | Consecutivo e identidad necesitan diseño; tabla nueva no demostrada |
| 10. ¿XML/modelos? | Verificar contrato de crédito financiero | Campos presentes; período no demostrado en DTO | Campos presentes; período no demostrado en DTO | Referencia existente | Referencia existente | Verificar contrato REP, agregar solo campos faltantes |
| 11. ¿Sin modificar SAP? | Sí si evidencia existente suficiente | Sí, lectura de original | Sí, lectura de nota/original | Sí | Sí | Sí como diseño objetivo; falta validar origen suficiente |
| 12. ¿Menor riesgo? | No implementar fórmula sin caso probado | Derivar período; mantener monto | Mantener importe de ajuste confirmado | Completar referencia condicionada | Completar referencia validando rechazo | Rama REP pequeña; reutilizar infraestructura |

No se propone SP nuevo para 12–16, ni función, tabla o trigger nuevo. Para REP, primero evaluar una proyección de lectura pequeña dentro del patrón de extracción existente. Un SP independiente solo se justifica si sus uniones de pagos y cardinalidad no pueden incorporarse con claridad y aislamiento; no copiar los procedimientos de venta. Ninguna propuesta requiere crear o modificar objetos de SAP.

## Información pendiente para cerrar aceptación

1. DocNum/caso aprobado de 12, documento original, autorización posterior y monto de crédito esperado, con ajustes anteriores.
2. Responsable/proceso que registra el período fiscal para 13/14. El servicio revisado no contabiliza declaraciones; agregar un if en totales no resuelve ese requisito.
3. Ejemplo de pago REP, relación ORCT/RCT2→facturas, moneda, tarifas, reglas de parcialidad y contrato GoSocket REP.
4. DTE y XML Hacienda de referencia para verificar transformación e importes. La cola de pruebas actual no proporciona archivos.
5. Confirmar aplicabilidad de salud/IVA devuelto y medios de pago múltiples.

Las pruebas A–E están diseñadas, pendientes de implementación posterior y datos suficientes. Esta auditoría no certifica los escenarios nuevos ni propone montos fiscales basados en suposiciones.