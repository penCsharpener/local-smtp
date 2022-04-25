# local-smtp

## Adding Migrations

```
cd src\LocalSmtp.Server.Persistence
dotnet ef migrations add InitialCreate -s ..\LocalSmtp\Server\ -o Data\Migrations
```