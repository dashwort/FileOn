Remove-Item 'C:\Users\david\Nextcloud\Personal_Coding_Projects\WorkProjects\FileOn\WebApi\bin' -Recurse -Force
Remove-Item 'C:\Users\david\Nextcloud\Personal_Coding_Projects\WorkProjects\FileOn\WebApi\obj' -Recurse -Force
Remove-Item 'C:\Users\david\Nextcloud\Personal_Coding_Projects\WorkProjects\FileOn\WebApi\migrations' -Recurse -Force
Remove-Item 'C:\Users\david\Nextcloud\Personal_Coding_Projects\WorkProjects\FileOn\WebApi\LocalDatabase.db'

dotnet tool install -g dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate
dotnet ef database update