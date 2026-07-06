FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MS.UserService/MS.UserService.csproj", "src/MS.UserService/"]
COPY ["src/MS.Shared/MS.Shared.csproj", "src/MS.Shared/"]
RUN dotnet restore "src/MS.UserService/MS.UserService.csproj"

COPY src/MS.UserService/ src/MS.UserService/
COPY src/MS.Shared/ src/MS.Shared/
WORKDIR /src/src/MS.UserService

RUN dotnet publish -c Release -o /app/publish -p:PublishAot=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["./MS.UserService"]
