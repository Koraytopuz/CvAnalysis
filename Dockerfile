FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/CvAnalysis.Server.csproj", "backend/"]
RUN dotnet restore "backend/CvAnalysis.Server.csproj"
COPY . .
WORKDIR "/src/backend"
RUN dotnet build "CvAnalysis.Server.csproj" -c Release -o /app/build
RUN dotnet publish "CvAnalysis.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CvAnalysis.Server.dll", "--urls", "http://0.0.0.0:10000"]
