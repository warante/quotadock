# Preparación para SignPath Foundation

Revisión de requisitos: **22 de octubre de 2026, hora peninsular española**. [Condiciones oficiales](https://signpath.org/terms.html) · [Solicitud](https://signpath.org/apply) · [Integración con GitHub](https://docs.signpath.io/trusted-build-systems/github).

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

**Description:** A lightweight Windows system-tray utility that displays remaining AI subscription quotas using one icon, a provider selector and two progress bars. It queries only providers enabled by the user. It has no analytics or telemetry, uses fixed HTTPS destinations, reads official client sessions without modifying them and encrypts manually supplied credentials with Windows DPAPI.

**Distribution:** Per-user EXE installer and standalone portable EXE/ZIP. Installation and uninstallation are documented. Source, native UI previews, synthetic tests and release hashes are available in the repository. Existing releases are unsigned.

**Build and signing proposal:** GitHub-hosted Windows runners build reviewed tags and upload artifacts before signing. Sign the application first, embed that signed executable into the installer, sign the installer, verify signatures and metadata, then package and calculate final hashes. All signing requests require manual approval by the maintainer. No provider credentials are needed in CI or sent to SignPath.

**Maintainer / committer / reviewer / approver:** GitHub account `warante`. Contact details and MFA confirmation must be supplied privately by the owner, not fabricated or committed here.

**Project maturity:** Recently released. Do not claim established adoption, independent reviews or an existing trust reputation that has not been demonstrated.

## Antes de anunciar una firma gratuita

- Completar la admisión y verificar una firma real de los artefactos.
- Añadir la atribución obligatoria sin la condición «pendiente» solo cuando corresponda.
- Mantener el enlace **Code signing policy** en la portada y en las notas de descarga/release.
- No prometer que SmartScreen dejará de advertir ni que una política empresarial aceptará automáticamente cualquier binario firmado.
