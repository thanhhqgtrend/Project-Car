# LuxuryCar MVC5 / .NET Framework 4.7.2

Ứng dụng ASP.NET MVC 5 chạy trên .NET Framework 4.7.2, triển khai bằng Windows IIS và dùng SQL Server 2025 bên ngoài.

## Yêu cầu

- Windows Server hoặc Windows development machine.
- IIS với ASP.NET 4.x enabled.
- .NET Framework 4.7.2 runtime/developer pack.
- Visual Studio hoặc Build Tools có .NET Framework targeting pack.
- SQL Server 2025 instance đã tạo database `LuxuryCarDb`.

## Cấu hình

Toàn bộ cấu hình runtime nằm trong `Web.config`.

Connection string mẫu:

```xml
<add name="DefaultConnection"
     connectionString="Server=SQL2025-SERVER,1433;Database=LuxuryCarDb;User Id=LuxuryCarApp;Password=CHANGE_ME;TrustServerCertificate=True;MultipleActiveResultSets=True;"
     providerName="System.Data.SqlClient" />
```

Các khóa cấu hình site, email, payment, Cloudinary và Geoapify nằm trong `appSettings`.

## Build và publish

```powershell
nuget restore LuxuryCar.csproj
msbuild LuxuryCar.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem /p:PublishUrl=C:\publish\LuxuryCar
```

## IIS

- App Pool: `.NET CLR Version v4.0`
- Pipeline: `Integrated`
- Site physical path: thư mục publish
- Cấp quyền đọc/ghi phù hợp cho identity của App Pool nếu cần upload/cache/log.

## SQL Server 2025

Kiểm tra version:

```sql
SELECT SERVERPROPERTY('ProductVersion') AS ProductVersion,
       SERVERPROPERTY('Edition') AS Edition;
```

Mục tiêu production: SQL Server 2025, major version `17`.

## Ghi chú

- Docker không còn là đường chạy của dự án này.
- EF Core migrations cũ đã bị loại bỏ; schema production phải được quản lý bằng backup/restore hoặc script SQL riêng cho SQL Server 2025.
