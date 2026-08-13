# Git Workflow

Use this flow because `bms-system-main` is protected.

```powershell
cd D:\CODING\ARYAMAN\AryamanBMS\AryamanBMS

git checkout bms-system-main
git pull origin bms-system-main

git checkout -b feature/short-change-name
```

Make your changes, then verify and commit:

```powershell
git status
git diff --stat
dotnet build AryamanBMS.slnx --no-restore

git add .
git commit -m "Describe the change"
git push origin feature/short-change-name
```

Open a Pull Request on GitHub:

```text
From: feature/short-change-name
Into: bms-system-main
```

After checks pass, click **Merge pull request**.

Then update local main:

```powershell
git checkout bms-system-main
git pull origin bms-system-main
git branch -d feature/short-change-name
```

