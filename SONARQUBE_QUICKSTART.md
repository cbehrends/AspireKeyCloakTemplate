# 🚀 SonarQube Cloud Setup - Quick Start Card

Print this or keep it bookmarked!

---

## Setup in 5 Steps (30 minutes total)

### 1️⃣ Create SonarQube Cloud Project
```
1. Visit: https://sonarcloud.io
2. Sign in with GitHub
3. Click: "Analyze new project"
4. Select your repository
5. Note your:
   - Organization Key (e.g., "your-org")
   - Project Key (e.g., "AspireKeyCloakTemplate")
```

### 2️⃣ Generate Token
```
In SonarQube Cloud:
1. Click your profile icon (top right)
2. Go to: Account → Security
3. Click: "Generate Tokens"
4. Enter name: "GitHub Actions"
5. Copy the token immediately!
```

### 3️⃣ Add GitHub Secrets
```
In GitHub repository settings:
1. Go to: Settings → Secrets and variables → Actions
2. Click: "New repository secret"
3. Add these 4 secrets:

   Name: SONAR_TOKEN
   Value: [paste from step 2]

   Name: SONAR_ORGANIZATION
   Value: [your org key]

   Name: SONAR_PROJECT_KEY
   Value: [your project key]

   Name: SONAR_HOST_URL
   Value: https://sonarcloud.io
```

### 4️⃣ Update Configuration
```
Edit: sonar-project.properties

Line 8: sonar.organization=your-sonarqube-organization
        (change to your actual org key)
```

### 5️⃣ Push & Go!
```bash
git add .
git commit -m "Add SonarQube Cloud code coverage"
git push origin main
```

Watch GitHub Actions run. Check SonarQube Cloud dashboard in 2-3 minutes!

---

## 🧪 Test Locally First (Optional)

### macOS/Linux
```bash
./scripts/test-with-coverage.sh
```

### Windows
```powershell
.\scripts\test-with-coverage.ps1
```

---

## 📍 Key Files

| File | What It Does |
|------|--------------|
| `sonar-project.properties` | SonarQube configuration |
| `.runsettings` | Coverage collection settings |
| `.github/workflows/sonarqube-cloud.yml` | Automated CI/CD |
| `scripts/test-with-coverage.*` | Local testing |

---

## 📚 Need More Info?

- **Quick answers**: `CODE_COVERAGE_QUICKREF.md`
- **Step-by-step**: `SONARQUBE_SETUP.md`
- **Visual guide**: `SONARQUBE_ARCHITECTURE.md`
- **Troubleshooting**: See `SONARQUBE_SETUP.md` "Troubleshooting" section

---

## ✅ Checklist

- [ ] SonarQube Cloud account created
- [ ] Project created and keys noted
- [ ] Token generated
- [ ] 4 GitHub Secrets added
- [ ] `sonar-project.properties` updated
- [ ] Changes committed and pushed
- [ ] GitHub Actions workflow running
- [ ] Results visible on SonarQube Cloud

---

## 🎯 Expected Results

After the workflow completes (2-5 minutes):

✅ SonarQube Cloud dashboard shows coverage metrics  
✅ GitHub Actions workflow shows green checkmark  
✅ PR comments show quality gate status  
✅ Code coverage trends tracked over time  

---

**That's it! You're done! 🎉**

Questions? Check the documentation files listed above.

