dotnet publish "D:/RouteTrace\src\RouteTrace.Web\RouteTrace.Web.csproj" `
  -c Release `
  --no-restore `
  -o "D:/RouteTrace\artifacts\rider-aot"

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

python -m http.server 5187 `
  --bind 127.0.0.1 `
  --directory "D:/RouteTrace\artifacts\rider-aot\wwwroot"