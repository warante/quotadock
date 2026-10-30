# Kit de integración · Piconera Tech

Recursos preparados para integrar QuotaDock en https://martamacfly.github.io/piconeratech/. **No se ha modificado ni desplegado esa web.**

## Formato comprobado en la web

La portada publica fichas HTML en `index.html`, dentro de `.projects`: un enlace `.card.project`, categoría `data-group`, banner `.project-banner`, icono `.project-logo`, descripción y etiquetas. Los filtros usan `herramientas`, `apps` y `ocio`. Los textos se traducen mediante los objetos `copy.es` y `copy.en` de `js/main.js`; los banners traducibles usan `data-i18n-src`. La fuente global es Outfit y la identidad utiliza papel/crema y cobre.

QuotaDock utiliza **herramientas**, etiquetas **Windows / GitHub / MIT**, un banner oscuro de 16:9 y acentos de cobre compatibles con la web. Se recomienda `banner-full` para mostrar el recurso sin recortarlo. No hace falta modificar el CSS actual.

## Integrar en cuatro pasos

1. Copia el contenido de `assets/` de este kit dentro del `assets/` de `piconeratech`, manteniendo las rutas.
2. Copia el contenido de `project-card.html` dentro de `<div class="projects">` en `index.html`, junto a las otras fichas.
3. Añade las claves de `i18n.json` a los objetos **existentes** `copy.es` y `copy.en` en `js/main.js`. No reemplaces esos objetos ni el archivo completo: perderías las traducciones de los otros proyectos.
4. Comprueba los filtros, el cambio ES/EN, el banner completo y los enlaces a código/descargas. Si la web cachea `main.js`, actualiza su parámetro de versión como hace el sitio.

La descripción y los atributos ARIA deben cambiar al cambiar el idioma. La bandera inglesa debe usar `quotadock-en.png`, no el banner español. `data-group="herramientas"` no se traduce: es un identificador funcional del filtro.

## Archivos

| Recurso | Uso |
| --- | --- |
| `assets/apps/quotadock.svg` | Icono vectorial, 512 × 512, sin fuentes ni recursos externos. |
| `assets/apps/quotadock.png` | Alternativa raster, 512 × 512. |
| `assets/apps/banners/quotadock.png` | Banner español, 1600 × 900. |
| `assets/apps/banners/quotadock-en.png` | Banner inglés, 1600 × 900. |
| `social/quotadock-og.png` | Imagen social, 1200 × 630. |
| `project-card.html` | Ficha para pegar en el porfolio, con las clases reales de la web. |
| `i18n.json` | Claves ES/EN para fusionar en `copy`. |
| `COPY.md` | Descripciones breves/ampliadas, CTAs y notas de precisión. |
| `preview.html` | Vista previa local autocontenida de la ficha; no depende de la web publicada. |
| `render.ps1` | Generación de los PNG, con gráficos propios y la vista previa de QuotaDock. |

Abre `preview.html` para revisar la ficha antes de integrarla. La vista local usa fuentes del sistema; al integrarse hereda Outfit y el resto del diseño de Piconera Tech.

### Regenerar las imágenes tras un cambio de interfaz

Los banners ES/EN y la imagen social incluyen el mismo panel nativo de demostración que el README. La vista actual muestra reinicios compactos junto al porcentaje y el icono `ⓘ`; no utiliza respuestas de cuentas reales. Regenera primero las capturas y después los banners, sin cambiar los textos traducidos existentes:

```powershell
.\test.ps1
Copy-Item .\dist\tests\provider-selector.png .\docs\images\provider-selector.png -Force
Copy-Item .\dist\tests\quotadock-preview.png .\docs\images\quotadock-preview.png -Force
.\integrations\piconera-tech\render.ps1
.\integrations\piconera-tech\test.ps1
.\integrations\piconera-tech\package.ps1
```

Ejecuta estos comandos desde la raíz del repositorio y continúa solo si las pruebas visuales han terminado correctamente. Si Windows las bloquea, no sustituyas las capturas por archivos antiguos ni desactives las protecciones.

`test.ps1` valida las rutas, dimensiones y claves de traducción. `package.ps1` genera un ZIP autocontenido en `dist/integrations`. `browser-test.yml` recoge la prueba ES → EN → ES para Maestro con la variable `PREVIEW_URL`, apuntando a un servidor local de esta carpeta. El flujo se ha probado en Chromium; no debe confundirse con una prueba de la web publicada, que no se ha modificado.

## Honestidad visual y privacidad

Los porcentajes, proveedores y hora mostrados en las imágenes son de demostración. El kit no incluye capturas de una cuenta real, cookies, claves, ni los logotipos o banners de otros proyectos. Se utiliza la vista nativa generada por las pruebas visuales de QuotaDock. El icono y el material gráfico generado aquí están cubiertos por la licencia MIT del proyecto.

No anuncies firma, aprobación de SignPath, disponibilidad para macOS o ausencia de avisos de Windows: actualmente no se ofrecen esas garantías.
