FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5213

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /app
COPY ["./itembank-index-backend/itembank-index-backend.csproj", "./"]
RUN dotnet restore "./itembank-index-backend.csproj"
COPY . .
WORKDIR /app
RUN dotnet build "./itembank-index-backend/itembank-index-backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./itembank-index-backend/itembank-index-backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UsppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
ENV ASPNETCORE_HTTP_PORTS=5213
ENV ELASTICSEARCH_API_KEY=${ELASTICSEARCH_API_KEY}
ENV MSSQL_CONNECTION_STRING=${MSSQL_CONNECTION_STRING}
# for mssql ssl/tls handshake, that runs lower protocal version
USER root
RUN sed -i 's/\[openssl_init\]/# [openssl_init]/' /etc/ssl/openssl.cnf
RUN printf "\n\n[openssl_init]\nssl_conf = ssl_sect" >> /etc/ssl/openssl.cnf
RUN printf "\n\n[ssl_sect]\nsystem_default = ssl_default_sect" >> /etc/ssl/openssl.cnf
RUN printf "\n\n[ssl_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf
USER $APP_UID
ENTRYPOINT ["dotnet", "itembank-index-backend.dll"]

# docker build -t itembank-index-backend .
# docker run -d -it -p 5213:5213 --restart always --name itembank-index-backend itembank-index-backend
