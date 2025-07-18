$branch = git rev-parse --abbrev-ref HEAD
Set-Content -Path $args[0] -Value $branch
