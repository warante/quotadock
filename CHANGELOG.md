# Changelog

## 1.2.0 — 2026-10-22 (hora peninsular española)

- Un único icono en la bandeja, con las dos barras del proveedor seleccionado.
- Selector de proveedores mediante pestañas al pulsar el icono; admite navegación con flechas y todas las regiones.
- Selección persistente y alternativa automática al desactivar el proveedor visible.
- Los demás proveedores siguen actualizándose y notificando sin generar iconos adicionales.
- Cambiar de pestaña muestra los datos ya consultados, sin nuevas peticiones ni reiniciar las esperas.
- Vistas previas de la interfaz con datos ficticios en el README y en el ZIP portable.
- Panel redondeado, pestañas dibujadas a mano con hover y foco de teclado, barras suaves, tipografía cuidada y menú oscuro; sin dependencias ni temporizadores gráficos adicionales.
- Pruebas de repintado del panel y actualización de iconos para comprobar que no se acumulan recursos GDI.

## 1.1.0 — 2026-10-22 (hora peninsular española)

- Un único ejecutable, selector de proveedores e inicio automático desde el menú.
- Conectores para OpenCode Go/Go Plus, Kimi Code, MiniMax Token/M Plan, Z.AI/Zhipu, Copilot y Gemini CLI, con regiones explícitas y etiquetas de cuota reales.
- Avisos opcionales con umbral y protección frente a avisos repetidos.
- Identificación por color, espera independiente por proveedor y respeto de `Retry-After`.
- Claves manuales cifradas mediante Windows DPAPI; sesiones oficiales de solo lectura.
- Respuestas limitadas, redirecciones bloqueadas y mensajes sin datos privados del servidor.
- Instalación con rollback y pruebas aisladas de instalación y desinstalación.
- Apertura al finalizar la instalación, con opción para desactivarla y diagnóstico separado del bloqueo por políticas de Windows.
- Preparación de firma Authenticode y verificación estricta opcional; sin certificado disponible, los artefactos siguen sin firmar.
- Investigación documentada de proveedores adicionales, sin anunciar soporte no implementado.

## 1.0.0 — 2026-10-22

- Lanzamiento inicial de **QuotaDock** para Windows 10/11.
- OpenAI/Codex y Claude/Claude Code, en ejecutables independientes.
- Dos barras de porcentaje restante: 5 horas y semana.
- Consultas cada 60 segundos y reintentos espaciados ante errores.
- ZIP portable e instalador por usuario, con inicio automático opcional y desinstalación.
- Credenciales locales de solo lectura, sin telemetría ni almacenamiento de tokens.
- Pruebas con datos sintéticos y plantilla opcional de automatización para Windows.
