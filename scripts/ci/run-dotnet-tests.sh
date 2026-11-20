#!/usr/bin/env bash
# Ejecuta la suite de pruebas del backend por categoria (Unit | Integration).
#
# Genera por cada proyecto de prueba:
#   - TestResults/<prefijo>-<proyecto>/<archivo>.trx
#   - TestResults/<prefijo>-<proyecto>.xml (JUnit)
#   - cobertura Cobertura dentro de TestResults/<prefijo>-<proyecto>/
#
# Uso:
#   bash scripts/ci/run-dotnet-tests.sh Unit unit
#   bash scripts/ci/run-dotnet-tests.sh Integration integration
#
# Requiere que el restore ya se haya ejecutado (dotnet restore Dsw2025Tpi.sln).

set -euo pipefail

category="${1:?Uso: run-dotnet-tests.sh <Unit|Integration> <prefijo>}"
prefix="${2:?Uso: run-dotnet-tests.sh <Unit|Integration> <prefijo>}"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
backend_dir="$repo_root/ICS_TPI2026_backend"
results_dir="$backend_dir/TestResults"

cd "$backend_dir"
mkdir -p "$results_dir"

while IFS= read -r project; do
  # Solo proyectos de prueba (Dsw2025Tpi.Tests*), sin importar cuantos se agreguen.
  [[ "$project" == *".csproj" ]] || continue
  [[ "$(basename "$project")" == *"Tests"* ]] || continue

  name="$(basename "$project" .csproj | tr '[:upper:]' '[:lower:]')"
  echo "::group::dotnet test $project ($category)"

  dotnet test "$project" -c Release --no-restore \
    --filter "Category=$category" \
    --logger "trx;LogFileName=$prefix-$name.trx" \
    --logger "junit;LogFilePath=$results_dir/$prefix-$name.xml" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$results_dir/$prefix-$name"

  echo "::endgroup::"
done < <(dotnet sln Dsw2025Tpi.sln list)

# Un filtro sin coincidencias devuelve exit code 0. El gate no debe pasar en ese caso.
count="$(grep -h "<UnitTestResult" "$results_dir/$prefix-"*/*.trx 2>/dev/null | wc -l || true)"
echo "Pruebas de categoria $category ejecutadas: $count"

if [ "$count" -eq 0 ]; then
  echo "::error::No se ejecuto ninguna prueba de categoria $category. Revisar los filtros y los traits."
  exit 1
fi
