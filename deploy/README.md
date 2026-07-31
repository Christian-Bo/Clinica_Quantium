# Publicación inicial en IIS

## Backend

```powershell
dotnet publish src/ClinicaPro.Api -c Release -o publish/api
```

## Frontend

```powershell
cd src/ClinicaPro.Client
npm install
npm run build
cd ../..
dotnet publish src/ClinicaPro.Client -c Release -o publish/client
```

En IIS se recomienda:

1. Publicar API y frontend bajo el mismo dominio, en aplicaciones separadas o mediante proxy inverso.
2. Instalar ASP.NET Core Hosting Bundle correspondiente a .NET 8.
3. Configurar HTTPS.
4. Configurar la cadena de SQL Server mediante variables de entorno o configuración protegida.
5. No copiar `appsettings.Development.json` como configuración productiva.
6. Verificar permisos de la identidad del Application Pool.
7. Probar restauración de respaldos de SQL Server.
