-- Initialisation de la base AutoFact (MySQL / MariaDB)

CREATE DATABASE IF NOT EXISTS `AutoFact` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `AutoFact`;

-- Utilisateur
CREATE TABLE IF NOT EXISTS `utilisateur` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `email` VARCHAR(200) NOT NULL UNIQUE,
  `mot_de_passe` VARCHAR(255),
  `date_inscription` DATE,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Client
CREATE TABLE IF NOT EXISTS `client` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(50),
  `adresse` VARCHAR(260),
  `mail` VARCHAR(50),
  `telephone` VARCHAR(50),
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Prestation (catalogue)
CREATE TABLE IF NOT EXISTS `prestation` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(50),
  `type` VARCHAR(450),
  `prixUnitaire` DECIMAL(15,2),
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Facture (incluant devis et avoirs via type)
CREATE TABLE IF NOT EXISTS `facture` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `date_` DATE,
  `montantTotal` DECIMAL(20,2),
  `statut` VARCHAR(50),
  `id_client` INT NOT NULL,
  `type` VARCHAR(50) DEFAULT 'Facture',
  PRIMARY KEY(`id`),
  FOREIGN KEY(`id_client`) REFERENCES `client`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Devis (hérite de facture)
CREATE TABLE IF NOT EXISTS `devis` (
  `id` INT NOT NULL,
  PRIMARY KEY(`id`),
  FOREIGN KEY(`id`) REFERENCES `facture`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Ligne de Facture
CREATE TABLE IF NOT EXISTS `ligneFacture` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `quantite` INT,
  `prix` DECIMAL(15,2),
  `codePromo` VARCHAR(110),
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Avoir (hérite de facture)
CREATE TABLE IF NOT EXISTS `avoir` (
  `id` INT NOT NULL,
  PRIMARY KEY(`id`),
  FOREIGN KEY(`id`) REFERENCES `facture`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Declaration
CREATE TABLE IF NOT EXISTS `declaration` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `periode` VARCHAR(50),
  `caCumule` VARCHAR(50),
  `montantPostPrelevement` DECIMAL(15,2),
  `id_client` INT NOT NULL,
  PRIMARY KEY(`id`),
  FOREIGN KEY(`id_client`) REFERENCES `client`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Prestation_Facture
CREATE TABLE IF NOT EXISTS `prestation_facture` (
  `id_prestation` INT NOT NULL,
  `id_facture` INT NOT NULL,
  PRIMARY KEY(`id_prestation`, `id_facture`),
  FOREIGN KEY(`id_prestation`) REFERENCES `prestation`(`id`),
  FOREIGN KEY(`id_facture`) REFERENCES `facture`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- LigneFacture_Prestation
CREATE TABLE IF NOT EXISTS `ligneFacture_prestation` (
  `id_prestation` INT NOT NULL,
  `id_facture` INT NOT NULL,
  PRIMARY KEY(`id_prestation`, `id_facture`),
  FOREIGN KEY(`id_prestation`) REFERENCES `prestation`(`id`),
  FOREIGN KEY(`id_facture`) REFERENCES `ligneFacture`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
