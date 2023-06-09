echo VER=${VER:-latest}
docker build . -f WebFiles/Dockerfile -t mgrcar/webfiles:${VER:-latest}
docker push mgrcar/webfiles:${VER:-latest}
