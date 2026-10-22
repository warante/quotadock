# Privacy policy / Política de privacidad

QuotaDock no tiene servidor propio, telemetría, publicidad ni analítica. No envía estadísticas al mantenedor, a Piconera Tech ni a SignPath. La consulta de cuotas es una conexión de red y no debe describirse como un funcionamiento completamente offline.

## Conexiones que eliges

En una instalación nueva no se activa ningún proveedor: tú eliges cuáles consultar. En una actualización se conservan los proveedores que ya habías activado.

Al activar un proveedor, QuotaDock envía su credencial de autorización **solo al endpoint HTTPS fijo de ese proveedor**, para leer cuotas y, cuando estén disponibles, fechas de reinicio. [PROVIDERS.md](PROVIDERS.md) enumera los hosts y los tipos de cuenta. No se siguen redirecciones con credenciales ni se prueban claves en otras regiones. Gemini CLI puede necesitar dos consultas al mismo servicio de Google para resolver el proyecto y las cuotas.

Los servicios externos reciben la petición, su IP de origen y los datos necesarios para autenticarla. Aplican sus propias políticas: [OpenAI](https://openai.com/policies/privacy-policy/), [Anthropic](https://www.anthropic.com/legal/privacy), [OpenCode](https://opencode.ai/legal/privacy-policy), [Kimi](https://www.kimi.com/), [MiniMax](https://www.minimax.io/), [Z.AI](https://z.ai/), [BigModel](https://open.bigmodel.cn/), [GitHub](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement) y [Google](https://policies.google.com/privacy). Para servicios regionales, consulta las condiciones del sitio en el que contrataste el plan.

## Sesiones y almacenamiento local

- Las sesiones de Codex, Claude Code y Gemini CLI se leen de sus archivos locales; no se modifican, copian al proyecto ni renuevan sus tokens.
- Si activas Copilot y no proporcionas otra credencial, puede ejecutarse `gh auth token --hostname github.com` del GitHub CLI instalado en Program Files. Su salida no se imprime ni se guarda.
- Las credenciales introducidas manualmente se guardan cifradas con Windows DPAPI para tu usuario, en `%APPDATA%\QuotaDock\credentials`. No se pueden transportar sin más a otra cuenta o equipo.
- `%APPDATA%\QuotaDock\settings.json` guarda proveedores habilitados, selección visible y preferencias. `cooldowns.json` conserva la próxima consulta permitida, no tu historial de uso.
- Las cuotas, respuestas y fechas de reinicio se mantienen en memoria; no se crea una base de datos de consumo ni un registro de las respuestas privadas del servidor.

## Control y eliminación

Desactiva un proveedor en el menú para detener sus consultas. **Olvidar clave** elimina la credencial que hayas guardado en QuotaDock; no elimina variables de entorno ni cierra sesiones de otros programas. Desactivar todos los proveedores deja únicamente el icono de control, sin consultas de cuota.

El instalador indica dónde se instala, ofrece inicio automático opcional y una opción para abrir la aplicación al terminar. La desinstalación no borra tus sesiones oficiales y conserva ajustes y claves cifradas. Usa **Olvidar clave** antes de desinstalar si quieres retirar estas últimas; para eliminar los ajustes restantes, cierra QuotaDock y elimina únicamente su carpeta `%APPDATA%\QuotaDock` tras comprobar que no necesitas conservarla.

## Web, descargas y firma

GitHub aloja el repositorio y las descargas y aplica su política de privacidad. La web de Piconera Tech y sus fuentes externas son independientes de la aplicación. Si se admite el proyecto en SignPath, ese servicio tratará los artefactos públicos de compilación y los metadatos de procedencia necesarios para firmarlos; **no recibe credenciales de los proveedores ni datos de uso de la aplicación**. Actualmente no se afirma que exista esa admisión o una firma gratuita concedida.

Las capturas y recursos promocionales del repositorio utilizan datos ficticios. Para informar de un posible problema de privacidad, usa el canal privado descrito en [SECURITY.md](SECURITY.md), sin publicar tokens ni archivos de sesión.
