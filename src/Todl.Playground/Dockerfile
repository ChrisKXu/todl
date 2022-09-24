FROM mcr.microsoft.com/azure-functions/dotnet:4

ARG ARG_GIT_BRANCH=unknown
ARG ARG_GIT_COMMIT=unknown
ENV GIT_BRANCH ${ARG_GIT_BRANCH}
ENV GIT_COMMIT ${ARG_GIT_COMMIT}

WORKDIR /home/site/wwwroot
EXPOSE 80

COPY . .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
