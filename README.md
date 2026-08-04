# appbuilder

A generic "upload C# source → get a compiled .exe" pipeline.

## How it works

1. Files are uploaded (via the Worker, from the web UI) into `uploads/{build_id}/`
   in this repo.
2. That commit triggers `.github/workflows/build.yml` via the GitHub API,
   passing `build_id`, `app_name`, and `project_type` as inputs.
3. The workflow copies the uploaded files into a clean build folder. If no
   `.csproj` was included, it generates a minimal one (Console or WinForms,
   self-contained, single-file `win-x64` publish).
4. `dotnet publish` runs on a `windows-latest` runner, and the resulting exe
   is attached to a GitHub Release tagged `build-{build_id}` — a unique,
   stable download URL per build.
5. The `uploads/{build_id}/` folder is removed from the repo afterward to
   keep things tidy.

This repo doesn't contain any specific application — it's the generic
build engine. See `SETUP.md` (in the accompanying delivery, not committed
here) for how the web UI and Cloudflare Worker plug into it.

## Notes

- Uploaded files should be source (`.cs`, `.csproj`, resource files) — not
  large binaries. GitHub's Contents API, which the Worker uses to commit
  files, is meant for text/source-sized content.
- Every build creates a new tagged release rather than overwriting one, so
  old builds stay downloadable unless you clean them up manually.
- If you upload your own `.csproj`, it's used as-is and `project_type` is
  ignored.
