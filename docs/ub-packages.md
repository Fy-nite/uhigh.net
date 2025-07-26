# μHigh Package Support (.ub files)

This document describes the .ub package management system for μHigh projects.

## Overview

.ub packages provide a simple, source-based package distribution system for μHigh libraries and applications. Unlike traditional package managers that distribute compiled binaries, .ub packages contain the original source code, making them perfect for a compile-to-C# language ecosystem.

## Package Format

.ub files are standard ZIP archives containing:

1. **package.json** - JSON manifest with package metadata
2. **Source files** - All .uh source files from the project
3. **Directory structure** - Preserved from the original project

### Package Manifest (package.json)

```json
{
  "packageFormatVersion": "1.0",
  "name": "PackageName",
  "version": "1.0.0",
  "description": "Package description",
  "author": "Author name",
  "targetFramework": "net9.0",
  "outputType": "Library",
  "rootNamespace": "PackageName",
  "sourceFiles": ["main.uh", "utils.uh"],
  "ubDependencies": {
    "OtherUbPackage": "1.2.0"
  },
  "nugetDependencies": {
    "Newtonsoft.Json": "13.0.1"
  },
  "createdAt": "2025-07-23T11:07:56.969Z",
  "keywords": ["math", "utility"],
  "uhighVersion": "1.1.2"
}
```

## CLI Commands

### Package Creation

```bash
# Create a .ub package from a project
uhigh pack <project-file> [--output <package-file>]

# Example
uhigh pack MyLibrary.uhighproj --output MyLibrary.ub
```

### Package Extraction

```bash
# Extract a .ub package to a directory
uhigh unpack <package-file> [--output <directory>]

# Example
uhigh unpack MyLibrary.ub --output extracted/
```

### Package Installation

```bash
# Install a .ub package as a project dependency
uhigh install-ub-package <project-file> <package-file> [--cache-dir <directory>]

# Example
uhigh install-ub-package MyApp.uhighproj MyLibrary.ub
```

### Package Management

```bash
# List installed .ub packages in a project
uhigh list-ub-packages <project-file> [--verbose]

# Build directly from a .ub package
uhigh build-from-package <package-file> [--output <executable>]
```

## Dependency Management

### Installing Packages

When a .ub package is installed:

1. Package is extracted to `.ub-packages/<name>-<version>/`
2. Project file is updated with package reference
3. Package metadata is stored in project properties

### Project Integration

Installed packages are tracked in the project file:

```xml
<Properties>
  <Property Name="UbPackageReferences" 
            Value="MathLibrary:1.0.0:/path/to/.ub-packages/MathLibrary-1.0.0" 
            Category="Package" />
</Properties>
```

### Cache Structure

```
.ub-packages/
├── PackageA-1.0.0/
│   ├── package.json
│   └── source.uh
└── PackageB-2.1.0/
    ├── package.json
    ├── main.uh
    └── utils.uh
```

## Use Cases

### Library Distribution
Package and share utility libraries:
```bash
uhigh pack MathUtils.uhighproj --output MathUtils.ub
# Share MathUtils.ub with other developers
```

### Project Templates
Create reusable project structures:
```bash
uhigh pack WebTemplate.uhighproj --output WebTemplate.ub
uhigh unpack WebTemplate.ub --output MyNewProject/
```

### Community Packages
Enable ecosystem growth through easy package sharing and discovery.

## Best Practices

### Package Naming
- Use descriptive, unique names
- Follow semantic versioning (MAJOR.MINOR.PATCH)
- Include meaningful descriptions and author information

### Source Organization
- Keep source files well-organized
- Use clear namespaces
- Include comprehensive documentation

### Dependencies
- Minimize external dependencies when possible
- Clearly document any NuGet dependencies
- Consider dependency version compatibility

## Future Enhancements

Potential future features:
- Central package registry/repository
- Package signing and verification
- Automated dependency resolution
- Package update management
- Binary package support for complex dependencies

## Examples

See the `/tmp/ub-demo/` directory for a complete working example with the MathLibrary package.

---

This package system provides the foundation for a robust μHigh ecosystem while maintaining the simplicity and source-code focus that makes μHigh unique.