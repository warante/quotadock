# Code signing policy

## Estado actual

**Solicitud preparada, admisión pendiente.** Las releases existentes siguen sin firma digital. No se afirma que SignPath haya aceptado QuotaDock, emitido un certificado para el proyecto o firmado sus binarios.

Si SignPath Foundation admite el proyecto y se activa el servicio, la atribución requerida será:

> Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

Hasta entonces, esa frase describe la atribución prevista y **no un servicio ya concedido**.

## Roles y responsabilidades

- **Committer y reviewer:** [@warante](https://github.com/warante), mantenedor del repositorio.
- **Approver de releases y solicitudes de firma:** [@warante](https://github.com/warante).
- Las contribuciones externas deben pasar revisión del mantenedor; no se ejecutará firma desde pull requests de forks.
- Toda release firmada requiere aprobación manual de una persona autorizada. Una firma no puede activarse simplemente por cambiar un archivo o una variable de un pull request.
- Los miembros con permisos de repositorio o firma deben usar MFA. Su verificación es un requisito pendiente del propietario y del servicio; este documento no certifica la configuración privada de sus cuentas.

## Procedencia de los artefactos

Solo se solicitará firma para binarios propios compilados desde un tag revisado en el repositorio público, en runners hospedados por GitHub. Los artefactos sin firmar se subirán a GitHub Actions antes de enviarlos a SignPath, para que el servicio pueda comprobar su procedencia.

Se comprobarán producto **QuotaDock**, versiones de producto/archivo y nombres de los ejecutables. La aplicación debe firmarse **antes de incrustarla en el instalador**; después se firma el instalador. No basta con firmar únicamente la envoltura y dejar el ejecutable instalado sin firmar. Los hashes se calculan al final, sin volver a compilar los binarios ya firmados.

Las claves privadas permanecen en el servicio de firma; nunca se añaden al repositorio, a fixtures o a artefactos públicos. El token de envío, cuando exista, irá en un secret de un entorno de GitHub protegido y con privilegios mínimos. Los identificadores de organización y proyecto no equivalen a una admisión ni sustituyen sus controles.

## Privacidad y cambios de sistema

Desde 1.4.0 se puede consultar GitHub al iniciar (desactivable) o manualmente para detectar releases estables. Cada descarga/apertura del instalador y cada sustitución de una instalación existente requiere confirmación. El hash comunicado por GitHub y los metadatos del archivo no se presentan como una firma Authenticode ni sustituyen la admisión en SignPath. No se desactivan controles de Windows ni se solicita elevación para instalar por usuario. Consulta [las instrucciones de actualización](../README.md#buscar-e-instalar-nuevas-versiones-140).

Consulta [Privacy policy](../PRIVACY.md): las consultas se realizan únicamente a los proveedores que el usuario activa. No hay telemetría ni envío de datos de uso al mantenedor o al servicio de firma. La instalación es por usuario, anuncia el inicio automático opcional y ofrece desinstalación. Las sesiones de otros clientes no se modifican.

## Solicitud y revisión

Los requisitos vigentes, el estado de preparación y los pasos que requieren intervención del propietario se documentan en [SIGNPATH.md](../SIGNPATH.md). Los cambios sensibles de compilación, workflows y publicación deben revisarse antes de cada solicitud. No se firman binarios de terceros como si fueran propios ni se intenta eludir las restricciones de SignPath.
