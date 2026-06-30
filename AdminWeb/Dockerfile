# AdminWeb 빌드 단계
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

WORKDIR /src

# 프로젝트 파일을 먼저 복사해 NuGet 복원 캐시를 활용한다.
COPY ["AdminWeb/poscam.AdminWeb.csproj", "AdminWeb/"]

RUN dotnet restore "AdminWeb/poscam.AdminWeb.csproj"

# 저장소 소스코드를 복사한다.
COPY . .

# 운영용 Release 결과물을 생성한다.
RUN dotnet publish "AdminWeb/poscam.AdminWeb.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


# AdminWeb 실행 단계
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build --chown=app:app /app/publish .

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "poscam.AdminWeb.dll"]