# Preparación para SignPath Foundation

Revisión de requisitos: **6 de noviembre de 2026, hora peninsular española**. [Condiciones oficiales](https://signpath.org/terms.html) · [Solicitud](https://signpath.org/apply) · [Integración con GitHub](https://docs.signpath.io/trusted-build-systems/github).

## Estado de la solicitud

**No enviada.** El repositorio y las releases son públicos, pero eso no constituye una admisión. La vía de la fundación permite firmar con un certificado emitido a nombre de **SignPath Foundation**; no entrega una clave privada ni un certificado personal al mantenedor.

## Preparación técnica publicada

La corrección [1.4.1](https://github.com/warante/quotadock/releases/tag/v1.4.1) ya está publicada: permite desactivar desde el instalador la consulta automática a GitHub antes del primer arranque, conserva el resto de ajustes y restaura el archivo previo si falla la instalación. En 1.4.0 esa opción solo estaba en el menú de la aplicación.

El [PR #3](https://github.com/warante/quotadock/pull/3) está fusionado. Las pruebas completas han pasado en GitHub Actions tanto en el PR como en [main](https://github.com/warante/quotadock/actions/runs/42285571398), incluidos conservación de ajustes, desactivación inicial y rollback. Los artefactos publicados proceden de ese build de CI; se comprobaron los hashes locales/remotos, el manifiesto y la codificación de las notas. El bloqueo local de los ejecutables de pruebas por Windows no se ha eludido ni se presenta como una ejecución local correcta.

Pendientes antes de presentarla:

- Confirmar MFA en GitHub en https://github.com/settings/security. No se ha podido verificar desde la API. Nunca facilitar códigos, contraseñas ni claves de recuperación en el repositorio o el formulario del proyecto.
- El propietario debe revisar y aceptar los términos, indicar sus datos de contacto directamente en el formulario y confirmar el envío. No se han rellenado ni enviado datos personales.
- Preparar señales verificables de mantenimiento y uso; no inventar usuarios, descargas, reviews ni reputación. SignPath puede rechazar o posponer la admisión de un proyecto reciente.

## Lo que proporciona este repositorio

- Licencia MIT aprobada por el propietario, sin dual licensing comercial.
- Código, scripts y recursos propios; bibliotecas de Windows/.NET como componentes del sistema, sin binarios externos incorporados.
- Aplicación ya publicada, instrucciones de instalación, funcionamiento y desinstalación.
- Políticas de privacidad, contribución, seguridad y [Code signing policy](docs/code-signing-policy.md), con roles identificados y atribución condicional correcta.
- Pruebas sin cuentas reales, auditoría de artefactos e historial y metadatos de producto y versión en ambos ejecutables.
- Configuración de referencia para un flujo de firma en dos etapas: aplicación y, después, instalador que la contiene.
- Repositorio ya público con MIT y reporte privado de vulnerabilidades habilitado tras la revisión de privacidad.

## Requisitos que no puede conceder el código

1. **Aceptación de SignPath.** Ser público y usar MIT no da derecho a un certificado. La fundación evalúa reputación, control del proyecto, mantenimiento y señales verificables de uso. Este proyecto es reciente y puede necesitar más trayectoria antes de ser admitido.
2. **MFA y roles reales.** El propietario debe confirmar MFA en GitHub y SignPath y configurar reviewers/approvers en el servicio.
3. **GitHub Actions y controles de publicación.** Se ha completado la autorización `workflow` para activar las comprobaciones de Windows. Los controles de firma adicionales y la verificación de procedencia deben configurarse con SignPath; tener CI no equivale a estar admitido.
4. **Cuenta, app y aprobación de SignPath.** El propietario debe instalar la GitHub App de SignPath solo para los repositorios necesarios, configurar el proyecto, las restricciones y los aprobadores y aceptar las condiciones del servicio.
5. **Solicitud con datos de contacto.** El formulario externo pide datos del solicitante. No se rellenan correos personales ni se aceptan términos en su nombre sin confirmación específica.

## Texto base para la solicitud (English)

**Project:** QuotaDock — https://github.com/warante/quotadock

**License:** MIT.

**Description:** A lightweight Windows system-tray utility that displays remaining AI subscription quotas using one icon, a provider selector and two progress bars. Quota queries are restricted to providers enabled by the user. The application can also check the public GitHub releases API on startup or on manual request; this sends the source IP and application version to GitHub, but no provider credentials or cookies. Downloads and replacement of an existing installation require explicit confirmation. It has no analytics or telemetry, reads official client sessions without modifying them and encrypts manually supplied credentials with Windows DPAPI.

**Privacy controls:** Published release 1.4.1 lets the user disable startup update checks in the installer before first launch, as well as in the application menu. The installer explains that GitHub receives the source IP and application version. Existing preferences are preserved, and the previous settings file is restored if installation fails. See the [privacy policy](PRIVACY.md).

**Distribution:** Per-user EXE installer and standalone portable EXE/ZIP. Installation and uninstallation are documented. Source, native UI previews, synthetic tests and release hashes are available in the repository. Existing releases are unsigned.

**Build and signing proposal:** GitHub-hosted Windows runners build reviewed tags and upload artifacts before signing. Sign the application first, embed that signed executable into the installer, sign the installer, verify signatures and metadata, then package and calculate final hashes. All signing requests require manual approval by the maintainer. No provider credentials are needed in CI or sent to SignPath.

**Maintainer / committer / reviewer / approver:** GitHub account `warante`. Contact details and MFA confirmation must be supplied privately by the owner, not fabricated or committed here.

**Reason for applying:** Windows Smart App Control currently blocks the unsigned executable because its publisher cannot be verified. We are seeking a verifiable build-and-signing process for our own open-source binaries, not a way to bypass Windows protections. We understand that signing does not guarantee acceptance by every Windows policy.

**Project maturity:** Recently released. Do not claim established adoption, independent reviews or an existing trust reputation that has not been demonstrated.

## Antes de anunciar una firma gratuita

- Completar la admisión y verificar una firma real de los artefactos.
- Añadir la atribución obligatoria sin la condición «pendiente» solo cuando corresponda.
- Mantener el enlace **Code signing policy** en la portada y en las notas de descarga/release.
- No prometer que SmartScreen dejará de advertir ni que una política empresarial aceptará automáticamente cualquier binario firmado.
