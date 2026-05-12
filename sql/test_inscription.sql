-- ============================================================================
-- TEST RAPIDE - Vérifier que tout est configuré correctement pour l'inscription
-- ============================================================================

USE AutoFact;

-- 1. Vérifier que la table utilisateur existe et a les bonnes colonnes
DESCRIBE utilisateur;

-- 2. Vérifier qu'elle est vide au démarrage (ou afficher les users existants)
SELECT COUNT(*) as nombre_utilisateurs FROM utilisateur;
SELECT id, email, date_inscription, actif FROM utilisateur;

-- 3. Vérifier les permissions de l'utilisateur 'app'
-- (Exécutez ceci depuis root/admin)
SHOW GRANTS FOR 'app'@'192.168.56.1';

-- 4. Essayer de créer un utilisateur de test manuellement
-- (pour vérifier que c'est possible)
INSERT INTO utilisateur (email, mot_de_passe, date_inscription, actif) 
VALUES ('test@example.com', 'test_hash_password_12345', CURDATE(), 1);

-- Vérifier que ça a marché
SELECT * FROM utilisateur WHERE email = 'test@example.com';

-- Nettoyer le test
DELETE FROM utilisateur WHERE email = 'test@example.com';

-- ============================================================================
-- Si vous recevez une ERREUR à une des étapes ci-dessus, notez :
-- 1. Le numéro de la ligne
-- 2. Le message d'erreur exact
-- 3. Partagez-le pour qu'on puisse diagnostiquer
-- ============================================================================
