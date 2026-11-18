FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json financial-lab-account.sln ./
COPY financial-lab-account.Api/financial-lab-account.Api.csproj financial-lab-account.Api/
COPY financial-lab-account.Business/financial-lab-account.Business.csproj financial-lab-account.Business/
COPY financial-lab-account.Contracts/financial-lab-account.Contracts.csproj financial-lab-account.Contracts/
COPY financial-lab-account.Data/financial-lab-account.Data.csproj financial-lab-account.Data/
COPY financial-lab-transfer.Api/financial-lab-transfer.Api.csproj financial-lab-transfer.Api/
COPY financial-lab-transfer.Business/financial-lab-transfer.Business.csproj financial-lab-transfer.Business/
COPY financial-lab-transfer.Data/financial-lab-transfer.Data.csproj financial-lab-transfer.Data/
COPY financial-lab-antifraud.Api/financial-lab-antifraud.Api.csproj financial-lab-antifraud.Api/
RUN dotnet restore financial-lab-account.sln
COPY . .

FROM build AS publish-account
RUN dotnet publish financial-lab-account.Api -c Release -o /app --no-restore

FROM build AS publish-transfer
RUN dotnet publish financial-lab-transfer.Api -c Release -o /app --no-restore

FROM build AS publish-antifraud
RUN dotnet publish financial-lab-antifraud.Api -c Release -o /app --no-restore

FROM build AS bundles
RUN dotnet tool install --global dotnet-ef --version 9.0.9
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle -p financial-lab-account.Data -s financial-lab-account.Api -o /bundles/account-migrations \
 && dotnet ef migrations bundle -p financial-lab-transfer.Data -s financial-lab-transfer.Api -o /bundles/transfer-migrations

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS migrations
WORKDIR /bundles
COPY --from=bundles /bundles .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "./account-migrations --connection \"$ACCOUNT_DB\" && ./transfer-migrations --connection \"$TRANSFER_DB\""]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS account-api
WORKDIR /app
COPY --from=publish-account /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "financial-lab-account.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS transfer-api
WORKDIR /app
COPY --from=publish-transfer /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "financial-lab-transfer.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS antifraud-api
WORKDIR /app
COPY --from=publish-antifraud /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "financial-lab-antifraud.Api.dll"]
