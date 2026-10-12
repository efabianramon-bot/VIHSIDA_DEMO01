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

$archivo = Join-Path $carpeta ("ConavihsidaCoreDb_{0}.bak" -f (Get-Date -Format 'yyyy-MM-dd_HHmmss'))
sqlcmd -S '(LocalDB)\MSSQLLocalDB' -E -b -l 60 -Q "BACKUP DATABASE ConavihsidaCoreDb TO DISK = N'$archivo' WITH INIT, CHECKSUM;"

if ($LASTEXITCODE -eq 0 -and (Test-Path $archivo)) {
    "Respaldo creado: $archivo ({0:N0} KB)" -f ((Get-Item $archivo).Length / 1KB)
} else {
    Write-Error 'No se pudo crear el respaldo.'
    exit 1
}
