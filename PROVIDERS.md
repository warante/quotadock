# Proveedores y fuentes de cuotas

Investigación realizada el **22 de octubre de 2026 (hora peninsular española)**. Los límites y contratos de los proveedores pueden cambiar.

## Conectores implementados en 1.1.0

Todos incluyen pruebas con respuestas **sintéticas**, no capturas de cuentas personales. Eso comprueba el lector, no garantiza que una cuenta concreta tenga acceso. No se han verificado en vivo los nuevos proveedores sin una suscripción de prueba disponible.

| Proveedor | Autenticación | Endpoint fijo | Qué se muestra |
| --- | --- | --- | --- |
| OpenAI / Codex | Sesión local de Codex | `https://chatgpt.com/backend-api/wham/usage` | 5 h / semana, identificados por duración |
| Claude | Sesión local de Claude Code | `https://api.anthropic.com/api/oauth/usage` | 5 h / semana global |
| OpenCode Go / Go Plus | Clave de Go/Go Plus | `https://opencode.ai/zen/go/v1/usage` | 5 h / semana; mes seleccionable |
| Kimi Code global | Clave de Kimi Code global | `https://api.kimi.ai/coding/v1/usages` | 5 h / semana; mes cuando el proveedor sustituye la cuota semanal |
| Kimi Code China | Clave de Kimi Code China | `https://api.kimi.com/coding/v1/usages` | Igual, sin intercambiar credenciales entre regiones |
| MiniMax Token / M Plan global | Subscription Key | `https://www.minimax.io/v1/token_plan/remains` | Intervalo / semana del bucket `general`; no vídeo |
| MiniMax China | Clave de suscripción china | `https://api.minimaxi.com/v1/token_plan/remains` | Mismo lector; sujeto al despliegue regional |
| Z.AI / GLM Coding | Clave del plan internacional | `https://api.z.ai/api/monitor/usage/quota/limit` | 5 h / semana o mes según identificadores del proveedor |
| Zhipu / BigModel | Clave del Coding Plan chino | `https://open.bigmodel.cn/api/monitor/usage/quota/limit` | Mismo lector; no confunde los límites de herramientas MCP con cuota de modelos |
| GitHub Copilot | Token OAuth de Copilot; alternativamente GitHub CLI oficial | `https://api.github.com/copilot_internal/user` | Premium/mes y Chat/mes; ilimitado se muestra sin porcentaje finito |
| Google Gemini CLI | OAuth local de Gemini CLI | `https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist` y `https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuota` | Pro / Flash, porcentaje restante por familia; no toda la app Gemini |

Los endpoints internos son **experimentales**: pueden cambiar, exigir permisos o ser bloqueados. OpenCode Go tiene documentación oficial del plan, pero su endpoint de uso no está documentado en la referencia pública consultada. La implementación de su formato y la de otros endpoints internos se ha contrastado con herramientas comunitarias; no se presenta como una API pública estable.

### Fuentes

- [OpenCode V2: Go y Go Plus](https://opencode.ai/v2/docs/console/go/): límites de 5 horas, semana y mes.
- [OpenCode V2: API de Console](https://opencode.ai/v2/docs/console/api/): claves y permisos. Una clave de inferencia puede no tener permiso de uso; comprueba **Usage: Read** cuando el plan lo permita.
- [OpenCode V2: almacenamiento de proveedores](https://opencode.ai/v2/docs/cli/providers/): SQLite en V2. QuotaDock no abre esa base de datos ni usa como actual un `auth.json` legado.
- [Kimi Code: endpoints y claves por región](https://www.kimi.com/code/docs/en/kimi-code/faq.html).
- [Kimi: beneficios y cupo mensual](https://www.kimi.com/en/help/kimi-code/benefits).
- [MiniMax: consulta oficial de cuota](https://platform.minimax.io/docs/m-plan/faq): `/v1/token_plan/remains`; Subscription Key no es la clave de pago por uso.
- [Gemini CLI: contrato del cliente oficial](https://github.com/google-gemini/gemini-cli/blob/main/packages/core/src/code_assist/types.ts): `loadCodeAssist`, `retrieveUserQuota`, `remainingFraction` y `modelId`.
- [Referencia comunitaria de endpoints](https://github.com/akitaonrails/ai-usagebar/blob/main/docs/vendor-endpoints.md): OpenCode Go, Copilot, Kimi, Z.AI y diferencias entre cuotas y saldos.
- [Kimi: diferencias de formatos](https://github.com/akitaonrails/ai-usagebar/blob/main/src/kimi/types.rs): `usage` semanal frente a `usages.limit_month_total` mensual; no sumar subcuotas ni inferir la semana.
- [Z.AI: discriminadores de cuota](https://github.com/akitaonrails/ai-usagebar/blob/main/src/zai/types.rs): `TOKENS_LIMIT`/`CREDIT_LIMIT` y códigos `unit`.

Se han utilizado como referencias de protocolo, sin incorporar su código ni credenciales a QuotaDock.

## Evaluados y no habilitados

| Proveedor | Motivo |
| --- | --- |
| DeepSeek directo / Moonshot API | Los endpoints de saldo monetario no equivalen al porcentaje restante de una suscripción. No se inventa un máximo para dibujar una barra. Puedes consultar las cuotas de los modelos incluidos en Go desde el conector Go. |
| OpenRouter | Predomina el saldo de crédito y los límites configurables por clave; no es una cuota universal de suscripción comparable a 5 h / semana. |
| Alibaba Cloud / Qwen Coding Plan | Hay integraciones comunitarias con el gateway de consola y su sesión. Falta verificar una vía Windows estable que no requiera extraer cookies del navegador. |
| Xiaomi MiMo directo | Las herramientas consultadas señalan que la cuota requiere una sesión web SSO, no la clave de inferencia. No se extraen cookies. |
| Cursor | Endpoint interno y sesiones de aplicación con protecciones específicas; pendiente de una integración segura y verificable en Windows. |
| Grok / SuperGrok | Distintos pools y protocolos según producto; no se equipara el saldo API a la cuota de la suscripción. |
| Gemini web / Google AI Pro | El conector Gemini CLI no demuestra el uso total de la web ni de todos los productos del plan. |

Estos proveedores **no aparecen como conectados ni generan porcentajes estimados**. Se añadirán cuando exista un contrato de cuotas y un mecanismo de autenticación seguro que se pueda verificar.
