# LocalSmtp

LocalSmtp is a fork of the backend of rnwood/smtp4dev. Since smtp4dev hasn't been updated in a long time I decided to 

* update it to .NET 6
* remove any nodejs dependency by using Blazor Wasm as a front end
* add a bit of clean architecture to the backend
* made some minor adjustments to the UI to work better with MudBlazor
* added possibility to run it as Windows Server on Kestrel (what I'm using mainly)

What I'm not going to do myself is adding Docker support. I invite anyone to submit Docker support via pull request.

![local-smtp](https://user-images.githubusercontent.com/26190934/168400943-f0308167-dbc7-4186-a177-29c18c3082c8.gif)

## Adding Migrations

```
cd src\LocalSmtp.Server.Persistence
dotnet ef migrations add InitialCreate -s ..\LocalSmtp\Server\ -o Data\Migrations
```
