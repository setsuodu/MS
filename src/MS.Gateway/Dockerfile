# build context 是仓库根目录，方便以后 Gateway 引用 MS.Shared
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MS.Gateway/MS.Gateway.csproj", "src/MS.Gateway/"]
COPY ["src/MS.Shared/MS.Shared.csproj", "src/MS.Shared/"]
RUN dotnet restore "src/MS.Gateway/MS.Gateway.csproj"

COPY src/MS.Gateway/ src/MS.Gateway/
COPY src/MS.Shared/ src/MS.Shared/
WORKDIR /src/src/MS.Gateway

RUN dotnet publish -c Release -o /app/publish -p:PublishAot=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["./MS.Gateway"]
