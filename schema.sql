-- ============================================
-- ÉTAPE 1 : Exécuter dans psql (en tant que postgres)
-- sudo -u postgres psql
-- ============================================

CREATE DATABASE ppe;
CREATE USER ppe WITH PASSWORD 'votre_mot_de_passe';
GRANT ALL PRIVILEGES ON DATABASE ppe TO ppe;

-- ============================================
-- CONFIGURATION : copier .env.example vers .env
-- et modifier PPE_DB_PASSWORD avec le mot de passe ci-dessus
-- ============================================

-- ============================================
-- ÉTAPE 2 : Se connecter à ppe puis exécuter le reste
-- sudo -u postgres psql -d ppe
-- OU taper \c ppe dans psql
-- ============================================

-- Table utilisateur avec authentification et infos personnelles
CREATE TABLE utilisateur (
    id UUID PRIMARY KEY,
    id_code SERIAL NOT NULL,
    login VARCHAR(50) UNIQUE NOT NULL,
    password VARCHAR(200) NOT NULL,
    admin BOOLEAN DEFAULT FALSE,
    nom VARCHAR(100),
    adresse VARCHAR(200),
    ville VARCHAR(100),
    code VARCHAR(10),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Droits sur la table et séquence
GRANT ALL PRIVILEGES ON TABLE utilisateur TO ppe;
GRANT USAGE, SELECT ON SEQUENCE utilisateur_id_code_seq TO ppe;

-- Fonction pour vérifier si un login existe déjà
CREATE OR REPLACE FUNCTION utilisateur_login_exists(p_login VARCHAR(50))
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS (SELECT 1 FROM utilisateur WHERE login = p_login);
END;
$$;

-- Procédure stockée de création de compte (retourne l'id_code auto-incrémenté)
CREATE OR REPLACE PROCEDURE utilisateur_add(
    p_id UUID,
    p_login VARCHAR(50),
    p_password VARCHAR(200),
    p_admin BOOLEAN,
    p_nom VARCHAR(100),
    p_adresse VARCHAR(200),
    p_ville VARCHAR(100),
    p_code VARCHAR(10),
    OUT p_id_code INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO utilisateur (id, login, password, admin, nom, adresse, ville, code)
    VALUES (p_id, p_login, p_password, p_admin, p_nom, p_adresse, p_ville, p_code)
    RETURNING id_code INTO p_id_code;
END;
$$;

-- Fonction pour récupérer un utilisateur par login (pour la connexion)
CREATE OR REPLACE FUNCTION utilisateur_get_by_login(p_login VARCHAR(50))
RETURNS TABLE (
    id UUID,
    id_code INTEGER,
    login VARCHAR(50),
    password VARCHAR(200),
    admin BOOLEAN,
    nom VARCHAR(100),
    adresse VARCHAR(200),
    ville VARCHAR(100),
    code VARCHAR(10)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT u.id, u.id_code, u.login, u.password, u.admin, u.nom, u.adresse, u.ville, u.code
                 FROM utilisateur u
                 WHERE u.login = p_login;
END;
$$;

-- Fonction de liste de tous les utilisateurs (pour l'admin)
DROP FUNCTION IF EXISTS utilisateur_list();
CREATE FUNCTION utilisateur_list()
RETURNS TABLE (
    id UUID,
    id_code INTEGER,
    login VARCHAR(50),
    password VARCHAR(200),
    admin BOOLEAN,
    nom VARCHAR(100),
    adresse VARCHAR(200),
    ville VARCHAR(100),
    code VARCHAR(10)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT u.id, u.id_code, u.login, u.password, u.admin, u.nom, u.adresse, u.ville, u.code FROM utilisateur u ORDER BY u.nom;
END;
$$;

-- Procédure de modification des infos d'un utilisateur
CREATE OR REPLACE PROCEDURE utilisateur_update(
    p_id UUID,
    p_nom VARCHAR(100),
    p_adresse VARCHAR(200),
    p_ville VARCHAR(100),
    p_code VARCHAR(10)
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE utilisateur SET nom = p_nom, adresse = p_adresse, ville = p_ville, code = p_code WHERE id = p_id;
END;
$$;

-- Procédure de suppression d'un utilisateur
CREATE OR REPLACE PROCEDURE utilisateur_delete(p_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM utilisateur WHERE id = p_id;
END;
$$;

-- ============================================
-- CRÉATION DE L'UTILISATEUR ADMIN
-- ============================================
-- Pour créer l'admin, lance l'application et crée un compte normal,
-- puis exécute cette requête pour le promouvoir admin :
-- UPDATE utilisateur SET admin = TRUE WHERE login = 'admin@domaine.com';