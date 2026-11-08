# Publish Sigil Survival on GitHub Pages

This project is currently a Unity source project. GitHub Pages needs a **Web**
player build; it cannot run the Unity Editor project directly. The repository is
`DontBeknown/sigil-survival`, so its default Pages address will be
`https://dontbeknown.github.io/sigil-survival/`. The code namespaces can stay
as they are. The repository is private; [GitHub Pages availability](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-github-pages-site)
requires a public repository on GitHub Free or an eligible plan for private
repository Pages.

## 1. Make a Web build

1. In Unity Hub, add the **Web Build Support** module for Unity **6000.3.15f1**
   if it is missing. Open this project in that Editor version.
2. Open **File > Build Profiles**. Add a **Web** profile, then switch to it.
   Confirm `Assets/Scene/Initialize.unity` is enabled and first in the scene list.
3. Open **Edit > Project Settings > Player > Web > Publishing Settings** and set
   **Compression Format** to **Disabled**. GitHub Pages does not let this project
   configure the response headers required by Unity's compressed build files.
   Disabled compression makes the first release simpler, at the cost of a larger
   download. Leave **Development Build** off.
4. Build into a new `PagesBuild` folder at the repository root. The result must
   contain `PagesBuild/index.html`, `PagesBuild/Build/`, and
   `PagesBuild/TemplateData/`. The build folder is distinct from this repository's
   existing `docs` folder, which holds source documentation.

Unity 6's [Build Profiles guide](https://docs.unity3d.com/6000.0/Documentation/Manual/create-build-profile.html)
describes profile setup. Unity's [Web deployment guide](https://docs.unity3d.com/Manual/webgl-deploying.html)
explains compression and server headers.

## 2. Add a Pages workflow

Create `.github/workflows/pages.yml` with this content:

```yaml
name: Deploy game to Pages

on:
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: github-pages
  cancel-in-progress: true

jobs:
  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/configure-pages@v5
      - uses: actions/upload-pages-artifact@v4
        with:
          path: PagesBuild
      - id: deployment
        uses: actions/deploy-pages@v4
```

This workflow publishes only when run manually from the Actions tab. The
`PagesBuild` directory must be committed because GitHub's runner checks out the
repository before uploading it. Check GitHub's per-file and repository size
limits if the Web build is large.

## 3. Enable and publish

1. Commit and push `PagesBuild` and `.github/workflows/pages.yml` to the
   repository's default branch. Add those paths specifically so unrelated local
   Unity changes are not included accidentally.
2. In the GitHub repository, open **Settings > Pages** and set **Build and
   deployment > Source** to **GitHub Actions**.
3. Open **Actions > Deploy game to Pages > Run workflow** on the default branch.
   Wait for the deployment job, then open the URL shown in **Settings > Pages**.
4. On a desktop browser, check that the title screen loads, the game accepts
   keyboard and mouse input, and a run can start. Test a second load to check
   browser save behavior.

For later releases, rebuild into `PagesBuild`, commit the changed build files,
push, and run the workflow again. GitHub's [Pages workflow documentation](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)
explains the deployment actions and permissions.

## If it does not load

- **404:** Check that `PagesBuild/index.html` was committed at the top of the
  uploaded folder and that Pages is set to **GitHub Actions**.
- **Compression or `.wasm` error:** Rebuild with **Compression Format: Disabled**
  and commit the entire fresh build, including its generated `index.html`.
- **Missing assets under `/sigil-survival/`:** Publish Unity's generated build folder
  intact. Do not move only `index.html`; its relative `Build/` and
  `TemplateData/` paths must stay beside it.

The Web build and deployment have not been run for this project yet.
