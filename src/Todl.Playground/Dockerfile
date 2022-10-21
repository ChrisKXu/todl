FROM mcr.microsoft.com/dotnet/aspnet:6.0

ARG ARG_GIT_BRANCH=unknown
ARG ARG_GIT_COMMIT=unknown
ENV GIT_BRANCH ${ARG_GIT_BRANCH}
ENV GIT_COMMIT ${ARG_GIT_COMMIT}

EXPOSE 80

COPY . .
ENTRYPOINT [ "dotnet", "Todl.Playground.dll" ]
