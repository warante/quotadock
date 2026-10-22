# QuotaDock

**Tus límites de IA, de un vistazo.**

Un monitor mínimo para Windows: dos barras junto al reloj que muestran cuánto queda de tu suscripción, sin abrir un panel ni interrumpir tu trabajo.

**v1.0.0 · Windows 10/11 · Portable o instalable · Sin administrador**

## Proveedores

| Proveedor | Aplicación | Límites mostrados | Sesión necesaria |
| --- | --- | --- | --- |
| OpenAI | `QuotaDock.OpenAI.exe` | Codex: 5 horas y semana | Codex con tu cuenta de ChatGPT |
| Anthropic | `QuotaDock.Claude.exe` | Claude: 5 horas y semana global | Claude Code con tu suscripción de Claude |

QuotaDock está enfocado en los proveedores de IA más populares. **Esta versión solo soporta los dos de la tabla**: no anuncia integración con Gemini, Copilot u otros sin una implementación y verificación previas.

## Descargar e instalar

En la sección **Releases → v1.0.0** del repositorio encontrarás:

| Archivo | Uso |
| --- | --- |
| `QuotaDock-1.0.0-Setup.exe` | Instalador por usuario: selecciona proveedores e inicio automático opcional. |
| `QuotaDock-1.0.0-Windows-Portable.zip` | Descomprime y abre el ejecutable del proveedor que quieras. |
| `QuotaDock.OpenAI.exe` / `QuotaDock.Claude.exe` | Ejecutables portables individuales, sin archivos adicionales. |
| `SHA256SUMS.txt` | Huellas SHA-256 de los cuatro artefactos. |

El repositorio y sus descargas son privados: debes iniciar sesión en GitHub con una cuenta autorizada.

El instalador usa `%LOCALAPPDATA%\QuotaDock`, crea accesos en Inicio y permite desinstalar desde **Configuración → Aplicaciones** o **Inicio → QuotaDock → Desinstalar**. No instala servicios, no necesita elevación y no descarga dependencias. Abre la aplicación desde Inicio tras instalarla.

**Los ejecutables no están firmados.** SmartScreen o las políticas de tu organización pueden advertir o bloquear su ejecución. En equipos administrados, solicita aprobación al administrador; no se debe desactivar la protección para ejecutarlos.

## Conectar tu cuenta

### OpenAI / Codex

Inicia sesión en Codex con tu cuenta de ChatGPT:

```text
codex login
```

Se lee `%CODEX_HOME%\auth.json`, si defines esa variable; en caso contrario, `%USERPROFILE%\.codex\auth.json`, con alternativa en `.config\codex\auth.json`.

Muestra **los límites de Codex**, no el saldo de la API ni el uso general de todos los modelos de ChatGPT.

### Claude

Abre Claude Code con `claude` y ejecuta `/login` con tu suscripción de Claude. Se lee `%CLAUDE_CONFIG_DIR%\.credentials.json`, si defines esa variable; en caso contrario, `%USERPROFILE%\.claude\.credentials.json`.

La barra semanal es el límite global, no las cuotas específicas de Opus/Sonnet ni el crédito de uso extra. Iniciar sesión solo en la web de Claude no crea las credenciales locales necesarias.

### Condiciones comunes

- Se necesita que el cliente oficial esté instalado y haya guardado la sesión **en archivo**; no se consulta el Administrador de credenciales de Windows.
- Las claves API no sirven para consultar estos porcentajes de suscripción.
- Si la sesión caduca, vuelve a iniciar sesión en el cliente correspondiente. QuotaDock relee las credenciales en cada consulta, sin renovarlas ni modificarlas.

## Dos barras. Nada más.

- **Arriba:** porcentaje restante de 5 horas.
- **Abajo:** porcentaje restante semanal.
- **Un clic:** las mismas dos líneas, con etiquetas y porcentajes.
- **Al pasar el ratón:** los dos porcentajes restantes.
- **Clic derecho:** estado de conexión, actualizar, ayuda y salir.

