# Router Quack

A tool to generate router configurations from user-friendly intent files.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/router-quack-screen-dark.png" />
  <img src="docs/images/router-quack-screen-light.png" alt="Router Quack screenshot" title="Router Quack screenshot" />
</picture>

## Overview

Router Quack takes intent files describing your network topology — autonomous systems, routers, interfaces, and BGP
relationships — and turns them into router configuration files. It validates the topology, resolves cross-AS neighbours,
and auto-generates IPv4/IPv6 link and loopback addresses so you don't have to.

## Installation

### Download the binary

Grab the latest `routerquack` binary from the
[Releases](https://github.com/Tuasco/router-quack/releases) page (linux-x64, self-contained — no runtime needed).

### Build from source

Requires [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/Tuasco/router-quack.git
cd router-quack
dotnet build
```

## Usage

```bash
# Run with the default example file
./routerquack

# Process a specific intent file
./routerquack -f examples/default.yaml

# Process multiple intent files (e.g. one per AS)
./routerquack -f examples/ManyFiles/as_15627.yaml examples/ManyFiles/as_67523.yaml
./routerquack -f examples/ManyFiles/* # You can use bash filename expansion too

# Write output to a specific directory
./routerquack -f examples/default.yaml -o ./my-output

# Dry run in strict mode with verbose logging
./routerquack -n -s -v
```

### CLI flags

| Flag        | Short      | Description                            | Default                 |
|-------------|------------|----------------------------------------|-------------------------|
| `--file`    | `-f`       | Intent file(s) to process              | `examples/default.yaml` |
| `--output`  | `-o`       | Output directory for generated configs | `output`                |
| `--verbose` | `-v`       | Enable debug-level logging             | `false`                 |
| `--quiet`   | `-q`       | Only show warnings and errors          | `false`                 |
| `--dry-run` | `-n`       | Run without writing anything to disk   | `false`                 |
| `--strict`  | `-s`       | Treat warnings as errors               | `false`                 |
| `--version` | None       | Print ASCII art and version and exit   | `false`                 |
| `--help`    | `-h`, `-?` | Print help section and exit            | `false`                 |
| `--debug`   | `-d`       | Print debug graph                      | `false`                 |

## Intent file format

Intent files describe your network as a hierarchy of **autonomous systems**, **routers**, and **interfaces**.
Currently, only **YAML** is supporter.
Here is a minimal example with two ASes peering over BGP:

```yaml
111:
  loopback_space_v4: "10.10.10.0/24"
  networks_space_v4: "192.168.1.0/24"
  loopback_space_v6: "2001:1:1:1::/64"
  networks_space_v6: "2001:db8:1:1::/64"
  brand: "Cisco"
  routers:
    R1:
      interfaces:
        GigabitEthernet1/0:
          neighbour: "112:R2"   # Cross-AS neighbour (AS 112, router R2)
          bgp: "peer"
        GigabitEthernet2/0:
          neighbour: "R3"       # Same-AS neighbour

    R3:
      interfaces:
        GigabitEthernet1/0:
          neighbour: "R1"

112:
  routers:
    R2:
      external: true
      interfaces:
        GigabitEthernet1/0:
          neighbour: "111:R1"
          bgp: "peer"
```

See the [wiki](https://github.com/Tuasco/router-quack/wiki/Documentation_YAML_TOC) for a comprehensive documentation
of the YAML intent file syntax.

Also see the [`examples/`](examples) directory for more, including a
[multi-file setup](examples/ManyFiles) that splits each AS into its own file.

## Contributing

Contributions are welcome! Please read the [contributing guide](CONTRIBUTING.md) before opening a pull request.

## Licence

This project is licensed under the [GPL-3.0 Licence](LICENCE).

##
