# ====================================================================
# STAGE 1: Build
# ====================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivo del proyecto y restaurar dependencias
COPY ["Back-end-RepostesSAE.csproj", "./"]
RUN dotnet restore "Back-end-RepostesSAE.csproj"

# Copiar el resto del código fuente
COPY . .

# Compilar la aplicación en modo Release
RUN dotnet build "Back-end-RepostesSAE.csproj" -c Release -o /app/build

# ====================================================================
# STAGE 2: Publish
# ====================================================================
FROM build AS publish
RUN dotnet publish "Back-end-RepostesSAE.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ====================================================================
# STAGE 3: Runtime
# ====================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Exponer puerto (Railway/Render inyectan $PORT)
EXPOSE 8080

# Copia los archivos publicados
COPY --from=publish /app/publish .

# Entorno
ENV ASPNETCORE_ENVIRONMENT=Production

# Entrypoint con shell para expandir $PORT
CMD ["sh", "-c", "dotnet Back-end-RepostesSAE.dll --urls http://+:${WEBSITES_PORT:-${PORT:-8080}}"]
