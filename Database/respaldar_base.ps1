# Hace un respaldo completo de ConavihsidaCoreDb en Database\Respaldos.
# Uso, desde PowerShell:  .\Database\respaldar_base.ps1
#
# Hazlo antes de cualquier cambio de estructura o de mover archivos. Los
# respaldos no se versionan (pueden contener datos de pacientes).
#
# Para restaurar uno (reemplaza la base actual):
#   sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -Q "ALTER DATABASE ConavihsidaCoreDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE ConavihsidaCoreDb FROM DISK = 'RUTA\ARCHIVO.bak' WITH REPLACE; ALTER DATABASE ConavihsidaCoreDb SET MULTI_USER;"

$carpeta = Join-Path $PSScriptRoot 'Respaldos'
New-Item -ItemType Directory -Force $carpeta | Out-Null

# Cuando Visual Studio (u otra sesion) ya tiene levantada la instancia de
# LocalDB, el nombre "(LocalDB)\MSSQLLocalDB" puede no conectar desde otra
# sesion. En ese caso se entra por el pipe de la instancia en ejecucion.
$servidor = '(LocalDB)\MSSQLLocalDB'
$instancia = Get-CimInstance Win32_Process -Filter "Name='sqlservr.exe'" |
    Where-Object { $_.CommandLine -match 'MSSQLLocalDB' } |
    ForEach-Object { if ($_.CommandLine -match '-s(LOCALDB#[0-9A-F]+)') { $Matches[1] } } |
    Select-Object -First 1
if ($instancia) {
    $servidor = "np:\\.\pipe\$instancia\tsql\query"
}

$archivo = Join-Path $carpeta ("ConavihsidaCoreDb_{0}.bak" -f (Get-Date -Format 'yyyy-MM-dd_HHmmss'))
sqlcmd -S $servidor -E -b -l 60 -Q "BACKUP DATABASE ConavihsidaCoreDb TO DISK = N'$archivo' WITH INIT, CHECKSUM;"

if ($LASTEXITCODE -eq 0 -and (Test-Path $archivo)) {
    "Respaldo creado: $archivo ({0:N0} KB)" -f ((Get-Item $archivo).Length / 1KB)
} else {
    Write-Error 'No se pudo crear el respaldo.'
    exit 1
}
