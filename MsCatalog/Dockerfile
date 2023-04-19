FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
 WORKDIR /app
 EXPOSE 80
 FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
 WORKDIR /src
 COPY ["ms-catalog.csproj", ""]
 RUN dotnet restore "ms-catalog.csproj"
 COPY . .
 WORKDIR "/src/."
 RUN dotnet build "ms-catalog.csproj" -c Release -o /app/build
 FROM build AS publish
 RUN dotnet publish "ms-catalog.csproj" -c Release -o /app/publish
 FROM base AS final
 WORKDIR /app
 COPY --from=publish /app/publish .
 ENTRYPOINT ["dotnet", "ms-catalog.dll"]