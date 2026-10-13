# Analisis previo — identificaciones y referencias Hacienda

Fecha: 8 de octubre de 2026. Estado: propuesta pendiente de autorizacion; sin implementacion.

## Verificacion y preservacion

Rama creada: `codex/hacienda-identificaciones-referencias`, desde `main`. Se conservaron los archivos locales no rastreados de Auditoria y .vs; no se hizo stash, reset ni commit. `git diff --stat` no muestra cambios de archivos rastreados.

Conexion real verificada con SELECT: destino dilarceappsynch.com, servidor SQL reportado SBO2, base Pruebas_SincroSapGoSocket. Se leyeron nuevamente los cuatro SP desplegados y metadatos de SAP SBO_LARCE. Evidencia: Metadatos.txt y cuatro archivos .desplegado.txt en esta carpeta. No se ejecutaron SP de negocio ni el Worker, ni se enviaron comprobantes. El servicio existente actualiza SAP: no iniciarlo en esta fase.

La configuracion principal apunta a SincroSapGoSocket, no a pruebas. Las consultas fijaron expresamente la base solicitada sin cambiar configuracion. Un primer intento de construir la conexion fallo al asignar propiedades PowerShell; no constituye verificacion de pruebas. La verificacion valida posterior uso Initial Catalog explicito. No se escribio en ninguna base.

## Fuente y alcance

