# Dreamine.PLC.Core

This package provides the common PLC runtime layer built on top of Dreamine.PLC.Abstractions.

## Purpose

`Dreamine.PLC.Core` is part of the Dreamine PLC package family.

The package is designed to keep PLC communication code separated by responsibility:

- Abstractions define contracts.
- Core provides shared runtime infrastructure.
- Vendor adapters implement device-specific communication.
- WPF provides monitoring and diagnostic UI components.

## Features

- PLC channel runtime foundation
- Common request dispatching structure
- Polling and monitoring service boundaries
- Shared validation and conversion utilities
- Base infrastructure for vendor-specific PLC adapters


## Project References

- `Dreamine.PLC.Abstractions`

## Target Framework

```xml
<TargetFramework>net8.0</TargetFramework>
```

## Package Metadata

| Item | Value |
|---|---|
| PackageId | `Dreamine.PLC.Core` |
| Version | `1.0.0` |
| License | `MIT` |
| Repository | `https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Core` |
| Project URL | `https://github.com/CodeMaru-Dreamine/Dreamine.PLC.FullKit` |

## Architecture Rule

This repository must not reference application-level projects.

Dependency direction must remain one-way:

```text
Abstractions
    ▲
    │
Core
    ▲
    │
Vendor Adapter / WPF UI Component
```

## License

This project is licensed under the MIT License.
