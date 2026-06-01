# HambusLog

## Build

The following commands use `dotnet publish` to create release builds.

### Linux (x64)

```bash
dotnet publish /home/darryl/github/Hambus/HamBusLog/HamBusLog.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

### Windows (x64)

```bash
dotnet publish /home/darryl/github/Hambus/HamBusLog/HamBusLog.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Publish output is under `bin/Release/net10.0/<runtime>/publish/`.

## Debian (.deb)

Keep all packaging artifacts inside the `debian` folder.

1) Publish a Linux build.

```bash
dotnet publish /home/darryl/github/Hambus/HamBusLog/HamBusLog.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

2) Prepare the folder structure and control file (already created if `debian/` exists).

```bash
mkdir -p /home/darryl/github/Hambus/HamBusLog/debian/DEBIAN
mkdir -p /home/darryl/github/Hambus/HamBusLog/debian/usr/local/bin
cat > /home/darryl/github/Hambus/HamBusLog/debian/DEBIAN/control << 'EOF'
Package: hambuslog
Version: 1.0.0
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Your Name <you@example.com>
Description: HamBusLog application
EOF
```

3) Copy the published executable into the staging folder (rename as desired).

```bash
cp /home/darryl/github/Hambus/HamBusLog/bin/Release/net10.0/linux-x64/publish/HamBusLog /home/darryl/github/Hambus/HamBusLog/debian/usr/local/bin/hambuslog
chmod 755 /home/darryl/github/Hambus/HamBusLog/debian/usr/local/bin/hambuslog
```

4) Build the `.deb` inside the `debian` folder.

```bash
dpkg-deb --build /home/darryl/github/Hambus/HamBusLog/debian /home/darryl/github/Hambus/HamBusLog/debian/hambuslog_1.0.0_amd64.deb
```
