# How to Download FFmpeg Libraries for macOS

### 1. Download Prebuilt Libraries
Download the prebuilt FFmpeg archive (LGPL or GPL) for your architecture (arm64 for Apple Silicon, x64 for Intel) from:
- https://github.com/ispysoftware/agentdvr-ffmpeg-build/releases

For example, download `ffmpeg-osx-arm64-lgpl.tar.gz`.

### 2. Extract and Copy `.dylib` Files
Extract the archive and copy all `.dylib` files from the extracted `lib/` directory into `FfmpegLibraries/macOS/`.

### 3. Resolve Dependent Libraries (Add `@loader_path`)
The prebuilt binaries reference each other via `@rpath` but do not include the binary directory in their search path by default.

Run the following command from the project root:

```sh
for f in FfmpegLibraries/macOS/*.dylib; do
  install_name_tool -add_rpath "@loader_path" "$f" 2>/dev/null || true
done