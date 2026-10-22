# Revisión previa a la apertura del repositorio

Fecha: **22 de octubre de 2026, hora peninsular española**.

## Alcance y resultado

- Se inspeccionaron **91 objetos Git** de todas las referencias y reflogs locales existentes al iniciar la revisión, incluyendo los commits de las versiones 1.0.0, 1.1.0 y 1.2.0. Se revisaron también identidades de commit: usan el alias público del mantenedor y correo `users.noreply.github.com`, no un correo personal.
- Se descargaron las **tres releases publicadas**, se cotejaron sus hashes con los digests de GitHub y se revisaron **16 archivos**, incluidas las notas guardadas para análisis y **14 entradas ZIP**. No se alteraron los ejecutables publicados.
- `privacy-check.ps1` inspecciona las fuentes actuales y los artefactos nuevos; `audit-history.ps1` permite repetir la inspección de todas las referencias, reflogs y un directorio de releases. No imprimen los valores coincidentes.
- La búsqueda de patrones conocidos de tokens, JWT, claves privadas, correos personales y rutas de usuario **no detectó hallazgos pendientes**. Se revisaron además la estructura de distribución, los datos ficticios de las imágenes, los destinos HTTPS, el bloqueo de redirecciones y la ausencia de paquetes externos incrustados.
- El repositorio no tenía issues que revisar en la consulta realizada. Los enlaces a los perfiles públicos del mantenedor y a repositorios de referencia son información de procedencia, no credenciales.

Los cambios nuevos se auditan otra vez antes de subirlos y antes del cambio de visibilidad. El número de objetos aumenta con los commits; la cifra anterior identifica la revisión inicial, no un límite del script.

## Apertura efectuada

Tras añadir el código y la documentación de preparación en el commit `7a0bb16`, se repitieron las comprobaciones: **157 objetos Git** y las fuentes y artefactos de desarrollo, sin hallazgos pendientes. Después se cambió el repositorio a **público**, GitHub reconoció la licencia **MIT** y se habilitó el reporte privado de vulnerabilidades. La autorización `workflow` se completó y el primer workflow se inició; su resultado se registra en `TODO.md`.

## Decisiones del propietario

El propietario confirmó la licencia **MIT** y autorizó cambiar el repositorio de privado a público **solo después de la auditoría**. No autorizó inventar datos de contacto, declarar una aprobación de SignPath ni aceptar términos externos en su nombre.

## Límites de esta revisión

Es una revisión de preparación y privacidad, **no una auditoría de seguridad independiente ni una garantía de ausencia de vulnerabilidades**. El detector de patrones puede tener falsos negativos y falsos positivos. Las claves de alta entropía sin prefijo, servicios externos y configuraciones privadas requieren sus propias comprobaciones.

No se han eliminado ni reescrito commits publicados ni cambiado claves de proveedores. La admisión de SignPath, MFA, permisos de workflows y protecciones del servicio de firma deben comprobarse por separado. No se ha afirmado que los binarios existentes estén firmados.
