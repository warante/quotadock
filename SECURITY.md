# Seguridad

## Versiones

Las correcciones se aplican a la rama `main` y a la versión estable más reciente. Las versiones antiguas pueden usar endpoints internos que hayan cambiado. No se ofrece un SLA ni se garantiza compatibilidad de APIs privadas de terceros.

## Informar de una vulnerabilidad

No publiques tokens, archivos de sesión, claves, correos o capturas privadas en un issue o pull request.

Usa **Security → Advisories → Report a vulnerability** del repositorio si el reporte privado está habilitado. Si esa opción todavía no aparece, contacta al mantenedor mediante su perfil público antes de enviar detalles sensibles. No se indica un correo personal en este documento.

Describe la versión, Windows utilizado, impacto y pasos de reproducción con datos ficticios. Podemos pedir más información sin necesidad de copiar tus credenciales. No ejecutes pruebas destructivas contra proveedores o cuentas ajenas.

## Alcance y límites

Son prioritarios: envío de credenciales a destinos incorrectos, exposición de datos privados, manipulación del proceso de publicación, cambios no anunciados en el sistema y problemas de instalación/desinstalación.

DPAPI protege las claves guardadas para tu usuario; no protege contra otro programa que ya controle esa misma cuenta. Los endpoints de cuota pueden fallar, restringir acceso o cambiar. La ausencia de firma no se debe ocultar ni compensar desactivando protecciones de Windows.

Desde 1.4.0, el actualizador consulta exclusivamente la release estable del repositorio oficial y requiere confirmación antes de descargar/abrir el instalador. Una instalación existente requiere una segunda confirmación antes de sustituirse. El tamaño, SHA-256 comunicado por GitHub y metadatos de producto/versión detectan inconsistencias, pero **no equivalen a una firma de editor**: dependen de la integridad de GitHub y del repositorio. El cliente no envía credenciales ni cookies y restringe las redirecciones de descarga a hosts HTTPS de GitHub. El cierre cooperativo solo se solicita para la ruta instalada del usuario; no se terminan procesos a la fuerza ni se solicita elevación. Los cambios en estos controles y cualquier instalación sin consentimiento son prioritarios para revisión de seguridad.

Consulta [PRIVACY.md](PRIVACY.md) y [Code signing policy](docs/code-signing-policy.md).
