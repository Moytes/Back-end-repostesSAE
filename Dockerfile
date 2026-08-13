FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Back-end-RepostesSAE.csproj", "./"]
RUN dotnet restore "Back-end-RepostesSAE.csproj"
COPY . .
RUN dotnet publish "Back-end-RepostesSAE.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
CMD ["sh", "-c", "dotnet Back-end-RepostesSAE.dll --urls http://+:${PORT:-8080}"]
