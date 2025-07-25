#! /usr/bin/env pwsh -c
$projects = (
    "array-test",
    "functions-test",
    "observe-test",
    "test-test"
)
foreach ($project in $projects) {
    $project_name = $project + ".uhighproj"
$command = "uhigh run $project\\$project_name"
    Write-Host "Running $command"
    Invoke-Expression $command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error running $project_name"
        exit $LASTEXITCODE
    }
}