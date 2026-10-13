# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# csproj만 먼저 복사해서 restore 결과를 레이어 캐시로 재사용
COPY src/PharmacyWorkflow.Domain/PharmacyWorkflow.Domain.csproj src/PharmacyWorkflow.Domain/
COPY src/PharmacyWorkflow.Api/PharmacyWorkflow.Api.csproj src/PharmacyWorkflow.Api/
RUN dotnet restore src/PharmacyWorkflow.Api/PharmacyWorkflow.Api.csproj

COPY src/ src/
RUN dotnet publish src/PharmacyWorkflow.Api/PharmacyWorkflow.Api.csproj \
    -c Release -o /out --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# SQLite DB 파일 위치 (docker-compose의 볼륨을 /data 에 연결)
RUN mkdir -p /data
ENV ConnectionStrings__Default="Data Source=/data/pharmacy.db"
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PharmacyWorkflow.Api.dll"]
