# Build stage: restore and publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Locally.Examples.Orders/Locally.Examples.Orders.csproj Locally.Examples.Orders/
RUN dotnet restore Locally.Examples.Orders/Locally.Examples.Orders.csproj
COPY Locally.Examples.Orders/ Locally.Examples.Orders/
RUN dotnet publish Locally.Examples.Orders/Locally.Examples.Orders.csproj -c Release -o /out --no-restore

# Runtime stage: the ASP.NET Core runtime and the published app only.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV PORT=8080
COPY --from=build /out .
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Locally.Examples.Orders.dll"]
