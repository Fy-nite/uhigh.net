# project overview

This project is a Custom programming language called Uhigh (micro-high) written in Csharp.

# notes
- try to use the `Dev` or `dev` branch for development, this is the main branch for development and will be merged into `stable` when ready.
if you can't find dev, run `git pull` to update your local repository and then try running `git checkout dev` to switch to the dev branch.
- if you are working on a feature, try to create a new branch from `dev` then merge it back into `dev` when you are done.
- try to write unit tests for any new features for the `Tests/` folder and make sure to run `dotnet run test` before pushing changes to see if the compiler builds and has any failing tests.


  ## coding standards
  - attempt to document the code with doc comments
  - comments can be detailed or not
  - if optimisations can be done in a part of the codebase, you can optimise but you have to check if any functionality broke in the process.
 

  # documenting
  - if new language features are added, make sure to create patch notes in the `docs/` directory for these and mark it with a unique identifier.
  - if you are adding a new feature, make sure to document it in the `docs/` directory.
  - if you are fixing a bug, make sure to document it in the `docs/` directory if you find what caused the bug and how you fixed it.