Verde: más de 25%; amarillo: hasta 25%; rojo: hasta 10%. Gris y `—%`: dato no disponible. Una cuenta sin alguno de los límites no muestra un porcentaje inventado.

Las consultas se hacen cada **60 segundos**, no en streaming. Los errores borran los valores visibles para no presentar información antigua como actual y espacian los reintentos hasta 10 minutos. Los proveedores pueden limitar las consultas o retrasar sus datos.

Si Windows oculta el icono en **^**, arrástralo junto al reloj. Puedes abrir ambos proveedores a la vez; cada uno tiene su propio icono. No se permite duplicar instancias de un mismo proveedor en la sesión de Windows.

## Inicio automático y versiones anteriores

En el instalador puedes activar el inicio automático de los proveedores seleccionados. Para una versión portable, pulsa `Win + R`, escribe `shell:startup` y coloca allí un acceso directo al ejecutable. No lo muevas después sin actualizar ese acceso.

Los antiguos `CodexUsageTray.exe` y `ClaudeUsageTray.exe` y sus accesos de inicio **no se modifican**. QuotaDock comparte su bloqueo de instancia para evitar iconos duplicados. Si migras a la instalación nueva, sal de la versión anterior y retira manualmente su acceso de inicio para que no arranque la copia antigua primero.

## Privacidad y limitaciones

- No hay telemetría, servidor propio, historial ni copia de tokens en disco.
- Las credenciales se leen localmente y solo se envían al proveedor correspondiente mediante HTTPS.
- OpenAI: `https://chatgpt.com/backend-api/wham/usage`.
- Anthropic: `https://api.anthropic.com/api/oauth/usage`.
- Son **endpoints internos, no APIs públicas estables**: pueden cambiar o dejar de funcionar. La disponibilidad depende de tu cuenta, sus permisos y la política del proveedor.
- QuotaDock no está afiliado a OpenAI ni a Anthropic. Las marcas pertenecen a sus propietarios.

## Compilar y probar

Requisitos: Windows y .NET Framework 4.x, normalmente incluido en Windows 10/11. No necesita Python, Node, SDK de .NET moderno ni paquetes NuGet.

```powershell
# Compilar ambos proveedores en dist\v1.0.0 (sin tocar los ejecutables antiguos)
.\build.ps1

# Compilar solo uno
.\build.ps1 -Provider Claude
.\build.ps1 -Provider Codex

# Pruebas unitarias: no leen credenciales ni consultan proveedores
.\test.ps1

# Opcional: pruebas reales de ambos proveedores con tus sesiones locales
.\test.ps1 -Live

# Generar ejecutables, instalador, ZIP y SHA-256
.\release.ps1
```

Si PowerShell bloquea scripts por su política de ejecución, utiliza los mecanismos aprobados de tu equipo. La política de ejecución de scripts no es lo mismo que el control de aplicaciones: una compilación correcta no garantiza que un equipo permita ejecutar binarios sin firmar.

### Estructura

- `UsageTray.cs`: interfaz común, lectura de credenciales y porcentajes.
- `Provider.cs`: selección de proveedor mediante compilación condicional.
- `Tests.cs` / `test.ps1`: pruebas de formatos, límites ausentes y valores inválidos.
- `Installer.cs` / `Uninstall.ps1`: instalación y desinstalación por usuario.
- `AssemblyInfo.cs`: metadatos de versión 1.0.0.
- `ci/windows.yml`: plantilla de GitHub Actions para pruebas y artefactos de Windows, sin secretos de cuentas. No está activada; para usarla, cópiala a `.github/workflows/windows.yml` y súbela con autorización `workflow` de GitHub.
- `privacy-check.ps1`: comprobación preventiva de secretos, correos y rutas personales en fuentes y artefactos.

Para añadir un proveedor se necesita una fuente real de cuotas, autenticación local segura y pruebas con datos sintéticos. No debe deducirse el saldo a partir de tokens contados localmente ni enviarse la sesión a terceros.

## Historial

Consulta [CHANGELOG.md](CHANGELOG.md).
