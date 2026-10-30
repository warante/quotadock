# Privacy policy / Política de privacidad

QuotaDock no tiene servidor propio, telemetría, publicidad ni analítica. No envía estadísticas al mantenedor, a Piconera Tech ni a SignPath. Las consultas de cuotas y de nuevas versiones son conexiones de red y no deben describirse como un funcionamiento completamente offline.

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
- Desde 1.4.0, `settings.json` también guarda si se comprueban versiones al iniciar. Los instaladores descargados con permiso se guardan en `%LOCALAPPDATA%\QuotaDock\Updates`, en carpetas únicas. No contienen tus credenciales. Las descargas fallidas se eliminan; los instaladores válidos se conservan y puedes borrarlos cuando no estén en ejecución. La desinstalación no borra recursivamente estas carpetas de descargas.

## Control y eliminación

Desactiva un proveedor en el menú para detener sus consultas. **Olvidar clave** elimina la credencial que hayas guardado en QuotaDock; no elimina variables de entorno ni cierra sesiones de otros programas. Desactivar todos los proveedores deja únicamente el icono de control, sin consultas de cuota.

El instalador indica dónde se instala, ofrece inicio automático opcional y una opción para abrir la aplicación al terminar. La desinstalación no borra tus sesiones oficiales y conserva ajustes y claves cifradas. Usa **Olvidar clave** antes de desinstalar si quieres retirar estas últimas; para eliminar los ajustes restantes, cierra QuotaDock y elimina únicamente su carpeta `%APPDATA%\QuotaDock` tras comprobar que no necesitas conservarla.

## Web, descargas y firma

GitHub aloja el repositorio y las descargas y aplica su política de privacidad. La web de Piconera Tech y sus fuentes externas son independientes de la aplicación. Si se admite el proyecto en SignPath, ese servicio tratará los artefactos públicos de compilación y los metadatos de procedencia necesarios para firmarlos; **no recibe credenciales de los proveedores ni datos de uso de la aplicación**. Actualmente no se afirma que exista esa admisión o una firma gratuita concedida.

Desde 1.4.0, **Buscar nuevas versiones al iniciar** está activado por defecto: consulta una vez por arranque `https://api.github.com/repos/warante/quotadock/releases/latest`. Puedes desactivarlo en el menú; **Buscar actualizaciones…** sigue disponible manualmente. Desactivar todos los proveedores no desactiva esta comprobación independiente. GitHub recibe tu IP y un User-Agent con la versión de QuotaDock, pero no se envían tokens de proveedores, cookies, identificadores de cuenta ni credenciales de GitHub. Los fallos de comprobación automática no muestran mensajes ni provocan reintentos durante ese arranque.

Detectar una release nueva solo genera un aviso. Descargar y abrir su instalador requiere siempre confirmación. La descarga parte de `github.com/warante/quotadock/releases/download/…`, con redirecciones restringidas a HTTPS en `github.com` o `release-assets.githubusercontent.com`; usa un cliente de red separado, sin autorización ni cookies. Se comprueba el hash SHA-256 anunciado por GitHub y la versión/producto, lo que no equivale a verificar una firma de editor.

Actualizar una instalación existente requiere además confirmar su sustitución en el instalador. Solo entonces se solicita el cierre ordenado de la instancia que coincide con la ruta instalada de tu usuario. No se terminan procesos a la fuerza, no se cierran copias portables y no se pide elevación. Una versión antigua que no admita este cierre debe cerrarse manualmente.

Las capturas y recursos promocionales del repositorio utilizan datos ficticios. Para informar de un posible problema de privacidad, usa el canal privado descrito en [SECURITY.md](SECURITY.md), sin publicar tokens ni archivos de sesión.
