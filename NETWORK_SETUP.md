# 🌐 Installation AutoFact sur plusieurs PC - Configuration Réseau

Ce document explique comment configurer AutoFact sur **plusieurs PC clients** qui partagent la même **VM MariaDB centralisée**.

---

## 📋 Architecture

```
PC Client 1 ──┐
              ├──── Réseau Virtuel ──── VM Debian (MariaDB)
PC Client 2 ──┘                         IP: 192.168.56.200
```

---

## 🔧 Configuration pour chaque PC Client

### **Étape 1 : Clone le repository**

```bash
git clone https://github.com/lenyLBG/AutoFact.git
cd AutoFact
```

### **Étape 2 : Configurer la chaîne de connexion**

Ouvrez **`AutoFact/appsettings.json`** et modifiez l'IP pour qu'elle pointe vers **VOTRE VM** :

```json
{
  "ConnectionStrings": {
    "Default": "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact"
  }
}
```

**⚠️ IMPORTANT :** Remplacez `192.168.56.200` par l'IP **réelle** de votre VM !

**Comment trouver l'IP de votre VM :**
```bash
# Sur la VM Debian, exécutez :
ip addr show

# Cherchez une adresse comme : inet 192.168.56.X/24
```

### **Étape 3 : Vérifier la connectivité réseau**

**Sur le PC Client, ouvrez PowerShell et testez :**

```powershell
# Tester le ping vers la VM
ping 192.168.56.200

# Tester si le port MariaDB (3306) est accessible
Test-NetConnection -ComputerName 192.168.56.200 -Port 3306

# Le résultat doit afficher : TcpTestSucceeded : True
```

Si ça échoue :
- ✅ VM est-elle allumée ?
- ✅ Réseau VirtualBox est-il bien configuré (Bridge ou Host-Only) ?
- ✅ Firewall de la VM bloque-t-il le port 3306 ?

### **Étape 4 : Lancer l'application**

```bash
# Dans Visual Studio, appuyez sur F5
# Ou via la ligne de commande :
dotnet run
```

---

## 🔐 Configuration côté VM MariaDB

### Créer l'utilisateur pour chaque PC Client

**Sur la VM MariaDB**, il faut créer l'utilisateur `app` pour **chaque PC Client** avec son IP unique :

```bash
sudo mariadb
```

```sql
-- Exemple pour PC Client 1 (IP: 192.168.56.1)
CREATE USER 'app'@'192.168.56.1' IDENTIFIED BY 'Demaindeslaube';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'192.168.56.1';

-- Exemple pour PC Client 2 (IP: 192.168.56.2)
CREATE USER 'app'@'192.168.56.2' IDENTIFIED BY 'Demaindeslaube';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'192.168.56.2';

-- Appliquer les changements
FLUSH PRIVILEGES;

-- Vérifier les utilisateurs créés
SELECT User, Host FROM mysql.user WHERE User = 'app';
exit
```

---

## ✅ Test d'inscription

Une fois l'application lancée :

1. Cliquez sur **"S'inscrire"**
2. Entrez un email et un mot de passe
3. Cliquez **"S'inscrire"** à nouveau
4. Vous devriez voir : **"Inscription réussie"**

**Si ça ne fonctionne pas :** Consultez [TROUBLESHOOTING_INSCRIPTION.md](../TROUBLESHOOTING_INSCRIPTION.md)

---

## 🐛 Diagnostic rapide

Exécutez ce script SQL sur la VM pour vérifier :

```sql
USE AutoFact;

-- Vérifier la table utilisateur
DESCRIBE utilisateur;

-- Afficher les utilisateurs créés
SELECT id, email, date_inscription, actif FROM utilisateur;

-- Vérifier les permissions de l'app user
SHOW GRANTS FOR 'app'@'192.168.56.1';
```

---

## 📚 Fichiers utiles

- **appsettings.json** → Chaîne de connexion
- **sql/init.sql** → Schéma de la base (auto-créé au démarrage)
- **sql/test_inscription.sql** → Script de test rapide
- **TROUBLESHOOTING_INSCRIPTION.md** → Guide de dépannage complet

---

## 💡 Bonnes pratiques

✅ **À faire :**
- Tester la connexion réseau avant de lancer l'app
- Vérifier que l'IP dans `appsettings.json` est correcte
- S'assurer que MariaDB accepte les connexions distantes

❌ **À ne pas faire :**
- Ne pas utiliser `localhost` ou `127.0.0.1` (c'est la machine locale, pas la VM)
- Ne pas oublier de créer l'utilisateur `app` sur la VM
- Ne pas utiliser le même compte `app` pour tous les PC (créer des comptes par IP)

---

## 🆘 Besoin d'aide ?

Si vous rencontrez des problèmes :
1. Consultez [TROUBLESHOOTING_INSCRIPTION.md](../TROUBLESHOOTING_INSCRIPTION.md)
2. Vérifiez les logs de la console Visual Studio (F5)
3. Exécutez `sql/test_inscription.sql` sur la VM pour diagnostiquer
