-- Initialisation de la base AutoFact (MySQL / MariaDB)

CREATE DATABASE IF NOT EXISTS `AutoFact` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
GO
USE `AutoFact`;
GO

-- Utilisateur
CREATE TABLE IF NOT EXISTS `utilisateur` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `email` VARCHAR(200) NOT NULL UNIQUE,
  `mot_de_passe` VARCHAR(255),
  `date_inscription` DATE,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
GO

-- Client
CREATE TABLE IF NOT EXISTS `client` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(50),
  `adresse` VARCHAR(260),
  `mail` VARCHAR(50),
  `telephone` VARCHAR(50),
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
GO

-- Prestation (catalogue)
CREATE TABLE IF NOT EXISTS `prestation` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nom` VARCHAR(50),
  `type` VARCHAR(450),
  `prixUnitaire` DECIMAL(15,2),
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
GO

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
GO

-- Code Promo
CREATE TABLE IF NOT EXISTS `code_promo` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(50) NOT NULL UNIQUE,
  `taux_remise` DECIMAL(5,2) NOT NULL DEFAULT 0,
  `date_expiration` DATE,
  `actif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
GO

-- Document de Facturation (Devis, Factures, Avoirs)
CREATE TABLE IF NOT EXISTS `document_facturation` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `numero` VARCHAR(50) NOT NULL UNIQUE,
  `type` VARCHAR(50) NOT NULL,
  `statut` VARCHAR(50) NOT NULL,
  `date_emission` DATE NOT NULL,
  `date_echeance` DATE,
  `client_id` INT NOT NULL,
  `numero_document_parent` VARCHAR(50),
  `sequence_index` INT NOT NULL,
  PRIMARY KEY(`id`),
  FOREIGN KEY(`client_id`) REFERENCES `client`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
GO

-- Ligne de Document
CREATE TABLE IF NOT EXISTS `ligne_document` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `document_id` INT NOT NULL,
  `prestation_id` INT,
  `designation` VARCHAR(255) NOT NULL,
  `quantite` INT NOT NULL DEFAULT 1,
  `prix_unitaire` DECIMAL(15,2) NOT NULL,
  `code_promo_id` INT,
  `taux_remise` DECIMAL(5,2) NOT NULL DEFAULT 0,
  PRIMARY KEY(`id`),
  FOREIGN KEY(`document_id`) REFERENCES `document_facturation`(`id`) ON DELETE CASCADE,
  FOREIGN KEY(`prestation_id`) REFERENCES `prestation`(`id`),
  FOREIGN KEY(`code_promo_id`) REFERENCES `code_promo`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
GO

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
GO

