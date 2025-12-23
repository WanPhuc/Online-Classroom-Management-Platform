# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 10000

# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["BTL_QuanLyLopHocTrucTuyen.csproj", "./"]
RUN dotnet restore "./BTL_QuanLyLopHocTrucTuyen.csproj"

COPY . .
RUN dotnet publish "./BTL_QuanLyLopHocTrucTuyen.csproj" -c Release -o /out /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=10000



ENTRYPOINT ["dotnet", "BTL_QuanLyLopHocTrucTuyen.dll"]
