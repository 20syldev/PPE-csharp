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
    totp_secret VARCHAR(500),
    totp_enabled BOOLEAN DEFAULT FALSE,
    recovery_codes VARCHAR(1000),
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
DROP FUNCTION IF EXISTS utilisateur_get_by_login(VARCHAR);
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
    code VARCHAR(10),
    totp_secret VARCHAR(500),
    totp_enabled BOOLEAN,
    recovery_codes VARCHAR(1000)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
        SELECT u.id, u.id_code, u.login, u.password, u.admin,
               u.nom, u.adresse, u.ville, u.code,
               u.totp_secret, u.totp_enabled, u.recovery_codes
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
    code VARCHAR(10),
    totp_secret VARCHAR(500),
    totp_enabled BOOLEAN,
    recovery_codes VARCHAR(1000)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
        SELECT u.id, u.id_code, u.login, u.password, u.admin,
               u.nom, u.adresse, u.ville, u.code,
               u.totp_secret, u.totp_enabled, u.recovery_codes
        FROM utilisateur u
        ORDER BY u.nom;
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
    UPDATE utilisateur
    SET nom = p_nom, adresse = p_adresse, ville = p_ville, code = p_code
    WHERE id = p_id;
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

-- Procédure de mise à jour des paramètres 2FA
CREATE OR REPLACE PROCEDURE utilisateur_update_2fa(
    p_id UUID,
    p_totp_secret VARCHAR(500),
    p_totp_enabled BOOLEAN,
    p_recovery_codes VARCHAR(1000)
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE utilisateur
    SET totp_secret = p_totp_secret,
        totp_enabled = p_totp_enabled,
        recovery_codes = p_recovery_codes
    WHERE id = p_id;
END;
$$;

-- Procédure de mise à jour du statut admin
CREATE OR REPLACE PROCEDURE utilisateur_update_admin(
    p_id UUID,
    p_admin BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE utilisateur
    SET admin = p_admin
    WHERE id = p_id;
END;
$$;

-- ============================================
-- TABLE HISTORIQUE DES MOTS DE PASSE
-- ============================================
-- Stocke les 3 derniers mots de passe hashés pour empêcher la réutilisation

CREATE TABLE password_history (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES utilisateur(id) ON DELETE CASCADE,
    password_hash VARCHAR(200) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_password_history_user_id ON password_history(user_id);

GRANT ALL PRIVILEGES ON TABLE password_history TO ppe;
GRANT USAGE, SELECT ON SEQUENCE password_history_id_seq TO ppe;

-- Fonction pour vérifier si un mot de passe existe dans l'historique des 3 derniers
CREATE OR REPLACE FUNCTION password_in_history(p_user_id UUID, p_password_hash VARCHAR(200))
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM password_history
        WHERE user_id = p_user_id AND password_hash = p_password_hash
        ORDER BY created_at DESC
        LIMIT 3
    );
END;
$$;

-- Fonction pour ajouter un mot de passe à l'historique et garder seulement les 3 derniers
CREATE OR REPLACE FUNCTION add_password_to_history(p_user_id UUID, p_password_hash VARCHAR(200))
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    -- Ajouter le nouveau mot de passe à l'historique
    INSERT INTO password_history (user_id, password_hash) VALUES (p_user_id, p_password_hash);

    -- Supprimer les anciens mots de passe au-delà des 3 derniers
    DELETE FROM password_history
    WHERE id IN (
        SELECT id FROM password_history
        WHERE user_id = p_user_id
        ORDER BY created_at DESC
        OFFSET 3
    );
END;
$$;

-- Procédure pour changer le mot de passe avec vérification de l'historique
-- Retourne: 0 = succès, 1 = mot de passe dans l'historique, 2 = erreur
CREATE OR REPLACE FUNCTION utilisateur_change_password(
    p_user_id UUID,
    p_new_password_hash VARCHAR(200)
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_old_password VARCHAR(200);
BEGIN
    -- Récupérer l'ancien mot de passe
    SELECT password INTO v_old_password
    FROM utilisateur
    WHERE id = p_user_id;

    IF v_old_password IS NULL THEN
        RETURN 2; -- Utilisateur non trouvé
    END IF;

    -- Vérifier si le nouveau mot de passe est identique à l'ancien
    IF v_old_password = p_new_password_hash THEN
        RETURN 1; -- Même mot de passe
    END IF;

    -- Vérifier si le mot de passe est dans l'historique des 3 derniers
    IF EXISTS (
        SELECT 1 FROM password_history
        WHERE user_id = p_user_id AND password_hash = p_new_password_hash
        ORDER BY created_at DESC
        LIMIT 3
    ) THEN
        RETURN 1; -- Mot de passe déjà utilisé récemment
    END IF;

    -- Ajouter l'ancien mot de passe à l'historique
    PERFORM add_password_to_history(p_user_id, v_old_password);

    -- Mettre à jour le mot de passe
    UPDATE utilisateur
    SET password = p_new_password_hash
    WHERE id = p_user_id;

    RETURN 0; -- Succès
END;
$$;

-- Trigger pour empêcher la réutilisation des 3 derniers mots de passe
CREATE OR REPLACE FUNCTION check_password_history()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    -- Vérifier seulement si le mot de passe a changé
    IF OLD.password IS DISTINCT FROM NEW.password THEN
        -- Vérifier si le nouveau mot de passe est dans l'historique
        IF EXISTS (
            SELECT 1 FROM password_history
            WHERE user_id = NEW.id AND password_hash = NEW.password
            ORDER BY created_at DESC
            LIMIT 3
        ) THEN
            RAISE EXCEPTION 'Ce mot de passe a déjà été utilisé récemment. Choisissez un mot de passe différent.';
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trigger_check_password_history
    BEFORE UPDATE OF password ON utilisateur
    FOR EACH ROW
    EXECUTE FUNCTION check_password_history();

-- ============================================
-- CRÉATION DE L'UTILISATEUR ADMIN
-- ============================================
-- Pour créer l'admin, lance l'application et crée un compte normal,
-- puis exécute cette requête pour le promouvoir admin :
-- UPDATE utilisateur SET admin = TRUE WHERE login = 'admin@domaine.com';