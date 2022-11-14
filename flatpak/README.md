# FieldWorks flatpak packaging

This directory holds flatpak packaging specification files and tools.

## Building

### Dependencies

Flathub repo, FW flatpak manifest, tools:

```bash
sudo add-apt-repository ppa:flatpak/stable
sudo apt install flatpak-builder flatpak
flatpak --user remote-add --if-not-exists flathub \
  https://flathub.org/repo/flathub.flatpakrepo
flatpak --user install flathub org.gnome.Platform//42 org.gnome.Sdk//42
# Use flatpak-builder from flathub since the fix at
# https://github.com/flatpak/flatpak-builder/pull/497 is not yet available in
# the .deb package.
flatpak install flathub org.flatpak.Builder
```

#### Optional dependencies

Clone projects somewhere to help generate dependency download lists:
```bash
git clone https://github.com/sillsdev/flexbridge.git --branch develop
git clone https://github.com/silnrsi/encoding-converters-core.git \
  --branch master
```

Install tool flatpak-dotnet-generator.py and dependency dotnet5 sdk:
```bash
flatpak install flathub \
  org.freedesktop.Sdk.Extension.dotnet5 org.freedesktop.Sdk/x86_64/20.08
git clone https://github.com/flatpak/flatpak-builder-tools.git
# Until feature/multiple-csproj PR #206 is merged, use unmerged branch:
cd flatpak-builder-tools
git remote add marksvc https://github.com/marksvc/flatpak-builder-tools.git
git fetch --all && git checkout marksvc/feature/multiple-csproj
```

Install xonsh to run some dependency-url-generating scripts.
```bash
sudo apt install xonsh
```

### Build

Build and install the flatpak package:

```bash
./build
```

Note that your first build will take time to download and build all
dependencies. Subsequent builds benefit from caching.

## Testing

Run FieldWorks in the flatpak package:

```bash
./run
```

Open a shell inside the FieldWorks flatpak instead of running FieldWorks:

```bash
./shell
```

## Debugging flatpak FieldWorks

Make an expected directory if you haven't built FW on the machine yet:
```bash
mkdir -p ../Output_x86_64/Debug
```

Open the FieldWorks workspace in VSCode.

### Managed debugging

Run FieldWorks from the flatpak by running this command (as copied from
FieldWorks launch.json):

```bash
flatpak run --devel --env=FW_DEBUG=true \
  --env=FW_MONO_OPTS=--debugger-agent=address=127.0.0.1:55555,transport=dt_socket,server=y,suspend=n \
  org.sil.FieldWorks
```

Debug target "Attach to local (such as flatpak)" in VSCode.

Note: Need to investigate why the vscode debugger isn't connecting to FW in the
flatpak correctly any more.

### Unmanaged debugging

Install VSCode gdb debugging extension:
`code --install-extension webfreak.debug`

Run FieldWorks from the flatpak by running this command (as copied from
FieldWorks launch.json):

```bash
flatpak run --devel --env=FW_DEBUG=true \
  --env=FW_COMMAND_PREFIX="gdbserver 127.0.0.1:9999" \
  --env=FW_MONO_COMMAND=/app/bin/mono-sgen \
  org.sil.FieldWorks
```

Debug target "Attach to local gdbserver" in VSCode.

You can check if you have org.sil.FieldWorks.Debug installed to get debugging
symbols in /app/lib/debug in the flatpak by running `flatpak list --all | cat`.

## Format files

```bash
npm run format
```

## Validate appdata

```bash
flatpak install flathub org.freedesktop.appstream-glib
flatpak run org.freedesktop.appstream-glib validate \
  ../DistFiles/Linux/org.sil.FieldWorks.metainfo.xml
```

## Clean up

You can safely delete the following directories, although it will make the next
package-build take longer:

```
node_modules
~/.cache/flatpak-build-cache
```
