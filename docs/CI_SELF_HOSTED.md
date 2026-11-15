# Runner self-hosted en WSL

Guía para ejecutar los workflows de GitHub Actions en esta PC, arrancando y
deteniendo el runner manualmente.

## Diagnóstico de esta máquina

| Elemento | Estado |
|---|---|
| WSL2 | OK, distro `Ubuntu` 26.04, `systemd` activo |
| GitHub CLI | OK, autenticado como `FT-Key` |
| Repositorio | `FT-Key/ICS_TPI2026` (público), rama base `development` |
| Runner registrado | No |
| .NET SDK en WSL | No instalado (no es obligatorio) |
| Node en WSL | No instalado (no es obligatorio) |
| Docker en WSL | No disponible (solo afecta `docker-build`) |
| RAM / CPU | 5,9 GB / 4 núcleos |

`actions/setup-dotnet` y `actions/setup-node` descargan las herramientas en cada
job, por eso no hace falta instalarlas a mano dentro de WSL.

## Antes de empezar: dos advertencias

1. **El repositorio es público.** GitHub recomienda no usar runners
   self-hosted en repositorios públicos: cualquier persona que abra un PR puede
   ejecutar código en tu PC. Si el repo sigue público, restringí la aprobación
   de workflows de forks en **Settings → Actions → General** o usá los runners
   gratuitos de GitHub.
2. **La cola máxima es de 24 horas.** GitHub cancela los jobs de runners
   self-hosted que llevan 24 h en cola (en la práctica puede estirarse hasta
   ~48 h, pero no es configurable). No se puede fijar en 48 h.

## Instalación (una sola vez)

Desde la raíz del repositorio:

```powershell
.\scripts\runner\setup.ps1
```

El script:

1. Detecta `owner/repo` desde `origin`.
2. Pide un token de registro a la API con `gh`.
3. Descarga la última versión del runner en `~/actions-runner` dentro de WSL.
4. Lo registra con el label `ics-wsl`.

Para verificar el resultado:

```powershell
.\runner.ps1 -StatusOnly
```

En GitHub: **Settings → Actions → Runners** debe aparecer el runner como
`Idle` (o `Offline` si todavía no lo arrancaste).

## Uso diario

```powershell
.\runner.ps1
```

Qué hace:

- Levanta `~/actions-runner/run.sh` dentro de WSL en segundo plano.
- Consulta GitHub cada 30 segundos.
- Muestra un panel con:
  - estado del runner (`online` / `offline`, `libre` / `ocupado`);
  - runs en cola y en ejecución;
  - jobs pendientes;
  - jobs completados de la sesión, fallidos y porcentaje;
  - barra de progreso y últimos eventos.
- Cuando presionás **Ctrl+C**, no corta el job en curso: marca la sesión como
  “drenando” y se cierra sola cuando la cola queda vacía y el runner está libre.
- Un segundo **Ctrl+C** fuerza la salida.
- Al salir, detiene el proceso del runner en WSL.

Opciones:

```powershell
.\runner.ps1 -StatusOnly          # solo estado y cola, sin arrancar
.\runner.ps1 -Stop                # detener un runner que quedó colgado
.\runner.ps1 -PollSeconds 15      # refresco más rápido
.\runner.ps1 -Distro Ubuntu       # otra distro de WSL
```

## Cómo probar el circuito completo

1. Arrancá el runner:

   ```powershell
   .\runner.ps1
   ```

2. En otra terminal, dispará el workflow de prueba:

   ```powershell
   gh workflow run runner-smoke.yml
   ```

3. Volvé al panel: el job debe pasar de “en cola” a “en ejecución” y después a
   `Succeeded`.

4. Si querés probar la cola con el resto del CI, abrí un PR o hacé push a
   `development`; los jobs quedan en `Queued` hasta que el runner esté online.

5. Cuando el panel marque 100 % y “sin trabajos pendientes”, presioná Ctrl+C.

## Limitar el uso del runner

Para evitar que el runner consuma recursos cuando no lo necesitás:

- Arrancalo al comenzar la sesión de trabajo y cerralo al terminar.
- Si la PC se suspende o se apaga, el runner queda `Offline` y los jobs esperan.
- Los jobs que queden en cola más de 24 h se cancelan.

## Docker

El job `docker-build` necesita Docker dentro de WSL. Dos opciones:

1. **Docker Desktop:** Settings → Resources → WSL Integration → habilitar
   `Ubuntu`. Requiere que Docker Desktop esté abierto.
2. **Docker Engine dentro de Ubuntu:**

   ```bash
   sudo apt update
   sudo apt install -y docker.io
   sudo usermod -aG docker $USER
   sudo systemctl enable --now docker
   ```

   Después cerrá y abrí WSL para que tome el grupo.

Mientras Docker no esté disponible, dejá `docker-build` en `ubuntu-latest`.
En un repositorio público esos minutos son gratuitos.

## Problemas frecuentes

| Síntoma | Causa / solución |
|---|---|
| `El runner todavía no está instalado` | Ejecutá `.\scripts\runner\setup.ps1`. |
| El runner figura `Offline` | WSL o la PC están apagados, o el proceso no arrancó. |
| Los jobs quedan en `Queued` | No hay runner online con el label `ics-wsl`. |
| `No pude generar el token` | Tu usuario no es admin del repo o `gh` no está logueado. |
| Docker no aparece | Habilitá la integración de Docker Desktop o instalá Docker Engine. |
| La PC se queda sin RAM | Cerrá Docker Desktop y aplicaciones pesadas; el runner no necesita Docker salvo para ese job. |
| Job cancelado tras muchas horas | GitHub cancela la cola a las 24 h. |

## Referencias

- [About self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners)
- [Usage limits for self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/usage-limits-for-self-hosted-runners)
- [Adding self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/adding-self-hosted-runners)
