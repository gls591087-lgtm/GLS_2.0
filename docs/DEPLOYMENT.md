# Build and Deployment

This checklist covers the Windows launcher and the files needed beside it at runtime. It does not grant permission to redistribute the KartRider client or its assets.

## Build

Install the .NET 8 SDK, then run from the project directory:

```powershell
dotnet restore Launch.csproj
dotnet build Launch.csproj --configuration Release
```

The output is `bin/Release/net8.0-windows/`. The project targets `net8.0-windows` and uses Windows Forms. The default build is framework-dependent; machines running it need the matching .NET 8 Desktop Runtime.

## Prepare a private release folder

1. Build the project in Release configuration.
2. Copy the compatible client executable and its required client files into the release folder, preserving their expected relative directory structure.
3. Include a compatible `KartRider.pin` where the PIN editor can find it under the release folder.
4. Place the server-required `data.bml` in the same directory as `GLS 2.0.exe`.
5. Include any required `Data`, `DataRaw`, runtime, and native client files required by the specific game-client build.
6. Start the launcher once and check that the server reaches the ready state before testing a client connection.

The source repository does not contain a reproducible manifest for all proprietary client assets. Validate the exact release folder against the client version being used; do not assume that copying only `KartRider.exe` is sufficient.

## Server settings

The launcher generates this configuration when **Server / Game Start** is clicked (the `ExternalIP` value comes from the Server Settings form):

```ini
[Server]
ExternalIP=127.0.0.1
Port=3305
AutoRestartTime=0
DataPath=Data

[Database]
Host=local
Schema=kcdb
Nickname=player
Password=player
```

The generated file is overwritten on each server start. The launcher explicitly sets the database host to `local` and seeds in-memory `playerN` accounts. `DataPath` is written, but the current game-data cache readers locate `data.bml` directly under the application base directory.

The server binds TCP and UDP port 3305 on all local IPv4 interfaces. `ExternalIP` is the endpoint it advertises to clients. The Server Settings form's port text box is not currently wired into this generated file; use 3305 unless the source is changed consistently for both transports and client PIN configuration.

## Local, LAN, and VPN use

### One computer

- Advertised address: `127.0.0.1`.
- Client PIN endpoint: `127.0.0.1:3305`.
- No router forwarding is required.

### LAN

- Set the server's LAN IPv4 address as the advertised address.
- Set each client's PIN login server to the same address and port 3305.
- Permit inbound TCP and UDP 3305 in the host firewall.

### VPN

- Set the server's VPN interface IPv4 address as the advertised address.
- Set each client's PIN endpoint to that VPN address and port 3305.
- Verify that the VPN routes client traffic to the host and permits both TCP and UDP traffic.
- Test login, room entry, race synchronization, and item events; a successful TCP connection alone does not prove the UDP path works.

## Account and client profile behavior

The Server Settings account count determines how many local accounts are created at startup. The Launcher Options player number selects which account to put in the generated profile. A selected player number greater than the account count prevents startup/client launch.

For account number `N`:

```text
Username: playerN
Password: playerN
```

`Profile/kr/launcher.xml` is written in UTF-16. The game is started with `-profile:launcher` and its working directory set to the release directory.

## PIN editor behavior

The PIN editor recursively searches below the launcher's base directory and selects the first file named `KartRider.pin`. It updates the first `AuthMethod` / first `LoginServer` endpoint, requires an IPv4 literal and a valid 16-bit port, writes the encoded file, and first saves a copy as `KartRider.pin.bak` beside the source PIN file.

If a release folder contains more than one PIN file, verify which one the editor selected before saving. Distribute a PIN file only when its target server address is appropriate for the recipient.

## Files to exclude from source control

The repository `.gitignore` excludes build outputs, user settings, generated profiles/configuration, PIN files and backups, and log files. Keep proprietary client binaries and data out of a public source repository unless redistribution is explicitly permitted. Review the staged file list before the first push, especially if the repository previously contained `bin` or `obj` files.

## Release verification

Before sharing a release folder:

- Confirm `GLS 2.0.exe` starts and the server reaches ready state.
- Confirm the selected player can log in using the generated `playerN` credentials.
- Confirm a second client can connect over the intended LAN/VPN path.
- Verify both TCP and UDP reachability on port 3305.
- Test room creation/join, race start, a pickup, a targeted item event, result display, and return to the room with the exact intended client build.
- Confirm no accidental database credentials, local configuration, PIN files, client assets, or oversized build files are staged for GitHub.
- Add the project's chosen `LICENSE` file before advertising the source as open source.

## Diagnostics and log files

The C# server reports diagnostics to its console. Item pickup and unhandled-packet file logging is disabled. Startup still creates or updates configuration/profile files, and the PIN editor creates a `.bak` file. Native client proxy DLLs may have independent logging behavior that is not controlled by this C# project.
