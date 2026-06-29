# hambusweb

`hambusweb` is the Blazor Server website for the HamBus suite.

It currently provides:
- Interactive Blazor routes for the site shell and roadmap page
- Static HTML pages under `wwwroot/static/`
- Initial suite/product framing for `HamBusLog`

## Run locally

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/hambusweb/hambusweb.csproj
```

Site available at **http://localhost:5180**.

## Build

```bash
dotnet build /home/darryl/github/Hambus/HamBusLog/hambusweb/hambusweb.csproj
```

## Docker

### Build the image

```bash
docker build -t hambusweb:latest ./hambusweb
```

### Run the container

```bash
docker run -d --name hambusweb -p 5180:8080 hambusweb:latest
```

Site available at **http://localhost:5180**.

### docker compose (recommended for deployment)

```bash
cd hambusweb
docker compose up -d          # start in background
docker compose down           # stop and remove
docker compose up -d --build  # rebuild and restart
```

The Compose file exposes port **5180** on the host and maps it to the internal port **8080** where Kestrel listens.

## Static pages

The following static pages are included out of the box:
- `/static/index.html`
- `/static/hambuslog.html`

These are served directly from `wwwroot/static/`.


