# Contribuir a QuotaDock

Gracias por ayudar a mantener una herramienta pequeña, segura y sin ruido.

## Preparación

Windows 10/11, Git y .NET Framework 4.x son suficientes para compilar. No se necesitan credenciales de proveedores, Python, Node ni paquetes NuGet para las pruebas.

```powershell
.\test.ps1
.\release.ps1
.\package-test.ps1
.\privacy-check.ps1
.\audit-history.ps1
```

Trabaja en una rama y abre un pull request pequeño, explicando el comportamiento anterior, el nuevo y cómo lo has probado. Los colaboradores externos no pueden aprobar su propio código para una publicación firmada. Las políticas de Windows pueden bloquear binarios sin firmar: no desactives el control de aplicaciones para ejecutar las pruebas.

Utiliza el correo `noreply` de GitHub en los commits si no quieres publicar tu dirección personal. La auditoría previa a publicación rechaza direcciones personales en el historial: acuerda primero con el mantenedor cualquier cambio de esta política.

## Reglas de seguridad y diseño

- Conserva un único icono, dos barras y el selector; no añadas procesos web o temporizadores de animación para cambios visuales menores.
- Nunca incluyas credenciales, correos, identificadores de cuenta, rutas de usuario ni capturas reales sin anonimizar, ni siquiera en fixtures o mensajes de error.
- Usa exclusivamente respuestas sintéticas. Las pruebas de CI no deben leer sesiones reales ni consultar proveedores.
- Para un proveedor nuevo, documenta la fuente del contrato, región, autenticación y límites reales. No inventes porcentajes a partir de un saldo o una estimación local de tokens.
- Un timestamp sin hora o sin zona no autoriza a inventarlas. Las horas mostradas deben usar `Europe/Madrid`, incluidos los cambios CET/CEST.
- No extraigas cookies de navegadores ni renueves por tu cuenta tokens rotatorios que pertenecen a otro cliente.
- Revisa especialmente los workflows, scripts de compilación, el instalador, la desinstalación y cualquier cambio de destinos de red.

## Licencia y procedencia

Las aportaciones se aceptan bajo [MIT](LICENSE). Confirma que puedes aportar el código y los recursos. No pegues implementaciones de otros proyectos sin permiso y avisos compatibles. Cita las referencias de protocolo, pero no copies respuestas privadas del proveedor. Consulta [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

El proceso de firma tiene sus propios controles y aprobaciones manuales, descritos en [Code signing policy](docs/code-signing-policy.md). Una prueba correcta no otorga permiso para publicar o firmar una release.
