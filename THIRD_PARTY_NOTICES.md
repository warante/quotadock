# Componentes y referencias de terceros

## Distribución

El proyecto se publica bajo [MIT](LICENSE). La compilación actual no incluye paquetes NuGet, JavaScript, un navegador, fuentes descargadas ni DLLs de otros proyectos. Utiliza las bibliotecas de sistema de Windows y .NET Framework (`System.Windows.Forms`, `System.Drawing`, HTTP, serialización y criptografía), suministradas por Microsoft; la licencia MIT de QuotaDock no cambia sus condiciones.

Segoe UI y sus variantes se solicitan al sistema operativo. No se distribuyen archivos de esas fuentes. Las ilustraciones y el icono de QuotaDock se generan con código del proyecto, usando datos ficticios. La política de firma debe revisarse si se añaden recursos o dependencias de terceros.

El proceso de CI hace referencia a GitHub Actions de `actions/checkout`, `actions/upload-artifact` y, en la plantilla opcional, `SignPath/github-action-submit-signing-request`, fijadas a commits del proveedor. Esas herramientas se ejecutan en el servicio de compilación; su código no se incorpora al ejecutable ni al ZIP de QuotaDock. Se aplican sus licencias y políticas en sus respectivos repositorios y servicios.

## Referencias de protocolo

[PROVIDERS.md](PROVIDERS.md) cita documentación oficial y herramientas comunitarias utilizadas para comprender los formatos de cuota y sus particularidades. Los campos de un protocolo no implican que QuotaDock distribuya el código de dichas herramientas. La implementación actual se ha escrito para este proyecto; no se han incorporado sus archivos fuente o capturas privadas.

## Marcas y recursos de integración

OpenAI, Claude, OpenCode, Kimi, MiniMax, Z.AI, GitHub, Google, Windows y otras marcas pertenecen a sus titulares. QuotaDock no está afiliado ni certificado por esos proveedores. No se distribuyen sus logotipos como parte del ejecutable.

Los recursos preparados para Piconera Tech pertenecen a la documentación de integración de QuotaDock. El paquete no incorpora imágenes de otros proyectos de su porfolio ni copia su web completa. Los recursos gráficos creados aquí se distribuyen bajo MIT; no se conceden derechos sobre marcas ajenas por el mero hecho de citarlas.
