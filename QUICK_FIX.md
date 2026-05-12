# ⚡ Quick Fix Checklist - L'inscription ne fonctionne pas

## 🔍 D'abord, identifiez le problème exact

**Quand vous cliquez sur "S'inscrire", qu'est-ce qui se passe ?**

- [ ] **Erreur : "Identifiant et mot de passe requis"** → Les champs sont vides, remplissez-les
- [ ] **Erreur : "Un compte avec cet identifiant existe déjà"** → Utilisez un autre email
- [ ] **Erreur réseau / timeout** → Voir **Section A** ci-dessous
- [ ] **Erreur SQL / base de données** → Voir **Section B**
- [ ] **Rien ne se passe (app freeze)** → Voir **Section C**

---

## **Section A : Erreur de connexion réseau**

### ✅ Vérification rapide

```powershell
# 1. Vérifier que la VM répond
ping 192.168.56.200

# 2. Vérifier le port MariaDB
Test-NetConnection -ComputerName 192.168.56.200 -Port 3306
```

### 🔧 Corrections

**Si le ping échoue :**
- ❌ VM est arrêtée → Redémarrez-la
- ❌ IP est mauvaise → Trouvez l'IP correcte avec `ip addr show` sur la VM
- ❌ Réseau VirtualBox mal configuré → Configurez-le en Bridge ou Host-Only

**Si le port 3306 échoue :**
```bash
# Sur la VM, vérifiez que MariaDB écoute sur 0.0.0.0 (pas localhost)
sudo netstat -tuln | grep 3306

# Devrait afficher quelque chose comme : 0.0.0.0:3306
```

### 🆘 Solution finale

**Modifiez `appsettings.json` :**
```json
{
  "ConnectionStrings": {
    "Default": "server=<IP_CORRECTE_DE_VOTRE_VM>;user id=app;password=Demaindeslaube;database=AutoFact"
  }
}
```

---

## **Section B : Erreur SQL / Base de données**

### ✅ Vérification rapide

**Sur la VM MariaDB :**
```bash
sudo mariadb
```

```sql
USE AutoFact;

-- 1. La table existe-t-elle ?
SHOW TABLES;

-- 2. La table utilisateur a-t-elle les bonnes colonnes ?
DESCRIBE utilisateur;

-- 3. L'utilisateur 'app' existe-t-il ?
SELECT User, Host FROM mysql.user WHERE User = 'app';

-- 4. A-t-il les permissions ?
SHOW GRANTS FOR 'app'@'192.168.56.1';
```

### 🔧 Corrections

**Si la table n'existe pas :**
```bash
# Réinitialiser la base
sudo mysql -p AutoFact < /chemin/vers/init.sql

# Ou créer manuellement
sudo mariadb
```

```sql
CREATE TABLE IF NOT EXISTS utilisateur (
    id INT PRIMARY KEY AUTO_INCREMENT,
    email VARCHAR(255) UNIQUE NOT NULL,
    mot_de_passe VARCHAR(255) NOT NULL,
    date_inscription DATE,
    actif BOOLEAN DEFAULT 1
);
exit
```

**Si l'utilisateur 'app' n'existe pas :**
```bash
sudo mariadb
```

```sql
CREATE USER 'app'@'192.168.56.1' IDENTIFIED BY 'Demaindeslaube';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'192.168.56.1';
FLUSH PRIVILEGES;
exit
```

---

## **Section C : App freeze / Ne répond pas**

### ✅ Causes possibles

1. **La requête INSERT prend trop de temps** → Augmentez le timeout dans Program.cs
2. **La connexion est bloquée** → Vérifiez le firewall
3. **Deadlock dans la base** → Redémarrez MariaDB

### 🔧 Quick fixes

**Redémarrer MariaDB sur la VM :**
```bash
sudo systemctl restart mariadb
```

**Augmenter le timeout dans Visual Studio** (si ça continue) :
Allez dans `bdd.cs` et modifiez :
```csharp
var conn = new MySqlConnection(_connectionString);
conn.ConnectionTimeout = 30;  // Augmentez-le si besoin
```

---

## 🚀 Après avoir appliqué un fix

**Testez chacune des étapes dans cet ordre :**

1. [ ] Relancez l'application (F5 dans Visual Studio)
2. [ ] Allez dans l'écran d'inscription
3. [ ] Entrez un nouvel email (ex: `test999@example.com`)
4. [ ] Entrez un mot de passe
5. [ ] Cliquez **"S'inscrire"**
6. [ ] Vérifiez le message de succès ou l'erreur exacte

---

## 📝 Collecte d'infos pour du support

Si rien ne fonctionne, partagez ceci :

**A. Screenshot ou message d'erreur exact**

**B. Résultat de ces commandes :**

```powershell
# Windows PowerShell
ping 192.168.56.200
Test-NetConnection -ComputerName 192.168.56.200 -Port 3306
```

**C. Résultat de ce script SQL (copié sur la VM) :**

```sql
USE AutoFact;
SELECT COUNT(*) as `Tables` FROM information_schema.tables WHERE table_schema='AutoFact';
DESCRIBE utilisateur;
SELECT User, Host FROM mysql.user WHERE User = 'app';
```

**D. Le contenu de `appsettings.json`** (masquez le mot de passe si public)

---

## ✨ Tout fonctionne maintenant ?

Bravo ! 🎉

Vous pouvez maintenant :
- ✅ Vous inscrire avec un nouvel email
- ✅ Vous connecter avec vos identifiants
- ✅ Utiliser l'application normalement

**Conseil :** Une fois qu'un utilisateur s'est inscrit, les autres utilisateurs peuvent aussi s'inscrire sur le même PC ou d'autres PC !
