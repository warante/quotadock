# Estado de tareas

## 1. Investigar la hora de restablecimiento de las cuotas

- [x] Revisar los campos por proveedor; matriz y fuentes en `PROVIDERS.md`.
- [x] Distinguir timestamps, segundos restantes, fechas sin hora y agregados ambiguos, sin inventar un reinicio.
- [x] Implementar tooltip por cuota y menú de restablecimientos, sin texto permanente adicional.
- [x] Mostrar horas en `Europe/Madrid`, con CET/CEST y zona explícita.
- [x] Añadir pruebas de datos ausentes, unidades, fechas inválidas, ambos cambios de horario y reinicios pasados.

## 2. Preparar el proyecto para open source y solicitar firma gratuita

Objetivo: estudiar la admisión en **SignPath Foundation**, no generar un certificado autofirmado ni prometer aprobación.

- [x] Consultar condiciones vigentes y límites de admisión; resumen en `SIGNPATH.md`.
- [x] Auditar fuentes, historial completo y las tres releases previas; informe y límites en `docs/security-audit.md`.
- [x] Licencia MIT confirmada por el propietario, `LICENSE` y avisos de terceros añadidos.
- [x] Documentación de privacidad, contribución, seguridad, conducta y Code signing policy preparada.
- [x] Autorizar el permiso `workflow` y preparar el workflow activo de pruebas, compilación, auditoría e integración. Verificar su primera ejecución antes de dar CI por completada.
- [ ] Confirmar la primera ejecución completa de GitHub Actions y registrar el cambio de visibilidad tras la auditoría final.
- [x] Revisar mantenimiento, reputación y trazabilidad; no se presupone la admisión.
- [x] Preparar texto de solicitud, configuración de artefactos y plantilla de firma en dos etapas con aprobación manual; sin claves privadas en el repositorio.
- [ ] El propietario debe completar el formulario, MFA, cuenta/app de SignPath y la configuración real de políticas, aprobadores y entorno protegido.
- [ ] Actualizar las instrucciones de distribución una vez que exista una firma real y verificable. No prometer que desaparecerán todas las advertencias de Windows.

## 3. Integración en Piconera Tech

- [x] Revisar las fichas, filtros, estilos y traducciones de la web publicada.
- [x] Generar icono SVG/PNG, banners ES/EN, imagen social y textos, con datos ficticios.
- [x] Preparar ficha HTML, claves de traducción e instrucciones de copia sin reemplazar la web.
- [x] Probar el cambio ES → EN → ES en Chromium y validar recursos.
- [ ] Integrar y desplegar en el repositorio de Piconera Tech cuando su propietario lo decida. No se ha modificado esa web.

**Autorizado:** licencia MIT y apertura del repositorio tras una auditoría sin hallazgos pendientes. **Pendiente de confirmación específica:** envío de datos personales o aceptación de condiciones en SignPath y permisos/configuraciones externos que no se hayan aprobado expresamente.
