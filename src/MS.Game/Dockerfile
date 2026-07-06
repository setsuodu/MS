FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MS.Game/MS.Game.csproj", "src/MS.Game/"]
RUN dotnet restore "src/MS.Game/MS.Game.csproj"

COPY src/MS.Game/ src/MS.Game/
WORKDIR /src/src/MS.Game

RUN dotnet publish -c Release -o /app/publish -p:PublishAot=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["./MS.Game"]
