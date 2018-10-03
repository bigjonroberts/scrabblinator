FROM microsoft/dotnet:2.1-sdk AS build-env

# install mono 5.14.0.177
RUN apt-get update \
  && apt-get install -y curl \
  && rm -rf /var/lib/apt/lists/*

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN echo "deb http://download.mono-project.com/repo/debian stretch/snapshots/5.14.0.177 main" > /etc/apt/sources.list.d/mono-official.list \
  && apt-get update \
  && apt-get install -y binutils locales make mono-devel ca-certificates-mono fsharp mono-vbnc nuget referenceassemblies-pcl \
  && rm -rf /var/lib/apt/lists/* /tmp/* \
  && echo 'en_US.UTF-8 UTF-8' > /etc/locale.gen && /usr/sbin/locale-gen

WORKDIR /scrabblinator

# copy everything and build
COPY . ./
RUN dotnet test ScrabblinatorTests/ScrabblinatorTests.fsproj
RUN dotnet publish Scrabblinator/Scrabblinator.fsproj -c Release -o ../build

# Done building app - now build runtime docker image
FROM microsoft/dotnet:2.1-runtime-alpine

RUN mkdir /app
COPY --from=0 /scrabblinator/build /app

ENV MONO_THREADS_PER_CPU=5000

ENTRYPOINT ["dotnet", "/app/Scrabblinator.dll", "--server"]
CMD ["dotnet", "/app/Scrabblinator.dll", "--server"]
