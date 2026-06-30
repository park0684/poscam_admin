# AuthServer 빌드 단계
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

WORKDIR /src

# NuGet 복원 캐시를 활용할 수 있도록 프로젝트 파일을 먼저 복사한다.
COPY ["AuthServer/poscam.AuthServer.csproj", "AuthServer/"]

RUN dotnet restore "AuthServer/poscam.AuthServer.csproj"

# 저장소의 실제 소스코드를 복사한다.
COPY . .

# 운영용 Release 결과물을 생성한다.
RUN dotnet publish "AuthServer/poscam.AuthServer.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


# AuthServer 실행 단계
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

# 빌드 결과물을 비루트 app 사용자 소유로 복사한다.
COPY --from=build --chown=app:app /app/publish .

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "poscam.AuthServer.dll"]