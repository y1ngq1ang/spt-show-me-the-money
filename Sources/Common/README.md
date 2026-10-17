# Vendored `spt-common`

This folder used to be a git submodule pointing at `spt-common`. Because that remote is no longer
reachable, the sources are now vendored directly into this repository.

- Upstream: `https://github.com/mattpsvreis/spt-common`
- Vendored from commit: `e2385c24b27b1433e0f22e2a2b18a2fbccffb604`
- Layout is preserved one-to-one: upstream `Sources/Common/<x>` is this repo's `Sources/Common/<x>`.
  Diffing against a newer upstream revision therefore needs no path translation.
- The upstream `Tests/Common.Tests` project (which lives outside `Sources/Common`) is not vendored,
  and neither is the standalone `SwiftXP.SPT.Common.csproj` — both were 4.x-only scaffolding.

The mod builds these files by explicit `<Compile Include>` globs in
`Sources/Client/SwiftXP.SPT.ShowMeTheMoney.Client.csproj` and
`Sources/Server/SwiftXP.SPT.ShowMeTheMoney.Server.csproj`, so only the subset each side needs is
compiled. `Http/`, `IO/` and `Extensions/` are currently not compiled by either project; they are
kept so the tree stays faithful to upstream.
