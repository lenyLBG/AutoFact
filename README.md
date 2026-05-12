
# AutoFact

## Documentation d'installation

## Description

AutoFact est un outils de gestion de facturation pour les auto-entrepreneurs.

## Prérequis

* **Visual Studio 2022 (ou 2026)**
* **Machine virtuelle Debian** avec :
    * **MariaDB**

## installation

### 1. Machine physique

```bash
git clone https://github.com/lenyLBG/AutoFact.git
```

#### Visual Studio 2022/2026

Appuyer sur *"Ouvrir un dossier local"* puis selectionner le dossier cloné.

### 2. Machine virtuelle 

Dans le code récupérer sur GitHub, dans le dossier `AutoFact/AutoFact/BDD` récupérer le fichier `Autofact.sql` puis transférer le sur votre machine virtuelle avec Filezilla ou un autre outil.

Une fois sur la machine virtuelle executer la commande 
```SQL
sudo mariadb
```

Puis
```SQL
CREATE DATABASE AutoFact;
exit
```
Ensuite
```bash
sudo mysql -p AutoFact < AutoFact.sql
```
Puis retourner dans la base de données
```bash
sudo mariadb
```
```SQL
USE AutoFact;

-- Création de l'utilisateur pour l'application

CREATE USER 'app'@'192.168.56.1' IDENTIFIED BY 'Un_M0t_d3_p@sse_trés_sécurisé';
GRANT ALL PRIVILEGES ON AutoFact.* TO 'app'@'192.168.56.1';
FLUSH PRIVILEGES;
```
Creation de l'utilisateur
```SQL
-- 1. Insertion de l'utilisateur administrateur
INSERT INTO `utilisateur` (`email`, `mot_de_passe`, `date_inscription`, `actif`) 
VALUES ('admin@autofact.fr', 'hashed_password_1', '2024-01-15', 1);
```
```SQL
-- 2. Insertion des clients
INSERT INTO `client` (`nom`, `adresse`, `mail`, `telephone`) VALUES
('Entreprise TechSoft', '123 Rue de la Paix, 75000 Paris', 'contact@techsoft.fr', '01 23 45 67 89'),
('Société Services Plus', '456 Avenue de la République, 69000 Lyon', 'hello@servicesplus.fr', '04 12 34 56 78'),
('Cabinet Conseil Pro', '789 Boulevard Central, 13000 Marseille', 'info@cabinetconseil.fr', '04 91 23 45 67'),
('Distribution Générale', '321 Rue Commerciale, 59000 Lille', 'ventes@distribution-gen.fr', '03 20 45 67 89'),
('Atelier Création', '654 Rue de l''Art, 31000 Toulouse', 'contact@ateliercreat.fr', '05 61 12 34 56');

-- 3. Catalogue de prestations
INSERT INTO `prestation` (`nom`, `type`, `prixUnitaire`) VALUES
('Consulting Technique', 'Service', 150.00),
('Développement Logiciel', 'Service', 200.00),
('Support Client', 'Service', 75.00),
('Formation Utilisateur', 'Service', 100.00),
('Audit Système', 'Service', 180.00),
('Maintenance Annuelle', 'Maintenance', 500.00),
('Hébergement Web', 'Infrastructure', 50.00),
('Licence Logiciel', 'Logiciel', 250.00);

-- 4. Codes Promo
INSERT INTO `code_promo` (`code`, `taux_remise`, `date_expiration`, `actif`) VALUES
('PROMO2024', 10.00, '2024-12-31', 1),
('VIP15', 15.00, '2025-06-30', 1),
('NOEL20', 20.00, '2024-12-25', 0),
('PRINTEMPS', 5.00, '2024-05-31', 1);

-- 5. Documents de Facturation
INSERT INTO `document_facturation` (`numero`, `type`, `statut`, `date_emission`, `date_echeance`, `client_id`, `sequence_index`) VALUES
('DEV-2024-001', 'Devis', 'Accepté', '2024-05-01', '2024-06-01', 1, 1),
('DEV-2024-002', 'Devis', 'En attente', '2024-05-05', '2024-06-05', 2, 1),
('FAC-2024-001', 'Facture', 'Payée', '2024-05-10', '2024-06-10', 1, 2),
('FAC-2024-002', 'Facture', 'En attente', '2024-05-15', '2024-06-15', 3, 1),
('FAC-2024-003', 'Facture', 'Payée', '2024-05-20', '2024-06-20', 2, 2),
('AV-2024-001', 'Avoir', 'Appliqué', '2024-05-25', NULL, 1, 3);

-- 6. Lignes de Document
INSERT INTO `ligne_document` (`document_id`, `prestation_id`, `designation`, `quantite`, `prix_unitaire`, `taux_remise`) VALUES
(1, 1, 'Consulting Technique - 40 heures', 40, 150.00, 0.00),
(1, 2, 'Développement Logiciel - 60 heures', 60, 200.00, 10.00),
(2, 3, 'Support Client - 20 heures', 20, 75.00, 0.00),
(3, 1, 'Consulting Technique - 30 heures', 30, 150.00, 0.00),
(3, 6, 'Maintenance Annuelle', 1, 500.00, 0.00),
(4, 2, 'Développement Logiciel - 50 heures', 50, 200.00, 15.00),
(4, 4, 'Formation Utilisateur - 10 heures', 10, 100.00, 0.00),
(5, 5, 'Audit Système', 1, 180.00, 5.00),
(5, 7, 'Hébergement Web - 6 mois', 6, 50.00, 0.00),
(6, 1, 'Ajustement Consulting - 5 heures', 5, 150.00, 0.00);

-- 7. Déclarations
-- Correction : uniformisation du nom de la colonne client et retrait des quotes sur les montants
INSERT INTO `declaration` (`periode`, `caCumule`, `montantPostPrelevement`, `client_id`) VALUES
('2024-Q1', 50000.00, 45000.00, 1),
('2024-Q1', 35000.00, 31500.00, 2),
('2024-Q2', 75000.00, 67500.00, 1),
('2024-Q2', 42000.00, 37800.00, 3),
('2024-Q2', 28000.00, 25200.00, 4);
```
## Lancement

Dans Visual Studio, assurez-vous que la chaîne de connexion pointe vers l'IP de votre VM.

Dans

```Chemin
\AutoFact\appsettings.json
```    

Compilez et lancez le projet (F5).