[Anexos oficiales V4.4, bitacora 22/04/2026](https://www.hacienda.go.cr/docs/ANEXOS_Y_ESTRUCTURAS_V4.4.pdf), paginas 20, 67, 69, 73 y 74; [comunicado oficial](https://www.hacienda.go.cr/docs/ActualizacionAnexosyEstructurasVersion4.4CEv2.pdf). Consultados nuevamente en esta revision. Juridica: 10 caracteres sin guiones; segmento de identificacion en clave: 12 caracteres con relleno. Mantener formato de fisica, DIMEX y NITE. No inventar una longitud de seis caracteres para XML a partir de otras noticias.

Nota 9 agrega 13-17 y modifica 12. 13/14 afectan el periodo del documento original; 15 identifica sustitucion por error material; 16 sustitucion de rechazado; 12 es credito financiero por exoneracion posterior y afecta el periodo de la nota. 17 solo puede usarse en REP. Nota 10 agrega 19 exportacion y 20 referencia a REP. Reconocer 20 no agrega generacion, pagos, consecutivos ni procesamiento REP. No incorporar devolucion IVA salud ni otras reformas fuera del alcance.

## Inventario y propuesta minima

Las rutas siguientes son relativas a Sincro_Sap_Gosocket/Sincro_Sap_Gosocket. Las lineas SQL citadas corresponden a las copias desplegadas, no a los scripts locales.

| Ajuste | Comportamiento actual comprobado | Propuesta y puntos afectados |
|---|---|---|
| Identificacion juridica | Dominio/GosocketDte.cs IDEmisor:169 y NroDocRecep:249 son string; TraductorXml.ConstruirEncabezadoDesdeSp usa GetString. OADM.TaxIdNum/OCRD.LicTradNum son nvarchar(32). Receptor conserva letras al quitar guiones/espacios. | Conservar DTO, columnas y mapeo. Validar solo tipo 02 alfanumerico en el metodo actual de encabezado, sin agregar un sistema general de validaciones. |
| Clave con letras | ClaveGenerada convierte Emisor_Numero a BIGINT en FE:197, NC:421 y ND:336. | CASE exclusivo para juridica alfanumerica; conservar conversion antigua para demas entradas. Scripts_Sql/6,7,9 y sus dbo.SP_Consulta_* correspondientes. FEC ya usa texto para el receptor que genera clave; sin cambio de formula. |
| Referencia a clave con letras | NC valida U_LDT_FiscalDoc exclusivamente numerico en seis bloques:174-215. | Ampliar solo segmento de cedula (10-21) a letras/numeros; conservar longitud y segmentos numericos restantes. No aceptar letras en toda la clave indiscriminadamente. |
| Nota 9 | NC:350-367 y ND:257-272 incluyen 01-12/99; 12 tiene texto anterior. 13-17 no tienen ramas. CodRef es string y pasa dos digitos. | Sustituir descripcion 12 y agregar 13-16 en CASE actual. Reconocer 17 con restriccion REP en ConstruirReferenciasDesdeSp; no habilitarlo en las familias implementadas. No cambiar formulas por agregar codigos. |
| Nota 10 | Tipo manual ya se propaga en NC/ND/FEC. NC deriva tipo desde clave; 09 significa FEE en catalogo de comprobantes, pero no en Nota 10. | Traducir tipo de clave 09 a tipo referencia 19; normalizar tipos manuales de un digito; admitir 20 como referencia. Ajustar solo condiciones NC de exportacion:663 y 1246; TraductorXml.ConstruirEncabezadoDesdeSp:78 debe reconocer 19 conservando 09 historico hasta comprobar origen. |
| Sustitucion 15/16 | FE tiene columnas/LEFT JOIN de referencia comentados (:1155-1156,1185); no existe CTE activa. TraductorXml.ConstruirReferenciasDesdeSp:584 ya serializa referencias completas. | Agregar una CTE pequena de lectura y proyectar referencia solo para escenarios autorizados. Reutilizar UDF/cola y el metodo existente; fijar clave y fecha originales antes de redactar ALTER completo. No descomentar un JOIN a una CTE inexistente. |
| FEC | InfoReferencia:194-220 permite tipo manual; codigo 11 y tipo 16 especiales para proveedor extranjero; razon generica. | Conservar esa prioridad y defaults. Para referencias nuevas validas, seleccionar razon del catalogo solo en rama nueva; no modificar compra extranjera. Codigo 12 exclusivo NC, no habilitar en FEC/ND. |

## Conflicto SAP comprobado

CUFD/UFD1 de OINV, ORIN y OPCH: Nota 9 tiene 12, no tiene 13-17; Nota 10 tiene 19="Otros", no tiene 20 ni 99. Es un conflicto real con 19 exportacion. No se modificaron estos catalogos y no se proponen UPDATE directos a tablas SAP.

La compatibilidad completa requiere resolver como se seleccionaran los nuevos codigos usando los UDF actuales. Un cambio del catalogo SAP requiere autorizacion especifica y el mecanismo soportado de SAP; queda separado de la autorizacion al proyecto. No convertir historicamente 19 a exportacion ni reasignarlo a 99 de forma masiva. Revisar documentos con ese valor y distinguir datos historicos. El catalogo SQL de razones ademas compara '01' contra valores SAP '1'; defecto preexistente. Corregir solo si se autoriza normalizacion en el CASE actual, manteniendo significados de los codigos antiguos fuera de alcance.

## Codigo 12 y limites funcionales

Actualizar el texto no demuestra cumplimiento del credito financiero. Los SP NC calculan lineas, impuestos y exoneracion ordinarios, sin rama por 12. La auditoria previa del 2 de octubre documenta esa limitacion; se usa como contexto, no como prueba nueva de datos. No proponer una formula global ni aplicar exoneracion ordinaria automaticamente a un credito posterior. Se necesita un caso concreto con factura original, autorizacion posterior, monto esperado y ajustes previos para determinar si SAP ya expresa correctamente el credito. Hasta resolverlo no se declara aceptacion funcional de 12 ni se ofrece un cambio de importes especulativo.

13/14 requieren periodo original en declaraciones; el servicio no muestra un proceso de declaracion fiscal. No retrofechar emision ni modificar TaxDate automaticamente. Identificar el consumidor contable y comprobar si ya aplica esa regla. Para 16 verificar rechazo fiscal del original; FAIL de transporte no basta.

## SQL entregado y objetos excluidos

Propuesta_bloques.sql contiene expresiones y bloques de sustitucion para revision, NO un despliegue ejecutable. Se entregan asi para mostrar precisamente la intervencion sin copiar procedimientos completos ni completar origenes no verificados. Los ALTER finales se prepararan sobre las definiciones de pruebas capturadas, cuando se resuelvan las dependencias. No se ejecutaron estos bloques.

Objetos candidatos: dbo.SP_Consulta_FE_FES_V44, dbo.SP_Consulta_NC_NCS_V44, dbo.SP_Consulta_ND_NDS_V44 y, solo por razon de referencias nuevas, dbo.SP_Consulta_FEC_V44. Sin nuevas tablas, funciones, vistas, triggers, SP ni cambios de tipos de columnas. Integration.DocumentosPendientes.Clave ya es varchar(50). C# candidato: TraductorXml.ConstruirEncabezadoDesdeSp y ConstruirReferenciasDesdeSp. GosocketDte, calculos de totales, selector de documentos, transporte y actualizacion SAP no requieren cambios para ampliar catalogos.

## Riesgos y pruebas requeridas

1. Regresion de fisica/juridica numerica/DIMEX/NITE: comparar clave y DTE antes/despues con mismas entradas; conservar ceros y montos. Juridica alfanumerica de 10 caracteres: emisor/receptor y FEC, clave 50 y segmento 12; rechazar longitud/caracteres invalidos antes de truncar.
2. NC con clave SAP alfanumerica sin clave en cola: conservar referencia completa. Claves numericas y prioridades COALESCE actuales deben dar igual resultado.
3. Nota 9: casos 12-16, razon especifica y referencia completa; bloquear 17 en FE/NC/ND/FEC. Regresion codigos existentes, incluidas representaciones SAP de un digito.
4. Nota 10: 19 exportacion debe mantener impuestos/ubicacion aplicables a NC de exportacion; 20 debe salir en referencia sin activar REP. Revisar 19 historico="Otros" antes de cualquier reinterpretacion.
5. FE sustituto 15/16: clave y fecha originales, no las del nuevo documento; caso de rechazo fiscal y de fallo HTTP, sin confundirlos. FEC extranjera 11/16 conserva salida existente.
6. 12: conciliar credito autorizado y ajustes previos; 13/14: revisar periodo con responsable contable sin modificar fechas de emision. No certificar estos efectos solo con XML.
7. Comparar decimales exactos de lineas/totales de los casos actuales en Scripts_Sql/DocumentosProbados.txt. No ejecutar SP ni Worker sin revisar previamente efectos; usar fixtures desconectados para la primera validacion.
8. Validar DTE emitido y XML final transformado por GoSocket contra esquema/ambiente de pruebas, con autorizacion separada para envios. Hoy no hay prueba de aceptacion fiscal nueva.

## Decision requerida

Autorizar los ajustes delimitados del proyecto despues de revisar esta propuesta. No incluye escrituras SAP, ejecucion de scripts SQL, envios ni REP. Para completar el diseño hacen falta: un caso 12 con importe esperado; fuente/caso de referencia original para 15/16; decision sobre conflicto 19 SAP y mecanismo permitido de seleccion de nuevos codigos; responsable del periodo contable 13/14. Estas dependencias no justifican alterar arquitectura ni formulas generales.
