# 🔧 Guide de Dépannage - Inscription non fonctionnelle

## ❌ Problème
L'inscription ne fonctionne pas sur un autre PC en suivant le README.md

---

## ✅ Solutions

### **1. Vérifier la chaîne de connexion**

**Problème :** L'IP `192.168.56.200` dans `appsettings.json` est spécifique à votre VM.

**Solution :**
Modifiez `appsettings.json` sur l'autre PC avec l'IP **correcte de votre VM** :

```json
{
  "ConnectionStrings": {
    "Default": "server=<IP_DE_VOTRE_VM>;user id=app;password=Demaindeslaube;database=AutoFact"
  }
}
```

**Comment trouver l'IP de la VM ?**
Sur la machine virtuelle Debian, exécutez :
```bash
ip addr show
```
Cherchez une adresse ressemblant à `192.168.56.X`

---

### **2. Vérifier les permissions de l'utilisateur MariaDB**

L'utilisateur `app` doit avoir les bonnes permissions.

**Sur la VM MariaDB, exécutez :**
```bash
sudo mariadb
```

```sql
-- Vérifier les droits existants
SHOW GRANTS FOR 'app'@'192.168.56.1';

-- Si besoin, accorder tous les droits (remplacer 192.168.56.1 par l'IP du PC client)
CREATE USER 'app'@'192.168.56.1' IDENTIFIED BY 'Demaindeslaube';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'192.168.56.1';
FLUSH PRIVILEGES;

-- Vérifier que c'est OK
SHOW GRANTS FOR 'app'@'192.168.56.1';
exit
```

---

### **3. Vérifier que le schéma de base est correct**

**Assurez-vous que la table `utilisateur` existe et a les bonnes colonnes :**

```bash
sudo mariadb
```

```sql
USE AutoFact;

-- Vérifier la structure de la table utilisateur
DESCRIBE utilisateur;

-- Doit afficher :
-- id                | INT PRIMARY KEY AUTO_INCREMENT
-- email             | VARCHAR(255) UNIQUE NOT NULL
-- mot_de_passe      | VARCHAR(255) NOT NULL
-- date_inscription  | DATE
-- actif             | BOOLEAN DEFAULT 1
```

---

### **4. Vérifier la connectivité réseau**

**Sur le PC client, testez la connexion à la VM :**

```powershell
# Depuis PowerShell sur Windows
ping 192.168.56.200

# Ou tester le port MariaDB (3306)
Test-NetConnection -ComputerName 192.168.56.200 -Port 3306
```

Si ça fail, vérifiez :
- La VM est bien allumée
- Réseau VirtualBox configuré en Bridge ou Host-Only
- Firewall de la VM n'est pas bloquant

---

### **5. Vérifier qu'InitializeDatabaseAsync() a bien exécuté init.sql**

**Dans Visual Studio, F5 pour lancer l'app.**

Au démarrage, l'application doit exécuter automatiquement le script `init.sql` qui crée le schéma.

**Dans les logs (si vous avez ajouté du logging), cherchez :**
```
"Base de données initialisée avec succès"
ou
"CreateTableIfNotExists"
```

---

### **6. Forcer la réinitialisation du schéma**

Si le schéma est corrompu, supprimez et recréez manuellement.

**Sur la VM :**
```bash
sudo mariadb
```

```sql
DROP DATABASE AutoFact;
CREATE DATABASE AutoFact;
exit

-- Puis rechargez le schéma initial
sudo mysql -p AutoFact < /chemin/vers/init.sql

-- Recréez l'utilisateur
sudo mariadb
```

```sql
CREATE USER 'app'@'192.168.56.1' IDENTIFIED BY 'Demaindeslaube';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'192.168.56.1';
FLUSH PRIVILEGES;
exit
```

---

## 📝 Checklist de dépannage

- [ ] IP de la VM correcte dans `appsettings.json`
- [ ] Ping vers la VM réussit
- [ ] Port 3306 est accessible (Test-NetConnection)
- [ ] Utilisateur `app` existe sur MariaDB avec bonnes permissions
- [ ] Table `utilisateur` existe avec les bonnes colonnes
- [ ] Application se lance sans erreur de connexion (F5)
- [ ] Test inscription : email + mdp + cliquer "S'inscrire"

---

## 🚀 Si ça fonctionne

L'inscription devrait :
1. Vérifier que l'email n'existe pas
2. Hasher le mot de passe
3. L'insérer dans la base
4. Afficher "Inscription réussie"

Puis vous pouvez vous connecter avec vos identifiants.

---

## 💡 Alternative : Mode Debug

Si vous avez toujours des problèmes, ajoutez un MessageBox dans `BtnRegister_Click` pour voir l'erreur exacte :

```csharp
catch (Exception ex)
{
    MessageBox.Show(
        $"Erreur : {ex.GetType().Name}\n{ex.Message}\n{ex.InnerException?.Message}",
        "Debug", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

Cela affichera l'erreur SQL exacte (permission denied, table not found, etc.)
