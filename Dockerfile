# שלב בסיס - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=http://+:80

# שלב בנייה - Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src

# העתקת קובץ ה-CSProj לתיקיית /src
COPY ["TodoApi.csproj", "./"]

# שחזור תלויות
RUN dotnet restore "TodoApi.csproj"

# העתקת שאר הקבצים
COPY . .

# הגדרת סביבת עבודה לתיקיית הפרויקט
WORKDIR /src

# בנייה
RUN dotnet build "TodoApi.csproj" -c $configuration -o /app/build

# שלב פרסום - Publish
FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TodoApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

# שלב סופי - Final Image
FROM base AS final
WORKDIR /app

# העתקת קבצים מהשלב הקודם
COPY --from=publish /app/publish .

# נקודת הכניסה
ENTRYPOINT ["dotnet", "TodoApi.dll"]
