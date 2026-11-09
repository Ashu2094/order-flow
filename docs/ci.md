# Continuous integration

**What triggers it.** Every push to any branch, plus a manual Run button (`workflow_dispatch`). The workflow is in `.github/workflows/ci.yml`.

**What it does.** It runs on a fresh Ubuntu machine that already has Docker, which the Testcontainers tests need:

| Step | Command | Protects you from |
|---|---|---|
| Check out | `actions/checkout` | |
| Install .NET 10 | `actions/setup-dotnet` with `10.0.x` | |
| Restore | `dotnet restore` | missing packages or wrong versions |
| Build | `dotnet build --no-restore --configuration Release` | code that does not compile |
| Test | `dotnet test --no-build --configuration Release` | code that compiles but behaves wrongly |

Because the machine starts from nothing, CI also catches "works on my machine" problems. Examples are a file that was never committed, or a file name whose capitals differ from the code (Windows ignores that, Linux does not).

**How to read a failed run.**

1. Open the Actions tab and click the red run.
2. Click the job, then the step with the red cross.
3. Read the first error line, not the last. For a failing test, find the test name, `Expected` and `Actual`, and the file and line.
4. If Build is green and Test is red, the code compiled but behaves differently from what a test expects.

The badge at the top of the README shows the status of the latest run on the default branch.

**Later.** Session 86 extends this pipeline to build Docker images and push them to GitHub Container Registry (the CD half).
